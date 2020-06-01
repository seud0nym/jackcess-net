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

using HealthMarketScience.Jackcess.Scsu;
using Sharpen;
using System;
using System.IO;
using System.Text;

namespace HealthMarketScience.Jackcess
{
    /// <summary>Describes which database pages a particular table uses</summary>
    /// <author>Tim McCune</author>
    public class UsageMap
    {
        /// <summary>Inline map type</summary>
        public const byte MAP_TYPE_INLINE = unchecked((int)(0x0));

        /// <summary>Reference map type, for maps that are too large to fit inline</summary>
        public const byte MAP_TYPE_REFERENCE = unchecked((int)(0x1));

        /// <summary>bit index value for an invalid page number</summary>
        private const int INVALID_BIT_INDEX = -1;

        /// <summary>owning database</summary>
        private readonly Database _database;

        /// <summary>Page number of the map table declaration</summary>
        private readonly int _tablePageNum;

        /// <summary>Offset of the data page at which the usage map data starts</summary>
        private int _startOffset;

        /// <summary>Offset of the data page at which the usage map declaration starts</summary>
        private readonly short _rowStart;

        /// <summary>First page that this usage map applies to</summary>
        private int _startPage;

        /// <summary>Last page that this usage map applies to</summary>
        private int _endPage;

        /// <summary>bits representing page numbers used, offset from _startPage</summary>
        private BitSet _pageNumbers = new BitSet();

        /// <summary>Buffer that contains the usage map table declaration page</summary>
        private readonly ByteBuffer _tableBuffer;

        /// <summary>
        /// modification count on the usage map, used to keep the cursors in
        /// sync
        /// </summary>
        private int _modCount;

        /// <summary>
        /// the current handler implementation for reading/writing the specific
        /// usage map type.
        /// </summary>
        /// <remarks>
        /// the current handler implementation for reading/writing the specific
        /// usage map type.  note, this may change over time.
        /// </remarks>
        private UsageMap.Handler _handler;

        /// <summary>Error message prefix used when map type is unrecognized.</summary>
        /// <remarks>Error message prefix used when map type is unrecognized.</remarks>
        internal static readonly string MSG_PREFIX_UNRECOGNIZED_MAP = "Unrecognized map type: ";

        /// <param name="database">database that contains this usage map</param>
        /// <param name="tableBuffer">Buffer that contains this map's declaration</param>
        /// <param name="pageNum">Page number that this usage map is contained in</param>
        /// <param name="rowStart">Offset at which the declaration starts in the buffer</param>
        /// <exception cref="System.IO.IOException"></exception>
        private UsageMap(Database database, ByteBuffer tableBuffer, int pageNum, short rowStart
            )
        {
            _database = database;
            _tableBuffer = tableBuffer;
            _tablePageNum = pageNum;
            _rowStart = rowStart;
            _tableBuffer.Position(_rowStart + GetFormat().OFFSET_USAGE_MAP_START);
            _startOffset = _tableBuffer.Position();
            if (Debug.IsDebugEnabled())
            {
                Debug.Out("Usage map block:\n" + ByteUtil.ToHexString(_tableBuffer, _rowStart, tableBuffer
                    .Limit() - _rowStart));
            }
        }

        public virtual Database GetDatabase()
        {
            return _database;
        }

        public virtual JetFormat GetFormat()
        {
            return GetDatabase().GetFormat();
        }

        public virtual PageChannel GetPageChannel()
        {
            return GetDatabase().GetPageChannel();
        }

        /// <param name="database">database that contains this usage map</param>
        /// <param name="pageNum">Page number that this usage map is contained in</param>
        /// <param name="rowNum">Number of the row on the page that contains this usage map</param>
        /// <returns>
        /// Either an InlineUsageMap or a ReferenceUsageMap, depending on
        /// which type of map is found
        /// </returns>
        /// <exception cref="System.IO.IOException"></exception>
        public static HealthMarketScience.Jackcess.UsageMap Read(Database database, int pageNum
            , int rowNum, bool assumeOutOfRangeBitsOn)
        {
            JetFormat format = database.GetFormat();
            PageChannel pageChannel = database.GetPageChannel();
            ByteBuffer tableBuffer = pageChannel.CreatePageBuffer();
            pageChannel.ReadPage(tableBuffer, pageNum);
            short rowStart = Table.FindRowStart(tableBuffer, rowNum, format);
            int rowEnd = Table.FindRowEnd(tableBuffer, rowNum, format);
            tableBuffer.Limit(rowEnd);
            byte mapType = tableBuffer.Get(rowStart);
            HealthMarketScience.Jackcess.UsageMap rtn = new HealthMarketScience.Jackcess.UsageMap
                (database, tableBuffer, pageNum, rowStart);
            rtn.InitHandler(mapType, assumeOutOfRangeBitsOn);
            return rtn;
        }

