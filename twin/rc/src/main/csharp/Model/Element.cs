// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Automation;
using System.Windows;
using System.Windows.Forms;
using Twin.Proxy;

namespace Twin.Model {
    class Element : IDisposable, IJSONProperties {
        AutomationElement element;
        int processId; // needed for scoping children

        public AutomationElement AutomationElement {
            get {
                return element;
            }
        }

        private object Pattern(AutomationPattern pattern) {
            try {
                return element.GetCurrentPattern(pattern);
            } catch (InvalidOperationException e) {
                ControlType type = (ControlType)element.GetCurrentPropertyValue(AutomationElement.ControlTypeProperty);
                throw new InvalidOperationException(type.ProgrammaticName + " does not support " + pattern.ProgrammaticName, e);
            }
        }

        private TransformPattern TransformPattern {
            get { return (TransformPattern)Pattern(TransformPattern.Pattern); }
        }
        private SelectionItemPattern SelectionItemPattern {
            get { return (SelectionItemPattern)Pattern(SelectionItemPattern.Pattern); }
        }
        private WindowPattern WindowPattern { 
            get { return (WindowPattern)Pattern(WindowPattern.Pattern); }
        }
        private InvokePattern InvokePattern {
            get { return (InvokePattern)Pattern(InvokePattern.Pattern); }
        }
        private ExpandCollapsePattern ExpandCollapsePattern {
            get { return (ExpandCollapsePattern)Pattern(ExpandCollapsePattern.Pattern); }
        }
        private TogglePattern TogglePattern {
            get { return (TogglePattern)Pattern(TogglePattern.Pattern); }
        }
        private ValuePattern ValuePattern {
            get { return (ValuePattern)Pattern(ValuePattern.Pattern); }
        }
        private ScrollPattern ScrollPattern {
            get { return (ScrollPattern)Pattern(ScrollPattern.Pattern); }
        }

        public static Element Create(AutomationElement element, int processId) {
        	AutomationElement.AutomationElementInformation current = element.Current;
            if (current.NativeWindowHandle != 0 && current.ControlType == ControlType.Window)
                return new NativeElement(element, processId);
            return new Element(element, processId);
        }
        protected Element(AutomationElement element, int processId) {
            this.processId = processId;
            this.element = element;
        }
        
        // threadsafe
        private object this[AutomationProperty property] {
        	get {
        		return STAHelper.Invoke(
        			delegate() {
        		    	return element.GetCurrentPropertyValue(property);
        			}
        		);
        	}
        }
        public string Id {
            get { 
        		string id = (string)this[AutomationElement.AutomationIdProperty];
        		return (id.Length == 0) ? null : id;
            }
        }
        public string Class {
            get { 
        		string className = (string)this[AutomationElement.ClassNameProperty];
                return (className.Length == 0) ? null : className;
            }
        }
        public bool Enabled {
            get { 
        		return (bool)this[AutomationElement.IsEnabledProperty]; 
        	}
        }
        public bool Expanded {
            get {
        		ExpandCollapseState state = (ExpandCollapseState)this[ExpandCollapsePattern.ExpandCollapseStateProperty];
                return (state == ExpandCollapseState.Expanded || state == ExpandCollapseState.PartiallyExpanded);
            }
            set {
        		STAHelper.Invoke(
        			delegate() {
		                if (value) {
		                    ExpandCollapseState state = (ExpandCollapseState)element.GetCurrentPropertyValue(ExpandCollapsePattern.ExpandCollapseStateProperty);
		                    if (state == ExpandCollapseState.LeafNode) { // Expand() will throw, but some poorly-coded apps use 'fake' internal nodes - actually leaves with a graphic
		                        this.Focus();
		                        SendKeys.SendWait("{RIGHT}");
		                        state = (ExpandCollapseState)element.GetCurrentPropertyValue(ExpandCollapsePattern.ExpandCollapseStateProperty);
		                        if (state == ExpandCollapseState.LeafNode) {
		                            throw new InvalidOperationException("Cannot expand a leaf node (tried to focus it and type {RIGHT}, but no effect)");
		                        }
		                    } else {
		                        ExpandCollapsePattern.Expand();
		                    }
		                } else {
		                    if (Expanded)
		                        ExpandCollapsePattern.Collapse();
		                }        				
        			}
        		);
            }
        }
        public string Name {
            get {
        		string name = (string)this[AutomationElement.NameProperty];
                return (name.Length == 0) ? null : name;
            }
        }
        public string Value {
            get {
        		return (string)this[ValuePattern.ValueProperty];
            }
            set {
        		STAHelper.Invoke(
        			delegate() {
		                ValuePattern.SetValue(value);
        			}
        		);
            }
        }
        public bool IsValueReadOnly() {
        	return (bool)this[ValuePattern.IsReadOnlyProperty];
        }

