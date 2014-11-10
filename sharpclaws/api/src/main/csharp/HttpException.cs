// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Twin.SharpClaws.API
{
	/// <summary>
	/// An exception that should cause a specific HTTP response, such as 404 not found.
	/// The servlet container will handle this exception and send the appropriate response. 
	/// It may include stack traces etc for debugging.
	/// </summary>
    public class HttpException : Exception
    {
        private int statusCode;
        /// <summary>
        /// The status code to be returned
        /// </summary>
        public int StatusCode { get { return statusCode; } }
        private string status;
        /// <summary>
        /// The status to be returned
        /// </summary>
        public string Status { get { return status; } }

        /// <summary>
        /// Create a '500 Internal Server Error' exception
        /// </summary>
        /// <param name="e">The inner exception</param>
        public HttpException(Exception e) : this(500, null, e.Message, e) { }
        /// <summary>
        /// Create an exception with the given status code and message
        /// </summary>
        /// <param name="statusCode">The HTTP status code such as 404</param>
        /// <param name="message">A descriptive message (not the HTTP status)</param>
        public HttpException(int statusCode, String message) : this(statusCode, null, message) { }
        /// <summary>
        /// Create an exception with the given status code, status, and message
        /// </summary>
        /// <param name="statusCode">The HTTP status code such as 404</param>
        /// <param name="status">The HTTP status code such as "Not Found"</param>
        /// <param name="message">A descriptive message (not the HTTP status)</param>
        public HttpException(int statusCode, String status, String message) : this(statusCode, status, message, null) { }
        /// <summary>
        /// Create an exception with the given status code, status, message, and nested exception.
        /// </summary>
        /// <param name="statusCode">The HTTP status code such as 404</param>
        /// <param name="status">The HTTP status code such as "Not Found"</param>
        /// <param name="message">A descriptive message (not the HTTP status)</param>
        /// <param name="b">The inner exception</param>
        public HttpException(int statusCode, String status, String message, Exception b) : base(message, b)
        {
            this.statusCode = statusCode;
            this.status = status == null ? DefaultStatus(statusCode) : status;
        }

        /// <summary>
        /// Get the default HTTP status for the given status code. 
        /// For example, DefaultStatus(404) is "Not Found".
        /// </summary>
        /// <param name="statusCode">The HTTP status code to look up.</param>
        /// <returns>The HTTP status for the given status code.</returns>
        public static String DefaultStatus(int statusCode)
        {
            switch (statusCode)
            {
                case 100: return "Continue";
                case 101: return "Switching protocols";
                case 200: return "OK";
                case 201: return "Created";
                case 202: return "Accepted";
                case 203: return "Non-Authoritative information";
                case 204: return "No content";
                case 205: return "Reset content";
                case 206: return "Partial content";

                case 300: return "Multiple choices";
                case 301: return "Moved permanently";
                case 302: return "Found";
                case 303: return "See other";
                case 304: return "Not modified";
                case 305: return "Use proxy";
                case 307: return "Temporary redirect";

                case 400: return "Bad request";
                case 401: return "Unauthorized";
                case 402: return "Payment required";
                case 403: return "Forbidden";
                case 404: return "Not found";
                case 405: return "Method not allowed";
                case 406: return "Not acceptable";
                case 407: return "Proxy authentication required";
                case 408: return "Request time-out";
                case 409: return "Conflict";
                case 410: return "Gone";
                case 411: return "Length required";
                case 412: return "Precondition failed";
                case 413: return "Request entity too large";
                case 414: return "Request URI too large";
                case 415: return "Unsupported media type";
                case 416: return "Requested range not satisfiable";
                case 417: return "Expectation failed";

                case 500: return "Internal server error";
                case 501: return "Not implemented";
                case 502: return "Bad gateway";
                case 503: return "Service unavailable";
                case 504: return "Gateway time-out";
                case 505: return "HTTP version not supported";

                default: return "Unknown status";
            }
        }
    }
}
