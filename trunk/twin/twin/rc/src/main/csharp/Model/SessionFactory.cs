// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics;
using Twin.SharpClaws.API;
using Twin.Generic;

namespace Twin.Model {
    class SessionFactory {
        TwinRC servlet;
        public readonly List<Configuration> Configurations = new List<Configuration>();

        public SessionFactory(TwinRC servlet) {
            this.servlet = servlet;
            createConfigurations();
        }
        private Dictionary<Guid, Session> sessions = new Dictionary<Guid, Session>();

        public Dictionary<Guid, Session> GetSessions() {
            return new Dictionary<Guid, Session>(sessions);
        }

        public Session this[string id] {
            get {
                Guid guid = Guid.Empty;
                try {
                    guid = new Guid(id);
                } catch (Exception) { }
                if (sessions.ContainsKey(guid))
                    return sessions[guid];
                throw new ArgumentException("Session " + id + " doesn't exist");
            }
        }

		// HashSet is in System.Core 3.5 or later. 
		// We actually do reference .NET 3.5 libraries but explicitly referencing System.Core 
		// causes build errors...
        private Dictionary<int,object> LockedProcesses = new Dictionary<int,object>();
        public void LockProcess(Process p) {
            if (!TryLockProcess(p))
                throw new Exception("Failed to lock process " + p + " - is it already in use?");
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TryLockProcess(Process p) {
            if (LockedProcesses.ContainsKey(p.Id))
                return false;
            LockedProcesses.Add(p.Id, new Object());
            return true;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void UnlockProcess(Process p) {
            if (!LockedProcesses.ContainsKey(p.Id))
                throw new Exception("Failed to unlock process " + p + " - seems not to be locked.");
            LockedProcesses.Remove(p.Id);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void createConfigurations() {
            foreach (KeyValuePair<string, string> kv in servlet.Configuration) {
                if (!(kv.Key.StartsWith("app.") && kv.Key.EndsWith(".path")))
                    continue;

                string identifier = kv.Key.Substring("app.".Length);
                identifier = identifier.Substring(0, identifier.Length - ".path".Length);
                Configuration config;
                try {
                    config = new Configuration(identifier, kv.Value);
                } catch (FileNotFoundException ex) {
                    servlet.Log.Error("Skipping app '{0}' - executable does not exist: {1}", identifier, kv.Value);
                    servlet.Log.Trace(ex);
                    continue;
                }

                config.properties["rc"] = servlet.ExternalUri.ToString();
                string prefix = "app." + identifier + ".";
                foreach (KeyValuePair<string, string> pair in servlet.Configuration) {
                    if (pair.Key.StartsWith(prefix)) {
                        string property = pair.Key.Substring(prefix.Length);
                        switch (property) {
                            case "path": // handled
                                break;
                            case "name":
                                config.properties["applicationName"] = pair.Value;
                                break;
                            default:
                                config.properties[property] = pair.Value;
                                break;
                        }
                    }
                }
                if (!config.properties.ContainsKey("applicationName"))
                    config.properties["applicationName"] = config.identifier;

                Configurations.Add(config);
            }
            if (Configurations.Count == 0)
                servlet.Log.Error("Warning: No valid app configurations found");
        }

        public Session Create(Dictionary<string, string> capabilities, Dictionary<string, object> sessionSetupInfo) {
            if (!capabilities.ContainsKey("applicationName") || capabilities["applicationName"]==null)
                throw new ArgumentException("Capabilities must specify applicationName");

            foreach (Configuration config in Configurations) {
                bool valid = true;
                foreach(string prop in capabilities.Keys)
                    if (capabilities[prop] != null && (!config.properties.ContainsKey(prop) || config.properties[prop] != capabilities[prop])) {
                        valid = false;
                        break;
                    }

                if (valid) {
                    Session session = new Session(this, config, sessionSetupInfo);
                    sessions[session.Guid] = session;
                    return session;
                }
            }
            throw new ArgumentException("Couldn't find any configuration matching the given capabilities " + JSON.ToString(capabilities));
        }

        public void Delete(Session session) {
            if (sessions.Remove(session.Guid))
                session.Dispose();
            else
                throw new ArgumentException("Specified session " + session.Guid + " doesn't exist");
        }
    }
}
