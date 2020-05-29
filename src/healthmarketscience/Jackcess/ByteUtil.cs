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
using System.IO;
using System.Text;

namespace HealthMarketScience.Jackcess
{
    /// <summary>Byte manipulation and display utilities</summary>
    /// <author>Tim McCune</author>
    public sealed class ByteUtil
    {
        private static readonly string[] HEX_CHARS = new string[] { "0", "1", "2", "3", "4"
            , "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F" };

        private const int NUM_BYTES_PER_BLOCK = 4;

        private const int NUM_BYTES_PER_LINE = 24;

        public ByteUtil()
        {
        }

        /// <summary>
        /// Put an integer into the given buffer at the given offset as a 3-byte
        /// integer.
        /// </summary>
        /// <remarks>
        /// Put an integer into the given buffer at the given offset as a 3-byte
        /// integer.
        /// </remarks>
        /// <param name="buffer">buffer into which to insert the int</param>
        /// <param name="val">Int to convert</param>
        public static void Put3ByteInt(ByteBuffer buffer, int val)
        {
            Put3ByteInt(buffer, val, buffer.Order());
        }

        /// <summary>
        /// Put an integer into the given buffer at the given offset as a 3-byte
        /// integer.
        /// </summary>
        /// <remarks>
        /// Put an integer into the given buffer at the given offset as a 3-byte
        /// integer.
        /// </remarks>
        /// <param name="buffer">buffer into which to insert the int</param>
        /// <param name="val">Int to convert</param>
        /// <param name="order">the order to insert the bytes of the int</param>
        public static void Put3ByteInt(ByteBuffer buffer, int val, ByteOrder order)
        {
            int pos = buffer.Position();
            Put3ByteInt(buffer, val, pos, order);
            buffer.Position(pos + 3);
        }

        /// <summary>
        /// Put an integer into the given buffer at the given offset as a 3-byte
        /// integer.
        /// </summary>
        /// <remarks>
        /// Put an integer into the given buffer at the given offset as a 3-byte
        /// integer.
        /// </remarks>
        /// <param name="buffer">buffer into which to insert the int</param>
        /// <param name="val">Int to convert</param>
        /// <param name="offset">offset at which to insert the int</param>
        /// <param name="order">the order to insert the bytes of the int</param>
        public static void Put3ByteInt(ByteBuffer buffer, int val, int offset, ByteOrder
            order)
        {
            int offInc = 1;
            if (order == ByteOrder.BIG_ENDIAN)
            {
                offInc = -1;
                offset += 2;
            }
            buffer.Put(offset, unchecked((byte)(val & unchecked((int)(0xFF)))));
            buffer.Put(offset + (1 * offInc), unchecked((byte)(((int)(((uint)val) >> 8)) & unchecked(
                (int)(0xFF)))));
            buffer.Put(offset + (2 * offInc), unchecked((byte)(((int)(((uint)val) >> 16)) & unchecked(
                (int)(0xFF)))));
        }

        /// <summary>Read a 3 byte int from a buffer</summary>
        /// <param name="buffer">Buffer containing the bytes</param>
        /// <returns>The int</returns>
        public static int Get3ByteInt(ByteBuffer buffer)
        {
            return Get3ByteInt(buffer, buffer.Order());
        }

        /// <summary>Read a 3 byte int from a buffer</summary>
        /// <param name="buffer">Buffer containing the bytes</param>
        /// <param name="order">the order of the bytes of the int</param>
        /// <returns>The int</returns>
        public static int Get3ByteInt(ByteBuffer buffer, ByteOrder order)
        {
            int pos = buffer.Position();
            int rtn = Get3ByteInt(buffer, pos, order);
            buffer.Position(pos + 3);
            return rtn;
        }

        /// <summary>Read a 3 byte int from a buffer</summary>
        /// <param name="buffer">Buffer containing the bytes</param>
        /// <param name="offset">Offset at which to start reading the int</param>
        /// <returns>The int</returns>
        public static int Get3ByteInt(ByteBuffer buffer, int offset)
        {
            return Get3ByteInt(buffer, offset, buffer.Order());
        }

        /// <summary>Read a 3 byte int from a buffer</summary>
        /// <param name="buffer">Buffer containing the bytes</param>
        /// <param name="offset">Offset at which to start reading the int</param>
        /// <param name="order">the order of the bytes of the int</param>
        /// <returns>The int</returns>
        public static int Get3ByteInt(ByteBuffer buffer, int offset, ByteOrder order)
        {
            int offInc = 1;
            if (order == ByteOrder.BIG_ENDIAN)
            {
                offInc = -1;
                offset += 2;
            }
            int rtn = GetUnsignedByte(buffer, offset);
            rtn += (GetUnsignedByte(buffer, offset + (1 * offInc)) << 8);
            rtn += (GetUnsignedByte(buffer, offset + (2 * offInc)) << 16);
            return rtn;
        }

