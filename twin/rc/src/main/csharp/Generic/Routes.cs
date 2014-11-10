// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Twin.SharpClaws.API;
using Twin.Logging;

namespace Twin.Generic {
    class Routes {
        internal Logger Log = new NullLogger();

        private Dictionary<string, Mapping> mappings = new Dictionary<string,Mapping>();

        public Mapping this[string pattern] {
            get {
                if (!mappings.ContainsKey(pattern)) {
                    Mapping mapping = new Mapping(Log, pattern);
                    mappings[pattern] = mapping;
                }
                return mappings[pattern];
             }
        }

        public Dictionary<string, Action<IRequest>> Match(string path) {
            foreach (Mapping mapping in mappings.Values) {
                Dictionary<string, string> parameters = mapping.Match(path);
                if (parameters != null) {
                    Log.Trace("Matched {0} to {1} with parameters {2}", path, mapping, JSON.ToString(parameters));
                    Dictionary<string, Action<IRequest>> map = new Dictionary<string, Action<IRequest>>();
                    foreach(KeyValuePair<string,Responder> kv in mapping.handlers)
                        map[kv.Key] = new Action<IRequest>(new ResponderInvoker(kv.Value, parameters).Action);
                    return map;
                }
            }
            return null;
        }
    }

	class ResponderInvoker {
		private Responder handler;
        private Dictionary<string,string> parameters;
		
        public ResponderInvoker(Responder handler, Dictionary<string,string> parameters) {
            this.handler = handler;
            this.parameters = parameters;
        }
        public void Action(IRequest request) {
        	handler.Respond(new ParsedRequest(request, parameters));
        }
	}
		
	/*
    class HandlerInvoker {
        private Handler handler;
        private Dictionary<string,string> parameters;
        public HandlerInvoker(Handler handler, Dictionary<string,string> parameters) {
            this.handler = handler;
            this.parameters = parameters;
        }
        public JSONResponse Action(JSONRequest request) {
            request.Parameters = parameters;
            return handler(request);
        }
    }
    */

    class Mapping {
        Logger Log;

        internal Mapping(Logger log, string pattern) {
            Log = log;
            pattern = pattern.Trim('/');
            string[] components = pattern.Split('/');

            captureVariableNames = new List<string>();
            StringBuilder regex = new StringBuilder("^");
            for(int i=0; i<components.Length; i++) {
                if (components[i].StartsWith(":")) { // variable
                    string variable = components[i].Substring(1);
                    captureVariableNames.Add(variable);
                    regex.Append("/([^/]+)");
                } else { // literal
                    regex.Append("/").Append(Regex.Escape(components[i]));
                }
            }
            regex.Append("$");
            matcher = new Regex(regex.ToString());

            Log.Trace("Created Mapping: regex={0} captureNames={1}", matcher, captureVariableNames);
        }

        Regex matcher;
        List<string> captureVariableNames;

        internal Dictionary<string, Responder> handlers = new Dictionary<string,Responder>();
        public Dictionary<string, string> Match(string url) {
            url = "/" + url.Trim('/');
            Match match = matcher.Match(url);
            if (!match.Success)
                return null;
            Log.Trace("Match success on {0} for {1}, got {2} capture groups", url, matcher, match.Groups.Count);
            Dictionary<string, string> result = new Dictionary<string, string>();
            for (int i = 0; i < match.Groups.Count - 1; i++) { // Groups[0] is always the whole match
                result[captureVariableNames[i]] = match.Groups[i + 1].Value;
            }
            return result;
        }
        public Responder this[string method] {
            get {
                if(handlers.ContainsKey(method.ToUpperInvariant()))
                    return handlers[method.ToUpper()];
                return null;
            }
            set {
                handlers[method.ToUpperInvariant()] = value;
            }
        }
    }

   /*
    delegate JSONResponse Handler(JSONRequest request);
    */
//   delegate void Handler(ParsedRequest request);
}
