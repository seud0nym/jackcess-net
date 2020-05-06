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
using System.Text;
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Access table (logical) index.</summary>
	/// <remarks>
	/// Access table (logical) index.  Logical indexes are backed for IndexData,
	/// where one or more logical indexes could be backed by the same data.
	/// </remarks>
	/// <author>Tim McCune</author>
	public class Index : IComparable<HealthMarketScience.Jackcess.Index>
	{
		/// <summary>index type for primary key indexes</summary>
		internal const byte PRIMARY_KEY_INDEX_TYPE = unchecked((byte)1);

		/// <summary>index type for foreign key indexes</summary>
		internal const byte FOREIGN_KEY_INDEX_TYPE = unchecked((byte)2);

		/// <summary>flag for indicating that updates should cascade in a foreign key index</summary>
		private const byte CASCADE_UPDATES_FLAG = unchecked((byte)1);

		/// <summary>flag for indicating that deletes should cascade in a foreign key index</summary>
		private const byte CASCADE_DELETES_FLAG = unchecked((byte)1);

		/// <summary>index table type for the "primary" table in a foreign key index</summary>
		private const byte PRIMARY_TABLE_TYPE = unchecked((byte)1);

		/// <summary>indicate an invalid index number for foreign key field</summary>
		private const int INVALID_INDEX_NUMBER = -1;

		/// <summary>
		/// the actual data backing this index (more than one index may be backed by
		/// the same data
		/// </summary>
		private readonly IndexData _data;

		/// <summary>0-based index number</summary>
		private readonly int _indexNumber;

		/// <summary>the type of the index</summary>
		private readonly byte _indexType;

		/// <summary>Index name</summary>
		private string _name;

		/// <summary>foreign key reference info, if any</summary>
		private readonly Index.ForeignKeyReference _reference;

		/// <exception cref="System.IO.IOException"></exception>
		protected internal Index(ByteBuffer tableBuffer, IList<IndexData> indexDatas, JetFormat
			 format)
		{
			ByteUtil.Forward(tableBuffer, format.SKIP_BEFORE_INDEX_SLOT);
			//Forward past Unknown
			_indexNumber = tableBuffer.GetInt();
			int indexDataNumber = tableBuffer.GetInt();
			// read foreign key reference info
			byte relIndexType = tableBuffer.Get();
			int relIndexNumber = tableBuffer.GetInt();
			int relTablePageNumber = tableBuffer.GetInt();
			byte cascadeUpdatesFlag = tableBuffer.Get();
			byte cascadeDeletesFlag = tableBuffer.Get();
			_indexType = tableBuffer.Get();
			if ((_indexType == FOREIGN_KEY_INDEX_TYPE) && (relIndexNumber != INVALID_INDEX_NUMBER
				))
			{
				_reference = new Index.ForeignKeyReference(relIndexType, relIndexNumber, relTablePageNumber
					, (cascadeUpdatesFlag == CASCADE_UPDATES_FLAG), (cascadeDeletesFlag == CASCADE_DELETES_FLAG
					));
			}
			else
			{
				_reference = null;
			}
			ByteUtil.Forward(tableBuffer, format.SKIP_AFTER_INDEX_SLOT);
			//Skip past Unknown
			_data = indexDatas[indexDataNumber];
			_data.AddIndex(this);
		}

		public virtual IndexData GetIndexData()
		{
			return _data;
		}

		public virtual Table GetTable()
		{
			return GetIndexData().GetTable();
		}

		public virtual JetFormat GetFormat()
		{
			return GetTable().GetFormat();
		}

		public virtual PageChannel GetPageChannel()
		{
			return GetTable().GetPageChannel();
		}

		public virtual int GetIndexNumber()
		{
			return _indexNumber;
		}

		public virtual byte GetIndexFlags()
		{
			return GetIndexData().GetIndexFlags();
		}

		public virtual int GetUniqueEntryCount()
		{
			return GetIndexData().GetUniqueEntryCount();
		}

		public virtual int GetUniqueEntryCountOffset()
		{
			return GetIndexData().GetUniqueEntryCountOffset();
		}

		public virtual string GetName()
		{
			return _name;
		}

		public virtual void SetName(string name)
		{
			_name = name;
		}

		public virtual bool IsPrimaryKey()
		{
			return _indexType == PRIMARY_KEY_INDEX_TYPE;
		}

		public virtual bool IsForeignKey()
		{
			return _indexType == FOREIGN_KEY_INDEX_TYPE;
		}

		public virtual Index.ForeignKeyReference GetReference()
		{
			return _reference;
		}

		/// <returns>
		/// the Index referenced by this Index's ForeignKeyReference (if it
		/// has one), otherwise
		/// <code>null</code>
		/// .
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual HealthMarketScience.Jackcess.Index GetReferencedIndex()
		{
			if (_reference == null)
			{
				return null;
			}
			Table refTable = GetTable().GetDatabase().GetTable(_reference.GetOtherTablePageNumber
				());
			if (refTable == null)
			{
				throw new IOException("Reference to missing table " + _reference.GetOtherTablePageNumber
					());
			}
			HealthMarketScience.Jackcess.Index refIndex = null;
			int idxNumber = _reference.GetOtherIndexNumber();
			foreach (HealthMarketScience.Jackcess.Index idx in refTable.GetIndexes())
			{
				if (idx.GetIndexNumber() == idxNumber)
				{
					refIndex = idx;
					break;
				}
			}
			if (refIndex == null)
			{
				throw new IOException("Reference to missing index " + idxNumber + " on table " + 
					refTable.GetName());
			}
			// finally verify that we found the expected index (should reference this
			// index)
			Index.ForeignKeyReference otherRef = refIndex.GetReference();
			if ((otherRef == null) || (otherRef.GetOtherTablePageNumber() != GetTable().GetTableDefPageNumber
				()) || (otherRef.GetOtherIndexNumber() != _indexNumber))
			{
				throw new IOException("Found unexpected index " + refIndex.GetName() + " on table "
					 + refTable.GetName() + " with reference " + otherRef);
			}
			return refIndex;
		}

		/// <summary>
		/// Whether or not
		/// <code>null</code>
		/// values are actually recorded in the index.
		/// </summary>
		public virtual bool ShouldIgnoreNulls()
		{
			return GetIndexData().ShouldIgnoreNulls();
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
			return GetIndexData().IsUnique();
		}

		/// <summary>Returns the Columns for this index (unmodifiable)</summary>
		public virtual IList<IndexData.ColumnDescriptor> GetColumns()
		{
			return GetIndexData().GetColumns();
		}

		/// <summary>Whether or not the complete index state has been read.</summary>
		/// <remarks>Whether or not the complete index state has been read.</remarks>
		public virtual bool IsInitialized()
		{
			return GetIndexData().IsInitialized();
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
			GetIndexData().Initialize();
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
			GetIndexData().Update();
		}

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
			GetIndexData().AddRow(row, rowId);
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
			GetIndexData().DeleteRow(row, rowId);
		}

		/// <summary>Gets a new cursor for this index.</summary>
		/// <remarks>
		/// Gets a new cursor for this index.
		/// <p>
		/// Forces index initialization.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual IndexData.EntryCursor Cursor()
		{
			return Cursor(null, true, null, true);
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
		public virtual IndexData.EntryCursor Cursor(object[] startRow, bool startInclusive
			, object[] endRow, bool endInclusive)
		{
			return GetIndexData().GetCursor(startRow, startInclusive, endRow, endInclusive);
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
			return GetIndexData().ConstructIndexRowFromEntry(values);
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
			return GetIndexData().ConstructIndexRow(row);
		}

		public override string ToString()
		{
			StringBuilder rtn = new StringBuilder();
			rtn.Append("\tName: (").Append(GetTable().GetName()).Append(") ").Append(_name);
			rtn.Append("\n\tNumber: ").Append(_indexNumber);
			rtn.Append("\n\tIs Primary Key: ").Append(IsPrimaryKey());
			rtn.Append("\n\tIs Foreign Key: ").Append(IsForeignKey());
			if (_reference != null)
			{
				rtn.Append("\n\tForeignKeyReference: ").Append(_reference);
			}
			rtn.Append(_data.ToString());
			rtn.Append("\n\n");
			return rtn.ToString();
		}

		public virtual int CompareTo(HealthMarketScience.Jackcess.Index other)
		{
			if (_indexNumber > other.GetIndexNumber())
			{
				return 1;
			}
			else
			{
				if (_indexNumber < other.GetIndexNumber())
				{
					return -1;
				}
				else
				{
					return 0;
				}
			}
		}

		/// <summary>Writes the logical index definitions into a table definition buffer.</summary>
		/// <remarks>Writes the logical index definitions into a table definition buffer.</remarks>
		/// <param name="buffer">Buffer to write to</param>
		/// <param name="indexes">List of IndexBuilders to write definitions for</param>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal static void WriteDefinitions(ByteBuffer buffer, IList<IndexBuilder
			> indexes, Encoding charset)
		{
			// write logical index information
			foreach (IndexBuilder idx in indexes)
			{
				buffer.PutInt(Table.MAGIC_TABLE_NUMBER);
				// seemingly constant magic value which matches the table def
				buffer.PutInt(idx.GetIndexNumber());
				// index num
				buffer.PutInt(idx.GetIndexDataNumber());
				// index data num
				buffer.Put(unchecked((byte)0));
				// related table type
				buffer.PutInt(INVALID_INDEX_NUMBER);
				// related index num
				buffer.PutInt(0);
				// related table definition page number
				buffer.Put(unchecked((byte)0));
				// cascade updates flag
				buffer.Put(unchecked((byte)0));
				// cascade deletes flag
				buffer.Put(idx.GetIndexType());
				// index type flags
				buffer.PutInt(0);
			}
			// unknown
			// write index names
			foreach (IndexBuilder idx_1 in indexes)
			{
				Table.WriteName(buffer, idx_1.GetName(), charset);
			}
		}

		/// <summary>
		/// Information about a foreign key reference defined in an index (when
		/// referential integrity should be enforced).
		/// </summary>
		/// <remarks>
		/// Information about a foreign key reference defined in an index (when
		/// referential integrity should be enforced).
		/// </remarks>
		public class ForeignKeyReference
		{
			private readonly byte _tableType;

			private readonly int _otherIndexNumber;

			private readonly int _otherTablePageNumber;

			private readonly bool _cascadeUpdates;

			private readonly bool _cascadeDeletes;

			public ForeignKeyReference(byte tableType, int otherIndexNumber, int otherTablePageNumber
				, bool cascadeUpdates, bool cascadeDeletes)
			{
				_tableType = tableType;
				_otherIndexNumber = otherIndexNumber;
				_otherTablePageNumber = otherTablePageNumber;
				_cascadeUpdates = cascadeUpdates;
				_cascadeDeletes = cascadeDeletes;
			}

			public virtual byte GetTableType()
			{
				return _tableType;
			}

			public virtual bool IsPrimaryTable()
			{
				return (GetTableType() == PRIMARY_TABLE_TYPE);
			}

			public virtual int GetOtherIndexNumber()
			{
				return _otherIndexNumber;
			}

			public virtual int GetOtherTablePageNumber()
			{
				return _otherTablePageNumber;
			}

			public virtual bool IsCascadeUpdates()
			{
				return _cascadeUpdates;
			}

			public virtual bool IsCascadeDeletes()
			{
				return _cascadeDeletes;
			}

			public override string ToString()
			{
				return new StringBuilder().Append("\n\t\tOther Index Number: ").Append(_otherIndexNumber
					).Append("\n\t\tOther Table Page Num: ").Append(_otherTablePageNumber).Append("\n\t\tIs Primary Table: "
					).Append(IsPrimaryTable()).Append("\n\t\tIs Cascade Updates: ").Append(IsCascadeUpdates
					()).Append("\n\t\tIs Cascade Deletes: ").Append(IsCascadeDeletes()).ToString();
			}
		}
	}
}