        public ControlType ControlType {
            get {
        		return (ControlType)this[AutomationElement.ControlTypeProperty];
            }
        }
        public virtual string ControlTypeName {
            get {
                return NameMappings.GetName(ControlType);
            }
        }
        public virtual Rect Bounds {
            get {
        		return (Rect)this[AutomationElement.BoundingRectangleProperty];
            }
            set {
                Location = value.Location;
                Size = value.Size;
            }
        }
        public Point Location {
            get {
                return Bounds.Location;
            }
            set {
        		STAHelper.Invoke(
        			delegate() {
		                TransformPattern.Move(value.X, value.Y);
        			}
        		);
            }
        }
        public Size Size {
            get {
                return Bounds.Size;
            }
            set {
        		STAHelper.Invoke(
        			delegate() {
		                TransformPattern.Resize(value.Width, value.Height);
        			}
        		);
            }
        }
        public void Close() {
        	STAHelper.Invoke(
        		delegate() {
		            WindowPattern.Close();
        		}
        	);
        }
        public void Focus() {
        	// run all in same thread for speed
        	bool focused = (bool)STAHelper.Invoke(
        		delegate() {
		            // make sure parent window is focused
		            if (ControlType != ControlType.Window) {
		                Element window = Parent;
		                while (window != null && window.ControlType != ControlType.Window)
		                    window = window.Parent;
		                if (window != null)
		                    window.element.SetFocus();
		            }
		            element.SetFocus();
		            return (bool)AutomationElement.GetCurrentPropertyValue(AutomationElement.HasKeyboardFocusProperty);
        		}
        	);
        	if(!focused) {
        		// we can't actually fail here because some elements respond to SetFocus
        		// but don't accept keyboard focus.
        		// on the other hand sometimes the focus is async and not done yet. 
        		// so if we're not sure we're done, sleep a little and hope it finishes
        		System.Threading.Thread.Sleep(100);
        	}
        }

        public Element Parent {
            get {
        		return (Element) STAHelper.Invoke(
        			delegate() {
		                TreeWalker tWalker = TreeWalker.RawViewWalker;  
		                AutomationElement parent = tWalker.GetParent(element);
		                if (parent == null || parent == element)
		                    return null;
		                if (parent == AutomationElement.RootElement)
		                    return Desktop.GetInstance(processId);
		                return Element.Create(parent, processId);
        			}
        		);
            }
        }
        public List<Element> Children {
            get {
        		return (List<Element>) STAHelper.Invoke(
        			delegate() {
		                List<AutomationElement> searchResults = AutomationExtensions.FindAllRaw(element, TreeScope.Children, new PropertyCondition2(AutomationElement.ProcessIdProperty, processId));
		                List<Element> result = new List<Element>();
		                foreach (AutomationElement e in searchResults)
		                    result.Add(Element.Create(e, processId));
		                return result;
        			}
        		);
            }
        }

        public List<Element> Selection {
            get {
        		return (List<Element>) STAHelper.Invoke(
        			delegate() {
		                List<Element> result = new List<Element>();
		                AutomationElement[] elts = (AutomationElement[])this[SelectionPattern.SelectionProperty];
		                foreach(AutomationElement elt in elts)
		                    result.Add(Element.Create(elt, processId));
		                return result;
		       		}
        	    );
            }
        }
        public bool SelectionIsRequired {
            get {
        		return (bool)this[SelectionPattern.IsSelectionRequiredProperty];
            }
        }
        public bool SelectionAllowsMultiple {
            get {
        		return (bool)this[SelectionPattern.CanSelectMultipleProperty];
            }
        }

