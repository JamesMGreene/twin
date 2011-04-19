// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Automation;
using System.Xml;

using Twin.Logging;
using Twin.Generic;
using Twin.Model;

namespace Twin.View {
    class Dump {
        public static object GetStructure(ElementRequest request) {
            bool verbose = request.Body != null && request.Body.ContainsKey("verbose") && (bool)request.Body["verbose"];

            XmlDocument doc = new XmlDocument();
            STAHelper.Invoke(
            	delegate() {
		            Add(doc, doc, request.Target, verbose);
            	}
            );

            StringWriter writer = new StringWriter();
            XmlTextWriter xmlWriter = new XmlTextWriter(writer);
            xmlWriter.Formatting = Formatting.Indented;
            xmlWriter.Indentation = 2;
            doc.WriteTo(xmlWriter);

            return writer.ToString();
        }

        private static void Add(XmlDocument doc, XmlNode context, Element target, bool verbose) {
            XmlElement elt = doc.CreateElement(target.ControlTypeName);

            if (!target.Enabled)
                elt.SetAttribute("enabled", "false");
            if (target.Id != null)
                elt.SetAttribute("id", target.Id);
            if (target.Name != null)
                elt.SetAttribute("name", target.Name);
            string value = target.Value;
            if (value != null && value != String.Empty)
                elt.SetAttribute("value", value);
            if (target.Class != null)
                elt.SetAttribute("className", target.Class);

            if (verbose) {
                StringBuilder patterns = new StringBuilder();
                foreach (AutomationPattern pattern in target.AutomationElement.GetSupportedPatterns()) {
                    if (patterns.Length > 0)
                        patterns.Append(" ");
                    string name = pattern.ProgrammaticName;
                    name = name.Replace("PatternIdentifiers.Pattern", "");
                    patterns.Append(name);
                }
                elt.SetAttribute("patterns", patterns.ToString());

                StringBuilder properties = new StringBuilder();
                foreach (AutomationProperty prop in target.AutomationElement.GetSupportedProperties()) {
                    if (properties.Length > 0)
                        properties.Append(" ");
                    string name = prop.ProgrammaticName;
                    name = name.Replace("AutomationElementIdentifiers.", "");
                    name = name.Replace("PatternIdentifiers", "");
                    name = name.Replace("Property", "");
                    properties.Append(name);
                }
                elt.SetAttribute("properties", properties.ToString());
            }
            
            foreach (Element child in target.Children)
                Add(doc, elt, child, verbose);
            context.AppendChild(elt);
        }
    }
}
