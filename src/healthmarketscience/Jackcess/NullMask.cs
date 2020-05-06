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

using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Bitmask that indicates whether or not each column in a row is null.</summary>
	/// <remarks>
	/// Bitmask that indicates whether or not each column in a row is null.  Also
	/// holds values of boolean columns.
	/// </remarks>
	/// <author>Tim McCune</author>
	public class NullMask
	{
		/// <summary>The actual bitmask</summary>
		private byte[] _mask;

		/// <param name="columnCount">
		/// Number of columns in the row that this mask will be
		/// used for
		/// </param>
		public NullMask(int columnCount)
		{
			// we leave everything initially marked as null so that we don't need to
			// do anything for deleted columns (we only need to mark as non-null
			// valid columns for which we actually have values).
			_mask = new byte[(columnCount + 7) / 8];
		}

		/// <summary>Read a mask in from a buffer</summary>
		public virtual void Read(ByteBuffer buffer)
		{
			buffer.Get(_mask);
		}

		/// <summary>Write a mask to a buffer</summary>
		public virtual void Write(ByteBuffer buffer)
		{
			buffer.Put(_mask);
		}

		/// <param name="column">
		/// column to test for
		/// <code>null</code>
		/// </param>
		/// <returns>
		/// Whether or not the value for that column is null.  For boolean
		/// columns, returns the actual value of the column (where
		/// non-
		/// <code>null</code>
		/// ==
		/// <code>true</code>
		/// )
		/// </returns>
		public virtual bool IsNull(Column column)
		{
			int columnNumber = column.GetColumnNumber();
			int maskIndex = columnNumber / 8;
			// if new columns were added to the table, old null masks may not include
			// them (meaning the field is null)
			if (maskIndex >= _mask.Length)
			{
				// it's null
				return true;
			}
			return (_mask[maskIndex] & unchecked((byte)(1 << (columnNumber % 8)))) == 0;
		}

		/// <summary>
		/// Indicate that the column with the given number is not
		/// <code>null</code>
		/// (or a
		/// boolean value is
		/// <code>true</code>
		/// ).
		/// </summary>
		/// <param name="column">
		/// column to be marked non-
		/// <code>null</code>
		/// </param>
		public virtual void MarkNotNull(Column column)
		{
			int columnNumber = column.GetColumnNumber();
			int maskIndex = columnNumber / 8;
			_mask[maskIndex] = unchecked((byte)(_mask[maskIndex] | unchecked((byte)(1 << (columnNumber
				 % 8)))));
		}

		/// <returns>Size in bytes of this mask</returns>
		public virtual int ByteSize()
		{
			return _mask.Length;
		}
	}
}
