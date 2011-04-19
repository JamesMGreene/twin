// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Twin.SharpClaws;
using Twin.SharpClaws.API;
using Twin.Logging;

namespace Twin.SharpClaws.UI {
    class Program {
        private static Server server;
        private static bool stopSignaled = false;
        private static Logger logger;

        private static void SignalExit(object sender, ConsoleCancelEventArgs args) {
            if (stopSignaled) {
                server.Log.Error("Recieved second exit signal, terminating");
            } else {
                server.Stop();
                stopSignaled = true;
                args.Cancel = true;
            }
        }
        
        private static List<IPAddress> GetAddresses() {
        	List<IPAddress> addresses = new List<IPAddress>(Dns.GetHostAddresses(Dns.GetHostName()));
        	addresses.Add(IPAddress.Loopback);
        	addresses.Add(IPAddress.IPv6Loopback);
        	addresses.Add(IPAddress.Any);
        	addresses.Add(IPAddress.IPv6Any);
        	return addresses;
        }
        
        private static IPAddress GetAddress(string spec) {
        	List<IPAddress> addresses = GetAddresses();
        	foreach(IPAddress address in addresses)
            	if(addressIs(address, spec))
            		return address;
        	foreach(IPAddress address in addresses)
            	if(addressHasPrefix(address, spec))
            		return address;
			return null;
        }
        
        private static bool addressHasPrefix(IPAddress address, string spec) {
        	string delim;
        	switch(address.AddressFamily) {
        		case AddressFamily.InterNetwork:
        			delim = ".";
        			break;
        		case AddressFamily.InterNetworkV6:
        			delim = ":";
        			break;
        		default:
        			return false;
        	}
        	string addressString = address.ToString();
        	string prefix = addressString.EndsWith(delim) ? addressString : (addressString + delim);
        	return addressString.Equals(spec, StringComparison.InvariantCultureIgnoreCase) ||
        		addressString.StartsWith(spec, StringComparison.InvariantCultureIgnoreCase);
        }
        private static bool addressIs(IPAddress address, string spec) {
        	return address.ToString().Equals(spec, StringComparison.InvariantCultureIgnoreCase);
        }

        public static void Main(string[] args) {

            string root = ".";
            string configFileName = "sharpclaws.xml";

            if(args.Length > 0)
                root = args[0];
            if (args.Length > 1)
                configFileName = args[1];

            string filename = Path.Combine(root, configFileName);
            if(args.Length > 2 || !File.Exists(filename)) {
                Console.Error.WriteLine("Configuration file {0} not found", filename);
                Console.Error.WriteLine("Usage: {0} [root] [config-file]", Environment.CommandLine);
                Console.Error.WriteLine("    root: the directory to run in.             default: .");
                Console.Error.WriteLine("    config-file: the xml configuration file.   default: sharpclaws.xml");
                Environment.Exit(1);
            }

            XmlDocument config = new XmlDocument();
            try {
                config.Load(filename);
            } catch (XmlException e) {
                Console.Error.WriteLine("Configuration file {0} has an invalid format:", filename);
                Console.Error.WriteLine(e);
                Environment.Exit(1);
            }

            XPathNavigator nav = config.CreateNavigator().SelectSingleNode("/server");
            if (nav == null) {
                Console.Error.WriteLine("Configuration file {0} has an invalid format: root <server> tag not found", filename);
                Environment.Exit(1);
            }

            LogLevel logLevel = LogLevel.Info;
   
            string logLevelAttribute = nav.GetAttribute("log-level", string.Empty);
            if (logLevelAttribute != string.Empty) {
                try {
                    logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), logLevelAttribute, true);
                } catch (ArgumentException) {
                    Console.Error.WriteLine("Invalid log-level {0}", logLevelAttribute);
                }
            }

            TextWriter logFileWriter = Console.Out;
            string logFileAttribute = nav.GetAttribute("log", string.Empty);
            if (logFileAttribute != string.Empty) {
                string logFileName = Path.Combine(root, logFileAttribute);
                StreamWriter writer = new StreamWriter(new FileStream(logFileName, FileMode.Append));
                writer.AutoFlush = true;
                logFileWriter = writer;
            }

            logger = new TextWriterLogger(logLevel, logFileWriter);

            int port = 80;
            IPAddress ip = IPAddress.Any;

            string portAttribute = nav.GetAttribute("port", string.Empty);
            string ipAttribute = nav.GetAttribute("interface", string.Empty);
            if (portAttribute != string.Empty)
                port = Convert.ToInt32(portAttribute);
            if (ipAttribute != string.Empty) {
            	ip = GetAddress(ipAttribute);
            	if(ip == null) {
            		logger.Error("interface={0} but no local address has this prefix",ipAttribute);
            		Environment.Exit(1);
            	}            		
			}

            server = new Server(ip, port);
            server.Log = logger;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(SignalExit);

            XPathNodeIterator iterator = nav.SelectChildren("servlet", string.Empty);
            while (iterator.MoveNext()) {
                string context = iterator.Current.GetAttribute("context", string.Empty); // returns empty on missing --> "/"
                if(!context.StartsWith("/"))
                    context = "/" + context;
                if(!context.EndsWith("/"))
                    context = context + "/";

                string className = iterator.Current.GetAttribute("class", string.Empty);
                string assemblyName = iterator.Current.GetAttribute("assembly", string.Empty);
                Dictionary<string, string> servletConfig = new Dictionary<string, string>();
                XPathNodeIterator paramsIterator = iterator.Current.SelectChildren("param", string.Empty);
                while (paramsIterator.MoveNext()) {
                    string name = paramsIterator.Current.GetAttribute("name", string.Empty);
                    if (name == null) {
                        logger.Error("<param> must have a name given.");
                        Environment.Exit(1);
                    }
                    servletConfig.Add(name, paramsIterator.Current.Value);
                }

                if (className == string.Empty) {
                    logger.Error("Servlet mapped to {0} has no class specified", context);
                    Environment.Exit(1);
                } 
                if(assemblyName == string.Empty) {
                    logger.Error("Servlet mapped to {0} has no assembly specified", context);
                    Environment.Exit(1);
                }
                try {
                    Assembly assembly = Assembly.LoadFile(Path.GetFullPath(Path.Combine(root, assemblyName)));
                    Type type = assembly.GetType(className, true, true);
                    if (!type.IsSubclassOf(typeof(Servlet)))
                        throw new Exception(string.Format("Type {0} is not a subclass of {1}", type, typeof(Servlet)));
                    ConstructorInfo constructor = type.GetConstructor(new Type[0]);
                    Servlet servlet = (Servlet)constructor.Invoke(null);
                    servlet.Configuration = servletConfig;
                    server[context] = servlet;
                } catch (Exception e) {
                    logger.Error("Couldn't instantiate class {0} from assembly {1}", className, assemblyName);
                    logger.Error(e);
                    Environment.Exit(1);
                }
            }

            try {
                server.Run();
            } catch (Exception e) {
                logger.Error("Server exception");
                logger.Error(e);
                Environment.Exit(2);
            }
        }
    }
}
