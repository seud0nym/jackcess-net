// 
// Channels.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Sharpen
{
    public static class Channels
    {
        public static ReadableByteChannel NewChannel(InputStream stream)
        {
            return new ReadableByteChannel(stream);
        }

        public static OutputStream NewOutputStream(FileChannel c)
        {
            return c.Stream;
        }
    }

    public class ReadableByteChannel : Stream
    {
        private InputStream stream;
        private const int TRANSFER_SIZE = 8192;
        private byte[] buf = new byte[0];

        public ReadableByteChannel(InputStream stream)
        {
            this.stream = stream;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false; 

        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => stream.Position; set => stream.Position = value; }

        public int Read(ByteBuffer dst)
        {
            int len = dst.Remaining();
            int totalRead = 0;
            int bytesRead = 0;

            while (totalRead < len)
            {
                int bytesToRead = Math.Min((len - totalRead),
                                           TRANSFER_SIZE);
                if (buf.Length < bytesToRead)
                    buf = new byte[bytesToRead];
                if ((totalRead > 0) && !(stream.Available() > 0))
                    break; // block at most once
                try
                {
                    bytesRead = stream.Read(buf, 0, bytesToRead);
                }
                finally
                {
                }
                if (bytesRead < 0)
                    break;
                else
                    totalRead += bytesRead;
                dst.Put(buf, 0, bytesRead);
            }
            if ((bytesRead < 0) && (totalRead == 0))
                return -1;

            return totalRead;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}

