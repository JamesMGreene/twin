// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Twin.SharpClaws.API;
using Twin.Generic;
using Twin.Logging;

namespace Twin.Model {
    class Session : IDisposable,IJSONable {
        internal Guid Guid = Guid.NewGuid();
        internal Configuration Configuration;
        internal Process Process;
        internal SessionFactory Factory;
        internal Dictionary<Guid, object> persistentObjects = new Dictionary<Guid, object>();
        internal Dictionary<Guid, int> retainCount = new Dictionary<Guid, int>();
        internal Dictionary<object, Guid> objectKey = new Dictionary<object, Guid>();
        internal List<SessionSetup> sessionSetups = new List<SessionSetup>();

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Guid Persist(object o, bool create) {
            Guid key;
            if (objectKey.TryGetValue(o, out key)) {
                if(create)
                    retainCount[key]++;
                return key;
            } else {
                if (!create)
                    throw new Exception("Object not found and create=false: " + o);
                key = Guid.NewGuid();
                persistentObjects[key] = o;
                retainCount[key] = 1;
                objectKey[o] = key;
                return key;
            }
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Release(object o) {
            Guid key = objectKey[o];
            if (--retainCount[key] == 0) {
                persistentObjects.Remove(key);
                retainCount.Remove(key);
                objectKey.Remove(o);
                if (o is IDisposable)
                    ((IDisposable)o).Dispose();
            }
        }

        public object this[Guid id] {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get {
                if(persistentObjects.ContainsKey(id))
                    return persistentObjects[id];
                return null;
            }
        }

        private Process Launch(LaunchStrategy strategy) {
            Logger.Current.Trace("Launching configuration {0} with strategy {1}", Configuration.identifier, strategy);
            switch (strategy) {
                case LaunchStrategy.AttachOrLaunch:
                    try {
                        return Launch(LaunchStrategy.Attach);
                    } catch (Exception e) {
                        Logger.Current.Trace(e);
                        try {
                            return Launch(LaunchStrategy.Launch);
                        } catch (Exception ex) {
                            throw ex;
                        }
                    }
                case LaunchStrategy.Launch:
                    Logger.Current.Trace("Launching path={0} args={1}", Configuration.appPath, Configuration.arguments);
                    this.LaunchStrategy = LaunchStrategy.Launch;
                    Process process = Process.Start(Configuration.appPath, Configuration.arguments);
                    Factory.LockProcess(process);
                    return process;
                case LaunchStrategy.Attach:
                    FileRef runningApp = new FileRef(Configuration.appPath);
                    foreach (Process candidate in Process.GetProcesses()) {
                        if (candidate.Id == 0) // idle process
                            continue;
                        try {
                            if (runningApp.Equals(new FileRef(candidate)) && Factory.TryLockProcess(candidate)) {
                                Logger.Current.Info("Attaching to process {0} for configuration {1}: {2}", candidate.Id, Configuration.identifier, candidate.MainModule.FileName);
                                this.LaunchStrategy = LaunchStrategy.Attach;
                                return candidate;
                            }
                        } catch (Exception) {
                            // couldn't read the process, e.g. svchost
                        }
                    }
                    Logger.Current.Info("Couldn't find a process to attach to for configuration {0} with path {1}", Configuration.identifier, Configuration.appPath);
                    throw new Exception("Could not find any process with path " + Configuration.appPath + " to bind to");
            }
            throw new Exception("Unknown LaunchStrategy " + strategy);
        }

        public Session(SessionFactory factory, Configuration config, Dictionary<string,object> sessionSetupInfo) {
            Factory = factory;
            Configuration = config;
            if (config.Capabilities.ContainsKey("sessionSetup.files") && Convert.ToBoolean(config.Capabilities["sessionSetup.files"]))
                sessionSetups.Add(new FileSessionSetup(config, sessionSetupInfo));

            foreach (SessionSetup setup in sessionSetups)
                setup.Before(this);

            try {
                Process = Launch(config.LaunchStrategy);
            } catch (Exception e) {
                foreach (SessionSetup setup in sessionSetups)
                    setup.After(this);
                throw e;
            }
        }
        internal LaunchStrategy LaunchStrategy = LaunchStrategy.Launch;
        public Dictionary<string, string> Capabilities {
            get {
                Dictionary<string, string> merged = new Dictionary<string, string>(Configuration.Capabilities);
                merged["launch-strategy"] = LaunchStrategy.ToString();
                return merged;
            }
        }
        public object JSONForm {
            get { return Capabilities; }
        }
        public override String ToString() {
            return Guid.ToString();
        }

        public void Dispose() {
            if (LaunchStrategy == LaunchStrategy.Launch && !Process.HasExited)
                Process.Kill();
            Factory.UnlockProcess(Process);
            Process.Dispose();
            foreach (object o in persistentObjects.Values)
                if (o is IDisposable)
                    ((IDisposable)o).Dispose();
            persistentObjects = null;
            foreach (SessionSetup setup in sessionSetups)
                setup.After(this);
        }
    }
}
