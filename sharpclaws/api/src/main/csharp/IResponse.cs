// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Twin.SharpClaws.API
{
	/// <summary>
	/// Provides an API for servlets to compose and send responses to clients.
	/// Obtained via the IRequest.Response property.
	/// </summary>
    public interface IResponse
    {
    	/// <summary>
    	/// The response headers (must all be set prior to using Body)
    	/// </summary>
        Headers Headers { get; }
        /// <summary>
        /// The status code to return (must be set prior to using Body)
        /// </summary>
        int StatusCode { get; set; }
        /// <summary>
        /// The status message to return (must be set prior to using Body)
        /// </summary>
        string Status { get; set; }
        /// <summary>
        /// The request that this is a response to.
        /// </summary>
        IRequest Request { get; }
        
        /// <summary>
        /// A stream writing to the client. If Content-Length has not been set, then 
        /// accessing this property will cause Transfer-Encoding to be set to Chunked.
        /// After accessing this property, Headers, StatusCode and Status cannot be changed.
        /// When the Body is closed, the response is complete.
        /// </summary>
        Stream Body { get; }
        /// <summary>
        /// Open a Writer around Body for the given Content-Type.
        /// The charset used (normally UTF-8) will be appended to the given content-type.
        /// </summary>
        /// <param name="contentType">The content-type of the text data, without encoding information</param>
        /// <returns>A TextWriter for writing the response</returns>
        TextWriter OpenWriter(string contentType);
        /// <summary>
        /// Send a text response to the client.
        /// </summary>
        /// <param name="contentType">The content-type of the response. The charset will be appended.</param>
        /// <param name="text">The text to send.</param>
        void WriteText(string contentType, string text);
        /// <summary>
        /// Send a binary response to the client
        /// </summary>
        /// <param name="bytes">The data to send</param>
        void WriteBytes(byte[] bytes);

        /// <summary>
        /// Get an absolute URL for the provided path.
        /// </summary>
        /// <param name="relativePath">A path relative to the current servlet.</param>
        /// <returns>A URL usable by the client (hostname etc will match this request)</returns>
        string URL(string relativePath);
        
        /// <summary>
        /// Send a literal resource embedded into the servlet's assembly to the client.
        /// If the resource is not found, 404 is sent instead.
        /// </summary>
        /// <param name="name">The resource name, e.g. "Project1.file.txt"</param>
        /// <param name="contentType">The content-type to use</param>
        void WriteResource(string name, string contentType);
        /// <summary>
        /// Send a local file to the client as the body.
        /// If the file is not found, 404 is sent instead.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="contentType">The content-type to use.</param>
        void WriteFile(string path, string contentType);
        /// <summary>
        /// Copy the contents of an input stream to the client as the body.
        /// If the stream is null, 404 is sent instead.
        /// </summary>
        /// <param name="stream">The stream to send</param>
        /// <param name="contentType">The content-type to use</param>
        void WriteStream(Stream stream, string contentType);
    }
}
