// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Twin.SharpClaws.API;

namespace Twin.SharpClaws {
    class ErrorPage {
        Exception exception;
        HttpException httpException;
        Server server;
        Request request;
        public ErrorPage(Server server, Request request, Exception e) {
            if (e == null)
                e = new NullReferenceException("Error page passed a null exception");
            this.exception = e;
            this.httpException = (e is HttpException) ? (HttpException)e : new HttpException(e);
            this.server = server;
            this.request = request;
        }

        public int StatusCode {
            get {
                return httpException.StatusCode;
            }
        }
        public string Status {
            get {
                return httpException.Status;
            }
        }
        public string ContentType {
            get { return "text/html; charset=utf-8"; }
        }
        byte[] body;
        public byte[] Body {
            get {
                if (body == null)
                    body = new UTF8Encoding(false).GetBytes(Text);
                return body;
            }
        }
        string text;
        public String Text {
            get {
                if (text == null) {
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine("<html>");
                    sb.AppendLine("<head>");
                    sb.AppendFormat("<title>{0} {1}</title>\n", StatusCode, escape(Status));
                    sb.Append(@"
<style type=""text/css"">
body { font-family: sans-serif; }
dl { margin: 0; }
dt { font-weight: bold; clear: left; text-align: right; float: left; width: 10em; line-height: 1.5em; margin: 0 1em 0 0; }
dd { clear: right; padding: 0.1em 0 0.1em 2em; line-height: 1.5em; }
h2 { font-size: 140%; margin-bottom: 0; }
h3 { font-size: 100%; margin-bottom: 0; }
#exception,#request { display: none; }
</style>
<script type=""text/javascript"">
    function toggle(name) {
        var elt = document.getElementById(name);
        elt.style.display = (elt.style.display == 'block') ? 'none' : 'block';
    }
</script>
                    ");
                    sb.AppendLine("</head>");
                    sb.AppendLine("<body>");
                    sb.AppendFormat("<h1>{0} {1}</h1>\n", StatusCode, escape(Status));
                    sb.AppendLine("<p>An error occurred processing your request.</p>");
                    if (exception.Message != null)
                        sb.AppendFormat("<p><b>{1}</b> ({0})</p>\n", escape(exception.GetType().Name), escape(exception.Message));
                    sb.AppendLine("<h2 class=\"expand\"><a href=\"#\" onclick=\"toggle('exception');return false\">Exception details</a></h2>\n");
                    sb.AppendLine("<div id=\"exception\">");
                    dumpException(sb, exception);

                    Exception current = exception;
                    while (current.InnerException != null) {
                        current = current.InnerException;
                        sb.AppendLine("<h3>Cause</h3>\n");
                        dumpException(sb, current);
                    }
                    sb.AppendLine("</div>");

                    sb.AppendLine("<h2 class=\"expand\"><a href=\"#\" onclick=\"toggle('request');return false\">Request details</a></h2>");
                    sb.AppendLine("<div id=\"request\">");
                    if (request == null) {
                        sb.AppendLine("<p>Request details not available as an error occurred before the request could be parsed.</p>");
                    } else {
                        sb.AppendLine("<h3>Basic</h3>");
                        sb.AppendLine("<dl>");
                        sb.AppendFormat("<dt>{0}</dt><dd>{1}</dd>\n", "Path", escape(request.Path));
                        sb.AppendFormat("<dt>{0}</dt><dd>{1}</dd>\n", "Query", escape(request.Query));
                        sb.AppendFormat("<dt>{0}</dt><dd>{1}</dd>\n", "Host", escape(request.Host));
                        sb.AppendFormat("<dt>{0}</dt><dd>{1}</dd>\n", "Protocol", escape(request.Protocol));
                        sb.AppendFormat("<dt>{0}</dt><dd>{1}</dd>\n", "Port", escape(request.Port.ToString()));
                        sb.AppendLine("</dl>");

                        sb.AppendLine("<h3>Servlet</h3>");
                        sb.AppendLine("<dl>");
                        sb.AppendFormat("<dt>{0}</dt><dd>{1}</dd>\n", "Servlet", escape(request.Servlet == null ? null : request.Servlet.GetType().FullName));
                        sb.AppendFormat("<dt>{0}</dt><dd>{1}</dd>\n", "Servlet Path", escape(request.ContextPath));
                        sb.AppendFormat("<dt>{0}</dt><dd>{1}</dd>\n", "Relative path", escape(request.RelativePath));
                        sb.AppendLine("</dl>");

                        sb.AppendLine("<h3>Headers</h3>");
                        sb.AppendLine("<dl>");
                        foreach (KeyValuePair<string, string> header in request.Headers) {
                            sb.AppendFormat("<dt>{0}</dt><dd>{1}</dd>\n", escape(header.Key), escape(header.Value));
                        }
                        sb.AppendLine("</dl>");
                    }
                    sb.AppendLine("</div>");

                    if (server.ServerHeader != null) {
                        sb.AppendLine("<hr>");
                        sb.AppendFormat("<p><i>Powered by {0}</i></p>\n", escape(server.ServerHeader));
                    }
                    sb.AppendLine("</body>");
                    sb.AppendLine("</html>");

                    text = sb.ToString();
                }
                return text;
            }
        }

        private void dumpException(StringBuilder sb, Exception exception) {
            sb.Append("<pre><code>");

            MethodBase method = exception.TargetSite;
            Type type = method.DeclaringType;
            string parameters = paramsString(method.GetParameters());

            sb.AppendFormat("{0}: {1}\n", escape(exception.GetType().FullName), escape(exception.Message));
//            sb.AppendFormat("{0}.{1}({2}) threw a {3}\n", escape(type.FullName), escape(method.Name), escape(parameters), escape(exception.GetType().FullName));
            sb.Append(escape(exception.StackTrace));
            sb.AppendLine("</pre></code>");
        }

        private string paramsString(ParameterInfo[] parameters) {
            StringBuilder sb = new StringBuilder();
            foreach (ParameterInfo parm in parameters) {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(parm.ParameterType.FullName);
            }
            return sb.ToString();
        }

        private string escape(string text) {
            if (text == null)
                text = "<null>";
            text = text.Trim();
            if (text.Length == 0)
                return "&nbsp;";
            return text.Replace("&", "&amp;").Replace("\"","&quot;").Replace("<", "&lt;");
        }
    }
}
