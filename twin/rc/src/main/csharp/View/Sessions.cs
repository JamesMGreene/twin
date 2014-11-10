// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using Twin.Model;
using Twin.Generic;
using System.Reflection;

namespace Twin.View {
    class Sessions {
        public static SessionFactory GetFactory(JSONRequest request) {
            return ((TwinRC)request.Servlet).SessionFactory;
        }
        public static Session GetSession(JSONRequest request) {
            return GetFactory(request)[request.Parameters["session"]];
        }

        private static Dictionary<string, string> flatten(Dictionary<string, object> o) {
            Dictionary<string, string> results = new Dictionary<string, string>();
            foreach (String k in o.Keys)
                results[k] = o[k] == null ? null : (o[k] is bool) ? o[k].ToString().ToLowerInvariant() : o[k].ToString();
            return results;
        }
        private static Dictionary<string, object> unflatten(Dictionary<string, string> o) {
            Dictionary<string, object> results = new Dictionary<string, object>();
            foreach (String k in o.Keys)
                results[k] = o[k];
            return results;
        }

        public static JSONResponse Status(JSONRequest request) {
            JSONResponse response = new JSONResponse();
            response.Body = new Dictionary<string, object>();
            response.Body["running"] = true;

            Dictionary<string,object> server = new Dictionary<string,object>();
            server["name"] = "Twin";
            server["version"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            response.Body["server"] = server;

            SessionFactory factory = GetFactory(request);
            List<Dictionary<string, object>> configurations = new List<Dictionary<string, object>>();
            foreach(Configuration config in factory.Configurations) {
                Dictionary<string, object> serial = new Dictionary<string, object>();
                serial["id"] = config.identifier;
                serial["capabilities"] = config.Capabilities;
                serial["path"] = config.appPath;
                if (config.arguments != null)
                    serial["arguments"] = config.arguments;
                configurations.Add(serial);
            }
            response.Body["configurations"] = configurations;

            List<Dictionary<string, object>> sessions = new List<Dictionary<string, object>>();
            foreach (KeyValuePair<Guid, Session> session in factory.GetSessions()) {
                Dictionary<string, object> serial = new Dictionary<string, object>();
                serial["id"] = session.Key.ToString();
                serial["configuration"] = session.Value.Configuration.identifier;
                serial["processId"] = session.Value.Process.Id;
                sessions.Add(serial);
            }
            response.Body["sessions"] = sessions;

            return response;
        }

        public static JSONResponse Create(JSONRequest request) {
            if(!request.Body.ContainsKey("desiredCapabilities"))
                throw new ArgumentException("desiredCapabilities not provided");
            Dictionary<string, object> sessionSetupInfo = request.Body.ContainsKey("sessionSetup") ? (Dictionary<string, object>)request.Body["sessionSetup"] : null;
            Session session = GetFactory(request).Create(flatten((Dictionary<string,object>)request.Body["desiredCapabilities"]), sessionSetupInfo);
            JSONResponse response = new JSONResponse();
            response.StatusCode = 303;
            response.Location = "/session/" + session.ToString();
            return response;
        }

        public static JSONResponse GetCapabilities(JSONRequest request) {
            Session session = GetSession(request);
            return CreateResponse(session, ResponseStatus.Success, session);
        }

        public static JSONResponse Delete(JSONRequest request) {
            GetFactory(request).Delete(GetSession(request));
            return CreateResponse(null, ResponseStatus.Success, null);
        }

        public static JSONResponse CreateResponse(Session session, ResponseStatus status, object value) {
            JSONResponse response = new JSONResponse();
            response.Body = new Dictionary<string, object>();
            response.Body["sessionId"] = session == null ? null : session.ToString();
            response.Body["status"] = (int)status;
            response.Body["value"] = value;
            return response;
        }
    }
}
