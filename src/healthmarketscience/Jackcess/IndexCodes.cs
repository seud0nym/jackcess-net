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
	/// <summary>Various constants used for creating index entries.</summary>
	/// <remarks>Various constants used for creating index entries.</remarks>
	/// <author>James Ahlborn</author>
	public class IndexCodes
	{
		internal const byte ASC_START_FLAG = unchecked((byte)unchecked((int)(0x7F)));

		internal const byte ASC_NULL_FLAG = unchecked((byte)unchecked((int)(0x00)));

		internal const byte DESC_START_FLAG = unchecked((byte)unchecked((int)(0x80)));

		internal const byte DESC_NULL_FLAG = unchecked((byte)unchecked((int)(0xFF)));

		internal const byte MID_GUID = unchecked((byte)unchecked((int)(0x09)));

		internal const byte ASC_END_GUID = unchecked((byte)unchecked((int)(0x08)));

		internal const byte DESC_END_GUID = unchecked((byte)unchecked((int)(0xF7)));

		internal const byte ASC_BOOLEAN_TRUE = unchecked((byte)unchecked((int)(0x00)));

		internal const byte ASC_BOOLEAN_FALSE = unchecked((byte)unchecked((int)(0xFF)));

		internal const byte DESC_BOOLEAN_TRUE = ASC_BOOLEAN_FALSE;

		internal const byte DESC_BOOLEAN_FALSE = ASC_BOOLEAN_TRUE;

		internal static bool IsNullEntry(byte startEntryFlag)
		{
			return ((startEntryFlag == ASC_NULL_FLAG) || (startEntryFlag == DESC_NULL_FLAG));
		}

		internal static byte GetNullEntryFlag(bool isAscending)
		{
			return (isAscending ? ASC_NULL_FLAG : DESC_NULL_FLAG);
		}

		internal static byte GetStartEntryFlag(bool isAscending)
		{
			return (isAscending ? ASC_START_FLAG : DESC_START_FLAG);
		}
	}
}
