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

namespace HealthMarketScience.Jackcess
{
    /// <summary>Cursor backed by an index with extended traversal options.</summary>
    /// <remarks>Cursor backed by an index with extended traversal options.</remarks>
    /// <author>James Ahlborn</author>
    public class IndexCursor : Cursor
    {
        /// <summary>IndexDirHandler for forward traversal</summary>
        private readonly IndexCursor.IndexDirHandler _forwardDirHandler;

        /// <summary>IndexDirHandler for backward traversal</summary>
        private readonly IndexCursor.IndexDirHandler _reverseDirHandler;

        /// <summary>logical index which this cursor is using</summary>
        private readonly Index _index;

        /// <summary>Cursor over the entries of the relvant index</summary>
        private readonly IndexData.EntryCursor _entryCursor;

        /// <summary>column names for the index entry columns</summary>
        private ICollection<string> _indexEntryPattern;

        /// <exception cref="System.IO.IOException"></exception>
        private IndexCursor(Table table, Index index, IndexData.EntryCursor entryCursor) :
            base(new Cursor.ID(table, index), table, new IndexCursor.IndexPosition(entryCursor
            .GetFirstEntry()), new IndexCursor.IndexPosition(entryCursor.GetLastEntry()))
        {
            _forwardDirHandler = new IndexCursor.ForwardIndexDirHandler(this);
            _reverseDirHandler = new IndexCursor.ReverseIndexDirHandler(this);
            _index = index;
            _index.Initialize();
            _entryCursor = entryCursor;
        }

        /// <summary>Creates an indexed cursor for the given table.</summary>
        /// <remarks>
        /// Creates an indexed cursor for the given table.
        /// <p>
        /// Note, index based table traversal may not include all rows, as certain
        /// types of indexes do not include all entries (namely, some indexes ignore
        /// null entries, see
        /// <see cref="Index.ShouldIgnoreNulls()">Index.ShouldIgnoreNulls()</see>
        /// ).
        /// </remarks>
        /// <param name="table">the table over which this cursor will traverse</param>
        /// <param name="index">
        /// index for the table which will define traversal order as
        /// well as enhance certain lookups
        /// </param>
        /// <exception cref="System.IO.IOException"></exception>
        public static HealthMarketScience.Jackcess.IndexCursor CreateCursor(Table table,
            Index index)
        {
            return CreateCursor(table, index, null, null);
        }

        /// <summary>
        /// Creates an indexed cursor for the given table, narrowed to the given
        /// range.
        /// </summary>
        /// <remarks>
        /// Creates an indexed cursor for the given table, narrowed to the given
        /// range.
        /// <p>
        /// Note, index based table traversal may not include all rows, as certain
        /// types of indexes do not include all entries (namely, some indexes ignore
        /// null entries, see
        /// <see cref="Index.ShouldIgnoreNulls()">Index.ShouldIgnoreNulls()</see>
        /// ).
        /// </remarks>
        /// <param name="table">the table over which this cursor will traverse</param>
        /// <param name="index">
        /// index for the table which will define traversal order as
        /// well as enhance certain lookups
        /// </param>
        /// <param name="startRow">
        /// the first row of data for the cursor (inclusive), or
        /// <code>null</code>
        /// for the first entry
        /// </param>
        /// <param name="endRow">
        /// the last row of data for the cursor (inclusive), or
        /// <code>null</code>
        /// for the last entry
        /// </param>
        /// <exception cref="System.IO.IOException"></exception>
        public static HealthMarketScience.Jackcess.IndexCursor CreateCursor(Table table,
            Index index, object[] startRow, object[] endRow)
        {
            return CreateCursor(table, index, startRow, true, endRow, true);
        }

