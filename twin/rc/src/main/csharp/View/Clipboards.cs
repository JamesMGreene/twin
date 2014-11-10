// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;

using Twin.Generic;
using Twin.Model;

namespace Twin.View {
    class Clipboards {
        public static object GetContent(SessionRequest request) {
            Dictionary<string,object> data = new Dictionary<string,object>();
            if(Clipboard.Empty) {
                data["type"]=null;
            } else {
                string text = Clipboard.Text;
                if(text == null)
                    data["type"] = "other";
                else {
                    data["type"] = "text";
                    data["text"] = text;
                }
            }
            return data;
        }
        public static object SetContent(SessionRequest request) {
            switch((string)request.Body["type"]) {
                case null:
                    Clipboard.Empty = true;
                    break;
                case "text":
                    Clipboard.Text = (string)request.Body["text"];
                    break;
                default:
                    throw new TwinException("Unknown clipboard data type "+request.Body["type"]);
            }
            return null;
        }
        public static object Clear(SessionRequest request) {
            Clipboard.Empty = true;
            return null;
        }
    }
}