        public Element SelectionContainer {
            get {
        		return (Element)STAHelper.Invoke(
        			delegate() {
		        		AutomationElement container = (AutomationElement)this[SelectionItemPattern.SelectionContainerProperty];
    		            return Element.Create(container, processId);
        			}
        		);
            }
        }

        private Dictionary<OrientationType, ScrollAxis> scrollAxes = new Dictionary<OrientationType, ScrollAxis>();
        public ScrollAxis GetScrollAxis(OrientationType orientation) {
        	return (ScrollAxis)STAHelper.Invoke(
        		delegate() {
		            if(!scrollAxes.ContainsKey(orientation)) {
		                object pattern;
		                if(AutomationElement.TryGetCurrentPattern(ScrollPattern.Pattern, out pattern)) {
		                    scrollAxes[orientation] = new ScrollPatternAxis((ScrollPattern)pattern, orientation);
		                } else if(ControlType == ControlType.ScrollBar) {
		                	if(orientation == (OrientationType)this[AutomationElement.OrientationProperty])
		                        scrollAxes[orientation] = new ScrollBarAxis(AutomationElement);
		                    else
		                        throw new ArgumentException("Cannot get "+orientation+" scroll-axis for a scrollbar that is not "+orientation);
		                } else if (ControlType == ControlType.Pane && Class == "ScrollBar") {
		                    scrollAxes[orientation] = new PaneScrollAxis(AutomationElement);
		                } else {
		                    Condition scrollBarCondition = 
		                        new AndCondition(
		                            new PropertyCondition2(AutomationElement.OrientationProperty, orientation),
		                            new PropertyCondition2(AutomationElement.ControlTypeProperty, ControlType.ScrollBar)
		                        );
		                    Condition scrollPaneCondition =
		                        new AndCondition(
		                            new PropertyCondition2(AutomationElement.ControlTypeProperty, ControlType.Pane),
		                            new PropertyCondition2(AutomationElement.ClassNameProperty, "ScrollBar")
		                        );
		                    Condition scrollPatternCondition = new PropertyCondition2(AutomationElement.IsScrollPatternAvailableProperty, true);
		                    Condition condition = new OrCondition(scrollBarCondition, scrollPaneCondition, scrollPatternCondition);
		                    List<AutomationElement> matches = Twin.View.Search.FindAll(AutomationElement, TreeScope.Descendants, 1 ,condition, (int)AutomationElement.GetCurrentPropertyValue(AutomationElement.ProcessIdProperty));
		                    for(int i=0; i<matches.Count; i++) {
		                        if(matches[i].GetCurrentPropertyValue(AutomationElement.ControlTypeProperty) != ControlType.Pane)
		                            continue;
		                        if((bool)matches[i].GetCurrentPropertyValue(AutomationElement.IsScrollPatternAvailableProperty))
		                        	continue;
		                        Rect bounds = (Rect)matches[i].GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
		                        if(orientation == OrientationType.Horizontal && bounds.Height > bounds.Width)
		                            matches.RemoveAt(i--);
		                        if(orientation == OrientationType.Vertical && bounds.Width > bounds.Height)
		                            matches.RemoveAt(i--);
		                    }
		                    if (matches.Count == 0)
		                        return null;
		                    if (matches.Count > 1)
		                        throw new ArgumentException("Scrollable Axis for element ambiguous");
		                    return Element.Create(matches[0], processId).GetScrollAxis(orientation);
		                }
		            }
		            return scrollAxes[orientation];
        		}
        	);
        }
        public NativeElement EnclosingNativeElement {
            get {
                if (this is NativeElement)
                    return (NativeElement)this;
                return Parent.EnclosingNativeElement;
            }
        }
        public System.Drawing.Bitmap CaptureScreenshot() {
        	Size size = this.Size;
        	return CaptureScreenshot(new Rect(0,0,size.Width, size.Height));
        }
        // overridden for native elements
        public virtual System.Drawing.Bitmap CaptureScreenshot(Rect bounds) {
            Element nativeParent = EnclosingNativeElement;
            bounds.Location += this.Location - nativeParent.Location;
            return nativeParent.CaptureScreenshot(bounds);
        }

