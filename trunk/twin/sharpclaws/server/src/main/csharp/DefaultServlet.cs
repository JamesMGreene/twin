// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using Twin.SharpClaws.API;

namespace Twin.SharpClaws
{
    class DefaultServlet : Servlet
    {
        protected override void HandleGet(IRequest request)
        {
            throw new HttpException(404, "The requested object was not found.");
        }
    }
}
