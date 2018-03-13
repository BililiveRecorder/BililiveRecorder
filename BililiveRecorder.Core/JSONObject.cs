#define PRETTY		//Comment out when you no longer need to read JSON to disable pretty Print system-wide
//Using doubles will cause errors in VectorTemplates.cs; Unity speaks floats
#define USEFLOAT	//Use floats for numbers instead of doubles	(enable if you're getting too many significant digits in string output)
//#define POOLING	//Currently using a build setting for this one (also it's experimental)

#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
using UnityEngine;
using Debug = UnityEngine.Debug;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
/*
Copyright (c) 2015 Matt Schoen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

public class JSONObject : IEnumerable
{
#if POOLING
	const int MAX_POOL_SIZE = 10000;
	public static Queue<JSONObject> releaseQueue = new Queue<JSONObject>();
#endif

    const int MAX_DEPTH = 100;
    const string INFINITY = "\"INFINITY\"";
    const string NEGINFINITY = "\"NEGINFINITY\"";
    const string NaN = "\"NaN\"";
    public static readonly char[] WHITESPACE = { ' ', '\r', '\n', '\t', '\uFEFF', '\u0009' };
    public enum Type { NULL, STRING, NUMBER, OBJECT, ARRAY, BOOL, BAKED }
    public bool isContainer { get { return (type == Type.ARRAY || type == Type.OBJECT); } }
    public Type type = Type.NULL;
    public int Count
    {
        get
        {
            if (list == null)
                return -1;
            return list.Count;
        }
    }
    public List<JSONObject> list;
    public List<string> keys;
    public string str;
#if USEFLOAT
    public float n;
    public float f
    {
        get
        {
            return n;
        }
    }
#else
	public double n;
	public float f {
		get {
			return (float)n;
		}
	}
#endif
    public bool useInt;
    public long i;
    public bool b;
    public delegate void AddJSONContents(JSONObject self);

    public static JSONObject nullJO { get { return Create(Type.NULL); } }   //an empty, null object
    public static JSONObject obj { get { return Create(Type.OBJECT); } }        //an empty object
    public static JSONObject arr { get { return Create(Type.ARRAY); } }     //an empty array

    public JSONObject(Type t)
    {
        type = t;
        switch (t)
        {
            case Type.ARRAY:
                list = new List<JSONObject>();
                break;
            case Type.OBJECT:
                list = new List<JSONObject>();
                keys = new List<string>();
                break;
        }
    }
    public JSONObject(bool b)
    {
        type = Type.BOOL;
        this.b = b;
    }
#if USEFLOAT
    public JSONObject(float f)
    {
        type = Type.NUMBER;
        n = f;
    }
#else
	public JSONObject(double d) {
		type = Type.NUMBER;
		n = d;
	}
#endif
    public JSONObject(int i)
    {
        type = Type.NUMBER;
        this.i = i;
        useInt = true;
        n = i;
    }
    public JSONObject(long l)
    {
        type = Type.NUMBER;
        i = l;
        useInt = true;
        n = l;
    }
    public JSONObject(Dictionary<string, string> dic)
    {
        type = Type.OBJECT;
        keys = new List<string>();
        list = new List<JSONObject>();
        //Not sure if it's worth removing the foreach here
        foreach (KeyValuePair<string, string> kvp in dic)
        {
            keys.Add(kvp.Key);
            list.Add(CreateStringObject(kvp.Value));
        }
    }
    public JSONObject(Dictionary<string, JSONObject> dic)
    {
        type = Type.OBJECT;
        keys = new List<string>();
        list = new List<JSONObject>();
        //Not sure if it's worth removing the foreach here
        foreach (KeyValuePair<string, JSONObject> kvp in dic)
        {
            keys.Add(kvp.Key);
            list.Add(kvp.Value);
        }
    }
    public JSONObject(AddJSONContents content)
    {
        content.Invoke(this);
    }
    public JSONObject(JSONObject[] objs)
    {
        type = Type.ARRAY;
        list = new List<JSONObject>(objs);
    }
    //Convenience function for creating a JSONObject containing a string.  This is not part of the constructor so that malformed JSON data doesn't just turn into a string object
    public static JSONObject StringObject(string val) { return CreateStringObject(val); }
    public void Absorb(JSONObject obj)
    {
        list.AddRange(obj.list);
        keys.AddRange(obj.keys);
        str = obj.str;
        n = obj.n;
        useInt = obj.useInt;
        i = obj.i;
        b = obj.b;
        type = obj.type;
    }
    public static JSONObject Create()
    {
#if POOLING
		JSONObject result = null;
		while(result == null && releaseQueue.Count > 0) {
			result = releaseQueue.Dequeue();
#if DEV
			//The following cases should NEVER HAPPEN (but they do...)
			if(result == null)
				Debug.WriteLine("wtf " + releaseQueue.Count);
			else if(result.list != null)
				Debug.WriteLine("wtflist " + result.list.Count);
#endif
		}
		if(result != null)
			return result;
#endif
        return new JSONObject();
    }
    public static JSONObject Create(Type t)
    {
        JSONObject obj = Create();
        obj.type = t;
        switch (t)
        {
            case Type.ARRAY:
                obj.list = new List<JSONObject>();
                break;
            case Type.OBJECT:
                obj.list = new List<JSONObject>();
                obj.keys = new List<string>();
                break;
        }
        return obj;
    }
    public static JSONObject Create(bool val)
    {
        JSONObject obj = Create();
        obj.type = Type.BOOL;
        obj.b = val;
        return obj;
    }
    public static JSONObject Create(float val)
    {
        JSONObject obj = Create();
        obj.type = Type.NUMBER;
        obj.n = val;
        return obj;
    }
    public static JSONObject Create(int val)
    {
        JSONObject obj = Create();
        obj.type = Type.NUMBER;
        obj.n = val;
        obj.useInt = true;
        obj.i = val;
        return obj;
    }
    public static JSONObject Create(long val)
    {
        JSONObject obj = Create();
        obj.type = Type.NUMBER;
        obj.n = val;
        obj.useInt = true;
        obj.i = val;
        return obj;
    }
    public static JSONObject CreateStringObject(string val)
    {
        JSONObject obj = Create();
        obj.type = Type.STRING;
        obj.str = val;
        return obj;
    }
    public static JSONObject CreateBakedObject(string val)
    {
        JSONObject bakedObject = Create();
        bakedObject.type = Type.BAKED;
        bakedObject.str = val;
        return bakedObject;
    }
    /// <summary>
    /// Create a JSONObject by parsing string data
    /// </summary>
    /// <param name="val">The string to be parsed</param>
    /// <param name="maxDepth">The maximum depth for the parser to search.  Set this to to 1 for the first level, 
    /// 2 for the first 2 levels, etc.  It defaults to -2 because -1 is the depth value that is parsed (see below)</param>
    /// <param name="storeExcessLevels">Whether to store levels beyond maxDepth in baked JSONObjects</param>
    /// <param name="strict">Whether to be strict in the parsing. For example, non-strict parsing will successfully 
    /// parse "a string" into a string-type </param>
    /// <returns></returns>
    public static JSONObject Create(string val, int maxDepth = -2, bool storeExcessLevels = false, bool strict = false)
    {
        JSONObject obj = Create();
        obj.Parse(val, maxDepth, storeExcessLevels, strict);
        return obj;
    }
    public static JSONObject Create(AddJSONContents content)
    {
        JSONObject obj = Create();
        content.Invoke(obj);
        return obj;
    }
    public static JSONObject Create(Dictionary<string, string> dic)
    {
        JSONObject obj = Create();
        obj.type = Type.OBJECT;
        obj.keys = new List<string>();
        obj.list = new List<JSONObject>();
        //Not sure if it's worth removing the foreach here
        foreach (KeyValuePair<string, string> kvp in dic)
        {
            obj.keys.Add(kvp.Key);
            obj.list.Add(CreateStringObject(kvp.Value));
        }
        return obj;
    }
    public JSONObject() { }
    #region PARSE
    public JSONObject(string str, int maxDepth = -2, bool storeExcessLevels = false, bool strict = false)
    {   //create a new JSONObject from a string (this will also create any children, and parse the whole string)
        Parse(str, maxDepth, storeExcessLevels, strict);
    }
    void Parse(string str, int maxDepth = -2, bool storeExcessLevels = false, bool strict = false)
    {
        if (!string.IsNullOrEmpty(str))
        {
            str = str.Trim(WHITESPACE);
            if (strict)
            {
                if (str[0] != '[' && str[0] != '{')
                {
                    type = Type.NULL;
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
					Debug.LogWarning
#else
                    Debug.WriteLine
#endif
                        ("Improper (strict) JSON formatting.  First character must be [ or {");
                    return;
                }
            }
            if (str.Length > 0)
            {
#if UNITY_WP8 || UNITY_WSA
				if (str == "true") {
					type = Type.BOOL;
					b = true;
				} else if (str == "false") {
					type = Type.BOOL;
					b = false;
				} else if (str == "null") {
					type = Type.NULL;
#else
                if (string.Compare(str, "true", true) == 0)
                {
                    type = Type.BOOL;
                    b = true;
                }
                else if (string.Compare(str, "false", true) == 0)
                {
                    type = Type.BOOL;
                    b = false;
                }
                else if (string.Compare(str, "null", true) == 0)
                {
                    type = Type.NULL;
#endif
#if USEFLOAT
                }
                else if (str == INFINITY)
                {
                    type = Type.NUMBER;
                    n = float.PositiveInfinity;
                }
                else if (str == NEGINFINITY)
                {
                    type = Type.NUMBER;
                    n = float.NegativeInfinity;
                }
                else if (str == NaN)
                {
                    type = Type.NUMBER;
                    n = float.NaN;
#else
				} else if(str == INFINITY) {
					type = Type.NUMBER;
					n = double.PositiveInfinity;
				} else if(str == NEGINFINITY) {
					type = Type.NUMBER;
					n = double.NegativeInfinity;
				} else if(str == NaN) {
					type = Type.NUMBER;
					n = double.NaN;
#endif
                }
                else if (str[0] == '"')
                {
                    type = Type.STRING;
                    this.str = str.Substring(1, str.Length - 2);
                }
                else
                {
                    int tokenTmp = 1;
                    /*
					 * Checking for the following formatting (www.json.org)
					 * object - {"field1":value,"field2":value}
					 * array - [value,value,value]
					 * value - string	- "string"
					 *		 - number	- 0.0
					 *		 - bool		- true -or- false
					 *		 - null		- null
					 */
                    int offset = 0;
                    switch (str[offset])
                    {
                        case '{':
                            type = Type.OBJECT;
                            keys = new List<string>();
                            list = new List<JSONObject>();
                            break;
                        case '[':
                            type = Type.ARRAY;
                            list = new List<JSONObject>();
                            break;
                        default:
                            try
                            {
#if USEFLOAT
                                n = System.Convert.ToSingle(str, CultureInfo.InvariantCulture);
#else
								n = System.Convert.ToDouble(str, CultureInfo.InvariantCulture);		 
#endif
                                if (!str.Contains("."))
                                {
                                    i = System.Convert.ToInt64(str);
                                    useInt = true;
                                }
                                type = Type.NUMBER;
                            }
                            catch (System.FormatException)
                            {
                                type = Type.NULL;
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
								Debug.LogWarning
#else
                                Debug.WriteLine
#endif
                                ("improper JSON formatting:" + str);
                            }
                            return;
                    }
                    string propName = "";
                    bool openQuote = false;
                    bool inProp = false;
                    int depth = 0;
                    while (++offset < str.Length)
                    {
                        if (System.Array.IndexOf(WHITESPACE, str[offset]) > -1)
                            continue;
                        if (str[offset] == '\\')
                        {
                            offset += 1;
                            continue;
                        }
                        if (str[offset] == '"')
                        {
                            if (openQuote)
                            {
                                if (!inProp && depth == 0 && type == Type.OBJECT)
                                    propName = str.Substring(tokenTmp + 1, offset - tokenTmp - 1);
                                openQuote = false;
                            }
                            else
                            {
                                if (depth == 0 && type == Type.OBJECT)
                                    tokenTmp = offset;
                                openQuote = true;
                            }
                        }
                        if (openQuote)
                            continue;
                        if (type == Type.OBJECT && depth == 0)
                        {
                            if (str[offset] == ':')
                            {
                                tokenTmp = offset + 1;
                                inProp = true;
                            }
                        }

                        if (str[offset] == '[' || str[offset] == '{')
                        {
                            depth++;
                        }
                        else if (str[offset] == ']' || str[offset] == '}')
                        {
                            depth--;
                        }
                        //if  (encounter a ',' at top level)  || a closing ]/}
                        if ((str[offset] == ',' && depth == 0) || depth < 0)
                        {
                            inProp = false;
                            string inner = str.Substring(tokenTmp, offset - tokenTmp).Trim(WHITESPACE);
                            if (inner.Length > 0)
                            {
                                if (type == Type.OBJECT)
                                    keys.Add(propName);
                                if (maxDepth != -1)                                                         //maxDepth of -1 is the end of the line
                                    list.Add(Create(inner, (maxDepth < -1) ? -2 : maxDepth - 1, storeExcessLevels));
                                else if (storeExcessLevels)
                                    list.Add(CreateBakedObject(inner));

                            }
                            tokenTmp = offset + 1;
                        }
                    }
                }
            }
            else type = Type.NULL;
        }
        else type = Type.NULL;  //If the string is missing, this is a null
                                //Profiler.EndSample();
    }
    #endregion
    public bool IsNumber { get { return type == Type.NUMBER; } }
    public bool IsNull { get { return type == Type.NULL; } }
    public bool IsString { get { return type == Type.STRING; } }
    public bool IsBool { get { return type == Type.BOOL; } }
    public bool IsArray { get { return type == Type.ARRAY; } }
    public bool IsObject { get { return type == Type.OBJECT || type == Type.BAKED; } }
    public void Add(bool val)
    {
        Add(Create(val));
    }
    public void Add(float val)
    {
        Add(Create(val));
    }
    public void Add(int val)
    {
        Add(Create(val));
    }
    public void Add(string str)
    {
        Add(CreateStringObject(str));
    }
    public void Add(AddJSONContents content)
    {
        Add(Create(content));
    }
    public void Add(JSONObject obj)
    {
        if (obj)
        {       //Don't do anything if the object is null
            if (type != Type.ARRAY)
            {
                type = Type.ARRAY;      //Congratulations, son, you're an ARRAY now
                if (list == null)
                    list = new List<JSONObject>();
            }
            list.Add(obj);
        }
    }
    public void AddField(string name, bool val)
    {
        AddField(name, Create(val));
    }
    public void AddField(string name, float val)
    {
        AddField(name, Create(val));
    }
    public void AddField(string name, int val)
    {
        AddField(name, Create(val));
    }
    public void AddField(string name, long val)
    {
        AddField(name, Create(val));
    }
    public void AddField(string name, AddJSONContents content)
    {
        AddField(name, Create(content));
    }
    public void AddField(string name, string val)
    {
        AddField(name, CreateStringObject(val));
    }
    public void AddField(string name, JSONObject obj)
    {
        if (obj)
        {       //Don't do anything if the object is null
            if (type != Type.OBJECT)
            {
                if (keys == null)
                    keys = new List<string>();
                if (type == Type.ARRAY)
                {
                    for (int i = 0; i < list.Count; i++)
                        keys.Add(i + "");
                }
                else
                    if (list == null)
                    list = new List<JSONObject>();
                type = Type.OBJECT;     //Congratulations, son, you're an OBJECT now
            }
            keys.Add(name);
            list.Add(obj);
        }
    }
    public void SetField(string name, string val) { SetField(name, CreateStringObject(val)); }
    public void SetField(string name, bool val) { SetField(name, Create(val)); }
    public void SetField(string name, float val) { SetField(name, Create(val)); }
    public void SetField(string name, int val) { SetField(name, Create(val)); }
    public void SetField(string name, JSONObject obj)
    {
        if (HasField(name))
        {
            list.Remove(this[name]);
            keys.Remove(name);
        }
        AddField(name, obj);
    }
    public void RemoveField(string name)
    {
        if (keys.IndexOf(name) > -1)
        {
            list.RemoveAt(keys.IndexOf(name));
            keys.Remove(name);
        }
    }
    public delegate void FieldNotFound(string name);
    public delegate void GetFieldResponse(JSONObject obj);
    public bool GetField(out bool field, string name, bool fallback)
    {
        field = fallback;
        return GetField(ref field, name);
    }
    public bool GetField(ref bool field, string name, FieldNotFound fail = null)
    {
        if (type == Type.OBJECT)
        {
            int index = keys.IndexOf(name);
            if (index >= 0)
            {
                field = list[index].b;
                return true;
            }
        }
        if (fail != null) fail.Invoke(name);
        return false;
    }