        /// <summary>
        /// Creates an indexed cursor for the given table, narrowed to the given
        /// range.
        /// </summary>
        /// <remarks>
        /// Creates an indexed cursor for the given table, narrowed to the given
        /// range.
        /// <p>
        /// Note, index based table traversal may not include all rows, as certain
        /// types of indexes do not include all entries (namely, some indexes ignore
        /// null entries, see
        /// <see cref="Index.ShouldIgnoreNulls()">Index.ShouldIgnoreNulls()</see>
        /// ).
        /// </remarks>
        /// <param name="table">the table over which this cursor will traverse</param>
        /// <param name="index">
        /// index for the table which will define traversal order as
        /// well as enhance certain lookups
        /// </param>
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
        public static HealthMarketScience.Jackcess.IndexCursor CreateCursor(Table table,
            Index index, object[] startRow, bool startInclusive, object[] endRow, bool endInclusive
            )
        {
            if (table != index.GetTable())
            {
                throw new ArgumentException("Given index is not for given table: " + index + ", "
                     + table);
            }
            if (!table.GetFormat().INDEXES_SUPPORTED)
            {
                throw new ArgumentException("JetFormat " + table.GetFormat() + " does not currently support index lookups"
                    );
            }
            if (index.GetIndexData().IsReadOnly())
            {
                throw new ArgumentException("Given index " + index + " is not usable for indexed lookups because it is read-only"
                    );
            }
            HealthMarketScience.Jackcess.IndexCursor cursor = new HealthMarketScience.Jackcess.IndexCursor
                (table, index, index.Cursor(startRow, startInclusive, endRow, endInclusive));
            // init the column matcher appropriately for the index type
            cursor.SetColumnMatcher(null);
            return cursor;
        }

        public virtual Index GetIndex()
        {
            return _index;
        }

