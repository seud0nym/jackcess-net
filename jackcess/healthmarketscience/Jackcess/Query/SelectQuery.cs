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

namespace HealthMarketScience.Jackcess.Query
{
    /// <summary>
    /// Concrete Query subclass which represents a select query, e.g.:
    /// <code>SELECT <columns> FROM <tables> WHERE <expression></code>
    /// </summary>
    /// <author>James Ahlborn</author>
    public class SelectQuery : BaseSelectQuery
    {
        public SelectQuery(string name, IList<Query.Row> rows, int objectId) : base(name,
            rows, objectId, Query.Type.SELECT)
        {
        }

        protected internal override void ToSQLString(StringBuilder builder)
        {
            ToSQLSelectString(builder, true);
        }
    }
}
