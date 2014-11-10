// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Automation;
using System.Windows;
using System.Runtime.InteropServices;

namespace Twin.Model {
    abstract class ScrollAxis {
        public abstract OrientationType Orientation { get; }
        public abstract double Position { get; set; }
    }

    // This is for Win32 ScrollBars who aren't recognized by UIAutomation
    // We control them with {Get,Set}ScrollInfo and by posting WM_{H,V}SCROLL to their parents
    // excuse the PInvoke grunge...
    class PaneScrollAxis : ScrollAxis {
        [StructLayout(LayoutKind.Sequential)]
        struct SCROLLINFO {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWINFO {
            public uint cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct RECT {
            public int Left;    // Specifies the x-coordinate of the upper-left corner of the rectangle. 
            public int Top;        // Specifies the y-coordinate of the upper-left corner of the rectangle. 
            public int Right;    // Specifies the x-coordinate of the lower-right corner of the rectangle.
            public int Bottom;    // Specifies the y-coordinate of the lower-right corner of the rectangle. 
        }
        enum ScrollBarDirection : int {
            SB_HORZ = 0,
            SB_VERT = 1,
            SB_CTL = 2,
            SB_BOTH = 3
        }
        enum WM_SCROLL_low : ushort {
            SB_THUMBPOSITION = 4,
            SB_THUMBTRACK = 5,
            SB_ENDSCROLL = 8
        }
        enum ScrollInfoMask : uint {
            SIF_RANGE = 0x1,
            SIF_PAGE = 0x2,
            SIF_POS = 0x4,
            SIF_DISABLENOSCROLL = 0x8,
            SIF_TRACKPOS = 0x10,
            SIF_ALL = SIF_RANGE + SIF_PAGE + SIF_POS + SIF_TRACKPOS
        }
        enum Messages : uint {
            WM_HSCROLL = 0x114,
            WM_VSCROLL = 0x115
        }
        enum Styles : uint {
            SBS_HORZ = 0,
            SBS_VERT = 1
        }

        [DllImport("user32.dll")]
        static extern bool GetScrollInfo(int hwnd, int fnBar, ref SCROLLINFO lpsi);
        [DllImport("user32.dll")]
        static extern int SetScrollInfo(int hwnd, int fnBar, [In] ref SCROLLINFO lpsi, bool fRedraw);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(int hWnd, UInt32 Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        static extern bool GetWindowInfo(int hwnd, ref WINDOWINFO info);
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetParent(int hWnd);

        int hwnd;
        public PaneScrollAxis(AutomationElement auto) {
        	STAHelper.Invoke(
        		delegate() {
		            hwnd = (int)auto.GetCurrentPropertyValue(AutomationElement.NativeWindowHandleProperty);
        		}
        	);
        }
        private bool orientationSet = false;
        private OrientationType orientation;
        public override OrientationType Orientation {
            get {
                if (!orientationSet) {
                    WINDOWINFO info = new WINDOWINFO();
                    info.cbSize = 60;
                    if(!GetWindowInfo(hwnd, ref info))
                        throw new InvalidOperationException("GetWindowInfo returned zero");
                    if ((info.dwStyle & (uint)Styles.SBS_VERT) != 0)
                        orientation = OrientationType.Vertical;
                    else
                        orientation = OrientationType.Horizontal;
                    orientationSet = true;
                }
                return orientation;
            }
        }
        private SCROLLINFO ScrollInfo {
            get {
                SCROLLINFO scrollInfo = new SCROLLINFO();
                scrollInfo.cbSize = 28;
                scrollInfo.fMask = (uint)ScrollInfoMask.SIF_ALL;
                if (!GetScrollInfo(hwnd, (int)ScrollBarDirection.SB_CTL, ref scrollInfo))
                    throw new InvalidOperationException("GetScrollInfo returned zero");
                return scrollInfo;
            }
            set {
                SetScrollInfo(hwnd, (int)ScrollBarDirection.SB_CTL, ref value, true);
            }
        }
        public override double Position {
            get {
                SCROLLINFO info = ScrollInfo;
                return (double)(info.nPos - info.nMin) / (info.nMax - info.nMin - info.nPage + 1);
            }
            set {
                int target = GetParent(hwnd); // not sure if this is the right approach in general
                if (target == 0)
                    target = hwnd;
                SCROLLINFO info = ScrollInfo;
                info.fMask = (uint)ScrollInfoMask.SIF_POS; // just set the position
                info.nPos = info.nMin + (int)(value * (info.nMax - info.nMin - info.nPage + 1));
                ScrollInfo = info;
                info = ScrollInfo; // refresh
                Messages message = Orientation == OrientationType.Horizontal ? Messages.WM_HSCROLL : Messages.WM_VSCROLL;
                int wParamHi = info.nPos;
                int wParamLo = (int)WM_SCROLL_low.SB_THUMBTRACK; // Send the full sequence for robustness, but AFAIK nothing uses ENDSCROLL...
                SendMessage(target, (uint)message, (wParamHi << 16) | wParamLo, hwnd);
                wParamLo = (int)WM_SCROLL_low.SB_THUMBPOSITION;
                SendMessage(target, (uint)message, (wParamHi << 16) | wParamLo, hwnd);
                wParamLo = (int)WM_SCROLL_low.SB_ENDSCROLL;
                SendMessage(target, (uint)message, (wParamHi << 16) | wParamLo, hwnd);
            }
        }
    }

