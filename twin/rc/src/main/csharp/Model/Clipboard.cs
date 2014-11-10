// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Twin.Model {
    class Clipboard {
		public static bool Empty {
			get {
				return (bool)STAHelper.Invoke(
					delegate() {
						return System.Windows.Clipboard.GetDataObject() == null;
					}
				);
			} 
			set {
				STAHelper.Invoke(
					delegate() { 
	            		if (value)
	            		    System.Windows.Clipboard.Clear();
			            else
    			            throw new ArgumentOutOfRangeException("value", value, "Cannot set Empty to false!");
					}
				);
			}
		}
		public static string Text {
			get {
				return (string)STAHelper.Invoke(
					delegate() { 
						if(System.Windows.Clipboard.ContainsText())
			                return System.Windows.Clipboard.GetText();
   				        return null;
					}
				);
			} 
			set {
				STAHelper.Invoke(
					delegate() { 
			            if (value == null)
			                System.Windows.Clipboard.Clear();
		    	        else
    		    	        System.Windows.Clipboard.SetText(value);
					}
				);
			}
		}		
	}
}
