using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Waffle.Homecoming.Profile
{
    // Strict subset used by the profile schema: objects, arrays, strings and integer numbers.
    // Reject incomplete documents and duplicate keys instead of accepting a half-written save.
    internal sealed class ProfileJson
    {
        readonly string text;
        int cursor;
        ProfileJson(string text) { this.text = text; }
        public static Dictionary<string, object> Read(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > ProfileConfig.MaxFileSizeBytes) throw new FormatException("Invalid profile size");
            var reader = new ProfileJson(text);
            var value = reader.Value(0) as Dictionary<string, object>;
            reader.White();
            if (value == null || reader.cursor != text.Length) throw new FormatException("Invalid profile document");
            return value;
        }
        void White() { while (cursor < text.Length && char.IsWhiteSpace(text[cursor])) cursor++; }
        bool Take(char c) { White(); if (cursor < text.Length && text[cursor] == c) { cursor++; return true; } return false; }
        void Require(char c) { if (!Take(c)) throw new FormatException("Expected " + c); }
        object Value(int depth)
        {
            if (depth > 8) throw new FormatException("Profile too deeply nested");
            White(); if (cursor >= text.Length) throw new FormatException("Incomplete profile");
            if (text[cursor] == '"') return String();
            if (Take('{'))
            {
                var map = new Dictionary<string, object>(StringComparer.Ordinal);
                if (Take('}')) return map;
                do { White(); string key = String(); Require(':'); if (map.ContainsKey(key)) throw new FormatException("Duplicate property"); map.Add(key, Value(depth + 1)); }
                while (Take(','));
                Require('}'); return map;
            }
            if (Take('['))
            {
                var list = new List<object>();
                if (Take(']')) return list;
                do { list.Add(Value(depth + 1)); } while (Take(','));
                Require(']'); return list;
            }
            int start = cursor;
            if (text[cursor] == '-') cursor++;
            int digits = cursor;
            while (cursor < text.Length && text[cursor] >= '0' && text[cursor] <= '9') cursor++;
            if (cursor == digits || (cursor - digits > 1 && text[digits] == '0')) throw new FormatException("Invalid integer");
            return long.Parse(text.Substring(start, cursor - start), CultureInfo.InvariantCulture);
        }
        string String()
        {
            if (cursor >= text.Length || text[cursor++] != '"') throw new FormatException("Expected string");
            var value = new StringBuilder();
            while (cursor < text.Length)
            {
                char c = text[cursor++];
                if (c == '"') return value.ToString();
                if (c < 32) throw new FormatException("Control character in string");
                if (c == '\\')
                {
                    if (cursor >= text.Length) throw new FormatException("Incomplete escape");
                    char escaped = text[cursor++];
                    switch (escaped)
                    {
                        case '"': c = '"'; break; case '\\': c = '\\'; break; case '/': c = '/'; break;
                        case 'n': c = '\n'; break; case 'r': c = '\r'; break; case 't': c = '\t'; break;
                        case 'b': c = '\b'; break; case 'f': c = '\f'; break;
                        case 'u': if (cursor + 4 > text.Length) throw new FormatException("Incomplete unicode");
                            c = (char)ushort.Parse(text.Substring(cursor, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture); cursor += 4; break;
                        default: throw new FormatException("Invalid escape");
                    }
                }
                value.Append(c);
            }
            throw new FormatException("Unterminated string");
        }
    }
}
