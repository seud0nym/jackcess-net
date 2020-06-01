// 
// BitSet.cs
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
using System.Collections.Generic;

namespace Sharpen
{
	public class BitSet
	{
		List<bool> bits;
		
		public BitSet ()
		{
			bits = new List<bool> ();
		}
		
		public BitSet (int size)
		{
			bits = new List<bool> (size);
			for (int i = 0; i < size; i++)
				bits[i] = false;
		}

		public int Cardinality()
		{
			int sum = 0;
			foreach (bool b in bits)
			{
				if (b)
					sum++;
			}
			return sum;
		}

		public void Set(int index)
		{
			if (index < 0)
				throw new IndexOutOfRangeException("index < 0: " + index);

			while (index >= bits.Count)
				bits.Add(false);
			bits[index] = true;
		}

		public void Set(int index, bool value)
		{
			if (value)
				Set(index);
			else
				Clear(index);
		}

		public void Set(int fromIndex, int toIndex)
		{
			if (fromIndex < 0)
				throw new IndexOutOfRangeException("fromIndex < 0: " + fromIndex);

			while (fromIndex >= bits.Count)
				bits.Add(false);

			for (int i = fromIndex; i < Math.Min(toIndex, bits.Count); i ++)
				bits[i] = true;

			while (toIndex >= bits.Count)
				bits.Add(true);
		}

		public bool Get (int index)
		{
			if (index >= bits.Count)
				return false;
			else
				return bits [index];
		}

		public void Clear()
		{
			for (int i = 0; i < bits.Count; i++)
				bits[i] = false;
		}

		public void Clear(int index)
		{
			if (index < 0)
				throw new IndexOutOfRangeException("bitIndex < 0: " + index);

			while (index >= bits.Count)
				bits.Add(false);
			bits[index] = false;
		}

		public object Clone()
		{
			BitSet clone = new BitSet(bits.Count);
			for (int i = 0; i < bits.Count; i++)
			{
				if (bits[i])
					clone.Set(i);
			}
			return clone;
		}
		public int Length()
		{
			return bits.Count;
		}

		public int NextSetBit(int fromIndex)
		{
			if (fromIndex < 0)
				throw new IndexOutOfRangeException("fromIndex < 0: " + fromIndex);

			int index = -1;
			for (int i = fromIndex; i < bits.Count; i++)
			{
				if (bits[i])
				{
					index = i;
					break;
				}
			}

			return index;
		}
	}
}

