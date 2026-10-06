using System;
using System.Collections.Generic;

#nullable enable
namespace BililiveRecorder.Core.Api.Danmaku
{
    /// <summary>
    /// <c>SEND_GIFT_V2</c> 消息中 <c>data.pb</c> 的解析结果，对应网页端的 protobuf 类型 <c>bilibili.live.gift.v1.SendGiftBroadcast</c>，只保留录播姬需要的字段。
    /// <para>一条消息可能包含多件礼物，例如盲盒的批量结果。</para>
    /// </summary>
    internal sealed class SendGiftV2Broadcast
    {
        public long Uid { get; private set; }

        public string? Uname { get; private set; }

        /// <summary>
        /// 网页端仅在此项为 true 时处理该消息。
        /// </summary>
        public bool Switch { get; private set; }

        public List<SendGiftV2GiftItem> GiftList { get; } = new List<SendGiftV2GiftItem>();

        public static SendGiftV2Broadcast Parse(ReadOnlySpan<byte> data)
        {
            var result = new SendGiftV2Broadcast();
            var reader = new ProtobufReader(data);

            while (reader.TryReadTag(out var fieldNumber, out var wireType))
            {
                switch (fieldNumber)
                {
                    case 1 when wireType == ProtobufWireType.Varint:
                        result.Uid = reader.ReadInt64();
                        break;
                    case 2 when wireType == ProtobufWireType.LengthDelimited:
                        result.Uname = reader.ReadString();
                        break;
                    case 10 when wireType == ProtobufWireType.LengthDelimited:
                        result.GiftList.Add(SendGiftV2GiftItem.Parse(reader.ReadBytes()));
                        break;
                    case 11 when wireType == ProtobufWireType.Varint:
                        result.Switch = reader.ReadBool();
                        break;
                    default:
                        reader.SkipField(wireType);
                        break;
                }
            }

            return result;
        }
    }

    internal sealed class SendGiftV2GiftItem
    {
        public long GiftId { get; private set; }

        public string? GiftName { get; private set; }

        public int Num { get; private set; }

        public long Price { get; private set; }

        public long TotalCoin { get; private set; }

        public string? CoinType { get; private set; }

        public static SendGiftV2GiftItem Parse(ReadOnlySpan<byte> data)
        {
            var result = new SendGiftV2GiftItem();
            var reader = new ProtobufReader(data);

            while (reader.TryReadTag(out var fieldNumber, out var wireType))
            {
                switch (fieldNumber)
                {
                    case 1 when wireType == ProtobufWireType.Varint:
                        result.GiftId = reader.ReadInt64();
                        break;
                    case 2 when wireType == ProtobufWireType.LengthDelimited:
                        result.GiftName = reader.ReadString();
                        break;
                    case 3 when wireType == ProtobufWireType.Varint:
                        result.Num = (int)reader.ReadInt64();
                        break;
                    case 5 when wireType == ProtobufWireType.Varint:
                        result.Price = reader.ReadInt64();
                        break;
                    case 7 when wireType == ProtobufWireType.Varint:
                        result.TotalCoin = reader.ReadInt64();
                        break;
                    case 8 when wireType == ProtobufWireType.LengthDelimited:
                        result.CoinType = reader.ReadString();
                        break;
                    default:
                        reader.SkipField(wireType);
                        break;
                }
            }

            return result;
        }
    }
}
