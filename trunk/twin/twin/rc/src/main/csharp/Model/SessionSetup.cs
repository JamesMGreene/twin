// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using Twin.Logging;

namespace Twin.Model {
    interface SessionSetup {
        void Before(Session s);
        void After(Session s);
    }

    class FileSessionSetup : SessionSetup {
        Dictionary<String, byte[]> setups = new Dictionary<String, byte[]>();
        Dictionary<String, bool> shouldRollback = new Dictionary<string, bool>();
        Dictionary<String, byte[]> rollbacks = new Dictionary<String, byte[]>();

        public FileSessionSetup(Configuration conf, Dictionary<string, object> config) {
            if (config != null && config.ContainsKey("files")) {
                foreach (object fileObj in (List<object>)config["files"]) {
                    Dictionary<string, object> file = (Dictionary<string, object>)fileObj;
                    string relFileName = (string)file["path"];
                    string fileName = Path.Combine(Path.GetDirectoryName(conf.appPath), relFileName);

                    setups[fileName] = Convert.FromBase64String((string)file["data"]);
                    shouldRollback[fileName] = !file.ContainsKey("revert") || Convert.ToBoolean(file["revert"]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Before(Session s) {
            foreach (string fileName in setups.Keys) {
                try {
                    if (shouldRollback[fileName]) {
                        if (File.Exists(fileName)) {
                            rollbacks[fileName] = File.ReadAllBytes(fileName);
                        } else {
                            rollbacks[fileName] = null;
                        }
                    }
                    File.WriteAllBytes(fileName, setups[fileName]);
                } catch (IOException e) {
                    Logger.Current.Error("Failed to perform file session setup for file {0}", fileName);
                    Logger.Current.Error(e);
                }
            }
        }

        public void After(Session s) {
            foreach (string fileName in rollbacks.Keys) {
                try {
                    byte[] data = rollbacks[fileName];
                    if (data == null) {
                        File.Delete(fileName);
                    } else {
                        File.WriteAllBytes(fileName, data);
                    }
                } catch (IOException e) {
                    Logger.Current.Error("Failed to perform file session teardown for file {0}", fileName);
                    Logger.Current.Error(e);
                }
            }
        }
    }
}