    // This is the nice case - targets that implements ScrollPattern. 
    // Because this pops up one level above the ScrollBar this takes precedence over the stuff below
    class ScrollPatternAxis : ScrollAxis {
        ScrollPattern scrollPattern;
        OrientationType orientation;
        public ScrollPatternAxis(ScrollPattern scrollPattern, OrientationType orientation) {
            this.scrollPattern = scrollPattern;
            this.orientation = orientation;
        }
        public override OrientationType Orientation {
            get { return orientation; }
        }
        public override double Position {
            get {
        		return (double)STAHelper.Invoke(
        			delegate() {
		                switch (orientation) {
		                    case OrientationType.Horizontal:
		                        return scrollPattern.Current.HorizontalScrollPercent / 100.0;
		                    case OrientationType.Vertical:
		                        return scrollPattern.Current.VerticalScrollPercent / 100.0;
		                }
		                throw new ArgumentOutOfRangeException("orientation", orientation, "Unknown orientation");
        			}
        		);
            }
            set {
        		STAHelper.Invoke(
        			delegate() {
		                switch (orientation) {
		                    case OrientationType.Horizontal:
		                        scrollPattern.SetScrollPercent(value * 100.0, ScrollPattern.NoScroll);
		                        break;
		                    case OrientationType.Vertical:
		                        scrollPattern.SetScrollPercent(ScrollPattern.NoScroll, value * 100.0);
		                        break;
		                }
        			}
        		);
            }
        }
    }

    // This is when we find a ScrollBar that wasn't surrounded by a ScrollPattern-having element
    class ScrollBarAxis : ScrollAxis {
        AutomationElement scrollBar;
        public ScrollBarAxis(AutomationElement scrollBar) {
            this.scrollBar = scrollBar;
        }
        private OrientationType orientation;
        private bool orientationSet = false;
        public override OrientationType Orientation {
            get {
                if (!orientationSet) {
        			Rect bounds = (Rect)STAHelper.Invoke(
        				delegate() {   	
        					return scrollBar.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
        				}
        			);
                    orientation = bounds.Width > bounds.Height ? OrientationType.Horizontal : OrientationType.Vertical;
                    orientationSet = true;
                }
                return orientation;
            }
        }
        private AutomationElement Thumb {
            get {
        		return (AutomationElement)STAHelper.Invoke(
        			delegate() {
        				return scrollBar.FindFirst(TreeScope.Children, new PropertyCondition2(AutomationElement.ControlTypeProperty, ControlType.Thumb));
        			}
        		);
            }
        }
        private Rect InternalRegion {
            get {
        		return (Rect)STAHelper.Invoke(
        			delegate() {
		                Rect bounds = (Rect)scrollBar.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
		                List<AutomationElement> buttons = AutomationExtensions.FindAllRaw(scrollBar, TreeScope.Children, new PropertyCondition2(AutomationElement.ControlTypeProperty, ControlType.Button));
		                foreach(AutomationElement button in buttons) {
		                    Rect buttonBounds = (Rect)button.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
		                    trim(ref bounds, buttonBounds);
		                }
		                return bounds;
        			}
        		);
            }
        }
        private void trim(ref Rect main, Rect subrect) {
            if (approx(subrect.Left, main.Left) && approx(subrect.Top, main.Top) && approx(subrect.Right, main.Right)) {
                double change = subrect.Bottom - main.Top;
                main.Y += change;
                main.Height -= change;
            }
            if (approx(subrect.Left, main.Left) && approx(subrect.Top, main.Top) && approx(subrect.Bottom, main.Bottom)) {
                double change = subrect.Right - main.Left;
                main.X += change;
                main.Width -= change;
            }
            if (approx(subrect.Left, main.Left) && approx(subrect.Bottom, main.Bottom) && approx(subrect.Right, main.Right)) {
                main.Height -= (main.Bottom - subrect.Top);
            }
            if (approx(subrect.Right, main.Right) && approx(subrect.Top, main.Top) && approx(subrect.Bottom, main.Bottom)) {
                main.Width -= (main.Right - subrect.Left);
            }
        }
        private bool approx(double a, double b) {
            return Math.Abs(a - b) < 5;
        }
        public override double Position {
            get {
        		return (double)STAHelper.Invoke(
        			delegate() {
		                AutomationElement thumb = Thumb;
		                if (thumb == null)
		                    return 0.0;
		                Rect internalRegion = InternalRegion;
		                Rect thumbRegion = (Rect)thumb.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
		
		                if (Orientation == OrientationType.Horizontal) {
		                    if (thumbRegion.Width >= internalRegion.Width)
		                        return 0.0;
		                    return (thumbRegion.X - internalRegion.X) / (internalRegion.Width - thumbRegion.Width);
		                } else {
		                    if (thumbRegion.Height >= internalRegion.Height)
		                        return 0.0;
		                    return (thumbRegion.Y - internalRegion.Y) / (internalRegion.Height - thumbRegion.Height);
		                }
        			}
        		);                	
            }
            set {
        		STAHelper.Invoke(
        			delegate() {
			            AutomationElement thumb = Thumb;
		                if (thumb == null)
		                    return;
		                Rect internalRegion = InternalRegion;
		                Rect thumbRegion = (Rect)thumb.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
		                TransformPattern transform = (TransformPattern)thumb.GetCurrentPattern(TransformPattern.Pattern);
		
		                if (Orientation == OrientationType.Horizontal) {
		                    if (thumbRegion.Width >= internalRegion.Width)
		                        return;
		                    double xPos = internalRegion.Left + value * (internalRegion.Width - thumbRegion.Width);
		                    transform.Move(xPos, internalRegion.Top);
		                } else {
		                    if (thumbRegion.Height >= internalRegion.Height)
		                        return;
		                    double yPos = internalRegion.Top + value * (internalRegion.Height - thumbRegion.Height);
		                    transform.Move(internalRegion.Left, yPos);
		                }
        			}
        		);
            }
        }
    }
}
