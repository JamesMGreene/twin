// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Twin.Logging {
    public enum LogLevel {
        Trace,
        Info,
        Error,
    }

    public abstract class Logger {
        private static NullLogger nullLogger = new NullLogger();
        [ThreadStatic]
        private static List<Logger> current; // cannot initialise threadstatic variables here
        /// <summary>
        /// Stack of loggers for the current thread
        /// </summary>
        public static Logger Current {
            get {
                if (current == null)
                    current = new List<Logger>();
                if (current.Count == 0)
                    return nullLogger;
                return current[current.Count - 1];
            }
            set {
                if (current == null)
                    current = new List<Logger>();
                if (value == null) {
                    if (current.Count == 0)
                        throw new Exception("Logger not set, cannot remove");
                    current.RemoveAt(current.Count - 1);
                } else {
                    current.Add(value);
                }
            }
        }

        public abstract void Log(LogLevel level, string message, params object[] details);
        public abstract void Log(LogLevel level, Exception e);

        public void Trace(string message, params object[] details) {
            Log(LogLevel.Trace, message, details);
        }
        public void Info(string message, params object[] details) {
            Log(LogLevel.Info, message, details);
        }
        public void Error(string message, params object[] details) {
            Log(LogLevel.Error, message, details);
        }

        public void Trace(Exception e) {
            Log(LogLevel.Trace, e);
        }
        public void Info(Exception e) {
            Log(LogLevel.Info, e);
        }
        public void Error(Exception e) {
            Log(LogLevel.Error, e);
        }
    }
	
	public class NullLogger : Logger {
        public override void Log(LogLevel level, Exception e) { }
        public override void Log(LogLevel level, string message, params object[] details) { }
    }
}
