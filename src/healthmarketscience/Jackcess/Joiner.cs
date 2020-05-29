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
using System.Collections.Generic;

namespace HealthMarketScience.Jackcess
{
    /// <summary>
    /// Utility for finding rows based on pre-defined, foreign-key table
    /// relationships.
    /// </summary>
    /// <remarks>
    /// Utility for finding rows based on pre-defined, foreign-key table
    /// relationships.
    /// </remarks>
    /// <author>James Ahlborn</author>
    public class Joiner
    {
        private readonly Index _fromIndex;

        private readonly IList<IndexData.ColumnDescriptor> _fromCols;

        private readonly IndexCursor _toCursor;

        private readonly object[] _entryValues;

        private Joiner(Index fromIndex, IndexCursor toCursor)
        {
            _fromIndex = fromIndex;
            _fromCols = _fromIndex.GetColumns();
            _entryValues = new object[_fromCols.Count];
            _toCursor = toCursor;
        }

        /// <summary>
        /// Creates a new Joiner based on the foreign-key relationship between the
        /// given "from"" table and the given "to"" table.
        /// </summary>
        /// <remarks>
        /// Creates a new Joiner based on the foreign-key relationship between the
        /// given "from"" table and the given "to"" table.
        /// </remarks>
        /// <param name="fromTable">the "from" side of the relationship</param>
        /// <param name="toTable">the "to" side of the relationship</param>
        /// <exception cref="System.ArgumentException">
        /// if there is no relationship between the
        /// given tables
        /// </exception>
        /// <exception cref="System.IO.IOException"></exception>
        public static HealthMarketScience.Jackcess.Joiner Create(Table fromTable, Table toTable
            )
        {
            return Create(fromTable.GetForeignKeyIndex(toTable));
        }

        /// <summary>
        /// Creates a new Joiner based on the given index which backs a foreign-key
        /// relationship.
        /// </summary>
        /// <remarks>
        /// Creates a new Joiner based on the given index which backs a foreign-key
        /// relationship.  The table of the given index will be the "from" table and
        /// the table on the other end of the relationship will be the "to" table.
        /// </remarks>
        /// <param name="fromIndex">the index backing one side of a foreign-key relationship</param>
        /// <exception cref="System.IO.IOException"></exception>
        public static HealthMarketScience.Jackcess.Joiner Create(Index fromIndex)
        {
            Index toIndex = fromIndex.GetReferencedIndex();
            IndexCursor toCursor = IndexCursor.CreateCursor(toIndex.GetTable(), toIndex);
            // text lookups are always case-insensitive
            toCursor.SetColumnMatcher(CaseInsensitiveColumnMatcher.INSTANCE);
            return new HealthMarketScience.Jackcess.Joiner(fromIndex, toCursor);
        }

        /// <summary>
        /// Creates a new Joiner that is the reverse of this Joiner (the "from" and
        /// "to" tables are swapped).
        /// </summary>
        /// <remarks>
        /// Creates a new Joiner that is the reverse of this Joiner (the "from" and
        /// "to" tables are swapped).
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual HealthMarketScience.Jackcess.Joiner CreateReverse()
        {
            return Create(GetToTable(), GetFromTable());
        }

        public virtual Table GetFromTable()
        {
            return GetFromIndex().GetTable();
        }

        public virtual Index GetFromIndex()
        {
            return _fromIndex;
        }

        public virtual Table GetToTable()
        {
            return GetToCursor().GetTable();
        }

        public virtual Index GetToIndex()
        {
            return GetToCursor().GetIndex();
        }

        public virtual IndexCursor GetToCursor()
        {
            return _toCursor;
        }

        /// <summary>
        /// Returns the first row in the "to" table based on the given columns in the
        /// "from" table if any,
        /// <code>null</code>
        /// if there is no matching row.
        /// </summary>
        /// <param name="fromRow">
        /// row from the "from" table (which must include the relevant
        /// columns for this join relationship)
        /// </param>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual IDictionary<string, object> FindFirstRow(IDictionary<string, object
            > fromRow)
        {
            return FindFirstRow(fromRow, null);
        }

