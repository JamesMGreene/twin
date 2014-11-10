// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using Twin.Logging;
using System.Net;
using Twin.Model;
using Twin.Generic;
using System.IO;

namespace Twin.Grid {
    class GridHub {
        internal TwinRC RC;
        internal Logger Log;
        internal Uri Uri;
        internal Dictionary<string, string> Configuration;

        public GridHub(TwinRC rc, Uri uri, Dictionary<string,string> Configuration, Logger log) {
            this.RC = rc;
            this.Uri = uri;
            this.Log = log;
            this.Configuration = Configuration;
        }

        private bool connected;
        public bool Connected {
            get {
                return connected;
            }
        }

 
        private void Request(String method, Uri uri, object body) {
            Log.Trace("{0}ing to {1}", method, uri);
            WebRequest request = WebRequest.Create(Uri);
            request.Method = method;

            if(body != null) {
                request.ContentType = "application/json; charset=UTF-8";
                string text = JSON.ToString(GetDescriptor(), 4);
                Log.Trace("Data: {0}", text);
                byte[] data = Encoding.UTF8.GetBytes(text);
                request.ContentLength = data.Length;
                Stream outstream = request.GetRequestStream();
                try {
                    outstream.Write(data, 0, data.Length);
                } finally {
                    outstream.Close();
                }
            }

            WebResponse response = request.GetResponse();
            if (response == null)
                return;
            StreamReader reader = new StreamReader(response.GetResponseStream());
            reader.ReadToEnd();
        }

        private Uri Join(Uri uri, String component) {
            if (uri.AbsolutePath.EndsWith("/"))
                return new Uri(uri, component);
            return new Uri(uri, uri.AbsolutePath + "/" + component);
        }

        public void Connect() {
            Log.Info("Registering with grid hub {0}", Uri);
            try {
                Request("POST", Join(Uri, "register"), GetDescriptor());
            } catch (WebException ex) {
                Log.Error("Failed to register with hub {0}", Uri);
                Log.Error(ex);
                return;
            }

            connected = true;
        }

        public void Disconnect() {
            if (!Connected)
                return;
            Log.Info("Unregistering with grid hub {0}", Uri);
            Log.Error("WARNING: Unregistering not implemented");
            /*
            try {
                Request("POST", Join(Uri, "unregister"), GetDescriptor());
            } catch (WebException ex) {
                Log.Error("Failed to unregister from hub {0}", Uri);
                Log.Error(ex);
                return;
            }
             */

            connected = false;
        }

        private Dictionary<string, object> GetDescriptor() {
            Dictionary<string, object> descriptor = new Dictionary<string, object>();

            List<Dictionary<string, string>> configurations = new List<Dictionary<string, string>>();
            foreach (Configuration config in RC.SessionFactory.Configurations)
                configurations.Add(config.Capabilities);
            descriptor["capabilities"] = configurations;

            Dictionary<string, object> configuration = new Dictionary<string, object>();
            configuration["url"] = RC.ExternalUri.ToString();
            foreach (string key in this.Configuration.Keys)
                configuration[key] = this.Configuration[key];
            descriptor["configuration"] = configuration;

            return descriptor;
        }
    }
}
