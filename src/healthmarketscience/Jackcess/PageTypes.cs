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
	/// <summary>Codes for page types</summary>
	/// <author>Tim McCune</author>
	public abstract class PageTypes
	{
		/// <summary>invalid page type</summary>
		public const byte INVALID = unchecked((byte)unchecked((int)(0x00)));

		/// <summary>Data page</summary>
		public const byte DATA = unchecked((byte)unchecked((int)(0x01)));

		/// <summary>Table definition page</summary>
		public const byte TABLE_DEF = unchecked((byte)unchecked((int)(0x02)));

		/// <summary>intermediate index page pointing to other index pages</summary>
		public const byte INDEX_NODE = unchecked((byte)unchecked((int)(0x03)));

		/// <summary>leaf index page containing actual entries</summary>
		public const byte INDEX_LEAF = unchecked((byte)unchecked((int)(0x04)));

		/// <summary>Table usage map page</summary>
		public const byte USAGE_MAP = unchecked((byte)unchecked((int)(0x05)));
	}
}
