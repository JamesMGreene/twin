// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using Twin.Model;

namespace Twin {
    class TwinException : Exception {
        ResponseStatus status;
        public ResponseStatus ResponseStatus {
            get {
                return status;
            }
        }
        public TwinException(String message) : this(ResponseStatus.UnknownError, message) { }
        public TwinException(ResponseStatus status, String message) : base(message){
            this.status = status;
        }
        public TwinException(ResponseStatus status, String message, Exception e) : base(message, e) {
            this.status = status;
        }
    }
}
