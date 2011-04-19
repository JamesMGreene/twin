// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Twin.Proxy;

namespace Twin.Model {
    class Attachment : IDisposable, IJSONProperties {
        public Attachment() {
            path = System.IO.Path.GetTempFileName();
        }
        public Attachment(string extension) {
            path = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + "." + extension;
        }

        public void Dispose() {
            if(path != null && System.IO.File.Exists(path))
                System.IO.File.Delete(path);
            path = null;
        }

        private string path;
        public string Path {
            get {
                return path;
            }
        }

        public byte[] Data {
            get {
                return System.IO.File.ReadAllBytes(path);
            }
            set {
                System.IO.File.WriteAllBytes(path, value);
            }
        }

        public void AddExtraJSONProperties(IDictionary<string, object> values) {
            values["path"] = path;
        }
    }
}
