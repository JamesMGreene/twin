// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Automation;
using System.Windows;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Twin.Model {
    class NativeElement : Element {
        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(int hwnd, IntPtr hDC, uint nFlags);

        int handle;
        internal NativeElement(AutomationElement el, int processId) : base(el, processId) {
            handle = el.Current.NativeWindowHandle;
        }

        public override Bitmap CaptureScreenshot(Rect bounds) {
        	int width = (int)bounds.Width;
        	int height = (int)bounds.Height;
        	int offX = (int)bounds.X;
        	int offY = (int)bounds.Y;
        	
	        Bitmap bitmap = new Bitmap(offX + width, offY + height);
            Graphics graphics = Graphics.FromImage(bitmap);
            IntPtr deviceContext = graphics.GetHdc();
            try {
                bool success = PrintWindow(handle, deviceContext, 0);
            } finally {
                graphics.ReleaseHdc(deviceContext);
            }
            
            if(offX != 0 || offY != 0)  {
            	Bitmap clipped = bitmap.Clone(new Rectangle(offX,offY,width,height), bitmap.PixelFormat);
            	bitmap.Dispose();
            	bitmap = clipped;
            }

            return bitmap;
        }
    }
}
