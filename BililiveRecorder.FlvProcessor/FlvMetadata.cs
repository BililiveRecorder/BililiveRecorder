using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BililiveRecorder.FlvProcessor
{
    public class FlvMetadata : IFlvMetadata
    {
        public IDictionary<string, object> Meta { get; set; } = new Dictionary<string, object>();

        public FlvMetadata()
        {
            Meta["duration"] = 0.0;
            Meta["lasttimestamp"] = 0.0;
        }

        public FlvMetadata(byte[] data)
        {
            Meta = _Decode(data);

            if (!Meta.ContainsKey("duration"))
            {
                Meta["duration"] = 0.0;
            }

            if (!Meta.ContainsKey("lasttimestamp"))
            {
                Meta["lasttimestamp"] = 0.0;
            }
        }

        public byte[] ToBytes()
        {
            return _Encode();
        }


        #region - Encode -

        private byte[] _Encode()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                const string onMetaData = "onMetaData";
                ms.WriteByte((byte)AMFTypes.String);
                UInt16 strSize = (UInt16)onMetaData.Length;
                byte[] strSizeb = BitConverter.GetBytes(strSize).ToBE();
                ms.Write(strSizeb, 0, strSizeb.Length);
                ms.Write(Encoding.ASCII.GetBytes(onMetaData), 0, onMetaData.Length);
                ms.WriteByte((byte)AMFTypes.Array);
                byte[] asize = BitConverter.GetBytes(Meta.Keys.Count).ToBE();
                ms.Write(asize, 0, asize.Length);
                foreach (string key in Meta.Keys)
                {
                    object val = Meta[key];
                    byte[] valBytes = _EncodeVal(val);
                    if (!string.IsNullOrWhiteSpace(key) && valBytes != null)
                    {
                        byte[] keyBytes = _EncodeKey(key);
                        ms.Write(keyBytes, 0, keyBytes.Length);
                        ms.Write(valBytes, 0, valBytes.Length);
                    }
                }

                /* *
                 * SCRIPTDATAVARIABLEEND
                 * Script Data Variable End
                 * Type: UI24
                 * Always 9
                 * */
                ms.WriteByte(0x0);
                ms.WriteByte(0x0);
                ms.WriteByte((byte)AMFTypes.End);


                return ms.ToArray();
            }
        }
        private byte[] _EncodeKey(string key)
        {
            byte[] ret = new byte[2 + key.Length];      // 2 for the size at the front
            UInt16 strSize = (UInt16)key.Length;
            byte[] strSizeb = BitConverter.GetBytes(strSize).ToBE();
            Buffer.BlockCopy(strSizeb, 0, ret, 0, strSizeb.Length);
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(key), 0, ret, 2, key.Length);
            return ret;
        }
        private byte[] _EncodeVal(object val)
        {
            if (val is double num)
            {
                byte[] ret = new byte[1 + sizeof(double)];
                ret[0] = (byte)AMFTypes.Number;
                byte[] numbits = BitConverter.GetBytes(num).ToBE();
                Buffer.BlockCopy(numbits, 0, ret, 1, numbits.Length);
                return ret;
            }
            else if (val is string)
            {
                string str = val as string;
                byte[] ret = new byte[3 + str.Length];
                ret[0] = (byte)AMFTypes.String;
                UInt16 strSize = (UInt16)str.Length;
                byte[] strSizeb = BitConverter.GetBytes(strSize).ToBE();
                Buffer.BlockCopy(strSizeb, 0, ret, 1, strSizeb.Length);
                Buffer.BlockCopy(Encoding.ASCII.GetBytes(str), 0, ret, 3, str.Length);
                return ret;
            }
            else if (val is byte bit)
            {
                byte[] ret = new byte[2];
                ret[0] = (byte)AMFTypes.Boolean;
                ret[1] = bit;
                return ret;
            }
            else
            {
                Debug.Write(string.Format("Unknown Value type: {0}\n", val?.GetType()?.Name));
                return null;
            }
        }

        #endregion

        #region - Decode -

        private static string _DecodeKey(byte[] buff, ref int _readHead)
        {
            // get length of string name
            byte[] flip = new byte[sizeof(short)];
            flip[0] = buff[_readHead++];
            flip[1] = buff[_readHead++];
            ushort klen = BitConverter.ToUInt16(flip.ToBE(), 0);
            string name = Encoding.Default.GetString(buff, _readHead, klen);
            _readHead += klen;
            return name;
        }

        private static object _DecodeVal(byte[] buff, ref int _readHead)
        {
            byte type = buff[_readHead++];
            AMFTypes amfType = (AMFTypes)Enum.ToObject(typeof(AMFTypes), (int)type);
            switch (amfType)
            {
                case AMFTypes.String:
                    return _DecodeKey(buff, ref _readHead);
                case AMFTypes.Number:
                    byte[] flip = new byte[sizeof(double)];
                    Buffer.BlockCopy(buff, _readHead, flip, 0, flip.Length);
                    double num = BitConverter.ToDouble(flip.ToBE(), 0);
                    _readHead += sizeof(double);
                    return num;
                case AMFTypes.Boolean:
                    byte b = buff[_readHead++];
                    return b;
                case AMFTypes.End:
                    return null;
                default:
                    throw new MissingMethodException();
            }
        }

        private static IDictionary<string, object> _Decode(byte[] buff)
        {
            IDictionary<string, object> keyval = new Dictionary<string, object>();
            int _readHead = 0;
            // get the onMetadata 
            string onMeta = _DecodeVal(buff, ref _readHead) as string;
            // read array type
            byte type = buff[_readHead++];
            Debug.Assert(type == (byte)AMFTypes.Array || type == (byte)AMFTypes.Object);
            if (type == (byte)AMFTypes.Array)
            {
                byte[] alen = new byte[sizeof(int)];
                Buffer.BlockCopy(buff, _readHead, alen, 0, alen.Length);
                _readHead += alen.Length;
                int arrayLen = BitConverter.ToInt32(alen.ToBE(), 0);
                Debug.Write(string.Format("onMetaData Array Len: {0}\n", arrayLen));
            }
            else if (type == (byte)AMFTypes.Object)
            {
                Debug.Write("onMetaData isn't an Array but Object!\n");
            }
            else
            {
                throw new Exception("Parse Script Tag Error"); // TODO: custom Exception
            }
            while (_readHead <= buff.Length - 1)
            {
                string key = _DecodeKey(buff, ref _readHead);
                object val = _DecodeVal(buff, ref _readHead);
                Debug.Write(string.Format("Parse Script Tag: {0} => {1}\n", key, val));
                keyval[key] = val;
            }
            return keyval;
        }

        #endregion

    }
}
