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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HealthMarketScience.Jackcess
{
    /// <summary>
    /// A single database table
    /// <p>
    /// Is not thread-safe.
    /// </summary>
    /// <remarks>
    /// A single database table
    /// <p>
    /// Is not thread-safe.
    /// </remarks>
    /// <author>Tim McCune</author>
    /// <usage>_general_class_</usage>
    public class Table : Iterable<IDictionary<string, object>>
    {
        private const short OFFSET_MASK = (short)unchecked((int)(0x1FFF));

        private const short DELETED_ROW_MASK = (short)unchecked((short)(0x8000));

        private const short OVERFLOW_ROW_MASK = (short)unchecked((int)(0x4000));

        internal const int MAGIC_TABLE_NUMBER = 1625;

        private const int MAX_BYTE = 256;

        /// <summary>Table type code for system tables</summary>
        /// <usage>_intermediate_class_</usage>
        public const byte TYPE_SYSTEM = unchecked((int)(0x53));

        /// <summary>Table type code for user tables</summary>
        /// <usage>_intermediate_class_</usage>
        public const byte TYPE_USER = unchecked((int)(0x4e));

        /// <summary>enum which controls the ordering of the columns in a table.</summary>
        /// <remarks>enum which controls the ordering of the columns in a table.</remarks>
        /// <usage>_intermediate_class_</usage>
        public enum ColumnOrder
        {
            DATA,
            DISPLAY
        }

        private sealed class _IComparer_94 : IComparer<Column>
        {
            public _IComparer_94()
            {
            }

            public int Compare(Column c1, Column c2)
            {
                return ((c1.GetVarLenTableIndex() < c2.GetVarLenTableIndex()) ? -1 : ((c1.GetVarLenTableIndex
                    () > c2.GetVarLenTableIndex()) ? 1 : 0));
            }
        }

        /// <summary>
        /// comparator which sorts variable length columns based on their index into
        /// the variable length offset table
        /// </summary>
        private static readonly IComparer<Column> VAR_LEN_COLUMN_COMPARATOR = new _IComparer_94
            ();

        private sealed class _IComparer_104 : IComparer<Column>
        {
            public _IComparer_104()
            {
            }

            public int Compare(Column c1, Column c2)
            {
                return ((c1.GetDisplayIndex() < c2.GetDisplayIndex()) ? -1 : ((c1.GetDisplayIndex
                    () > c2.GetDisplayIndex()) ? 1 : 0));
            }
        }

        /// <summary>comparator which sorts columns based on their display index</summary>
        private static readonly IComparer<Column> DISPLAY_ORDER_COMPARATOR = new _IComparer_104
            ();

        /// <summary>owning database</summary>
        private readonly Database _database;

        /// <summary>additional table flags from the catalog entry</summary>
        private int _flags;

        /// <summary>Type of the table (either TYPE_SYSTEM or TYPE_USER)</summary>
        private byte _tableType;

        /// <summary>Number of actual indexes on the table</summary>
        private int _indexCount;

        /// <summary>Number of logical indexes for the table</summary>
        private int _logicalIndexCount;

        /// <summary>Number of rows in the table</summary>
        private int _rowCount;

        /// <summary>last long auto number for the table</summary>
        private int _lastLongAutoNumber;

        /// <summary>page number of the definition of this table</summary>
        private readonly int _tableDefPageNumber;

        /// <summary>max Number of columns in the table (includes previous deletions)</summary>
        private short _maxColumnCount;

        /// <summary>max Number of variable columns in the table</summary>
        private short _maxVarColumnCount;

        /// <summary>List of columns in this table, ordered by column number</summary>
        private IList<Column> _columns = new AList<Column>();

        /// <summary>List of variable length columns in this table, ordered by offset</summary>
        private IList<Column> _varColumns = new AList<Column>();

        /// <summary>
        /// List of indexes on this table (multiple logical indexes may be backed by
        /// the same index data)
        /// </summary>
        private IList<Index> _indexes = new AList<Index>();

        /// <summary>
        /// List of index datas on this table (the actual backing data for an
        /// index)
        /// </summary>
        private IList<IndexData> _indexDatas = new AList<IndexData>();

        /// <summary>Table name as stored in Database</summary>
        private readonly string _name;

        /// <summary>Usage map of pages that this table owns</summary>
        private UsageMap _ownedPages;

        /// <summary>Usage map of pages that this table owns with free space on them</summary>
        private UsageMap _freeSpacePages;

        /// <summary>modification count for the table, keeps row-states up-to-date</summary>
        private int _modCount;

        /// <summary>page buffer used to update data pages when adding rows</summary>
        private readonly TempPageHolder _addRowBufferH = TempPageHolder.NewHolder(TempBufferHolder.Type
            .SOFT);

        /// <summary>page buffer used to update the table def page</summary>
        private readonly TempPageHolder _tableDefBufferH = TempPageHolder.NewHolder(TempBufferHolder.Type
            .SOFT);

        /// <summary>buffer used to writing single rows of data</summary>
        private readonly TempBufferHolder _singleRowBufferH = TempBufferHolder.NewHolder(
            TempBufferHolder.Type.SOFT, true);

        /// <summary>
        /// "buffer" used to writing multi rows of data (will create new buffer on
        /// every call)
        /// </summary>
        private readonly TempBufferHolder _multiRowBufferH = TempBufferHolder.NewHolder(TempBufferHolder.Type
            .NONE, true);

        /// <summary>page buffer used to write out-of-line "long value" data</summary>
        private readonly TempPageHolder _longValueBufferH = TempPageHolder.NewHolder(TempBufferHolder.Type
            .SOFT);

        /// <summary>"big index support" is optional</summary>
        private readonly bool _useBigIndex;

        /// <summary>optional error handler to use when row errors are encountered</summary>
        private ErrorHandler _tableErrorHandler;

        /// <summary>properties for this table</summary>
        private PropertyMap _props;

        /// <summary>properties group for this table (and columns)</summary>
        private PropertyMaps _propertyMaps;

        /// <summary>
        /// common cursor for iterating through the table, kept here for historic
        /// reasons
        /// </summary>
        private Cursor _cursor;

        /// <summary>Only used by unit tests</summary>
        /// <exception cref="System.IO.IOException"></exception>
        internal Table(bool testing, IList<Column> columns)
        {
            if (!testing)
            {
                throw new ArgumentException();
            }
            _database = null;
            _tableDefPageNumber = PageChannel.INVALID_PAGE_NUMBER;
            _name = null;
            _useBigIndex = true;
            SetColumns(columns);
        }

        /// <param name="database">database which owns this table</param>
        /// <param name="tableBuffer">Buffer to read the table with</param>
        /// <param name="pageNumber">Page number of the table definition</param>
        /// <param name="name">Table name</param>
        /// <param name="useBigIndex">
        /// whether or not "big index support" should be enabled
        /// for the table
        /// </param>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal Table(Database database, ByteBuffer tableBuffer, int pageNumber
            , string name, int flags, bool useBigIndex)
        {
            _database = database;
            _tableDefPageNumber = pageNumber;
            _name = name;
            _flags = flags;
            _useBigIndex = useBigIndex;
            int nextPage = tableBuffer.GetInt(GetFormat().OFFSET_NEXT_TABLE_DEF_PAGE);
            ByteBuffer nextPageBuffer = null;
            while (nextPage != 0)
            {
                if (nextPageBuffer == null)
                {
                    nextPageBuffer = GetPageChannel().CreatePageBuffer();
                }
                GetPageChannel().ReadPage(nextPageBuffer, nextPage);
                nextPage = nextPageBuffer.GetInt(GetFormat().OFFSET_NEXT_TABLE_DEF_PAGE);
                ByteBuffer newBuffer = GetPageChannel().CreateBuffer(tableBuffer.Capacity() + GetFormat
                    ().PAGE_SIZE - 8);
                newBuffer.Put(tableBuffer);
                newBuffer.Put(((byte[])nextPageBuffer.Array()), 8, GetFormat().PAGE_SIZE - 8);
                tableBuffer = newBuffer;
                tableBuffer.Flip();
            }
            ReadTableDefinition(tableBuffer);
            tableBuffer = null;
        }

        /// <returns>The name of the table</returns>
        /// <usage>_general_method_</usage>
        public virtual string GetName()
        {
            return _name;
        }

        /// <summary>Whether or not this table has been marked as hidden.</summary>
        /// <remarks>Whether or not this table has been marked as hidden.</remarks>
        /// <usage>_general_method_</usage>
        public virtual bool IsHidden()
        {
            return ((_flags & Database.HIDDEN_OBJECT_FLAG) != 0);
        }

        /// <usage>_advanced_method_</usage>
        public virtual bool DoUseBigIndex()
        {
            return _useBigIndex;
        }

        /// <usage>_advanced_method_</usage>
        public virtual int GetMaxColumnCount()
        {
            return _maxColumnCount;
        }

        /// <usage>_general_method_</usage>
        public virtual int GetColumnCount()
        {
            return _columns.Count;
        }

        /// <usage>_general_method_</usage>
        public virtual Database GetDatabase()
        {
            return _database;
        }

        /// <usage>_advanced_method_</usage>
        public virtual JetFormat GetFormat()
        {
            return GetDatabase().GetFormat();
        }

        /// <usage>_advanced_method_</usage>
        public virtual PageChannel GetPageChannel()
        {
            return GetDatabase().GetPageChannel();
        }

        /// <summary>
        /// Gets the currently configured ErrorHandler (always non-
        /// <code>null</code>
        /// ).
        /// This will be used to handle all errors unless overridden at the Cursor
        /// level.
        /// </summary>
        /// <usage>_intermediate_method_</usage>
        public virtual ErrorHandler GetErrorHandler()
        {
            return ((_tableErrorHandler != null) ? _tableErrorHandler : GetDatabase().GetErrorHandler
                ());
        }

        /// <summary>Sets a new ErrorHandler.</summary>
        /// <remarks>
        /// Sets a new ErrorHandler.  If
        /// <code>null</code>
        /// , resets to using the
        /// ErrorHandler configured at the Database level.
        /// </remarks>
        /// <usage>_intermediate_method_</usage>
        public virtual void SetErrorHandler(ErrorHandler newErrorHandler)
        {
            _tableErrorHandler = newErrorHandler;
        }

        protected internal virtual int GetTableDefPageNumber()
        {
            return _tableDefPageNumber;
        }

        /// <usage>_advanced_method_</usage>
        public virtual Table.RowState CreateRowState()
        {
            return new Table.RowState(this, TempBufferHolder.Type.HARD);
        }

        protected internal virtual UsageMap.PageCursor GetOwnedPagesCursor()
        {
            return _ownedPages.Cursor();
        }

        /// <summary>
        /// Returns the <i>approximate</i> number of database pages owned by this
        /// table and all related indexes (this number does <i>not</i> take into
        /// account pages used for large OLE/MEMO fields).
        /// </summary>
        /// <remarks>
        /// Returns the <i>approximate</i> number of database pages owned by this
        /// table and all related indexes (this number does <i>not</i> take into
        /// account pages used for large OLE/MEMO fields).
        /// <p>
        /// To calculate the approximate number of bytes owned by a table:
        /// <code>
        /// int approxTableBytes = (table.getApproximateOwnedPageCount()
        /// table.getFormat().PAGE_SIZE);
        /// </code>
        /// </remarks>
        /// <usage>_intermediate_method_</usage>
        public virtual int GetApproximateOwnedPageCount()
        {
            // add a page for the table def (although that might actually be more than
            // one page)
            int count = _ownedPages.GetPageCount() + 1;
            // note, we count owned pages from _physical_ indexes, not logical indexes
            // (otherwise we could double count pages)
            foreach (IndexData indexData in _indexDatas)
            {
                count += indexData.GetOwnedPageCount();
            }
            return count;
        }

        protected internal virtual TempPageHolder GetLongValueBuffer()
        {
            return _longValueBufferH;
        }

        /// <returns>All of the columns in this table (unmodifiable List)</returns>
        /// <usage>_general_method_</usage>
        public virtual IList<Column> GetColumns()
        {
            return Sharpen.Collections.UnmodifiableList(_columns);
        }

        /// <returns>the column with the given name</returns>
        /// <usage>_general_method_</usage>
        public virtual Column GetColumn(string name)
        {
            foreach (Column column in _columns)
            {
                if (Sharpen.Runtime.EqualsIgnoreCase(column.GetName(), name))
                {
                    return column;
                }
            }
            throw new ArgumentException("Column with name " + name + " does not exist in this table"
                );
        }

        /// <summary>Only called by unit tests</summary>
        private void SetColumns(IList<Column> columns)
        {
            _columns = columns;
            int colIdx = 0;
            int varLenIdx = 0;
            int fixedOffset = 0;
            foreach (Column col in _columns)
            {
                col.SetColumnNumber((short)colIdx);
                col.SetColumnIndex(colIdx++);
                if (col.IsVariableLength())
                {
                    col.SetVarLenTableIndex(varLenIdx++);
                    _varColumns.AddItem(col);
                }
                else
                {
                    col.SetFixedDataOffset(fixedOffset);
                    fixedOffset += DataTypeUtil.GetFixedSize(col.GetDataType());
                }
            }
            _maxColumnCount = (short)_columns.Count;
            _maxVarColumnCount = (short)_varColumns.Count;
        }

        /// <returns>the properties for this table</returns>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual PropertyMap GetProperties()
        {
            if (_props == null)
            {
                _props = GetPropertyMaps().GetDefault();
            }
            return _props;
        }

        /// <returns>all PropertyMaps for this table (and columns)</returns>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal virtual PropertyMaps GetPropertyMaps()
        {
            if (_propertyMaps == null)
            {
                _propertyMaps = GetDatabase().GetPropertiesForObject(_tableDefPageNumber);
            }
            return _propertyMaps;
        }

        /// <returns>All of the Indexes on this table (unmodifiable List)</returns>
        /// <usage>_intermediate_method_</usage>
        public virtual IList<Index> GetIndexes()
        {
            return Sharpen.Collections.UnmodifiableList(_indexes);
        }

        /// <returns>the index with the given name</returns>
        /// <exception cref="System.ArgumentException">if there is no index with the given name
        /// 	</exception>
        /// <usage>_intermediate_method_</usage>
        public virtual Index GetIndex(string name)
        {
            foreach (Index index in _indexes)
            {
                if (Sharpen.Runtime.EqualsIgnoreCase(index.GetName(), name))
                {
                    return index;
                }
            }
            throw new ArgumentException("Index with name " + name + " does not exist on this table"
                );
        }

        /// <returns>the primary key index for this table</returns>
        /// <exception cref="System.ArgumentException">
        /// if there is no primary key index on this
        /// table
        /// </exception>
        /// <usage>_intermediate_method_</usage>
        public virtual Index GetPrimaryKeyIndex()
        {
            foreach (Index index in _indexes)
            {
                if (index.IsPrimaryKey())
                {
                    return index;
                }
            }
            throw new ArgumentException("Table " + GetName() + " does not have a primary key index"
                );
        }

        /// <returns>the foreign key index joining this table to the given other table</returns>
        /// <exception cref="System.ArgumentException">
        /// if there is no relationship between this
        /// table and the given table
        /// </exception>
        /// <usage>_intermediate_method_</usage>
        public virtual Index GetForeignKeyIndex(HealthMarketScience.Jackcess.Table otherTable
            )
        {
            foreach (Index index in _indexes)
            {
                if (index.IsForeignKey() && (index.GetReference() != null) && (index.GetReference
                    ().GetOtherTablePageNumber() == otherTable.GetTableDefPageNumber()))
                {
                    return index;
                }
            }
            throw new ArgumentException("Table " + GetName() + " does not have a foreign key reference to "
                 + otherTable.GetName());
        }

        /// <returns>All of the IndexData on this table (unmodifiable List)</returns>
        internal virtual IList<IndexData> GetIndexDatas()
        {
            return Sharpen.Collections.UnmodifiableList(_indexDatas);
        }

        /// <summary>Only called by unit tests</summary>
        internal virtual int GetLogicalIndexCount()
        {
            return _logicalIndexCount;
        }

        private Cursor GetInternalCursor()
        {
            if (_cursor == null)
            {
                _cursor = Cursor.CreateCursor(this);
            }
            return _cursor;
        }

        /// <summary>
        /// After calling this method, getNextRow will return the first row in the
        /// table, see
        /// <see cref="Cursor.Reset()">Cursor.Reset()</see>
        /// .
        /// </summary>
        /// <usage>_general_method_</usage>
        public virtual void Reset()
        {
            GetInternalCursor().Reset();
        }

        /// <summary>
        /// Delete the current row (retrieved by a call to
        /// <see cref="GetNextRow()">GetNextRow()</see>
        /// ).
        /// </summary>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void DeleteCurrentRow()
        {
            GetInternalCursor().DeleteCurrentRow();
        }

        /// <summary>Delete the row on which the given rowState is currently positioned.</summary>
        /// <remarks>
        /// Delete the row on which the given rowState is currently positioned.
        /// <p>
        /// Note, this method is not generally meant to be used directly.  You should
        /// use the
        /// <see cref="DeleteCurrentRow()">DeleteCurrentRow()</see>
        /// method or use the Cursor class, which
        /// allows for more complex table interactions.
        /// </remarks>
        /// <usage>_advanced_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void DeleteRow(Table.RowState rowState, RowId rowId)
        {
            RequireValidRowId(rowId);
            // ensure that the relevant row state is up-to-date
            ByteBuffer rowBuffer = PositionAtRowHeader(rowState, rowId);
            RequireNonDeletedRow(rowState, rowId);
            // delete flag always gets set in the "header" row (even if data is on
            // overflow row)
            int pageNumber = rowState.GetHeaderRowId().GetPageNumber();
            int rowNumber = rowState.GetHeaderRowId().GetRowNumber();
            // use any read rowValues to help update the indexes
            object[] rowValues = (!_indexDatas.IsEmpty() ? rowState.GetRowValues() : null);
            int rowIndex = GetRowStartOffset(rowNumber, GetFormat());
            rowBuffer.PutShort(rowIndex, (short)(rowBuffer.GetShort(rowIndex) | DELETED_ROW_MASK
                 | OVERFLOW_ROW_MASK));
            WriteDataPage(rowBuffer, pageNumber);
            // update the indexes
            foreach (IndexData indexData in _indexDatas)
            {
                indexData.DeleteRow(rowValues, rowId);
            }
            // make sure table def gets updated
            UpdateTableDefinition(-1);
        }

        /// <returns>The next row in this table (Column name -&gt; Column value)</returns>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual IDictionary<string, object> GetNextRow()
        {
            return GetNextRow(null);
        }

        /// <param name="columnNames">Only column names in this collection will be returned</param>
        /// <returns>The next row in this table (Column name -&gt; Column value)</returns>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual IDictionary<string, object> GetNextRow(ICollection<string> columnNames
            )
        {
            return GetInternalCursor().GetNextRow(columnNames);
        }

        /// <summary>Reads a single column from the given row.</summary>
        /// <remarks>
        /// Reads a single column from the given row.
        /// <p>
        /// Note, this method is not generally meant to be used directly.  Instead
        /// use the Cursor class, which allows for more complex table interactions,
        /// e.g.
        /// <see cref="Cursor.GetCurrentRowValue(Column)">Cursor.GetCurrentRowValue(Column)</see>
        /// .
        /// </remarks>
        /// <usage>_advanced_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual object GetRowValue(Table.RowState rowState, RowId rowId, Column column
            )
        {
            if (this != column.GetTable())
            {
                throw new ArgumentException("Given column " + column + " is not from this table");
            }
            RequireValidRowId(rowId);
            // position at correct row
            ByteBuffer rowBuffer = PositionAtRowData(rowState, rowId);
            RequireNonDeletedRow(rowState, rowId);
            return GetRowColumn(GetFormat(), rowBuffer, GetRowNullMask(rowBuffer), column, rowState
                );
        }

        /// <summary>Reads some columns from the given row.</summary>
        /// <remarks>Reads some columns from the given row.</remarks>
        /// <param name="columnNames">Only column names in this collection will be returned</param>
        /// <usage>_advanced_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual IDictionary<string, object> GetRow(Table.RowState rowState, RowId
            rowId, ICollection<string> columnNames)
        {
            RequireValidRowId(rowId);
            // position at correct row
            ByteBuffer rowBuffer = PositionAtRowData(rowState, rowId);
            RequireNonDeletedRow(rowState, rowId);
            return GetRow(GetFormat(), rowState, rowBuffer, GetRowNullMask(rowBuffer), _columns
                , columnNames);
        }

        /// <summary>Reads the row data from the given row buffer.</summary>
        /// <remarks>
        /// Reads the row data from the given row buffer.  Leaves limit unchanged.
        /// Saves parsed row values to the given rowState.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private static IDictionary<string, object> GetRow(JetFormat format, Table.RowState
             rowState, ByteBuffer rowBuffer, NullMask nullMask, ICollection<Column> columns,
            ICollection<string> columnNames)
        {
            IDictionary<string, object> rtn = new LinkedHashMap<string, object>(columns.Count
                );
            foreach (Column column in columns)
            {
                if ((columnNames == null) || (columnNames.Contains(column.GetName())))
                {
                    // Add the value to the row data
                    rtn.Put(column.GetName(), GetRowColumn(format, rowBuffer, nullMask, column, rowState
                        ));
                }
            }
            return rtn;
        }

        /// <summary>Reads the column data from the given row buffer.</summary>
        /// <remarks>
        /// Reads the column data from the given row buffer.  Leaves limit unchanged.
        /// Caches the returned value in the rowState.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private static object GetRowColumn(JetFormat format, ByteBuffer rowBuffer, NullMask
             nullMask, Column column, Table.RowState rowState)
        {
            byte[] columnData = null;
            try
            {
                bool isNull = nullMask.IsNull(column);
                if (column.GetDataType() == DataType.BOOLEAN)
                {
                    // Boolean values are stored in the null mask.  see note about
                    // caching below
                    return rowState.SetRowValue(column.GetColumnIndex(), Sharpen.Extensions.ValueOf(!
                        isNull));
                }
                else
                {
                    if (isNull)
                    {
                        // well, that's easy! (no need to update cache w/ null)
                        return null;
                    }
                }
                // reset position to row start
                rowBuffer.Reset();
                // locate the column data bytes
                int rowStart = rowBuffer.Position();
                int colDataPos = 0;
                int colDataLen = 0;
                if (!column.IsVariableLength())
                {
                    // read fixed length value (non-boolean at this point)
                    int dataStart = rowStart + format.OFFSET_COLUMN_FIXED_DATA_ROW_OFFSET;
                    colDataPos = dataStart + column.GetFixedDataOffset();
                    colDataLen = DataTypeUtil.GetFixedSize(column.GetDataType(), column.GetLength());
                }
                else
                {
                    int varDataStart;
                    int varDataEnd;
                    if (format.SIZE_ROW_VAR_COL_OFFSET == 2)
                    {
                        // read simple var length value
                        int varColumnOffsetPos = (rowBuffer.Limit() - nullMask.ByteSize() - 4) - (column.
                            GetVarLenTableIndex() * 2);
                        varDataStart = rowBuffer.GetShort(varColumnOffsetPos);
                        varDataEnd = rowBuffer.GetShort(varColumnOffsetPos - 2);
                    }
                    else
                    {
                        // read jump-table based var length values
                        short[] varColumnOffsets = ReadJumpTableVarColOffsets(rowState, rowBuffer, rowStart
                            , nullMask);
                        varDataStart = varColumnOffsets[column.GetVarLenTableIndex()];
                        varDataEnd = varColumnOffsets[column.GetVarLenTableIndex() + 1];
                    }
                    colDataPos = rowStart + varDataStart;
                    colDataLen = varDataEnd - varDataStart;
                }
                // grab the column data
                columnData = new byte[colDataLen];
                rowBuffer.Position(colDataPos);
                rowBuffer.Get(columnData);
                // parse the column data.  we cache the row values in order to be able
                // to update the index on row deletion.  note, most of the returned
                // values are immutable, except for binary data (returned as byte[]),
                // but binary data shouldn't be indexed anyway.
                return rowState.SetRowValue(column.GetColumnIndex(), column.Read(columnData));
            }
            catch (Exception e)
            {
                // cache "raw" row value.  see note about caching above
                rowState.SetRowValue(column.GetColumnIndex(), Column.RawDataWrapper(columnData));
                return rowState.HandleRowError(column, columnData, e);
            }
        }

        internal static short[] ReadJumpTableVarColOffsets(Table.RowState rowState, ByteBuffer
             rowBuffer, int rowStart, NullMask nullMask)
        {
            short[] varColOffsets = rowState.GetVarColOffsets();
            if (varColOffsets != null)
            {
                return varColOffsets;
            }
            // calculate offsets using jump-table info
            int nullMaskSize = nullMask.ByteSize();
            int rowEnd = rowStart + rowBuffer.Remaining() - 1;
            int numVarCols = ByteUtil.GetUnsignedByte(rowBuffer, rowEnd - nullMaskSize);
            varColOffsets = new short[numVarCols + 1];
            int rowLen = rowEnd - rowStart + 1;
            int numJumps = (rowLen - 1) / MAX_BYTE;
            int colOffset = rowEnd - nullMaskSize - numJumps - 1;
            // If last jump is a dummy value, ignore it
            if (((colOffset - rowStart - numVarCols) / MAX_BYTE) < numJumps)
            {
                numJumps--;
            }
            int jumpsUsed = 0;
            for (int i = 0; i < numVarCols + 1; i++)
            {
                while ((jumpsUsed < numJumps) && (i == ByteUtil.GetUnsignedByte(rowBuffer, rowEnd
                     - nullMaskSize - jumpsUsed - 1)))
                {
                    jumpsUsed++;
                }
                varColOffsets[i] = (short)(ByteUtil.GetUnsignedByte(rowBuffer, colOffset - i) + (
                    jumpsUsed * MAX_BYTE));
            }
            rowState.SetVarColOffsets(varColOffsets);
            return varColOffsets;
        }

        /// <summary>Reads the null mask from the given row buffer.</summary>
        /// <remarks>Reads the null mask from the given row buffer.  Leaves limit unchanged.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private NullMask GetRowNullMask(ByteBuffer rowBuffer)
        {
            // reset position to row start
            rowBuffer.Reset();
            // Number of columns in this row
            int columnCount = ByteUtil.GetUnsignedVarInt(rowBuffer, GetFormat().SIZE_ROW_COLUMN_COUNT
                );
            // read null mask
            NullMask nullMask = new NullMask(columnCount);
            rowBuffer.Position(rowBuffer.Limit() - nullMask.ByteSize());
            //Null mask at end
            nullMask.Read(rowBuffer);
            return nullMask;
        }

        /// <summary>
        /// Sets a new buffer to the correct row header page using the given rowState
        /// according to the given rowId.
        /// </summary>
        /// <remarks>
        /// Sets a new buffer to the correct row header page using the given rowState
        /// according to the given rowId.  Deleted state is
        /// determined, but overflow row pointers are not followed.
        /// </remarks>
        /// <returns>a ByteBuffer of the relevant page, or null if row was invalid</returns>
        /// <usage>_advanced_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static ByteBuffer PositionAtRowHeader(Table.RowState rowState, RowId rowId
            )
        {
            ByteBuffer rowBuffer = rowState.SetHeaderRow(rowId);
            if (rowState.IsAtHeaderRow())
            {
                // this task has already been accomplished
                return rowBuffer;
            }
            if (!rowState.IsValid())
            {
                // this was an invalid page/row
                rowState.SetStatus(Table.RowStateStatus.AT_HEADER);
                return null;
            }
            // note, we don't use findRowStart here cause we need the unmasked value
            short rowStart = rowBuffer.GetShort(GetRowStartOffset(rowId.GetRowNumber(), rowState
                .GetTable().GetFormat()));
            // check the deleted, overflow flags for the row (the "real" flags are
            // always set on the header row)
            Table.RowStatus rowStatus = Table.RowStatus.NORMAL;
            if (IsDeletedRow(rowStart))
            {
                rowStatus = Table.RowStatus.DELETED;
            }
            else
            {
                if (IsOverflowRow(rowStart))
                {
                    rowStatus = Table.RowStatus.OVERFLOW;
                }
            }
            rowState.SetRowStatus(rowStatus);
            rowState.SetStatus(Table.RowStateStatus.AT_HEADER);
            return rowBuffer;
        }

        /// <summary>
        /// Sets the position and limit in a new buffer using the given rowState
        /// according to the given row number and row end, following overflow row
        /// pointers as necessary.
        /// </summary>
        /// <remarks>
        /// Sets the position and limit in a new buffer using the given rowState
        /// according to the given row number and row end, following overflow row
        /// pointers as necessary.
        /// </remarks>
        /// <returns>
        /// a ByteBuffer narrowed to the actual row data, or null if row was
        /// invalid or deleted
        /// </returns>
        /// <usage>_advanced_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static ByteBuffer PositionAtRowData(Table.RowState rowState, RowId rowId)
        {
            PositionAtRowHeader(rowState, rowId);
            if (!rowState.IsValid() || rowState.IsDeleted())
            {
                // row is invalid or deleted
                rowState.SetStatus(Table.RowStateStatus.AT_FINAL);
                return null;
            }
            ByteBuffer rowBuffer = rowState.GetFinalPage();
            int rowNum = rowState.GetFinalRowId().GetRowNumber();
            JetFormat format = rowState.GetTable().GetFormat();
            if (rowState.IsAtFinalRow())
            {
                // we've already found the final row data
                return PageChannel.NarrowBuffer(rowBuffer, FindRowStart(rowBuffer, rowNum, format
                    ), FindRowEnd(rowBuffer, rowNum, format));
            }
            while (true)
            {
                // note, we don't use findRowStart here cause we need the unmasked value
                short rowStart = rowBuffer.GetShort(GetRowStartOffset(rowNum, format));
                short rowEnd = FindRowEnd(rowBuffer, rowNum, format);
                // note, at this point we know the row is not deleted, so ignore any
                // subsequent deleted flags (as overflow rows are always marked deleted
                // anyway)
                bool overflowRow = IsOverflowRow(rowStart);
                // now, strip flags from rowStart offset
                rowStart = (short)(rowStart & OFFSET_MASK);
                if (overflowRow)
                {
                    if ((rowEnd - rowStart) < 4)
                    {
                        throw new IOException("invalid overflow row info");
                    }
                    // Overflow page.  the "row" data in the current page points to
                    // another page/row
                    int overflowRowNum = ByteUtil.GetUnsignedByte(rowBuffer, rowStart);
                    int overflowPageNum = ByteUtil.Get3ByteInt(rowBuffer, rowStart + 1);
                    rowBuffer = rowState.SetOverflowRow(new RowId(overflowPageNum, overflowRowNum));
                    rowNum = overflowRowNum;
                }
                else
                {
                    rowState.SetStatus(Table.RowStateStatus.AT_FINAL);
                    return PageChannel.NarrowBuffer(rowBuffer, rowStart, rowEnd);
                }
            }
        }

        /// <summary>
        /// Calls <code>reset</code> on this table and returns a modifiable
        /// Iterator which will iterate through all the rows of this table.
        /// </summary>
        /// <remarks>
        /// Calls <code>reset</code> on this table and returns a modifiable
        /// Iterator which will iterate through all the rows of this table.  Use of
        /// the Iterator follows the same restrictions as a call to
        /// <code>getNextRow</code>.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">
        /// if an IOException is thrown by one of the
        /// operations, the actual exception will be contained within
        /// </exception>
        /// <usage>_general_method_</usage>
        public override Sharpen.Iterator<IDictionary<string, object>> Iterator()
        {
            return Iterator(null);
        }

        /// <summary>
        /// Calls <code>reset</code> on this table and returns a modifiable
        /// Iterator which will iterate through all the rows of this table, returning
        /// only the given columns.
        /// </summary>
        /// <remarks>
        /// Calls <code>reset</code> on this table and returns a modifiable
        /// Iterator which will iterate through all the rows of this table, returning
        /// only the given columns.  Use of the Iterator follows the same
        /// restrictions as a call to <code>getNextRow</code>.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">
        /// if an IOException is thrown by one of the
        /// operations, the actual exception will be contained within
        /// </exception>
        /// <usage>_general_method_</usage>
        public virtual Sharpen.Iterator<IDictionary<string, object>> Iterator(ICollection
            <string> columnNames)
        {
            Reset();
            return GetInternalCursor().Iterator(columnNames);
        }

        /// <summary>
        /// Writes a new table defined by the given columns and indexes to the
        /// database.
        /// </summary>
        /// <remarks>
        /// Writes a new table defined by the given columns and indexes to the
        /// database.
        /// </remarks>
        /// <returns>the first page of the new table's definition</returns>
        /// <usage>_advanced_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static int WriteTableDefinition(IList<Column> columns, IList<IndexBuilder>
             indexes, PageChannel pageChannel, JetFormat format, Encoding charset)
        {
            int indexCount = 0;
            int logicalIndexCount = 0;
            if (!indexes.IsEmpty())
            {
                // sort out index numbers.  for now, these values will always match
                // (until we support writing foreign key indexes)
                foreach (IndexBuilder idx in indexes)
                {
                    idx.SetIndexNumber(logicalIndexCount++);
                    idx.SetIndexDataNumber(indexCount++);
                }
            }
            // allocate first table def page
            int tdefPageNumber = pageChannel.AllocateNewPage();
            // first, create the usage map page
            int usageMapPageNumber = CreateUsageMapDefinitionBuffer(indexes, pageChannel, format
                );
            // next, determine how big the table def will be (in case it will be more
            // than one page)
            int idxDataLen = (indexCount * (format.SIZE_INDEX_DEFINITION + format.SIZE_INDEX_COLUMN_BLOCK
                )) + (logicalIndexCount * format.SIZE_INDEX_INFO_BLOCK);
            int totalTableDefSize = format.SIZE_TDEF_HEADER + (format.SIZE_COLUMN_DEF_BLOCK *
                 columns.Count) + idxDataLen + format.SIZE_TDEF_TRAILER;
            // total up the amount of space used by the column and index names (2
            // bytes per char + 2 bytes for the length)
            foreach (Column col in columns)
            {
                int nameByteLen = (col.GetName().Length * JetFormat.TEXT_FIELD_UNIT_SIZE);
                totalTableDefSize += nameByteLen + 2;
            }
            foreach (IndexBuilder idx_1 in indexes)
            {
                int nameByteLen = (idx_1.GetName().Length * JetFormat.TEXT_FIELD_UNIT_SIZE);
                totalTableDefSize += nameByteLen + 2;
            }
            // now, create the table definition
            ByteBuffer buffer = pageChannel.CreateBuffer(Math.Max(totalTableDefSize, format.PAGE_SIZE
                ));
            WriteTableDefinitionHeader(buffer, columns, usageMapPageNumber, totalTableDefSize
                , indexCount, logicalIndexCount, format);
            if (indexCount > 0)
            {
                // index row counts
                IndexData.WriteRowCountDefinitions(buffer, indexCount, format);
            }
            // column definitions
            Column.WriteDefinitions(buffer, columns, format, charset);
            if (indexCount > 0)
            {
                // index and index data definitions
                IndexData.WriteDefinitions(buffer, columns, indexes, tdefPageNumber, pageChannel,
                    format);
                Index.WriteDefinitions(buffer, indexes, charset);
            }
            //End of tabledef
            buffer.Put(unchecked((byte)unchecked((int)(0xff))));
            buffer.Put(unchecked((byte)unchecked((int)(0xff))));
            // write table buffer to database
            if (totalTableDefSize <= format.PAGE_SIZE)
            {
                // easy case, fits on one page
                buffer.PutShort(format.OFFSET_FREE_SPACE, (short)(buffer.Remaining() - 8));
                // overwrite page free space
                // Write the tdef page to disk.
                pageChannel.WritePage(buffer, tdefPageNumber);
            }
            else
            {
                // need to split across multiple pages
                ByteBuffer partialTdef = pageChannel.CreatePageBuffer();
                buffer.Rewind();
                int nextTdefPageNumber = PageChannel.INVALID_PAGE_NUMBER;
                while (buffer.HasRemaining())
                {
                    // reset for next write
                    partialTdef.Clear();
                    if (nextTdefPageNumber == PageChannel.INVALID_PAGE_NUMBER)
                    {
                        // this is the first page.  note, the first page already has the
                        // page header, so no need to write it here
                        nextTdefPageNumber = tdefPageNumber;
                    }
                    else
                    {
                        // write page header
                        WriteTablePageHeader(partialTdef);
                    }
                    // copy the next page of tdef bytes
                    int curTdefPageNumber = nextTdefPageNumber;
                    int writeLen = Math.Min(partialTdef.Remaining(), buffer.Remaining());
                    partialTdef.Put(((byte[])buffer.Array()), buffer.Position(), writeLen);
                    ByteUtil.Forward(buffer, writeLen);
                    if (buffer.HasRemaining())
                    {
                        // need a next page
                        nextTdefPageNumber = pageChannel.AllocateNewPage();
                        partialTdef.PutInt(format.OFFSET_NEXT_TABLE_DEF_PAGE, nextTdefPageNumber);
                    }
                    // update page free space
                    partialTdef.PutShort(format.OFFSET_FREE_SPACE, (short)(partialTdef.Remaining() -
                        8));
                    // overwrite page free space
                    // write partial page to disk
                    pageChannel.WritePage(partialTdef, curTdefPageNumber);
                }
            }
            return tdefPageNumber;
        }

        /// <param name="buffer">Buffer to write to</param>
        /// <param name="columns">List of Columns in the table</param>
        /// <exception cref="System.IO.IOException"></exception>
        private static void WriteTableDefinitionHeader(ByteBuffer buffer, IList<Column> columns
            , int usageMapPageNumber, int totalTableDefSize, int indexCount, int logicalIndexCount
            , JetFormat format)
        {
            //Start writing the tdef
            WriteTablePageHeader(buffer);
            buffer.PutInt(totalTableDefSize);
            //Length of table def
            buffer.PutInt(MAGIC_TABLE_NUMBER);
            // seemingly constant magic value
            buffer.PutInt(0);
            //Number of rows
            buffer.PutInt(0);
            //Last Autonumber
            buffer.Put(unchecked((byte)1));
            // this makes autonumbering work in access
            for (int i = 0; i < 15; i++)
            {
                //Unknown
                buffer.Put(unchecked((byte)0));
            }
            buffer.Put(HealthMarketScience.Jackcess.Table.TYPE_USER);
            //Table type
            buffer.PutShort((short)columns.Count);
            //Max columns a row will have
            buffer.PutShort(Column.CountVariableLength(columns));
            //Number of variable columns in table
            buffer.PutShort((short)columns.Count);
            //Number of columns in table
            buffer.PutInt(logicalIndexCount);
            //Number of logical indexes in table
            buffer.PutInt(indexCount);
            //Number of indexes in table
            buffer.Put(unchecked((byte)0));
            //Usage map row number
            ByteUtil.Put3ByteInt(buffer, usageMapPageNumber);
            //Usage map page number
            buffer.Put(unchecked((byte)1));
            //Free map row number
            ByteUtil.Put3ByteInt(buffer, usageMapPageNumber);
            //Free map page number
            if (Debug.IsDebugEnabled())
            {
                int position = buffer.Position();
                buffer.Rewind();
                Debug.Out("Creating new table def block:\n" + ByteUtil.ToHexString(buffer, format
                    .SIZE_TDEF_HEADER));
                buffer.Position(position);
            }
        }

        /// <summary>Writes the page header for a table definition page</summary>
        /// <param name="buffer">Buffer to write to</param>
        private static void WriteTablePageHeader(ByteBuffer buffer)
        {
            buffer.Put(PageTypes.TABLE_DEF);
            //Page type
            buffer.Put(unchecked((byte)unchecked((int)(0x01))));
            //Unknown
            buffer.Put(unchecked((byte)0));
            //Unknown
            buffer.Put(unchecked((byte)0));
            //Unknown
            buffer.PutInt(0);
        }

        //Next TDEF page pointer
        /// <summary>
        /// Writes the given name into the given buffer in the format as expected by
        /// <see cref="ReadName(Sharpen.ByteBuffer)">ReadName(Sharpen.ByteBuffer)</see>
        /// .
        /// </summary>
        internal static void WriteName(ByteBuffer buffer, string name, Encoding charset)
        {
            ByteBuffer encName = Column.EncodeUncompressedText(name, charset);
            buffer.PutShort((short)encName.Remaining());
            buffer.Put(encName);
        }

        /// <summary>Create the usage map definition page buffer.</summary>
        /// <remarks>
        /// Create the usage map definition page buffer.  The "used pages" map is in
        /// row 0, the "pages with free space" map is in row 1.  Index usage maps are
        /// in subsequent rows.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private static int CreateUsageMapDefinitionBuffer(IList<IndexBuilder> indexes, PageChannel
             pageChannel, JetFormat format)
        {
            // 2 table usage maps plus 1 for each index
            int umapNum = 2 + indexes.Count;
            int usageMapRowLength = format.OFFSET_USAGE_MAP_START + format.USAGE_MAP_TABLE_BYTE_LENGTH;
            int freeSpace = format.DATA_PAGE_INITIAL_FREE_SPACE - (umapNum * GetRowSpaceUsage
                (usageMapRowLength, format));
            // for now, don't handle writing that many indexes
            if (freeSpace < 0)
            {
                throw new IOException("FIXME attempting to write too many indexes");
            }
            int umapPageNumber = pageChannel.AllocateNewPage();
            ByteBuffer rtn = pageChannel.CreatePageBuffer();
            rtn.Put(PageTypes.DATA);
            rtn.Put(unchecked((byte)unchecked((int)(0x1))));
            //Unknown
            rtn.PutShort((short)freeSpace);
            //Free space in page
            rtn.PutInt(0);
            //Table definition
            rtn.PutInt(0);
            //Unknown
            rtn.PutShort((short)2);
            //Number of records on this page
            // write two rows of usage map definitions for the table
            int rowStart = FindRowEnd(rtn, 0, format) - usageMapRowLength;
            for (int i = 0; i < 2; ++i)
            {
                rtn.PutShort(GetRowStartOffset(i, format), (short)rowStart);
                if (i == 0)
                {
                    // initial "usage pages" map definition
                    rtn.Put(rowStart, UsageMap.MAP_TYPE_REFERENCE);
                }
                else
                {
                    // initial "pages with free space" map definition
                    rtn.Put(rowStart, UsageMap.MAP_TYPE_INLINE);
                }
                rowStart -= usageMapRowLength;
            }
            if (!indexes.IsEmpty())
            {
                for (int i_1 = 0; i_1 < indexes.Count; ++i_1)
                {
                    IndexBuilder idx = indexes[i_1];
                    // allocate root page for the index
                    int rootPageNumber = pageChannel.AllocateNewPage();
                    int umapRowNum = i_1 + 2;
                    // stash info for later use
                    idx.SetRootPageNumber(rootPageNumber);
                    idx.SetUmapRowNumber(unchecked((byte)umapRowNum));
                    idx.SetUmapPageNumber(umapPageNumber);
                    // index map definition, including initial root page
                    rtn.PutShort(GetRowStartOffset(umapRowNum, format), (short)rowStart);
                    rtn.Put(rowStart, UsageMap.MAP_TYPE_INLINE);
                    rtn.PutInt(rowStart + 1, rootPageNumber);
                    rtn.Put(rowStart + 5, unchecked((byte)1));
                    rowStart -= usageMapRowLength;
                }
            }
            pageChannel.WritePage(rtn, umapPageNumber);
            return umapPageNumber;
        }

        /// <summary>Read the table definition</summary>
        /// <exception cref="System.IO.IOException"></exception>
        private void ReadTableDefinition(ByteBuffer tableBuffer)
        {
            if (Debug.IsDebugEnabled())
            {
                tableBuffer.Rewind();
                Debug.Out("Table def block:\n" + ByteUtil.ToHexString(tableBuffer, GetFormat().SIZE_TDEF_HEADER
                    ));
            }
            _rowCount = tableBuffer.GetInt(GetFormat().OFFSET_NUM_ROWS);
            _lastLongAutoNumber = tableBuffer.GetInt(GetFormat().OFFSET_NEXT_AUTO_NUMBER);
            _tableType = tableBuffer.Get(GetFormat().OFFSET_TABLE_TYPE);
            _maxColumnCount = tableBuffer.GetShort(GetFormat().OFFSET_MAX_COLS);
            _maxVarColumnCount = tableBuffer.GetShort(GetFormat().OFFSET_NUM_VAR_COLS);
            short columnCount = tableBuffer.GetShort(GetFormat().OFFSET_NUM_COLS);
            _logicalIndexCount = tableBuffer.GetInt(GetFormat().OFFSET_NUM_INDEX_SLOTS);
            _indexCount = tableBuffer.GetInt(GetFormat().OFFSET_NUM_INDEXES);
            int rowNum = ByteUtil.GetUnsignedByte(tableBuffer, GetFormat().OFFSET_OWNED_PAGES
                );
            int pageNum = ByteUtil.Get3ByteInt(tableBuffer, GetFormat().OFFSET_OWNED_PAGES +
                1);
            _ownedPages = UsageMap.Read(GetDatabase(), pageNum, rowNum, false);
            rowNum = ByteUtil.GetUnsignedByte(tableBuffer, GetFormat().OFFSET_FREE_SPACE_PAGES
                );
            pageNum = ByteUtil.Get3ByteInt(tableBuffer, GetFormat().OFFSET_FREE_SPACE_PAGES +
                 1);
            _freeSpacePages = UsageMap.Read(GetDatabase(), pageNum, rowNum, false);
            for (int i = 0; i < _indexCount; i++)
            {
                _indexDatas.AddItem(IndexData.Create(this, tableBuffer, i, GetFormat()));
            }
            int colOffset = GetFormat().OFFSET_INDEX_DEF_BLOCK + _indexCount * GetFormat().SIZE_INDEX_DEFINITION;
            int dispIndex = 0;
            for (int i_1 = 0; i_1 < columnCount; i_1++)
            {
                Column column = new Column(this, tableBuffer, colOffset + (i_1 * GetFormat().SIZE_COLUMN_HEADER
                    ), dispIndex++);
                _columns.AddItem(column);
                if (column.IsVariableLength())
                {
                    // also shove it in the variable columns list, which is ordered
                    // differently from the _columns list
                    _varColumns.AddItem(column);
                }
            }
            tableBuffer.Position(colOffset + (columnCount * GetFormat().SIZE_COLUMN_HEADER));
            for (int i_2 = 0; i_2 < columnCount; i_2++)
            {
                Column column = _columns[i_2];
                column.SetName(ReadName(tableBuffer));
            }
            _columns.Sort();
            // setup the data index for the columns
            int colIdx = 0;
            foreach (Column col in _columns)
            {
                col.SetColumnIndex(colIdx++);
            }
            // sort variable length columns based on their index into the variable
            // length offset table, because we will write the columns in this order
            _varColumns.Sort(VAR_LEN_COLUMN_COMPARATOR);
            // read index column information
            for (int i_3 = 0; i_3 < _indexCount; i_3++)
            {
                _indexDatas[i_3].Read(tableBuffer, _columns);
            }
            // read logical index info (may be more logical indexes than index datas)
            for (int i_4 = 0; i_4 < _logicalIndexCount; i_4++)
            {
                _indexes.AddItem(new Index(tableBuffer, _indexDatas, GetFormat()));
            }
            // read logical index names
            for (int i_5 = 0; i_5 < _logicalIndexCount; i_5++)
            {
                _indexes[i_5].SetName(ReadName(tableBuffer));
            }
            _indexes.Sort();
            // re-sort columns if necessary
            if (GetDatabase().GetColumnOrder() != Table.ColumnOrder.DATA)
            {
                _columns.Sort(DISPLAY_ORDER_COMPARATOR);
            }
        }

        /// <summary>
        /// Writes the given page data to the given page number, clears any other
        /// relevant buffers.
        /// </summary>
        /// <remarks>
        /// Writes the given page data to the given page number, clears any other
        /// relevant buffers.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private void WriteDataPage(ByteBuffer pageBuffer, int pageNumber)
        {
            // write the page data
            GetPageChannel().WritePage(pageBuffer, pageNumber);
            // possibly invalidate the add row buffer if a different data buffer is
            // being written (e.g. this happens during deleteRow)
            _addRowBufferH.PossiblyInvalidate(pageNumber, pageBuffer);
            // update modification count so any active RowStates can keep themselves
            // up-to-date
            ++_modCount;
        }

        /// <summary>Returns a name read from the buffer at the current position.</summary>
        /// <remarks>
        /// Returns a name read from the buffer at the current position. The
        /// expected name format is the name length followed by the name
        /// encoded using the
        /// <see cref="JetFormat.CHARSET">JetFormat.CHARSET</see>
        /// </remarks>
        private string ReadName(ByteBuffer buffer)
        {
            int nameLength = ReadNameLength(buffer);
            byte[] nameBytes = new byte[nameLength];
            buffer.Get(nameBytes);
            return Column.DecodeUncompressedText(nameBytes, GetDatabase().GetCharset());
        }

        /// <summary>Returns a name length read from the buffer at the current position.</summary>
        /// <remarks>Returns a name length read from the buffer at the current position.</remarks>
        private int ReadNameLength(ByteBuffer buffer)
        {
            return ByteUtil.GetUnsignedVarInt(buffer, GetFormat().SIZE_NAME_LENGTH);
        }

        /// <summary>
        /// Converts a map of columnName -&gt; columnValue to an array of row values
        /// appropriate for a call to
        /// <see cref="AddRow(object[])">AddRow(object[])</see>
        /// .
        /// </summary>
        /// <usage>_general_method_</usage>
        public virtual object[] AsRow(IDictionary<string, object> rowMap)
        {
            return AsRow(rowMap, null);
        }

        /// <summary>
        /// Converts a map of columnName -&gt; columnValue to an array of row values
        /// appropriate for a call to
        /// <see cref="UpdateCurrentRow(object[])">UpdateCurrentRow(object[])</see>
        /// .
        /// </summary>
        /// <usage>_general_method_</usage>
        public virtual object[] AsUpdateRow(IDictionary<string, object> rowMap)
        {
            return AsRow(rowMap, Column.KEEP_VALUE);
        }

        /// <summary>Converts a map of columnName -&gt; columnValue to an array of row values.
        /// 	</summary>
        /// <remarks>Converts a map of columnName -&gt; columnValue to an array of row values.
        /// 	</remarks>
        private object[] AsRow(IDictionary<string, object> rowMap, object defaultValue)
        {
            object[] row = new object[_columns.Count];
            if (defaultValue != null)
            {
                Arrays.Fill(row, defaultValue);
            }
            if (rowMap == null)
            {
                return row;
            }
            foreach (Column col in _columns)
            {
                if (rowMap.ContainsKey(col.GetName()))
                {
                    row[col.GetColumnIndex()] = rowMap.Get(col.GetName());
                }
            }
            return row;
        }

        /// <summary>
        /// Add a single row to this table and write it to disk
        /// <p>
        /// Note, if this table has an auto-number column, the value written will be
        /// put back into the given row array.
        /// </summary>
        /// <remarks>
        /// Add a single row to this table and write it to disk
        /// <p>
        /// Note, if this table has an auto-number column, the value written will be
        /// put back into the given row array.
        /// </remarks>
        /// <param name="row">
        /// row values for a single row.  the row will be modified if
        /// this table contains an auto-number column, otherwise it
        /// will not be modified.
        /// </param>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void AddRow(params object[] row)
        {
            AddRows(Sharpen.Collections.SingletonList(row), _singleRowBufferH);
        }

        /// <summary>
        /// Add multiple rows to this table, only writing to disk after all
        /// rows have been written, and every time a data page is filled.
        /// </summary>
        /// <remarks>
        /// Add multiple rows to this table, only writing to disk after all
        /// rows have been written, and every time a data page is filled.  This
        /// is much more efficient than calling <code>addRow</code> multiple times.
        /// <p>
        /// Note, if this table has an auto-number column, the values written will be
        /// put back into the given row arrays.
        /// </remarks>
        /// <param name="rows">
        /// List of Object[] row values.  the rows will be modified if
        /// this table contains an auto-number column, otherwise they
        /// will not be modified.
        /// </param>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void AddRows(IList<object[]> rows)
        {
            AddRows(rows, _multiRowBufferH);
        }

        /// <summary>
        /// Add multiple rows to this table, only writing to disk after all
        /// rows have been written, and every time a data page is filled.
        /// </summary>
        /// <remarks>
        /// Add multiple rows to this table, only writing to disk after all
        /// rows have been written, and every time a data page is filled.
        /// </remarks>
        /// <param name="inRows">List of Object[] row values</param>
        /// <param name="writeRowBufferH">
        /// TempBufferHolder used to generate buffers for
        /// writing the row data
        /// </param>
        /// <exception cref="System.IO.IOException"></exception>
        private void AddRows(IList<object[]> inRows, TempBufferHolder writeRowBufferH)
        {
            if (inRows.IsEmpty())
            {
                return;
            }
            // copy the input rows to a modifiable list so we can update the elements
            IList<object[]> rows = new AList<object[]>(inRows);
            ByteBuffer[] rowData = new ByteBuffer[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                // we need to make sure the row is the right length (fill with null).
                // note, if the row is copied the caller will not be able to access any
                // generated auto-number value, but if they need that info they should
                // use a row array of the right size!
                object[] row = rows[i];
                if (row.Length < _columns.Count)
                {
                    row = DupeRow(row, _columns.Count);
                    // we copied the row, so put the copy back into the rows list
                    rows.Set(i, row);
                }
                // write the row of data to a temporary buffer
                rowData[i] = CreateRow(row, GetFormat().MAX_ROW_SIZE, writeRowBufferH.GetPageBuffer
                    (GetPageChannel()), false, 0);
                if (rowData[i].Limit() > GetFormat().MAX_ROW_SIZE)
                {
                    throw new IOException("Row size " + rowData[i].Limit() + " is too large");
                }
            }
            ByteBuffer dataPage = null;
            int pageNumber = PageChannel.INVALID_PAGE_NUMBER;
            for (int i_1 = 0; i_1 < rowData.Length; i_1++)
            {
                int rowSize = rowData[i_1].Remaining();
                // get page with space
                dataPage = FindFreeRowSpace(rowSize, dataPage, pageNumber);
                pageNumber = _addRowBufferH.GetPageNumber();
                // write out the row data
                int rowNum = AddDataPageRow(dataPage, rowSize, GetFormat(), 0);
                dataPage.Put(rowData[i_1]);
                // update the indexes
                RowId rowId = new RowId(pageNumber, rowNum);
                foreach (IndexData indexData in _indexDatas)
                {
                    indexData.AddRow(rows[i_1], rowId);
                }
            }
            WriteDataPage(dataPage, pageNumber);
            // Update tdef page
            UpdateTableDefinition(rows.Count);
        }

        /// <summary>Updates the current row to the new values.</summary>
        /// <remarks>
        /// Updates the current row to the new values.
        /// <p>
        /// Note, if this table has an auto-number column(s), the existing value(s)
        /// will be maintained, unchanged.
        /// </remarks>
        /// <param name="row">new row values for the current row.</param>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void UpdateCurrentRow(params object[] row)
        {
            GetInternalCursor().UpdateCurrentRow(row);
        }

        /// <summary>Update the row on which the given rowState is currently positioned.</summary>
        /// <remarks>
        /// Update the row on which the given rowState is currently positioned.
        /// <p>
        /// Note, this method is not generally meant to be used directly.  You should
        /// use the
        /// <see cref="UpdateCurrentRow(object[])">UpdateCurrentRow(object[])</see>
        /// method or use the Cursor class, which
        /// allows for more complex table interactions, e.g.
        /// <see cref="Cursor.SetCurrentRowValue(Column, object)">Cursor.SetCurrentRowValue(Column, object)
        /// 	</see>
        /// and
        /// <see cref="Cursor.UpdateCurrentRow(object[])">Cursor.UpdateCurrentRow(object[])</see>
        /// .
        /// </remarks>
        /// <usage>_advanced_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void UpdateRow(Table.RowState rowState, RowId rowId, params object
            [] row)
        {
            RequireValidRowId(rowId);
            // ensure that the relevant row state is up-to-date
            ByteBuffer rowBuffer = PositionAtRowData(rowState, rowId);
            int oldRowSize = rowBuffer.Remaining();
            RequireNonDeletedRow(rowState, rowId);
            // we need to make sure the row is the right length (fill with null).
            if (row.Length < _columns.Count)
            {
                row = DupeRow(row, _columns.Count);
            }
            // fill in any auto-numbers (we don't allow autonumber values to be
            // modified) or "keep value" fields
            NullMask nullMask = GetRowNullMask(rowBuffer);
            foreach (Column column in _columns)
            {
                if (column.IsAutoNumber() || (row[column.GetColumnIndex()] == Column.KEEP_VALUE))
                {
                    row[column.GetColumnIndex()] = GetRowColumn(GetFormat(), rowBuffer, nullMask, column
                        , rowState);
                }
            }
            // generate new row bytes
            ByteBuffer newRowData = CreateRow(row, GetFormat().MAX_ROW_SIZE, _singleRowBufferH
                .GetPageBuffer(GetPageChannel()), true, oldRowSize);
            if (newRowData.Limit() > GetFormat().MAX_ROW_SIZE)
            {
                throw new IOException("Row size " + newRowData.Limit() + " is too large");
            }
            object[] oldRowValues = (!_indexDatas.IsEmpty() ? rowState.GetRowValues() : null);
            // delete old values from indexes
            foreach (IndexData indexData in _indexDatas)
            {
                indexData.DeleteRow(oldRowValues, rowId);
            }
            // see if we can squeeze the new row data into the existing row
            rowBuffer.Reset();
            int rowSize = newRowData.Remaining();
            ByteBuffer dataPage = null;
            int pageNumber = PageChannel.INVALID_PAGE_NUMBER;
            if (oldRowSize >= rowSize)
            {
                // awesome, slap it in!
                rowBuffer.Put(newRowData);
                // grab the page we just updated
                dataPage = rowState.GetFinalPage();
                pageNumber = rowState.GetFinalRowId().GetPageNumber();
            }
            else
            {
                // bummer, need to find a new page for the data
                dataPage = FindFreeRowSpace(rowSize, null, PageChannel.INVALID_PAGE_NUMBER);
                pageNumber = _addRowBufferH.GetPageNumber();
                RowId headerRowId = rowState.GetHeaderRowId();
                ByteBuffer headerPage = rowState.GetHeaderPage();
                if (pageNumber == headerRowId.GetPageNumber())
                {
                    // new row is on the same page as header row, share page
                    dataPage = headerPage;
                }
                // write out the new row data (set the deleted flag on the new data row
                // so that it is ignored during normal table traversal)
                int rowNum = AddDataPageRow(dataPage, rowSize, GetFormat(), DELETED_ROW_MASK);
                dataPage.Put(newRowData);
                // write the overflow info into the header row and clear out the
                // remaining header data
                rowBuffer = PageChannel.NarrowBuffer(headerPage, FindRowStart(headerPage, headerRowId
                    .GetRowNumber(), GetFormat()), FindRowEnd(headerPage, headerRowId.GetRowNumber()
                    , GetFormat()));
                rowBuffer.Put(unchecked((byte)rowNum));
                ByteUtil.Put3ByteInt(rowBuffer, pageNumber);
                ByteUtil.ClearRemaining(rowBuffer);
                // set the overflow flag on the header row
                int headerRowIndex = GetRowStartOffset(headerRowId.GetRowNumber(), GetFormat());
                headerPage.PutShort(headerRowIndex, (short)(headerPage.GetShort(headerRowIndex) |
                     OVERFLOW_ROW_MASK));
                if (pageNumber != headerRowId.GetPageNumber())
                {
                    WriteDataPage(headerPage, headerRowId.GetPageNumber());
                }
            }
            // update the indexes
            foreach (IndexData indexData_1 in _indexDatas)
            {
                indexData_1.AddRow(row, rowId);
            }
            WriteDataPage(dataPage, pageNumber);
            UpdateTableDefinition(0);
        }

        /// <exception cref="System.IO.IOException"></exception>
        private ByteBuffer FindFreeRowSpace(int rowSize, ByteBuffer dataPage, int pageNumber
            )
        {
            if (dataPage == null)
            {
                // find last data page (Not bothering to check other pages for free
                // space.)
                UsageMap.PageCursor revPageCursor = _ownedPages.Cursor();
                revPageCursor.AfterLast();
                while (true)
                {
                    int tmpPageNumber = revPageCursor.GetPreviousPage();
                    if (tmpPageNumber < 0)
                    {
                        break;
                    }
                    dataPage = _addRowBufferH.SetPage(GetPageChannel(), tmpPageNumber);
                    if (dataPage.Get() == PageTypes.DATA)
                    {
                        // found last data page, only use if actually listed in free space
                        // pages
                        if (_freeSpacePages.ContainsPageNumber(tmpPageNumber))
                        {
                            pageNumber = tmpPageNumber;
                        }
                        break;
                    }
                }
                if (pageNumber == PageChannel.INVALID_PAGE_NUMBER)
                {
                    // No data pages exist (with free space).  Create a new one.
                    return NewDataPage();
                }
            }
            if (!RowFitsOnDataPage(rowSize, dataPage, GetFormat()))
            {
                // Last data page is full.  Create a new one.
                WriteDataPage(dataPage, pageNumber);
                _freeSpacePages.RemovePageNumber(pageNumber);
                dataPage = NewDataPage();
            }
            return dataPage;
        }

        /// <summary>Updates the table definition after rows are modified.</summary>
        /// <remarks>Updates the table definition after rows are modified.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private void UpdateTableDefinition(int rowCountInc)
        {
            // load table definition
            ByteBuffer tdefPage = _tableDefBufferH.SetPage(GetPageChannel(), _tableDefPageNumber
                );
            // make sure rowcount and autonumber are up-to-date
            _rowCount += rowCountInc;
            tdefPage.PutInt(GetFormat().OFFSET_NUM_ROWS, _rowCount);
            tdefPage.PutInt(GetFormat().OFFSET_NEXT_AUTO_NUMBER, _lastLongAutoNumber);
            // write any index changes
            foreach (IndexData indexData in _indexDatas)
            {
                // write the unique entry count for the index to the table definition
                // page
                tdefPage.PutInt(indexData.GetUniqueEntryCountOffset(), indexData.GetUniqueEntryCount
                    ());
                // write the entry page for the index
                indexData.Update();
            }
            // write modified table definition
            GetPageChannel().WritePage(tdefPage, _tableDefPageNumber);
        }

        /// <summary>Create a new data page</summary>
        /// <returns>Page number of the new page</returns>
        /// <exception cref="System.IO.IOException"></exception>
        private ByteBuffer NewDataPage()
        {
            if (Debug.IsDebugEnabled())
            {
                Debug.Out("Creating new data page");
            }
            ByteBuffer dataPage = _addRowBufferH.SetNewPage(GetPageChannel());
            dataPage.Put(PageTypes.DATA);
            //Page type
            dataPage.Put(unchecked((byte)1));
            //Unknown
            dataPage.PutShort((short)GetFormat().DATA_PAGE_INITIAL_FREE_SPACE);
            //Free space in this page
            dataPage.PutInt(_tableDefPageNumber);
            //Page pointer to table definition
            dataPage.PutInt(0);
            //Unknown
            dataPage.PutShort((short)0);
            //Number of rows on this page
            int pageNumber = _addRowBufferH.GetPageNumber();
            GetPageChannel().WritePage(dataPage, pageNumber);
            _ownedPages.AddPageNumber(pageNumber);
            _freeSpacePages.AddPageNumber(pageNumber);
            return dataPage;
        }

        /// <summary>Serialize a row of Objects into a byte buffer.</summary>
        /// <remarks>
        /// Serialize a row of Objects into a byte buffer.
        /// <p>
        /// Note, if this table has an auto-number column, the value written will be
        /// put back into the given row array.
        /// </remarks>
        /// <param name="rowArray">row data, expected to be correct length for this table</param>
        /// <param name="maxRowSize">max size the data can be for this row</param>
        /// <param name="buffer">buffer to which to write the row data</param>
        /// <returns>the given buffer, filled with the row data</returns>
        /// <exception cref="System.IO.IOException"></exception>
        internal virtual ByteBuffer CreateRow(object[] rowArray, int maxRowSize, ByteBuffer
             buffer, bool isUpdate, int minRowSize)
        {
            buffer.PutShort(_maxColumnCount);
            NullMask nullMask = new NullMask(_maxColumnCount);
            //Fixed length column data comes first
            int fixedDataStart = buffer.Position();
            int fixedDataEnd = fixedDataStart;
            foreach (Column col in _columns)
            {
                if (col.IsVariableLength())
                {
                    continue;
                }
                object rowValue = rowArray[col.GetColumnIndex()];
                if (col.GetDataType() == DataType.BOOLEAN)
                {
                    if (Column.ToBooleanValue(rowValue))
                    {
                        //Booleans are stored in the null mask
                        nullMask.MarkNotNull(col);
                    }
                    rowValue = null;
                }
                else
                {
                    if (col.IsAutoNumber() && !isUpdate)
                    {
                        // ignore given row value, use next autonumber
                        rowValue = col.GetAutoNumberGenerator().GetNext();
                        // we need to stick this back in the row so that the indexes get
                        // updated correctly (and caller can get the generated value)
                        rowArray[col.GetColumnIndex()] = rowValue;
                    }
                }
                if (rowValue != null)
                {
                    // we have a value to write
                    nullMask.MarkNotNull(col);
                    // remainingRowLength is ignored when writing fixed length data
                    buffer.Position(fixedDataStart + col.GetFixedDataOffset());
                    buffer.Put(col.Write(rowValue, 0));
                }
                // always insert space for the entire fixed data column length
                // (including null values), access expects the row to always be at least
                // big enough to hold all fixed values
                buffer.Position(fixedDataStart + col.GetFixedDataOffset() + col.GetLength());
                // keep track of the end of fixed data
                if (buffer.Position() > fixedDataEnd)
                {
                    fixedDataEnd = buffer.Position();
                }
            }
            // reposition at end of fixed data
            buffer.Position(fixedDataEnd);
            // only need this info if this table contains any var length data
            if (_maxVarColumnCount > 0)
            {
                // figure out how much space remains for var length data.  first,
                // account for already written space
                maxRowSize -= buffer.Position();
                // now, account for trailer space
                int trailerSize = (nullMask.ByteSize() + 4 + (_maxVarColumnCount * 2));
                maxRowSize -= trailerSize;
                // for each non-null long value column we need to reserve a small
                // amount of space so that we don't end up running out of row space
                // later by being too greedy
                foreach (Column varCol in _varColumns)
                {
                    if ((DataTypeProperties.Get(varCol.GetDataType()).longValue) && (rowArray[varCol.GetColumnIndex
                        ()] != null))
                    {
                        maxRowSize -= GetFormat().SIZE_LONG_VALUE_DEF;
                    }
                }
                //Now write out variable length column data
                short[] varColumnOffsets = new short[_maxVarColumnCount];
                int varColumnOffsetsIndex = 0;
                foreach (Column varCol_1 in _varColumns)
                {
                    short offset = (short)buffer.Position();
                    object rowValue = rowArray[varCol_1.GetColumnIndex()];
                    if (rowValue != null)
                    {
                        // we have a value
                        nullMask.MarkNotNull(varCol_1);
                        ByteBuffer varDataBuf = varCol_1.Write(rowValue, maxRowSize);
                        maxRowSize -= varDataBuf.Remaining();
                        if (DataTypeProperties.Get(varCol_1.GetDataType()).longValue)
                        {
                            // we already accounted for some amount of the long value data
                            // above.  add that space back so we don't double count
                            maxRowSize += GetFormat().SIZE_LONG_VALUE_DEF;
                        }
                        buffer.Put(varDataBuf);
                    }
                    // we do a loop here so that we fill in offsets for deleted columns
                    while (varColumnOffsetsIndex <= varCol_1.GetVarLenTableIndex())
                    {
                        varColumnOffsets[varColumnOffsetsIndex++] = offset;
                    }
                }
                // fill in offsets for any remaining deleted columns
                while (varColumnOffsetsIndex < varColumnOffsets.Length)
                {
                    varColumnOffsets[varColumnOffsetsIndex++] = (short)buffer.Position();
                }
                // record where we stopped writing
                int eod = buffer.Position();
                // insert padding if necessary
                PadRowBuffer(buffer, minRowSize, trailerSize);
                buffer.PutShort((short)eod);
                //EOD marker
                //Now write out variable length offsets
                //Offsets are stored in reverse order
                for (int i = _maxVarColumnCount - 1; i >= 0; i--)
                {
                    buffer.PutShort(varColumnOffsets[i]);
                }
                buffer.PutShort(_maxVarColumnCount);
            }
            else
            {
                //Number of var length columns
                // insert padding for row w/ no var cols
                PadRowBuffer(buffer, minRowSize, nullMask.ByteSize());
            }
            nullMask.Write(buffer);
            //Null mask
            buffer.Flip();
            if (Debug.IsDebugEnabled())
            {
                Debug.Out("Creating new data block:\n" + ByteUtil.ToHexString(buffer, buffer.Limit
                    ()));
            }
            return buffer;
        }

        private static void PadRowBuffer(ByteBuffer buffer, int minRowSize, int trailerSize
            )
        {
            int pos = buffer.Position();
            if ((pos + trailerSize) < minRowSize)
            {
                // pad the row to get to the min byte size
                int padSize = minRowSize - (pos + trailerSize);
                ByteUtil.ClearRange(buffer, pos, pos + padSize);
                ByteUtil.Forward(buffer, padSize);
            }
        }

        /// <usage>_general_method_</usage>
        public virtual int GetRowCount()
        {
            return _rowCount;
        }

        internal virtual int GetNextLongAutoNumber()
        {
            // note, the saved value is the last one handed out, so pre-increment
            return ++_lastLongAutoNumber;
        }

        internal virtual int GetLastLongAutoNumber()
        {
            // gets the last used auto number (does not modify)
            return _lastLongAutoNumber;
        }

        public override string ToString()
        {
            StringBuilder rtn = new StringBuilder();
            rtn.Append("Type: " + _tableType + ((_tableType == TYPE_USER) ? " (USER)" : " (SYSTEM)"
                ));
            rtn.Append("\nName: " + _name);
            rtn.Append("\nRow count: " + _rowCount);
            rtn.Append("\nColumn count: " + _columns.Count);
            rtn.Append("\nIndex (data) count: " + _indexCount);
            rtn.Append("\nLogical Index count: " + _logicalIndexCount);
            rtn.Append("\nColumns:\n");
            foreach (Column col in _columns)
            {
                rtn.Append(col);
            }
            rtn.Append("\nIndexes:\n");
            foreach (Index index in _indexes)
            {
                rtn.Append(index);
            }
            rtn.Append("\nOwned pages: " + _ownedPages + "\n");
            return rtn.ToString();
        }

        /// <returns>
        /// A simple String representation of the entire table in
        /// tab-delimited format
        /// </returns>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual string Display()
        {
            return Display(long.MaxValue);
        }

        /// <param name="limit">Maximum number of rows to display</param>
        /// <returns>
        /// A simple String representation of the entire table in
        /// tab-delimited format
        /// </returns>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual string Display(long limit)
        {
            Reset();
            StringBuilder rtn = new StringBuilder();
            for (Sharpen.Iterator<Column> iter = _columns.Iterator(); iter.HasNext();)
            {
                Column col = iter.Next();
                rtn.Append(col.GetName());
                if (iter.HasNext())
                {
                    rtn.Append("\t");
                }
            }
            rtn.Append("\n");
            IDictionary<string, object> row;
            int rowCount = 0;
            while ((rowCount++ < limit) && (row = GetNextRow()) != null)
            {
                for (Sharpen.Iterator<object> iter_1 = row.Values.Iterator(); iter_1.HasNext();)
                {
                    object obj = iter_1.Next();
                    if (obj is byte[])
                    {
                        byte[] b = (byte[])obj;
                        rtn.Append(ByteUtil.ToHexString(b));
                    }
                    else
                    {
                        //This block can be used to easily dump a binary column to a file
                        rtn.Append(obj.ToString());
                    }
                    if (iter_1.HasNext())
                    {
                        rtn.Append("\t");
                    }
                }
                rtn.Append("\n");
            }
            return rtn.ToString();
        }

        /// <summary>
        /// Updates free space and row info for a new row of the given size in the
        /// given data page.
        /// </summary>
        /// <remarks>
        /// Updates free space and row info for a new row of the given size in the
        /// given data page.  Positions the page for writing the row data.
        /// </remarks>
        /// <returns>the row number of the new row</returns>
        /// <usage>_advanced_method_</usage>
        public static int AddDataPageRow(ByteBuffer dataPage, int rowSize, JetFormat format
            , int rowFlags)
        {
            int rowSpaceUsage = GetRowSpaceUsage(rowSize, format);
            // Decrease free space record.
            short freeSpaceInPage = dataPage.GetShort(format.OFFSET_FREE_SPACE);
            dataPage.PutShort(format.OFFSET_FREE_SPACE, (short)(freeSpaceInPage - rowSpaceUsage
                ));
            // Increment row count record.
            short rowCount = dataPage.GetShort(format.OFFSET_NUM_ROWS_ON_DATA_PAGE);
            dataPage.PutShort(format.OFFSET_NUM_ROWS_ON_DATA_PAGE, (short)(rowCount + 1));
            // determine row position
            short rowLocation = FindRowEnd(dataPage, rowCount, format);
            rowLocation -= (short)rowSize;
            // write row position
            dataPage.PutShort(GetRowStartOffset(rowCount, format), (short)((short)rowLocation | (short)rowFlags
                ));
            // set position for row data
            dataPage.Position(rowLocation);
            return rowCount;
        }

        /// <summary>Returns the row count for the current page.</summary>
        /// <remarks>
        /// Returns the row count for the current page.  If the page is invalid
        /// (
        /// <code>null</code>
        /// ) or the page is not a DATA page, 0 is returned.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private static int GetRowsOnDataPage(ByteBuffer rowBuffer, JetFormat format)
        {
            int rowsOnPage = 0;
            if ((rowBuffer != null) && (rowBuffer.Get(0) == PageTypes.DATA))
            {
                rowsOnPage = rowBuffer.GetShort(format.OFFSET_NUM_ROWS_ON_DATA_PAGE);
            }
            return rowsOnPage;
        }

        /// <exception cref="System.InvalidOperationException">if the given rowId is invalid</exception>
        private static void RequireValidRowId(RowId rowId)
        {
            if (!rowId.IsValid())
            {
                throw new ArgumentException("Given rowId is invalid: " + rowId);
            }
        }

        /// <exception cref="System.InvalidOperationException">if the given row is invalid or deleted
        /// 	</exception>
        private static void RequireNonDeletedRow(Table.RowState rowState, RowId rowId)
        {
            if (!rowState.IsValid())
            {
                throw new ArgumentException("Given rowId is invalid for this table: " + rowId);
            }
            if (rowState.IsDeleted())
            {
                throw new InvalidOperationException("Row is deleted: " + rowId);
            }
        }

        /// <usage>_advanced_method_</usage>
        public static bool IsDeletedRow(short rowStart)
        {
            return ((rowStart & DELETED_ROW_MASK) != 0);
        }

        /// <usage>_advanced_method_</usage>
        public static bool IsOverflowRow(short rowStart)
        {
            return ((rowStart & OVERFLOW_ROW_MASK) != 0);
        }

        /// <usage>_advanced_method_</usage>
        public static short CleanRowStart(short rowStart)
        {
            return (short)(rowStart & OFFSET_MASK);
        }

        /// <usage>_advanced_method_</usage>
        public static short FindRowStart(ByteBuffer buffer, int rowNum, JetFormat format)
        {
            return CleanRowStart(buffer.GetShort(GetRowStartOffset(rowNum, format)));
        }

        /// <usage>_advanced_method_</usage>
        public static int GetRowStartOffset(int rowNum, JetFormat format)
        {
            return format.OFFSET_ROW_START + (format.SIZE_ROW_LOCATION * rowNum);
        }

        /// <usage>_advanced_method_</usage>
        public static short FindRowEnd(ByteBuffer buffer, int rowNum, JetFormat format)
        {
            return (short)((rowNum == 0) ? format.PAGE_SIZE : CleanRowStart(buffer.GetShort(GetRowEndOffset
                (rowNum, format))));
        }

        /// <usage>_advanced_method_</usage>
        public static int GetRowEndOffset(int rowNum, JetFormat format)
        {
            return format.OFFSET_ROW_START + (format.SIZE_ROW_LOCATION * (rowNum - 1));
        }

        /// <usage>_advanced_method_</usage>
        public static int GetRowSpaceUsage(int rowSize, JetFormat format)
        {
            return rowSize + format.SIZE_ROW_LOCATION;
        }

        /// <returns>the "AutoNumber" columns in the given collection of columns.</returns>
        /// <usage>_advanced_method_</usage>
        public static IList<Column> GetAutoNumberColumns(ICollection<Column> columns)
        {
            IList<Column> autoCols = new AList<Column>();
            foreach (Column c in columns)
            {
                if (c.IsAutoNumber())
                {
                    autoCols.AddItem(c);
                }
            }
            return autoCols;
        }

        /// <summary>
        /// Returns
        /// <code>true</code>
        /// if a row of the given size will fit on the given
        /// data page,
        /// <code>false</code>
        /// otherwise.
        /// </summary>
        /// <usage>_advanced_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static bool RowFitsOnDataPage(int rowLength, ByteBuffer dataPage, JetFormat
             format)
        {
            int rowSpaceUsage = GetRowSpaceUsage(rowLength, format);
            short freeSpaceInPage = dataPage.GetShort(format.OFFSET_FREE_SPACE);
            int rowsOnPage = GetRowsOnDataPage(dataPage, format);
            return ((rowSpaceUsage <= freeSpaceInPage) && (rowsOnPage < format.MAX_NUM_ROWS_ON_DATA_PAGE
                ));
        }

        /// <summary>
        /// Duplicates and returns a row of data, optionally with a longer length
        /// filled with
        /// <code>null</code>
        /// .
        /// </summary>
        internal static object[] DupeRow(object[] row, int newRowLength)
        {
            object[] copy = new object[newRowLength];
            System.Array.Copy(row, 0, copy, 0, row.Length);
            return copy;
        }

        /// <summary>various statuses for the row data</summary>
        internal enum RowStatus
        {
            INIT,
            INVALID_PAGE,
            INVALID_ROW,
            VALID,
            DELETED,
            NORMAL,
            OVERFLOW
        }

        /// <summary>the phases the RowState moves through as the data is parsed</summary>
        internal enum RowStateStatus
        {
            INIT,
            AT_HEADER,
            AT_FINAL
        }

        /// <summary>Maintains the state of reading a row of data.</summary>
        /// <remarks>Maintains the state of reading a row of data.</remarks>
        /// <usage>_advanced_class_</usage>
        public sealed class RowState
        {
            /// <summary>Buffer used for reading the header row data pages</summary>
            private readonly TempPageHolder _headerRowBufferH;

            /// <summary>the header rowId</summary>
            private RowId _headerRowId = RowId.FIRST_ROW_ID;

            /// <summary>the number of rows on the header page</summary>
            private int _rowsOnHeaderPage;

            /// <summary>the rowState status</summary>
            private Table.RowStateStatus _status = Table.RowStateStatus.INIT;

            /// <summary>the row status</summary>
            private Table.RowStatus _rowStatus = Table.RowStatus.INIT;

            /// <summary>buffer used for reading overflow pages</summary>
            private readonly TempPageHolder _overflowRowBufferH = TempPageHolder.NewHolder(TempBufferHolder.Type
                .SOFT);

            /// <summary>
            /// the row buffer which contains the final data (after following any
            /// overflow pointers)
            /// </summary>
            private ByteBuffer _finalRowBuffer;

            /// <summary>
            /// the rowId which contains the final data (after following any overflow
            /// pointers)
            /// </summary>
            private RowId _finalRowId = null;

            /// <summary>true if the row values array has data</summary>
            private bool _haveRowValues;

            /// <summary>values read from the last row</summary>
            private readonly object[] _rowValues;

            /// <summary>
            /// last modification count seen on the table we track this so that the
            /// rowState can detect updates to the table and re-read any buffered
            /// data
            /// </summary>
            private int _lastModCount;

            /// <summary>optional error handler to use when row errors are encountered</summary>
            private ErrorHandler _errorHandler;

            /// <summary>cached variable column offsets for jump-table based rows</summary>
            private short[] _varColOffsets;

            internal RowState(Table _enclosing, TempBufferHolder.Type headerType)
            {
                this._enclosing = _enclosing;
                this._headerRowBufferH = TempPageHolder.NewHolder(headerType);
                this._rowValues = new object[this._enclosing.GetColumnCount()];
                this._lastModCount = this._enclosing._modCount;
            }

            public Table GetTable()
            {
                return this._enclosing;
            }

            public ErrorHandler GetErrorHandler()
            {
                return ((this._errorHandler != null) ? this._errorHandler : this.GetTable().GetErrorHandler
                    ());
            }

            public void SetErrorHandler(ErrorHandler newErrorHandler)
            {
                this._errorHandler = newErrorHandler;
            }

            public void Reset()
            {
                this._finalRowId = null;
                this._finalRowBuffer = null;
                this._rowsOnHeaderPage = 0;
                this._status = Table.RowStateStatus.INIT;
                this._rowStatus = Table.RowStatus.INIT;
                this._varColOffsets = null;
                if (this._haveRowValues)
                {
                    Arrays.Fill(this._rowValues, null);
                    this._haveRowValues = false;
                }
            }

            public bool IsUpToDate()
            {
                return (this._enclosing._modCount == this._lastModCount);
            }

            internal void CheckForModification()
            {
                if (!this.IsUpToDate())
                {
                    this.Reset();
                    this._headerRowBufferH.Invalidate();
                    this._overflowRowBufferH.Invalidate();
                    this._lastModCount = this._enclosing._modCount;
                }
            }

            /// <exception cref="System.IO.IOException"></exception>
            internal ByteBuffer GetFinalPage()
            {
                if (this._finalRowBuffer == null)
                {
                    // (re)load current page
                    this._finalRowBuffer = this.GetHeaderPage();
                }
                return this._finalRowBuffer;
            }

            public RowId GetFinalRowId()
            {
                if (this._finalRowId == null)
                {
                    this._finalRowId = this.GetHeaderRowId();
                }
                return this._finalRowId;
            }

            internal void SetRowStatus(Table.RowStatus rowStatus)
            {
                this._rowStatus = rowStatus;
            }

            public bool IsValid()
            {
                return ((int)(this._rowStatus) >= (int)(Table.RowStatus.VALID));
            }

            public bool IsDeleted()
            {
                return (this._rowStatus == Table.RowStatus.DELETED);
            }

            public bool IsOverflow()
            {
                return (this._rowStatus == Table.RowStatus.OVERFLOW);
            }

            public bool IsHeaderPageNumberValid()
            {
                return ((int)(this._rowStatus) > (int)(Table.RowStatus.INVALID_PAGE));
            }

            public bool IsHeaderRowNumberValid()
            {
                return ((int)(this._rowStatus) > (int)(Table.RowStatus.INVALID_ROW));
            }

            internal void SetStatus(Table.RowStateStatus status)
            {
                this._status = status;
            }

            public bool IsAtHeaderRow()
            {
                return ((int)(this._status) >= (int)(Table.RowStateStatus.AT_HEADER));
            }

            public bool IsAtFinalRow()
            {
                return ((int)(this._status) >= (int)(Table.RowStateStatus.AT_FINAL));
            }

            internal object SetRowValue(int idx, object value)
            {
                this._haveRowValues = true;
                this._rowValues[idx] = value;
                return value;
            }

            public object[] GetRowValues()
            {
                return Table.DupeRow(this._rowValues, this._rowValues.Length);
            }

            internal short[] GetVarColOffsets()
            {
                return this._varColOffsets;
            }

            internal void SetVarColOffsets(short[] varColOffsets)
            {
                this._varColOffsets = varColOffsets;
            }

            public RowId GetHeaderRowId()
            {
                return this._headerRowId;
            }

            public int GetRowsOnHeaderPage()
            {
                return this._rowsOnHeaderPage;
            }

            /// <exception cref="System.IO.IOException"></exception>
            internal ByteBuffer GetHeaderPage()
            {
                this.CheckForModification();
                return this._headerRowBufferH.GetPage(this._enclosing.GetPageChannel());
            }

            /// <exception cref="System.IO.IOException"></exception>
            internal ByteBuffer SetHeaderRow(RowId rowId)
            {
                this.CheckForModification();
                // don't do any work if we are already positioned correctly
                if (this.IsAtHeaderRow() && (this.GetHeaderRowId().Equals(rowId)))
                {
                    return (this.IsValid() ? this.GetHeaderPage() : null);
                }
                // rejigger everything
                this.Reset();
                this._headerRowId = rowId;
                this._finalRowId = rowId;
                int pageNumber = rowId.GetPageNumber();
                int rowNumber = rowId.GetRowNumber();
                if ((pageNumber < 0) || !this._enclosing._ownedPages.ContainsPageNumber(pageNumber
                    ))
                {
                    this.SetRowStatus(Table.RowStatus.INVALID_PAGE);
                    return null;
                }
                this._finalRowBuffer = this._headerRowBufferH.SetPage(this._enclosing.GetPageChannel
                    (), pageNumber);
                this._rowsOnHeaderPage = Table.GetRowsOnDataPage(this._finalRowBuffer, this._enclosing
                    .GetFormat());
                if ((rowNumber < 0) || (rowNumber >= this._rowsOnHeaderPage))
                {
                    this.SetRowStatus(Table.RowStatus.INVALID_ROW);
                    return null;
                }
                this.SetRowStatus(Table.RowStatus.VALID);
                return this._finalRowBuffer;
            }

            /// <exception cref="System.IO.IOException"></exception>
            internal ByteBuffer SetOverflowRow(RowId rowId)
            {
                // this should never see modifications because it only happens within
                // the positionAtRowData method
                if (!this.IsUpToDate())
                {
                    throw new InvalidOperationException("Table modified while searching?");
                }
                if (this._rowStatus != Table.RowStatus.OVERFLOW)
                {
                    throw new InvalidOperationException("Row is not an overflow row?");
                }
                this._finalRowId = rowId;
                this._finalRowBuffer = this._overflowRowBufferH.SetPage(this._enclosing.GetPageChannel
                    (), rowId.GetPageNumber());
                return this._finalRowBuffer;
            }

            /// <exception cref="System.IO.IOException"></exception>
            internal object HandleRowError(Column column, byte[] columnData, Exception error)
            {
                return this.GetErrorHandler().HandleRowError(column, columnData, this, error);
            }

            public override string ToString()
            {
                return "RowState: headerRowId = " + this._headerRowId + ", finalRowId = " + this.
                    _finalRowId;
            }

            private readonly Table _enclosing;
        }
    }
}