#if USEFLOAT
    public bool GetField(out float field, string name, float fallback)
    {
#else
	public bool GetField(out double field, string name, double fallback) {
#endif
        field = fallback;
        return GetField(ref field, name);
    }
#if USEFLOAT
    public bool GetField(ref float field, string name, FieldNotFound fail = null)
    {
#else
	public bool GetField(ref double field, string name, FieldNotFound fail = null) {
#endif
        if (type == Type.OBJECT)
        {
            int index = keys.IndexOf(name);
            if (index >= 0)
            {
                field = list[index].n;
                return true;
            }
        }
        if (fail != null) fail.Invoke(name);
        return false;
    }
    public bool GetField(out int field, string name, int fallback)
    {
        field = fallback;
        return GetField(ref field, name);
    }
    public bool GetField(ref int field, string name, FieldNotFound fail = null)
    {
        if (IsObject)
        {
            int index = keys.IndexOf(name);
            if (index >= 0)
            {
                field = (int)list[index].n;
                return true;
            }
        }
        if (fail != null) fail.Invoke(name);
        return false;
    }
    public bool GetField(out long field, string name, long fallback)
    {
        field = fallback;
        return GetField(ref field, name);
    }
    public bool GetField(ref long field, string name, FieldNotFound fail = null)
    {
        if (IsObject)
        {
            int index = keys.IndexOf(name);
            if (index >= 0)
            {
                field = (long)list[index].n;
                return true;
            }
        }
        if (fail != null) fail.Invoke(name);
        return false;
    }
    public bool GetField(out uint field, string name, uint fallback)
    {
        field = fallback;
        return GetField(ref field, name);
    }
    public bool GetField(ref uint field, string name, FieldNotFound fail = null)
    {
        if (IsObject)
        {
            int index = keys.IndexOf(name);
            if (index >= 0)
            {
                field = (uint)list[index].n;
                return true;
            }
        }
        if (fail != null) fail.Invoke(name);
        return false;
    }
    public bool GetField(out string field, string name, string fallback)
    {
        field = fallback;
        return GetField(ref field, name);
    }
    public bool GetField(ref string field, string name, FieldNotFound fail = null)
    {
        if (IsObject)
        {
            int index = keys.IndexOf(name);
            if (index >= 0)
            {
                field = list[index].str;
                return true;
            }
        }
        if (fail != null) fail.Invoke(name);
        return false;
    }
    public void GetField(string name, GetFieldResponse response, FieldNotFound fail = null)
    {
        if (response != null && IsObject)
        {
            int index = keys.IndexOf(name);
            if (index >= 0)
            {
                response.Invoke(list[index]);
                return;
            }
        }
        if (fail != null) fail.Invoke(name);
    }
    public JSONObject GetField(string name)
    {
        if (IsObject)
            for (int i = 0; i < keys.Count; i++)
                if (keys[i] == name)
                    return list[i];
        return null;
    }
    public bool HasFields(string[] names)
    {
        if (!IsObject)
            return false;
        for (int i = 0; i < names.Length; i++)
            if (!keys.Contains(names[i]))
                return false;
        return true;
    }
    public bool HasField(string name)
    {
        if (!IsObject)
            return false;
        for (int i = 0; i < keys.Count; i++)
            if (keys[i] == name)
                return true;
        return false;
    }
    public void Clear()
    {
        type = Type.NULL;
        if (list != null)
            list.Clear();
        if (keys != null)
            keys.Clear();
        str = "";
        n = 0;
        b = false;
    }
    /// <summary>
    /// Copy a JSONObject. This could probably work better
    /// </summary>
    /// <returns></returns>
    public JSONObject Copy()
    {
        return Create(Print());
    }
    /*
	 * The Merge function is experimental. Use at your own risk.
	 */
    public void Merge(JSONObject obj)
    {
        MergeRecur(this, obj);
    }
    /// <summary>
    /// Merge object right into left recursively
    /// </summary>
    /// <param name="left">The left (base) object</param>
    /// <param name="right">The right (new) object</param>
    static void MergeRecur(JSONObject left, JSONObject right)
    {
        if (left.type == Type.NULL)
            left.Absorb(right);
        else if (left.type == Type.OBJECT && right.type == Type.OBJECT)
        {
            for (int i = 0; i < right.list.Count; i++)
            {
                string key = right.keys[i];
                if (right[i].isContainer)
                {
                    if (left.HasField(key))
                        MergeRecur(left[key], right[i]);
                    else
                        left.AddField(key, right[i]);
                }
                else
                {
                    if (left.HasField(key))
                        left.SetField(key, right[i]);
                    else
                        left.AddField(key, right[i]);
                }
            }
        }
        else if (left.type == Type.ARRAY && right.type == Type.ARRAY)
        {
            if (right.Count > left.Count)
            {
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
				Debug.LogError
#else
                Debug.WriteLine
#endif
                ("Cannot merge arrays when right object has more elements");
                return;
            }
            for (int i = 0; i < right.list.Count; i++)
            {
                if (left[i].type == right[i].type)
                {           //Only overwrite with the same type
                    if (left[i].isContainer)
                        MergeRecur(left[i], right[i]);
                    else
                    {
                        left[i] = right[i];
                    }
                }
            }
        }
    }
    public void Bake()
    {
        if (type != Type.BAKED)
        {
            str = Print();
            type = Type.BAKED;
        }
    }
    public IEnumerable BakeAsync()
    {
        if (type != Type.BAKED)
        {
            foreach (string s in PrintAsync())
            {
                if (s == null)
                    yield return s;
                else
                {
                    str = s;
                }
            }
            type = Type.BAKED;
        }
    }
#pragma warning disable 219
    public string Print(bool pretty = false)
    {
        StringBuilder builder = new StringBuilder();
        Stringify(0, builder, pretty);
        return builder.ToString();
    }
    public IEnumerable<string> PrintAsync(bool pretty = false)
    {
        StringBuilder builder = new StringBuilder();
        printWatch.Reset();
        printWatch.Start();
        foreach (IEnumerable e in StringifyAsync(0, builder, pretty))
        {
            yield return null;
        }
        yield return builder.ToString();
    }
#pragma warning restore 219
    #region STRINGIFY
    const float maxFrameTime = 0.008f;
    static readonly Stopwatch printWatch = new Stopwatch();
    IEnumerable StringifyAsync(int depth, StringBuilder builder, bool pretty = false)
    {   //Convert the JSONObject into a string
        //Profiler.BeginSample("JSONprint");
        if (depth++ > MAX_DEPTH)
        {
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
			Debug.Log
#else
            Debug.WriteLine
#endif
            ("reached max depth!");
            yield break;
        }
        if (printWatch.Elapsed.TotalSeconds > maxFrameTime)
        {
            printWatch.Reset();
            yield return null;
            printWatch.Start();
        }
        switch (type)
        {
            case Type.BAKED:
                builder.Append(str);
                break;
            case Type.STRING:
                builder.AppendFormat("\"{0}\"", str);
                break;
            case Type.NUMBER:
                if (useInt)
                {
                    builder.Append(i.ToString());
                }
                else
                {
#if USEFLOAT
                    if (float.IsInfinity(n))
                        builder.Append(INFINITY);
                    else if (float.IsNegativeInfinity(n))
                        builder.Append(NEGINFINITY);
                    else if (float.IsNaN(n))
                        builder.Append(NaN);
#else
				if(double.IsInfinity(n))
					builder.Append(INFINITY);
				else if(double.IsNegativeInfinity(n))
					builder.Append(NEGINFINITY);
				else if(double.IsNaN(n))
					builder.Append(NaN);
#endif
                    else
                        builder.Append(n.ToString());
                }
                break;
            case Type.OBJECT:
                builder.Append("{");
                if (list.Count > 0)
                {
#if (PRETTY)        //for a bit more readability, comment the define above to disable system-wide
                    if (pretty)
                        builder.Append("\n");
#endif
                    for (int i = 0; i < list.Count; i++)
                    {
                        string key = keys[i];
                        JSONObject obj = list[i];
                        if (obj)
                        {
#if (PRETTY)
                            if (pretty)
                                for (int j = 0; j < depth; j++)
                                    builder.Append("\t"); //for a bit more readability
#endif
                            builder.AppendFormat("\"{0}\":", key);
                            foreach (IEnumerable e in obj.StringifyAsync(depth, builder, pretty))
                                yield return e;
                            builder.Append(",");
#if (PRETTY)
                            if (pretty)
                                builder.Append("\n");
#endif
                        }
                    }
#if (PRETTY)
                    if (pretty)
                        builder.Length -= 2;
                    else
#endif
                        builder.Length--;
                }
#if (PRETTY)
                if (pretty && list.Count > 0)
                {
                    builder.Append("\n");
                    for (int j = 0; j < depth - 1; j++)
                        builder.Append("\t"); //for a bit more readability
                }
#endif
                builder.Append("}");
                break;
            case Type.ARRAY:
                builder.Append("[");
                if (list.Count > 0)
                {
#if (PRETTY)
                    if (pretty)
                        builder.Append("\n"); //for a bit more readability
#endif
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i])
                        {
#if (PRETTY)
                            if (pretty)
                                for (int j = 0; j < depth; j++)
                                    builder.Append("\t"); //for a bit more readability
#endif
                            foreach (IEnumerable e in list[i].StringifyAsync(depth, builder, pretty))
                                yield return e;
                            builder.Append(",");
#if (PRETTY)
                            if (pretty)
                                builder.Append("\n"); //for a bit more readability
#endif
                        }
                    }
#if (PRETTY)
                    if (pretty)
                        builder.Length -= 2;
                    else
#endif
                        builder.Length--;
                }
#if (PRETTY)
                if (pretty && list.Count > 0)
                {
                    builder.Append("\n");
                    for (int j = 0; j < depth - 1; j++)
                        builder.Append("\t"); //for a bit more readability
                }
#endif
                builder.Append("]");
                break;
            case Type.BOOL:
                if (b)
                    builder.Append("true");
                else
                    builder.Append("false");
                break;
            case Type.NULL:
                builder.Append("null");
                break;
        }
        //Profiler.EndSample();
    }
    //TODO: Refactor Stringify functions to share core logic
    /*
	 * I know, I know, this is really bad form.  It turns out that there is a
	 * significant amount of garbage created when calling as a coroutine, so this
	 * method is duplicated.  Hopefully there won't be too many future changes, but
	 * I would still like a more elegant way to optionaly yield
	 */
    void Stringify(int depth, StringBuilder builder, bool pretty = false)
    {   //Convert the JSONObject into a string
        //Profiler.BeginSample("JSONprint");
        if (depth++ > MAX_DEPTH)
        {
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
			Debug.Log
#else
            Debug.WriteLine
#endif
            ("reached max depth!");
            return;
        }
        switch (type)
        {
            case Type.BAKED:
                builder.Append(str);
                break;
            case Type.STRING:
                builder.AppendFormat("\"{0}\"", str);
                break;
            case Type.NUMBER:
                if (useInt)
                {
                    builder.Append(i.ToString());
                }
                else
                {
#if USEFLOAT
                    if (float.IsInfinity(n))
                        builder.Append(INFINITY);
                    else if (float.IsNegativeInfinity(n))
                        builder.Append(NEGINFINITY);
                    else if (float.IsNaN(n))
                        builder.Append(NaN);
#else
				if(double.IsInfinity(n))
					builder.Append(INFINITY);
				else if(double.IsNegativeInfinity(n))
					builder.Append(NEGINFINITY);
				else if(double.IsNaN(n))
					builder.Append(NaN);
#endif
                    else
                        builder.Append(n.ToString());
                }
                break;
            case Type.OBJECT:
                builder.Append("{");
                if (list.Count > 0)
                {
#if (PRETTY)        //for a bit more readability, comment the define above to disable system-wide
                    if (pretty)
                        builder.Append("\n");
#endif
                    for (int i = 0; i < list.Count; i++)
                    {
                        string key = keys[i];
                        JSONObject obj = list[i];
                        if (obj)
                        {
#if (PRETTY)
                            if (pretty)
                                for (int j = 0; j < depth; j++)
                                    builder.Append("\t"); //for a bit more readability
#endif
                            builder.AppendFormat("\"{0}\":", key);
                            obj.Stringify(depth, builder, pretty);
                            builder.Append(",");
#if (PRETTY)
                            if (pretty)
                                builder.Append("\n");
#endif
                        }
                    }
#if (PRETTY)
                    if (pretty)
                        builder.Length -= 2;
                    else
#endif
                        builder.Length--;
                }
#if (PRETTY)
                if (pretty && list.Count > 0)
                {
                    builder.Append("\n");
                    for (int j = 0; j < depth - 1; j++)
                        builder.Append("\t"); //for a bit more readability
                }
#endif
                builder.Append("}");
                break;
            case Type.ARRAY:
                builder.Append("[");
                if (list.Count > 0)
                {
#if (PRETTY)
                    if (pretty)
                        builder.Append("\n"); //for a bit more readability
#endif
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i])
                        {
#if (PRETTY)
                            if (pretty)
                                for (int j = 0; j < depth; j++)
                                    builder.Append("\t"); //for a bit more readability
#endif
                            list[i].Stringify(depth, builder, pretty);
                            builder.Append(",");
#if (PRETTY)
                            if (pretty)
                                builder.Append("\n"); //for a bit more readability
#endif
                        }
                    }
