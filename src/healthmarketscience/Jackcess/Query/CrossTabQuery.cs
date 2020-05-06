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
	/// <summary>
	/// Concrete Query subclass which represents a crosstab/pivot query, e.g.:
	/// <code>TRANSFORM <expr> SELECT <query> PIVOT <expr></code>
	/// </summary>
	/// <author>James Ahlborn</author>
	public class CrossTabQuery : BaseSelectQuery
	{
		public CrossTabQuery(string name, IList<Query.Row> rows, int objectId) : base(name
			, rows, objectId, Query.Type.CROSS_TAB)
		{
		}

		protected internal virtual Query.Row GetTransformRow()
		{
			return GetUniqueRow(FilterRowsByNotFlag(base.GetColumnRows(), (short)(QueryFormat.CROSSTAB_PIVOT_FLAG
				 | QueryFormat.CROSSTAB_NORMAL_FLAG)));
		}

		public override IList<Query.Row> GetColumnRows()
		{
			return FilterRowsByFlag(base.GetColumnRows(), QueryFormat.CROSSTAB_NORMAL_FLAG);
		}

		public override IList<Query.Row> GetGroupByRows()
		{
			return FilterRowsByFlag(base.GetGroupByRows(), QueryFormat.CROSSTAB_NORMAL_FLAG);
		}

		protected internal virtual Query.Row GetPivotRow()
		{
			return GetUniqueRow(FilterRowsByFlag(base.GetColumnRows(), QueryFormat.CROSSTAB_PIVOT_FLAG));
		}

		public virtual string GetTransformExpression()
		{
			Query.Row row = GetTransformRow();
			if (row.expression == null)
			{
				return null;
			}
			// note column expression are always quoted appropriately
			StringBuilder builder = new StringBuilder(row.expression);
			return ToAlias(builder, row.name1).ToString();
		}

		public virtual string GetPivotExpression()
		{
			return GetPivotRow().expression;
		}

		protected internal override void ToSQLString(StringBuilder builder)
		{
			string transformExpr = GetTransformExpression();
			if (transformExpr != null)
			{
				builder.Append("TRANSFORM ").Append(transformExpr).Append(QueryFormat.NEWLINE);
			}
			ToSQLSelectString(builder, true);
			builder.Append(QueryFormat.NEWLINE).Append("PIVOT ").Append(GetPivotExpression());
		}
	}
}
