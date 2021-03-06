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

namespace HealthMarketScience.Jackcess
{
    /// <summary>
    /// Concrete implementation of ColumnMatcher which tests textual columns
    /// case-insensitively (
    /// <see cref="DataType.TEXT">DataType.TEXT</see>
    /// and
    /// <see cref="DataType.MEMO">DataType.MEMO</see>
    /// ), and
    /// all other columns using simple equality.
    /// </summary>
    /// <author>James Ahlborn</author>
    public class CaseInsensitiveColumnMatcher : ColumnMatcher
    {
        public static readonly HealthMarketScience.Jackcess.CaseInsensitiveColumnMatcher
            INSTANCE = new HealthMarketScience.Jackcess.CaseInsensitiveColumnMatcher();

        public CaseInsensitiveColumnMatcher()
        {
        }

        public virtual bool Matches(Table table, string columnName, object value1, object
             value2)
        {
            if (!DataTypeUtil.IsTextual(table.GetColumn(columnName).GetDataType()))
            {
                // use simple equality
                return SimpleColumnMatcher.INSTANCE.Matches(table, columnName, value1, value2);
            }
            // convert both values to Strings and compare case-insensitively
            try
            {
                CharSequence cs1 = Column.ToCharSequence(value1);
                CharSequence cs2 = Column.ToCharSequence(value2);
                return ((cs1 == cs2) || ((cs1 != null) && (cs2 != null) && Sharpen.Runtime.EqualsIgnoreCase
                    (cs1.ToString(), cs2.ToString())));
            }
            catch (IOException e)
            {
                throw new InvalidOperationException("Could not read column " + columnName + " value"
                    , e);
            }
        }
    }
}