#if (PRETTY)
                    if (pretty)
                        builder.Length -= 2;
                    else
#endif
                        builder.Length--;
                }
#if (PRETTY)
                if (pretty && list.Count > 0)
                {
                    builder.Append("\n");
                    for (int j = 0; j < depth - 1; j++)
                        builder.Append("\t"); //for a bit more readability
                }
#endif
                builder.Append("]");
                break;
            case Type.BOOL:
                if (b)
                    builder.Append("true");
                else
                    builder.Append("false");
                break;
            case Type.NULL:
                builder.Append("null");
                break;
        }
        //Profiler.EndSample();
    }
    #endregion
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
	public static implicit operator WWWForm(JSONObject obj) {
		WWWForm form = new WWWForm();
		for(int i = 0; i < obj.list.Count; i++) {
			string key = i + "";
			if(obj.type == Type.OBJECT)
				key = obj.keys[i];
			string val = obj.list[i].ToString();
			if(obj.list[i].type == Type.STRING)
				val = val.Replace("\"", "");
			form.AddField(key, val);
		}
		return form;
	}
#endif
    public JSONObject this[int index]
    {
        get
        {
            if (list.Count > index) return list[index];
            return null;
        }
        set
        {
            if (list.Count > index)
                list[index] = value;
        }
    }
    public JSONObject this[string index]
    {
        get
        {
            return GetField(index);
        }
        set
        {
            SetField(index, value);
        }
    }
    public override string ToString()
    {
        return Print();
    }
    public string ToString(bool pretty)
    {
        return Print(pretty);
    }
    public Dictionary<string, string> ToDictionary()
    {
        if (type == Type.OBJECT)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            for (int i = 0; i < list.Count; i++)
            {
                JSONObject val = list[i];
                switch (val.type)
                {
                    case Type.STRING: result.Add(keys[i], val.str); break;
                    case Type.NUMBER: result.Add(keys[i], val.n + ""); break;
                    case Type.BOOL: result.Add(keys[i], val.b + ""); break;
                    default:
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
						Debug.LogWarning
#else
                        Debug.WriteLine
#endif
                        ("Omitting object: " + keys[i] + " in dictionary conversion");
                        break;
                }
            }
            return result;
        }
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
		Debug.Log
