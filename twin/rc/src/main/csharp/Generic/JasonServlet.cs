// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Automation;
using Twin;
using Twin.SharpClaws.API;
using Twin.Logging;
using Twin.Model;

namespace Twin.Generic {
    abstract class JasonServlet : Servlet {
        Routes routes = new Routes();
        protected Routes Routes {
            get { return routes; }
        }
        public override void Initialize() {
            base.Initialize();
            routes.Log = this.Log;
        }
        
		public override void Handle(IRequest http) {
            Logger.Current.Trace("Finding match for {0} in {1}", http.RelativePath, routes);
            Dictionary<string, Action<IRequest>> methodHandlers = routes.Match(http.RelativePath);
            if (methodHandlers == null)
                throw new HttpException(404, "No resource mapped to path " + http.RelativePath);
            string method = http.Method.ToUpperInvariant();
            if (!methodHandlers.ContainsKey(method))
                throw new HttpException(405, "Method " + method + " not defined for path " + http.RelativePath);

            methodHandlers[method](http);
        }
    }
}
