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
using System.Text;
using HealthMarketScience.Jackcess.Query;
using Sharpen;

namespace HealthMarketScience.Jackcess.Query
{
	/// <summary>
	/// Concrete Query subclass which represents a UNION query, e.g.:
	/// <code>SELECT <query1> UNION SELECT <query2></code>
	/// </summary>
	/// <author>James Ahlborn</author>
	public class UnionQuery : HealthMarketScience.Jackcess.Query.Query
	{
		public UnionQuery(string name, IList<Query.Row> rows, int objectId) : base(name, 
			rows, objectId, Query.Type.UNION)
		{
		}

		public virtual string GetUnionType()
		{
			return (HasFlag(QueryFormat.UNION_FLAG) ? QueryFormat.DEFAULT_TYPE : "ALL");
		}

		public virtual string GetUnionString1()
		{
			return GetUnionString(QueryFormat.UNION_PART1);
		}

		public virtual string GetUnionString2()
		{
			return GetUnionString(QueryFormat.UNION_PART2);
		}

		public override IList<string> GetOrderings()
		{
			return base.GetOrderings();
		}

		private string GetUnionString(string id)
		{
			foreach (Query.Row row in GetTableRows())
			{
				if (id.Equals(row.name2))
				{
					return CleanUnionString(row.expression);
				}
			}
			throw new InvalidOperationException("Could not find union query with id " + id);
		}

		protected internal override void ToSQLString(StringBuilder builder)
		{
			builder.Append(GetUnionString1()).Append(QueryFormat.NEWLINE).Append("UNION ");
			string unionType = GetUnionType();
			if (!QueryFormat.DEFAULT_TYPE.Equals(unionType))
			{
				builder.Append(unionType).Append(' ');
			}
			builder.Append(GetUnionString2());
			IList<string> orderings = GetOrderings();
			if (!orderings.IsEmpty())
			{
				builder.Append(QueryFormat.NEWLINE).Append("ORDER BY ").Append(orderings);
			}
		}

		private static string CleanUnionString(string str)
		{
			return str.Trim().ReplaceAll("[\r\n]+", QueryFormat.NEWLINE);
		}
	}
}
