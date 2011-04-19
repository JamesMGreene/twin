// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using Twin.Logging;

namespace Twin.SharpClaws.API
{
	/// <summary>
	/// An incoming HTTP request from a client.
	/// </summary>
    public interface IRequest
    {
    	/// <summary>
    	/// The servlet responsible for this request
    	/// </summary>
    	Servlet Servlet { get; }
    	
    	/// <summary>
    	/// The encoding of the body (if it is text). 
    	/// This is the encoding specified in the Content-Type, or ISO-8859-1 by default.
    	/// </summary>
        Encoding Encoding { get; }
        /// <summary>
        /// Query-string parameters of the form ?key1=value1&key2=value2 from the URL.
        /// Does not include POSTed form data.
        /// </summary>
        NameValueCollection Parameters { get; }

        /// <summary>
        /// A stream representing the entity body, or null if no body was sent.
        /// </summary>
        Stream Body { get; }
        /// <summary>
        /// Creates a reader to read from the entity body. 
        /// </summary>
        /// <returns>A reader reading from Body</returns>
        TextReader OpenReader();
        /// <summary>
        /// Reads the body as a collection of POSTed form data: key1=value1&key2=value2
        /// </summary>
        /// <returns>The form data as a name/value collection</returns>
        NameValueCollection ReadParameters();
        /// <summary>
        /// Reads the whole body as text using Encoding.
        /// </summary>
        /// <returns>The body text</returns>
        string ReadText();
        /// <summary>
        /// Reads the whole body as raw data.
        /// </summary>
        /// <returns>The data of the body.</returns>
        byte[] ReadBytes();
        /// <summary>
        /// The HTTP Content-Type, with encoding information stripped out.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// The request protocol, e.g. http
        /// </summary>
        String Protocol { get; }
        /// <summary>
        /// The local IP address/port the request was received on.
        /// </summary>
        IPEndPoint LocalAddress { get; }
        /// <summary>
        /// The remote IP address/port that sent the request.
        /// </summary>
        IPEndPoint RemoteAddress { get; }
        /// <summary>
        /// The hostname as specified in the Host: header of the request.
        /// If none was specified, then the LocalAddress IP.
        /// </summary>
        string Host { get; }
        /// <summary>
        /// The port as specified in the request.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// The servlet path relative to the server root.
        /// </summary>
        string ContextPath { get; }
        /// <summary>
        /// The request path relative to the servlet path.
        /// </summary>
        string RelativePath { get; }
        /// <summary>
        /// The request path relative to the server root.
        /// </summary>
        string Path { get; }
        /// <summary>
        /// The query string (portion of the request after "?")
        /// </summary>
        string Query { get; }
        /// <summary>
        /// The request method (e.g. GET)
        /// </summary>
        string Method { get; }

        /// <summary>
        /// The request headers
        /// </summary>
        Headers Headers { get; }
        /// <summary>
        /// Get the Response object that should be used to respond to this request.
        /// </summary>
        IResponse Response { get; }
    }
}
