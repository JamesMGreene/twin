// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;

using Twin.SharpClaws.API;

namespace Twin.SharpClaws {
    public class Response : IResponse {
        private Stream stream;
        private Server server;
        internal Response(Server server, Request request, Stream stream) {
            this.server = server;
            this.request = request;
            this.stream = stream;
            bodyStream = new StreamAsNeeded(this);

            headers = DefaultHeaders();
        }

        internal Stream OpenBody() { // called on first write
            try {
                if (headers["Transfer-Encoding"] != null && "chunked".Equals(headers["Transfer-Encoding"].Trim(), StringComparison.InvariantCultureIgnoreCase)) {
					return new ChunkedStream(stream, false, true);
        		}
                if (headers["Content-Length"] != null) {
                    return new LengthBoundStream(stream, 0, Convert.ToInt64(headers["Content-Length"]));
                }
                headers["Transfer-Encoding"] = "chunked";
				return new ChunkedStream(stream, false, true);
            } finally {
                HeadersFinalized = true;
            }
        }

        bool headersFinalized;
        internal bool HeadersFinalized {
            get { return headersFinalized; }
            set {
                if (!headersFinalized && value) {
                    headersFinalized = value;
                    ConnectionHandler.writeStatusLine(stream, StatusCode, Status);
                    ConnectionHandler.writeHeaders(stream, Headers);
                }
            }
        }

        string status = null;
        public string Status {
            get { return status == null ? HttpException.DefaultStatus(statusCode) : status; }
            set { status = value; }
        }
        int statusCode = 200;
        public int StatusCode {
            get { return statusCode; }
            set { statusCode = value; }
        }
        Headers headers;
        public Headers Headers {
            get { return headers; }
            set { headers = value; }
        }
        Request request;
        public IRequest Request {
            get { return request; }
        }
        private Stream bodyStream; // initialised in constructor
        public Stream Body {
            get { return bodyStream; }
        }
        private TextWriter writer;
        public TextWriter OpenWriter(string type) {
//            if (headersSent)
//                throw new Exception("Headers already sent, do not mix Stream and OpenWriter()");
            if (writer != null)
                throw new Exception("Writer was already created");
            headers["Content-Type"] = type + "; charset=utf-8";
            return writer = new StreamWriter(Body, new UTF8Encoding(false));
        }
        public void WriteText(string contentType, string text) {
            using (TextWriter writer = OpenWriter(contentType)) {
                writer.Write(text);
            }
        }
        public void WriteBytes(byte[] data) {
            Body.Write(data, 0, data.Length);
        }

        public Headers DefaultHeaders() {
            Headers headers = new Headers();
//            headers["Content-Type"] = "application/octet-stream";
            if(server.ServerHeader != null)
                headers["Server"] = server.ServerHeader;
            return headers;
        }

        public string URL(string contextRelativeUrl) {
            return string.Format("{0}://{1}{2}{3}{4}",
                Request.Protocol,
                Request.Host,
                (Request.Port == 80 ? "" : ":" + Request.Port),
                (Request.ContextPath == "/" ? "" : Request.ContextPath),
                contextRelativeUrl);
        }
        
        public void WriteFile(string path, string contentType) {
        	FileStream f=null;
        	try {
        		f = File.OpenRead(path);
        		WriteStream(f, contentType);
        	} catch (FileNotFoundException ex) {
        		throw new HttpException(404, null, "File not found: "+path, ex);
        	} catch (UnauthorizedAccessException ex) {
        		throw new HttpException(403, null, "File could not be read: "+path, ex);
        	} finally {
        		if(f != null) 
        			f.Close();
        	}
        }
        
        public void WriteResource(string resource, string contentType) {
        	// resource is assumed to come from the servlet instance's assembly
        	Assembly containingAssembly = request.Servlet.GetType().Assembly;
        	Stream stream = null;
        	
        	try {
	        	stream = containingAssembly.GetManifestResourceStream(resource);
        		WriteStream(stream, contentType);
        	} catch (FileNotFoundException ex) {
        		throw new HttpException(404, null, "Resource not found: "+resource, ex);
        	} finally {
        		if(stream != null)
        			stream.Close();
        	}
        	
        }
        
        public void WriteStream(Stream stream, string contentType) {
        	if(stream == null)
        		throw new HttpException(404, "The stream was null");

        	try {
        		this.Headers["Content-Length"] = stream.Length.ToString();
        	} catch (NotSupportedException) {}
       		this.Headers["Content-Type"] = contentType;
       		
       		int read;
       		byte[] buf = new Byte[1024];
       		while((read = stream.Read(buf, 0, buf.Length)) > 0)
      			Body.Write(buf, 0, read);
        }
    }
}
