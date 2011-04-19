// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using Twin.Model;
using System.Windows;
using System.IO;
using Twin.Proxy;
using System.Windows.Automation;
using Twin.Generic;

namespace Twin.View {
    class Elements {
        public static object Get(ElementRequest request) {
            return PersistedObject.Get(request.Target, request.Session);
        }

        public static object GetFocused(SessionRequest request) {
            return PersistedObject.Get(Element.Create(AutomationElement.FocusedElement, request.Session.Process.Id), request.Session);
        }
        public static object SetFocused(SessionRequest request) {
            Dictionary<string,object> element = (Dictionary<string,object>)request.Body["focusedElement"];
            Guid guid = new Guid((string)element["uuid"]);
            Element elt = (Element)request.Session[guid];
            elt.Focus();
            return null;
        }
		
        public static object Close(ElementRequest request) {
            request.Target.Close();
            return null;
        }

        public static object GetExists(ElementRequest request) {
            return request.Target.Exists;
        }
        const double PollInterval = 1.0;
        public static object PollExists(ElementRequest request) {
            bool target = (bool)request.Body["value"];
            double timeout = request.Body.ContainsKey("timeout") ? Convert.ToDouble(request.Body["timeout"]) : 0;
            if(timeout > 0) {
                Logging.Logger.Current.Trace("Waiting up to {0} for exists={1}", timeout, target);
                long startTicks = DateTime.Now.Ticks;
                long targetTicks = startTicks + (long)(10000000 * timeout);
                while (true) {
                    if (request.Target.Exists == target)
                        break;
                    int deficitMs = (int) (targetTicks - DateTime.Now.Ticks) / 10000;
                    if (deficitMs <= 0)
                        break;
                    System.Threading.Thread.Sleep((int)Math.Min(deficitMs, (int)(PollInterval * 1000)));
                }
            }
            bool exists = request.Target.Exists;
            Logging.Logger.Current.Trace("Finished waiting, element exists={0}", exists);
            if (exists != target)
                throw new TwinException(ResponseStatus.InvalidElementState, "Element did not reach exists state " + target);
            return null;
        }
        public static object GetParent(ElementRequest request) {
            return PersistedObject.Get(request.Target.Parent, request.Session);
        }
        public static object SendKeys(ElementRequest request) {
            string text = (string)request.Body["keys"];
            request.Target.Focus();
            try {
	            System.Windows.Forms.SendKeys.SendWait(text);
            } catch {
            	// if the argument is invalid (e.g. foo{XYZ}) then the "foo" gets kept in a buffer 
            	// and sent with the next SendKeys. ideally we could flush this somehow but 
            	// making sure it's sent now is better than nothing
            	try { System.Windows.Forms.SendKeys.SendWait(""); } catch {}
            	throw;
            }
            return null;
        }
        public static object GetToggleState(ElementRequest request) {
            return request.Target.ToggleState;
        }
        public static object SetToggleState(ElementRequest request) {
            if (request.Body.ContainsKey("state"))
                request.Target.ToggleState = Convert.ToBoolean(request.Body["state"]);
            else
                request.Target.Toggle();
            return request.Target.ToggleState;
        }
        public static object GetSelected(ElementRequest request) {
            return request.Target.Selected;
        }
        public static object SetSelected(ElementRequest request) {
            request.Target.Selected = (bool)request.Body["selected"];
            return null;
        }
        public static object GetEnabled(ElementRequest request) {
            return request.Target.Enabled;
        }
        public static object GetName(ElementRequest request) {
            return request.Target.Name;
        }
        public static object GetValue(ElementRequest request) {
            return request.Target.Value;
        }
        public static object GetExpanded(ElementRequest request) {
            return request.Target.Expanded;
        }
        public static object SetExpanded(ElementRequest request) {
            request.Target.Expanded = (bool)request.Body["expanded"];
            return null;
        }
        public static object SetValue(ElementRequest request) {
            request.Target.Value = (string)request.Body["value"];
            return null;
        }
        public static object GetValueOptions(ElementRequest request) {
            JSONResponse response = new JSONResponse();
            if (request.Target.IsValueReadOnly()) {
                response.Options = new string[] { "GET", "POST", "OPTIONS" };
            } else {
                response.Options = new string[] { "GET", "OPTIONS" };
            }
            return response;
        }
        public static object SetSize(ElementRequest request) {
            double width = Convert.ToDouble(request.Body["width"]);
            double height = Convert.ToDouble(request.Body["height"]);
            request.Target.Size = new Size(width, height);
            return null;
        }
        public static object SetLocation(ElementRequest request) {
            double x = Convert.ToDouble(request.Body["x"]);
            double y = Convert.ToDouble(request.Body["y"]);
            request.Target.Location = new Point(x, y);
            return null;
        }
        public static object GetBounds(ElementRequest request) {
            Dictionary<string, object> data = new Dictionary<string, object>();
            Rect bounds = request.Target.Bounds;
            data["width"] = bounds.Width;
            data["height"] = bounds.Height;
            data["x"] = bounds.X;
            data["y"] = bounds.Y;
            return data;
        }
        public static object SetBounds(ElementRequest request) {
            double x = Convert.ToDouble(request.Body["x"]);
            double y = Convert.ToDouble(request.Body["y"]);
            double width = Convert.ToDouble(request.Body["width"]);
            double height = Convert.ToDouble(request.Body["height"]);
            request.Target.Bounds = new Rect(x, y, width, height);
            return null;
        }

