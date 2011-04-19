// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Automation;

using Twin.SharpClaws.API;
using Twin.Logging;
using Twin.Model;
using Twin.View;

// ResponseStatus
namespace Twin.Generic
{
	class ParsedRequest {
		public readonly JasonServlet Servlet;
		public readonly IRequest Request;
		public readonly Dictionary<string, string> Parameters;
		public ParsedRequest(IRequest request, Dictionary<string, string> parameters) {
			this.Request = request;
			this.Servlet = (JasonServlet)request.Servlet;
			this.Parameters = parameters;
		}
	}
	delegate void Handler(ParsedRequest request);
	class Responder {
		private Handler handler;
		public Responder(Handler handler) {
			this.handler = handler;
		}
		public virtual void Respond(ParsedRequest request) {
			handler(request);
		}

		public static implicit operator Responder(Handler handler) {
			return new Responder(handler);
		}
		public static implicit operator Responder(JSONHandler handler) {
			return new JSONResponder(handler);
		}
		public static implicit operator Responder(SessionHandler handler) {
			return new SessionResponder(handler);
		}
		public static implicit operator Responder(ElementHandler handler) {
			return new ElementResponder(handler, false); // element must exist
		}
	}
	
	class ResourceResponder : Responder {
		string name;
		string contentType;
		public ResourceResponder(string name, string contentType) : base(null) {
			this.name = name;
			this.contentType = contentType;
		}
		public override void Respond(ParsedRequest request) {
			request.Request.Response.WriteResource(name, contentType);
		}
	}

	class JSONRequest : ParsedRequest {
		public JSONRequest(ParsedRequest parent) : base(parent.Request, parent.Parameters) {
            if (parent.Request.ContentType == "application/json") {
				byte[] dataBytes = parent.Request.ReadBytes();
				string data = dataBytes == null ? null : Encoding.UTF8.GetString(dataBytes);
                object body = JSON.ToObject(data);
                if (!(body is Dictionary<string, object>))
                    throw new HttpException(400, "Body was application/json, but decoded as a " + 
                	                        (body==null ? "null" : body.GetType().Name)
                	                        + " rather than Dictionary");
                Body = (Dictionary<string, object>)body;
            }
 		}
		public JSONRequest(JSONRequest parent) : base(parent.Request, parent.Parameters) {
			Body = parent.Body;
		}
		
        public Dictionary<string, object> Body;
    }
    class JSONResponse {
        public int StatusCode = 200;
        public Dictionary<string,object> Body;
        public string Location;
        public string[] Options;
    }
	delegate JSONResponse JSONHandler(JSONRequest request);
	class JSONResponder : Responder {
		JSONHandler handler;
		public JSONResponder(JSONHandler handler) : base(null) {
			this.handler = handler;
		}
		public virtual JSONResponse Respond(JSONRequest request) {
			return handler(request);
		}
		
		public override void Respond(ParsedRequest request) {
			JSONRequest jreq = new JSONRequest(request);
			JSONResponse jres = null;
            try {
            	jres = Respond(jreq);
            } catch (Exception e) {
                if (e is HttpException)
                    throw e;
                jres = GetExceptionResponse(e);
            }
            WriteJSONResponse(jres, request.Request.Response);
		}
		
        JSONResponse GetExceptionResponse(Exception e) {
        	Logger.Current.Trace("Building response for thrown exception");
            Logger.Current.Trace(e);
            JSONResponse response = new JSONResponse();
            response.StatusCode = 500;

            // TODO: should this be here? breaks layering
            ResponseStatus responseCode = ResponseStatus.UnknownError; // unknown
            if (e is TwinException)
                responseCode = ((TwinException)e).ResponseStatus;
            if (e is ElementNotAvailableException)
                responseCode = ResponseStatus.NoSuchElement;

            Dictionary<string, object> wrapper = new Dictionary<string, object>();
            wrapper["status"] = (int)responseCode;
            wrapper["value"] = EncodeException(e);

            response.Body = wrapper;
            return response;
        }		
		
