// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

using Twin.SharpClaws.API;
using Twin.Generic;
using Twin.Grid;
using Twin.Model;
using Twin.View;

namespace Twin {
    class TwinRC : JasonServlet {
        internal SessionFactory SessionFactory;
        internal GridHub Hub;

        public override void Initialize() {
            base.Initialize();

            SessionFactory = new SessionFactory(this);

            Routes["/"]["GET"] = new ResourceResponder("twin-rc.index.html","text/html; charset=utf-8");
            Routes["/ide"]["GET"] = new ResourceResponder("twin-rc.ide.html","text/html; charset=utf-8");
            Routes["/ide.jar"]["GET"] = new ResourceResponder("twin-rc.twin-ide.jar","application/x-java-archive");
            Routes["/status"]["GET"] = new JSONHandler(Sessions.Status);

            Routes["/session"]["POST"] = new JSONHandler(Sessions.Create);
            Routes["/session/:session"]["GET"] = new JSONHandler(Sessions.GetCapabilities);
            Routes["/session/:session"]["DELETE"] = new JSONHandler(Sessions.Delete);

            Routes["/session/:session/element/active"]["GET"] = new SessionHandler(Elements.GetFocused);
            Routes["/session/:session/element/active"]["POST"] = new SessionHandler(Elements.SetFocused);

            Routes["/session/:session/attachment"]["POST"] = new SessionHandler(Attachments.Create);
            Routes["/session/:session/attachment/:attachment"]["GET"] = new SessionHandler(Attachments.Get);
            Routes["/session/:session/attachment/:attachment"]["POST"] = new SessionHandler(Attachments.Update);
            Routes["/session/:session/attachment/:attachment"]["DELETE"] = new SessionHandler(Attachments.Delete);

            Routes["/session/:session/clipboard"]["POST"] = new SessionHandler(Clipboards.SetContent);
            Routes["/session/:session/clipboard"]["GET"] = new SessionHandler(Clipboards.GetContent);
            Routes["/session/:session/clipboard"]["DELETE"] = new SessionHandler(Clipboards.Clear);

            Element("/session/:session/element/:target");
            Desktop("/session/:session/desktop");

            if (Configuration.ContainsKey("grid.hub")) {
                Uri uri = new Uri(Configuration["grid.hub"]);
                Dictionary<string, string> hubConfiguration = new Dictionary<string, string>();
                string configPrefix = "grid.configuration.";
                foreach (string key in Configuration.Keys) {
                    if (!key.StartsWith(configPrefix))
                        continue;
                    string shortKey = key.Substring(configPrefix.Length);
                    hubConfiguration[shortKey] = Configuration[key];
                }

                Hub = new GridHub(this, uri, hubConfiguration, Log);
                Hub.Connect();
            }
        }

        public override void Destroy() {
            if (Hub != null && Hub.Connected) {
                Hub.Disconnect();
            }

            base.Destroy();
        }

