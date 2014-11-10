// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace Twin.Model {
    class Configuration {
        internal string appPath;
        internal string arguments = "";
        internal string identifier;
        internal Dictionary<string, string> properties = new Dictionary<string,string>();

        public Configuration(string identifier, string appPath) {
            this.appPath = appPath;
            this.identifier = identifier;

            FileVersionInfo info = FileVersionInfo.GetVersionInfo(appPath);
            if(info.FileMajorPart != 0 || info.FileMinorPart != 0 || info.FileBuildPart != 0 || info.FilePrivatePart != 0)
                properties["version"] = info.FileMajorPart + "." + info.FileMinorPart + "." + info.FileBuildPart + "." + info.FilePrivatePart;
            if (info.Language != null)
                properties["language"] = info.Language;
        }

        public Dictionary<string, string> Capabilities {
            get {
                return properties;
            }
        }

        public LaunchStrategy LaunchStrategy {
            get {
                if (Capabilities.ContainsKey("launch-strategy")) {
                    FieldInfo enumValue = typeof(LaunchStrategy).GetField(Capabilities["launch-strategy"], BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                    if(enumValue != null)
                        return (LaunchStrategy)enumValue.GetValue(null);
                }
                return LaunchStrategy.Launch;
            }
        }
    }

    enum LaunchStrategy {
        Launch,
        Attach,
        AttachOrLaunch
    }
}
