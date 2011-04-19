// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using System.Web;
using Twin.Logging;
using Twin.SharpClaws.API;

namespace Twin.SharpClaws {
    internal class Request : IRequest {
        public Request(Server server, Stream stream) {
            response = new Response(server, this, stream);
        }

        Logger log;
        public Logger Log {
            get { return log; }
            internal set { log = value; }
        }
        int port;
        public int Port {
            get { return port; }
            internal set { port = value; }
        }
        IPEndPoint localEndpoint;
        public IPEndPoint LocalAddress {
            get { return localEndpoint; }
            internal set { localEndpoint = value; }
        }
        IPEndPoint remoteEndpoint;
        public IPEndPoint RemoteAddress {
            get { return remoteEndpoint; }
            internal set { remoteEndpoint = value; }
        }
        string protocol;
        public string Protocol {
            get { return protocol; }
            internal set { protocol = value; }
        }
        string host;
        public string Host {
            get { return host; }
            internal set { host = value; }
        }
        string query;
        public string Query {
            get { return query; }
            internal set { query = value; }
        }
        Servlet servlet;
        public Servlet Servlet {
            get { return servlet; }
            internal set { servlet = value; }
        }
        string contextPath;
        public string ContextPath {
            get { return contextPath; }
            internal set { contextPath = value; }
        }
        string relativePath;
        public string RelativePath {
            get { return relativePath; }
            internal set { relativePath = value; }
        }
        string path;
        public string Path {
            get { return path; }
            internal set { path = value; }
        }
        string method;
        public string Method {
            get { return method; }
            internal set { method = value; }
        }
        Headers headers = new Headers();
        public Headers Headers {
            get { return headers; }
        }
        private Encoding encoding;
        public Encoding Encoding {
            get {
                if(encoding != null)
                    return encoding;

                string encodingName = "iso-8859-1";
                string contentType = Headers["Content-Type"];
                if (contentType != null) {
                    foreach(string component in contentType.Split(';')) {
                        string chunk = component.ToLower().Trim();
                        if (chunk.StartsWith("charset")) {
                            chunk = chunk.Substring("charset".Length).TrimStart();
                            if(chunk.StartsWith("=")) {
                                chunk = chunk.Substring("=".Length).TrimStart();
                                encodingName = chunk;
                            }
                        }
                    }
                }
                return encoding = Encoding.GetEncoding(encodingName);
            }
        }
        private string contentType;
        public string ContentType {
            get {
                if (contentType != null)
                    return contentType;

                string ct = Headers["Content-Type"];
                if (ct == null)
                    return null;

                if (ct.Contains(";"))
                    ct = ct.Substring(0, ct.IndexOf(";")).Trim();

                return contentType = ct;
            }
        }

        internal Response response;
        public IResponse Response {
            get {
                return response;
            }
        }

        Stream bodyStream;
        public Stream Body {
            get { return bodyStream; }
            set { bodyStream = value; }
        }


        public NameValueCollection Parameters {
            get {
                return HttpUtility.ParseQueryString(Query, Encoding.UTF8);
            }
        }

        public TextReader OpenBodyReader() {
            return new StreamReader(Body, Encoding);
        }

        public NameValueCollection ReadParameters() {
            if (ContentType == "application/x-www-form-urlencoded") {
                string body = ReadText(Encoding.ASCII);
                if(body == null)
                	return new NameValueCollection();
                return HttpUtility.ParseQueryString(body, Encoding);
            }
            if (ContentType == "multipart/form-data") {
                throw new Exception("multipart/form-data parsing is not implemented.");
            }
            Log.Trace("Called ReadPostParameters() but content-type is {0}, returning null", ContentType);
            return null;
        }

        public byte[] ReadBytes() {
        	if(Body == null)
        		return null;
            int lengthHint = 0;
            if(Headers["Content-Length"] != null)
                lengthHint = Convert.ToInt32(Headers["Content-Length"]);
            MemoryStream memory = new MemoryStream(lengthHint);
            copy(Body, memory);
            Body.Close();
            return memory.ToArray();
        }

        public TextReader OpenReader() {
            return OpenReader(Encoding);
        }
        private TextReader OpenReader(Encoding encoding) {
            StreamReader reader = new StreamReader(Body, encoding);
            return reader;
        }

        private string ReadText(Encoding encoding) {
        	if(Body == null)
        		return null;
            using (TextReader reader = OpenReader(encoding)) {
                return reader.ReadToEnd();
            }
        }
        public string ReadText() {
            return ReadText(Encoding);
        }

        private void copy(Stream from, Stream to) {
            byte[] buf = new byte[1024];
            int count;
            while ((count = from.Read(buf, 0, buf.Length)) > 0)
                to.Write(buf, 0, count);
        }
    }
}