        private void Element(string path) {
        	Routes[path]["GET"] = new ElementHandler(Elements.Get);
        	Routes[path]["DELETE"] = new ElementHandler(Elements.Close);
            Routes[path + "/structure"]["GET"] = new ElementHandler(Dump.GetStructure);
            Routes[path + "/enabled"]["GET"] = new ElementHandler(Elements.GetEnabled);
            Routes[path + "/expanded"]["GET"] = new ElementHandler(Elements.GetExpanded);
            Routes[path + "/expanded"]["POST"] = new ElementHandler(Elements.SetExpanded);
            Routes[path + "/selected"]["GET"] = new ElementHandler(Elements.GetSelected);
            Routes[path + "/selected"]["POST"] = new ElementHandler(Elements.SetSelected);
            Routes[path + "/value"]["GET"] = new ElementHandler(Elements.GetValue);
            Routes[path + "/value"]["POST"] = new ElementHandler(Elements.SetValue);
            Routes[path + "/value"]["OPTIONS"] = new ElementHandler(Elements.GetValueOptions);
            Routes[path + "/size"]["POST"] = new ElementHandler(Elements.SetSize);
            Routes[path + "/location"]["POST"] = new ElementHandler(Elements.SetLocation);
            Routes[path + "/bounds"]["GET"] = new ElementHandler(Elements.GetBounds);
            Routes[path + "/bounds"]["POST"] = new ElementHandler(Elements.SetBounds);
            Routes[path + "/axis/:axis"]["POST"] = new ElementHandler(Elements.Scroll);
            Routes[path + "/axis/:axis"]["GET"] = new ElementHandler(Elements.GetScrollPosition);
            Routes[path + "/axisX"]["GET"] = new ElementHandler(Elements.GetScrollerX);
            Routes[path + "/axisY"]["GET"] = new ElementHandler(Elements.GetScrollerY);
            Routes[path + "/screenshot"]["GET"] = new ElementHandler(Elements.GetScreenshot);
            Routes[path + "/click"]["POST"] = new ElementHandler(Elements.Click);
            Routes[path + "/name"]["GET"] = new ElementHandler(Elements.GetName);
            Routes[path + "/keyboard"]["POST"] = new ElementHandler(Elements.SendKeys);
            Routes[path + "/parent"]["GET"] = new ElementHandler(Elements.GetParent);
            Routes[path + "/children"]["GET"] = new ElementHandler(Search.FindChildren);
            Routes[path + "/descendants"]["GET"] = new ElementHandler(Search.FindDescendants);
            Routes[path + "/exists"]["GET"] = new ElementResponder(new ElementHandler(Elements.GetExists), true); // don't throw if element doesn't exist
            Routes[path + "/exists"]["POST"] = new ElementResponder(new ElementHandler(Elements.PollExists), true); // don't throw if element doesn't exist
            Routes[path + "/toggle"]["GET"] = new ElementHandler(Elements.GetToggleState);
            Routes[path + "/toggle"]["POST"] = new ElementHandler(Elements.SetToggleState);
            Routes[path + "/window-state"]["GET"] = new ElementHandler(Elements.GetWindowState);
            Routes[path + "/window-state"]["POST"] = new ElementHandler(Elements.SetWindowState);
            Routes[path + "/selection-container"]["GET"] = new ElementHandler(Elements.GetSelectionContainer);
            Routes[path + "/selection"]["GET"] = new ElementHandler(Elements.GetSelection);
        }
        private void Desktop(string path) {
        	Routes[path]["GET"] = (DesktopResponder)new ElementHandler(Elements.Get);
        	Routes[path + "/structure"]["GET"] = (DesktopResponder)new ElementHandler(Dump.GetStructure);
        	Routes[path + "/bounds"]["GET"] = (DesktopResponder)new ElementHandler(Elements.GetBounds);
        	Routes[path + "/screenshot"]["GET"] = (DesktopResponder)new ElementHandler(Elements.GetScreenshot);
        	Routes[path + "/click"]["POST"] = (DesktopResponder)new ElementHandler(Elements.Click);
        	Routes[path + "/keyboard"]["POST"] = (DesktopResponder)new ElementHandler(Elements.SendKeys);
        	Routes[path + "/children"]["GET"] = (DesktopResponder)new ElementHandler(Search.FindChildren);
        	Routes[path + "/descendants"]["GET"] = (DesktopResponder)new ElementHandler(Search.FindDescendants);
        }

        internal Uri ExternalUri {
            get {
        		IPAddress ip = GetExternalIPAddress();
        		string host = (ip.AddressFamily == AddressFamily.InterNetworkV6) ? ("["+ip+"]") : ip.ToString();
        		return new UriBuilder("http", host, IPEndPoint.Port, ContextPath).Uri;
            }
        }
        // return the server-configured one, if it's wildcard, find the first non-loopback address
        internal IPAddress GetExternalIPAddress() {
            IPAddress address = IPEndPoint.Address;
            if (address != IPAddress.Any)
                return address;
            
            IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
            // prefer external ip4 address
            foreach (IPAddress localAddress in addresses) {
            	if(localAddress.AddressFamily != AddressFamily.InterNetwork)
            		continue;
            	if(IPAddress.IsLoopback(localAddress))
                    continue;
                return localAddress;
            }   
            // else external ip6 address
            foreach (IPAddress localAddress in addresses) {
            	if(localAddress.AddressFamily != AddressFamily.InterNetworkV6)
            		continue;
            	if(localAddress.IsIPv6LinkLocal)
            		continue; // skip link-local addresses since we want to be routable
            	if(IPAddress.IsLoopback(localAddress))
                    continue;
                return localAddress;
            }
            return IPAddress.Loopback;
        }
    }
}