		public static Object EncodeException(Exception e) {
            Dictionary<string, object> body = new Dictionary<string, object>();
            body["message"] = e.Message;
            body["class"] = e.GetType().FullName;

            List<object> traceJson = new List<Object>();
            StackTrace trace = new StackTrace(e, true);
            foreach (StackFrame frame in trace.GetFrames()) {
                Dictionary<string, object> frameJson = new Dictionary<string, object>();
                frameJson["className"] = frame.GetMethod().DeclaringType.FullName;
                frameJson["methodName"] = frame.GetMethod().Name;
                if (frame.GetFileName() != null)
                    frameJson["fileName"] = frame.GetFileName();
                if (frame.GetFileLineNumber() != 0)
                    frameJson["lineNumber"] = frame.GetFileLineNumber();
                traceJson.Add(frameJson);
            }
            if (e.InnerException != null && e.InnerException != e) {
                body["cause"] = EncodeException(e.InnerException);
            }

            body["stackTrace"] = traceJson;
            return body;
        }

        void WriteJSONResponse(JSONResponse response, IResponse http) {
            http.StatusCode = response.StatusCode;
            if (response.Location != null)
                http.Headers["Location"] = http.URL(response.Location);
            if (response.Options != null)
                http.Headers["Allow"] = string.Join(",", response.Options);

            if(response.Body != null) 
                using(TextWriter writer = http.OpenWriter("application/json")) {
                    // Ideally we'd stream the object for perf reasons.
                    // however during dev, if the serialiser hits an unrecognised object we want the stacktrace to be sent to the client
                    // this can't happen if data has already been written. So for now, convert to a string in memory, then write when done.
                    // JSON.Write(response.Body, writer);
                    writer.Write(JSON.ToString(response.Body, 4));
                }
        }
	}
	
	class SessionRequest : JSONRequest {
		public readonly Session Session;

        public SessionRequest(JSONRequest basic) : base(basic) {
			try {
	            this.Session = Sessions.GetSession(basic);
			} catch (ArgumentException ex) {
				throw new TwinException(ResponseStatus.NoSuchSession, ex.Message, ex);
			}
        }		
	}
	delegate object SessionHandler(SessionRequest request);
	class SessionResponder : JSONResponder {
		SessionHandler handler;
		public SessionResponder(SessionHandler handler) : base(null) {
			this.handler = handler;
		}
		public virtual object Respond(SessionRequest request) {
			return handler(request);
		}
		
		public override JSONResponse Respond(JSONRequest request) {
            SessionRequest sessionRequest = new SessionRequest(request);
            object value = Respond(sessionRequest);

            if (value is JSONResponse)
                return (JSONResponse)value;

            JSONResponse response = new JSONResponse();
            response.Body = new Dictionary<string, object>();
            response.Body["sessionId"] = sessionRequest.Session.ToString();
            response.Body["status"] = (int)ResponseStatus.Success;
            response.Body["value"] = value;
            return response;
		}
	}
	
	class ElementRequest : SessionRequest {
        public ElementRequest(SessionRequest basic, Element target) : base(basic) {
            Target = target;
        }
		
        public readonly Element Target;
    }
	delegate object ElementHandler(ElementRequest request);
	class ElementResponder : SessionResponder {
		ElementHandler handler;
		bool optional;
		public ElementResponder(ElementHandler handler, bool optional) : base(null) {
			this.handler = handler;
			this.optional = optional;
		}
		public virtual object Respond(ElementRequest request) {
			return handler(request);
		}
	
		public override object Respond(SessionRequest request) {
			Element element = GetTargetElement(request);
			if(!optional && !element.Exists)
				throw new ElementNotAvailableException("Element no longer exists");
			return Respond(new ElementRequest(request, element));
		}
		
        protected virtual Element GetTargetElement(SessionRequest basic) {
            string targetId = (string)basic.Parameters["target"];
            return (Element)basic.Session[new Guid(targetId)];
        }
	}
	
	class DesktopResponder : ElementResponder {
		public DesktopResponder(ElementHandler handler) : base(handler, true) {} // optional for efficiency, desktop will always exist
		
		public static explicit operator DesktopResponder(ElementHandler handler) {
			return new DesktopResponder(handler);
		}		
        protected override Element GetTargetElement(SessionRequest basic) {
            return Desktop.GetInstance(basic.Session.Process.Id);
        }
    }
}
