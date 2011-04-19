// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Twin.Logging {
    public class TextWriterLogger : Logger {
        TextWriter writer;
        LogLevel level;
        public TextWriterLogger(LogLevel level, TextWriter writer) {
            this.level = level;
            this.writer = writer;
        }
        public override void Log(LogLevel level, Exception e) {
            Log(level, "{0}", e);
        }
        public override void Log(LogLevel level, string message, params object[] details) {
            if (level < this.level)
                return;

            string levelStr = level.ToString().ToUpper();
            string timestampStr = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string messageStr = string.Format(message, details);

            writer.WriteLine("[{0}] {1}: {2}", levelStr, timestampStr, messageStr);
        }
    }
}
