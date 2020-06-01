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
using System.Text;

namespace HealthMarketScience.Jackcess.Query
{
    /// <summary>
    /// Concrete Query subclass which represents an append query, e.g.:
    /// <code>INSERT INTO <table> VALUES (<values>)</code>
    /// </summary>
    /// <author>James Ahlborn</author>
    public class AppendQuery : BaseSelectQuery
    {
        public AppendQuery(string name, IList<Query.Row> rows, int objectId) : base(name,
            rows, objectId, Query.Type.APPEND)
        {
        }

        public virtual string GetTargetTable()
        {
            return GetTypeRow().name1;
        }

        public virtual string GetRemoteDbPath()
        {
            return GetTypeRow().name2;
        }

        public virtual string GetRemoteDbType()
        {
            return GetTypeRow().expression;
        }

        protected internal virtual IList<Query.Row> GetValueRows()
        {
            return FilterRowsByFlag(base.GetColumnRows(), QueryFormat.APPEND_VALUE_FLAG);
        }

        public override IList<Query.Row> GetColumnRows()
        {
            return FilterRowsByNotFlag(base.GetColumnRows(), QueryFormat.APPEND_VALUE_FLAG);
        }

        public virtual IList<string> GetValues()
        {
            return new _RowFormatter_70(GetValueRows()).Format();
        }

        private sealed class _RowFormatter_70 : Query.RowFormatter
        {
            public _RowFormatter_70(IList<Query.Row> baseArg1) : base(baseArg1)
            {
            }

            protected internal override void Format(StringBuilder builder, Query.Row row)
            {
                builder.Append(row.expression);
            }
        }

        protected internal override void ToSQLString(StringBuilder builder)
        {
            builder.Append("INSERT INTO ").Append(GetTargetTable());
            ToRemoteDb(builder, GetRemoteDbPath(), GetRemoteDbType());
            builder.Append(QueryFormat.NEWLINE);
            IList<string> values = GetValues();
            if (!values.IsEmpty())
            {
                builder.Append("VALUES (").Append(values).Append(')');
            }
            else
            {
                ToSQLSelectString(builder, true);
            }
        }
    }
}
