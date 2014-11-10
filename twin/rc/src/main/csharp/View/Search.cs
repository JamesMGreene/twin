// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows.Automation;

using Twin.Logging;
using Twin.Generic;
using Twin.Model;
using Twin.Proxy;

namespace Twin.View {
    class Search {
        public static object FindChildren(ElementRequest request) {
            return FindAll(request.Target.AutomationElement, TreeScope.Children, ParseCount(request.Body), ParseResultsTimeout(request.Body), ParseCondition(request.Body), request.Session);
        }
        public static object FindDescendants(ElementRequest request) {
            return FindAll(request.Target.AutomationElement, TreeScope.Descendants, ParseCount(request.Body), ParseResultsTimeout(request.Body), ParseCondition(request.Body), request.Session);
        }
        private static int ParseCount(Dictionary<string, object> body) {
            if (body == null || !body.ContainsKey("count"))
                return 0;
            return Convert.ToInt32(body["count"]);
        }
        private static Condition ParseCondition(Dictionary<string, object> body) {
            if (body == null || !body.ContainsKey("criteria"))
                return null;

            return ParseCriteria((Dictionary<string, object>)body["criteria"]);
        }
        private static Condition ParseCriteria(Dictionary<string, object> body) {
            if(!body.ContainsKey("type"))
                throw new ArgumentException("Body should contain key 'type' describing type of condition");
            switch ((string)body["type"]) {
                case "and":
                    return new AndCondition(ParseCriteria((List<object>)body["target"]));
                case "or":
                    return new OrCondition(ParseCriteria((List<object>)body["target"]));
                case "not":
                    return new NotCondition(ParseCriteria((Dictionary<string,object>)body["target"]));
                case "property":
                    return ParseProperty((string)body["name"], body["value"]);
            }
            throw new ArgumentOutOfRangeException("body[\"type\"]", body["type"], "Unrecognised condition type");
        }
        private static Condition[] ParseCriteria(IList<object> list) {
            Condition[] results = new Condition[list.Count];
            for (int i = 0; i < list.Count; i++)
                results[i] = ParseCriteria((Dictionary<string, object>)list[i]);
            return results;
        }
        private static Condition ParseProperty(string name, object value) {
            switch (name.ToLowerInvariant()) {
                case "name":
                    return new PropertyCondition2(AutomationElement.NameProperty, (string)value);
                case "controltype":
                    return new PropertyCondition2(AutomationElement.ControlTypeProperty, NameMappings.GetType((string)value));
                case "controlpattern":
                    return new PropertyCondition2(NameMappings.GetPatternPresenceProperty(NameMappings.GetPattern((string)value)), true);
                case "id":
                    return new PropertyCondition2(AutomationElement.AutomationIdProperty, (string)value);
                case "classname":
                    return new PropertyCondition2(AutomationElement.ClassNameProperty, (string)value);
                case "enabled":
                    return new PropertyCondition2(AutomationElement.IsEnabledProperty, (bool)value);
                case "value":
                    return new PropertyCondition2(ValuePattern.ValueProperty, (string)value);
                default:
                    throw new ArgumentOutOfRangeException("name", name, "Unknown condition");
            }
        }
        private static double ParseResultsTimeout(Dictionary<string, object> body) {
            if (body != null && body.ContainsKey("waitForResults")) {
                if(body["waitForResults"] is bool) {
                    return (bool)body["waitForResults"] ? double.PositiveInfinity : 0.0;
                }
                return Convert.ToDouble(body["waitForResults"]);
            }
            return 0.0;
        }