        /// <exception cref="System.IO.IOException"></exception>
        private void InitHandler(byte mapType, bool assumeOutOfRangeBitsOn)
        {
            if (mapType == MAP_TYPE_INLINE)
            {
                _handler = new UsageMap.InlineHandler(this, assumeOutOfRangeBitsOn);
            }
            else
            {
                if (mapType == MAP_TYPE_REFERENCE)
                {
                    _handler = new UsageMap.ReferenceHandler(this);
                }
                else
                {
                    throw new IOException(MSG_PREFIX_UNRECOGNIZED_MAP + mapType);
                }
            }
        }

        public virtual UsageMap.PageCursor Cursor()
        {
            return new UsageMap.PageCursor(this);
        }

        public virtual int GetPageCount()
        {
            return _pageNumbers.Cardinality();
        }

        protected internal virtual short GetRowStart()
        {
            return _rowStart;
        }

        protected internal virtual int GetRowEnd()
        {
            return GetTableBuffer().Limit();
        }

        protected internal virtual void SetStartOffset(int startOffset)
        {
            _startOffset = startOffset;
        }

        protected internal virtual int GetStartOffset()
        {
            return _startOffset;
        }

        protected internal virtual ByteBuffer GetTableBuffer()
        {
            return _tableBuffer;
        }

        protected internal virtual int GetTablePageNumber()
        {
            return _tablePageNum;
        }

        protected internal virtual int GetStartPage()
        {
            return _startPage;
        }

        protected internal virtual int GetEndPage()
        {
            return _endPage;
        }

        protected internal virtual BitSet GetPageNumbers()
        {
            return _pageNumbers;
        }

        protected internal virtual void SetPageRange(int newStartPage, int newEndPage)
        {
            _startPage = newStartPage;
            _endPage = newEndPage;
        }

        protected internal virtual bool IsPageWithinRange(int pageNumber)
        {
            return ((pageNumber >= _startPage) && (pageNumber < _endPage));
        }

        protected internal virtual int GetFirstPageNumber()
        {
            return BitIndexToPageNumber(GetNextBitIndex(-1), RowId.LAST_PAGE_NUMBER);
        }

        protected internal virtual int GetNextPageNumber(int curPage)
        {
            return BitIndexToPageNumber(GetNextBitIndex(PageNumberToBitIndex(curPage)), RowId
                .LAST_PAGE_NUMBER);
        }

        protected internal virtual int GetNextBitIndex(int curIndex)
        {
            return _pageNumbers.NextSetBit(curIndex + 1);
        }

        protected internal virtual int GetLastPageNumber()
        {
            return BitIndexToPageNumber(GetPrevBitIndex(_pageNumbers.Length()), RowId.FIRST_PAGE_NUMBER
                );
        }

        protected internal virtual int GetPrevPageNumber(int curPage)
        {
            return BitIndexToPageNumber(GetPrevBitIndex(PageNumberToBitIndex(curPage)), RowId
                .FIRST_PAGE_NUMBER);
        }

        protected internal virtual int GetPrevBitIndex(int curIndex)
        {
            --curIndex;
            while ((curIndex >= 0) && !_pageNumbers.Get(curIndex))
            {
                --curIndex;
            }
            return curIndex;
        }

        protected internal virtual int BitIndexToPageNumber(int bitIndex, int invalidPageNumber
            )
        {
            return ((bitIndex >= 0) ? (_startPage + bitIndex) : invalidPageNumber);
        }

        protected internal virtual int PageNumberToBitIndex(int pageNumber)
        {
            return ((pageNumber >= 0) ? (pageNumber - _startPage) : INVALID_BIT_INDEX);
        }