        /// <summary>Read an unsigned byte from a buffer</summary>
        /// <param name="buffer">Buffer containing the bytes</param>
        /// <returns>The unsigned byte as an int</returns>
        public static int GetUnsignedByte(ByteBuffer buffer)
        {
            int pos = buffer.Position();
            int rtn = GetUnsignedByte(buffer, pos);
            buffer.Position(pos + 1);
            return rtn;
        }

        /// <summary>Read an unsigned byte from a buffer</summary>
        /// <param name="buffer">Buffer containing the bytes</param>
        /// <param name="offset">Offset at which to read the byte</param>
        /// <returns>The unsigned byte as an int</returns>
        public static int GetUnsignedByte(ByteBuffer buffer, int offset)
        {
            return AsUnsignedByte(buffer.Get(offset));
        }

        /// <summary>Read an unsigned short from a buffer</summary>
        /// <param name="buffer">Buffer containing the short</param>
        /// <returns>The unsigned short as an int</returns>
        public static int GetUnsignedShort(ByteBuffer buffer)
        {
            int pos = buffer.Position();
            int rtn = GetUnsignedShort(buffer, pos);
            buffer.Position(pos + 2);
            return rtn;
        }

        /// <summary>Read an unsigned short from a buffer</summary>
        /// <param name="buffer">Buffer containing the short</param>
        /// <param name="offset">Offset at which to read the short</param>
        /// <returns>The unsigned short as an int</returns>
        public static int GetUnsignedShort(ByteBuffer buffer, int offset)
        {
            return AsUnsignedShort(buffer.GetShort(offset));
        }

        /// <param name="buffer">Buffer containing the bytes</param>
        /// <param name="order">the order of the bytes of the int</param>
        /// <returns>
        /// an int from the current position in the given buffer, read using
        /// the given ByteOrder
        /// </returns>
        public static int GetInt(ByteBuffer buffer, ByteOrder order)
        {
            int offset = buffer.Position();
            int rtn = GetInt(buffer, offset, order);
            buffer.Position(offset + 4);
            return rtn;
        }

        /// <param name="buffer">Buffer containing the bytes</param>
        /// <param name="offset">Offset at which to start reading the int</param>
        /// <param name="order">the order of the bytes of the int</param>
        /// <returns>
        /// an int from the given position in the given buffer, read using
        /// the given ByteOrder
        /// </returns>
        public static int GetInt(ByteBuffer buffer, int offset, ByteOrder order)
        {
            ByteOrder origOrder = buffer.Order();
            try
            {
                return buffer.Order(order).GetInt(offset);
            }
            finally
            {
                buffer.Order(origOrder);
            }
        }

        /// <summary>
        /// Writes an int at the current position in the given buffer, using the
        /// given ByteOrder
        /// </summary>
        /// <param name="buffer">buffer into which to insert the int</param>
        /// <param name="val">Int to insert</param>
        /// <param name="order">the order to insert the bytes of the int</param>
        public static void PutInt(ByteBuffer buffer, int val, ByteOrder order)
        {
            int offset = buffer.Position();
            PutInt(buffer, val, offset, order);
            buffer.Position(offset + 4);
        }

        /// <summary>
        /// Writes an int at the given position in the given buffer, using the
        /// given ByteOrder
        /// </summary>
        /// <param name="buffer">buffer into which to insert the int</param>
        /// <param name="val">Int to insert</param>
        /// <param name="offset">offset at which to insert the int</param>
        /// <param name="order">the order to insert the bytes of the int</param>
        public static void PutInt(ByteBuffer buffer, int val, int offset, ByteOrder order
            )
        {
            ByteOrder origOrder = buffer.Order();
            try
            {
                buffer.Order(order).PutInt(offset, val);
            }
            finally
            {
                buffer.Order(origOrder);
            }
        }

        /// <summary>Read an unsigned variable length int from a buffer</summary>
        /// <param name="buffer">Buffer containing the variable length int</param>
        /// <returns>The unsigned int</returns>
        public static int GetUnsignedVarInt(ByteBuffer buffer, int numBytes)
        {
            int pos = buffer.Position();
            int rtn = GetUnsignedVarInt(buffer, pos, numBytes);
            buffer.Position(pos + numBytes);
            return rtn;
        }

