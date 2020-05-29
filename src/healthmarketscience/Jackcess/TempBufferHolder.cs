/*
Copyright (c) 2008 Health Market Science, Inc.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
USA

You can contact Health Market Science at info@healthmarketscience.com
or at the following address:

Health Market Science
2700 Horizon Drive
Suite 200
King of Prussia, PA 19406
*/

using Sharpen;
using System;

namespace HealthMarketScience.Jackcess
{
    /// <summary>Manages a reference to a ByteBuffer.</summary>
    /// <remarks>Manages a reference to a ByteBuffer.</remarks>
    /// <author>James Ahlborn</author>
    public abstract class TempBufferHolder
    {
        private static readonly Reference<ByteBuffer> EMPTY_BUFFER_REF = new SoftReference
            <ByteBuffer>(null);

        /// <summary>The caching type for the buffer holder.</summary>
        /// <remarks>The caching type for the buffer holder.</remarks>
        public enum Type
        {
            HARD,
            SOFT,
            NONE
        }

        /// <summary>whether or not every get automatically rewinds the buffer</summary>
        private readonly bool _autoRewind;

        /// <summary>ByteOrder for all allocated buffers</summary>
        private readonly ByteOrder _order;

        /// <summary>the mod count of the current buffer (changes on every realloc)</summary>
        private int _modCount;

        protected internal TempBufferHolder(bool autoRewind, ByteOrder order)
        {
            _autoRewind = autoRewind;
            _order = order;
        }

        /// <returns>
        /// the modification count of the current buffer (this count is
        /// changed every time the buffer is reallocated)
        /// </returns>
        public virtual int GetModCount()
        {
            return _modCount;
        }

        /// <summary>Creates a new TempBufferHolder.</summary>
        /// <remarks>Creates a new TempBufferHolder.</remarks>
        /// <param name="type">the type of reference desired for any created buffer</param>
        /// <param name="autoRewind">
        /// whether or not every get automatically rewinds the
        /// buffer
        /// </param>
        public static HealthMarketScience.Jackcess.TempBufferHolder NewHolder(TempBufferHolder.Type
             type, bool autoRewind)
        {
            return NewHolder(type, autoRewind, PageChannel.DEFAULT_BYTE_ORDER);
        }

        /// <summary>Creates a new TempBufferHolder.</summary>
        /// <remarks>Creates a new TempBufferHolder.</remarks>
        /// <param name="type">the type of reference desired for any created buffer</param>
        /// <param name="autoRewind">
        /// whether or not every get automatically rewinds the
        /// buffer
        /// </param>
        /// <param name="order">byte order for all allocated buffers</param>
        public static HealthMarketScience.Jackcess.TempBufferHolder NewHolder(TempBufferHolder.Type
             type, bool autoRewind, ByteOrder order)
        {
            switch (type)
            {
                case TempBufferHolder.Type.HARD:
                    {
                        return new TempBufferHolder.HardTempBufferHolder(autoRewind, order);
                    }

                case TempBufferHolder.Type.SOFT:
                    {
                        return new TempBufferHolder.SoftTempBufferHolder(autoRewind, order);
                    }

                case TempBufferHolder.Type.NONE:
                    {
                        return new TempBufferHolder.NoneTempBufferHolder(autoRewind, order);
                    }

                default:
                    {
                        throw new InvalidOperationException("Unknown type " + type);
                    }
            }
        }

        /// <summary>
        /// Returns a ByteBuffer of at least the defined page size, with the limit
        /// set to the page size, and the predefined byteOrder.
        /// </summary>
        /// <remarks>
        /// Returns a ByteBuffer of at least the defined page size, with the limit
        /// set to the page size, and the predefined byteOrder.  Will be rewound iff
        /// autoRewind is enabled for this buffer.
        /// </remarks>
        public ByteBuffer GetPageBuffer(PageChannel pageChannel)
        {
            return GetBuffer(pageChannel, pageChannel.GetFormat().PAGE_SIZE);
        }

        /// <summary>
        /// Returns a ByteBuffer of at least the given size, with the limit set to
        /// the given size, and the predefined byteOrder.
        /// </summary>
        /// <remarks>
        /// Returns a ByteBuffer of at least the given size, with the limit set to
        /// the given size, and the predefined byteOrder.  Will be rewound iff
        /// autoRewind is enabled for this buffer.
        /// </remarks>
        public ByteBuffer GetBuffer(PageChannel pageChannel, int size)
        {
            ByteBuffer buffer = GetExistingBuffer();
            if ((buffer == null) || (buffer.Capacity() < size))
            {
                buffer = pageChannel.CreateBuffer(size, _order);
                ++_modCount;
                SetNewBuffer(buffer);
            }
            else
            {
                buffer.Limit(size);
            }
            if (_autoRewind)
            {
                buffer.Rewind();
            }
            return buffer;
        }

        /// <returns>
        /// the currently referenced buffer,
        /// <code>null</code>
        /// if none
        /// </returns>
        public abstract ByteBuffer GetExistingBuffer();

        /// <summary>Releases any referenced memory.</summary>
        /// <remarks>Releases any referenced memory.</remarks>
        public abstract void Clear();

        /// <summary>Sets a new buffer for this holder.</summary>
        /// <remarks>Sets a new buffer for this holder.</remarks>
        protected internal abstract void SetNewBuffer(ByteBuffer newBuffer);

        /// <summary>TempBufferHolder which has a hard reference to the buffer.</summary>
        /// <remarks>TempBufferHolder which has a hard reference to the buffer.</remarks>
        internal sealed class HardTempBufferHolder : TempBufferHolder
        {
            private ByteBuffer _buffer;

            internal HardTempBufferHolder(bool autoRewind, ByteOrder order) : base(
                autoRewind, order)
            {
            }

            public override ByteBuffer GetExistingBuffer()
            {
                return _buffer;
            }

            protected internal override void SetNewBuffer(ByteBuffer newBuffer)
            {
                _buffer = newBuffer;
            }

            public override void Clear()
            {
                _buffer = null;
            }
        }

        /// <summary>TempBufferHolder which has a soft reference to the buffer.</summary>
        /// <remarks>TempBufferHolder which has a soft reference to the buffer.</remarks>
        internal sealed class SoftTempBufferHolder : TempBufferHolder
        {
            private Reference<ByteBuffer> _buffer = EMPTY_BUFFER_REF;

            internal SoftTempBufferHolder(bool autoRewind, ByteOrder order) : base(
                autoRewind, order)
            {
            }

            public override ByteBuffer GetExistingBuffer()
            {
                return _buffer.Get();
            }

            protected internal override void SetNewBuffer(ByteBuffer newBuffer)
            {
                _buffer.Clear();
                _buffer = new SoftReference<ByteBuffer>(newBuffer);
            }

            public override void Clear()
            {
                _buffer.Clear();
            }
        }

        /// <summary>TempBufferHolder which has a no reference to the buffer.</summary>
        /// <remarks>TempBufferHolder which has a no reference to the buffer.</remarks>
        internal sealed class NoneTempBufferHolder : TempBufferHolder
        {
            internal NoneTempBufferHolder(bool autoRewind, ByteOrder order) : base(
                autoRewind, order)
            {
            }

            public override ByteBuffer GetExistingBuffer()
            {
                return null;
            }

            protected internal override void SetNewBuffer(ByteBuffer newBuffer)
            {
            }

            // nothing to do
            public override void Clear()
            {
            }
            // nothing to do
        }
    }
}