        protected internal virtual void ClearTableAndPages()
        {
            // reset some values
            _pageNumbers.Clear();
            _startPage = 0;
            _endPage = 0;
            ++_modCount;
            // clear out the table data (everything except map type)
            int tableStart = GetRowStart() + 1;
            int tableEnd = GetRowEnd();
            ByteUtil.ClearRange(_tableBuffer, tableStart, tableEnd);
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal virtual void WriteTable()
        {
            // note, we only want to write the row data with which we are working
            GetPageChannel().WritePage(_tableBuffer, _tablePageNum, _rowStart);
        }

        /// <summary>Read in the page numbers in this inline map</summary>
        protected internal virtual void ProcessMap(ByteBuffer buffer, int bufferStartPage
            )
        {
            int byteCount = 0;
            while (buffer.HasRemaining())
            {
                byte b = buffer.Get();
                if (b != unchecked((byte)0))
                {
                    for (int i = 0; i < 8; i++)
                    {
                        if ((b & (1 << i)) != 0)
                        {
                            int pageNumberOffset = (byteCount * 8 + i) + bufferStartPage;
                            int pageNumber = BitIndexToPageNumber(pageNumberOffset, PageChannel.INVALID_PAGE_NUMBER
                                );
                            if (!IsPageWithinRange(pageNumber))
                            {
                                throw new InvalidOperationException("found page number " + pageNumber + " in usage map outside of expected range "
                                     + _startPage + " to " + _endPage);
                            }
                            _pageNumbers.Set(pageNumberOffset);
                        }
                    }
                }
                byteCount++;
            }
        }

        /// <summary>Determines if the given page number is contained in this map.</summary>
        /// <remarks>Determines if the given page number is contained in this map.</remarks>
        public virtual bool ContainsPageNumber(int pageNumber)
        {
            return _handler.ContainsPageNumber(pageNumber);
        }

        /// <summary>Add a page number to this usage map</summary>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void AddPageNumber(int pageNumber)
        {
            ++_modCount;
            _handler.AddOrRemovePageNumber(pageNumber, true, false);
        }

        /// <summary>Remove a page number from this usage map</summary>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void RemovePageNumber(int pageNumber)
        {
            RemovePageNumber(pageNumber, false);
        }

        /// <summary>Remove a page number from this usage map</summary>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal virtual void RemovePageNumber(int pageNumber, bool force)
        {
            ++_modCount;
            _handler.AddOrRemovePageNumber(pageNumber, false, force);
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal virtual void UpdateMap(int absolutePageNumber, int bufferRelativePageNumber
            , ByteBuffer buffer, bool add, bool force)
        {
            //Find the byte to which to apply the bitmask and create the bitmask
            int offset = bufferRelativePageNumber / 8;
            byte bitmask = (byte)(1 << (bufferRelativePageNumber % 8));
            byte b = buffer.Get(_startOffset + offset);
            // check current value for this page number
            int pageNumberOffset = PageNumberToBitIndex(absolutePageNumber);
            bool isOn = _pageNumbers.Get(pageNumberOffset);
            if ((isOn == add) && !force)
            {
                throw new IOException("Page number " + absolutePageNumber + " already " + ((add) ?
                    "added to" : "removed from") + " usage map, expected range " + _startPage + " to "
                     + _endPage);
            }
            //Apply the bitmask
            if (add)
            {
                b |= bitmask;
                _pageNumbers.Set(pageNumberOffset);
            }
            else
            {
                b &= (byte)~bitmask;
                _pageNumbers.Clear(pageNumberOffset);
            }
            buffer.Put(_startOffset + offset, b);
        }

        /// <summary>Promotes and inline usage map to a reference usage map.</summary>
        /// <remarks>Promotes and inline usage map to a reference usage map.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private void PromoteInlineHandlerToReferenceHandler(int newPageNumber)
        {
            // copy current page number info to new references and then clear old
            int oldStartPage = _startPage;
            BitSet oldPageNumbers = (BitSet)_pageNumbers.Clone();
            // clear out the main table (inline usage map data and start page)
            ClearTableAndPages();
            // set the new map type
            _tableBuffer.Put(GetRowStart(), MAP_TYPE_REFERENCE);
            // write the new table data
            WriteTable();
            // set new handler
            _handler = new UsageMap.ReferenceHandler(this);
            // update new handler with old data
            ReAddPages(oldStartPage, oldPageNumbers, newPageNumber);
        }

        /// <exception cref="System.IO.IOException"></exception>
        private void ReAddPages(int oldStartPage, BitSet oldPageNumbers, int newPageNumber
            )
        {
            // add all the old pages back in
            for (int i = oldPageNumbers.NextSetBit(0); i >= 0; i = oldPageNumbers.NextSetBit(
                i + 1))
            {
                AddPageNumber(oldStartPage + i);
            }
            if (newPageNumber > PageChannel.INVALID_PAGE_NUMBER)
            {
                // and then add the new page
                AddPageNumber(newPageNumber);
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder("(" + _handler.GetType().Name + ") page numbers (range "
                 + _startPage + " " + _endPage + "): [");
            UsageMap.PageCursor pCursor = Cursor();
            int curRangeStart = int.MinValue;
            int prevPage = int.MinValue;
            while (true)
            {
                int nextPage = pCursor.GetNextPage();
                if (nextPage < 0)
                {
                    break;
                }
                if (nextPage != (prevPage + 1))
                {
                    if (prevPage >= 0)
                    {
                        RangeToString(builder, curRangeStart, prevPage);
                    }
                    curRangeStart = nextPage;
                }
                prevPage = nextPage;
            }
            if (prevPage >= 0)
            {
                RangeToString(builder, curRangeStart, prevPage);
            }
            builder.Append("]");
            return builder.ToString();
        }

        private static void RangeToString(StringBuilder builder, int rangeStart, int rangeEnd
            )
        {
            builder.Append(rangeStart);
            if (rangeEnd > rangeStart)
            {
                builder.Append("-").Append(rangeEnd);
            }
            builder.Append(", ");
        }

        internal abstract class Handler
        {
            public Handler(UsageMap _enclosing)
            {
                this._enclosing = _enclosing;
            }

            public virtual bool ContainsPageNumber(int pageNumber)
            {
                return (this._enclosing.IsPageWithinRange(pageNumber) && this._enclosing.GetPageNumbers
                    ().Get(this._enclosing.PageNumberToBitIndex(pageNumber)));
            }

            /// <param name="pageNumber">Page number to add or remove from this map</param>
            /// <param name="add">True to add it, false to remove it</param>
            /// <param name="force">true to force add/remove and ignore certain inconsistencies</param>
            /// <exception cref="System.IO.IOException"></exception>
            public abstract void AddOrRemovePageNumber(int pageNumber, bool add, bool force);

            private readonly UsageMap _enclosing;
        }

        /// <summary>Usage map whose map is written inline in the same page.</summary>
        /// <remarks>
        /// Usage map whose map is written inline in the same page.  For Jet4, this
        /// type of map can usually contains a maximum of 512 pages.  Free space maps
        /// are always inline, used space maps may be inline or reference.  It has a
        /// start page, which all page numbers in its map are calculated as starting
        /// from.
        /// </remarks>
        /// <author>Tim McCune</author>
        internal class InlineHandler : UsageMap.Handler
        {
            private readonly bool _assumeOutOfRangeBitsOn;

            private readonly int _maxInlinePages;

            /// <exception cref="System.IO.IOException"></exception>
            internal InlineHandler(UsageMap _enclosing, bool assumeOutOfRangeBitsOn) : base(_enclosing
                )
            {
                this._enclosing = _enclosing;
                this._assumeOutOfRangeBitsOn = assumeOutOfRangeBitsOn;
                this._maxInlinePages = (this.GetInlineDataEnd() - this.GetInlineDataStart()) * 8;
                int startPage = this._enclosing.GetTableBuffer().GetInt(this._enclosing.GetRowStart
                    () + 1);
                this.SetInlinePageRange(startPage);
                this._enclosing.ProcessMap(this._enclosing.GetTableBuffer(), 0);
            }

            private int GetMaxInlinePages()
            {
                return this._maxInlinePages;
            }

            private int GetInlineDataStart()
            {
                return this._enclosing.GetRowStart() + this._enclosing.GetFormat().OFFSET_USAGE_MAP_START;
            }

            private int GetInlineDataEnd()
            {
                return this._enclosing.GetRowEnd();
            }

            /// <summary>
            /// Sets the page range for an inline usage map starting from the given
            /// page.
            /// </summary>
            /// <remarks>
            /// Sets the page range for an inline usage map starting from the given
            /// page.
            /// </remarks>
            private void SetInlinePageRange(int startPage)
            {
                this._enclosing.SetPageRange(startPage, startPage + this.GetMaxInlinePages());
            }

            public override bool ContainsPageNumber(int pageNumber)
            {
                return (base.ContainsPageNumber(pageNumber) || (this._assumeOutOfRangeBitsOn && (
                    pageNumber >= 0) && !this._enclosing.IsPageWithinRange(pageNumber)));
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void AddOrRemovePageNumber(int pageNumber, bool add, bool force)
            {
                if (this._enclosing.IsPageWithinRange(pageNumber))
                {
                    // easy enough, just update the inline data
                    int bufferRelativePageNumber = this._enclosing.PageNumberToBitIndex(pageNumber);
                    this._enclosing.UpdateMap(pageNumber, bufferRelativePageNumber, this._enclosing.GetTableBuffer
                        (), add, force);
                    // Write the updated map back to disk
                    this._enclosing.WriteTable();
                }
                else
                {
                    // uh-oh, we've split our britches.  what now?  determine what our
                    // status is
                    int firstPage = this._enclosing.GetFirstPageNumber();
                    int lastPage = this._enclosing.GetLastPageNumber();
                    if (add)
                    {
                        // we can ignore out-of-range page addition if we are already
                        // assuming out-of-range bits are "on".  Note, we are leaving small
                        // holes in the database here (leaving behind some free pages), but
                        // it's not the end of the world.
                        if (!this._assumeOutOfRangeBitsOn)
                        {
                            // we are adding, can we shift the bits and stay inline?
                            if (firstPage <= PageChannel.INVALID_PAGE_NUMBER)
                            {
                                // no pages currently
                                firstPage = pageNumber;
                                lastPage = pageNumber;
                            }
                            else
                            {
                                if (pageNumber > lastPage)
                                {
                                    lastPage = pageNumber;
                                }
                                else
                                {
                                    firstPage = pageNumber;
                                }
                            }
                            if ((lastPage - firstPage + 1) < this.GetMaxInlinePages())
                            {
                                // we can still fit within an inline map
                                this.MoveToNewStartPage(firstPage, pageNumber);
                            }
                            else
                            {
                                // not going to happen, need to promote the usage map to a
                                // reference map
                                this._enclosing.PromoteInlineHandlerToReferenceHandler(pageNumber);
                            }
                        }
                    }
                    else
                    {
                        // we are removing, what does that mean?
                        if (this._assumeOutOfRangeBitsOn)
                        {
                            // we are using an inline map and assuming that anything not
                            // within the current range is "on".  so, if we attempt to set a
                            // bit which is before the current page, ignore it, we are not
                            // going back for it.
                            if ((firstPage <= PageChannel.INVALID_PAGE_NUMBER) || (pageNumber > lastPage))
                            {
                                // move to new start page, filling in as we move
                                this.MoveToNewStartPageForRemove(firstPage, pageNumber);
                            }
                        }
                        else
                        {
                            if (!force)
                            {
                                // this should not happen, we are removing a page which is not in
                                // the map
                                throw new IOException("Page number " + pageNumber + " already removed from usage map"
                                     + ", expected range " + this._enclosing._startPage + " to " + this._enclosing._endPage
                                    );
                            }
                        }
                    }
                }
            }

            /// <summary>Shifts the inline usage map so that it now starts with the given page.</summary>
            /// <remarks>Shifts the inline usage map so that it now starts with the given page.</remarks>
            /// <param name="newStartPage">new page at which to start</param>
            /// <param name="newPageNumber">
            /// optional page number to add once the map has been
            /// shifted to the new start page
            /// </param>
            /// <exception cref="System.IO.IOException"></exception>
            private void MoveToNewStartPage(int newStartPage, int newPageNumber)
            {
                int oldStartPage = this._enclosing.GetStartPage();
                BitSet oldPageNumbers = (BitSet)this._enclosing.GetPageNumbers().Clone();
                // clear out the main table (inline usage map data and start page)
                this._enclosing.ClearTableAndPages();
                // write new start page
                ByteBuffer tableBuffer = this._enclosing.GetTableBuffer();
                tableBuffer.Position(this._enclosing.GetRowStart() + 1);
                tableBuffer.PutInt(newStartPage);
                // write the new table data
                this._enclosing.WriteTable();
                // set new page range
                this.SetInlinePageRange(newStartPage);
                // put the pages back in
                this._enclosing.ReAddPages(oldStartPage, oldPageNumbers, newPageNumber);
            }

            /// <summary>
            /// Shifts the inline usage map so that it now starts with the given
            /// firstPage (if valid), otherwise the newPageNumber.
            /// </summary>
            /// <remarks>
            /// Shifts the inline usage map so that it now starts with the given
            /// firstPage (if valid), otherwise the newPageNumber.  Any page numbers
            /// added to the end of the usage map are set to "on".
            /// </remarks>
            /// <param name="firstPage">current first used page</param>
            /// <param name="newPageNumber">
            /// page number to remove once the map has been
            /// shifted to the new start page
            /// </param>
            /// <exception cref="System.IO.IOException"></exception>
            private void MoveToNewStartPageForRemove(int firstPage, int newPageNumber)
            {
                int oldEndPage = this._enclosing.GetEndPage();
                int newStartPage = ((firstPage <= PageChannel.INVALID_PAGE_NUMBER) ? newPageNumber
                     : (newPageNumber - (this.GetMaxInlinePages() / 2)));
                // just shift a little and discard any initial unused pages.
                // move the current data
                this.MoveToNewStartPage(newStartPage, PageChannel.INVALID_PAGE_NUMBER);
                if (firstPage <= PageChannel.INVALID_PAGE_NUMBER)
                {
                    // this is the common case where we left everything behind
                    ByteUtil.FillRange(this._enclosing._tableBuffer, this.GetInlineDataStart(), this.
                        GetInlineDataEnd());
                    // write out the updated table
                    this._enclosing.WriteTable();
                    // "add" all the page numbers
                    this._enclosing.GetPageNumbers().Set(0, this.GetMaxInlinePages());
                }
                else
                {
                    // add every new page manually
                    for (int i = oldEndPage; i < this._enclosing.GetEndPage(); ++i)
                    {
                        this._enclosing.AddPageNumber(i);
                    }
                }
                // lastly, remove the new page
                this._enclosing.RemovePageNumber(newPageNumber);
            }

            private readonly UsageMap _enclosing;
        }

        /// <summary>
        /// Usage map whose map is written across one or more entire separate pages
        /// of page type USAGE_MAP.
        /// </summary>
        /// <remarks>
        /// Usage map whose map is written across one or more entire separate pages
        /// of page type USAGE_MAP.  For Jet4, this type of map can contain 32736
        /// pages per reference page, and a maximum of 17 reference map pages for a
        /// total maximum of 556512 pages (2 GB).
        /// </remarks>
        /// <author>Tim McCune</author>
        private class ReferenceHandler : UsageMap.Handler
        {
            /// <summary>Buffer that contains the current reference map page</summary>
            private readonly TempPageHolder _mapPageHolder = TempPageHolder.NewHolder(TempBufferHolder.Type
                .SOFT);

            /// <exception cref="System.IO.IOException"></exception>
            public ReferenceHandler(UsageMap _enclosing) : base(_enclosing)
            {
                this._enclosing = _enclosing;
                int numUsagePages = (this._enclosing.GetRowEnd() - this._enclosing.GetRowStart()
                    - 1) / 4;
                this._enclosing.SetStartOffset(this._enclosing.GetFormat().OFFSET_USAGE_MAP_PAGE_DATA
                    );
                this._enclosing.SetPageRange(0, (numUsagePages * this.GetMaxPagesPerUsagePage()));
                // there is no "start page" for a reference usage map, so we get an
                // extra page reference on top of the number of page references that fit
                // in the table
                for (int i = 0; i < numUsagePages; i++)
                {
                    int mapPageNum = this._enclosing.GetTableBuffer().GetInt(this.CalculateMapPagePointerOffset
                        (i));
                    if (mapPageNum > 0)
                    {
                        ByteBuffer mapPageBuffer = this._mapPageHolder.SetPage(this._enclosing.GetPageChannel
                            (), mapPageNum);
                        byte pageType = mapPageBuffer.Get();
                        if (pageType != PageTypes.USAGE_MAP)
                        {
                            throw new IOException("Looking for usage map at page " + mapPageNum + ", but page type is "
                                 + pageType);
                        }
                        mapPageBuffer.Position(this._enclosing.GetFormat().OFFSET_USAGE_MAP_PAGE_DATA);
                        this._enclosing.ProcessMap(mapPageBuffer, (this.GetMaxPagesPerUsagePage() * i));
                    }
                }
            }

            private int GetMaxPagesPerUsagePage()
            {
                return ((this._enclosing.GetFormat().PAGE_SIZE - this._enclosing.GetFormat().OFFSET_USAGE_MAP_PAGE_DATA
                    ) * 8);
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void AddOrRemovePageNumber(int pageNumber, bool add, bool force)
            {
                if (!this._enclosing.IsPageWithinRange(pageNumber))
                {
                    if (force)
                    {
                        return;
                    }
                    throw new IOException("Page number " + pageNumber + " is out of supported range");
                }
                int pageIndex = (pageNumber / this.GetMaxPagesPerUsagePage());
                int mapPageNum = this._enclosing.GetTableBuffer().GetInt(this.CalculateMapPagePointerOffset
                    (pageIndex));
                ByteBuffer mapPageBuffer = null;
                if (mapPageNum > 0)
                {
                    mapPageBuffer = this._mapPageHolder.SetPage(this._enclosing.GetPageChannel(), mapPageNum
                        );
                }
                else
                {
                    // Need to create a new usage map page
                    mapPageBuffer = this.CreateNewUsageMapPage(pageIndex);
                    mapPageNum = this._mapPageHolder.GetPageNumber();
                }
                this._enclosing.UpdateMap(pageNumber, (pageNumber - (this.GetMaxPagesPerUsagePage
                    () * pageIndex)), mapPageBuffer, add, force);
                this._enclosing.GetPageChannel().WritePage(mapPageBuffer, mapPageNum);
            }

            /// <summary>
            /// Create a new usage map page and update the map declaration with a
            /// pointer to it.
            /// </summary>
            /// <remarks>
            /// Create a new usage map page and update the map declaration with a
            /// pointer to it.
            /// </remarks>
            /// <param name="pageIndex">Index of the page reference within the map declaration</param>
            /// <exception cref="System.IO.IOException"></exception>
            private ByteBuffer CreateNewUsageMapPage(int pageIndex)
            {
                ByteBuffer mapPageBuffer = this._mapPageHolder.SetNewPage(this._enclosing.GetPageChannel
                    ());
                mapPageBuffer.Put(PageTypes.USAGE_MAP);
                mapPageBuffer.Put(unchecked((byte)unchecked((int)(0x01))));
                //Unknown
                mapPageBuffer.PutShort((short)0);
                //Unknown
                int mapPageNum = this._mapPageHolder.GetPageNumber();
                this._enclosing.GetTableBuffer().PutInt(this.CalculateMapPagePointerOffset(pageIndex
                    ), mapPageNum);
                this._enclosing.WriteTable();
                return mapPageBuffer;
            }

            private int CalculateMapPagePointerOffset(int pageIndex)
            {
                return this._enclosing.GetRowStart() + this._enclosing.GetFormat().OFFSET_REFERENCE_MAP_PAGE_NUMBERS
                     + (pageIndex * 4);
            }

            private readonly UsageMap _enclosing;
        }

        /// <summary>Utility class to traverse over the pages in the UsageMap.</summary>
        /// <remarks>
        /// Utility class to traverse over the pages in the UsageMap.  Remains valid
        /// in the face of usage map modifications.
        /// </remarks>
        public sealed class PageCursor
        {
            /// <summary>handler for moving the page cursor forward</summary>
            private readonly UsageMap.PageCursor.DirHandler _forwardDirHandler;

            /// <summary>handler for moving the page cursor backward</summary>
            private readonly UsageMap.PageCursor.DirHandler _reverseDirHandler;

            /// <summary>the current used page number</summary>
            private int _curPageNumber;

            /// <summary>the previous used page number</summary>
            private int _prevPageNumber;

            /// <summary>the last read modification count on the UsageMap.</summary>
            /// <remarks>
            /// the last read modification count on the UsageMap.  we track this so
            /// that the cursor can detect updates to the usage map while traversing
            /// and act accordingly
            /// </remarks>
            private int _lastModCount;

            public PageCursor(UsageMap _enclosing)
            {
                this._enclosing = _enclosing;
                _forwardDirHandler = new UsageMap.PageCursor.ForwardDirHandler(this);
                _reverseDirHandler = new UsageMap.PageCursor.ReverseDirHandler(this);
                this.Reset();
            }

            public UsageMap GetUsageMap()
            {
                return this._enclosing;
            }

            /// <summary>Returns the DirHandler for the given direction</summary>
            private UsageMap.PageCursor.DirHandler GetDirHandler(bool moveForward)
            {
                return (moveForward ? this._forwardDirHandler : this._reverseDirHandler);
            }

            /// <summary>
            /// Returns
            /// <code>true</code>
            /// if this cursor is up-to-date with respect to its
            /// usage map.
            /// </summary>
            public bool IsUpToDate()
            {
                return (this._enclosing._modCount == this._lastModCount);
            }

            /// <returns>
            /// valid page number if there was another page to read,
            /// <see cref="RowId.LAST_PAGE_NUMBER">RowId.LAST_PAGE_NUMBER</see>
            /// otherwise
            /// </returns>
            public int GetNextPage()
            {
                return this.GetAnotherPage(Jackcess.Cursor.MOVE_FORWARD);
            }

            /// <returns>
            /// valid page number if there was another page to read,
            /// <see cref="RowId.FIRST_PAGE_NUMBER">RowId.FIRST_PAGE_NUMBER</see>
            /// otherwise
            /// </returns>
            public int GetPreviousPage()
            {
                return this.GetAnotherPage(Jackcess.Cursor.MOVE_REVERSE);
            }

            /// <summary>Gets another page in the given direction, returning the new page.</summary>
            /// <remarks>Gets another page in the given direction, returning the new page.</remarks>
            private int GetAnotherPage(bool moveForward)
            {
                UsageMap.PageCursor.DirHandler handler = this.GetDirHandler(moveForward);
                if (this._curPageNumber == handler.GetEndPageNumber())
                {
                    if (!this.IsUpToDate())
                    {
                        this.RestorePosition(this._prevPageNumber);
                    }
                    else
                    {
                        // drop through and retry moving to another page
                        // at end, no more
                        return this._curPageNumber;
                    }
                }
                this.CheckForModification();
                this._prevPageNumber = this._curPageNumber;
                this._curPageNumber = handler.GetAnotherPageNumber(this._curPageNumber);
                return this._curPageNumber;
            }

            /// <summary>
            /// After calling this method, getNextPage will return the first page in
            /// the map
            /// </summary>
            public void Reset()
            {
                this.BeforeFirst();
            }

            /// <summary>
            /// After calling this method,
            /// <see cref="GetNextPage()">GetNextPage()</see>
            /// will return the first
            /// page in the map
            /// </summary>
            public void BeforeFirst()
            {
                this.Reset(Jackcess.Cursor.MOVE_FORWARD);
            }

            /// <summary>
            /// After calling this method,
            /// <see cref="GetPreviousPage()">GetPreviousPage()</see>
            /// will return the
            /// last page in the map
            /// </summary>
            public void AfterLast()
            {
                this.Reset(Jackcess.Cursor.MOVE_REVERSE);
            }

            /// <summary>Resets this page cursor for traversing the given direction.</summary>
            /// <remarks>Resets this page cursor for traversing the given direction.</remarks>
            internal void Reset(bool moveForward)
            {
                this._curPageNumber = this.GetDirHandler(moveForward).GetBeginningPageNumber();
                this._prevPageNumber = this._curPageNumber;
                this._lastModCount = this._enclosing._modCount;
            }

            /// <summary>
            /// Restores a current position for the cursor (current position becomes
            /// previous position).
            /// </summary>
            /// <remarks>
            /// Restores a current position for the cursor (current position becomes
            /// previous position).
            /// </remarks>
            private void RestorePosition(int curPageNumber)
            {
                this.RestorePosition(curPageNumber, this._curPageNumber);
            }

            /// <summary>Restores a current and previous position for the cursor.</summary>
            /// <remarks>Restores a current and previous position for the cursor.</remarks>
            internal void RestorePosition(int curPageNumber, int prevPageNumber)
            {
                if ((curPageNumber != this._curPageNumber) || (prevPageNumber != this._prevPageNumber
                    ))
                {
                    this._prevPageNumber = this.UpdatePosition(prevPageNumber);
                    this._curPageNumber = this.UpdatePosition(curPageNumber);
                    this._lastModCount = this._enclosing._modCount;
                }
                else
                {
                    this.CheckForModification();
                }
            }

            /// <summary>Checks the usage map for modifications an updates state accordingly.</summary>
            /// <remarks>Checks the usage map for modifications an updates state accordingly.</remarks>
            private void CheckForModification()
            {
                if (!this.IsUpToDate())
                {
                    this._prevPageNumber = this.UpdatePosition(this._prevPageNumber);
                    this._curPageNumber = this.UpdatePosition(this._curPageNumber);
                    this._lastModCount = this._enclosing._modCount;
                }
            }

            private int UpdatePosition(int pageNumber)
            {
                if (pageNumber < this._enclosing.GetFirstPageNumber())
                {
                    pageNumber = RowId.FIRST_PAGE_NUMBER;
                }
                else
                {
                    if (pageNumber > this._enclosing.GetLastPageNumber())
                    {
                        pageNumber = RowId.LAST_PAGE_NUMBER;
                    }
                }
                return pageNumber;
            }

            public override string ToString()
            {
                return this.GetType().Name + " CurPosition " + this._curPageNumber + ", PrevPosition "
                     + this._prevPageNumber;
            }

            /// <summary>Handles moving the cursor in a given direction.</summary>
            /// <remarks>
            /// Handles moving the cursor in a given direction.  Separates cursor
            /// logic from value storage.
            /// </remarks>
            private abstract class DirHandler
            {
                public abstract int GetAnotherPageNumber(int curPageNumber);

                public abstract int GetBeginningPageNumber();

                public abstract int GetEndPageNumber();

                internal DirHandler(PageCursor _enclosing)
                {
                    this._enclosing = _enclosing;
                }

                private readonly PageCursor _enclosing;
            }

            /// <summary>Handles moving the cursor forward.</summary>
            /// <remarks>Handles moving the cursor forward.</remarks>
            private sealed class ForwardDirHandler : UsageMap.PageCursor.DirHandler
            {
                public override int GetAnotherPageNumber(int curPageNumber)
                {
                    if (curPageNumber == this.GetBeginningPageNumber())
                    {
                        return this._enclosing._enclosing.GetFirstPageNumber();
                    }
                    return this._enclosing._enclosing.GetNextPageNumber(curPageNumber);
                }

                public override int GetBeginningPageNumber()
                {
                    return RowId.FIRST_PAGE_NUMBER;
                }

                public override int GetEndPageNumber()
                {
                    return RowId.LAST_PAGE_NUMBER;
                }

                internal ForwardDirHandler(PageCursor _enclosing) : base(_enclosing)
                {
                    this._enclosing = _enclosing;
                }

                private readonly PageCursor _enclosing;
            }

            /// <summary>Handles moving the cursor backward.</summary>
            /// <remarks>Handles moving the cursor backward.</remarks>
            private sealed class ReverseDirHandler : UsageMap.PageCursor.DirHandler
            {
                public override int GetAnotherPageNumber(int curPageNumber)
                {
                    if (curPageNumber == this.GetBeginningPageNumber())
                    {
                        return this._enclosing._enclosing.GetLastPageNumber();
                    }
                    return this._enclosing._enclosing.GetPrevPageNumber(curPageNumber);
                }

                public override int GetBeginningPageNumber()
                {
                    return RowId.LAST_PAGE_NUMBER;
                }

                public override int GetEndPageNumber()
                {
                    return RowId.FIRST_PAGE_NUMBER;
                }

                internal ReverseDirHandler(PageCursor _enclosing) : base(_enclosing)
                {
                    this._enclosing = _enclosing;
                }

                private readonly PageCursor _enclosing;
            }

            private readonly UsageMap _enclosing;
        }
    }
}
