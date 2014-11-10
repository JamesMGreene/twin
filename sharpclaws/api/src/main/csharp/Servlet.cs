// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Twin.Logging;

namespace Twin.SharpClaws.API
{
	/// <summary>
	/// An application capable of handling HTTP requests when used in a servlet container.
	/// You should extend this class and override either Handle(), 
	/// or at least one of HandleGet(), HandlePost(), HandleOther(). 
	/// </summary>
    public class Servlet
    {
        private String contextPath;
        /// <summary>
        /// The path of this servlet relative to the server root.
        /// This will be set by the servlet container.
        /// </summary>
        public String ContextPath {
            get { return contextPath; }
            set { contextPath = value; }
        }

        private IPEndPoint ipEndPoint;
        /// <summary>
        /// The interface and port that the servlet is responding to.
        /// The interface may be a wildcard (IPAddress.Any)
        /// </summary>
        public IPEndPoint IPEndPoint {
            get { return ipEndPoint; }
            set { ipEndPoint = value; }
        }

        private Logger log;
        /// <summary>
        /// The logger for this servlet.
        /// The servlet container will set this, and will provide this logger 
        /// (or a wrapper backed by it) as Logger.Current during requests.
        /// </summary>
        public Logger Log {
            get { return log; }
            set { log = value; }
        }
        private Dictionary<string, string> config;
        /// <summary>
        /// Servlets are configured with name/value pairs in a container-specific way.
        /// The servlet container will set this before Initialize() is called
        /// </summary>
        public Dictionary<string, string> Configuration {
            get { return config; }
            set { config = value; }
        }
        /// <summary>
        /// This method is called after the servlet has been created and its 
        /// configuration has been provided.
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// This method will be called by the container before server shutdown.
        /// </summary>
        public virtual void Destroy() { }

        /// <summary>
        /// Determine whether a request should be continued or vetoed in response to an 
        /// Expect: 100-continue header.
        /// </summary>
        /// <param name="request">The request (you must not access the Body).</param>
        /// <returns>True if 100: Continue should be sent, False to veto the request.</returns>
        public virtual bool ShouldContinue(IRequest request) {
            return true;
        }
        /// <summary>
        /// This method will be invoked by the container when an HTTP request is received.
        /// The servlet should send a response using the request.Response object or throw an HTTPException. 
        /// The default implementation calls HandleGet, HandlePost, or HandleOther.
        /// </summary>
        /// <param name="request">The request that was received.</param>
        public virtual void Handle(IRequest request)
        {
            if ("GET".Equals(request.Method, StringComparison.InvariantCultureIgnoreCase))
                HandleGet(request);
            else if ("POST".Equals(request.Method, StringComparison.InvariantCultureIgnoreCase))
                HandlePost(request);
            else
                HandleOther(request);
        }
        
        /// <summary>
        /// Handle a GET request.
        /// This is called by the default implementation of Handle() when a GET request is received. 
        /// If you override Handle() then this method will not be called by the servlet container.
        /// The default implementation of this method throws an HttpException representing 405 - method not allowed.
        /// </summary>
        /// <param name="request">The request received.</param>
        protected virtual void HandleGet(IRequest request)
        {
            throw new HttpException(405, "HandleGet not overridden in "+this.GetType().ToString());
        }
        /// <summary>
        /// Handle a POST request.
        /// This is called by the default implementation of Handle() when a POST request is received. 
        /// If you override Handle() then this method will not be called by the servlet container.
        /// The default implementation of this method throws an HttpException representing 405 - method not allowed.
        /// </summary>
        /// <param name="request">The request received.</param>
        protected virtual void HandlePost(IRequest request)
        {
            throw new HttpException(405, "HandlePost not overridden in " + this.GetType().ToString());
        }
        /// <summary>
        /// Handle an HTTP request.
        /// This is called by the default implementation of Handle() when a request is received with a method other than GET or POST. 
        /// If you override Handle() then this method will not be called by the servlet container.
        /// The default implementation of this method throws an HttpException representing 405 - method not allowed.
        /// </summary>
        /// <param name="request">The request received.</param>
        protected virtual void HandleOther(IRequest request)
        {
            throw new HttpException(405, "HandleOther not overridden in " + this.GetType().ToString());
        }
    }
}