        public void Click() {
            try {
                Invoke();
                return;
            } catch (InvalidOperationException) {}
            try {
                Toggle();
                return;
            } catch (InvalidOperationException) {}
            try {
                SelectionItemPattern.Select();
                return;
            } catch (InvalidOperationException) {}
            try {
                ExpandCollapse();
                return;
            } catch (InvalidOperationException) {}
            Click(1);
        }
        public void Click(int button) {
        	STAHelper.Invoke(
        		delegate() {
	            	Point clickable = element.GetClickablePoint(); // absolute coords
	           		Desktop.GetInstance(processId).Click(button, clickable.X, clickable.Y);
        	    }
        	);
        }
        public virtual void Click(int button, double x, double y) {
            Point point = Location;
            point.X += x;
            point.Y += y;
            // TODO make sure window is in front?
            Desktop.GetInstance(processId).Click(button, (int)point.X, (int)point.Y);
        }

        public void Toggle() {
        	STAHelper.Invoke(
        		delegate() {
		            TogglePattern.Toggle();
        		}
        	);
        }
        public bool ToggleState {
            get {
        		ToggleState state = (ToggleState)this[TogglePattern.ToggleStateProperty];
                if (state == System.Windows.Automation.ToggleState.Off)
                    return false;
                return true;
            }
            set {
                if (ToggleState != value)
                	STAHelper.Invoke(delegate() { TogglePattern.Toggle(); });
            }
        }
        public WindowVisualState WindowState {
            get {
        		return (WindowVisualState)this[WindowPattern.WindowVisualStateProperty];
            }
            set {
        		STAHelper.Invoke(delegate() { WindowPattern.SetWindowVisualState(value); });
            }
        }
        public bool Selected {
            get {
        		return (bool) this[SelectionItemPattern.IsSelectedProperty];
            }
            set {
        		STAHelper.Invoke(
        			delegate() {
		                if (value) {
		        			AutomationElement container = (AutomationElement)this[SelectionItemPattern.SelectionContainerProperty];
		                    if ((bool)container.GetCurrentPropertyValue(SelectionPattern.CanSelectMultipleProperty))
		                        SelectionItemPattern.AddToSelection();
		                    else
		                        SelectionItemPattern.Select();
		                } else {
		                    SelectionItemPattern.RemoveFromSelection();
		                }
        			}
        		);
            }
        }
        public void Deselect() {
        	STAHelper.Invoke(delegate() { SelectionItemPattern.RemoveFromSelection(); });
        }
        public void ExpandCollapse() {
        	STAHelper.Invoke(
        		delegate() {
		            ExpandCollapsePattern expandCollapse = ExpandCollapsePattern;
		            if (expandCollapse.Current.ExpandCollapseState == ExpandCollapseState.Collapsed)
		                expandCollapse.Expand();
		            else
		                expandCollapse.Collapse();
        		}
        	);
        }
        public void Invoke() {
        	STAHelper.Invoke(delegate() { InvokePattern.Invoke(); });
        }
        public bool Exists {
            get {
                try {
        			STAHelper.Invoke(
        				delegate() { 
        					Rect rect = (Rect)element.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
        					if(Double.IsInfinity(rect.Top) || Double.IsNaN(rect.Top))
        						throw new ElementNotAvailableException();
        				}
        			);
                    return true;
                } catch (ElementNotAvailableException) {
                    return false;
                }
            }
        }

        public void Dispose() {
        	// STAHelper.Invoke(delegate() { element.Dispose(); });
        }

        public void AddExtraJSONProperties(IDictionary<string,object> values) {
            values["name"] = Name;
            values["id"] = Id;
            values["controlType"] = ControlTypeName;
            values["className"] = Class;
            values["controlPatterns"] = STAHelper.Invoke(
            	delegate() {
		        	List<object> controlPatterns = new List<object>();
                	foreach (AutomationPattern pattern in element.GetSupportedPatterns()) {
		                string name = NameMappings.GetName(pattern);
		                if(name != null)
		                    controlPatterns.Add(name);
		            }
		        	return controlPatterns;
                }
            );
        }

        // don't think equality/hashcode need to be run in STA thread...
        public override int GetHashCode() {
        	return element.GetHashCode();
        }
        public override bool Equals(object other) {
            return (other is Element) && (element == ((Element)other).element);
        }
        public static bool operator ==(Element first, Element second) {
            return first.Equals(second);
        }
        public static bool operator !=(Element first, Element second) {
            return !first.Equals(second);
        }
    }
}
