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
	/// Concrete Query subclass which represents a row update query, e.g.:
	/// <code>UPDATE <table> SET <newValues></code>
	/// </summary>
	/// <author>James Ahlborn</author>
	public class UpdateQuery : HealthMarketScience.Jackcess.Query.Query
	{
		public UpdateQuery(string name, IList<Query.Row> rows, int objectId) : base(name, 
			rows, objectId, Query.Type.UPDATE)
		{
		}

		public virtual IList<string> GetTargetTables()
		{
			return base.GetFromTables();
		}

		public virtual string GetRemoteDbPath()
		{
			return base.GetFromRemoteDbPath();
		}

		public virtual string GetRemoteDbType()
		{
			return base.GetFromRemoteDbType();
		}

		public virtual IList<string> GetNewValues()
		{
			return (new _RowFormatter_65(GetColumnRows())).Format();
		}

		private sealed class _RowFormatter_65 : Query.RowFormatter
		{
			public _RowFormatter_65(IList<Query.Row> baseArg1) : base(baseArg1)
			{
			}

			protected internal override void Format(StringBuilder builder, Query.Row row)
			{
				HealthMarketScience.Jackcess.Query.Query.ToOptionalQuotedExpr(builder, row.name2, 
					true).Append(" = ").Append(row.expression);
			}
		}

		public override string GetWhereExpression()
		{
			return base.GetWhereExpression();
		}

		protected internal override void ToSQLString(StringBuilder builder)
		{
			builder.Append("UPDATE ").Append(GetTargetTables());
			ToRemoteDb(builder, GetRemoteDbPath(), GetRemoteDbType());
			builder.Append(QueryFormat.NEWLINE).Append("SET ").Append(GetNewValues());
			string whereExpr = GetWhereExpression();
			if (whereExpr != null)
			{
				builder.Append(QueryFormat.NEWLINE).Append("WHERE ").Append(whereExpr);
			}
		}
	}
}
