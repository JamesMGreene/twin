// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Twin.SharpClaws.API;

namespace Twin.SharpClaws {
    internal class ChunkedStream : Stream {
        const int OUTPUT_CHUNK_SIZE = 1024;

        Stream underlying;
        byte[] bufferIn;
        int bufferInPos = 0;
        int bufferInSize = 0;
        bool inEof = false;
        byte[] bufferOut;
        int bufferOutPos = 0;
        internal bool allowRead;
        internal bool allowWrite;
        public ChunkedStream(Stream underlying) : this(underlying, true, true) {}
        public ChunkedStream(Stream underlying, bool allowRead, bool allowWrite) {
        	this.underlying = underlying;
        	this.allowRead = allowRead;
        	this.allowWrite = allowWrite;
        }

        public override bool CanRead {
        	get { return allowRead && underlying.CanRead; }
        }

        public override bool CanSeek {
            get { return false; }
        }

        public override bool CanWrite {
            get { return allowWrite && underlying.CanWrite; }
        }

        public override void Flush() {
            if (bufferOutPos != 0) {
                byte[] chunkData = Encoding.ASCII.GetBytes(string.Format("{0:x}\r\n", bufferOutPos));
                underlying.Write(chunkData, 0, chunkData.Length);
                underlying.Write(bufferOut, 0, bufferOutPos);
                underlying.Write(new byte[] { (byte)'\r', (byte)'\n' }, 0, 2);
                bufferOutPos = 0;
                underlying.Flush();
            }
        }

        public override void Close() {
            if (CanWrite) {
                Flush();
                underlying.Write(new byte[] { (byte)'0', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' }, 0, 5);
                underlying.Flush();
            }
            base.Close();
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

        internal string readLine() {
            StringBuilder line = new StringBuilder();
            byte[] bytes = new byte[1];
            while (true) {
                int nextByte = underlying.ReadByte();
                if (nextByte == -1 || nextByte == '\n') {
                    if (nextByte == 1 && line.Length == 0)
                        return null;
                    return line.ToString().TrimEnd('\r', '\n');
                }
                bytes[0] = (byte)nextByte;
                line.Append(Encoding.ASCII.GetChars(bytes));
            }
        }

        public override int Read(byte[] buffer, int offset, int n) {
            if (inEof)
                return -1;
            // skip reading more data if we have data in the buffer already
            if (!(bufferInSize > 0 && bufferInPos < bufferInSize)) {
                string line = readLine();
                int count = -1;
                if (line == null)
                    throw new HttpException(400, "Unexpected EOF in chunked data");
                string number = line.Contains(";") ? line.Substring(0, line.IndexOf(';')).Trim() : line.Trim();
                try {
                    count = Convert.ToInt32(number, 16);
                } catch (FormatException) {
                    throw new HttpException(400, "Bad chunk length line " + line);
                }
                if (count == 0) {
                    inEof = true;

                    string blank = readLine();
                    if (blank.Length > 0)
                        throw new HttpException(400, "Expected blank line after last chunk but got " + blank);

                    return -1;
                }
                if (bufferIn == null || bufferIn.Length < count) {
                    bufferIn = new byte[count];
                    bufferInPos = 0;
                    bufferInSize = count;

                    while (bufferInPos < bufferInSize) {
                        count = underlying.Read(bufferIn, bufferInPos, bufferInSize - bufferInPos);
                        if (count == -1)
                            throw new HttpException(400, "Reached eof during chunk");
                        bufferInPos += count;
                    }
                    bufferInPos = 0;
                }
                string blank2 = readLine();
                if (blank2.Length > 0)
                    throw new HttpException(400, "Expected blank line after chunk but got " + blank2);
            }
            int bufferToReturnCount = Math.Min(bufferInSize - bufferInPos, n);
            Array.Copy(bufferIn, bufferInPos, buffer, offset, bufferToReturnCount);
            bufferInPos += bufferToReturnCount;
            return bufferToReturnCount;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            if (bufferOut == null)
                bufferOut = new byte[OUTPUT_CHUNK_SIZE];
            while (count > 0) {
                int bytesToCopy = Math.Min(bufferOut.Length - bufferOutPos, count);
                Array.Copy(buffer, offset, bufferOut, bufferOutPos, bytesToCopy);
                bufferOutPos += bytesToCopy;
                offset += bytesToCopy;
                count -= bytesToCopy;
                if (bufferOutPos == bufferOut.Length)
                    Flush();
            }
        }
    }
}
