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
	/// <summary>Builder style class for constructing an Index.</summary>
	/// <remarks>Builder style class for constructing an Index.</remarks>
	/// <author>James Ahlborn</author>
	public class IndexBuilder
	{
		/// <summary>name typically used by MS Access for the primary key index</summary>
		public static readonly string PRIMARY_KEY_NAME = "PrimaryKey";

		/// <summary>name of the new index</summary>
		private string _name;

		/// <summary>the type of the index</summary>
		private byte _type;

		/// <summary>
		/// additional index flags (UNKNOWN_INDEX_FLAG always seems to be set in
		/// access 2000+)
		/// </summary>
		private byte _flags = IndexData.UNKNOWN_INDEX_FLAG;

		/// <summary>the names and orderings of the indexed columns</summary>
		private readonly IList<IndexBuilder.Column> _columns = new AList<IndexBuilder.Column
			>();

		private int _indexNumber;

		private int _indexDataNumber;

		private byte _umapRowNumber;

		private int _umapPageNumber;

		private int _rootPageNumber;

		public IndexBuilder(string name)
		{
			// used by table definition writing code
			_name = name;
		}

		public virtual string GetName()
		{
			return _name;
		}

		public virtual byte GetIndexType()
		{
			return _type;
		}

		public virtual byte GetFlags()
		{
			return _flags;
		}

		public virtual bool IsPrimaryKey()
		{
			return (GetIndexType() == Index.PRIMARY_KEY_INDEX_TYPE);
		}

		public virtual bool IsUnique()
		{
			return ((GetFlags() & IndexData.UNIQUE_INDEX_FLAG) != 0);
		}

		public virtual bool IsIgnoreNulls()
		{
			return ((GetFlags() & IndexData.IGNORE_NULLS_INDEX_FLAG) != 0);
		}

		public virtual IList<IndexBuilder.Column> GetColumns()
		{
			return _columns;
		}

		/// <summary>Sets the name of the index.</summary>
		/// <remarks>Sets the name of the index.</remarks>
		public virtual HealthMarketScience.Jackcess.IndexBuilder SetName(string name)
		{
			_name = name;
			return this;
		}

		/// <summary>Adds the columns with ASCENDING ordering to the index.</summary>
		/// <remarks>Adds the columns with ASCENDING ordering to the index.</remarks>
		public virtual HealthMarketScience.Jackcess.IndexBuilder AddColumns(params string
			[] names)
		{
			return AddColumns(true, names);
		}

		/// <summary>Adds the columns with the given ordering to the index.</summary>
		/// <remarks>Adds the columns with the given ordering to the index.</remarks>
		public virtual HealthMarketScience.Jackcess.IndexBuilder AddColumns(bool ascending
			, params string[] names)
		{
			if (names != null)
			{
				foreach (string name in names)
				{
					_columns.AddItem(new IndexBuilder.Column(name, ascending));
				}
			}
			return this;
		}

		/// <summary>
		/// Sets this index to be a primary key index (additionally sets the index as
		/// unique).
		/// </summary>
		/// <remarks>
		/// Sets this index to be a primary key index (additionally sets the index as
		/// unique).
		/// </remarks>
		public virtual HealthMarketScience.Jackcess.IndexBuilder SetPrimaryKey()
		{
			_type = Index.PRIMARY_KEY_INDEX_TYPE;
			return SetUnique();
		}

		/// <summary>Sets this index to enforce uniqueness.</summary>
		/// <remarks>Sets this index to enforce uniqueness.</remarks>
		public virtual HealthMarketScience.Jackcess.IndexBuilder SetUnique()
		{
			_flags |= IndexData.UNIQUE_INDEX_FLAG;
			return this;
		}

		/// <summary>Sets this index to ignore null values.</summary>
		/// <remarks>Sets this index to ignore null values.</remarks>
		public virtual HealthMarketScience.Jackcess.IndexBuilder SetIgnoreNulls()
		{
			_flags |= IndexData.IGNORE_NULLS_INDEX_FLAG;
			return this;
		}

		public virtual void Validate(ICollection<string> tableColNames)
		{
			if (GetColumns().IsEmpty())
			{
				throw new ArgumentException("index " + GetName() + " has no columns");
			}
			if (GetColumns().Count > IndexData.MAX_COLUMNS)
			{
				throw new ArgumentException("index " + GetName() + " has too many columns, max " 
					+ IndexData.MAX_COLUMNS);
			}
			ICollection<string> idxColNames = new HashSet<string>();
			foreach (IndexBuilder.Column col in GetColumns())
			{
				string idxColName = col.GetName().ToUpper();
				if (!idxColNames.AddItem(idxColName))
				{
					throw new ArgumentException("duplicate column name " + col.GetName() + " in index "
						 + GetName());
				}
				if (!tableColNames.Contains(idxColName))
				{
					throw new ArgumentException("column named " + col.GetName() + " not found in table"
						);
				}
			}
		}

		protected internal virtual int GetIndexNumber()
		{
			return _indexNumber;
		}

		protected internal virtual void SetIndexNumber(int newIndexNumber)
		{
			_indexNumber = newIndexNumber;
		}

		protected internal virtual int GetIndexDataNumber()
		{
			return _indexDataNumber;
		}

		protected internal virtual void SetIndexDataNumber(int newIndexDataNumber)
		{
			_indexDataNumber = newIndexDataNumber;
		}

		protected internal virtual byte GetUmapRowNumber()
		{
			return _umapRowNumber;
		}

		protected internal virtual void SetUmapRowNumber(byte newUmapRowNumber)
		{
			_umapRowNumber = newUmapRowNumber;
		}

		protected internal virtual int GetUmapPageNumber()
		{
			return _umapPageNumber;
		}

		protected internal virtual void SetUmapPageNumber(int newUmapPageNumber)
		{
			_umapPageNumber = newUmapPageNumber;
		}

		protected internal virtual int GetRootPageNumber()
		{
			return _rootPageNumber;
		}

		protected internal virtual void SetRootPageNumber(int newRootPageNumber)
		{
			_rootPageNumber = newRootPageNumber;
		}

		/// <summary>Information about a column in this index (name and ordering).</summary>
		/// <remarks>Information about a column in this index (name and ordering).</remarks>
		public class Column
		{
			/// <summary>name of the column to be indexed</summary>
			private string _name;

			/// <summary>column flags (ordering)</summary>
			private byte _flags;

			public Column(string name, bool ascending)
			{
				_name = name;
				_flags = (ascending ? IndexData.ASCENDING_COLUMN_FLAG : (byte)0);
			}

			public virtual string GetName()
			{
				return _name;
			}

			public virtual IndexBuilder.Column SetName(string name)
			{
				_name = name;
				return this;
			}

			public virtual bool IsAscending()
			{
				return ((GetFlags() & IndexData.ASCENDING_COLUMN_FLAG) != 0);
			}

			public virtual byte GetFlags()
			{
				return _flags;
			}
		}
	}
}