        /// <summary>
        /// Moves to the first row (as defined by the cursor) where the index entries
        /// match the given values.
        /// </summary>
        /// <remarks>
        /// Moves to the first row (as defined by the cursor) where the index entries
        /// match the given values.  If a match is not found (or an exception is
        /// thrown), the cursor is restored to its previous state.
        /// </remarks>
        /// <param name="entryValues">the column values for the index's columns.</param>
        /// <returns>
        /// 
        /// <code>true</code>
        /// if a valid row was found with the given values,
        /// <code>false</code>
        /// if no row was found
        /// </returns>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual bool FindRowByEntry(params object[] entryValues)
        {
            Cursor.Position curPos = _curPos;
            Cursor.Position prevPos = _prevPos;
            bool found = false;
            try
            {
                found = FindRowByEntryImpl(ToRowValues(entryValues), true);
                return found;
            }
            finally
            {
                if (!found)
                {
                    try
                    {
                        RestorePosition(curPos, prevPos);
                    }
                    catch (IOException e)
                    {
                        System.Console.Error.WriteLine("Failed restoring position");
                        Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Moves to the first row (as defined by the cursor) where the index entries
        /// are &gt;= the given values.
        /// </summary>
        /// <remarks>
        /// Moves to the first row (as defined by the cursor) where the index entries
        /// are &gt;= the given values.  If a an exception is thrown, the cursor is
        /// restored to its previous state.
        /// </remarks>
        /// <param name="entryValues">the column values for the index's columns.</param>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void FindClosestRowByEntry(params object[] entryValues)
        {
            Cursor.Position curPos = _curPos;
            Cursor.Position prevPos = _prevPos;
            bool found = false;
            try
            {
                FindRowByEntryImpl(ToRowValues(entryValues), false);
                found = true;
            }
            finally
            {
                if (!found)
                {
                    try
                    {
                        RestorePosition(curPos, prevPos);
                    }
                    catch (IOException e)
                    {
                        System.Console.Error.WriteLine("Failed restoring position");
                        Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Returns
        /// <code>true</code>
        /// if the current row matches the given index entries.
        /// </summary>
        /// <param name="entryValues">the column values for the index's columns.</param>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual bool CurrentRowMatchesEntry(params object[] entryValues)
        {
            return CurrentRowMatchesEntryImpl(ToRowValues(entryValues));
        }

        /// <summary>
        /// Returns a modifiable Iterator which will iterate through all the rows of
        /// this table which match the given index entries.
        /// </summary>
        /// <remarks>
        /// Returns a modifiable Iterator which will iterate through all the rows of
        /// this table which match the given index entries.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">
        /// if an IOException is thrown by one of the
        /// operations, the actual exception will be contained within
        /// </exception>
        public virtual Iterator<IDictionary<string, object>> EntryIterator(params object[]
             entryValues)
        {
            return EntryIterator((ICollection<string>)null, entryValues);
        }

        /// <summary>
        /// Returns a modifiable Iterator which will iterate through all the rows of
        /// this table which match the given index entries, returning only the given
        /// columns.
        /// </summary>
        /// <remarks>
        /// Returns a modifiable Iterator which will iterate through all the rows of
        /// this table which match the given index entries, returning only the given
        /// columns.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">
        /// if an IOException is thrown by one of the
        /// operations, the actual exception will be contained within
        /// </exception>
        public virtual Iterator<IDictionary<string, object>> EntryIterator(ICollection<string
            > columnNames, params object[] entryValues)
        {
            return new IndexCursor.RowEntryIterator(this, columnNames, ToRowValues(entryValues
                ));
        }

        /// <summary>
        /// Returns an Iterable whose iterator() method returns the result of a call
        /// to
        /// <see cref="EntryIterator(object[])">EntryIterator(object[])</see>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// if an IOException is thrown by one of the
        /// operations, the actual exception will be contained within
        /// </exception>
        public virtual Iterable<IDictionary<string, object>> EntryIterable(params object[]
             entryValues)
        {
            return EntryIterable((ICollection<string>)null, entryValues);
        }

        /// <summary>
        /// Returns an Iterable whose iterator() method returns the result of a call
        /// to
        /// <see cref="EntryIterator(System.Collections.Generic.ICollection{E}, object[])">EntryIterator(System.Collections.Generic.ICollection&lt;E&gt;, object[])
        /// 	</see>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// if an IOException is thrown by one of the
        /// operations, the actual exception will be contained within
        /// </exception>
        public virtual Iterable<IDictionary<string, object>> EntryIterable(ICollection<string
            > columnNames, params object[] entryValues)
        {
            return new _Iterable_268(this, columnNames, entryValues);
        }

        private sealed class _Iterable_268 : Iterable<IDictionary<string, object>>
        {
            public _Iterable_268(IndexCursor _enclosing, ICollection<string> columnNames, object
                [] entryValues)
            {
                this._enclosing = _enclosing;
                this.columnNames = columnNames;
                this.entryValues = entryValues;
            }

            public override Iterator<IDictionary<string, object>> Iterator()
            {
                return new IndexCursor.RowEntryIterator(this._enclosing, columnNames, this._enclosing.ToRowValues
                    (entryValues));
            }

            private readonly IndexCursor _enclosing;

            private readonly ICollection<string> columnNames;

            private readonly object[] entryValues;
        }

        protected internal override Cursor.DirHandler GetDirHandler(bool moveForward)
        {
            return (moveForward ? _forwardDirHandler : _reverseDirHandler);
        }

        protected internal override bool IsUpToDate()
        {
            return (base.IsUpToDate() && _entryCursor.IsUpToDate());
        }

        protected internal override void Reset(bool moveForward)
        {
            _entryCursor.Reset(moveForward);
            base.Reset(moveForward);
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override void RestorePositionImpl(Cursor.Position curPos, Cursor.Position
             prevPos)
        {
            if (!(curPos is IndexCursor.IndexPosition) || !(prevPos is IndexCursor.IndexPosition
                ))
            {
                throw new ArgumentException("Restored positions must be index positions");
            }
            _entryCursor.RestorePosition(((IndexCursor.IndexPosition)curPos).GetEntry(), ((IndexCursor.IndexPosition
                )prevPos).GetEntry());
            base.RestorePositionImpl(curPos, prevPos);
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override bool FindRowImpl(Column columnPattern, object valuePattern
            )
        {
            object[] rowValues = _entryCursor.GetIndexData().ConstructIndexRow(columnPattern.
                GetName(), valuePattern);
            if (rowValues == null)
            {
                // bummer, use the default table scan
                return base.FindRowImpl(columnPattern, valuePattern);
            }
            // sweet, we can use our index
            if (!FindPotentialRow(rowValues, true))
            {
                return false;
            }
            // either we found a row with the given value, or none exist in the
            // table
            return CurrentRowMatches(columnPattern, valuePattern);
        }

        /// <summary>
        /// Moves to the first row (as defined by the cursor) where the index entries
        /// match the given values.
        /// </summary>
        /// <remarks>
        /// Moves to the first row (as defined by the cursor) where the index entries
        /// match the given values.  Caller manages save/restore on failure.
        /// </remarks>
        /// <param name="rowValues">the column values built from the index column values</param>
        /// <param name="requireMatch">whether or not an exact match is found</param>
        /// <returns>
        /// 
        /// <code>true</code>
        /// if a valid row was found with the given values,
        /// <code>false</code>
        /// if no row was found
        /// </returns>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal virtual bool FindRowByEntryImpl(object[] rowValues, bool requireMatch
            )
        {
            if (!FindPotentialRow(rowValues, requireMatch))
            {
                return false;
            }
            else
            {
                if (!requireMatch)
                {
                    // nothing more to do, we have moved to the closest row
                    return true;
                }
            }
            return CurrentRowMatchesEntryImpl(rowValues);
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override bool FindRowImpl(IDictionary<string, object> rowPattern
            )
        {
            IndexData indexData = _entryCursor.GetIndexData();
            object[] rowValues = indexData.ConstructIndexRow(rowPattern);
            if (rowValues == null)
            {
                // bummer, use the default table scan
                return base.FindRowImpl(rowPattern);
            }
            // sweet, we can use our index
            if (!FindPotentialRow(rowValues, true))
            {
                // at end of index, no potential matches
                return false;
            }
            // find actual matching row
            IDictionary<string, object> indexRowPattern = null;
            if (rowPattern.Count == indexData.GetColumns().Count)
            {
                // the rowPattern matches our index columns exactly, so we can
                // streamline our testing below
                indexRowPattern = rowPattern;
            }
            else
            {
                // the rowPattern has more columns than just the index, so we need to
                // do more work when testing below
                indexRowPattern = new LinkedHashMap<string, object>();
                foreach (IndexData.ColumnDescriptor idxCol in indexData.GetColumns())
                {
                    indexRowPattern.Put(idxCol.GetName(), rowValues[idxCol.GetColumnIndex()]);
                }
            }
            do
            {
                // there may be multiple columns which fit the pattern subset used by
                // the index, so we need to keep checking until our index values no
                // longer match
                if (!CurrentRowMatches(indexRowPattern))
                {
                    // there are no more rows which could possibly match
                    break;
                }
                // note, if rowPattern == indexRowPattern, no need to do an extra
                // comparison with the current row
                if ((rowPattern == indexRowPattern) || CurrentRowMatches(rowPattern))
                {
                    // found it!
                    return true;
                }
            }
            while (MoveToNextRow());
            // none of the potential rows matched
            return false;
        }

        /// <exception cref="System.IO.IOException"></exception>
        private bool CurrentRowMatchesEntryImpl(object[] rowValues)
        {
            if (_indexEntryPattern == null)
            {
                // init our set of index column names
                _indexEntryPattern = new HashSet<string>();
                foreach (IndexData.ColumnDescriptor col in GetIndex().GetColumns())
                {
                    _indexEntryPattern.AddItem(col.GetName());
                }
            }
            // check the next row to see if it actually matches
            IDictionary<string, object> row = GetCurrentRow(_indexEntryPattern);
            foreach (IndexData.ColumnDescriptor col_1 in GetIndex().GetColumns())
            {
                string columnName = col_1.GetName();
                object patValue = rowValues[col_1.GetColumnIndex()];
                object rowValue = row.Get(columnName);
                if (!_columnMatcher.Matches(GetTable(), columnName, patValue, rowValue))
                {
                    return false;
                }
            }
            return true;
        }

        /// <exception cref="System.IO.IOException"></exception>
        private bool FindPotentialRow(object[] rowValues, bool requireMatch)
        {
            _entryCursor.BeforeEntry(rowValues);
            IndexData.Entry startEntry = _entryCursor.GetNextEntry();
            if (requireMatch && !startEntry.GetRowId().IsValid())
            {
                // at end of index, no potential matches
                return false;
            }
            // move to position and check it out
            RestorePosition(new IndexCursor.IndexPosition(startEntry));
            return true;
        }

        private object[] ToRowValues(object[] entryValues)
        {
            return _entryCursor.GetIndexData().ConstructIndexRowFromEntry(entryValues);
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override Cursor.Position FindAnotherPosition(Table.RowState rowState
            , Cursor.Position curPos, bool moveForward)
        {
            IndexCursor.IndexDirHandler handler = ((IndexCursor.IndexDirHandler)GetDirHandler
                (moveForward));
            IndexCursor.IndexPosition endPos = (IndexCursor.IndexPosition)handler.GetEndPosition
                ();
            IndexData.Entry entry = handler.GetAnotherEntry();
            return ((!entry.Equals(endPos.GetEntry())) ? new IndexCursor.IndexPosition(entry)
                 : endPos);
        }

        protected internal override ColumnMatcher GetDefaultColumnMatcher()
        {
            if (GetIndex().IsUnique())
            {
                // text indexes are case-insensitive, therefore we should always use a
                // case-insensitive matcher for unique indexes.
                return CaseInsensitiveColumnMatcher.INSTANCE;
            }
            return SimpleColumnMatcher.INSTANCE;
        }

        /// <summary>Handles moving the table index cursor in a given direction.</summary>
        /// <remarks>
        /// Handles moving the table index cursor in a given direction.  Separates
        /// cursor logic from value storage.
        /// </remarks>
        private abstract class IndexDirHandler : Cursor.DirHandler
        {
            /// <exception cref="System.IO.IOException"></exception>
            public abstract IndexData.Entry GetAnotherEntry();

            internal IndexDirHandler(IndexCursor _enclosing) : base(_enclosing)
            {
                this._enclosing = _enclosing;
            }

            private readonly IndexCursor _enclosing;
        }

        /// <summary>Handles moving the table index cursor forward.</summary>
        /// <remarks>Handles moving the table index cursor forward.</remarks>
        private sealed class ForwardIndexDirHandler : IndexCursor.IndexDirHandler
        {
            public override Cursor.Position GetBeginningPosition()
            {
                return this._enclosing.GetFirstPosition();
            }

            public override Cursor.Position GetEndPosition()
            {
                return this._enclosing.GetLastPosition();
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override IndexData.Entry GetAnotherEntry()
            {
                return this._enclosing._entryCursor.GetNextEntry();
            }

            internal ForwardIndexDirHandler(IndexCursor _enclosing) : base(_enclosing)
            {
                this._enclosing = _enclosing;
            }

            private readonly IndexCursor _enclosing;
        }

        /// <summary>Handles moving the table index cursor backward.</summary>
        /// <remarks>Handles moving the table index cursor backward.</remarks>
        private sealed class ReverseIndexDirHandler : IndexCursor.IndexDirHandler
        {
            public override Cursor.Position GetBeginningPosition()
            {
                return this._enclosing.GetLastPosition();
            }

            public override Cursor.Position GetEndPosition()
            {
                return this._enclosing.GetFirstPosition();
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override IndexData.Entry GetAnotherEntry()
            {
                return this._enclosing._entryCursor.GetPreviousEntry();
            }

            internal ReverseIndexDirHandler(IndexCursor _enclosing) : base(_enclosing)
            {
                this._enclosing = _enclosing;
            }

            private readonly IndexCursor _enclosing;
        }

        /// <summary>Value object which maintains the current position of an IndexCursor.</summary>
        /// <remarks>Value object which maintains the current position of an IndexCursor.</remarks>
        internal sealed class IndexPosition : Cursor.Position
        {
            private readonly IndexData.Entry _entry;

            internal IndexPosition(IndexData.Entry entry)
            {
                _entry = entry;
            }

            public override RowId GetRowId()
            {
                return GetEntry().GetRowId();
            }

            public IndexData.Entry GetEntry()
            {
                return _entry;
            }

            protected internal override bool EqualsImpl(object o)
            {
                return GetEntry().Equals(((IndexCursor.IndexPosition)o).GetEntry());
            }

            public override string ToString()
            {
                return "Entry = " + GetEntry();
            }
        }

        /// <summary>Row iterator (by matching entry) for this cursor, modifiable.</summary>
        /// <remarks>Row iterator (by matching entry) for this cursor, modifiable.</remarks>
        internal sealed class RowEntryIterator : Cursor.BaseIterator
        {
            private readonly object[] _rowValues;

            internal RowEntryIterator(IndexCursor _enclosing, ICollection<string> columnNames,
                object[] rowValues) : base(_enclosing, columnNames)
            {
                this._enclosing = _enclosing;
                this._rowValues = rowValues;
                try
                {
                    this._hasNext = this._enclosing.FindRowByEntryImpl(rowValues, true);
                    this._validRow = this._hasNext.Value;
                }
                catch (IOException e)
                {
                    throw new InvalidOperationException(e.ToString());
                }
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override bool FindNext()
            {
                return (this._enclosing.MoveToNextRow() && this._enclosing.CurrentRowMatchesEntryImpl
                    (this._rowValues));
            }

            private readonly IndexCursor _enclosing;
        }
    }
}
