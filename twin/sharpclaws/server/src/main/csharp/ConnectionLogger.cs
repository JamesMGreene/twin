// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Net;
using Twin.SharpClaws.API;
using Twin.Logging;

namespace Twin.SharpClaws {
    class ConnectionLogger : Logger {
        Logger parent;

        static int nextId = 0;
        [MethodImpl(MethodImplOptions.Synchronized)]
        private static int NextId() {
            return nextId++;
        }

        EndPoint remote;
        Request request;
        int id;

        public ConnectionLogger(Logger parent, EndPoint remote) {
            this.parent = parent;
            this.remote = remote;
            id = NextId();
        }
        public Request Request {
            get { return request; }
            set { request = value; }
        }

        public override void Log(LogLevel level, Exception e) {
            parent.Log(level, e);
        }
        public override void Log(LogLevel level, string message, params object[] details) {
            string identifier = null;
            if(request == null) {
                identifier = remote.ToString();
            } else {
                identifier = remote.ToString() + " " + request.Method + " " + request.Path;
            }
            string messageStr = string.Format("({0}) {1}", identifier, string.Format(message,details));
            parent.Log(level, "{0}", messageStr);
        }
    }
}
