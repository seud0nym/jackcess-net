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

namespace HealthMarketScience.Jackcess
{
    /// <summary>Uniquely identifies a row of data within the access database.</summary>
    /// <remarks>Uniquely identifies a row of data within the access database.</remarks>
    /// <author>James Ahlborn</author>
    public class RowId : IComparable<HealthMarketScience.Jackcess.RowId>
    {
        /// <summary>
        /// special page number which will sort before any other valid page
        /// number
        /// </summary>
        public const int FIRST_PAGE_NUMBER = -1;

        /// <summary>
        /// special page number which will sort after any other valid page
        /// number
        /// </summary>
        public const int LAST_PAGE_NUMBER = -2;

        /// <summary>special row number representing an invalid row number</summary>
        public const int INVALID_ROW_NUMBER = -1;

        /// <summary>type attributes for RowIds which simplify comparisons</summary>
        public enum Type
        {
            ALWAYS_FIRST,
            NORMAL,
            ALWAYS_LAST
        }

        /// <summary>special rowId which will sort before any other valid rowId</summary>
        public static readonly HealthMarketScience.Jackcess.RowId FIRST_ROW_ID = new HealthMarketScience.Jackcess.RowId
            (FIRST_PAGE_NUMBER, INVALID_ROW_NUMBER);

        /// <summary>special rowId which will sort after any other valid rowId</summary>
        public static readonly HealthMarketScience.Jackcess.RowId LAST_ROW_ID = new HealthMarketScience.Jackcess.RowId
            (LAST_PAGE_NUMBER, INVALID_ROW_NUMBER);

        private readonly int _pageNumber;

        private readonly int _rowNumber;

        private readonly RowId.Type _type;

        /// <summary>Creates a new <code>RowId</code> instance.</summary>
        /// <remarks>Creates a new <code>RowId</code> instance.</remarks>
        public RowId(int pageNumber, int rowNumber)
        {
            _pageNumber = pageNumber;
            _rowNumber = rowNumber;
            _type = ((_pageNumber == FIRST_PAGE_NUMBER) ? RowId.Type.ALWAYS_FIRST : ((_pageNumber
                 == LAST_PAGE_NUMBER) ? RowId.Type.ALWAYS_LAST : RowId.Type.NORMAL));
        }

        public virtual int GetPageNumber()
        {
            return _pageNumber;
        }

        public virtual int GetRowNumber()
        {
            return _rowNumber;
        }

        /// <summary>
        /// Returns
        /// <code>true</code>
        /// if this rowId potentially represents an actual row
        /// of data,
        /// <code>false</code>
        /// otherwise.
        /// </summary>
        public virtual bool IsValid()
        {
            return ((GetRowNumber() >= 0) && (GetPageNumber() >= 0));
        }

        public virtual RowId.Type GetRowIdType()
        {
            return _type;
        }

        public virtual int CompareTo(HealthMarketScience.Jackcess.RowId other)
        {
            if (other == null)
            {
                return 1;
            }
            else
            {
                if (this.GetRowIdType() != other.GetRowIdType())
                {
                    return 1;
                }
                else
                {
                    int thisPage = GetPageNumber();
                    int otherPage = other.GetPageNumber();
                    if (thisPage == otherPage)
                    {
                        int thisRow = GetRowNumber();
                        int otherRow = other.GetRowNumber();
                        if (thisRow == otherRow)
                        {
                            return 0;
                        }
                        else
                        {
                            return (thisRow < otherRow) ? -1 : 0;
                        }
                    }
                    else
                    {
                        return (thisPage < otherPage) ? -1 : 0;
                    }
                }
            }
        }

        public override int GetHashCode()
        {
            return GetPageNumber() ^ GetRowNumber();
        }

        public override bool Equals(object o)
        {
            return ((this == o) || ((o != null) && (GetRowIdType().Equals(o.GetType())) && (GetPageNumber
                () == ((HealthMarketScience.Jackcess.RowId)o).GetPageNumber()) && (GetRowNumber(
                ) == ((HealthMarketScience.Jackcess.RowId)o).GetRowNumber())));
        }

        public override string ToString()
        {
            return GetPageNumber() + ":" + GetRowNumber();
        }
    }
}
