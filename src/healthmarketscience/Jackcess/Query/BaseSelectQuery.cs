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
using HealthMarketScience.Jackcess.Query;
using Sharpen;

namespace HealthMarketScience.Jackcess.Query
{
	/// <summary>Base class for queries which represent some form of SELECT statement.</summary>
	/// <remarks>Base class for queries which represent some form of SELECT statement.</remarks>
	/// <author>James Ahlborn</author>
	public abstract class BaseSelectQuery : HealthMarketScience.Jackcess.Query.Query
	{
		protected internal BaseSelectQuery(string name, IList<Query.Row> rows, int objectId
			, Query.Type type) : base(name, rows, objectId, type)
		{
		}

		protected internal virtual void ToSQLSelectString(StringBuilder builder, bool useSelectPrefix
			)
		{
			if (useSelectPrefix)
			{
				builder.Append("SELECT ");
				string selectType = GetSelectType();
				if (!QueryFormat.DEFAULT_TYPE.Equals(selectType))
				{
					builder.Append(selectType).Append(' ');
				}
			}
			builder.Append(GetSelectColumns());
			ToSelectInto(builder);
			IList<string> fromTables = GetFromTables();
			if (!fromTables.IsEmpty())
			{
				builder.Append(QueryFormat.NEWLINE).Append("FROM ").Append(fromTables);
				ToRemoteDb(builder, GetFromRemoteDbPath(), GetFromRemoteDbType());
			}
			string whereExpr = GetWhereExpression();
			if (whereExpr != null)
			{
				builder.Append(QueryFormat.NEWLINE).Append("WHERE ").Append(whereExpr);
			}
			IList<string> groupings = GetGroupings();
			if (!groupings.IsEmpty())
			{
				builder.Append(QueryFormat.NEWLINE).Append("GROUP BY ").Append(groupings);
			}
			string havingExpr = GetHavingExpression();
			if (havingExpr != null)
			{
				builder.Append(QueryFormat.NEWLINE).Append("HAVING ").Append(havingExpr);
			}
			IList<string> orderings = GetOrderings();
			if (!orderings.IsEmpty())
			{
				builder.Append(QueryFormat.NEWLINE).Append("ORDER BY ").Append(orderings);
			}
		}

		public virtual string GetSelectType()
		{
			if (HasFlag(QueryFormat.DISTINCT_SELECT_TYPE))
			{
				return "DISTINCT";
			}
			if (HasFlag(QueryFormat.DISTINCT_ROW_SELECT_TYPE))
			{
				return "DISTINCTROW";
			}
			if (HasFlag(QueryFormat.TOP_SELECT_TYPE))
			{
				StringBuilder builder = new StringBuilder();
				builder.Append("TOP ").Append(GetFlagRow().name1);
				if (HasFlag(QueryFormat.PERCENT_SELECT_TYPE))
				{
					builder.Append(" PERCENT");
				}
				return builder.ToString();
			}
			return QueryFormat.DEFAULT_TYPE;
		}

		public virtual IList<string> GetSelectColumns()
		{
			IList<string> result = (new _RowFormatter_113(GetColumnRows())).Format();
			// note column expression are always quoted appropriately
			if (HasFlag(QueryFormat.SELECT_STAR_SELECT_TYPE))
			{
				result.AddItem("*");
			}
			return result;
		}

		private sealed class _RowFormatter_113 : Query.RowFormatter
		{
			public _RowFormatter_113(IList<Query.Row> baseArg1) : base(baseArg1)
			{
			}

			protected internal override void Format(StringBuilder builder, Query.Row row)
			{
				builder.Append(row.expression);
				HealthMarketScience.Jackcess.Query.Query.ToAlias(builder, row.name1);
			}
		}

		protected internal virtual void ToSelectInto(StringBuilder builder)
		{
		}

		// base does nothing
		public override IList<string> GetFromTables()
		{
			return base.GetFromTables();
		}

		public override string GetFromRemoteDbPath()
		{
			return base.GetFromRemoteDbPath();
		}

		public override string GetFromRemoteDbType()
		{
			return base.GetFromRemoteDbType();
		}

		public override string GetWhereExpression()
		{
			return base.GetWhereExpression();
		}

		public virtual IList<string> GetGroupings()
		{
			return (new _RowFormatter_157(GetGroupByRows())).Format();
		}

		private sealed class _RowFormatter_157 : Query.RowFormatter
		{
			public _RowFormatter_157(IList<Query.Row> baseArg1) : base(baseArg1)
			{
			}

			protected internal override void Format(StringBuilder builder, Query.Row row)
			{
				builder.Append(row.expression);
			}
		}

		public virtual string GetHavingExpression()
		{
			return GetHavingRow().expression;
		}

		public override IList<string> GetOrderings()
		{
			return base.GetOrderings();
		}
	}
}
