// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Reflection;
using Twin.Logging;
using Twin.SharpClaws.API;

namespace Twin.SharpClaws
{
    internal class LongestFirstStringComparer : Comparer<String> {
        public override int Compare(string x, string y) {
            return y.Length - x.Length;
        }
    }

    class ServerLogger : Logger {
        Server server;
        public ServerLogger(Server server) {
            this.server = server;
        }
        public override void Log(LogLevel level, Exception e) {
            server.Log.Log(level, e);
        }
        public override void Log(LogLevel level, string message, params object[] details) {
            server.Log.Log(level, message, details);
        }
    }

    class Server
    {
        private const int DEFAULT_PORT = 8080;

        Servlet defaultServlet = new DefaultServlet();

        SortedDictionary<string, Servlet> servlets;
        public Servlet this[string context] {
            get {
                context = "/" + context.Trim('/');
                if(!servlets.ContainsKey(context))
                    return null;
                return servlets[context];
            }
            set {
                context = "/" + context.Trim('/');
                Servlet oldValue = servlets.ContainsKey(context) ? servlets[context] : null;
                value.Log = new ServerLogger(this);
                value.ContextPath = context;
                value.IPEndPoint = this.endpoint;
                value.Initialize();
                servlets[context] = value;
                if(oldValue != null)
                    oldValue.Destroy();
            }
        }

        private bool started = false;
        private IPEndPoint endpoint;

        public Server() : this(DEFAULT_PORT) { }
        public Server(int port) : this(IPAddress.Any, port) { }
        public Server(IPAddress address, int port) {
            // sort by longest context path first
            // this means the most specific mapping will be preferred
            servlets = new SortedDictionary<string, Servlet>(new LongestFirstStringComparer());

            this.endpoint = new IPEndPoint(address, port);

            AssemblyName name = Assembly.GetExecutingAssembly().GetName();
            this.ServerHeader = string.Format("{0} {1}", name.Name, name.Version);
            Log = new NullLogger();
        }

        Logger log;
        public Logger Log {
            get { return log; }
            set { log = value; }
        }

        private TcpListener listener;
        public void Start() {
            if (started)
                throw new ThreadStateException();
            started = true;

            listener = new TcpListener(endpoint);
            listener.Start();

            Log.Info("Listening on {0}", endpoint);

            IAsyncResult result = listener.BeginAcceptTcpClient(new AsyncCallback(HandleClient), null);
        }
        private void HandleClient(IAsyncResult result) {
            if (listener.Server.IsBound) {
                TcpClient client;
                try {
                    client = listener.EndAcceptTcpClient(result);
                } catch (ObjectDisposedException) { // listener was closed between IsBound and EndAcceptTcpClient
                    Finish();
                    return;
                }
                ConnectionHandler handler = new ConnectionHandler(this, client);
                handler.Start();
                listener.BeginAcceptTcpClient(new AsyncCallback(HandleClient), null);
            } else {
                Finish();
            }
        }
        EventWaitHandle shutdown = new ManualResetEvent(false);
        public void Run() {
            Start();
            shutdown.WaitOne();
        }
        private void Finish() {
            Log.Info("Server shutting down");
            foreach (Servlet servlet in servlets.Values) {
                servlet.Destroy();
            }
            shutdown.Set();
        }
        public void Stop()
        {
            Log.Info("Signaling server shut down");
            listener.Stop();
        }

        public void MapServlet(Request request)
        {
            foreach (KeyValuePair<string, Servlet> mapping in servlets) {
                string relativePath = RelativePath(request.Path, mapping.Key); 
                if(relativePath != null) {
                    request.ContextPath = mapping.Key;
                    request.RelativePath = relativePath;
                    request.Servlet = mapping.Value;
                    return;
                }
            }
            request.ContextPath = "/";
            request.RelativePath = request.Path;
            request.Servlet = defaultServlet;
        }
        public static string RelativePath(string path, string context) {
            if(path.TrimEnd('/').Equals(context.TrimEnd('/')))
                return "/";
            if(path.StartsWith(context)) {
                path = path.Substring(context.Length);
                if(!path.StartsWith("/"))
                    path = "/" + path;
                return path;
            }
            return null;
        }

        string serverHeader;
        public string ServerHeader {
            get { return serverHeader; }
            set { serverHeader = value; }
        }
    }
}