        /// <summary>
        /// Returns selected columns from the first row in the "to" table based on
        /// the given columns in the "from" table if any,
        /// <code>null</code>
        /// if there is no
        /// matching row.
        /// </summary>
        /// <param name="fromRow">
        /// row from the "from" table (which must include the relevant
        /// columns for this join relationship)
        /// </param>
        /// <param name="columnNames">desired columns in the from table row</param>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual IDictionary<string, object> FindFirstRow(IDictionary<string, object
            > fromRow, ICollection<string> columnNames)
        {
            ToEntryValues(fromRow);
            return ((_toCursor.FindRowByEntry(_entryValues) ? _toCursor.GetCurrentRow(columnNames
                ) : null));
        }

        /// <summary>
        /// Returns an Iterator over all the rows in the "to" table based on the
        /// given columns in the "from" table.
        /// </summary>
        /// <remarks>
        /// Returns an Iterator over all the rows in the "to" table based on the
        /// given columns in the "from" table.
        /// </remarks>
        /// <param name="fromRow">
        /// row from the "from" table (which must include the relevant
        /// columns for this join relationship)
        /// </param>
        public virtual Iterator<IDictionary<string, object>> FindRows(IDictionary<string,
            object> fromRow)
        {
            return FindRows(fromRow, null);
        }

        /// <summary>
        /// Returns an Iterator with the selected columns over all the rows in the
        /// "to" table based on the given columns in the "from" table.
        /// </summary>
        /// <remarks>
        /// Returns an Iterator with the selected columns over all the rows in the
        /// "to" table based on the given columns in the "from" table.
        /// </remarks>
        /// <param name="fromRow">
        /// row from the "from" table (which must include the relevant
        /// columns for this join relationship)
        /// </param>
        /// <param name="columnNames">desired columns in the from table row</param>
        public virtual Iterator<IDictionary<string, object>> FindRows(IDictionary<string,
            object> fromRow, ICollection<string> columnNames)
        {
            ToEntryValues(fromRow);
            return _toCursor.EntryIterator(columnNames, _entryValues);
        }

        /// <summary>
        /// Returns an Iterable whose iterator() method returns the result of a call
        /// to
        /// <see cref="FindRows(System.Collections.Generic.IDictionary{K, V})">FindRows(System.Collections.Generic.IDictionary&lt;K, V&gt;)
        /// 	</see>
        /// </summary>
        /// <param name="fromRow">
        /// row from the "from" table (which must include the relevant
        /// columns for this join relationship)
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// if an IOException is thrown by one of the
        /// operations, the actual exception will be contained within
        /// </exception>
        public virtual Iterable<IDictionary<string, object>> FindRowsIterable(IDictionary
            <string, object> fromRow)
        {
            return FindRowsIterable(fromRow, null);
        }

        /// <summary>
        /// Returns an Iterable whose iterator() method returns the result of a call
        /// to
        /// <see cref="FindRows(System.Collections.Generic.IDictionary{K, V}, System.Collections.Generic.ICollection{E})
        /// 	">FindRows(System.Collections.Generic.IDictionary&lt;K, V&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
        /// 	</see>
        /// </summary>
        /// <param name="fromRow">
        /// row from the "from" table (which must include the relevant
        /// columns for this join relationship)
        /// </param>
        /// <param name="columnNames">desired columns in the from table row</param>
        /// <exception cref="System.InvalidOperationException">
        /// if an IOException is thrown by one of the
        /// operations, the actual exception will be contained within
        /// </exception>
        public virtual Iterable<IDictionary<string, object>> FindRowsIterable(IDictionary
            <string, object> fromRow, ICollection<string> columnNames)
        {
            return new _Iterable_203(this, fromRow, columnNames);
        }

        private sealed class _Iterable_203 : Iterable<IDictionary<string, object>>
        {
            public _Iterable_203(Joiner _enclosing, IDictionary<string, object> fromRow, ICollection
                <string> columnNames)
            {
                this._enclosing = _enclosing;
                this.fromRow = fromRow;
                this.columnNames = columnNames;
            }

            public override Iterator<IDictionary<string, object>> Iterator()
            {
                return this._enclosing.FindRows(fromRow, columnNames);
            }

            private readonly Joiner _enclosing;

            private readonly IDictionary<string, object> fromRow;

            private readonly ICollection<string> columnNames;
        }

        /// <summary>
        /// Fills in the _entryValues with the relevant info from the given "from"
        /// table row.
        /// </summary>
        /// <remarks>
        /// Fills in the _entryValues with the relevant info from the given "from"
        /// table row.
        /// </remarks>
        private void ToEntryValues(IDictionary<string, object> fromRow)
        {
            for (int i = 0; i < _entryValues.Length; ++i)
            {
                _entryValues[i] = fromRow.Get(_fromCols[i].GetName());
            }
        }
    }
}
