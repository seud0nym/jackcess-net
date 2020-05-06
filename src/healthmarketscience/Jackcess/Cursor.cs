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
using System.Collections.Generic;
using System.IO;
using Apache.Commons.Lang;
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Manages iteration for a Table.</summary>
	/// <remarks>
	/// Manages iteration for a Table.  Different cursors provide different methods
	/// of traversing a table.  Cursors should be fairly robust in the face of
	/// table modification during traversal (although depending on how the table is
	/// traversed, row updates may or may not be seen).  Multiple cursors may
	/// traverse the same table simultaneously.
	/// <p>
	/// The Cursor provides a variety of static utility methods to construct
	/// cursors with given characteristics or easily search for specific values.
	/// For even friendlier and more flexible construction, see
	/// <see cref="CursorBuilder">CursorBuilder</see>
	/// .
	/// <p>
	/// Is not thread-safe.
	/// </remarks>
	/// <author>James Ahlborn</author>
	public abstract class Cursor : Sharpen.Iterable<IDictionary<string, object>>
	{
		/// <summary>boolean value indicating forward movement</summary>
		public const bool MOVE_FORWARD = true;

		/// <summary>boolean value indicating reverse movement</summary>
		public const bool MOVE_REVERSE = false;

		/// <summary>first position for the TableScanCursor</summary>
		private static readonly Cursor.ScanPosition FIRST_SCAN_POSITION = new Cursor.ScanPosition
			(RowId.FIRST_ROW_ID);

		/// <summary>last position for the TableScanCursor</summary>
		private static readonly Cursor.ScanPosition LAST_SCAN_POSITION = new Cursor.ScanPosition
			(RowId.LAST_ROW_ID);

		/// <summary>identifier for this cursor</summary>
		private readonly Cursor.ID _id;

		/// <summary>owning table</summary>
		private readonly Table _table;

		/// <summary>State used for reading the table rows</summary>
		private readonly Table.RowState _rowState;

		/// <summary>the first (exclusive) row id for this cursor</summary>
		private readonly Cursor.Position _firstPos;

		/// <summary>the last (exclusive) row id for this cursor</summary>
		private readonly Cursor.Position _lastPos;

		/// <summary>the previous row</summary>
		protected internal Cursor.Position _prevPos;

		/// <summary>the current row</summary>
		protected internal Cursor.Position _curPos;

		/// <summary>ColumnMatcher to be used when matching column values</summary>
		protected internal ColumnMatcher _columnMatcher = SimpleColumnMatcher.INSTANCE;

		protected internal Cursor(Cursor.ID id, Table table, Cursor.Position firstPos, Cursor.Position
			 lastPos)
		{
			_id = id;
			_table = table;
			_rowState = _table.CreateRowState();
			_firstPos = firstPos;
			_lastPos = lastPos;
			_curPos = firstPos;
			_prevPos = firstPos;
		}

		/// <summary>Creates a normal, un-indexed cursor for the given table.</summary>
		/// <remarks>Creates a normal, un-indexed cursor for the given table.</remarks>
		/// <param name="table">the table over which this cursor will traverse</param>
		public static HealthMarketScience.Jackcess.Cursor CreateCursor(Table table)
		{
			return new Cursor.TableScanCursor(table);
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
		public static HealthMarketScience.Jackcess.Cursor CreateIndexCursor(Table table, 
			Index index)
		{
			return IndexCursor.CreateCursor(table, index);
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
		public static HealthMarketScience.Jackcess.Cursor CreateIndexCursor(Table table, 
			Index index, object[] startRow, object[] endRow)
		{
			return IndexCursor.CreateCursor(table, index, startRow, endRow);
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
		public static HealthMarketScience.Jackcess.Cursor CreateIndexCursor(Table table, 
			Index index, object[] startRow, bool startInclusive, object[] endRow, bool endInclusive
			)
		{
			return IndexCursor.CreateCursor(table, index, startRow, startInclusive, endRow, endInclusive
				);
		}

		/// <summary>
		/// Convenience method for finding a specific row in a table which matches a
		/// given row "pattern".
		/// </summary>
		/// <remarks>
		/// Convenience method for finding a specific row in a table which matches a
		/// given row "pattern".  See
		/// <see cref="FindRow(System.Collections.Generic.IDictionary{K, V})">FindRow(System.Collections.Generic.IDictionary&lt;K, V&gt;)
		/// 	</see>
		/// for details on the
		/// rowPattern.
		/// </remarks>
		/// <param name="table">the table to search</param>
		/// <param name="rowPattern">pattern to be used to find the row</param>
		/// <returns>
		/// the matching row or
		/// <code>null</code>
		/// if a match could not be found.
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public static IDictionary<string, object> FindRow(Table table, IDictionary<string
			, object> rowPattern)
		{
			HealthMarketScience.Jackcess.Cursor cursor = CreateCursor(table);
			if (cursor.FindRow(rowPattern))
			{
				return cursor.GetCurrentRow();
			}
			return null;
		}

		/// <summary>
		/// Convenience method for finding a specific row in a table which matches a
		/// given row "pattern".
		/// </summary>
		/// <remarks>
		/// Convenience method for finding a specific row in a table which matches a
		/// given row "pattern".  See
		/// <see cref="FindRow(Column, object)">FindRow(Column, object)</see>
		/// for details on
		/// the pattern.
		/// <p>
		/// Note, a
		/// <code>null</code>
		/// result value is ambiguous in that it could imply no
		/// match or a matching row with
		/// <code>null</code>
		/// for the desired value.  If
		/// distinguishing this situation is important, you will need to use a Cursor
		/// directly instead of this convenience method.
		/// </remarks>
		/// <param name="table">the table to search</param>
		/// <param name="column">column whose value should be returned</param>
		/// <param name="columnPattern">column being matched by the valuePattern</param>
		/// <param name="valuePattern">
		/// value from the columnPattern which will match the
		/// desired row
		/// </param>
		/// <returns>
		/// the matching row or
		/// <code>null</code>
		/// if a match could not be found.
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public static object FindValue(Table table, Column column, Column columnPattern, 
			object valuePattern)
		{
			HealthMarketScience.Jackcess.Cursor cursor = CreateCursor(table);
			if (cursor.FindRow(columnPattern, valuePattern))
			{
				return cursor.GetCurrentRowValue(column);
			}
			return null;
		}

		/// <summary>
		/// Convenience method for finding a specific row in an indexed table which
		/// matches a given row "pattern".
		/// </summary>
		/// <remarks>
		/// Convenience method for finding a specific row in an indexed table which
		/// matches a given row "pattern".  See
		/// <see cref="FindRow(System.Collections.Generic.IDictionary{K, V})">FindRow(System.Collections.Generic.IDictionary&lt;K, V&gt;)
		/// 	</see>
		/// for details on
		/// the rowPattern.
		/// </remarks>
		/// <param name="table">the table to search</param>
		/// <param name="index">index to assist the search</param>
		/// <param name="rowPattern">pattern to be used to find the row</param>
		/// <returns>
		/// the matching row or
		/// <code>null</code>
		/// if a match could not be found.
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public static IDictionary<string, object> FindRow(Table table, Index index, IDictionary
			<string, object> rowPattern)
		{
			HealthMarketScience.Jackcess.Cursor cursor = CreateIndexCursor(table, index);
			if (cursor.FindRow(rowPattern))
			{
				return cursor.GetCurrentRow();
			}
			return null;
		}

		/// <summary>
		/// Convenience method for finding a specific row in a table which matches a
		/// given row "pattern".
		/// </summary>
		/// <remarks>
		/// Convenience method for finding a specific row in a table which matches a
		/// given row "pattern".  See
		/// <see cref="FindRow(Column, object)">FindRow(Column, object)</see>
		/// for details on
		/// the pattern.
		/// <p>
		/// Note, a
		/// <code>null</code>
		/// result value is ambiguous in that it could imply no
		/// match or a matching row with
		/// <code>null</code>
		/// for the desired value.  If
		/// distinguishing this situation is important, you will need to use a Cursor
		/// directly instead of this convenience method.
		/// </remarks>
		/// <param name="table">the table to search</param>
		/// <param name="index">index to assist the search</param>
		/// <param name="column">column whose value should be returned</param>
		/// <param name="columnPattern">column being matched by the valuePattern</param>
		/// <param name="valuePattern">
		/// value from the columnPattern which will match the
		/// desired row
		/// </param>
		/// <returns>
		/// the matching row or
		/// <code>null</code>
		/// if a match could not be found.
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public static object FindValue(Table table, Index index, Column column, Column columnPattern
			, object valuePattern)
		{
			HealthMarketScience.Jackcess.Cursor cursor = CreateIndexCursor(table, index);
			if (cursor.FindRow(columnPattern, valuePattern))
			{
				return cursor.GetCurrentRowValue(column);
			}
			return null;
		}

		public virtual Cursor.ID GetId()
		{
			return _id;
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

		/// <summary>
		/// Gets the currently configured ErrorHandler (always non-
		/// <code>null</code>
		/// ).
		/// This will be used to handle all errors.
		/// </summary>
		public virtual ErrorHandler GetErrorHandler()
		{
			return _rowState.GetErrorHandler();
		}

		/// <summary>Sets a new ErrorHandler.</summary>
		/// <remarks>
		/// Sets a new ErrorHandler.  If
		/// <code>null</code>
		/// , resets to using the
		/// ErrorHandler configured at the Table level.
		/// </remarks>
		public virtual void SetErrorHandler(ErrorHandler newErrorHandler)
		{
			_rowState.SetErrorHandler(newErrorHandler);
		}

		/// <summary>
		/// Returns the currently configured ColumnMatcher, always non-
		/// <code>null</code>
		/// .
		/// </summary>
		public virtual ColumnMatcher GetColumnMatcher()
		{
			return _columnMatcher;
		}

		/// <summary>Sets a new ColumnMatcher.</summary>
		/// <remarks>
		/// Sets a new ColumnMatcher.  If
		/// <code>null</code>
		/// , resets to using the
		/// default matcher,
		/// <see cref="SimpleColumnMatcher.INSTANCE">SimpleColumnMatcher.INSTANCE</see>
		/// .
		/// </remarks>
		public virtual void SetColumnMatcher(ColumnMatcher columnMatcher)
		{
			if (columnMatcher == null)
			{
				columnMatcher = GetDefaultColumnMatcher();
			}
			_columnMatcher = columnMatcher;
		}

		/// <summary>Returns the default ColumnMatcher for this Cursor.</summary>
		/// <remarks>Returns the default ColumnMatcher for this Cursor.</remarks>
		protected internal virtual ColumnMatcher GetDefaultColumnMatcher()
		{
			return SimpleColumnMatcher.INSTANCE;
		}

		/// <summary>
		/// Returns the current state of the cursor which can be restored at a future
		/// point in time by a call to
		/// <see cref="RestoreSavepoint(Savepoint)">RestoreSavepoint(Savepoint)</see>
		/// .
		/// <p>
		/// Savepoints may be used across different cursor instances for the same
		/// table, but they must have the same
		/// <see cref="ID">ID</see>
		/// .
		/// </summary>
		public virtual Cursor.Savepoint GetSavepoint()
		{
			return new Cursor.Savepoint(_id, _curPos, _prevPos);
		}

		/// <summary>
		/// Moves the cursor to a savepoint previously returned from
		/// <see cref="GetSavepoint()">GetSavepoint()</see>
		/// .
		/// </summary>
		/// <exception cref="System.ArgumentException">
		/// if the given savepoint does not have a
		/// cursorId equal to this cursor's id
		/// </exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void RestoreSavepoint(Cursor.Savepoint savepoint)
		{
			if (!_id.Equals(savepoint.GetCursorId()))
			{
				throw new ArgumentException("Savepoint " + savepoint + " is not valid for this cursor with id "
					 + _id);
			}
			RestorePosition(savepoint.GetCurrentPosition(), savepoint.GetPreviousPosition());
		}

		/// <summary>Returns the first row id (exclusive) as defined by this cursor.</summary>
		/// <remarks>Returns the first row id (exclusive) as defined by this cursor.</remarks>
		protected internal virtual Cursor.Position GetFirstPosition()
		{
			return _firstPos;
		}

		/// <summary>Returns the last row id (exclusive) as defined by this cursor.</summary>
		/// <remarks>Returns the last row id (exclusive) as defined by this cursor.</remarks>
		protected internal virtual Cursor.Position GetLastPosition()
		{
			return _lastPos;
		}

		/// <summary>Resets this cursor for forward traversal.</summary>
		/// <remarks>
		/// Resets this cursor for forward traversal.  Calls
		/// <see cref="BeforeFirst()">BeforeFirst()</see>
		/// .
		/// </remarks>
		public virtual void Reset()
		{
			BeforeFirst();
		}

		/// <summary>
		/// Resets this cursor for forward traversal (sets cursor to before the first
		/// row).
		/// </summary>
		/// <remarks>
		/// Resets this cursor for forward traversal (sets cursor to before the first
		/// row).
		/// </remarks>
		public virtual void BeforeFirst()
		{
			Reset(MOVE_FORWARD);
		}

		/// <summary>
		/// Resets this cursor for reverse traversal (sets cursor to after the last
		/// row).
		/// </summary>
		/// <remarks>
		/// Resets this cursor for reverse traversal (sets cursor to after the last
		/// row).
		/// </remarks>
		public virtual void AfterLast()
		{
			Reset(MOVE_REVERSE);
		}

		/// <summary>
		/// Returns
		/// <code>true</code>
		/// if the cursor is currently positioned before the
		/// first row,
		/// <code>false</code>
		/// otherwise.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual bool IsBeforeFirst()
		{
			if (GetFirstPosition().Equals(_curPos))
			{
				return !RecheckPosition(MOVE_REVERSE);
			}
			return false;
		}

		/// <summary>
		/// Returns
		/// <code>true</code>
		/// if the cursor is currently positioned after the
		/// last row,
		/// <code>false</code>
		/// otherwise.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual bool IsAfterLast()
		{
			if (GetLastPosition().Equals(_curPos))
			{
				return !RecheckPosition(MOVE_FORWARD);
			}
			return false;
		}

		/// <summary>
		/// Returns
		/// <code>true</code>
		/// if the row at which the cursor is currently
		/// positioned is deleted,
		/// <code>false</code>
		/// otherwise (including invalid rows).
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual bool IsCurrentRowDeleted()
		{
			// we need to ensure that the "deleted" flag has been read for this row
			// (or re-read if the table has been recently modified)
			Table.PositionAtRowData(_rowState, _curPos.GetRowId());
			return _rowState.IsDeleted();
		}

		/// <summary>Resets this cursor for traversing the given direction.</summary>
		/// <remarks>Resets this cursor for traversing the given direction.</remarks>
		protected internal virtual void Reset(bool moveForward)
		{
			_curPos = GetDirHandler(moveForward).GetBeginningPosition();
			_prevPos = _curPos;
			_rowState.Reset();
		}

		/// <summary>
		/// Returns an Iterable whose iterator() method calls <code>afterLast</code>
		/// on this cursor and returns a modifiable Iterator which will iterate
		/// through all the rows of this table in reverse order.
		/// </summary>
		/// <remarks>
		/// Returns an Iterable whose iterator() method calls <code>afterLast</code>
		/// on this cursor and returns a modifiable Iterator which will iterate
		/// through all the rows of this table in reverse order.  Use of the Iterator
		/// follows the same restrictions as a call to <code>getPreviousRow</code>.
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">
		/// if an IOException is thrown by one of the
		/// operations, the actual exception will be contained within
		/// </exception>
		public virtual Sharpen.Iterable<IDictionary<string, object>> ReverseIterable()
		{
			return ReverseIterable(null);
		}

		/// <summary>
		/// Returns an Iterable whose iterator() method calls <code>afterLast</code>
		/// on this table and returns a modifiable Iterator which will iterate
		/// through all the rows of this table in reverse order, returning only the
		/// given columns.
		/// </summary>
		/// <remarks>
		/// Returns an Iterable whose iterator() method calls <code>afterLast</code>
		/// on this table and returns a modifiable Iterator which will iterate
		/// through all the rows of this table in reverse order, returning only the
		/// given columns.  Use of the Iterator follows the same restrictions as a
		/// call to <code>getPreviousRow</code>.
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">
		/// if an IOException is thrown by one of the
		/// operations, the actual exception will be contained within
		/// </exception>
		public virtual Sharpen.Iterable<IDictionary<string, object>> ReverseIterable(ICollection
			<string> columnNames)
		{
			return new _Iterable_468(this, columnNames);
		}

		private sealed class _Iterable_468 : Sharpen.Iterable<IDictionary<string, object>
			>
		{
			public _Iterable_468(Cursor cursor, ICollection<string> columnNames)
			{
				this.cursor = cursor;
				this.columnNames = columnNames;
			}

			public override Sharpen.Iterator<IDictionary<string, object>> Iterator()
			{
				return new Cursor.RowIterator(this.cursor, columnNames, HealthMarketScience.Jackcess.Cursor
					.MOVE_REVERSE);
			}

			private readonly ICollection<string> columnNames;
			private readonly Cursor cursor;
		}

		/// <summary>
		/// Calls <code>beforeFirst</code> on this cursor and returns a modifiable
		/// Iterator which will iterate through all the rows of this table.
		/// </summary>
		/// <remarks>
		/// Calls <code>beforeFirst</code> on this cursor and returns a modifiable
		/// Iterator which will iterate through all the rows of this table.  Use of
		/// the Iterator follows the same restrictions as a call to
		/// <code>getNextRow</code>.
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">
		/// if an IOException is thrown by one of the
		/// operations, the actual exception will be contained within
		/// </exception>
		public override Sharpen.Iterator<IDictionary<string, object>> Iterator()
		{
			return Iterator(null);
		}

		/// <summary>
		/// Returns an Iterable whose iterator() method returns the result of a call
		/// to
		/// <see cref="Iterator(System.Collections.Generic.ICollection{E})">Iterator(System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// </summary>
		/// <exception cref="System.InvalidOperationException">
		/// if an IOException is thrown by one of the
		/// operations, the actual exception will be contained within
		/// </exception>
		public virtual Sharpen.Iterable<IDictionary<string, object>> Iterable(ICollection
			<string> columnNames)
		{
			return new _Iterable_497(this, columnNames);
		}

		private sealed class _Iterable_497 : Sharpen.Iterable<IDictionary<string, object>
			>
		{
			public _Iterable_497(Cursor _enclosing, ICollection<string> columnNames)
			{
				this._enclosing = _enclosing;
				this.columnNames = columnNames;
			}

			public override Sharpen.Iterator<IDictionary<string, object>> Iterator()
			{
				return this._enclosing.Iterator(columnNames);
			}

			private readonly Cursor _enclosing;

			private readonly ICollection<string> columnNames;
		}

		/// <summary>
		/// Calls <code>beforeFirst</code> on this table and returns a modifiable
		/// Iterator which will iterate through all the rows of this table, returning
		/// only the given columns.
		/// </summary>
		/// <remarks>
		/// Calls <code>beforeFirst</code> on this table and returns a modifiable
		/// Iterator which will iterate through all the rows of this table, returning
		/// only the given columns.  Use of the Iterator follows the same
		/// restrictions as a call to <code>getNextRow</code>.
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">
		/// if an IOException is thrown by one of the
		/// operations, the actual exception will be contained within
		/// </exception>
		public virtual Sharpen.Iterator<IDictionary<string, object>> Iterator(ICollection
			<string> columnNames)
		{
			return new Cursor.RowIterator(this, columnNames, MOVE_FORWARD);
		}

		/// <summary>Delete the current row.</summary>
		/// <remarks>Delete the current row.</remarks>
		/// <exception cref="System.InvalidOperationException">
		/// if the current row is not valid (at
		/// beginning or end of table), or already deleted.
		/// </exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void DeleteCurrentRow()
		{
			_table.DeleteRow(_rowState, _curPos.GetRowId());
		}

		/// <summary>Update the current row.</summary>
		/// <remarks>Update the current row.</remarks>
		/// <exception cref="System.InvalidOperationException">
		/// if the current row is not valid (at
		/// beginning or end of table), or deleted.
		/// </exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void UpdateCurrentRow(params object[] row)
		{
			_table.UpdateRow(_rowState, _curPos.GetRowId(), row);
		}

		/// <summary>Moves to the next row in the table and returns it.</summary>
		/// <remarks>Moves to the next row in the table and returns it.</remarks>
		/// <returns>
		/// The next row in this table (Column name -&gt; Column value), or
		/// <code>null</code>
		/// if no next row is found
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual IDictionary<string, object> GetNextRow()
		{
			return GetNextRow(null);
		}

		/// <summary>Moves to the next row in the table and returns it.</summary>
		/// <remarks>Moves to the next row in the table and returns it.</remarks>
		/// <param name="columnNames">Only column names in this collection will be returned</param>
		/// <returns>
		/// The next row in this table (Column name -&gt; Column value), or
		/// <code>null</code>
		/// if no next row is found
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual IDictionary<string, object> GetNextRow(ICollection<string> columnNames
			)
		{
			return GetAnotherRow(columnNames, MOVE_FORWARD);
		}

		/// <summary>Moves to the previous row in the table and returns it.</summary>
		/// <remarks>Moves to the previous row in the table and returns it.</remarks>
		/// <returns>
		/// The previous row in this table (Column name -&gt; Column value), or
		/// <code>null</code>
		/// if no previous row is found
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual IDictionary<string, object> GetPreviousRow()
		{
			return GetPreviousRow(null);
		}

		/// <summary>Moves to the previous row in the table and returns it.</summary>
		/// <remarks>Moves to the previous row in the table and returns it.</remarks>
		/// <param name="columnNames">Only column names in this collection will be returned</param>
		/// <returns>
		/// The previous row in this table (Column name -&gt; Column value), or
		/// <code>null</code>
		/// if no previous row is found
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual IDictionary<string, object> GetPreviousRow(ICollection<string> columnNames
			)
		{
			return GetAnotherRow(columnNames, MOVE_REVERSE);
		}

		/// <summary>
		/// Moves to another row in the table based on the given direction and
		/// returns it.
		/// </summary>
		/// <remarks>
		/// Moves to another row in the table based on the given direction and
		/// returns it.
		/// </remarks>
		/// <param name="columnNames">Only column names in this collection will be returned</param>
		/// <returns>
		/// another row in this table (Column name -&gt; Column value), where
		/// "next" may be backwards if moveForward is
		/// <code>false</code>
		/// , or
		/// <code>null</code>
		/// if there is not another row in the given direction.
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		private IDictionary<string, object> GetAnotherRow(ICollection<string> columnNames
			, bool moveForward)
		{
			if (MoveToAnotherRow(moveForward))
			{
				return GetCurrentRow(columnNames);
			}
			return null;
		}

		/// <summary>Moves to the next row as defined by this cursor.</summary>
		/// <remarks>Moves to the next row as defined by this cursor.</remarks>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if a valid next row was found,
		/// <code>false</code>
		/// otherwise
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual bool MoveToNextRow()
		{
			return MoveToAnotherRow(MOVE_FORWARD);
		}

		/// <summary>Moves to the previous row as defined by this cursor.</summary>
		/// <remarks>Moves to the previous row as defined by this cursor.</remarks>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if a valid previous row was found,
		/// <code>false</code>
		/// otherwise
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual bool MoveToPreviousRow()
		{
			return MoveToAnotherRow(MOVE_REVERSE);
		}

		/// <summary>Moves to another row in the given direction as defined by this cursor.</summary>
		/// <remarks>Moves to another row in the given direction as defined by this cursor.</remarks>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if another valid row was found in the given
		/// direction,
		/// <code>false</code>
		/// otherwise
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		private bool MoveToAnotherRow(bool moveForward)
		{
			if (_curPos.Equals(GetDirHandler(moveForward).GetEndPosition()))
			{
				// already at end, make sure nothing has changed
				return RecheckPosition(moveForward);
			}
			return MoveToAnotherRowImpl(moveForward);
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
		protected internal virtual void RestorePosition(Cursor.Position curPos)
		{
			RestorePosition(curPos, _curPos);
		}

		/// <summary>
		/// Restores a current and previous position for the cursor if the given
		/// positions are different from the current positions.
		/// </summary>
		/// <remarks>
		/// Restores a current and previous position for the cursor if the given
		/// positions are different from the current positions.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal void RestorePosition(Cursor.Position curPos, Cursor.Position prevPos
			)
		{
			if (!curPos.Equals(_curPos) || !prevPos.Equals(_prevPos))
			{
				RestorePositionImpl(curPos, prevPos);
			}
		}

		/// <summary>Restores a current and previous position for the cursor.</summary>
		/// <remarks>Restores a current and previous position for the cursor.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void RestorePositionImpl(Cursor.Position curPos, Cursor.Position
			 prevPos)
		{
			// make the current position previous, and the new position current
			_prevPos = _curPos;
			_curPos = curPos;
			_rowState.Reset();
		}

		/// <summary>
		/// Rechecks the current position if the underlying data structures have been
		/// modified.
		/// </summary>
		/// <remarks>
		/// Rechecks the current position if the underlying data structures have been
		/// modified.
		/// </remarks>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if the cursor ended up in a new position,
		/// <code>false</code>
		/// otherwise.
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		private bool RecheckPosition(bool moveForward)
		{
			if (IsUpToDate())
			{
				// nothing has changed
				return false;
			}
			// move the cursor back to the previous position
			RestorePosition(_prevPos);
			return MoveToAnotherRowImpl(moveForward);
		}

		/// <summary>
		/// Does the grunt work of moving the cursor to another position in the given
		/// direction.
		/// </summary>
		/// <remarks>
		/// Does the grunt work of moving the cursor to another position in the given
		/// direction.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private bool MoveToAnotherRowImpl(bool moveForward)
		{
			_rowState.Reset();
			_prevPos = _curPos;
			_curPos = FindAnotherPosition(_rowState, _curPos, moveForward);
			Table.PositionAtRowHeader(_rowState, _curPos.GetRowId());
			return (!_curPos.Equals(GetDirHandler(moveForward).GetEndPosition()));
		}

		/// <summary>
		/// Moves to the first row (as defined by the cursor) where the given column
		/// has the given value.
		/// </summary>
		/// <remarks>
		/// Moves to the first row (as defined by the cursor) where the given column
		/// has the given value.  This may be more efficient on some cursors than
		/// others.  If a match is not found (or an exception is thrown), the cursor
		/// is restored to its previous state.
		/// </remarks>
		/// <param name="columnPattern">
		/// column from the table for this cursor which is being
		/// matched by the valuePattern
		/// </param>
		/// <param name="valuePattern">
		/// value which is equal to the corresponding value in
		/// the matched row
		/// </param>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if a valid row was found with the given value,
		/// <code>false</code>
		/// if no row was found
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual bool FindRow(Column columnPattern, object valuePattern)
		{
			Cursor.Position curPos = _curPos;
			Cursor.Position prevPos = _prevPos;
			bool found = false;
			try
			{
				found = FindRowImpl(columnPattern, valuePattern);
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
					catch (IOException)
					{
					}
				}
			}
		}

		/// <summary>
		/// Moves to the first row (as defined by the cursor) where the given columns
		/// have the given values.
		/// </summary>
		/// <remarks>
		/// Moves to the first row (as defined by the cursor) where the given columns
		/// have the given values.  This may be more efficient on some cursors than
		/// others.  If a match is not found (or an exception is thrown), the cursor
		/// is restored to its previous state.
		/// </remarks>
		/// <param name="rowPattern">
		/// column names and values which must be equal to the
		/// corresponding values in the matched row
		/// </param>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if a valid row was found with the given values,
		/// <code>false</code>
		/// if no row was found
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual bool FindRow(IDictionary<string, object> rowPattern)
		{
			Cursor.Position curPos = _curPos;
			Cursor.Position prevPos = _prevPos;
			bool found = false;
			try
			{
				found = FindRowImpl(rowPattern);
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
		/// Returns
		/// <code>true</code>
		/// if the current row matches the given pattern.
		/// </summary>
		/// <param name="columnPattern">
		/// column from the table for this cursor which is being
		/// matched by the valuePattern
		/// </param>
		/// <param name="valuePattern">
		/// value which is tested for equality with the
		/// corresponding value in the current row
		/// </param>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual bool CurrentRowMatches(Column columnPattern, object valuePattern)
		{
			return _columnMatcher.Matches(GetTable(), columnPattern.GetName(), valuePattern, 
				GetCurrentRowValue(columnPattern));
		}

		/// <summary>
		/// Returns
		/// <code>true</code>
		/// if the current row matches the given pattern.
		/// </summary>
		/// <param name="rowPattern">
		/// column names and values which must be equal to the
		/// corresponding values in the current row
		/// </param>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual bool CurrentRowMatches(IDictionary<string, object> rowPattern)
		{
			IDictionary<string, object> row = GetCurrentRow(rowPattern.Keys);
			if (rowPattern.Count != row.Count)
			{
				return false;
			}
			foreach (KeyValuePair<string, object> e in row.EntrySet())
			{
				string columnName = e.Key;
				if (!_columnMatcher.Matches(GetTable(), columnName, rowPattern.Get(columnName), e
					.Value))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Moves to the first row (as defined by the cursor) where the given column
		/// has the given value.
		/// </summary>
		/// <remarks>
		/// Moves to the first row (as defined by the cursor) where the given column
		/// has the given value.  Caller manages save/restore on failure.
		/// <p>
		/// Default implementation scans the table from beginning to end.
		/// </remarks>
		/// <param name="columnPattern">
		/// column from the table for this cursor which is being
		/// matched by the valuePattern
		/// </param>
		/// <param name="valuePattern">
		/// value which is equal to the corresponding value in
		/// the matched row
		/// </param>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if a valid row was found with the given value,
		/// <code>false</code>
		/// if no row was found
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual bool FindRowImpl(Column columnPattern, object valuePattern
			)
		{
			BeforeFirst();
			while (MoveToNextRow())
			{
				if (CurrentRowMatches(columnPattern, valuePattern))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Moves to the first row (as defined by the cursor) where the given columns
		/// have the given values.
		/// </summary>
		/// <remarks>
		/// Moves to the first row (as defined by the cursor) where the given columns
		/// have the given values.  Caller manages save/restore on failure.
		/// <p>
		/// Default implementation scans the table from beginning to end.
		/// </remarks>
		/// <param name="rowPattern">
		/// column names and values which must be equal to the
		/// corresponding values in the matched row
		/// </param>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if a valid row was found with the given values,
		/// <code>false</code>
		/// if no row was found
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual bool FindRowImpl(IDictionary<string, object> rowPattern
			)
		{
			BeforeFirst();
			while (MoveToNextRow())
			{
				if (CurrentRowMatches(rowPattern))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Moves forward as many rows as possible up to the given number of rows.</summary>
		/// <remarks>Moves forward as many rows as possible up to the given number of rows.</remarks>
		/// <returns>the number of rows moved.</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual int MoveNextRows(int numRows)
		{
			return MoveSomeRows(numRows, MOVE_FORWARD);
		}

		/// <summary>Moves backward as many rows as possible up to the given number of rows.</summary>
		/// <remarks>Moves backward as many rows as possible up to the given number of rows.</remarks>
		/// <returns>the number of rows moved.</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual int MovePreviousRows(int numRows)
		{
			return MoveSomeRows(numRows, MOVE_REVERSE);
		}

		/// <summary>
		/// Moves as many rows as possible in the given direction up to the given
		/// number of rows.
		/// </summary>
		/// <remarks>
		/// Moves as many rows as possible in the given direction up to the given
		/// number of rows.
		/// </remarks>
		/// <returns>the number of rows moved.</returns>
		/// <exception cref="System.IO.IOException"></exception>
		private int MoveSomeRows(int numRows, bool moveForward)
		{
			int numMovedRows = 0;
			while ((numMovedRows < numRows) && MoveToAnotherRow(moveForward))
			{
				++numMovedRows;
			}
			return numMovedRows;
		}

		/// <summary>Returns the current row in this cursor (Column name -&gt; Column value).
		/// 	</summary>
		/// <remarks>Returns the current row in this cursor (Column name -&gt; Column value).
		/// 	</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual IDictionary<string, object> GetCurrentRow()
		{
			return GetCurrentRow(null);
		}

		/// <summary>Returns the current row in this cursor (Column name -&gt; Column value).
		/// 	</summary>
		/// <remarks>Returns the current row in this cursor (Column name -&gt; Column value).
		/// 	</remarks>
		/// <param name="columnNames">Only column names in this collection will be returned</param>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual IDictionary<string, object> GetCurrentRow(ICollection<string> columnNames
			)
		{
			return _table.GetRow(_rowState, _curPos.GetRowId(), columnNames);
		}

		/// <summary>Returns the given column from the current row.</summary>
		/// <remarks>Returns the given column from the current row.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual object GetCurrentRowValue(Column column)
		{
			return _table.GetRowValue(_rowState, _curPos.GetRowId(), column);
		}

		/// <summary>Updates a single value in the current row.</summary>
		/// <remarks>Updates a single value in the current row.</remarks>
		/// <exception cref="System.InvalidOperationException">
		/// if the current row is not valid (at
		/// beginning or end of table), or deleted.
		/// </exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void SetCurrentRowValue(Column column, object value)
		{
			object[] row = new object[_table.GetColumnCount()];
			Arrays.Fill(row, Column.KEEP_VALUE);
			row[column.GetColumnIndex()] = value;
			_table.UpdateRow(_rowState, _curPos.GetRowId(), row);
		}

		/// <summary>
		/// Returns
		/// <code>true</code>
		/// if this cursor is up-to-date with respect to the
		/// relevant table and related table objects,
		/// <code>false</code>
		/// otherwise.
		/// </summary>
		protected internal virtual bool IsUpToDate()
		{
			return _rowState.IsUpToDate();
		}

		public override string ToString()
		{
			return GetType().Name + " CurPosition " + _curPos + ", PrevPosition " + _prevPos;
		}

		/// <summary>
		/// Finds the next non-deleted row after the given row (as defined by this
		/// cursor) and returns the id of the row, where "next" may be backwards if
		/// moveForward is
		/// <code>false</code>
		/// .  If there are no more rows, the returned
		/// rowId should equal the value returned by
		/// <see cref="GetLastPosition()">GetLastPosition()</see>
		/// if
		/// moving forward and
		/// <see cref="GetFirstPosition()">GetFirstPosition()</see>
		/// if moving backward.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal abstract Cursor.Position FindAnotherPosition(Table.RowState rowState
			, Cursor.Position curPos, bool moveForward);

		/// <summary>Returns the DirHandler for the given movement direction.</summary>
		/// <remarks>Returns the DirHandler for the given movement direction.</remarks>
		protected internal abstract Cursor.DirHandler GetDirHandler(bool moveForward);

		/// <summary>Base implementation of iterator for this cursor, modifiable.</summary>
		/// <remarks>Base implementation of iterator for this cursor, modifiable.</remarks>
		protected internal abstract class BaseIterator : Iterator<IDictionary<string, object
			>>
		{
			protected internal readonly ICollection<string> _columnNames;

			protected internal bool? _hasNext;

			protected internal bool _validRow;

			protected internal BaseIterator(Cursor _enclosing, ICollection<string> columnNames
				)
			{
				this._enclosing = _enclosing;
				this._columnNames = columnNames;
			}

			public override bool HasNext()
			{
				if (this._hasNext == null)
				{
					try
					{
						this._hasNext = this.FindNext();
						this._validRow = this._hasNext.Value;
					}
					catch (IOException e)
					{
						throw new InvalidOperationException(e.ToString());
					}
				}
				return this._hasNext.Value;
			}

			public override IDictionary<string, object> Next()
			{
				if (!this.HasNext())
				{
					throw new NoSuchElementException();
				}
				try
				{
					IDictionary<string, object> rtn = this._enclosing.GetCurrentRow(this._columnNames
						);
					this._hasNext = null;
					return rtn;
				}
				catch (IOException e)
				{
					throw new InvalidOperationException(e.ToString());
				}
			}

			public override void Remove()
			{
				if (this._validRow)
				{
					try
					{
						this._enclosing.DeleteCurrentRow();
						this._validRow = false;
					}
					catch (IOException e)
					{
						throw new InvalidOperationException(e.ToString());
					}
				}
				else
				{
					throw new InvalidOperationException("Not at valid row");
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal abstract bool FindNext();

			private readonly Cursor _enclosing;
		}

		/// <summary>Row iterator for this cursor, modifiable.</summary>
		/// <remarks>Row iterator for this cursor, modifiable.</remarks>
		private sealed class RowIterator : Cursor.BaseIterator
		{
			private readonly bool _moveForward;

			public RowIterator(Cursor _enclosing, ICollection<string> columnNames, bool moveForward
				) : base(_enclosing, columnNames)
			{
				this._enclosing = _enclosing;
				this._moveForward = moveForward;
				this._enclosing.Reset(this._moveForward);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override bool FindNext()
			{
				return this._enclosing.MoveToAnotherRow(this._moveForward);
			}

			private readonly Cursor _enclosing;
		}

		/// <summary>Handles moving the cursor in a given direction.</summary>
		/// <remarks>
		/// Handles moving the cursor in a given direction.  Separates cursor
		/// logic from value storage.
		/// </remarks>
		protected internal abstract class DirHandler
		{
			public abstract Cursor.Position GetBeginningPosition();

			public abstract Cursor.Position GetEndPosition();

			internal DirHandler(Cursor _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly Cursor _enclosing;
		}

		/// <summary>Simple un-indexed cursor.</summary>
		/// <remarks>Simple un-indexed cursor.</remarks>
		private sealed class TableScanCursor : Cursor
		{
			/// <summary>ScanDirHandler for forward traversal</summary>
			private readonly Cursor.TableScanCursor.ScanDirHandler _forwardDirHandler;

			/// <summary>ScanDirHandler for backward traversal</summary>
			private readonly Cursor.TableScanCursor.ScanDirHandler _reverseDirHandler;

			/// <summary>Cursor over the pages that this table owns</summary>
			private readonly UsageMap.PageCursor _ownedPagesCursor;

			public TableScanCursor(Table table) : base(new Cursor.ID(table, null), table, FIRST_SCAN_POSITION
				, LAST_SCAN_POSITION)
			{
				_forwardDirHandler = new Cursor.TableScanCursor.ForwardScanDirHandler(this);
				_reverseDirHandler = new Cursor.TableScanCursor.ReverseScanDirHandler(this);
				_ownedPagesCursor = table.GetOwnedPagesCursor();
			}

			protected internal override Cursor.DirHandler GetDirHandler(bool moveForward)
			{
				return (moveForward ? _forwardDirHandler : _reverseDirHandler);
			}

			protected internal override bool IsUpToDate()
			{
				return (base.IsUpToDate() && _ownedPagesCursor.IsUpToDate());
			}

			protected internal override void Reset(bool moveForward)
			{
				_ownedPagesCursor.Reset(moveForward);
				base.Reset(moveForward);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void RestorePositionImpl(Cursor.Position curPos, Cursor.Position
				 prevPos)
			{
				if (!(curPos is Cursor.ScanPosition) || !(prevPos is Cursor.ScanPosition))
				{
					throw new ArgumentException("Restored positions must be scan positions");
				}
				_ownedPagesCursor.RestorePosition(curPos.GetRowId().GetPageNumber(), prevPos.GetRowId
					().GetPageNumber());
				base.RestorePositionImpl(curPos, prevPos);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override Cursor.Position FindAnotherPosition(Table.RowState rowState
				, Cursor.Position curPos, bool moveForward)
			{
				Cursor.TableScanCursor.ScanDirHandler handler = ((Cursor.TableScanCursor.ScanDirHandler
					)GetDirHandler(moveForward));
				// figure out how many rows are left on this page so we can find the
				// next row
				RowId curRowId = curPos.GetRowId();
				Table.PositionAtRowHeader(rowState, curRowId);
				int currentRowNumber = curRowId.GetRowNumber();
				// loop until we find the next valid row or run out of pages
				while (true)
				{
					currentRowNumber = handler.GetAnotherRowNumber(currentRowNumber);
					curRowId = new RowId(curRowId.GetPageNumber(), currentRowNumber);
					Table.PositionAtRowHeader(rowState, curRowId);
					if (!rowState.IsValid())
					{
						// load next page
						curRowId = new RowId(handler.GetAnotherPageNumber(), RowId.INVALID_ROW_NUMBER);
						Table.PositionAtRowHeader(rowState, curRowId);
						if (!rowState.IsHeaderPageNumberValid())
						{
							//No more owned pages.  No more rows.
							return handler.GetEndPosition();
						}
						// update row count and initial row number
						currentRowNumber = handler.GetInitialRowNumber(rowState.GetRowsOnHeaderPage());
					}
					else
					{
						if (!rowState.IsDeleted())
						{
							// we found a valid, non-deleted row, return it
							return new Cursor.ScanPosition(curRowId);
						}
					}
				}
			}

			/// <summary>Handles moving the table scan cursor in a given direction.</summary>
			/// <remarks>
			/// Handles moving the table scan cursor in a given direction.  Separates
			/// cursor logic from value storage.
			/// </remarks>
			private abstract class ScanDirHandler : Cursor.DirHandler
			{
				public abstract int GetAnotherRowNumber(int curRowNumber);

				public abstract int GetAnotherPageNumber();

				public abstract int GetInitialRowNumber(int rowsOnPage);

				internal ScanDirHandler(TableScanCursor _enclosing) : base(_enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly TableScanCursor _enclosing;
			}

			/// <summary>Handles moving the table scan cursor forward.</summary>
			/// <remarks>Handles moving the table scan cursor forward.</remarks>
			private sealed class ForwardScanDirHandler : Cursor.TableScanCursor.ScanDirHandler
			{
				public override Cursor.Position GetBeginningPosition()
				{
					return this._enclosing.GetFirstPosition();
				}

				public override Cursor.Position GetEndPosition()
				{
					return this._enclosing.GetLastPosition();
				}

				public override int GetAnotherRowNumber(int curRowNumber)
				{
					return curRowNumber + 1;
				}

				public override int GetAnotherPageNumber()
				{
					return this._enclosing._ownedPagesCursor.GetNextPage();
				}

				public override int GetInitialRowNumber(int rowsOnPage)
				{
					return -1;
				}

				internal ForwardScanDirHandler(TableScanCursor _enclosing) : base(_enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly TableScanCursor _enclosing;
			}

			/// <summary>Handles moving the table scan cursor backward.</summary>
			/// <remarks>Handles moving the table scan cursor backward.</remarks>
			private sealed class ReverseScanDirHandler : Cursor.TableScanCursor.ScanDirHandler
			{
				public override Cursor.Position GetBeginningPosition()
				{
					return this._enclosing.GetLastPosition();
				}

				public override Cursor.Position GetEndPosition()
				{
					return this._enclosing.GetFirstPosition();
				}

				public override int GetAnotherRowNumber(int curRowNumber)
				{
					return curRowNumber - 1;
				}

				public override int GetAnotherPageNumber()
				{
					return this._enclosing._ownedPagesCursor.GetPreviousPage();
				}

				public override int GetInitialRowNumber(int rowsOnPage)
				{
					return rowsOnPage;
				}

				internal ReverseScanDirHandler(TableScanCursor _enclosing) : base(_enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly TableScanCursor _enclosing;
			}
		}

		/// <summary>Identifier for a cursor.</summary>
		/// <remarks>
		/// Identifier for a cursor.  Will be equal to any other cursor of the same
		/// type for the same table.  Primarily used to check the validity of a
		/// Savepoint.
		/// </remarks>
		public sealed class ID
		{
			private readonly string _tableName;

			private readonly string _indexName;

			public ID(Table table, Index index)
			{
				_tableName = table.GetName();
				_indexName = ((index != null) ? index.GetName() : null);
			}

			public override int GetHashCode()
			{
				return _tableName.GetHashCode();
			}

			public override bool Equals(object o)
			{
				return ((this == o) || ((o != null) && (GetType() == o.GetType()) && ObjectUtils.
					Equals(_tableName, ((Cursor.ID)o)._tableName) && ObjectUtils.Equals(_indexName, 
					((Cursor.ID)o)._indexName)));
			}

			public override string ToString()
			{
				return GetType().Name + " " + _tableName + ":" + _indexName;
			}
		}

		/// <summary>Value object which represents a complete save state of the cursor.</summary>
		/// <remarks>Value object which represents a complete save state of the cursor.</remarks>
		public sealed class Savepoint
		{
			private readonly Cursor.ID _cursorId;

			private readonly Cursor.Position _curPos;

			private readonly Cursor.Position _prevPos;

			public Savepoint(Cursor.ID cursorId, Cursor.Position curPos, Cursor.Position prevPos
				)
			{
				_cursorId = cursorId;
				_curPos = curPos;
				_prevPos = prevPos;
			}

			public Cursor.ID GetCursorId()
			{
				return _cursorId;
			}

			public Cursor.Position GetCurrentPosition()
			{
				return _curPos;
			}

			public Cursor.Position GetPreviousPosition()
			{
				return _prevPos;
			}

			public override string ToString()
			{
				return GetType().Name + " " + _cursorId + " CurPosition " + _curPos + ", PrevPosition "
					 + _prevPos;
			}
		}

		/// <summary>Value object which maintains the current position of the cursor.</summary>
		/// <remarks>Value object which maintains the current position of the cursor.</remarks>
		public abstract class Position
		{
			public Position()
			{
			}

			public sealed override int GetHashCode()
			{
				return GetRowId().GetHashCode();
			}

			public sealed override bool Equals(object o)
			{
				return ((this == o) || ((o != null) && (GetType() == o.GetType()) && EqualsImpl(o
					)));
			}

			/// <summary>Returns the unique RowId of the position of the cursor.</summary>
			/// <remarks>Returns the unique RowId of the position of the cursor.</remarks>
			public abstract RowId GetRowId();

			/// <summary>
			/// Returns
			/// <code>true</code>
			/// if the subclass specific info in a Position is
			/// equal,
			/// <code>false</code>
			/// otherwise.
			/// </summary>
			/// <param name="o">
			/// object being tested for equality, guaranteed to be the same
			/// class as this object
			/// </param>
			protected internal abstract bool EqualsImpl(object o);
		}

		/// <summary>Value object which maintains the current position of a TableScanCursor.</summary>
		/// <remarks>Value object which maintains the current position of a TableScanCursor.</remarks>
		public sealed class ScanPosition : Cursor.Position
		{
			private readonly RowId _rowId;

			public ScanPosition(RowId rowId)
			{
				_rowId = rowId;
			}

			public override RowId GetRowId()
			{
				return _rowId;
			}

			protected internal override bool EqualsImpl(object o)
			{
				return GetRowId().Equals(((Cursor.ScanPosition)o).GetRowId());
			}

			public override string ToString()
			{
				return "RowId = " + GetRowId();
			}
		}
	}
}
