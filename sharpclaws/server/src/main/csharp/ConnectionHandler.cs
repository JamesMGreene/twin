// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;
using Twin.Logging;
using Twin.SharpClaws.API;

namespace Twin.SharpClaws
{
    class ConnectionHandler
    {
        private TcpClient client;
        private Server server;
        private ConnectionLogger log;

        public ConnectionHandler(Server server, TcpClient c)
        {
            this.server = server;
            this.client = c;
            this.log = new ConnectionLogger(server.Log, c.Client.RemoteEndPoint);
        }

        public void Start() {
            Thread t = new Thread(new ThreadStart(Run));
            t.Start();
        }

        private void HandleExpectation(Request request, Stream stream) {
            foreach (string expectation in request.Headers.All("Expect")) {
                switch (expectation.ToLowerInvariant()) {
                    case "100-continue":
                        log.Trace("Got Expect: 100-Continue, querying servlet");
                        if (request.Servlet.ShouldContinue(request)) {
                            log.Trace("Sending 100-Continue intermediate status");
                            writeStatusLine(stream, 100, "Continue");
                            write(stream, "\r\n");
                        } else {
                            throw new HttpException(417, "Servlet indicated the request should not continue");
                        }
                        break;
                    default:
                        throw new HttpException(417, "Couldn't satisfy the expectation " + expectation);
                }
            }
        }
        
        public void Run()
        {
           	bool normalConnection=true; // whether connection was 'successful' and we can send a valid HTTP response (if not, don't try to keepalive)
           	bool keepAlive = true; // whether the client allows keepalive
           	
            log.Trace("Accepted connection from {0}", client.Client.RemoteEndPoint);
            NetworkStream stream = client.GetStream();
            Logger.Current = log;
            int requests = 0;
            try {
            	while(keepAlive && normalConnection) {
            		normalConnection = false;
            		
	                Request request = null;
	                try {
	                	log.Request = null;
	                    request = readRequest(stream);
	                    requests++;
	                    log.Request = request;
	                    log.Trace("Parsed request: method={0} path={1} query={2}", request.Method, request.Path, request.Query);
	                    server.MapServlet(request);
	                    HandleExpectation(request, stream);
	                    foreach(string connection in request.Headers.All("Connection"))
	                    	if(connection.Equals("close", StringComparison.InvariantCultureIgnoreCase))
	                    		keepAlive = false;
	                    
	                    log.Trace("Mapped request to servlet={0} context={1} relative={2}", request.Servlet, request.ContextPath, request.RelativePath);
	                } catch(TimeoutException ex) {
	                	log.Trace("Couldn't read request, will close connection. Served {0} requests: {1}", requests, ex.Message);
	                	normalConnection = false;
	                	continue;
	                } catch (Exception e) {
	                    log.Error("Error parsing/mapping request");
	                    log.Error(e);
	                    writeResponse(stream, request, e);
	                    stream.Flush();
	                    return;
	                }
	
	                try {
	                    try {
	                        long start = System.DateTime.Now.Ticks;
	                        request.Servlet.Handle(request);
	                        long end = System.DateTime.Now.Ticks;
	                        long duration = end - start;
	
	                        log.Trace("Servlet executed in {0}.{1:3}ms", duration / 10000, (duration / 10) % 1000);
	
	                        request.Response.Body.Close();
	                        request.response.HeadersFinalized = true;
	                        log.Info("Sent response: statusCode={0} status={1} contentType={2}", request.Response.StatusCode, request.Response.Status, request.Response.Headers["Content-Type"]);
	                        normalConnection = true;
	                    } finally {
	                        try {
	                            request.Response.Body.Flush();
			                    stream.Flush();
	                        } catch (Exception) { }
	                    }
	                } catch (Exception e) {
	                    bool headersSent = request.response.HeadersFinalized;
						normalConnection = !headersSent;
	                    bool normal = !headersSent && (e is HttpException) && isRoutine((HttpException)e);
	                    
	                    if (normal) { // don't log if we can send the response to the client and it's a 'boring' HTTP exception like a 404
	                        log.Info("Sent standard response: statusCode={0}", ((HttpException)e).StatusCode);
	                        log.Trace(e);
	                    } else {
	                        log.Error("Exception thrown by servlet");
	                        if (headersSent)
	                            log.Error("Headers already sent, so the error will not be shown to the user");
	                        log.Error(e);
	                    }
	
	                    if (!headersSent) {
	                        writeResponse(stream, request, e);
	                    }
	                    stream.Flush();
	                }
            	} // while keepalive
            } finally {
                Logger.Current = null;
                log.Request = null;
				log.Trace("Closing connection to {0}", client.Client.RemoteEndPoint);
				stream.Flush();
				// XXX Without this line we sometimes seem to send a RST without the last PSH (so client gets ECONNRESET etc)
                client.Client.Shutdown(SocketShutdown.Send);
                stream.Close();
                client.Close();
            }
        }

        private bool isRoutine(HttpException e) {
            return e.StatusCode == 404 || e.StatusCode == 405;
        }

