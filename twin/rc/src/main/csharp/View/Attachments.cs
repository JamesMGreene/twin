// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;

using Twin.Generic;
using Twin.Model;
using Twin.Proxy;

namespace Twin.View {
    class Attachments {
        public static object Create(SessionRequest request) {
            string extension = null;
            if (request.Body.ContainsKey("name")) {
                string name = (string)request.Body["name"];
                if (name != null && name.Contains("."))
                    extension = name.Substring(name.LastIndexOf(".") + 1);
            }
            Attachment attachment = (extension == null) ? new Attachment() : new Attachment(extension);
            UpdateAttachment(attachment, request.Body);
            return PersistedObject.Get(attachment, request.Session);
        }
        public static object Get(SessionRequest request) {
            Attachment attachment = (Attachment)request.Session[new Guid(request.Parameters["attachment"])];
            return PersistedObject.GetNoCreate(attachment, request.Session);
        }
        public static object Update(SessionRequest request) {
            Attachment attachment = (Attachment)request.Session[new Guid(request.Parameters["attachment"])];
            UpdateAttachment(attachment, request.Body);
            return PersistedObject.GetNoCreate(attachment, request.Session);
        }
        public static object Delete(SessionRequest request) {
            Attachment attachment = (Attachment)request.Session[new Guid(request.Parameters["attachment"])];
            request.Session.Release(attachment);
            attachment.Dispose();
            return null;
        }

        private static void UpdateAttachment(Attachment attachment, Dictionary<string,Object> body) {
            byte[] data = Convert.FromBase64String((string)body["data"]);
            attachment.Data = data;
        }
    }
}
