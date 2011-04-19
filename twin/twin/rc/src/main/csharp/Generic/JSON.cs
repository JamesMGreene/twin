// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Twin.Generic;

namespace Twin.Generic {
    class JSON {
        public static string quote(string text) {
            StringBuilder result = new StringBuilder();
            result.Append('"');
            foreach (char c in text) {
                if (c < 32 || c >= 128)
                    result.AppendFormat("\\u{0:x4}", (int)c);
                else if (c == '\\')
                    result.Append(@"\\");
                else if (c == '"')
                    result.Append(@"\""");
                else
                    result.Append(c);
            }
            result.Append('"');
            return result.ToString();
        }
        public static void Write(object p, TextWriter writer) {
            Write(p, writer, -1, 0);
        }
        public static void Write(object p, TextWriter writer, int indent) {
            Write(p, writer, 0, indent);
        }
        private static void Indent(int count, TextWriter writer) {
            for (int i = 0; i < count; i++)
                writer.Write(' ');
        }
        private static void Write(object p, TextWriter writer, int indent, int indentIncrement) {
            if (p == null) {
                writer.Write("null");
            } else if (p is IJSONable) {
                Write(((IJSONable)p).JSONForm, writer, indent, indentIncrement);
            } else if (p is Byte || p is SByte || p is Int16 || p is UInt16 || p is Int32 || p is UInt32 || p is Int64 || p is UInt64) {
                writer.Write(p.ToString());
            } else if (p is Single || p is Double || p is Decimal) {
                writer.Write(p.ToString());
            } else if (p is Boolean) {
                writer.Write((Boolean)p ? "true" : "false");
            } else if (p is String) {
                writer.Write(quote((String)p));
            } else if (p is System.Collections.IDictionary) {
                bool first = true;
                writer.Write('{');
                System.Collections.IDictionary dict = (System.Collections.IDictionary)p;
                if (dict.Count > 0) {
                    if (indent >= 0)
                        writer.Write('\n');
                    foreach (System.Collections.DictionaryEntry kv in dict) {
                        if (!first) {
                            writer.Write(',');
                            if (indent >= 0)
                                writer.Write("\n");
                        }
                        first = false;

                        Indent(indent + indentIncrement, writer);
                        Write(kv.Key.ToString(), writer, indent + indentIncrement, indentIncrement);
                        writer.Write(':');
                        if (indent >= 0)
                            writer.Write(' ');
                        Write(kv.Value, writer, indent + indentIncrement, indentIncrement);
                    }
                    if (indent >= 0)
                        writer.Write("\n");
                    Indent(indent, writer);
                }
                writer.Write('}');
            } else if (p is System.Collections.IEnumerable) {
                bool first = true;
                writer.Write('[');
                foreach (object o in (System.Collections.IEnumerable)p) {
                    if (!first)
                        writer.Write(',');
                    if (indent >= 0)
                        writer.Write('\n');
                    first = false;

                    Indent(indent + indentIncrement, writer);
                    Write(o, writer, indent + indentIncrement, indentIncrement);
                }
                if (!first) {
                    if (indent >= 0)
                        writer.Write('\n');
                    Indent(indent, writer);
                }
                writer.Write(']');
            } else {
                throw new ArgumentException("Cannot serialise " + p + " of type " + p.GetType());
            }
        }

        private static void SkipSpace(TextReader reader) {
            while(true) {
                int c = reader.Peek();
                if(c < 0 || !Char.IsWhiteSpace((char)c))
                    return;
                reader.Read();
            }
        }
        public static object Read(TextReader reader) {
            SkipSpace(reader);
            int c = reader.Read();
            if (c < 0)
                throw new FormatException("Unexpected EOF");
            if (c == '{') {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                while (true) {
                    SkipSpace(reader);
                    int d = reader.Peek();
                    if (d == '"') {
                        string key = (string)Read(reader);
                        SkipSpace(reader);
                        int e = reader.Read();
                        if (e == ':') {
                            object value = Read(reader);
                            dict[key] = value;
                            SkipSpace(reader);
                            int f = reader.Read();
                            if(f == '}')
                                break;
                            else if(f == ',')
                                continue;
                            else
                                throw new FormatException("Expected } or , after key-value pair in object but got "+(char)f);
                        } else {
                            throw new FormatException("Expected : after key " + key+" but got "+(char)e);
                        }
                    } else if (d == '}' && dict.Count == 0) {
                        reader.Read();
                        break;
                    } else {
                        throw new FormatException("Expected \" to start an object but got "+(char)d);
                    }
                }
                return dict;
            }
            if (c == '[') {
                List<object> list = new List<object>();
                while (true) {
                    SkipSpace(reader);
                    int d = reader.Peek();

                    if (d == ']' && list.Count == 0) {
                        reader.Read();
                        break;
                    } else {
                        list.Add(Read(reader));
                        SkipSpace(reader);
                        int e = reader.Read();
                        if (e == ']')
                            break;
                        else if (e == ',')
                            continue;
                        else
                            throw new FormatException("Expected ] or , after array entry but got " + (char)e);
                    }
                }
                return list;
            }
            if (c == 'n') {
                int i = reader.Read();
                int j = reader.Read();
                int k = reader.Read();
                if (i == 'u' && j == 'l' && k == 'l')
                    return null;
                throw new FormatException("Expected 'ull' after n but got " + (char)i + " " + (char)j + " " + (char)k);
            }
            if (c == 't') {
                int i = reader.Read();
                int j = reader.Read();
                int k = reader.Read();
                if (i == 'r' && j == 'u' && k == 'e')
                    return true;
                throw new FormatException("Expected 'rue' after t but got " + (char)i + " " + (char)j + " " + (char)k);
            }
            if (c == 'f') {
                int i = reader.Read();
                int j = reader.Read();
                int k = reader.Read();
                int m = reader.Read();
                if (i == 'a' && j == 'l' && k == 's' && m=='e')
                    return false;
                throw new FormatException("Expected 'alse' after f but got " + (char)i + " " + (char)j + " " + (char)k + " " + (char)m);
            }
            if (c == '"') {
                StringBuilder text = new StringBuilder();
                while (true) {
                    int next = reader.Read();
                    if (next < 0)
                        throw new FormatException("Runaway string meets EOF");
                    if (next == '"')
                        return text.ToString();
                    if (next == '\\') {
                        int escape = reader.Read();
                        switch (escape) {
                            case -1:
                                throw new FormatException("Escaped EOF in string");
                            case '\"':
                            case '\\':
                            case '/':
                                text.Append((char)escape);
                                break;
                            case 'b':
                                text.Append('\b');
                                break;
                            case 'f':
                                text.Append('\f');
                                break;
                            case 'n':
                                text.Append('\n');
                                break;
                            case 'r':
                                text.Append('\r');
                                break;
                            case 't':
                                text.Append('\t');
                                break;
                            case 'u':
                                int hex1 = reader.Read();
                                int hex2 = reader.Read();
                                int hex3 = reader.Read();
                                int hex4 = reader.Read();
                                if(hex4 < 0)
                                    throw new FormatException("Unicode escape meets EOF");
                                text.Append((char)Convert.ToInt32("" + (char)hex1 + (char)hex2 + (char)hex3 + (char)hex4, 16));
                                break;
                            default:
                                throw new FormatException("Unknown string escape "+(char)escape);
                        }
                    } else {
                        if(next < 32)
                            throw new FormatException("Control character included inline in string: "+next);
                        text.Append((char)next);
                    }
                }
            }
            if(Char.IsDigit((char)c) || c=='-') {
                int multiplier = (c=='-') ? -1 : 1;
                UInt64 integer = (c == '-') ? 0 : (ulong)(c - '0');
                while(true) {
                    int d = reader.Peek();
                    if(d >= 0 && Char.IsDigit((char)d)) {
                        reader.Read();
                        integer *= 10;
                        integer += (ulong)(d - '0');
                        continue;
                    }
                    break;
                }
                int decimalDigits = 0;
                UInt64 decimalData = 0;
                if(reader.Peek() == '.') {
                    reader.Read();
                    while(true) {  
                        int d = reader.Peek();
                        if (d >= 0 && Char.IsDigit((char)d)) {
                            reader.Read();
                            decimalData *= 10;
                            decimalData += (ulong)(d - '0');
                            decimalDigits++;
                            continue;
                        }
                        break;
                    }
                }
                int exponentMultiplier = 1;
                UInt64 exponentData = 0;
                if(reader.Peek() == 'e' || reader.Peek() == 'E') {
                    reader.Read();
                    int sign = reader.Peek();
                    if(sign == '+')
                        reader.Read();
                    if(sign == '-') {
                        reader.Read();
                        exponentMultiplier = -1;
                    }
                    while(true) {
                        int d = reader.Peek();
                        if (d >= 0 && char.IsDigit((char)d)) {
                            reader.Read();
                            exponentData *= 10;
                            exponentData += (ulong)(d - '0');
                            continue;
                        }
                        break;
                    }
                }
                if(exponentData==0 && decimalDigits ==0 && integer <= (multiplier > 0 ? (ulong)int.MaxValue : (ulong)(- (long)int.MinValue))) {
                    return multiplier * (int)integer; 
                }
                double value = (double)multiplier * (double)integer;
                if(decimalDigits != 0)
                    value += decimalData / Math.Pow(10, decimalDigits);
                if(exponentData != 0)
                    value *= Math.Pow(10, (double)exponentMultiplier * (double)exponentData);
                return value;
            }
            throw new FormatException("Unknown JSON type starting with "+(char)c);
        }

        public static string ToString(object o) {
            using (StringWriter writer = new StringWriter()) {
                Write(o, writer);
                return writer.ToString();
            }
        }

        public static string ToString(object o, int indent) {
            using (StringWriter writer = new StringWriter()) {
                Write(o, writer, indent);
                return writer.ToString();
            }
        }

        public static object ToObject(string s) {
			if(s == null)
				return null;
            using (TextReader reader = new StringReader(s)) {
                return Read(reader);
            }
        }
    }
}
