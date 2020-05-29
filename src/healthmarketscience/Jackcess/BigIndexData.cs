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

namespace HealthMarketScience.Jackcess
{
    /// <summary>Implementation of an Access table index which supports large indexes.</summary>
    /// <remarks>Implementation of an Access table index which supports large indexes.</remarks>
    /// <author>James Ahlborn</author>
    public class BigIndexData : IndexData
    {
        /// <summary>Cache which manages the index pages</summary>
        private readonly IndexPageCache _pageCache;

        protected internal BigIndexData(Table table, int number, int uniqueEntryCount, int
             uniqueEntryCountOffset) : base(table, number, uniqueEntryCount, uniqueEntryCountOffset
            )
        {
            _pageCache = new IndexPageCache(this);
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override void UpdateImpl()
        {
            _pageCache.Write();
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override void ReadIndexEntries()
        {
            _pageCache.SetRootPageNumber(GetRootPageNumber());
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override IndexData.DataPage FindDataPage(IndexData.Entry entry
            )
        {
            return _pageCache.FindCacheDataPage(entry);
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override IndexData.DataPage GetDataPage(int pageNumber)
        {
            return _pageCache.GetCacheDataPage(pageNumber);
        }

        public override string ToString()
        {
            return base.ToString() + "\n" + _pageCache.ToString();
        }

        /// <summary>Used by unit tests to validate the internal status of the index.</summary>
        /// <remarks>Used by unit tests to validate the internal status of the index.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        internal virtual void Validate()
        {
            _pageCache.Validate();
        }
    }
}
