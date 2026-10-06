using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BrowserSelector
{
    /// <summary>
    /// A small JSON reader/writer, so the app doesn't load System.Web just to read settings and
    /// browser profile lists. Values are Dictionary&lt;string, object&gt;, List&lt;object&gt;, string,
    /// double, bool or null.
    /// </summary>
    public static class Json
    {
        public static object Parse(string text)
        {
            int i = 0;
            var value = ReadValue(text, ref i);
            SkipSpace(text, ref i);
            if (i != text.Length) throw Error(i);
            return value;
        }

        public static string Write(object value)
        {
            var sb = new StringBuilder();
            WriteValue(sb, value, 0);
            return sb.ToString();
        }

        // ---- reading

        static object ReadValue(string s, ref int i)
        {
            SkipSpace(s, ref i);
            if (i >= s.Length) throw Error(i);
            switch (s[i])
            {
                case '{': return ReadObject(s, ref i);
                case '[': return ReadArray(s, ref i);
                case '"': return ReadString(s, ref i);
                case 't': Expect(s, ref i, "true"); return true;
                case 'f': Expect(s, ref i, "false"); return false;
                case 'n': Expect(s, ref i, "null"); return null;
                default: return ReadNumber(s, ref i);
            }
        }

        static Dictionary<string, object> ReadObject(string s, ref int i)
        {
            var obj = new Dictionary<string, object>();
            i++; // {
            SkipSpace(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return obj; }
            while (true)
            {
                SkipSpace(s, ref i);
                if (i >= s.Length || s[i] != '"') throw Error(i);
                var key = ReadString(s, ref i);
                SkipSpace(s, ref i);
                if (i >= s.Length || s[i] != ':') throw Error(i);
                i++;
                obj[key] = ReadValue(s, ref i);
                SkipSpace(s, ref i);
                if (i >= s.Length) throw Error(i);
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return obj; }
                throw Error(i);
            }
        }

        static List<object> ReadArray(string s, ref int i)
        {
            var list = new List<object>();
            i++; // [
            SkipSpace(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return list; }
            while (true)
            {
                list.Add(ReadValue(s, ref i));
                SkipSpace(s, ref i);
                if (i >= s.Length) throw Error(i);
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return list; }
                throw Error(i);
            }
        }

        static string ReadString(string s, ref int i)
        {
            i++; // opening quote
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw Error(i);
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw Error(i - 1);
                }
            }
            throw Error(i);
        }

        static double ReadNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (i == start || !double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                throw Error(start);
            return d;
        }

        static void Expect(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) throw Error(i);
            i += word.Length;
        }

        static void SkipSpace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        static FormatException Error(int at) => new FormatException("Invalid JSON at position " + at);

        // ---- writing (indented, so the settings file stays readable)

        static void WriteValue(StringBuilder sb, object v, int depth)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case string str: WriteString(sb, str); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case IDictionary<string, object> dict:
                    if (dict.Count == 0) { sb.Append("{}"); break; }
                    sb.Append('{');
                    bool first = true;
                    foreach (var kv in dict)
                    {
                        sb.Append(first ? "\n" : ",\n").Append(' ', (depth + 1) * 2);
                        first = false;
                        WriteString(sb, kv.Key);
                        sb.Append(": ");
                        WriteValue(sb, kv.Value, depth + 1);
                    }
                    sb.Append('\n').Append(' ', depth * 2).Append('}');
                    break;
                case IEnumerable list:
                    var items = new List<object>();
                    foreach (var item in list) items.Add(item);
                    if (items.Count == 0) { sb.Append("[]"); break; }
                    sb.Append('[');
                    for (int k = 0; k < items.Count; k++)
                    {
                        sb.Append(k == 0 ? "\n" : ",\n").Append(' ', (depth + 1) * 2);
                        WriteValue(sb, items[k], depth + 1);
                    }
                    sb.Append('\n').Append(' ', depth * 2).Append(']');
                    break;
                case IFormattable num: sb.Append(num.ToString(null, CultureInfo.InvariantCulture)); break;
                default: WriteString(sb, v.ToString()); break;
            }
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // ---- helpers for reading parsed values

        public static string Str(this Dictionary<string, object> o, string key) =>
            o != null && o.TryGetValue(key, out var v) ? v as string : null;

        public static bool Bool(this Dictionary<string, object> o, string key, bool fallback) =>
            o != null && o.TryGetValue(key, out var v) && v is bool b ? b : fallback;

        public static Dictionary<string, object> Obj(this Dictionary<string, object> o, string key) =>
            o != null && o.TryGetValue(key, out var v) ? v as Dictionary<string, object> : null;

        public static List<object> Arr(this Dictionary<string, object> o, string key) =>
            o != null && o.TryGetValue(key, out var v) ? v as List<object> : null;
    }
}
