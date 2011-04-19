// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using Twin.Generic;
using Twin.Model;

namespace Twin.Proxy {
    class PersistedObject {
        public static PersistedObject<T> Get<T>(T target, Session session) where T : class {
            if (target == null)
                return null;
            return new PersistedObject<T>(session, target);
        }
        public static PersistedObject<T> GetNoCreate<T>(T target, Session session) where T : class {
            if (target == null)
                return null;
            return new PersistedObject<T>(session, target, false);
        }
    }

    class PersistedObject<T> : IJSONable, IDisposable where T : class {
        internal Session Session;
        internal T Target { get { return (T)Session[Id]; } }
        internal Guid Id;

        public PersistedObject(Session session, T target) : this(session, target, true) {
        }
        public PersistedObject(Session session, T target, bool create) {
            Session = session;
            Id = session.Persist(target, create);
        }

        public object JSONForm {
            get {
                if(Target == null)
                    return null;

                Dictionary<string, object> map = new Dictionary<string, object>();
                map["uuid"] = Id.ToString();
                map["hCode"] = Target.GetHashCode();
                map["class"] = Target.GetType().FullName;

                if (Target is IJSONProperties)
                    ((IJSONProperties)Target).AddExtraJSONProperties(map);

                return map;
            }
        }

        // TODO: this is not called anywhere. 
        // We could have some finalisation magic on the client side that disposes unused objects in batches

        public void Dispose() {
            Session.Release(Target);
        }
    }
}
