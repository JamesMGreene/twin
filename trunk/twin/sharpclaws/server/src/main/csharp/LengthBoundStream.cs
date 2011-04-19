// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Twin.Logging;

namespace Twin.SharpClaws {
    internal class LengthBoundStream : Stream {
        Int64 readRemaining;
        Int64 writeRemaining;
        Stream underlying;
        public LengthBoundStream(Stream underlying, Int64 readLength, Int64 writeLength) {
            this.underlying = underlying;
            writeRemaining = writeLength;
            readRemaining = readLength;
        }

        public override bool CanRead {
            get { return underlying.CanRead; }
        }

        public override bool CanSeek {
            get { return false; }
        }

        public override bool CanWrite {
            get { return underlying.CanWrite; }
        }

        public override void Flush() {
            if(underlying.CanWrite)
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
            count = Math.Min(count, (readRemaining > Int32.MaxValue ? Int32.MaxValue : (int)readRemaining));
            if (count == 0)
                return 0;
            
            int result = underlying.Read(buffer, offset, count);
            readRemaining -= result;

            return result;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            if (count > writeRemaining) {
                if(writeRemaining > 0)
                    Write(buffer, offset, (int)writeRemaining);
                throw new EndOfStreamException();
            }
            underlying.Write(buffer, offset, count);
            writeRemaining -= count;
        }
    }
}
