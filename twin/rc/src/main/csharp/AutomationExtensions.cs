// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Windows.Automation;

namespace Twin
{
	/// <summary>
	/// UIAutomation provides two mechanisms for searching the AutomationElement tree.
	/// TreeWalkers provide operations to get the parent, first child, next sibling etc of an element.
	/// The Find methods (AutomationElement.FindFirst and AutomationElement.FindAll) allow searching of scoped sections of a tree.
	/// 
	/// Both provide facilities for accessing a filtered view of the tree:
	///  - Raw View is unfiltered, 
	///  - Control View is somewhat filtered,
	///  - Content View is heavily filtered
	/// 
	/// To provide maximum power to the automator, we expose Raw View (and may expose this as an option later).
	/// Unfortunately, while Find methods are more convenient for our purposes, they always operate on a filtered tree 
	/// that looks a lot like the Control View. This is explicitly contradicted by the documentation.
	/// 
	/// Therefore, this class provides a reimplementation that behaves correctly (albeit probably slower).
	/// The methods are static rather than AutomationElement extensions to keep the project compatible with C# 2.0.
	/// </summary>
	public static class AutomationExtensions
	{
		/// <summary>
		/// A callback invoked on every node traversed.
		/// <param name="parent">the parent of the element being visited</param> 
		/// <param name="element">the element being visited</param> 
		/// <param name="visitChildren">set to true if all the children of element should be visited</param>
		/// <param name="abort">set to true if no more elements should be visited</param>
		/// </summary>
		delegate void Visitor(AutomationElement parent, AutomationElement element, out bool visitChildren, out bool abort);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="root"></param>
		/// <param name="visitor"></param>
		private static void Traverse(AutomationElement root, Visitor visitor) {
			Queue<KeyValuePair<AutomationElement,AutomationElement>> searchQueue = new Queue<KeyValuePair<AutomationElement,AutomationElement>>();
			searchQueue.Enqueue(new KeyValuePair<AutomationElement, AutomationElement>(null, root));
			bool visitChildren, abort;
			while(searchQueue.Count > 0) {
				KeyValuePair<AutomationElement,AutomationElement> nextElement = searchQueue.Dequeue();
				visitor.Invoke(nextElement.Key, nextElement.Value, out visitChildren, out abort);
				if(abort)
					break;
				if(visitChildren) {
					AutomationElement child = TreeWalker.RawViewWalker.GetFirstChild(nextElement.Value);
					while(child != null) {
						searchQueue.Enqueue(new KeyValuePair<AutomationElement,AutomationElement>(nextElement.Value, child));
						child = TreeWalker.RawViewWalker.GetNextSibling(child);
					}
				}
			}
		}
		
		private static void ScopeDecision(TreeScope scope, AutomationElement root, AutomationElement parent, AutomationElement element, out bool considerNode, out bool visitChildren) {
         	switch(scope) {
				case TreeScope.Subtree:
					considerNode = true;
					visitChildren = true;
					break;
         		case TreeScope.Children:
         			considerNode = (parent == root);
         			visitChildren = (parent == null);
         			break;
         		case TreeScope.Descendants:
         			considerNode = (parent != null);
         			visitChildren = true;
         			break;
         		case TreeScope.Element:
         			considerNode = (parent == null);
         			visitChildren = false;
         			break;
         		default:
         			throw new Exception("TreeScope.Ancestors and TreeScope.Parent not supported");
         	}			
		}
		
		private static bool Matches(AutomationElement element, Condition condition) {
//			return element.FindFirst(TreeScope.Element, condition) != null; // TODO does this suffer the same bug?
			if(condition == Condition.TrueCondition)
				return true;
			if(condition == Condition.FalseCondition)
				return false;
			if(condition is NotCondition)
				return !Matches(element, ((NotCondition)condition).Condition);
			if(condition is AndCondition) {
				foreach(Condition c in ((AndCondition)condition).GetConditions())
					if(!Matches(element, c))
						return false;
				return true;
			}
			if(condition is OrCondition) {
				foreach(Condition c in ((OrCondition)condition).GetConditions())
					if(Matches(element, c))
						return true;
				return false;				
			}
			if(condition is PropertyCondition) {
				if(!(condition is PropertyCondition2))
					throw new Exception("Please use PropertyCondition2 instead of PropertyCondition. PropertyCondition does not properly expose its value.");
				PropertyCondition2 pc = (PropertyCondition2)condition;
				object actualValue = element.GetCurrentPropertyValue(pc.Property);
				object desiredValue = pc.RealValue;
				if((pc.Flags & PropertyConditionFlags.IgnoreCase) == PropertyConditionFlags.IgnoreCase && 
				   (actualValue is string) && (desiredValue is string))
					return ((string)actualValue).Equals((string)desiredValue, StringComparison.InvariantCultureIgnoreCase);
				return Equals(actualValue,desiredValue);
			}
			throw new Exception("Unsupported condition type "+condition);
		}

		
		public static List<AutomationElement> FindAllRaw(AutomationElement root, TreeScope scope, Condition condition) {
			if(scope == TreeScope.Ancestors || scope == TreeScope.Parent)
      			throw new Exception("TreeScope.Ancestors and TreeScope.Parent not supported");
			
			List<AutomationElement> results = new List<AutomationElement>();
			Traverse(root,
			         delegate(AutomationElement parent, AutomationElement element, out bool visitChildren, out bool abort) {
			         	bool considerNode;
			         	ScopeDecision(scope, root, parent, element, out considerNode, out visitChildren);			         	
			         	if(considerNode && Matches(element, condition))
			         		results.Add(element);			         	
			         	abort = false;
			         }
			);
			return results;
		}
		
		public static AutomationElement FindFirstRaw(AutomationElement root, TreeScope scope, Condition condition) {
			AutomationElement result = null;
			Traverse(root, 
			         delegate(AutomationElement parent, AutomationElement element, out bool visitChildren, out bool abort) {
			         	bool considerNode;
			         	ScopeDecision(scope, root, parent, element, out considerNode, out visitChildren);			         	
			         	if(considerNode && Matches(element, condition)) {
			         		result = element;
			         		abort = true;
			         	} else {
				         	abort = false;
			         	}
			         }
			);
			return result;
		}		
	}
	
	public class PropertyCondition2 : PropertyCondition {
		public PropertyCondition2(AutomationProperty property, object value) : base(property, value) {
			realValue = value;
		}
		public PropertyCondition2(AutomationProperty property, object value, PropertyConditionFlags flags) : base(property, value, flags) {
           	realValue = value;
        }
		
		private object realValue;
		public object RealValue {
			get {
				return realValue;
			}
		}
		
		public override String ToString() {
			return "PropertyCondition2("+this.Property.ProgrammaticName+"="+this.RealValue+")";
		}
	}
}