        public static object GetWindowState(ElementRequest request) {
            return request.Target.WindowState.ToString();
        }
        public static object SetWindowState(ElementRequest request) {
            request.Target.WindowState = (WindowVisualState)Enum.Parse(typeof(WindowVisualState), (string)request.Body["state"], true);
            return null;
        }

        public static object GetSelectionContainer(ElementRequest request) {
            return PersistedObject.Get(request.Target.SelectionContainer, request.Session);
        }

        public static object GetSelection(ElementRequest request) {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["multiple"] = request.Target.SelectionAllowsMultiple;
            result["required"] = request.Target.SelectionIsRequired;

            List<object> values = new List<object>();
            foreach (Element elt in request.Target.Selection)
                values.Add(PersistedObject.Get(elt, request.Session));
            result["values"] = values;
            return result;
        }

        public static object GetScreenshot(ElementRequest request) {
            Dictionary<string, object> data = new Dictionary<string, object>();

            Rect defaultRect = new Rect(0, 0, 0, 0);
            Rect bounds = defaultRect;
            if (request.Body != null) {
                if (request.Body.ContainsKey("x"))
                    bounds.X = Convert.ToDouble(request.Body["x"]);
                if (request.Body.ContainsKey("y"))
                    bounds.Y = Convert.ToDouble(request.Body["y"]);
                if (request.Body.ContainsKey("width"))
                    bounds.Width = Convert.ToDouble(request.Body["width"]);
                if (request.Body.ContainsKey("height"))
                    bounds.Height = Convert.ToDouble(request.Body["height"]);
            }

            System.Drawing.Bitmap bitmap = (bounds == defaultRect) ? request.Target.CaptureScreenshot() : request.Target.CaptureScreenshot(bounds);            
            byte[] imageData = BitmapToPNG(bitmap);
            bitmap.Dispose();
            
            data["contentType"] = "image/png";
            data["data"] = Convert.ToBase64String(imageData);
            return data;
        }
        public static object Click(ElementRequest request) {
            int button = 0;
            double x = double.NaN;
            double y = double.NaN;

            if (request.Body != null) {
                if (request.Body.ContainsKey("x"))
                    x = Convert.ToDouble(request.Body["x"]);
                if (request.Body.ContainsKey("y"))
                    y = Convert.ToDouble(request.Body["y"]);
                if (request.Body.ContainsKey("button")) {
                    switch (((string)request.Body["button"]).ToLowerInvariant()) {
                        case "left":
                            button = 1;
                            break;
                        case "right":
                            button = 2;
                            break;
                        case "middle":
                            button = 3;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("button", request.Body["button"], "Should be left/middle/right");
                    }
                }
            }

            if (button == 0 && double.IsNaN(x) && double.IsNaN(y)) {
                request.Target.Click();
            } else {
                if (button == 0)
                    button = 1;

                if (double.IsNaN(x) && double.IsNaN(y)) {
                    request.Target.Click(button);
                } else if (double.IsNaN(x) || double.IsNaN(y)) {
                    throw new ArgumentNullException((double.IsNaN(x) ? "x" : "y"), "If either x or y is set, both must be");
                } else {
                    request.Target.Click(button, x, y);
                }
            }
            return null;
        }

        public static object Scroll(ElementRequest request) {
            ScrollAxis axis = (ScrollAxis)request.Session[new Guid(request.Parameters["axis"])];
            double position = Convert.ToDouble(request.Body["position"]);
            axis.Position = position;
            return null;
        }
        public static object GetScrollPosition(ElementRequest request) {
            ScrollAxis axis = (ScrollAxis)request.Session[new Guid(request.Parameters["axis"])];
            return axis.Position;
        }

        public static object GetScrollerX(ElementRequest request) {
            return PersistedObject.Get(request.Target.GetScrollAxis(OrientationType.Horizontal), request.Session);
        }
        public static object GetScrollerY(ElementRequest request) {
            return PersistedObject.Get(request.Target.GetScrollAxis(OrientationType.Vertical), request.Session);
        }

        private static byte[] BitmapToPNG(System.Drawing.Bitmap bitmap) {
            MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream.ToArray();
        }
    }
}
