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

using System;
using HealthMarketScience.Jackcess;
using HealthMarketScience.Jackcess.Scsu;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>
	/// Implementation of ErrorHandler which is useful for generating debug
	/// information about bad row data (great for bug reports!).
	/// </summary>
	/// <remarks>
	/// Implementation of ErrorHandler which is useful for generating debug
	/// information about bad row data (great for bug reports!).  After logging a
	/// debug entry for the failed column, it will return some sort of replacement
	/// value, see
	/// <see cref="ReplacementErrorHandler">ReplacementErrorHandler</see>
	/// .
	/// </remarks>
	/// <author>James Ahlborn</author>
	public class DebugErrorHandler : ReplacementErrorHandler
	{
		/// <summary>
		/// Constructs a DebugErrorHandler which replaces all errored values with
		/// <code>null</code>
		/// .
		/// </summary>
		public DebugErrorHandler()
		{
		}

		/// <summary>
		/// Constructs a DebugErrorHandler which replaces all errored values with the
		/// given Object.
		/// </summary>
		/// <remarks>
		/// Constructs a DebugErrorHandler which replaces all errored values with the
		/// given Object.
		/// </remarks>
		public DebugErrorHandler(object replacement) : base(replacement)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override object HandleRowError(Column column, byte[] columnData, Table.RowState
			 rowState, Exception error)
		{
			if (Debug.IsDebugEnabled())
			{
				Debug.Out("Failed reading column " + column + ", row " + rowState + ", bytes " + 
					((columnData != null) ? ByteUtil.ToHexString(columnData) : "null"), error);
			}
			return base.HandleRowError(column, columnData, rowState, error);
		}
	}
}
