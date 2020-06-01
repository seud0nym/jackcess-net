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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HealthMarketScience.Jackcess
{
    /// <summary>Access table index data.</summary>
    /// <remarks>
    /// Access table index data.  This is the actual data which backs a logical
    /// Index, where one or more logical indexes can be backed by the same index
    /// data.
    /// </remarks>
    /// <author>Tim McCune</author>
    public abstract class IndexData
    {
        /// <summary>special entry which is less than any other entry</summary>
        public static readonly IndexData.Entry FIRST_ENTRY = CreateSpecialEntry(RowId.FIRST_ROW_ID
            );

        /// <summary>special entry which is greater than any other entry</summary>
        public static readonly IndexData.Entry LAST_ENTRY = CreateSpecialEntry(RowId.LAST_ROW_ID
            );

        /// <summary>
        /// special object which will always be greater than any other value, when
        /// searching for an index entry range in a multi-value index
        /// </summary>
        public static readonly object MAX_VALUE = new object();

        /// <summary>
        /// special object which will always be greater than any other value, when
        /// searching for an index entry range in a multi-value index
        /// </summary>
        public static readonly object MIN_VALUE = new object();

        protected internal const int INVALID_INDEX_PAGE_NUMBER = 0;

        /// <summary>Max number of columns in an index</summary>
        internal const int MAX_COLUMNS = 10;

        protected internal static readonly byte[] EMPTY_PREFIX = new byte[0];

        private const short COLUMN_UNUSED = -1;

        internal const byte ASCENDING_COLUMN_FLAG = unchecked((byte)unchecked((int)(0x01)
            ));

        internal const byte UNIQUE_INDEX_FLAG = unchecked((byte)unchecked((int)(0x01)));

        internal const byte IGNORE_NULLS_INDEX_FLAG = unchecked((byte)unchecked((int)(0x02
            )));

        internal const byte SPECIAL_INDEX_FLAG = unchecked((byte)unchecked((int)(0x08)));

        internal const byte UNKNOWN_INDEX_FLAG = unchecked((byte)unchecked((int)(0x80)));

        private const int MAGIC_INDEX_NUMBER = 1923;

        private static readonly ByteOrder ENTRY_BYTE_ORDER = ByteOrder.BIG_ENDIAN;

        /// <summary>type attributes for Entries which simplify comparisons</summary>
        public enum EntryType
        {
            ALWAYS_FIRST,
            FIRST_VALID,
            NORMAL,
            LAST_VALID,
            ALWAYS_LAST
        }

        private sealed class _IComparer_109 : IComparer<byte[]>
        {
            public _IComparer_109()
            {
            }

            // set on MSysACEs and MSysAccessObjects indexes, purpose unknown
            // always seems to be set on indexes in access 2000+
            public int Compare(byte[] left, byte[] right)
            {
                if (left == right)
                {
                    return 0;
                }
                if (left == null)
                {
                    return -1;
                }
                if (right == null)
                {
                    return 1;
                }
                int len = Math.Min(left.Length, right.Length);
                int pos = 0;
                while ((pos < len) && (left[pos] == right[pos]))
                {
                    ++pos;
                }
                if (pos < len)
                {
                    return ((ByteUtil.AsUnsignedByte(left[pos]) < ByteUtil.AsUnsignedByte(right[pos])
                        ) ? -1 : 1);
                }
                return ((left.Length < right.Length) ? -1 : ((left.Length > right.Length) ? 1 : 0
                    ));
            }
        }

        internal static readonly IComparer<byte[]> BYTE_CODE_COMPARATOR = new _IComparer_109
            ();

        /// <summary>owning table</summary>
        private readonly Table _table;

        /// <summary>0-based index data number</summary>
        private readonly int _number;

        /// <summary>Page number of the root index data</summary>
        private int _rootPageNumber;

        /// <summary>
        /// offset within the tableDefinition buffer of the uniqueEntryCount for
        /// this index
        /// </summary>
        private readonly int _uniqueEntryCountOffset;

        /// <summary>The number of unique entries which have been added to this index.</summary>
        /// <remarks>
        /// The number of unique entries which have been added to this index.  note,
        /// however, that it is never decremented, only incremented (as observed in
        /// Access).
        /// </remarks>
        private int _uniqueEntryCount;

        /// <summary>List of columns and flags</summary>
        private readonly IList<IndexData.ColumnDescriptor> _columns = new AList<IndexData.ColumnDescriptor
            >();

        /// <summary>the logical indexes which this index data backs</summary>
        private readonly IList<Index> _indexes = new AList<Index>();

        /// <summary>flags for this index</summary>
        private byte _indexFlags;

        /// <summary>Usage map of pages that this index owns</summary>
        private UsageMap _ownedPages;

        /// <summary>
        /// <code>true</code> if the index entries have been initialized,
        /// <code>false</code> otherwise
        /// </summary>
        private bool _initialized;

        /// <summary>modification count for the table, keeps cursors up-to-date</summary>
        private int _modCount;

        /// <summary>temp buffer used to read/write the index pages</summary>
        private readonly TempBufferHolder _indexBufferH = TempBufferHolder.NewHolder(TempBufferHolder.Type
            .SOFT, true);

        /// <summary>temp buffer used to create index entries</summary>
        private ByteUtil.ByteStream _entryBuffer;

        /// <summary>max size for all the entries written to a given index data page</summary>
        private readonly int _maxPageEntrySize;

        /// <summary>whether or not this index data is backing a primary key logical index</summary>
        private bool _primaryKey;

        /// <summary>FIXME, for SimpleIndex, we can't write multi-page indexes or indexes using the entry compression scheme
        /// 	</summary>
        private bool _readOnly;

        protected internal IndexData(Table table, int number, int uniqueEntryCount, int uniqueEntryCountOffset
            )
        {
            _table = table;
            _number = number;
            _uniqueEntryCount = uniqueEntryCount;
            _uniqueEntryCountOffset = uniqueEntryCountOffset;
            _maxPageEntrySize = CalcMaxPageEntrySize(_table.GetFormat());
        }

        /// <summary>
        /// Creates an IndexData appropriate for the given table, using information
        /// from the given table definition buffer.
        /// </summary>
        /// <remarks>
        /// Creates an IndexData appropriate for the given table, using information
        /// from the given table definition buffer.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public static HealthMarketScience.Jackcess.IndexData Create(Table table, ByteBuffer
             tableBuffer, int number, JetFormat format)
        {
            int uniqueEntryCountOffset = (format.OFFSET_INDEX_DEF_BLOCK + (number * format.SIZE_INDEX_DEFINITION
                ) + 4);
            int uniqueEntryCount = tableBuffer.GetInt(uniqueEntryCountOffset);
            return (table.DoUseBigIndex() ? (IndexData)new BigIndexData(table, number, uniqueEntryCount,
                uniqueEntryCountOffset) : (IndexData)new SimpleIndexData(table, number, uniqueEntryCount, uniqueEntryCountOffset
                ));
        }

        public virtual Table GetTable()
        {
            return _table;
        }

        public virtual JetFormat GetFormat()
        {
            return GetTable().GetFormat();
        }

        public virtual PageChannel GetPageChannel()
        {
            return GetTable().GetPageChannel();
        }

        /// <returns>the "main" logical index which is backed by this data.</returns>
        public virtual Index GetPrimaryIndex()
        {
            return _indexes[0];
        }

        /// <returns>All of the Indexes backed by this data (unmodifiable List)</returns>
        public virtual IList<Index> GetIndexes()
        {
            return Sharpen.Collections.UnmodifiableList(_indexes);
        }

        /// <summary>Adds a logical index which this data is backing.</summary>
        /// <remarks>Adds a logical index which this data is backing.</remarks>
        internal virtual void AddIndex(Index index)
        {
            // we keep foreign key indexes at the back of the list.  this way the
            // primary index will be a non-foreign key index (if any)
            if (index.IsForeignKey())
            {
                _indexes.AddItem(index);
            }
            else
            {
                int pos = _indexes.Count;
                while (pos > 0)
                {
                    if (!_indexes[pos - 1].IsForeignKey())
                    {
                        break;
                    }
                    --pos;
                }
                _indexes.Add(pos, index);
                // also, keep track of whether or not this is a primary key index
                _primaryKey |= index.IsPrimaryKey();
            }
        }

        public virtual byte GetIndexFlags()
        {
            return _indexFlags;
        }

        public virtual int GetIndexDataNumber()
        {
            return _number;
        }

        public virtual int GetUniqueEntryCount()
        {
            return _uniqueEntryCount;
        }

        public virtual int GetUniqueEntryCountOffset()
        {
            return _uniqueEntryCountOffset;
        }

        protected internal virtual bool IsBackingPrimaryKey()
        {
            return _primaryKey;
        }

        /// <summary>
        /// Whether or not
        /// <code>null</code>
        /// values are actually recorded in the index.
        /// </summary>
        public virtual bool ShouldIgnoreNulls()
        {
            return ((_indexFlags & IGNORE_NULLS_INDEX_FLAG) != 0);
        }

        /// <summary>Whether or not index entries must be unique.</summary>
        /// <remarks>
        /// Whether or not index entries must be unique.
        /// <p>
        /// Some notes about uniqueness:
        /// <ul>
        /// <li>Access does not seem to consider multiple
        /// <code>null</code>
        /// entries
        /// invalid for a unique index</li>
        /// <li>text indexes collapse case, and Access seems to compare <b>only</b>
        /// the index entry bytes, therefore two strings which differ only in
        /// case <i>will violate</i> the unique constraint</li>
        /// </ul>
        /// </remarks>
        public virtual bool IsUnique()
        {
            return (IsBackingPrimaryKey() || ((_indexFlags & UNIQUE_INDEX_FLAG) != 0));
        }

        /// <summary>Returns the Columns for this index (unmodifiable)</summary>
        public virtual IList<IndexData.ColumnDescriptor> GetColumns()
        {
            return Sharpen.Collections.UnmodifiableList(_columns);
        }

        /// <summary>Whether or not the complete index state has been read.</summary>
        /// <remarks>Whether or not the complete index state has been read.</remarks>
        public virtual bool IsInitialized()
        {
            return _initialized;
        }

        protected internal virtual int GetRootPageNumber()
        {
            return _rootPageNumber;
        }

        protected internal virtual void SetReadOnly()
        {
            _readOnly = true;
        }

        protected internal virtual bool IsReadOnly()
        {
            return _readOnly;
        }

        protected internal virtual int GetMaxPageEntrySize()
        {
            return _maxPageEntrySize;
        }

        /// <summary>Returns the number of database pages owned by this index data.</summary>
        /// <remarks>Returns the number of database pages owned by this index data.</remarks>
        public virtual int GetOwnedPageCount()
        {
            return _ownedPages.GetPageCount();
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal virtual void AddOwnedPage(int pageNumber)
        {
            _ownedPages.AddPageNumber(pageNumber);
        }

        /// <summary>Returns the number of index entries in the index.</summary>
        /// <remarks>
        /// Returns the number of index entries in the index.  Only called by unit
        /// tests.
        /// <p>
        /// Forces index initialization.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal virtual int GetEntryCount()
        {
            Initialize();
            IndexData.EntryCursor cursor = GetCursor();
            IndexData.Entry endEntry = cursor.GetLastEntry();
            int count = 0;
            while (!endEntry.Equals(cursor.GetNextEntry()))
            {
                ++count;
            }
            return count;
        }

        /// <summary>Forces initialization of this index (actual parsing of index pages).</summary>
        /// <remarks>
        /// Forces initialization of this index (actual parsing of index pages).
        /// normally, the index will not be initialized until the entries are
        /// actually needed.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void Initialize()
        {
            if (!_initialized)
            {
                ReadIndexEntries();
                _initialized = true;
            }
        }

        /// <summary>Writes the current index state to the database.</summary>
        /// <remarks>
        /// Writes the current index state to the database.
        /// <p>
        /// Forces index initialization.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void Update()
        {
            // make sure we've parsed the entries
            Initialize();
            if (_readOnly)
            {
                throw new NotSupportedException("FIXME cannot write indexes of this type yet, see Database javadoc for info on enabling large index support"
                    );
            }
            UpdateImpl();
        }

        /// <summary>Read the rest of the index info from a tableBuffer</summary>
        /// <param name="tableBuffer">table definition buffer to read from initial info</param>
        /// <param name="availableColumns">Columns that this index may use</param>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void Read(ByteBuffer tableBuffer, IList<Column> availableColumns)
        {
            ByteUtil.Forward(tableBuffer, GetFormat().SKIP_BEFORE_INDEX);
            //Forward past Unknown
            for (int i = 0; i < MAX_COLUMNS; i++)
            {
                short columnNumber = tableBuffer.GetShort();
                byte colFlags = tableBuffer.Get();
                if (columnNumber != COLUMN_UNUSED)
                {
                    // find the desired column by column number (which is not necessarily
                    // the same as the column index)
                    Column idxCol = null;
                    foreach (Column col in availableColumns)
                    {
                        if (col.GetColumnNumber() == columnNumber)
                        {
                            idxCol = col;
                            break;
                        }
                    }
                    if (idxCol == null)
                    {
                        throw new IOException("Could not find column with number " + columnNumber + " for index"
                            );
                    }
                    _columns.AddItem(NewColumnDescriptor(idxCol, colFlags));
                }
            }
            int umapRowNum = tableBuffer.Get();
            int umapPageNum = ByteUtil.Get3ByteInt(tableBuffer);
            _ownedPages = UsageMap.Read(GetTable().GetDatabase(), umapPageNum, umapRowNum, false
                );
            _rootPageNumber = tableBuffer.GetInt();
            ByteUtil.Forward(tableBuffer, GetFormat().SKIP_BEFORE_INDEX_FLAGS);
            //Forward past Unknown
            _indexFlags = tableBuffer.Get();
            ByteUtil.Forward(tableBuffer, GetFormat().SKIP_AFTER_INDEX_FLAGS);
        }

        //Forward past other stuff
        /// <summary>Writes the index row count definitions into a table definition buffer.</summary>
        /// <remarks>Writes the index row count definitions into a table definition buffer.</remarks>
        /// <param name="buffer">Buffer to write to</param>
        /// <param name="indexes">List of IndexBuilders to write definitions for</param>
        protected internal static void WriteRowCountDefinitions(ByteBuffer buffer, int indexCount
            , JetFormat format)
        {
            // index row counts (empty data)
            ByteUtil.Forward(buffer, (indexCount * format.SIZE_INDEX_DEFINITION));
        }

        /// <summary>Writes the index definitions into a table definition buffer.</summary>
        /// <remarks>Writes the index definitions into a table definition buffer.</remarks>
        /// <param name="buffer">Buffer to write to</param>
        /// <param name="indexes">List of IndexBuilders to write definitions for</param>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal static void WriteDefinitions(ByteBuffer buffer, IList<Column>
            columns, IList<IndexBuilder> indexes, int tdefPageNumber, PageChannel pageChannel
            , JetFormat format)
        {
            ByteBuffer rootPageBuffer = pageChannel.CreatePageBuffer();
            WriteDataPage(rootPageBuffer, SimpleIndexData.NEW_ROOT_DATA_PAGE, tdefPageNumber,
                format);
            foreach (IndexBuilder idx in indexes)
            {
                buffer.PutInt(MAGIC_INDEX_NUMBER);
                // seemingly constant magic value
                // write column information (always MAX_COLUMNS entries)
                IList<IndexBuilder.Column> idxColumns = idx.GetColumns();
                for (int i = 0; i < MAX_COLUMNS; ++i)
                {
                    short columnNumber = COLUMN_UNUSED;
                    byte flags = 0;
                    if (i < idxColumns.Count)
                    {
                        // determine column info
                        IndexBuilder.Column idxCol = idxColumns[i];
                        flags = idxCol.GetFlags();
                        // find actual table column number
                        foreach (Column col in columns)
                        {
                            if (Sharpen.Runtime.EqualsIgnoreCase(col.GetName(), idxCol.GetName()))
                            {
                                columnNumber = col.GetColumnNumber();
                                break;
                            }
                        }
                        if (columnNumber == COLUMN_UNUSED)
                        {
                            // should never happen as this is validated before
                            throw new ArgumentException("Column with name " + idxCol.GetName() + " not found"
                                );
                        }
                    }
                    buffer.PutShort(columnNumber);
                    // table column number
                    buffer.Put(flags);
                }
                // column flags (e.g. ordering)
                buffer.Put(idx.GetUmapRowNumber());
                // umap row
                ByteUtil.Put3ByteInt(buffer, idx.GetUmapPageNumber());
                // umap page
                // write empty root index page
                pageChannel.WritePage(rootPageBuffer, idx.GetRootPageNumber());
                buffer.PutInt(idx.GetRootPageNumber());
                buffer.PutInt(0);
                // unknown
                buffer.Put(idx.GetFlags());
                // index flags (unique, etc.)
                ByteUtil.Forward(buffer, 5);
            }
        }

        // unknown
        /// <summary>
        /// Adds a row to this index
        /// <p>
        /// Forces index initialization.
        /// </summary>
        /// <remarks>
        /// Adds a row to this index
        /// <p>
        /// Forces index initialization.
        /// </remarks>
        /// <param name="row">Row to add</param>
        /// <param name="rowId">rowId of the row to be added</param>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void AddRow(object[] row, RowId rowId)
        {
            int nullCount = CountNullValues(row);
            bool isNullEntry = (nullCount == _columns.Count);
            if (ShouldIgnoreNulls() && isNullEntry)
            {
                // nothing to do
                return;
            }
            if (IsBackingPrimaryKey() && (nullCount > 0))
            {
                throw new IOException("Null value found in row " + Arrays.AsList(row) + " for primary key index "
                     + this);
            }
            // make sure we've parsed the entries
            Initialize();
            IndexData.Entry newEntry = new IndexData.Entry(CreateEntryBytes(row), rowId);
            if (AddEntry(newEntry, isNullEntry, row))
            {
                ++_modCount;
            }
            else
            {
                System.Console.Error.WriteLine("Added duplicate index entry " + newEntry + " for row: "
                     + Arrays.AsList(row));
            }
        }

        /// <summary>Adds an entry to the correct index dataPage, maintaining the order.</summary>
        /// <remarks>Adds an entry to the correct index dataPage, maintaining the order.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private bool AddEntry(IndexData.Entry newEntry, bool isNullEntry, object[] row)
        {
            IndexData.DataPage dataPage = FindDataPage(newEntry);
            int idx = dataPage.FindEntry(newEntry);
            if (idx < 0)
            {
                // this is a new entry
                idx = MissingIndexToInsertionPoint(idx);
                IndexData.Position newPos = new IndexData.Position(dataPage, idx, newEntry, true);
                IndexData.Position nextPos = GetNextPosition(newPos);
                IndexData.Position prevPos = GetPreviousPosition(newPos);
                // determine if the addition of this entry would break the uniqueness
                // constraint.  See isUnique() for some notes about uniqueness as
                // defined by Access.
                bool isDupeEntry = (((nextPos != null) && newEntry.EqualsEntryBytes(nextPos.GetEntry
                    ())) || ((prevPos != null) && newEntry.EqualsEntryBytes(prevPos.GetEntry())));
                if (IsUnique() && !isNullEntry && isDupeEntry)
                {
                    throw new IOException("New row " + Arrays.AsList(row) + " violates uniqueness constraint for index "
                         + this);
                }
                if (!isDupeEntry)
                {
                    ++_uniqueEntryCount;
                }
                dataPage.AddEntry(idx, newEntry);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a row from this index
        /// <p>
        /// Forces index initialization.
        /// </summary>
        /// <remarks>
        /// Removes a row from this index
        /// <p>
        /// Forces index initialization.
        /// </remarks>
        /// <param name="row">Row to remove</param>
        /// <param name="rowId">rowId of the row to be removed</param>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void DeleteRow(object[] row, RowId rowId)
        {
            int nullCount = CountNullValues(row);
            if (ShouldIgnoreNulls() && (nullCount == _columns.Count))
            {
                // nothing to do
                return;
            }
            // make sure we've parsed the entries
            Initialize();
            IndexData.Entry oldEntry = new IndexData.Entry(CreateEntryBytes(row), rowId);
            if (RemoveEntry(oldEntry))
            {
                ++_modCount;
            }
            else
            {
                System.Console.Error.WriteLine("Failed removing index entry " + oldEntry + " for row: "
                     + Arrays.AsList(row));
            }
        }

        /// <summary>Removes an entry from the relevant index dataPage, maintaining the order.
        /// 	</summary>
        /// <remarks>
        /// Removes an entry from the relevant index dataPage, maintaining the order.
        /// Will search by RowId if entry is not found (in case a partial entry was
        /// provided).
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private bool RemoveEntry(IndexData.Entry oldEntry)
        {
            IndexData.DataPage dataPage = FindDataPage(oldEntry);
            int idx = dataPage.FindEntry(oldEntry);
            bool doRemove = false;
            if (idx < 0)
            {
                // the caller may have only read some of the row data, if this is the
                // case, just search for the page/row numbers
                // FIXME, we could force caller to get relevant values?
                IndexData.EntryCursor cursor = GetCursor();
                IndexData.Position tmpPos = null;
                IndexData.Position endPos = cursor.LastPos;
                while (!endPos.Equals(tmpPos = cursor.GetAnotherPosition(HealthMarketScience.Jackcess.Cursor
                    .MOVE_FORWARD)))
                {
                    if (tmpPos.GetEntry().GetRowId().Equals(oldEntry.GetRowId()))
                    {
                        dataPage = tmpPos.GetDataPage();
                        idx = tmpPos.GetIndex();
                        doRemove = true;
                        break;
                    }
                }
            }
            else
            {
                doRemove = true;
            }
            if (doRemove)
            {
                // found it!
                dataPage.RemoveEntry(idx);
            }
            return doRemove;
        }

        /// <summary>Gets a new cursor for this index.</summary>
        /// <remarks>
        /// Gets a new cursor for this index.
        /// <p>
        /// Forces index initialization.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual IndexData.EntryCursor GetCursor()
        {
            return GetCursor(null, true, null, true);
        }

        /// <summary>
        /// Gets a new cursor for this index, narrowed to the range defined by the
        /// given startRow and endRow.
        /// </summary>
        /// <remarks>
        /// Gets a new cursor for this index, narrowed to the range defined by the
        /// given startRow and endRow.
        /// <p>
        /// Forces index initialization.
        /// </remarks>
        /// <param name="startRow">
        /// the first row of data for the cursor, or
        /// <code>null</code>
        /// for
        /// the first entry
        /// </param>
        /// <param name="startInclusive">whether or not startRow is inclusive or exclusive</param>
        /// <param name="endRow">
        /// the last row of data for the cursor, or
        /// <code>null</code>
        /// for
        /// the last entry
        /// </param>
        /// <param name="endInclusive">whether or not endRow is inclusive or exclusive</param>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual IndexData.EntryCursor GetCursor(object[] startRow, bool startInclusive
            , object[] endRow, bool endInclusive)
        {
            Initialize();
            IndexData.Entry startEntry = FIRST_ENTRY;
            byte[] startEntryBytes = null;
            if (startRow != null)
            {
                startEntryBytes = CreateEntryBytes(startRow);
                startEntry = new IndexData.Entry(startEntryBytes, (startInclusive ? RowId.FIRST_ROW_ID
                     : RowId.LAST_ROW_ID));
            }
            IndexData.Entry endEntry = LAST_ENTRY;
            if (endRow != null)
            {
                // reuse startEntryBytes if startRow and endRow are same array.  this is
                // common for "lookup" code
                byte[] endEntryBytes = ((startRow == endRow) ? startEntryBytes : CreateEntryBytes
                    (endRow));
                endEntry = new IndexData.Entry(endEntryBytes, (endInclusive ? RowId.LAST_ROW_ID :
                    RowId.FIRST_ROW_ID));
            }
            return new IndexData.EntryCursor(this, FindEntryPosition(startEntry), FindEntryPosition
                (endEntry));
        }

        /// <exception cref="System.IO.IOException"></exception>
        private IndexData.Position FindEntryPosition(IndexData.Entry entry)
        {
            IndexData.DataPage dataPage = FindDataPage(entry);
            int idx = dataPage.FindEntry(entry);
            bool between = false;
            if (idx < 0)
            {
                // given entry was not found exactly.  our current position is now
                // really between two indexes, but we cannot support that as an integer
                // value, so we set a flag instead
                idx = MissingIndexToInsertionPoint(idx);
                between = true;
            }
            return new IndexData.Position(dataPage, idx, entry, between);
        }

        /// <exception cref="System.IO.IOException"></exception>
        private IndexData.Position GetNextPosition(IndexData.Position curPos)
        {
            // get the next index (between-ness is handled internally)
            int nextIdx = curPos.GetNextIndex();
            IndexData.Position nextPos = null;
            if (nextIdx < curPos.GetDataPage().GetEntries().Count)
            {
                nextPos = new IndexData.Position(curPos.GetDataPage(), nextIdx);
            }
            else
            {
                int nextPageNumber = curPos.GetDataPage().GetNextPageNumber();
                IndexData.DataPage nextDataPage = null;
                while (nextPageNumber != INVALID_INDEX_PAGE_NUMBER)
                {
                    IndexData.DataPage dp = GetDataPage(nextPageNumber);
                    if (!dp.IsEmpty())
                    {
                        nextDataPage = dp;
                        break;
                    }
                    nextPageNumber = dp.GetNextPageNumber();
                }
                if (nextDataPage != null)
                {
                    nextPos = new IndexData.Position(nextDataPage, 0);
                }
            }
            return nextPos;
        }

        /// <summary>
        /// Returns the Position before the given one, or
        /// <code>null</code>
        /// if none.
        /// </summary>
        /// <exception cref="System.IO.IOException"></exception>
        private IndexData.Position GetPreviousPosition(IndexData.Position curPos)
        {
            // get the previous index (between-ness is handled internally)
            int prevIdx = curPos.GetPrevIndex();
            IndexData.Position prevPos = null;
            if (prevIdx >= 0)
            {
                prevPos = new IndexData.Position(curPos.GetDataPage(), prevIdx);
            }
            else
            {
                int prevPageNumber = curPos.GetDataPage().GetPrevPageNumber();
                IndexData.DataPage prevDataPage = null;
                while (prevPageNumber != INVALID_INDEX_PAGE_NUMBER)
                {
                    IndexData.DataPage dp = GetDataPage(prevPageNumber);
                    if (!dp.IsEmpty())
                    {
                        prevDataPage = dp;
                        break;
                    }
                    prevPageNumber = dp.GetPrevPageNumber();
                }
                if (prevDataPage != null)
                {
                    prevPos = new IndexData.Position(prevDataPage, (prevDataPage.GetEntries().Count -
                         1));
                }
            }
            return prevPos;
        }

        /// <summary>
        /// Returns the valid insertion point for an index indicating a missing
        /// entry.
        /// </summary>
        /// <remarks>
        /// Returns the valid insertion point for an index indicating a missing
        /// entry.
        /// </remarks>
        protected internal static int MissingIndexToInsertionPoint(int idx)
        {
            return -(idx + 1);
        }

        /// <summary>
        /// Constructs an array of values appropriate for this index from the given
        /// column values, expected to match the columns for this index.
        /// </summary>
        /// <remarks>
        /// Constructs an array of values appropriate for this index from the given
        /// column values, expected to match the columns for this index.
        /// </remarks>
        /// <returns>the appropriate sparse array of data</returns>
        /// <exception cref="System.ArgumentException">
        /// if the wrong number of values are
        /// provided
        /// </exception>
        public virtual object[] ConstructIndexRowFromEntry(params object[] values)
        {
            if (values.Length != _columns.Count)
            {
                throw new ArgumentException("Wrong number of column values given " + values.Length
                     + ", expected " + _columns.Count);
            }
            int valIdx = 0;
            object[] idxRow = new object[GetTable().GetColumnCount()];
            foreach (IndexData.ColumnDescriptor col in _columns)
            {
                idxRow[col.GetColumnIndex()] = values[valIdx++];
            }
            return idxRow;
        }

        /// <summary>
        /// Constructs an array of values appropriate for this index from the given
        /// column value.
        /// </summary>
        /// <remarks>
        /// Constructs an array of values appropriate for this index from the given
        /// column value.
        /// </remarks>
        /// <returns>
        /// the appropriate sparse array of data or
        /// <code>null</code>
        /// if not all
        /// columns for this index were provided
        /// </returns>
        public virtual object[] ConstructIndexRow(string colName, object value)
        {
            return ConstructIndexRow(Sharpen.Collections.SingletonMap(colName, value));
        }

        /// <summary>
        /// Constructs an array of values appropriate for this index from the given
        /// column values.
        /// </summary>
        /// <remarks>
        /// Constructs an array of values appropriate for this index from the given
        /// column values.
        /// </remarks>
        /// <returns>
        /// the appropriate sparse array of data or
        /// <code>null</code>
        /// if not all
        /// columns for this index were provided
        /// </returns>
        public virtual object[] ConstructIndexRow(IDictionary<string, object> row)
        {
            foreach (IndexData.ColumnDescriptor col in _columns)
            {
                if (!row.ContainsKey(col.GetName()))
                {
                    return null;
                }
            }
            object[] idxRow = new object[GetTable().GetColumnCount()];
            foreach (IndexData.ColumnDescriptor col_1 in _columns)
            {
                idxRow[col_1.GetColumnIndex()] = row.Get(col_1.GetName());
            }
            return idxRow;
        }

        public override string ToString()
        {
            StringBuilder rtn = new StringBuilder();
            rtn.Append("\n\tData number: ").Append(_number);
            rtn.Append("\n\tPage number: ").Append(_rootPageNumber);
            rtn.Append("\n\tIs Backing Primary Key: ").Append(IsBackingPrimaryKey());
            rtn.Append("\n\tIs Unique: ").Append(IsUnique());
            rtn.Append("\n\tIgnore Nulls: ").Append(ShouldIgnoreNulls());
            rtn.Append("\n\tColumns: ").Append(_columns);
            rtn.Append("\n\tInitialized: ").Append(_initialized);
            if (_initialized)
            {
                try
                {
                    rtn.Append("\n\tEntryCount: ").Append(GetEntryCount());
                }
                catch (IOException e)
                {
                    throw new RuntimeException(e);
                }
            }
            return rtn.ToString();
        }

        /// <summary>Write the given index page out to a buffer</summary>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal virtual void WriteDataPage(IndexData.DataPage dataPage)
        {
            if (dataPage.GetCompressedEntrySize() > _maxPageEntrySize)
            {
                if (this is SimpleIndexData)
                {
                    throw new NotSupportedException("FIXME cannot write large index yet, see Database javadoc for info on enabling large index support"
                        );
                }
                throw new InvalidOperationException("data page is too large");
            }
            ByteBuffer buffer = _indexBufferH.GetPageBuffer(GetPageChannel());
            WriteDataPage(buffer, dataPage, GetTable().GetTableDefPageNumber(), GetFormat());
            GetPageChannel().WritePage(buffer, dataPage.GetPageNumber());
        }

        /// <summary>Writes the data page info to the given buffer.</summary>
        /// <remarks>Writes the data page info to the given buffer.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal static void WriteDataPage(ByteBuffer buffer, IndexData.DataPage
             dataPage, int tdefPageNumber, JetFormat format)
        {
            buffer.Put(dataPage.IsLeaf() ? PageTypes.INDEX_LEAF : PageTypes.INDEX_NODE);
            //Page type
            buffer.Put(unchecked((byte)unchecked((int)(0x01))));
            //Unknown
            buffer.PutShort((short)0);
            //Free space
            buffer.PutInt(tdefPageNumber);
            buffer.PutInt(0);
            //Unknown
            buffer.PutInt(dataPage.GetPrevPageNumber());
            //Prev page
            buffer.PutInt(dataPage.GetNextPageNumber());
            //Next page
            buffer.PutInt(dataPage.GetChildTailPageNumber());
            //ChildTail page
            byte[] entryPrefix = dataPage.GetEntryPrefix();
            buffer.PutShort((short)entryPrefix.Length);
            // entry prefix byte count
            buffer.Put(unchecked((byte)0));
            //Unknown
            byte[] entryMask = new byte[format.SIZE_INDEX_ENTRY_MASK];
            // first entry includes the prefix
            int totalSize = entryPrefix.Length;
            foreach (IndexData.Entry entry in dataPage.GetEntries())
            {
                totalSize += (entry.Size() - entryPrefix.Length);
                int idx = totalSize / 8;
                entryMask[idx] |= (byte)(1 << (totalSize % 8));
            }
            buffer.Put(entryMask);
            // first entry includes the prefix
            buffer.Put(entryPrefix);
            foreach (IndexData.Entry entry_1 in dataPage.GetEntries())
            {
                entry_1.Write(buffer, entryPrefix);
            }
            // update free space
            buffer.PutShort(2, (short)(format.PAGE_SIZE - buffer.Position()));
        }

        /// <summary>
        /// Reads an index page, populating the correct collection based on the page
        /// type (node or leaf).
        /// </summary>
        /// <remarks>
        /// Reads an index page, populating the correct collection based on the page
        /// type (node or leaf).
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal virtual void ReadDataPage(IndexData.DataPage dataPage)
        {
            ByteBuffer buffer = _indexBufferH.GetPageBuffer(GetPageChannel());
            GetPageChannel().ReadPage(buffer, dataPage.GetPageNumber());
            bool isLeaf = IsLeafPage(buffer);
            dataPage.SetLeaf(isLeaf);
            // note, "header" data is in LITTLE_ENDIAN format, entry data is in
            // BIG_ENDIAN format
            int entryPrefixLength = ByteUtil.GetUnsignedShort(buffer, GetFormat().OFFSET_INDEX_COMPRESSED_BYTE_COUNT
                );
            int entryMaskLength = GetFormat().SIZE_INDEX_ENTRY_MASK;
            int entryMaskPos = GetFormat().OFFSET_INDEX_ENTRY_MASK;
            int entryPos = entryMaskPos + entryMaskLength;
            int lastStart = 0;
            int totalEntrySize = 0;
            byte[] entryPrefix = null;
            IList<IndexData.Entry> entries = new AList<IndexData.Entry>();
            TempBufferHolder tmpEntryBufferH = TempBufferHolder.NewHolder(TempBufferHolder.Type
                .HARD, true, ENTRY_BYTE_ORDER);
            IndexData.Entry prevEntry = FIRST_ENTRY;
            for (int i = 0; i < entryMaskLength; i++)
            {
                byte entryMask = buffer.Get(entryMaskPos + i);
                for (int j = 0; j < 8; j++)
                {
                    if ((entryMask & (1 << j)) != 0)
                    {
                        int length = (i * 8) + j - lastStart;
                        buffer.Position(entryPos + lastStart);
                        // determine if we can read straight from the index page (if no
                        // entryPrefix).  otherwise, create temp buf with complete entry.
                        ByteBuffer curEntryBuffer = buffer;
                        int curEntryLen = length;
                        if (entryPrefix != null)
                        {
                            curEntryBuffer = GetTempEntryBuffer(buffer, length, entryPrefix, tmpEntryBufferH);
                            curEntryLen += entryPrefix.Length;
                        }
                        totalEntrySize += curEntryLen;
                        IndexData.Entry entry = NewEntry(curEntryBuffer, curEntryLen, isLeaf);
                        if (prevEntry.CompareTo(entry) >= 0)
                        {
                            throw new IOException("Unexpected order in index entries, " + prevEntry + " >= "
                                + entry);
                        }
                        entries.AddItem(entry);
                        if ((entries.Count == 1) && (entryPrefixLength > 0))
                        {
                            // read any shared entry prefix
                            entryPrefix = new byte[entryPrefixLength];
                            buffer.Position(entryPos + lastStart);
                            buffer.Get(entryPrefix);
                        }
                        lastStart += length;
                        prevEntry = entry;
                    }
                }
            }
            dataPage.SetEntryPrefix(entryPrefix != null ? entryPrefix : EMPTY_PREFIX);
            dataPage.SetEntries(entries);
            dataPage.SetTotalEntrySize(totalEntrySize);
            int prevPageNumber = buffer.GetInt(GetFormat().OFFSET_PREV_INDEX_PAGE);
            int nextPageNumber = buffer.GetInt(GetFormat().OFFSET_NEXT_INDEX_PAGE);
            int childTailPageNumber = buffer.GetInt(GetFormat().OFFSET_CHILD_TAIL_INDEX_PAGE);
            dataPage.SetPrevPageNumber(prevPageNumber);
            dataPage.SetNextPageNumber(nextPageNumber);
            dataPage.SetChildTailPageNumber(childTailPageNumber);
        }

        /// <summary>Returns a new Entry of the correct type for the given data and page type.
        /// 	</summary>
        /// <remarks>Returns a new Entry of the correct type for the given data and page type.
        /// 	</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private IndexData.Entry NewEntry(ByteBuffer buffer, int entryLength, bool isLeaf)
        {
            if (isLeaf)
            {
                return new IndexData.Entry(buffer, entryLength);
            }
            return new IndexData.NodeEntry(buffer, entryLength);
        }

        /// <summary>
        /// Returns an entry buffer containing the relevant data for an entry given
        /// the valuePrefix.
        /// </summary>
        /// <remarks>
        /// Returns an entry buffer containing the relevant data for an entry given
        /// the valuePrefix.
        /// </remarks>
        private ByteBuffer GetTempEntryBuffer(ByteBuffer indexPage, int entryLen, byte[]
            valuePrefix, TempBufferHolder tmpEntryBufferH)
        {
            ByteBuffer tmpEntryBuffer = tmpEntryBufferH.GetBuffer(GetPageChannel(), valuePrefix
                .Length + entryLen);
            // combine valuePrefix and rest of entry from indexPage, then prep for
            // reading
            tmpEntryBuffer.Put(valuePrefix);
            tmpEntryBuffer.Put(((byte[])indexPage.Array()), indexPage.Position(), entryLen);
            tmpEntryBuffer.Flip();
            return tmpEntryBuffer;
        }

        /// <summary>Determines if the given index page is a leaf or node page.</summary>
        /// <remarks>Determines if the given index page is a leaf or node page.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private static bool IsLeafPage(ByteBuffer buffer)
        {
            byte pageType = buffer.Get(0);
            if (pageType == PageTypes.INDEX_LEAF)
            {
                return true;
            }
            else
            {
                if (pageType == PageTypes.INDEX_NODE)
                {
                    return false;
                }
            }
            throw new IOException("Unexpected page type " + pageType);
        }

        /// <summary>
        /// Determines the number of
        /// <code>null</code>
        /// values for this index from the
        /// given row.
        /// </summary>
        private int CountNullValues(object[] values)
        {
            if (values == null)
            {
                return _columns.Count;
            }
            // annoyingly, the values array could come from different sources, one
            // of which will make it a different size than the other.  we need to
            // handle both situations.
            int nullCount = 0;
            foreach (IndexData.ColumnDescriptor col in _columns)
            {
                object value = values[col.GetColumnIndex()];
                if (col.IsNullValue(value))
                {
                    ++nullCount;
                }
            }
            return nullCount;
        }

        /// <summary>Creates the entry bytes for a row of values.</summary>
        /// <remarks>Creates the entry bytes for a row of values.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private byte[] CreateEntryBytes(object[] values)
        {
            if (values == null)
            {
                return null;
            }
            if (_entryBuffer == null)
            {
                _entryBuffer = new ByteUtil.ByteStream();
            }
            _entryBuffer.Reset();
            foreach (IndexData.ColumnDescriptor col in _columns)
            {
                object value = values[col.GetColumnIndex()];
                if (Column.IsRawData(value))
                {
                    // ignore it, we could not parse it
                    continue;
                }
                if (value == MIN_VALUE)
                {
                    // null is the "least" value
                    _entryBuffer.Write(IndexCodes.GetNullEntryFlag(col.IsAscending()));
                    continue;
                }
                if (value == MAX_VALUE)
                {
                    // the opposite null is the "greatest" value
                    _entryBuffer.Write(IndexCodes.GetNullEntryFlag(!col.IsAscending()));
                    continue;
                }
                col.WriteValue(value, _entryBuffer);
            }
            return _entryBuffer.ToByteArray();
        }

        /// <summary>Writes the current index state to the database.</summary>
        /// <remarks>
        /// Writes the current index state to the database.  Index has already been
        /// initialized.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal abstract void UpdateImpl();

        /// <summary>Reads the actual index entries.</summary>
        /// <remarks>Reads the actual index entries.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal abstract void ReadIndexEntries();

        /// <summary>Finds the data page for the given entry.</summary>
        /// <remarks>Finds the data page for the given entry.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal abstract IndexData.DataPage FindDataPage(IndexData.Entry entry
            );

        /// <summary>Gets the data page for the pageNumber.</summary>
        /// <remarks>Gets the data page for the pageNumber.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal abstract IndexData.DataPage GetDataPage(int pageNumber);

        /// <summary>Flips the first bit in the byte at the given index.</summary>
        /// <remarks>Flips the first bit in the byte at the given index.</remarks>
        private static byte[] FlipFirstBitInByte(byte[] value, int index)
        {
            value[index] = unchecked((byte)(value[index] ^ unchecked((int)(0x80))));
            return value;
        }

        /// <summary>Flips all the bits in the byte array.</summary>
        /// <remarks>Flips all the bits in the byte array.</remarks>
        private static byte[] FlipBytes(byte[] value)
        {
            return FlipBytes(value, 0, value.Length);
        }

        /// <summary>Flips the bits in the specified bytes in the byte array.</summary>
        /// <remarks>Flips the bits in the specified bytes in the byte array.</remarks>
        internal static byte[] FlipBytes(byte[] value, int offset, int length)
        {
            for (int i = offset; i < (offset + length); ++i)
            {
                value[i] = unchecked((byte)(~value[i]));
            }
            return value;
        }

        /// <summary>Writes the value of the given column type to a byte array and returns it.
        /// 	</summary>
        /// <remarks>Writes the value of the given column type to a byte array and returns it.
        /// 	</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private static byte[] EncodeNumberColumnValue(object value, Column column)
        {
            // always write in big endian order
            return ((byte[])column.Write(value, 0, ENTRY_BYTE_ORDER).Array());
        }

        /// <summary>Creates one of the special index entries.</summary>
        /// <remarks>Creates one of the special index entries.</remarks>
        private static IndexData.Entry CreateSpecialEntry(RowId rowId)
        {
            return new IndexData.Entry((byte[])null, rowId);
        }

        /// <summary>Constructs a ColumnDescriptor of the relevant type for the given Column.
        /// 	</summary>
        /// <remarks>Constructs a ColumnDescriptor of the relevant type for the given Column.
        /// 	</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private IndexData.ColumnDescriptor NewColumnDescriptor(Column col, byte flags)
        {
            switch (col.GetDataType())
            {
                case DataType.TEXT:
                case DataType.MEMO:
                    {
                        Column.SortOrder sortOrder = col.GetTextSortOrder();
                        if (Column.GENERAL_LEGACY_SORT_ORDER.Equals(sortOrder))
                        {
                            return new IndexData.GenLegTextColumnDescriptor(col, flags);
                        }
                        if (Column.GENERAL_SORT_ORDER.Equals(sortOrder))
                        {
                            return new IndexData.GenTextColumnDescriptor(col, flags);
                        }
                        // unsupported sort order
                        System.Console.Error.WriteLine("Unsupported collating sort order " + sortOrder +
                            " for text index, making read-only");
                        SetReadOnly();
                        return new IndexData.ReadOnlyColumnDescriptor(col, flags);
                    }

                case DataType.INT:
                case DataType.LONG:
                case DataType.MONEY:
                    {
                        return new IndexData.IntegerColumnDescriptor(col, flags);
                    }

                case DataType.FLOAT:
                case DataType.DOUBLE:
                case DataType.SHORT_DATE_TIME:
                    {
                        return new IndexData.FloatingPointColumnDescriptor(col, flags);
                    }

                case DataType.NUMERIC:
                    {
                        return (col.GetFormat().LEGACY_NUMERIC_INDEXES ? new IndexData.LegacyFixedPointColumnDescriptor
                            (col, flags) : new IndexData.FixedPointColumnDescriptor(col, flags));
                    }

                case DataType.BYTE:
                    {
                        return new IndexData.ByteColumnDescriptor(col, flags);
                    }

                case DataType.BOOLEAN:
                    {
                        return new IndexData.BooleanColumnDescriptor(col, flags);
                    }

                case DataType.GUID:
                    {
                        return new IndexData.GuidColumnDescriptor(col, flags);
                    }

                default:
                    {
                        // FIXME we can't modify this index at this point in time
                        System.Console.Error.WriteLine("Unsupported data type " + col.GetDataType() + " for index, making read-only"
                            );
                        SetReadOnly();
                        return new IndexData.ReadOnlyColumnDescriptor(col, flags);
                    }
            }
        }

        /// <summary>Returns the EntryType based on the given entry info.</summary>
        /// <remarks>Returns the EntryType based on the given entry info.</remarks>
        private static IndexData.EntryType DetermineEntryType(byte[] entryBytes, RowId rowId
            )
        {
            if (entryBytes != null)
            {
                return ((rowId.GetRowIdType() == RowId.Type.NORMAL) ? IndexData.EntryType.NORMAL : ((rowId
                    .GetRowIdType() == RowId.Type.ALWAYS_FIRST) ? IndexData.EntryType.FIRST_VALID : IndexData.EntryType
                    .LAST_VALID));
            }
            else
            {
                if (!rowId.IsValid())
                {
                    // this is a "special" entry (first/last)
                    return ((rowId.GetRowIdType() == RowId.Type.ALWAYS_FIRST) ? IndexData.EntryType.ALWAYS_FIRST
                         : IndexData.EntryType.ALWAYS_LAST);
                }
            }
            throw new ArgumentException("Values was null for valid entry");
        }

        /// <summary>
        /// Returns the maximum amount of entry data which can be encoded on any
        /// index page.
        /// </summary>
        /// <remarks>
        /// Returns the maximum amount of entry data which can be encoded on any
        /// index page.
        /// </remarks>
        private static int CalcMaxPageEntrySize(JetFormat format)
        {
            // the max data we can fit on a page is the min of the space on the page
            // vs the number of bytes which can be encoded in the entry mask
            int pageDataSize = (format.PAGE_SIZE - (format.OFFSET_INDEX_ENTRY_MASK + format.SIZE_INDEX_ENTRY_MASK
                ));
            int entryMaskSize = (format.SIZE_INDEX_ENTRY_MASK * 8);
            return Math.Min(pageDataSize, entryMaskSize);
        }

        /// <summary>Information about the columns in an index.</summary>
        /// <remarks>
        /// Information about the columns in an index.  Also encodes new index
        /// values.
        /// </remarks>
        public abstract class ColumnDescriptor
        {
            private readonly Column _column;

            private readonly byte _flags;

            /// <exception cref="System.IO.IOException"></exception>
            public ColumnDescriptor(Column column, byte flags)
            {
                _column = column;
                _flags = flags;
            }

            public virtual Column GetColumn()
            {
                return _column;
            }

            public virtual byte GetFlags()
            {
                return _flags;
            }

            public virtual bool IsAscending()
            {
                return ((GetFlags() & ASCENDING_COLUMN_FLAG) != 0);
            }

            public virtual int GetColumnIndex()
            {
                return GetColumn().GetColumnIndex();
            }

            public virtual string GetName()
            {
                return GetColumn().GetName();
            }

            protected internal virtual bool IsNullValue(object value)
            {
                return (value == null);
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal void WriteValue(object value, ByteUtil.ByteStream bout)
            {
                if (IsNullValue(value))
                {
                    // write null value
                    bout.Write(IndexCodes.GetNullEntryFlag(IsAscending()));
                    return;
                }
                // write the start flag
                bout.Write(IndexCodes.GetStartEntryFlag(IsAscending()));
                // write the rest of the value
                WriteNonNullValue(value, bout);
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract void WriteNonNullValue(object value, ByteUtil.ByteStream
                 bout);

            public override string ToString()
            {
                return "ColumnDescriptor " + GetColumn() + "\nflags: " + GetFlags();
            }
        }

        /// <summary>ColumnDescriptor for integer based columns.</summary>
        /// <remarks>ColumnDescriptor for integer based columns.</remarks>
        internal sealed class IntegerColumnDescriptor : IndexData.ColumnDescriptor
        {
            /// <exception cref="System.IO.IOException"></exception>
            internal IntegerColumnDescriptor(Column column, byte flags) : base(column, flags)
            {
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void WriteNonNullValue(object value, ByteUtil.ByteStream
                 bout)
            {
                byte[] valueBytes = EncodeNumberColumnValue(value, GetColumn());
                // bit twiddling rules:
                // - isAsc  => flipFirstBit
                // - !isAsc => flipFirstBit, flipBytes
                FlipFirstBitInByte(valueBytes, 0);
                if (!IsAscending())
                {
                    FlipBytes(valueBytes);
                }
                bout.Write(valueBytes);
            }
        }

        /// <summary>ColumnDescriptor for floating point based columns.</summary>
        /// <remarks>ColumnDescriptor for floating point based columns.</remarks>
        internal sealed class FloatingPointColumnDescriptor : IndexData.ColumnDescriptor
        {
            /// <exception cref="System.IO.IOException"></exception>
            internal FloatingPointColumnDescriptor(Column column, byte flags) : base(column, flags
                )
            {
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void WriteNonNullValue(object value, ByteUtil.ByteStream
                 bout)
            {
                byte[] valueBytes = EncodeNumberColumnValue(value, GetColumn());
                // determine if the number is negative by testing if the first bit is
                // set
                bool isNegative = ((valueBytes[0] & unchecked((int)(0x80))) != 0);
                // bit twiddling rules:
                // isAsc && !isNeg => flipFirstBit
                // isAsc && isNeg => flipBytes
                // !isAsc && !isNeg => flipFirstBit, flipBytes
                // !isAsc && isNeg => nothing
                if (!isNegative)
                {
                    FlipFirstBitInByte(valueBytes, 0);
                }
                if (isNegative == IsAscending())
                {
                    FlipBytes(valueBytes);
                }
                bout.Write(valueBytes);
            }
        }

        /// <summary>ColumnDescriptor for fixed point based columns (legacy sort order).</summary>
        /// <remarks>ColumnDescriptor for fixed point based columns (legacy sort order).</remarks>
        internal class LegacyFixedPointColumnDescriptor : IndexData.ColumnDescriptor
        {
            /// <exception cref="System.IO.IOException"></exception>
            internal LegacyFixedPointColumnDescriptor(Column column, byte flags) : base(column
                , flags)
            {
            }

            protected internal virtual void HandleNegationAndOrder(bool isNegative, byte[] valueBytes
                )
            {
                if (isNegative == IsAscending())
                {
                    FlipBytes(valueBytes);
                }
                // reverse the sign byte (after any previous byte flipping)
                valueBytes[0] = (isNegative ? unchecked((byte)unchecked((int)(0x00))) : unchecked(
                    (byte)unchecked((int)(0xFF))));
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void WriteNonNullValue(object value, ByteUtil.ByteStream
                 bout)
            {
                byte[] valueBytes = EncodeNumberColumnValue(value, GetColumn());
                // determine if the number is negative by testing if the first bit is
                // set
                bool isNegative = ((valueBytes[0] & unchecked((int)(0x80))) != 0);
                // bit twiddling rules:
                // isAsc && !isNeg => setReverseSignByte             => FF 00 00 ...
                // isAsc && isNeg => flipBytes, setReverseSignByte   => 00 FF FF ...
                // !isAsc && !isNeg => flipBytes, setReverseSignByte => FF FF FF ...
                // !isAsc && isNeg => setReverseSignByte             => 00 00 00 ...
                // v2007 bit twiddling rules (old ordering was a bug, MS kb 837148):
                // isAsc && !isNeg => setSignByte 0xFF            => FF 00 00 ...
                // isAsc && isNeg => setSignByte 0xFF, flipBytes  => 00 FF FF ...
                // !isAsc && !isNeg => setSignByte 0xFF           => FF 00 00 ...
                // !isAsc && isNeg => setSignByte 0xFF, flipBytes => 00 FF FF ...
                HandleNegationAndOrder(isNegative, valueBytes);
                bout.Write(valueBytes);
            }
        }

        /// <summary>ColumnDescriptor for new-style fixed point based columns.</summary>
        /// <remarks>ColumnDescriptor for new-style fixed point based columns.</remarks>
        internal sealed class FixedPointColumnDescriptor : IndexData.LegacyFixedPointColumnDescriptor
        {
            /// <exception cref="System.IO.IOException"></exception>
            internal FixedPointColumnDescriptor(Column column, byte flags) : base(column, flags
                )
            {
            }

            protected internal override void HandleNegationAndOrder(bool isNegative, byte[] valueBytes
                )
            {
                // see notes above in FixedPointColumnDescriptor for bit twiddling rules
                // reverse the sign byte (before any byte flipping)
                valueBytes[0] = unchecked((byte)unchecked((int)(0xFF)));
                if (isNegative == IsAscending())
                {
                    FlipBytes(valueBytes);
                }
            }
        }

        /// <summary>ColumnDescriptor for byte based columns.</summary>
        /// <remarks>ColumnDescriptor for byte based columns.</remarks>
        internal sealed class ByteColumnDescriptor : IndexData.ColumnDescriptor
        {
            /// <exception cref="System.IO.IOException"></exception>
            internal ByteColumnDescriptor(Column column, byte flags) : base(column, flags)
            {
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void WriteNonNullValue(object value, ByteUtil.ByteStream
                 bout)
            {
                byte[] valueBytes = EncodeNumberColumnValue(value, GetColumn());
                // bit twiddling rules:
                // - isAsc  => nothing
                // - !isAsc => flipBytes
                if (!IsAscending())
                {
                    FlipBytes(valueBytes);
                }
                bout.Write(valueBytes);
            }
        }

        /// <summary>ColumnDescriptor for boolean columns.</summary>
        /// <remarks>ColumnDescriptor for boolean columns.</remarks>
        internal sealed class BooleanColumnDescriptor : IndexData.ColumnDescriptor
        {
            /// <exception cref="System.IO.IOException"></exception>
            internal BooleanColumnDescriptor(Column column, byte flags) : base(column, flags)
            {
            }

            protected internal override bool IsNullValue(object value)
            {
                // null values are handled as booleans
                return false;
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void WriteNonNullValue(object value, ByteUtil.ByteStream
                 bout)
            {
                bout.Write(Column.ToBooleanValue(value) ? (IsAscending() ? IndexCodes.ASC_BOOLEAN_TRUE : IndexCodes.DESC_BOOLEAN_TRUE
                    ) : (IsAscending() ? IndexCodes.ASC_BOOLEAN_FALSE : IndexCodes.DESC_BOOLEAN_FALSE));
            }
        }

        /// <summary>ColumnDescriptor for "general legacy" sort order text based columns.</summary>
        /// <remarks>ColumnDescriptor for "general legacy" sort order text based columns.</remarks>
        internal sealed class GenLegTextColumnDescriptor : IndexData.ColumnDescriptor
        {
            /// <exception cref="System.IO.IOException"></exception>
            internal GenLegTextColumnDescriptor(Column column, byte flags) : base(column, flags
                )
            {
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void WriteNonNullValue(object value, ByteUtil.ByteStream
                 bout)
            {
                GeneralLegacyIndexCodes.GEN_LEG_INSTANCE.WriteNonNullIndexTextValue(value, bout,
                    IsAscending());
            }
        }

        /// <summary>ColumnDescriptor for "general" sort order (2010+) text based columns.</summary>
        /// <remarks>ColumnDescriptor for "general" sort order (2010+) text based columns.</remarks>
        internal sealed class GenTextColumnDescriptor : IndexData.ColumnDescriptor
        {
            /// <exception cref="System.IO.IOException"></exception>
            internal GenTextColumnDescriptor(Column column, byte flags) : base(column, flags)
            {
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void WriteNonNullValue(object value, ByteUtil.ByteStream
                 bout)
            {
                GeneralIndexCodes.GEN_INSTANCE.WriteNonNullIndexTextValue(value, bout, IsAscending
                    ());
            }
        }

        /// <summary>ColumnDescriptor for guid columns.</summary>
        /// <remarks>ColumnDescriptor for guid columns.</remarks>
        internal sealed class GuidColumnDescriptor : IndexData.ColumnDescriptor
        {
            /// <exception cref="System.IO.IOException"></exception>
            internal GuidColumnDescriptor(Column column, byte flags) : base(column, flags)
            {
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void WriteNonNullValue(object value, ByteUtil.ByteStream
                 bout)
            {
                byte[] valueBytes = EncodeNumberColumnValue(value, GetColumn());
                // index format <8-bytes> 0x09 <8-bytes> 0x08
                // bit twiddling rules:
                // - isAsc  => nothing
                // - !isAsc => flipBytes, _but keep 09 unflipped_!      
                if (!IsAscending())
                {
                    FlipBytes(valueBytes);
                }
                bout.Write(valueBytes, 0, 8);
                bout.Write(IndexCodes.MID_GUID);
                bout.Write(valueBytes, 8, 8);
                bout.Write(IsAscending() ? IndexCodes.ASC_END_GUID : IndexCodes.DESC_END_GUID);
            }
        }

        /// <summary>ColumnDescriptor for columns which we cannot currently write.</summary>
        /// <remarks>ColumnDescriptor for columns which we cannot currently write.</remarks>
        internal sealed class ReadOnlyColumnDescriptor : IndexData.ColumnDescriptor
        {
            /// <exception cref="System.IO.IOException"></exception>
            internal ReadOnlyColumnDescriptor(Column column, byte flags) : base(column, flags)
            {
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void WriteNonNullValue(object value, ByteUtil.ByteStream
                 bout)
            {
                throw new NotSupportedException("should not be called");
            }
        }

        /// <summary>A single leaf entry in an index (points to a single row)</summary>
        public class Entry : IComparable<IndexData.Entry>
        {
            /// <summary>page/row on which this row is stored</summary>
            private readonly RowId _rowId;

            /// <summary>the entry value</summary>
            private readonly byte[] _entryBytes;

            /// <summary>comparable type for the entry</summary>
            private readonly IndexData.EntryType _type;

            /// <summary>Create a new entry</summary>
            /// <param name="entryBytes">encoded bytes for this index entry</param>
            /// <param name="rowId">rowId in which the row is stored</param>
            /// <param name="type">the type of the entry</param>
            public Entry(byte[] entryBytes, RowId rowId, IndexData.EntryType type)
            {
                _rowId = rowId;
                _entryBytes = entryBytes;
                _type = type;
            }

            /// <summary>Create a new entry</summary>
            /// <param name="entryBytes">encoded bytes for this index entry</param>
            /// <param name="rowId">rowId in which the row is stored</param>
            public Entry(byte[] entryBytes, RowId rowId) : this(entryBytes, rowId, DetermineEntryType
                (entryBytes, rowId))
            {
            }

            /// <summary>Read an existing entry in from a buffer</summary>
            /// <exception cref="System.IO.IOException"></exception>
            public Entry(ByteBuffer buffer, int entryLen) : this(buffer, entryLen, 0)
            {
            }

            /// <summary>Read an existing entry in from a buffer</summary>
            /// <exception cref="System.IO.IOException"></exception>
            public Entry(ByteBuffer buffer, int entryLen, int extraTrailingLen)
            {
                // we need 4 trailing bytes for the rowId, plus whatever the caller
                // wants
                int colEntryLen = entryLen - (4 + extraTrailingLen);
                // read the entry bytes
                _entryBytes = new byte[colEntryLen];
                buffer.Get(_entryBytes);
                // read the rowId
                int page = ByteUtil.Get3ByteInt(buffer, ENTRY_BYTE_ORDER);
                int row = ByteUtil.GetUnsignedByte(buffer);
                _rowId = new RowId(page, row);
                _type = IndexData.EntryType.NORMAL;
            }

            public virtual RowId GetRowId()
            {
                return _rowId;
            }

            public virtual IndexData.EntryType GetEntryType()
            {
                return _type;
            }

            public virtual int GetSubPageNumber()
            {
                throw new NotSupportedException();
            }

            public virtual bool IsLeafEntry()
            {
                return true;
            }

            public virtual bool IsValid()
            {
                return (_entryBytes != null);
            }

            protected internal byte[] GetEntryBytes()
            {
                return _entryBytes;
            }

            /// <summary>Size of this entry in the db.</summary>
            /// <remarks>Size of this entry in the db.</remarks>
            protected internal virtual int Size()
            {
                // need 4 trailing bytes for the rowId
                return _entryBytes.Length + 4;
            }

            /// <summary>Write this entry into a buffer</summary>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal virtual void Write(ByteBuffer buffer, byte[] prefix)
            {
                if (prefix.Length <= _entryBytes.Length)
                {
                    // write entry bytes, not including prefix
                    buffer.Put(_entryBytes, prefix.Length, (_entryBytes.Length - prefix.Length));
                    ByteUtil.Put3ByteInt(buffer, GetRowId().GetPageNumber(), ENTRY_BYTE_ORDER);
                }
                else
                {
                    if (prefix.Length <= (_entryBytes.Length + 3))
                    {
                        // the prefix includes part of the page number, write to temp buffer
                        // and copy last bytes to output buffer
                        ByteBuffer tmp = ByteBuffer.Allocate(3);
                        ByteUtil.Put3ByteInt(tmp, GetRowId().GetPageNumber(), ENTRY_BYTE_ORDER);
                        tmp.Flip();
                        tmp.Position(prefix.Length - _entryBytes.Length);
                        buffer.Put(tmp);
                    }
                    else
                    {
                        // since the row number would never be the same if the page number is
                        // the same, nothing past the page number should ever be included in
                        // the prefix.
                        // FIXME, this could happen if page has only one row...
                        throw new InvalidOperationException("prefix should never be this long");
                    }
                }
                buffer.Put(unchecked((byte)GetRowId().GetRowNumber()));
            }

            protected internal string EntryBytesToString()
            {
                return (IsValid() ? ", Bytes = " + ByteUtil.ToHexString(ByteBuffer.Wrap(_entryBytes
                    ), _entryBytes.Length) : string.Empty);
            }

            public override string ToString()
            {
                return "RowId = " + _rowId + EntryBytesToString() + "\n";
            }

            public override int GetHashCode()
            {
                return _rowId.GetHashCode();
            }

            public override bool Equals(object o)
            {
                return ((this == o) || ((o != null) && GetEntryType().Equals(o.GetType())) && (CompareTo((
                    IndexData.Entry)o) == 0));
            }

            /// <returns>
            /// 
            /// <code>true</code>
            /// iff the entryBytes are equal between this
            /// Entry and the given Entry
            /// </returns>
            public virtual bool EqualsEntryBytes(IndexData.Entry o)
            {
                return (BYTE_CODE_COMPARATOR.Compare(_entryBytes, o._entryBytes) == 0);
            }

            public virtual int CompareTo(IndexData.Entry other)
            {
                if (this == other)
                {
                    return 0;
                }
                if (IsValid() && other.IsValid())
                {
                    // comparing two valid entries.  first, compare by actual byte values
                    int entryCmp = BYTE_CODE_COMPARATOR.Compare(_entryBytes, other._entryBytes);
                    if (entryCmp != 0)
                    {
                        return entryCmp;
                    }
                }
                else
                {
                    // if the entries are of mixed validity (or both invalid), we defer
                    // next to the EntryType
                    int typeCmp = _type.CompareTo(other._type);
                    if (typeCmp != 0)
                    {
                        return typeCmp;
                    }
                }
                // at this point we let the RowId decide the final result
                return _rowId.CompareTo(other.GetRowId());
            }

            /// <summary>
            /// Returns a copy of this entry as a node Entry with the given
            /// subPageNumber.
            /// </summary>
            /// <remarks>
            /// Returns a copy of this entry as a node Entry with the given
            /// subPageNumber.
            /// </remarks>
            protected internal virtual IndexData.Entry AsNodeEntry(int subPageNumber)
            {
                return new IndexData.NodeEntry(_entryBytes, _rowId, _type, subPageNumber);
            }
        }

        /// <summary>A single node entry in an index (points to a sub-page in the index)</summary>
        public sealed class NodeEntry : IndexData.Entry
        {
            /// <summary>index page number of the page to which this node entry refers</summary>
            private readonly int _subPageNumber;

            /// <summary>Create a new node entry</summary>
            /// <param name="entryBytes">encoded bytes for this index entry</param>
            /// <param name="rowId">rowId in which the row is stored</param>
            /// <param name="type">the type of the entry</param>
            /// <param name="subPageNumber">the sub-page to which this node entry refers</param>
            public NodeEntry(byte[] entryBytes, RowId rowId, IndexData.EntryType type, int subPageNumber
                ) : base(entryBytes, rowId, type)
            {
                _subPageNumber = subPageNumber;
            }

            /// <summary>Read an existing node entry in from a buffer</summary>
            /// <exception cref="System.IO.IOException"></exception>
            public NodeEntry(ByteBuffer buffer, int entryLen) : base(buffer, entryLen, 4)
            {
                // we need 4 trailing bytes for the sub-page number
                _subPageNumber = ByteUtil.GetInt(buffer, ENTRY_BYTE_ORDER);
            }

            public override int GetSubPageNumber()
            {
                return _subPageNumber;
            }

            public override bool IsLeafEntry()
            {
                return false;
            }

            protected internal override int Size()
            {
                // need 4 trailing bytes for the sub-page number
                return base.Size() + 4;
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void Write(ByteBuffer buffer, byte[] prefix)
            {
                base.Write(buffer, prefix);
                ByteUtil.PutInt(buffer, _subPageNumber, ENTRY_BYTE_ORDER);
            }

            public override bool Equals(object o)
            {
                return ((this == o) || ((o != null) && (GetEntryType().Equals(o.GetType())) && (CompareTo((
                    IndexData.Entry)o) == 0) && (GetSubPageNumber().Equals(((IndexData.Entry)o).GetSubPageNumber
                    ()))));
            }

            public override int GetHashCode()
            {
                return _subPageNumber.GetHashCode();
            }

            public override string ToString()
            {
                return ("Node RowId = " + GetRowId() + ", SubPage = " + _subPageNumber + EntryBytesToString
                    () + "\n");
            }
        }

        /// <summary>Utility class to traverse the entries in the Index.</summary>
        /// <remarks>
        /// Utility class to traverse the entries in the Index.  Remains valid in the
        /// face of index entry modifications.
        /// </remarks>
        public sealed class EntryCursor
        {
            /// <summary>handler for moving the page cursor forward</summary>
            private readonly IndexData.EntryCursor.DirHandler _forwardDirHandler;

            /// <summary>handler for moving the page cursor backward</summary>
            private readonly IndexData.EntryCursor.DirHandler _reverseDirHandler;

            /// <summary>the first (exclusive) row id for this cursor</summary>
            private IndexData.Position _firstPos;

            /// <summary>the last (exclusive) row id for this cursor</summary>
            private IndexData.Position _lastPos;

            /// <summary>the current entry</summary>
            private IndexData.Position _curPos;

            /// <summary>the previous entry</summary>
            private IndexData.Position _prevPos;

            /// <summary>the last read modification count on the Index.</summary>
            /// <remarks>
            /// the last read modification count on the Index.  we track this so that
            /// the cursor can detect updates to the index while traversing and act
            /// accordingly
            /// </remarks>
            private int _lastModCount;

            public IndexData.Position LastPos
            {
                get
                {
                    return _lastPos;
                }
            }

            public EntryCursor(IndexData _enclosing, IndexData.Position firstPos, IndexData.Position
                 lastPos)
            {
                this._enclosing = _enclosing;
                _forwardDirHandler = new IndexData.EntryCursor.ForwardDirHandler(this);
                _reverseDirHandler = new IndexData.EntryCursor.ReverseDirHandler(this);
                this._firstPos = firstPos;
                this._lastPos = lastPos;
                this._lastModCount = this.GetIndexModCount();
                this.Reset();
            }

            /// <summary>Returns the DirHandler for the given direction</summary>
            private IndexData.EntryCursor.DirHandler GetDirHandler(bool moveForward)
            {
                return (moveForward ? this._forwardDirHandler : this._reverseDirHandler);
            }

            public IndexData GetIndexData()
            {
                return this._enclosing;
            }

            private int GetIndexModCount()
            {
                return this._enclosing._modCount;
            }

            /// <summary>Returns the first entry (exclusive) as defined by this cursor.</summary>
            /// <remarks>Returns the first entry (exclusive) as defined by this cursor.</remarks>
            public IndexData.Entry GetFirstEntry()
            {
                return this._firstPos.GetEntry();
            }

            /// <summary>Returns the last entry (exclusive) as defined by this cursor.</summary>
            /// <remarks>Returns the last entry (exclusive) as defined by this cursor.</remarks>
            public IndexData.Entry GetLastEntry()
            {
                return this._lastPos.GetEntry();
            }

            /// <summary>
            /// Returns
            /// <code>true</code>
            /// if this cursor is up-to-date with respect to its
            /// index.
            /// </summary>
            public bool IsUpToDate()
            {
                return (this.GetIndexModCount() == this._lastModCount);
            }

            public void Reset()
            {
                this.BeforeFirst();
            }

            public void BeforeFirst()
            {
                this.Reset(Cursor.MOVE_FORWARD);
            }

            public void AfterLast()
            {
                this.Reset(Cursor.MOVE_REVERSE);
            }

            internal void Reset(bool moveForward)
            {
                this._curPos = this.GetDirHandler(moveForward).GetBeginningPosition();
                this._prevPos = this._curPos;
            }

            /// <summary>
            /// Repositions the cursor so that the next row will be the first entry
            /// &gt;= the given row.
            /// </summary>
            /// <remarks>
            /// Repositions the cursor so that the next row will be the first entry
            /// &gt;= the given row.
            /// </remarks>
            /// <exception cref="System.IO.IOException"></exception>
            public void BeforeEntry(object[] row)
            {
                this.RestorePosition(new IndexData.Entry(this._enclosing.CreateEntryBytes(row), RowId
                    .FIRST_ROW_ID));
            }

            /// <summary>
            /// Repositions the cursor so that the previous row will be the first
            /// entry &lt;= the given row.
            /// </summary>
            /// <remarks>
            /// Repositions the cursor so that the previous row will be the first
            /// entry &lt;= the given row.
            /// </remarks>
            /// <exception cref="System.IO.IOException"></exception>
            public void AfterEntry(object[] row)
            {
                this.RestorePosition(new IndexData.Entry(this._enclosing.CreateEntryBytes(row), RowId
                    .LAST_ROW_ID));
            }

            /// <returns>
            /// valid entry if there was a next entry,
            /// <code>#getLastEntry</code>
            /// otherwise
            /// </returns>
            /// <exception cref="System.IO.IOException"></exception>
            public IndexData.Entry GetNextEntry()
            {
                return this.GetAnotherPosition(Cursor.MOVE_FORWARD).GetEntry();
            }

            /// <returns>
            /// valid entry if there was a next entry,
            /// <code>#getFirstEntry</code>
            /// otherwise
            /// </returns>
            /// <exception cref="System.IO.IOException"></exception>
            public IndexData.Entry GetPreviousEntry()
            {
                return this.GetAnotherPosition(Cursor.MOVE_REVERSE).GetEntry();
            }

            /// <summary>
            /// Restores a current position for the cursor (current position becomes
            /// previous position).
            /// </summary>
            /// <remarks>
            /// Restores a current position for the cursor (current position becomes
            /// previous position).
            /// </remarks>
            /// <exception cref="System.IO.IOException"></exception>
            internal void RestorePosition(IndexData.Entry curEntry)
            {
                this.RestorePosition(curEntry, this._curPos.GetEntry());
            }

            /// <summary>Restores a current and previous position for the cursor.</summary>
            /// <remarks>Restores a current and previous position for the cursor.</remarks>
            /// <exception cref="System.IO.IOException"></exception>
            internal void RestorePosition(IndexData.Entry curEntry, IndexData.Entry
                 prevEntry)
            {
                if (!this._curPos.EqualsEntry(curEntry) || !this._prevPos.EqualsEntry(prevEntry))
                {
                    if (!this.IsUpToDate())
                    {
                        this.UpdateBounds();
                        this._lastModCount = this.GetIndexModCount();
                    }
                    this._prevPos = this.UpdatePosition(prevEntry);
                    this._curPos = this.UpdatePosition(curEntry);
                }
                else
                {
                    this.CheckForModification();
                }
            }

            /// <summary>Gets another entry in the given direction, returning the new entry.</summary>
            /// <remarks>Gets another entry in the given direction, returning the new entry.</remarks>
            /// <exception cref="System.IO.IOException"></exception>
            public IndexData.Position GetAnotherPosition(bool moveForward)
            {
                IndexData.EntryCursor.DirHandler handler = this.GetDirHandler(moveForward);
                if (this._curPos.Equals(handler.GetEndPosition()))
                {
                    if (!this.IsUpToDate())
                    {
                        this.RestorePosition(this._prevPos.GetEntry());
                    }
                    else
                    {
                        // drop through and retry moving to another entry
                        // at end, no more
                        return this._curPos;
                    }
                }
                this.CheckForModification();
                this._prevPos = this._curPos;
                this._curPos = handler.GetAnotherPosition(this._curPos);
                return this._curPos;
            }

            /// <summary>Checks the index for modifications and updates state accordingly.</summary>
            /// <remarks>Checks the index for modifications and updates state accordingly.</remarks>
            /// <exception cref="System.IO.IOException"></exception>
            private void CheckForModification()
            {
                if (!this.IsUpToDate())
                {
                    this.UpdateBounds();
                    this._prevPos = this.UpdatePosition(this._prevPos.GetEntry());
                    this._curPos = this.UpdatePosition(this._curPos.GetEntry());
                    this._lastModCount = this.GetIndexModCount();
                }
            }

            /// <summary>Updates the given position, taking boundaries into account.</summary>
            /// <remarks>Updates the given position, taking boundaries into account.</remarks>
            /// <exception cref="System.IO.IOException"></exception>
            private IndexData.Position UpdatePosition(IndexData.Entry entry)
            {
                if (!entry.IsValid())
                {
                    // no use searching if "updating" the first/last pos
                    if (this._firstPos.EqualsEntry(entry))
                    {
                        return this._firstPos;
                    }
                    else
                    {
                        if (this._lastPos.EqualsEntry(entry))
                        {
                            return this._lastPos;
                        }
                        else
                        {
                            throw new ArgumentException("Invalid entry given " + entry);
                        }
                    }
                }
                IndexData.Position pos = this._enclosing.FindEntryPosition(entry);
                if (pos.CompareTo(this._lastPos) >= 0)
                {
                    return this._lastPos;
                }
                else
                {
                    if (pos.CompareTo(this._firstPos) <= 0)
                    {
                        return this._firstPos;
                    }
                }
                return pos;
            }

            /// <summary>Updates any the boundary info (_firstPos/_lastPos).</summary>
            /// <remarks>Updates any the boundary info (_firstPos/_lastPos).</remarks>
            /// <exception cref="System.IO.IOException"></exception>
            private void UpdateBounds()
            {
                this._firstPos = this._enclosing.FindEntryPosition(this._firstPos.GetEntry());
                this._lastPos = this._enclosing.FindEntryPosition(this._lastPos.GetEntry());
            }

            public override string ToString()
            {
                return this.GetType().Name + " CurPosition " + this._curPos + ", PrevPosition " +
                     this._prevPos;
            }

            /// <summary>Handles moving the cursor in a given direction.</summary>
            /// <remarks>
            /// Handles moving the cursor in a given direction.  Separates cursor
            /// logic from value storage.
            /// </remarks>
            private abstract class DirHandler
            {
                /// <exception cref="System.IO.IOException"></exception>
                public abstract IndexData.Position GetAnotherPosition(IndexData.Position curPos);

                public abstract IndexData.Position GetBeginningPosition();

                public abstract IndexData.Position GetEndPosition();

                internal DirHandler(EntryCursor _enclosing)
                {
                    this._enclosing = _enclosing;
                }

                private readonly EntryCursor _enclosing;
            }

            /// <summary>Handles moving the cursor forward.</summary>
            /// <remarks>Handles moving the cursor forward.</remarks>
            private sealed class ForwardDirHandler : IndexData.EntryCursor.DirHandler
            {
                /// <exception cref="System.IO.IOException"></exception>
                public override IndexData.Position GetAnotherPosition(IndexData.Position curPos)
                {
                    IndexData.Position newPos = this._enclosing._enclosing.GetNextPosition(curPos);
                    if ((newPos == null) || (newPos.CompareTo(this._enclosing._lastPos) >= 0))
                    {
                        newPos = this._enclosing._lastPos;
                    }
                    return newPos;
                }

                public override IndexData.Position GetBeginningPosition()
                {
                    return this._enclosing._firstPos;
                }

                public override IndexData.Position GetEndPosition()
                {
                    return this._enclosing._lastPos;
                }

                internal ForwardDirHandler(EntryCursor _enclosing) : base(_enclosing)
                {
                    this._enclosing = _enclosing;
                }

                private readonly EntryCursor _enclosing;
            }

            /// <summary>Handles moving the cursor backward.</summary>
            /// <remarks>Handles moving the cursor backward.</remarks>
            private sealed class ReverseDirHandler : IndexData.EntryCursor.DirHandler
            {
                /// <exception cref="System.IO.IOException"></exception>
                public override IndexData.Position GetAnotherPosition(IndexData.Position curPos)
                {
                    IndexData.Position newPos = this._enclosing._enclosing.GetPreviousPosition(curPos
                        );
                    if ((newPos == null) || (newPos.CompareTo(this._enclosing._firstPos) <= 0))
                    {
                        newPos = this._enclosing._firstPos;
                    }
                    return newPos;
                }

                public override IndexData.Position GetBeginningPosition()
                {
                    return this._enclosing._lastPos;
                }

                public override IndexData.Position GetEndPosition()
                {
                    return this._enclosing._firstPos;
                }

                internal ReverseDirHandler(EntryCursor _enclosing) : base(_enclosing)
                {
                    this._enclosing = _enclosing;
                }

                private readonly EntryCursor _enclosing;
            }

            private readonly IndexData _enclosing;
        }

        /// <summary>Simple value object for maintaining some cursor state.</summary>
        /// <remarks>Simple value object for maintaining some cursor state.</remarks>
        public sealed class Position : IComparable<IndexData.Position>
        {
            /// <summary>the last known page of the given entry</summary>
            private readonly IndexData.DataPage _dataPage;

            /// <summary>the last known index of the given entry</summary>
            private readonly int _idx;

            /// <summary>the entry at the given index</summary>
            private readonly IndexData.Entry _entry;

            /// <summary>
            /// <code>true</code>
            /// if this entry does not currently exist in the entry list,
            /// <code>false</code>
            /// otherwise (this is equivalent to adding -0.5 to the
            /// _idx)
            /// </summary>
            private readonly bool _between;

            internal Position(IndexData.DataPage dataPage, int idx) : this(dataPage, idx, dataPage
                .GetEntries()[idx], false)
            {
            }

            internal Position(IndexData.DataPage dataPage, int idx, IndexData.Entry entry, bool
                 between)
            {
                _dataPage = dataPage;
                _idx = idx;
                _entry = entry;
                _between = between;
            }

            public IndexData.DataPage GetDataPage()
            {
                return _dataPage;
            }

            public int GetIndex()
            {
                return _idx;
            }

            public int GetNextIndex()
            {
                // note, _idx does not need to be advanced if it was pointing at a
                // between position
                return (_between ? _idx : (_idx + 1));
            }

            public int GetPrevIndex()
            {
                // note, we ignore the between flag here because the index will be
                // pointing at the correct next index in either the between or
                // non-between case
                return (_idx - 1);
            }

            public IndexData.Entry GetEntry()
            {
                return _entry;
            }

            public bool IsBetween()
            {
                return _between;
            }

            public bool EqualsEntry(IndexData.Entry entry)
            {
                return _entry.Equals(entry);
            }

            public int CompareTo(IndexData.Position other)
            {
                if (this == other)
                {
                    return 0;
                }
                if (_dataPage.Equals(other._dataPage))
                {
                    // "simple" index comparison (handle between-ness)
                    int idxCmp = ((_idx < other._idx) ? -1 : ((_idx > other._idx) ? 1 : ((_between ==
                         other._between) ? 0 : (_between ? -1 : 1))));
                    if (idxCmp != 0)
                    {
                        return idxCmp;
                    }
                }
                // compare the entries.
                return _entry.CompareTo(other._entry);
            }

            public override int GetHashCode()
            {
                return _entry.GetHashCode();
            }

            public override bool Equals(object o)
            {
                return ((this == o) || ((o != null) && (GetType() == o.GetType()) && (CompareTo((
                    IndexData.Position)o) == 0)));
            }

            public override string ToString()
            {
                return "Page = " + _dataPage.GetPageNumber() + ", Idx = " + _idx + ", Entry = " +
                     _entry + ", Between = " + _between;
            }
        }

        /// <summary>Object used to maintain state about an Index page.</summary>
        /// <remarks>Object used to maintain state about an Index page.</remarks>
        public abstract class DataPage
        {
            public abstract int GetPageNumber();

            public abstract bool IsLeaf();

            public abstract void SetLeaf(bool isLeaf);

            public abstract int GetPrevPageNumber();

            public abstract void SetPrevPageNumber(int pageNumber);

            public abstract int GetNextPageNumber();

            public abstract void SetNextPageNumber(int pageNumber);

            public abstract int GetChildTailPageNumber();

            public abstract void SetChildTailPageNumber(int pageNumber);

            public abstract int GetTotalEntrySize();

            public abstract void SetTotalEntrySize(int totalSize);

            public abstract byte[] GetEntryPrefix();

            public abstract void SetEntryPrefix(byte[] entryPrefix);

            public abstract IList<IndexData.Entry> GetEntries();

            public abstract void SetEntries(IList<IndexData.Entry> entries);

            /// <exception cref="System.IO.IOException"></exception>
            public abstract void AddEntry(int idx, IndexData.Entry entry);

            /// <exception cref="System.IO.IOException"></exception>
            public abstract void RemoveEntry(int idx);

            public bool IsEmpty()
            {
                return GetEntries().IsEmpty();
            }

            public int GetCompressedEntrySize()
            {
                // when written to the index page, the entryPrefix bytes will only be
                // written for the first entry, so we subtract the entry prefix size
                // from all the other entries to determine the compressed size
                return GetTotalEntrySize() - (GetEntryPrefix().Length * (GetEntries().Count - 1));
            }

            public int FindEntry(IndexData.Entry entry)
            {
                return Sharpen.Collections.BinarySearch(GetEntries(), entry);
            }

            public sealed override int GetHashCode()
            {
                return GetPageNumber();
            }

            public sealed override bool Equals(object o)
            {
                return ((this == o) || ((o != null) && (GetType() == o.GetType()) && (GetPageNumber
                    () == ((IndexData.DataPage)o).GetPageNumber())));
            }

            public sealed override string ToString()
            {
                IList<IndexData.Entry> entries = GetEntries();
                return (IsLeaf() ? "Leaf" : "Node") + "DataPage[" + GetPageNumber() + "] " + GetPrevPageNumber
                    () + ", " + GetNextPageNumber() + ", (" + GetChildTailPageNumber() + "), " + ((IsLeaf
                    () && !entries.IsEmpty()) ? ("[" + entries[0] + ", " + entries[entries.Count - 1
                    ] + "]") : entries.ToString());
            }
        }
    }
}
