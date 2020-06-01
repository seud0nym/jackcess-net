namespace Sharpen
{
    using System;

    public class ByteBuffer
    {
        private byte[] buffer;
        private DataConverter c;
        private int capacity;
        private int index;
        private int limit;
        private int mark;
        private int offset;
        private ByteOrder order;

        bool bigEndian = true;

        public ByteBuffer()
        {
            this.c = DataConverter.BigEndian;
        }

        private ByteBuffer(byte[] buf, int start, int len)
        {
            this.buffer = buf;
            this.offset = 0;
            this.limit = start + len;
            this.index = start;
            this.mark = start;
            this.capacity = buf.Length;
            this.c = DataConverter.BigEndian;
        }

        public static ByteBuffer Allocate(int size)
        {
            return new ByteBuffer(new byte[size], 0, size);
        }

        public static ByteBuffer AllocateDirect(int size)
        {
            return Allocate(size);
        }

        public byte[] Array()
        {
            return buffer;
        }

        public int ArrayOffset()
        {
            return offset;
        }

        public int Capacity()
        {
            return capacity;
        }

        private void CheckGetLimit(int inc)
        {
            if ((index + inc) > limit)
            {
                throw new BufferUnderflowException();
            }
        }

        private void CheckPutLimit(int inc)
        {
            if ((index + inc) > limit)
            {
                throw new BufferUnderflowException();
            }
        }

        public ByteBuffer Clear()
        {
            index = offset;
            limit = offset + capacity;
            return this;
        }

        public ByteBuffer Duplicate()
        {
            return new ByteBuffer(buffer, index, buffer.Length - index);
        }

        public void Flip()
        {
            limit = index;
            index = offset;
        }

        public byte Get()
        {
            CheckGetLimit(1);
            return buffer[index++];
        }

        public byte Get(int idx)
        {
            return buffer[idx];
        }

        public void Get(byte[] data)
        {
            Get(data, 0, data.Length);
        }

        public void Get(byte[] data, int start, int len)
        {
            CheckGetLimit(len);
            for (int i = 0; i < len; i++)
            {
                data[i + start] = buffer[index++];
            }
        }

        public double GetDouble()
        {
            CheckGetLimit(8);
            double num = c.GetDouble(buffer, index);
            index += 8;
            return num;
        }

        public float GetFloat()
        {
            CheckGetLimit(4);
            float num = c.GetFloat(buffer, index);
            index += 4;
            return num;
        }

        public int GetInt()
        {
            CheckGetLimit(4);
            int num = c.GetInt32(buffer, index);
            index += 4;
            return num;
        }

        public int GetInt(int idx)
        {
            return c.GetInt32(buffer, idx);
        }

        public long GetLong()
        {
            CheckGetLimit(8);
            long num = c.GetInt64(buffer, index);
            index += 8;
            return num;
        }

        public long GetLong(int idx)
        {
            return c.GetInt64(buffer, idx);
        }

        public short GetShort()
        {
            CheckGetLimit(2);
            short num = c.GetInt16(buffer, index);
            index += 2;
            return num;
        }

        public short GetShort(int idx)
        {
            return c.GetInt16(buffer, idx);
        }

        public bool HasArray()
        {
            return true;
        }

        public bool HasRemaining()
        {
            return index < limit;
        }

        public int Limit()
        {
            return (limit - offset);
        }

        public ByteBuffer Limit(int newLimit)
        {
            limit = newLimit;
            return this;
        }

        public ByteBuffer Mark()
        {
            mark = index;
            return this;
        }

        public ByteOrder Order()
        {
            return bigEndian ? ByteOrder.BIG_ENDIAN : ByteOrder.LITTLE_ENDIAN;
        }

        public ByteBuffer Order(ByteOrder order)
        {
            this.order = order;
            if (order == ByteOrder.BIG_ENDIAN)
            {
                bigEndian = true;
                c = DataConverter.BigEndian;
            }
            else
            {
                bigEndian = false;
                c = DataConverter.LittleEndian;
            }
            return this;
        }

        public int Position()
        {
            return (index - offset);
        }

        public ByteBuffer Position(int pos)
        {
            if ((pos < offset) || (pos > limit))
            {
                throw new BufferUnderflowException();
            }
            index = pos + offset;
            return this;
        }

        public void Put(Number number)
        {
            if (number is BigDecimal || number is Double)
            {
                Put(c.GetBytes(number.DoubleValue()));
            } 
            else if (number is Byte)
            {
                Put(number.ByteValue());
            }
            else if (number is Float)
            {
                Put(c.GetBytes(number.FloatValue()));
            }
            else if (number is Integer)
            {
                Put(c.GetBytes(number.IntValue()));
            }
            else if (number is Long)
            {
                Put(c.GetBytes(number.LongValue()));
            }
            else if (number is Short)
            {
                Put(c.GetBytes(number.ShortValue()));
            }
        }

        public void Put(ByteBuffer buf)
        {
            Put(buf.Array());
        }

        public void Put(byte data)
        {
            CheckPutLimit(1);
            buffer[index++] = data;
        }

        public void Put(byte[] data)
        {
            Put(data, 0, data.Length);
        }

        public void Put(byte[] data, int start, int len)
        {
            CheckPutLimit(len);
            for (int i = 0; i < len; i++)
            {
                buffer[index++] = data[i + start];
            }
        }

        public void Put(int idx, byte data)
        {
            buffer[idx] = data;
        }

        public void Put(int idx, byte[] data)
        {
            for (int offset = 0; offset < data.Length; offset++)
            {
                Put(idx + offset, data[offset]);
            }
        }

        public void PutDouble(double i)
        {
            Put(c.GetBytes(i));
        }

        public void PutFloat(float i)
        {
            Put(c.GetBytes(i));
        }

        public void PutInt(int i)
        {
            Put(c.GetBytes(i));
        }

        public void PutInt(int idx, int i)
        {
            Put(idx, c.GetBytes(i));
        }

        public void PutLong(long i)
        {
            Put(c.GetBytes(i));
        }

        public void PutShort(short i)
        {
            Put(c.GetBytes(i));
        }

        public void PutShort(int idx, short i)
        {
            Put(idx, c.GetBytes(i));
        }

        public int Remaining()
        {
            return (limit - index);
        }

        public void Reset()
        {
            index = mark;
        }

        public void Rewind()
        {
            index = 0;
            mark = -1;
        }


        public ByteBuffer Slice()
        {
            ByteBuffer b = Wrap(buffer, index, buffer.Length - index);
            b.offset = index;
            b.limit = limit;
            b.order = order;
            b.c = c;
            b.capacity = limit - index;
            return b;
        }

        public static ByteBuffer Wrap(byte[] buf)
        {
            return new ByteBuffer(buf, 0, buf.Length);
        }

        public static ByteBuffer Wrap(byte[] buf, int start, int len)
        {
            return new ByteBuffer(buf, start, len);
        }
    }
}
