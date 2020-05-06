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

using System.Collections.Generic;
using System.Text;
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Information about a relationship between two tables in the database.</summary>
	/// <remarks>Information about a relationship between two tables in the database.</remarks>
	/// <author>James Ahlborn</author>
	public class Relationship
	{
		/// <summary>flag indicating one-to-one relationship</summary>
		private const int ONE_TO_ONE_FLAG = unchecked((int)(0x00000001));

		/// <summary>flag indicating no referential integrity</summary>
		private const int NO_REFERENTIAL_INTEGRITY_FLAG = unchecked((int)(0x00000002));

		/// <summary>flag indicating cascading updates (requires referential integrity)</summary>
		private const int CASCADE_UPDATES_FLAG = unchecked((int)(0x00000100));

		/// <summary>flag indicating cascading deletes (requires referential integrity)</summary>
		private const int CASCADE_DELETES_FLAG = unchecked((int)(0x00001000));

		/// <summary>flag indicating left outer join</summary>
		private const int LEFT_OUTER_JOIN_FLAG = unchecked((int)(0x01000000));

		/// <summary>flag indicating right outer join</summary>
		private const int RIGHT_OUTER_JOIN_FLAG = unchecked((int)(0x02000000));

		/// <summary>the name of this relationship</summary>
		private readonly string _name;

		/// <summary>the "from" table in this relationship</summary>
		private readonly Table _fromTable;

		/// <summary>the "to" table in this relationship</summary>
		private readonly Table _toTable;

		/// <summary>
		/// the columns in the "from" table in this relationship (aligned w/
		/// toColumns list)
		/// </summary>
		private IList<Column> _toColumns;

		/// <summary>
		/// the columns in the "to" table in this relationship (aligned w/
		/// toColumns list)
		/// </summary>
		private IList<Column> _fromColumns;

		/// <summary>the various flags describing this relationship</summary>
		private readonly int _flags;

		public Relationship(string name, Table fromTable, Table toTable, int flags, int numCols
			)
		{
			_name = name;
			_fromTable = fromTable;
			_fromColumns = new AList<Column>(Sharpen.Collections.NCopies(numCols, (Column)null
				));
			_toTable = toTable;
			_toColumns = new AList<Column>(Sharpen.Collections.NCopies(numCols, (Column)null)
				);
			_flags = flags;
		}

		public virtual string GetName()
		{
			return _name;
		}

		public virtual Table GetFromTable()
		{
			return _fromTable;
		}

		public virtual IList<Column> GetFromColumns()
		{
			return _fromColumns;
		}

		public virtual Table GetToTable()
		{
			return _toTable;
		}

		public virtual IList<Column> GetToColumns()
		{
			return _toColumns;
		}

		public virtual int GetFlags()
		{
			return _flags;
		}

		public virtual bool IsOneToOne()
		{
			return HasFlag(ONE_TO_ONE_FLAG);
		}

		public virtual bool HasReferentialIntegrity()
		{
			return !HasFlag(NO_REFERENTIAL_INTEGRITY_FLAG);
		}

		public virtual bool CascadeUpdates()
		{
			return HasFlag(CASCADE_UPDATES_FLAG);
		}

		public virtual bool CascadeDeletes()
		{
			return HasFlag(CASCADE_DELETES_FLAG);
		}

		public virtual bool IsLeftOuterJoin()
		{
			return HasFlag(LEFT_OUTER_JOIN_FLAG);
		}

		public virtual bool IsRightOuterJoin()
		{
			return HasFlag(RIGHT_OUTER_JOIN_FLAG);
		}

		private bool HasFlag(int flagMask)
		{
			return ((GetFlags() & flagMask) != 0);
		}

		public override string ToString()
		{
			StringBuilder rtn = new StringBuilder();
			rtn.Append("\tName: " + _name);
			rtn.Append("\n\tFromTable: " + _fromTable.GetName());
			rtn.Append("\n\tFromColumns: " + _fromColumns);
			rtn.Append("\n\tToTable: " + _toTable.GetName());
			rtn.Append("\n\tToColumns: " + _toColumns);
			rtn.Append("\n\tFlags: " + Sharpen.Extensions.ToHexString(_flags));
			rtn.Append("\n\n");
			return rtn.ToString();
		}
	}
}