#else
        Debug.WriteLine
#endif
        ("Tried to turn non-Object JSONObject into a dictionary");
        return null;
    }
    public static implicit operator bool(JSONObject o)
    {
        return o != null;
    }
#if POOLING
	static bool pool = true;
	public static void ClearPool() {
		pool = false;
		releaseQueue.Clear();
		pool = true;
	}

	~JSONObject() {
		if(pool && releaseQueue.Count < MAX_POOL_SIZE) {
			type = Type.NULL;
			list = null;
			keys = null;
			str = "";
			n = 0;
			b = false;
			releaseQueue.Enqueue(this);
		}
	}
#endif

    IEnumerator IEnumerable.GetEnumerator()
    {
        return (IEnumerator)GetEnumerator();
    }

    public JSONObjectEnumer GetEnumerator()
    {
        return new JSONObjectEnumer(this);
    }
}

public class JSONObjectEnumer : IEnumerator
{
    public JSONObject _jobj;

    // Enumerators are positioned before the first element
    // until the first MoveNext() call.
    int position = -1;

    public JSONObjectEnumer(JSONObject jsonObject)
    {
        Debug.Assert(jsonObject.isContainer); //must be an array or object to itterate
        _jobj = jsonObject;
    }

    public bool MoveNext()
    {
        position++;
        return (position < _jobj.Count);
    }

    public void Reset()
    {
        position = -1;
    }

    object IEnumerator.Current
    {
        get
        {
            return Current;
        }
    }

    public JSONObject Current
    {
        get
        {
            if (_jobj.IsArray)
            {
                return _jobj[position];
            }
            else
            {
                string key = _jobj.keys[position];
                return _jobj[key];
            }
        }
    }
}