        Request readRequest(Stream stream) {
        	int origTimeout = stream.ReadTimeout;
        	RequestLine requestLine;
        	try {
        		stream.ReadTimeout = 10000;
	            requestLine = readRequestLine(stream);
	            if(requestLine == null)
	            	throw new TimeoutException("EOF reading request line");
        	} catch(IOException ex) {
        		if(ex.InnerException is SocketException && ((SocketException)ex.InnerException).SocketErrorCode == SocketError.TimedOut)
        			throw new TimeoutException("Timed out reading request line", ex);
        		throw new TimeoutException("IOException reading request line", ex);
        	} finally {
        		stream.ReadTimeout = origTimeout;
        	}
            Headers headers = readHeaders(stream);

            Request impl = new Request(server, stream);
            impl.Headers.AddRange(headers);
            impl.Method = requestLine.method;

            if (requestLine.path.Contains("?")) {
                impl.Path = requestLine.path.Substring(0, requestLine.path.IndexOf('?'));
                impl.Query = requestLine.path.Substring(requestLine.path.IndexOf('?') + 1);
            } else {
                impl.Path = requestLine.path;
                impl.Query = "";
            }

            impl.LocalAddress = (IPEndPoint)client.Client.LocalEndPoint;
            impl.RemoteAddress = (IPEndPoint)client.Client.RemoteEndPoint;
            if (headers["Host"] != null) {
                string[] hostHeader = headers["Host"].Split(':');
                if (hostHeader.Length == 1) {
                    impl.Host = hostHeader[0];
                    impl.Port = 80;
                } else {
                    impl.Host = hostHeader[0];
                    impl.Port = Convert.ToInt32(hostHeader[1]);
                }
            } else { // have to guess
                impl.Host = impl.LocalAddress.Address.ToString();
                impl.Port = impl.LocalAddress.Port;
            }
            impl.Protocol = "http";

            impl.Body = CreateBodyStream(impl, stream);

            return impl;
        }

        Stream CreateBodyStream(Request request, Stream stream) {
            if (request.Headers["Transfer-Encoding"] != null && "chunked".Equals(request.Headers["Transfer-Encoding"], StringComparison.InvariantCultureIgnoreCase)) {
                return new ChunkedStream(stream, true, false);
            } else if (request.Headers["Content-Length"] != null) {
                try {
                    return new LengthBoundStream(stream, Convert.ToInt64(request.Headers["Content-Length"]), 0);
                } catch (FormatException) {
                    throw new HttpException(400, "Bad Content-Length value " + request.Headers["Content-Length"]);
                }
            }
            return null;
        }

        class RequestLine {
        	public RequestLine(string method, string path, string protocol) {
        		this.method = method;
        		this.path = path;
        		this.protocol = protocol;
        	}
        	internal string method;
        	internal string path;
        	internal string protocol;
        }
        
        RequestLine readRequestLine(Stream stream) {
            string line = readLine(stream);
            if(line == null)
            	return null;
            string[] chunks = line.Split(' ');
            if (chunks.Length != 3)
                throw new HttpException(400, "Bad request line: " + line);
            if (!chunks[2].Equals("HTTP/1.1", StringComparison.InvariantCultureIgnoreCase) && !chunks[2].Equals("HTTP/1.0", StringComparison.InvariantCultureIgnoreCase))
                throw new HttpException(505, "Unrecognised HTTP version in request line " + line);
            return new RequestLine(chunks[0], chunks[1], chunks[2]);
        }

        internal string readLine(Stream stream) {
            StringBuilder line = new StringBuilder();
            byte[] bytes = new byte[1];
            while (true) {
            	int nextByte = stream.ReadByte();
                if (nextByte == -1 || nextByte == '\n') {
                    if (nextByte == -1 && line.Length == 0)
                        return null;
                    return line.ToString().TrimEnd('\r', '\n');
                }
                bytes[0] = (byte)nextByte;
                line.Append(Encoding.ASCII.GetChars(bytes));
            }
        }

        internal Headers readHeaders(Stream stream) {
            Headers headers = new Headers();
            while (true) {
                string line = readLine(stream);
                if (line == null)
                    throw new HttpException(400, "EOF reached before end of headers");
                if (line.Length == 0)
                    return headers;
                if (!line.Contains(":"))
                    throw new HttpException(400, "No colon in header line: " + line);

                string key = line.Substring(0, line.IndexOf(':')).Trim();
                string value = line.Substring(line.IndexOf(':') + 1).Trim();
                headers[key] = value;
            }
        }

        internal void writeResponse(Stream stream, Request request, Exception exception) {
            ErrorPage error = new ErrorPage(server, request, exception);

            Headers headers = new Headers();
            headers["Content-Type"] = error.ContentType;
            headers["Content-Length"] = error.Body.Length.ToString();

            writeStatusLine(stream, error.StatusCode, error.Status);
            writeHeaders(stream, headers);
            stream.Write(error.Body, 0, error.Body.Length);
        }

        internal static void write(Stream stream, string format, params object[] data) {
            string text = string.Format(format, data);
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

        internal static void writeStatusLine(Stream stream, int statusCode, string status) {
            write(stream, "HTTP/1.1 {0} {1}\r\n", statusCode, status);
        }
        internal static void writeHeaders(Stream stream, Headers headers) {
            foreach (KeyValuePair<string, string> header in headers)
                write(stream, "{0}: {1}\r\n", header.Key, header.Value);
            write(stream,"\r\n");
        }
    }
}
