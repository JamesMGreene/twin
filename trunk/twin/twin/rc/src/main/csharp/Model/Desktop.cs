// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.Windows;
using System.Drawing.Imaging;

namespace Twin.Model {
    class Desktop : NativeElement {
        private Desktop(int processId) : base(AutomationElement.RootElement, processId) {
        }

        static Dictionary<int, Desktop> instances = new Dictionary<int,Desktop>();
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Desktop GetInstance(int process) {
            if (!instances.ContainsKey(process))
                instances[process] = new Desktop(process);
            return instances[process];
        }

        [DllImport("user32.dll")]
        static extern void mouse_event(MouseEventFlags dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        [Flags]
        public enum MouseEventFlags {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }
        private static MouseEventFlags MouseDown(int button) {
            switch (button) {
                case 1:
                    return MouseEventFlags.LEFTDOWN;
                case 2:
                    return MouseEventFlags.RIGHTDOWN;
                case 3:
                    return MouseEventFlags.MIDDLEDOWN;
            }
            throw new ArgumentOutOfRangeException("button");
        }
        private static MouseEventFlags MouseUp(int button) {
            switch (button) {
                case 1:
                    return MouseEventFlags.LEFTUP;
                case 2:
                    return MouseEventFlags.RIGHTUP;
                case 3:
                    return MouseEventFlags.MIDDLEUP;
            }
            throw new ArgumentOutOfRangeException("button");
        }

        public override void Click(int button, double x, double y) {
            System.Drawing.Point oldPosition = Cursor.Position;
            Cursor.Position = new System.Drawing.Point((int)x, (int)y);
            mouse_event(MouseDown(button), 0, 0, 0, 0);
            mouse_event(MouseUp(button), 0, 0, 0, 0);
            Cursor.Position = oldPosition;
        }

        public override Bitmap CaptureScreenshot(Rect bounds) {
            Bitmap screenshot = new Bitmap((int)bounds.Width, (int)bounds.Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(screenshot)) {
                graphics.CopyFromScreen(
                    (int)(Screen.PrimaryScreen.Bounds.X + bounds.X), (int)(Screen.PrimaryScreen.Bounds.Y + bounds.Y), // from
                    0,0, // to
                    new System.Drawing.Size((int)bounds.Width, (int)bounds.Height),
                    CopyPixelOperation.SourceCopy
                );
            }
            return screenshot;
        }

        public override string ControlTypeName {
            get { return "Desktop"; }
        }
    }
}