        const double PollInterval = 1.0;
        private static List<PersistedObject<Element>> FindAll(AutomationElement root, TreeScope scope, int count, double waitForResults, Condition condition, Session session) {
            Logger.Current.Trace("Searching for {1} with a timeout of {0} sec", waitForResults, condition);
            List<PersistedObject<Element>> results = null;
            double lastDuration = Double.NaN;
            do {
                if (lastDuration < PollInterval) {
                    double sleepDuration = Math.Min(waitForResults, PollInterval - lastDuration);
                    int millis = (int)(sleepDuration * 1000);
                    if (millis > 0)
                        System.Threading.Thread.Sleep(millis);
                    waitForResults -= sleepDuration;
                }

                long startTicks = DateTime.Now.Ticks;
                results = Wrap(FindAll(root, scope, count, condition, session.Process.Id), session);
                long endTicks = DateTime.Now.Ticks;
                lastDuration = (endTicks - startTicks) / 10000000.0;
                waitForResults -= lastDuration;
                Logger.Current.Trace("{0} sec left", waitForResults);
            } while (waitForResults > 0 && results.Count == 0);
            Logger.Current.Trace("Returning {0} results", results.Count);
            return results;
        }
        // threadsafe
        // we may return more than count results - we include complete levels
        // e.g searching for 'B' from 'A' in
        //   A
        //  / \
        // B   C
        //    / \
        //   B   B
        // will yield 1 results with a count of 1
        // will yield 3 results with a count of 2
        internal static List<AutomationElement> FindAll(AutomationElement root, TreeScope scope, int count, Condition condition, int processId) {
            if (count <= 0)
                return FindAll(root, scope, condition, processId);

            // Breadth first search, so we find the result closest to the root
            List<AutomationElement> results = new List<AutomationElement>();
            LinkedList<AutomationElement> queue = new LinkedList<AutomationElement>();
            queue.AddLast(root);
            int levelSize = 1;

            while (queue.Count > 0) {
                AutomationElement current = queue.First.Value;
                queue.RemoveFirst();
                levelSize--;

                bool includeElement=false;
                bool includeChildren=false;
                bool includeGrandchildren = false;

                switch (scope) {
                    case TreeScope.Ancestors:
                    case TreeScope.Parent:
                        throw new NotImplementedException();
                    case TreeScope.Element:
                        includeElement = true;
                        break;
                    case TreeScope.Children:
                        includeChildren = true;
                        break;
                    case TreeScope.Subtree:
                        includeElement = true;
                        includeChildren = true;
                        includeGrandchildren = true;
                        break;
                    case TreeScope.Descendants:
                        includeChildren = true;
                        includeGrandchildren = true;
                        break;
                }

                STAHelper.Invoke(
                	delegate() {
		                if (includeElement)
    		                FindAndAdd(current, TreeScope.Element, condition, results, processId);
        		        if (includeChildren && results.Count < count)
            		        FindAndAdd(current, TreeScope.Children, condition, results, processId);
                		if (includeGrandchildren && results.Count < count)
                    		FindAndAdd(current, TreeScope.Children, Condition.TrueCondition, queue, processId);
                	}
                );

                if (levelSize == 0) {
                    if (results.Count >= count)
                        break;
                    if(scope == TreeScope.Subtree)
                        scope = TreeScope.Descendants;
                    levelSize = queue.Count;
                }
            }
            return results;
        }

        private static void FindAndAdd(AutomationElement root, TreeScope scope, Condition condition, ICollection<AutomationElement> results, int processId) {
            List<AutomationElement> values = FindAll(root, scope, condition, processId);
            foreach (AutomationElement e in values)
                results.Add(e);
        }

        public static List<AutomationElement> FindAll(AutomationElement root, TreeScope scope, Condition condition, int processId) {
        	return (List<AutomationElement>)STAHelper.Invoke(
        		delegate() {
		            int elementProcessId = (int)root.GetCurrentPropertyValue(AutomationElement.ProcessIdProperty);
		            if (elementProcessId != processId) {
		                // This happens when the element represents the desktop.
		                // We could just filter using the ProcessIdProperty but this searches all nodes and *then* filters, 
		                // which is incredibly slow if we're searching a lot of nodes (i.e. TreeScope is Descendant).
		                // Instead we find all direct children with the right process id and then search them inclusively.
		                // Helpfully, there's a Subtree TreeScope which does what we want
		
		                Condition processCondition = new PropertyCondition2(AutomationElement.ProcessIdProperty, processId);
		                if (scope == TreeScope.Descendants) {
		                    List<AutomationElement> roots = AutomationExtensions.FindAllRaw(root, TreeScope.Children, processCondition);
		                    List<AutomationElement> mergedResults = new List<AutomationElement>();
		                    foreach (AutomationElement currentRoot in roots)
		                        mergedResults.AddRange(FindAll(currentRoot, TreeScope.Subtree, condition, processId));
		                    return mergedResults;
		                } else {
		                    condition = (condition == null) ? processCondition : new AndCondition(condition, processCondition);
		                }
		            }
		            if (condition == null)
		                condition = Condition.TrueCondition;
		
		            return AutomationExtensions.FindAllRaw(root, scope, condition);
        		}
        	);
        }

        private static List<PersistedObject<Element>> Wrap(List<AutomationElement> searchResults, Session session) {
            List<PersistedObject<Element>> result = new List<PersistedObject<Element>>();
            STAHelper.Invoke(
            	delegate() {
	     	       foreach (AutomationElement element in searchResults)
    	    	        result.Add(PersistedObject.Get(Element.Create(element, session.Process.Id), session));
            	}
            );
            return result;
        }
    }
}
