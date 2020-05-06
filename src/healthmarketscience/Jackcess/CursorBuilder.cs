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
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Builder style class for constructing a Cursor.</summary>
	/// <remarks>
	/// Builder style class for constructing a Cursor.  By default, a cursor is
	/// created at the beginning of the table, and any start/end rows are
	/// inclusive.
	/// </remarks>
	/// <author>James Ahlborn</author>
	public class CursorBuilder
	{
		/// <summary>the table which the cursor will traverse</summary>
		private readonly Table _table;

		/// <summary>optional index to use in traversal</summary>
		private Index _index;

		/// <summary>optional start row for an index cursor</summary>
		private object[] _startRow;

		/// <summary>whether or not start row for an index cursor is inclusive</summary>
		private bool _startRowInclusive = true;

		/// <summary>optional end row for an index cursor</summary>
		private object[] _endRow;

		/// <summary>whether or not end row for an index cursor is inclusive</summary>
		private bool _endRowInclusive = true;

		/// <summary>whether to start at beginning or end of cursor</summary>
		private bool _beforeFirst = true;

		/// <summary>optional save point to restore to the cursor</summary>
		private Cursor.Savepoint _savepoint;

		/// <summary>ColumnMatcher to be used when matching column values</summary>
		private ColumnMatcher _columnMatcher;

		public CursorBuilder(Table table)
		{
			_table = table;
		}

		/// <summary>
		/// Sets the cursor so that it will start at the beginning (unless a
		/// savepoint is given).
		/// </summary>
		/// <remarks>
		/// Sets the cursor so that it will start at the beginning (unless a
		/// savepoint is given).
		/// </remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder BeforeFirst()
		{
			_beforeFirst = true;
			return this;
		}

		/// <summary>
		/// Sets the cursor so that it will start at the end (unless a savepoint is
		/// given).
		/// </summary>
		/// <remarks>
		/// Sets the cursor so that it will start at the end (unless a savepoint is
		/// given).
		/// </remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder AfterLast()
		{
			_beforeFirst = false;
			return this;
		}

		/// <summary>Sets a savepoint to restore for the initial position of the cursor.</summary>
		/// <remarks>Sets a savepoint to restore for the initial position of the cursor.</remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder RestoreSavepoint(Cursor.Savepoint
			 savepoint)
		{
			_savepoint = savepoint;
			return this;
		}

		/// <summary>Sets an index to use for the cursor.</summary>
		/// <remarks>Sets an index to use for the cursor.</remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetIndex(Index index)
		{
			_index = index;
			return this;
		}

		/// <summary>
		/// Sets an index to use for the cursor by searching the table for an index
		/// with the given name.
		/// </summary>
		/// <remarks>
		/// Sets an index to use for the cursor by searching the table for an index
		/// with the given name.
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		/// if no index can be found on the table
		/// with the given name
		/// </exception>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetIndexByName(string indexName
			)
		{
			return SetIndex(_table.GetIndex(indexName));
		}

		/// <summary>
		/// Sets an index to use for the cursor by searching the table for an index
		/// with exactly the given columns.
		/// </summary>
		/// <remarks>
		/// Sets an index to use for the cursor by searching the table for an index
		/// with exactly the given columns.
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		/// if no index can be found on the table
		/// with the given columns
		/// </exception>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetIndexByColumnNames(params 
			string[] columnNames)
		{
			return SetIndexByColumns(Arrays.AsList(columnNames));
		}

		/// <summary>
		/// Sets an index to use for the cursor by searching the table for an index
		/// with exactly the given columns.
		/// </summary>
		/// <remarks>
		/// Sets an index to use for the cursor by searching the table for an index
		/// with exactly the given columns.
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		/// if no index can be found on the table
		/// with the given columns
		/// </exception>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetIndexByColumns(params 
			Column[] columns)
		{
			IList<string> colNames = new AList<string>();
			foreach (Column col in columns)
			{
				colNames.AddItem(col.GetName());
			}
			return SetIndexByColumns(colNames);
		}

		/// <summary>Searches for an index with the given column names.</summary>
		/// <remarks>Searches for an index with the given column names.</remarks>
		private HealthMarketScience.Jackcess.CursorBuilder SetIndexByColumns(IList<string
			> searchColumns)
		{
			bool found = false;
			foreach (Index index in _table.GetIndexes())
			{
				ICollection<IndexData.ColumnDescriptor> indexColumns = index.GetColumns();
				if (indexColumns.Count != searchColumns.Count)
				{
					continue;
				}
				Iterator<string> sIter = searchColumns.Iterator();
				Iterator<IndexData.ColumnDescriptor> iIter = indexColumns.Iterator();
				bool matches = true;
				while (sIter.HasNext())
				{
					string sColName = sIter.Next();
					string iColName = iIter.Next().GetName();
					if ((sColName != iColName) && ((sColName == null) || !Sharpen.Runtime.EqualsIgnoreCase
						(sColName, iColName)))
					{
						matches = false;
						break;
					}
				}
				if (matches)
				{
					_index = index;
					found = true;
					break;
				}
			}
			if (!found)
			{
				throw new ArgumentException("Index with columns " + searchColumns + " does not exist in table "
					 + _table);
			}
			return this;
		}

		/// <summary>Sets the starting and ending row for a range based index cursor.</summary>
		/// <remarks>
		/// Sets the starting and ending row for a range based index cursor.
		/// <p>
		/// A valid index must be specified before calling this method.
		/// </remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetSpecificRow(object[]
			 specificRow)
		{
			SetStartRow(specificRow);
			SetEndRow(specificRow);
			return this;
		}

		/// <summary>
		/// Sets the starting and ending row for a range based index cursor to the
		/// given entry (where the given values correspond to the index's columns).
		/// </summary>
		/// <remarks>
		/// Sets the starting and ending row for a range based index cursor to the
		/// given entry (where the given values correspond to the index's columns).
		/// <p>
		/// A valid index must be specified before calling this method.
		/// </remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetSpecificEntry(params 
			object[] specificEntry)
		{
			if (specificEntry != null)
			{
				SetSpecificRow(_index.ConstructIndexRowFromEntry(specificEntry));
			}
			return this;
		}

		/// <summary>Sets the starting row for a range based index cursor.</summary>
		/// <remarks>
		/// Sets the starting row for a range based index cursor.
		/// <p>
		/// A valid index must be specified before calling this method.
		/// </remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetStartRow(object[] startRow
			)
		{
			_startRow = startRow;
			return this;
		}

		/// <summary>
		/// Sets the starting row for a range based index cursor to the given entry
		/// (where the given values correspond to the index's columns).
		/// </summary>
		/// <remarks>
		/// Sets the starting row for a range based index cursor to the given entry
		/// (where the given values correspond to the index's columns).
		/// <p>
		/// A valid index must be specified before calling this method.
		/// </remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetStartEntry(params object
			[] startEntry)
		{
			if (startEntry != null)
			{
				SetStartRow(_index.ConstructIndexRowFromEntry(startEntry));
			}
			return this;
		}

		/// <summary>
		/// Sets whether the starting row for a range based index cursor is inclusive
		/// or exclusive.
		/// </summary>
		/// <remarks>
		/// Sets whether the starting row for a range based index cursor is inclusive
		/// or exclusive.
		/// </remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetStartRowInclusive(bool
			 inclusive)
		{
			_startRowInclusive = inclusive;
			return this;
		}

		/// <summary>Sets the ending row for a range based index cursor.</summary>
		/// <remarks>
		/// Sets the ending row for a range based index cursor.
		/// <p>
		/// A valid index must be specified before calling this method.
		/// </remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetEndRow(object[] endRow
			)
		{
			_endRow = endRow;
			return this;
		}

		/// <summary>
		/// Sets the ending row for a range based index cursor to the given entry
		/// (where the given values correspond to the index's columns).
		/// </summary>
		/// <remarks>
		/// Sets the ending row for a range based index cursor to the given entry
		/// (where the given values correspond to the index's columns).
		/// <p>
		/// A valid index must be specified before calling this method.
		/// </remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetEndEntry(params object
			[] endEntry)
		{
			if (endEntry != null)
			{
				SetEndRow(_index.ConstructIndexRowFromEntry(endEntry));
			}
			return this;
		}

		/// <summary>
		/// Sets whether the ending row for a range based index cursor is inclusive
		/// or exclusive.
		/// </summary>
		/// <remarks>
		/// Sets whether the ending row for a range based index cursor is inclusive
		/// or exclusive.
		/// </remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetEndRowInclusive(bool
			 inclusive)
		{
			_endRowInclusive = inclusive;
			return this;
		}

		/// <summary>Sets the ColumnMatcher to use for matching row patterns.</summary>
		/// <remarks>Sets the ColumnMatcher to use for matching row patterns.</remarks>
		public virtual HealthMarketScience.Jackcess.CursorBuilder SetColumnMatcher(ColumnMatcher
			 columnMatcher)
		{
			_columnMatcher = columnMatcher;
			return this;
		}

		/// <summary>
		/// Returns a new cursor for the table, constructed to the given
		/// specifications.
		/// </summary>
		/// <remarks>
		/// Returns a new cursor for the table, constructed to the given
		/// specifications.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual Cursor ToCursor()
		{
			Cursor cursor = null;
			if (_index == null)
			{
				cursor = Cursor.CreateCursor(_table);
			}
			else
			{
				cursor = Cursor.CreateIndexCursor(_table, _index, _startRow, _startRowInclusive, 
					_endRow, _endRowInclusive);
			}
			cursor.SetColumnMatcher(_columnMatcher);
			if (_savepoint == null)
			{
				if (!_beforeFirst)
				{
					cursor.AfterLast();
				}
			}
			else
			{
				cursor.RestoreSavepoint(_savepoint);
			}
			return cursor;
		}

		/// <summary>
		/// Returns a new index cursor for the table, constructed to the given
		/// specifications.
		/// </summary>
		/// <remarks>
		/// Returns a new index cursor for the table, constructed to the given
		/// specifications.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual IndexCursor ToIndexCursor()
		{
			return (IndexCursor)ToCursor();
		}
	}
}