        /// <summary>Read an unsigned variable length int from a buffer</summary>
        /// <param name="buffer">Buffer containing the variable length int</param>
        /// <param name="offset">Offset at which to read the value</param>
        /// <returns>The unsigned int</returns>
        public static int GetUnsignedVarInt(ByteBuffer buffer, int offset, int numBytes)
        {
            switch (numBytes)
            {
                case 1:
                    {
                        return GetUnsignedByte(buffer, offset);
                    }

                case 2:
                    {
                        return GetUnsignedShort(buffer, offset);
                    }

                case 3:
                    {
                        return Get3ByteInt(buffer, offset);
                    }

                case 4:
                    {
                        return buffer.GetInt(offset);
                    }

                default:
                    {
                        throw new ArgumentException("Invalid num bytes " + numBytes);
                    }
            }
        }

        /// <summary>Sets all bits in the given remaining byte range to 0.</summary>
        /// <remarks>Sets all bits in the given remaining byte range to 0.</remarks>
        public static void ClearRemaining(ByteBuffer buffer)
        {
            if (!buffer.HasRemaining())
            {
                return;
            }
            int pos = buffer.Position();
            ClearRange(buffer, pos, pos + buffer.Remaining());
        }

        /// <summary>Sets all bits in the given byte range to 0.</summary>
        /// <remarks>Sets all bits in the given byte range to 0.</remarks>
        public static void ClearRange(ByteBuffer buffer, int start, int end)
        {
            PutRange(buffer, start, end, unchecked((byte)unchecked((int)(0x00))));
        }

        /// <summary>Sets all bits in the given byte range to 1.</summary>
        /// <remarks>Sets all bits in the given byte range to 1.</remarks>
        public static void FillRange(ByteBuffer buffer, int start, int end)
        {
            PutRange(buffer, start, end, unchecked((byte)unchecked((int)(0xff))));
        }

        /// <summary>Sets all bytes in the given byte range to the given byte value.</summary>
        /// <remarks>Sets all bytes in the given byte range to the given byte value.</remarks>
        public static void PutRange(ByteBuffer buffer, int start, int end, byte b)
        {
            for (int i = start; i < end; ++i)
            {
                buffer.Put(i, b);
            }
        }

