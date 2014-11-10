// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Twin.SharpClaws {
    class StreamAsNeeded : Stream {
        Response response;
        Stream underlying;

        public StreamAsNeeded(Response response) {
            this.response = response;
        }

        public override bool CanRead {
            get { return false; }
        }

        public override bool CanSeek {
            get { return false; }
        }

        public override bool CanWrite {
            get { return true; }
        }

        public override void Flush() {
            if (underlying != null)
                underlying.Flush();
        }

        public override long Length {
            get { throw new NotImplementedException(); }
        }

        public override long Position {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            if (count == 0)
                return;
            if (underlying == null)
                underlying = response.OpenBody();
            underlying.Write(buffer, offset, count);
        }

        bool closed = false;
        public override void Close() {
            if (closed)
                return;
            closed = true;
            if (underlying != null) {
                underlying.Flush();
                underlying.Close();
            }
            base.Close();
        }
    }
}
