// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Twin.SharpClaws.API {
	/// <summary>
	/// A set of headers in an HTTP message.
	/// This is a little bit like a Dictionary but the order is potentially important 
	/// and repeated keys are allowed so it's a List of string -> string mappings.
	/// </summary>
    public class Headers : List<KeyValuePair<string, string>> {
        public string this[string name] {
            get {
                return First(name);
            }
            set {
                Set(name, value);
            }
        }
		/// <summary>
		/// Get the value of a header
		/// </summary>
		/// <param name="s">The header name</param>
		/// <returns>The first value defined for the header s, or null</returns>
        public string First(string s) {
            string[] all = All(s);
            if (all.Length == 0)
                return null;
            return all[0];
        }
		/// <summary>
		/// Get all headers with a given name
		/// </summary>
		/// <param name="s">The header name</param>
		/// <returns>All headers defined for the header s (may be empty)</returns>
        public string[] All(string s) {
            List<string> list = new List<string>();
            foreach (KeyValuePair<string, string> kv in this)
                if (kv.Key == s)
                    list.Add(kv.Value);
            return list.ToArray();
        }
		/// <summary>
		/// Set the value of a header, replacing existing entries.
		/// </summary>
		/// <param name="s">The header name</param>
		/// <param name="h">The header value</param>
        public void Set(string s, string h) {
            Remove(s);
            Add(s, h);
        }
		/// <summary>
		/// Add a value for a header, retaining existing entries.
		/// </summary>
		/// <param name="s">The header name</param>
		/// <param name="h">The header value</param>
        public void Add(string s, string h) {
            Add(new KeyValuePair<string, string>(s, h));
        }
		/// <summary>
		/// Remove all values of a header.
		/// </summary>
		/// <param name="s">The header name</param>
        public void Remove(string s) {
            for (int i = 0; i < Count; i++)
                if (s == this[i].Key)
                    this.RemoveAt(i--);
        }
		/// <summary>
		/// Remove all occurrences of a header with a particular value.
		/// </summary>
		/// <param name="s">The header name</param>
		/// <param name="h">The header value</param>
        public void Remove(string s, string h) {
            Remove(new KeyValuePair<string, string>(s, h));
        }
    }
}