        /// <summary>
        /// Matches a pattern of bytes against the given buffer starting at the given
        /// offset.
        /// </summary>
        /// <remarks>
        /// Matches a pattern of bytes against the given buffer starting at the given
        /// offset.
        /// </remarks>
        public static bool MatchesRange(ByteBuffer buffer, int start, byte[] pattern)
        {
            for (int i = 0; i < pattern.Length; ++i)
            {
                if (pattern[i] != buffer.Get(start + i))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Searches for a pattern of bytes in the given buffer starting at the
        /// given offset.
        /// </summary>
        /// <remarks>
        /// Searches for a pattern of bytes in the given buffer starting at the
        /// given offset.
        /// </remarks>
        /// <returns>the offset of the pattern if a match is found, -1 otherwise</returns>
        public static int FindRange(ByteBuffer buffer, int start, byte[] pattern)
        {
            byte firstByte = pattern[0];
            int limit = buffer.Limit() - pattern.Length;
            for (int i = start; i < limit; ++i)
            {
                if ((firstByte == buffer.Get(i)) && MatchesRange(buffer, i, pattern))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Convert a byte buffer to a hexadecimal string for display</summary>
        /// <param name="buffer">Buffer to display, starting at offset 0</param>
        /// <param name="size">Number of bytes to read from the buffer</param>
        /// <returns>The display String</returns>
        public static string ToHexString(ByteBuffer buffer, int size)
        {
            return ToHexString(buffer, 0, size);
        }

        /// <summary>Convert a byte array to a hexadecimal string for display</summary>
        /// <param name="array">byte array to display, starting at offset 0</param>
        /// <returns>The display String</returns>
        public static string ToHexString(byte[] array)
        {
            return ToHexString(ByteBuffer.Wrap(array), 0, array.Length);
        }

        /// <summary>Convert a byte buffer to a hexadecimal string for display</summary>
        /// <param name="buffer">Buffer to display, starting at offset 0</param>
        /// <param name="offset">Offset at which to start reading the buffer</param>
        /// <param name="size">Number of bytes to read from the buffer</param>
        /// <returns>The display String</returns>
        public static string ToHexString(ByteBuffer buffer, int offset, int size)
        {
            return ToHexString(buffer, offset, size, true);
        }

        /// <summary>Convert a byte buffer to a hexadecimal string for display</summary>
        /// <param name="buffer">Buffer to display, starting at offset 0</param>
        /// <param name="offset">Offset at which to start reading the buffer</param>
        /// <param name="size">Number of bytes to read from the buffer</param>
        /// <param name="formatted">flag indicating if formatting is required</param>
        /// <returns>The display String</returns>
        public static string ToHexString(ByteBuffer buffer, int offset, int size, bool formatted
            )
        {
            StringBuilder rtn = new StringBuilder();
            int position = buffer.Position();
            buffer.Position(offset);
            for (int i = 0; i < size; i++)
            {
                byte b = buffer.Get();
                byte h = unchecked((byte)(b & unchecked((int)(0xF0))));
                h = unchecked((byte)(h >> 4));
                h = unchecked((byte)(h & unchecked((int)(0x0F))));
                rtn.Append(HEX_CHARS[h]);
                h = unchecked((byte)(b & unchecked((int)(0x0F))));
                rtn.Append(HEX_CHARS[h]);
                int next = (i + 1);
                if (formatted && (next < size))
                {
                    if ((next % NUM_BYTES_PER_LINE) == 0)
                    {
                        rtn.Append("\n");
                    }
                    else
                    {
                        rtn.Append(" ");
                        if ((next % NUM_BYTES_PER_BLOCK) == 0)
                        {
                            rtn.Append(" ");
                        }
                    }
                }
            }
            buffer.Position(position);
            return rtn.ToString();
        }

        /// <summary>
        /// Convert the given number of bytes from the given database page to a
        /// hexidecimal string for display.
        /// </summary>
        /// <remarks>
        /// Convert the given number of bytes from the given database page to a
        /// hexidecimal string for display.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public static string ToHexString(Database db, int pageNumber, int size)
        {
            ByteBuffer buffer = db.GetPageChannel().CreatePageBuffer();
            db.GetPageChannel().ReadPage(buffer, pageNumber);
            return ToHexString(buffer, size);
        }

        /// <summary>
        /// Writes a sequence of hexidecimal values into the given buffer, where
        /// every two characters represent one byte value.
        /// </summary>
        /// <remarks>
        /// Writes a sequence of hexidecimal values into the given buffer, where
        /// every two characters represent one byte value.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public static void WriteHexString(ByteBuffer buffer, string hexStr)
        {
            char[] hexChars = hexStr.ToCharArray();
            if ((hexChars.Length % 2) != 0)
            {
                throw new IOException("Hex string length must be even");
            }
            for (int i = 0; i < hexChars.Length; i += 2)
            {
                string tmpStr = new string(hexChars, i, 2);
                buffer.Put(unchecked((byte)long.Parse(tmpStr, System.Globalization.NumberStyles.HexNumber)));
            }
        }

        /// <summary>Writes a chunk of data to a file in pretty printed hexidecimal.</summary>
        /// <remarks>Writes a chunk of data to a file in pretty printed hexidecimal.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public static void ToHexFile(string fileName, ByteBuffer buffer, int offset, int
            size)
        {
            PrintWriter writer = new PrintWriter(new FileWriter(fileName));
            try
            {
                writer.WriteLine(ToHexString(buffer, offset, size));
            }
            finally
            {
                writer.Close();
            }
        }

        /// <returns>the byte value converted to an unsigned int value</returns>
        public static int AsUnsignedByte(byte b)
        {
            return b & unchecked((int)(0xFF));
        }

        /// <returns>the short value converted to an unsigned int value</returns>
        public static int AsUnsignedShort(short s)
        {
            return s & unchecked((int)(0xFFFF));
        }

        /// <summary>Swaps the 4 bytes (changes endianness) of the bytes at the given offset.
        /// 	</summary>
        /// <remarks>Swaps the 4 bytes (changes endianness) of the bytes at the given offset.
        /// 	</remarks>
        /// <param name="bytes">buffer containing bytes to swap</param>
        /// <param name="offset">offset of the first byte of the bytes to swap</param>
        public static void Swap4Bytes(byte[] bytes, int offset)
        {
            byte b = bytes[offset + 0];
            bytes[offset + 0] = bytes[offset + 3];
            bytes[offset + 3] = b;
            b = bytes[offset + 1];
            bytes[offset + 1] = bytes[offset + 2];
            bytes[offset + 2] = b;
        }

        /// <summary>Swaps the 2 bytes (changes endianness) of the bytes at the given offset.
        /// 	</summary>
        /// <remarks>Swaps the 2 bytes (changes endianness) of the bytes at the given offset.
        /// 	</remarks>
        /// <param name="bytes">buffer containing bytes to swap</param>
        /// <param name="offset">offset of the first byte of the bytes to swap</param>
        public static void Swap2Bytes(byte[] bytes, int offset)
        {
            byte b = bytes[offset + 0];
            bytes[offset + 0] = bytes[offset + 1];
            bytes[offset + 1] = b;
        }

        /// <summary>
        /// Moves the position of the given buffer the given count from the current
        /// position.
        /// </summary>
        /// <remarks>
        /// Moves the position of the given buffer the given count from the current
        /// position.
        /// </remarks>
        /// <returns>the new buffer position</returns>
        public static int Forward(ByteBuffer buffer, int count)
        {
            int newPos = buffer.Position() + count;
            buffer.Position(newPos);
            return newPos;
        }

        /// <summary>Returns a copy of the the given array of the given length.</summary>
        /// <remarks>Returns a copy of the the given array of the given length.</remarks>
        public static byte[] CopyOf(byte[] arr, int newLength)
        {
            return CopyOf(arr, 0, newLength);
        }

        /// <summary>
        /// Returns a copy of the the given array of the given length starting at the
        /// given position.
        /// </summary>
        /// <remarks>
        /// Returns a copy of the the given array of the given length starting at the
        /// given position.
        /// </remarks>
        public static byte[] CopyOf(byte[] arr, int offset, int newLength)
        {
            byte[] newArr = new byte[newLength];
            int srcLen = arr.Length - offset;
            System.Array.Copy(arr, offset, newArr, 0, Math.Min(srcLen, newLength));
            return newArr;
        }

        /// <summary>
        /// Utility byte stream similar to ByteArrayOutputStream but with extended
        /// accessibility to the bytes.
        /// </summary>
        /// <remarks>
        /// Utility byte stream similar to ByteArrayOutputStream but with extended
        /// accessibility to the bytes.
        /// </remarks>
        public class ByteStream
        {
            private byte[] _bytes;

            private int _length;

            private int _lastLength;

            public ByteStream() : this(32)
            {
            }

            public ByteStream(int capacity)
            {
                _bytes = new byte[capacity];
            }

            public virtual int GetLength()
            {
                return _length;
            }

            public virtual byte[] GetBytes()
            {
                return _bytes;
            }

            protected internal virtual void EnsureNewCapacity(int numBytes)
            {
                int newLength = _length + numBytes;
                if (newLength > _bytes.Length)
                {
                    byte[] temp = new byte[newLength * 2];
                    System.Array.Copy(_bytes, 0, temp, 0, _length);
                    _bytes = temp;
                }
            }

            public virtual void Write(int b)
            {
                EnsureNewCapacity(1);
                _bytes[_length++] = unchecked((byte)b);
            }

            public virtual void Write(byte[] b)
            {
                Write(b, 0, b.Length);
            }

            public virtual void Write(byte[] b, int offset, int length)
            {
                EnsureNewCapacity(length);
                System.Array.Copy(b, offset, _bytes, _length, length);
                _length += length;
            }

            public virtual byte Get(int offset)
            {
                return _bytes[offset];
            }

            public virtual void Set(int offset, byte b)
            {
                _bytes[offset] = b;
            }

            public virtual void WriteFill(int length, byte b)
            {
                EnsureNewCapacity(length);
                int oldLength = _length;
                _length += length;
                Arrays.Fill(_bytes, oldLength, _length, b);
            }

            public virtual void WriteTo(ByteUtil.ByteStream @out)
            {
                @out.Write(_bytes, 0, _length);
            }

            public virtual byte[] ToByteArray()
            {
                byte[] result = null;
                if (_length == _bytes.Length)
                {
                    result = _bytes;
                    _bytes = null;
                }
                else
                {
                    result = CopyOf(_bytes, _length);
                    if (_lastLength == _length)
                    {
                        // if we get the same result length bytes twice in a row, clear the
                        // _bytes so that the next _bytes will be _lastLength
                        _bytes = null;
                    }
                }
                // save result length so we can potentially get the right length of the
                // next byte[] in reset()
                _lastLength = _length;
                return result;
            }

            public virtual void Reset()
            {
                _length = 0;
                if (_bytes == null)
                {
                    _bytes = new byte[_lastLength];
                }
            }

            public virtual void TrimTrailing(byte minTrimCode, byte maxTrimCode)
            {
                int minTrim = ByteUtil.AsUnsignedByte(minTrimCode);
                int maxTrim = ByteUtil.AsUnsignedByte(maxTrimCode);
                int idx = _length - 1;
                while (idx >= 0)
                {
                    int val = AsUnsignedByte(Get(idx));
                    if ((val >= minTrim) && (val <= maxTrim))
                    {
                        --idx;
                    }
                    else
                    {
                        break;
                    }
                }
                _length = idx + 1;
            }
        }
    }
}
