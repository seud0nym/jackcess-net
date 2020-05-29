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
using System;
using System.Collections.Generic;
using System.Text;

namespace HealthMarketScience.Jackcess.Query
{
    /// <summary>Base class for classes which encapsulate information about an Access query.
    /// 	</summary>
    /// <remarks>
    /// Base class for classes which encapsulate information about an Access query.
    /// The
    /// <see cref="ToSQLString()">ToSQLString()</see>
    /// method can be used to convert this object into
    /// the actual SQL string which this query data represents.
    /// </remarks>
    /// <author>James Ahlborn</author>
    public abstract class Query
    {
        private static readonly Query.Row EMPTY_ROW = new Query.Row(Collections.EmptyMap<
            string, object>());

        public class Type
        {
            public static readonly Type SELECT = new Type(QueryFormat.SELECT_QUERY_OBJECT_FLAG, 1);

            public static readonly Type MAKE_TABLE = new Type(QueryFormat.MAKE_TABLE_QUERY_OBJECT_FLAG, 2);
            public static readonly Type APPEND = new Type(QueryFormat.APPEND_QUERY_OBJECT_FLAG, 3);
            public static readonly Type UPDATE = new Type(QueryFormat.UPDATE_QUERY_OBJECT_FLAG, 4);
            public static readonly Type DELETE = new Type(QueryFormat.DELETE_QUERY_OBJECT_FLAG, 5);
            public static readonly Type CROSS_TAB = new Type(QueryFormat.CROSS_TAB_QUERY_OBJECT_FLAG, 6);
            public static readonly Type DATA_DEFINITION = new Type(QueryFormat.DATA_DEF_QUERY_OBJECT_FLAG, 7);
            public static readonly Type PASSTHROUGH = new Type(QueryFormat.PASSTHROUGH_QUERY_OBJECT_FLAG, 8);
            public static readonly Type UNION = new Type(QueryFormat.UNION_QUERY_OBJECT_FLAG, 9);
            public static readonly Type UNKNOWN = new Type(-1, -1);

            public readonly int objectFlag;
            public readonly short value;

            private Type(int objectFlag, int value)
            {
                this.objectFlag = objectFlag;
                this.value = (short)value;
            }
        }

        private readonly string _name;

        private readonly IList<Query.Row> _rows;

        private readonly int _objectId;

        private readonly Query.Type _type;

        protected internal Query(string name, IList<Query.Row> rows, int objectId, Query.Type
             type)
        {
            _name = name;
            _rows = rows;
            _objectId = objectId;
            _type = type;
            if (type != Query.Type.UNKNOWN)
            {
                short foundType = GetShortValue(GetQueryType(rows), _type.value);
                if (foundType != _type.value)
                {
                    throw new InvalidOperationException("Unexpected query type " + foundType);
                }
            }
        }

        /// <summary>Returns the name of the query.</summary>
        /// <remarks>Returns the name of the query.</remarks>
        public virtual string GetName()
        {
            return _name;
        }

        /// <summary>Returns the type of the query.</summary>
        /// <remarks>Returns the type of the query.</remarks>
        public virtual Query.Type GetQueryType()
        {
            return _type;
        }

        /// <summary>Returns the unique object id of the query.</summary>
        /// <remarks>Returns the unique object id of the query.</remarks>
        public virtual int GetObjectId()
        {
            return _objectId;
        }

        public virtual int GetObjectFlag()
        {
            return GetQueryType().objectFlag;
        }

        /// <summary>
        /// Returns the rows from the system query table from which the query
        /// information was derived.
        /// </summary>
        /// <remarks>
        /// Returns the rows from the system query table from which the query
        /// information was derived.
        /// </remarks>
        public virtual IList<Query.Row> GetRows()
        {
            return _rows;
        }

        public virtual IList<Query.Row> GetRowsByAttribute(byte attribute)
        {
            return GetRowsByAttribute(GetRows(), attribute);
        }

        public virtual Query.Row GetRowByAttribute(byte attribute)
        {
            return GetUniqueRow(GetRowsByAttribute(GetRows(), attribute));
        }

        public virtual Query.Row GetTypeRow()
        {
            return GetRowByAttribute(QueryFormat.TYPE_ATTRIBUTE);
        }

        public virtual IList<Query.Row> GetParameterRows()
        {
            return GetRowsByAttribute(QueryFormat.PARAMETER_ATTRIBUTE);
        }

        public virtual Query.Row GetFlagRow()
        {
            return GetRowByAttribute(QueryFormat.FLAG_ATTRIBUTE);
        }

        public virtual Query.Row GetRemoteDatabaseRow()
        {
            return GetRowByAttribute(QueryFormat.REMOTEDB_ATTRIBUTE);
        }

        public virtual IList<Query.Row> GetTableRows()
        {
            return GetRowsByAttribute(QueryFormat.TABLE_ATTRIBUTE);
        }

        public virtual IList<Query.Row> GetColumnRows()
        {
            return GetRowsByAttribute(QueryFormat.COLUMN_ATTRIBUTE);
        }

        public virtual IList<Query.Row> GetJoinRows()
        {
            return GetRowsByAttribute(QueryFormat.JOIN_ATTRIBUTE);
        }

        public virtual Query.Row GetWhereRow()
        {
            return GetRowByAttribute(QueryFormat.WHERE_ATTRIBUTE);
        }

        public virtual IList<Query.Row> GetGroupByRows()
        {
            return GetRowsByAttribute(QueryFormat.GROUPBY_ATTRIBUTE);
        }

        public virtual Query.Row GetHavingRow()
        {
            return GetRowByAttribute(QueryFormat.HAVING_ATTRIBUTE);
        }

        public virtual IList<Query.Row> GetOrderByRows()
        {
            return GetRowsByAttribute(QueryFormat.ORDERBY_ATTRIBUTE);
        }

        protected internal abstract void ToSQLString(StringBuilder builder);

        protected internal virtual void ToSQLParameterString(StringBuilder builder)
        {
            // handle any parameters
            IList<string> @params = GetParameters();
            if (!@params.IsEmpty())
            {
                builder.Append("PARAMETERS ").Append(@params).Append(';').Append(QueryFormat.NEWLINE
                    );
            }
        }

        public virtual IList<string> GetParameters()
        {
            return (new _RowFormatter_231(GetParameterRows())).Format();
        }

        private sealed class _RowFormatter_231 : Query.RowFormatter
        {
            public _RowFormatter_231(IList<Query.Row> baseArg1) : base(baseArg1)
            {
            }

            protected internal override void Format(StringBuilder builder, Query.Row row)
            {
                string typeName = QueryFormat.PARAM_TYPE_MAP.Get(row.flag);
                if (typeName == null)
                {
                    throw new InvalidOperationException("Unknown param type " + row.flag);
                }
                builder.Append(row.name1).Append(' ').Append(typeName);
                if ((QueryFormat.TEXT_FLAG.Equals(row.flag)) && (HealthMarketScience.Jackcess.Query.Query
                    .GetIntValue(row.extra, 0) > 0))
                {
                    builder.Append('(').Append(row.extra).Append(')');
                }
            }
        }

        public virtual IList<string> GetFromTables()
        {
            IList<Query.Join> joinExprs = new AList<Query.Join>();
            foreach (Query.Row table in GetTableRows())
            {
                StringBuilder builder = new StringBuilder();
                if (table.expression != null)
                {
                    ToQuotedExpr(builder, table.expression).Append(QueryFormat.IDENTIFIER_SEP_CHAR);
                }
                if (table.name1 != null)
                {
                    ToOptionalQuotedExpr(builder, table.name1, true);
                }
                ToAlias(builder, table.name2);
                string key = ((table.name2 != null) ? table.name2 : table.name1);
                joinExprs.AddItem(new Query.Join(key, builder.ToString()));
            }
            IList<Query.Row> joins = GetJoinRows();
            if (!joins.IsEmpty())
            {
                // combine any multi-column joins
                ICollection<IList<Query.Row>> comboJoins = CombineJoins(joins);
                foreach (IList<Query.Row> comboJoin in comboJoins)
                {
                    Query.Row join = comboJoin[0];
                    string joinExpr = join.expression;
                    if (comboJoin.Count > 1)
                    {
                        // combine all the join expressions with "AND"
                        Query.AppendableList<string> comboExprs = new _AppendableList_279();
                        foreach (Query.Row tmpJoin in comboJoin)
                        {
                            comboExprs.AddItem(tmpJoin.expression);
                        }
                        joinExpr = new StringBuilder().Append("(").Append(comboExprs).Append(")").ToString
                            ();
                    }
                    string fromTable = join.name1;
                    string toTable = join.name2;
                    Query.Join fromExpr = GetJoinExpr(fromTable, joinExprs);
                    Query.Join toExpr = GetJoinExpr(toTable, joinExprs);
                    string joinType = QueryFormat.JOIN_TYPE_MAP.Get(join.flag);
                    if (joinType == null)
                    {
                        throw new InvalidOperationException("Unknown join type " + join.flag);
                    }
                    string expr = new StringBuilder().Append(fromExpr).Append(joinType).Append(toExpr
                        ).Append(" ON ").Append(joinExpr).ToString();
                    fromExpr.Add(toExpr, expr);
                    joinExprs.AddItem(fromExpr);
                }
            }
            IList<string> result = new Query.AppendableList<string>();
            foreach (Query.Join joinExpr_1 in joinExprs)
            {
                result.AddItem(joinExpr_1.expression);
            }
            return result;
        }

        private sealed class _AppendableList_279 : Query.AppendableList<string>
        {
            public _AppendableList_279()
            {
            }

            protected internal override string GetSeparator()
            {
                return ") AND (";
            }
        }

        private Query.Join GetJoinExpr(string table, IList<Query.Join> joinExprs)
        {
            for (Iterator<Query.Join> iter = joinExprs.Iterator(); iter.HasNext();)
            {
                Query.Join joinExpr = iter.Next();
                if (joinExpr.tables.Contains(table))
                {
                    iter.Remove();
                    return joinExpr;
                }
            }
            throw new InvalidOperationException("Cannot find join table " + table);
        }

        private ICollection<IList<Query.Row>> CombineJoins(IList<Query.Row> joins)
        {
            // combine joins with the same to/from tables
            IDictionary<IList<string>, IList<Query.Row>> comboJoinMap = new LinkedHashMap<IList
                <string>, IList<Query.Row>>();
            foreach (Query.Row join in joins)
            {
                IList<string> key = Arrays.AsList(join.name1, join.name2);
                IList<Query.Row> comboJoins = comboJoinMap.Get(key);
                if (comboJoins == null)
                {
                    comboJoins = new AList<Query.Row>();
                    comboJoinMap.Put(key, comboJoins);
                }
                else
                {
                    if ((short)comboJoins[0].flag != (short)join.flag)
                    {
                        throw new InvalidOperationException("Mismatched join flags for combo joins");
                    }
                }
                comboJoins.AddItem(join);
            }
            return comboJoinMap.Values;
        }

        public virtual string GetFromRemoteDbPath()
        {
            return GetRemoteDatabaseRow().name1;
        }

        public virtual string GetFromRemoteDbType()
        {
            return GetRemoteDatabaseRow().expression;
        }

        public virtual string GetWhereExpression()
        {
            return GetWhereRow().expression;
        }

        public virtual IList<string> GetOrderings()
        {
            return (new _RowFormatter_371(GetOrderByRows())).Format();
        }

        private sealed class _RowFormatter_371 : Query.RowFormatter
        {
            public _RowFormatter_371(IList<Query.Row> baseArg1) : base(baseArg1)
            {
            }

            protected internal override void Format(StringBuilder builder, Query.Row row)
            {
                builder.Append(row.expression);
                if (Sharpen.Runtime.EqualsIgnoreCase(QueryFormat.DESCENDING_FLAG, row.name1))
                {
                    builder.Append(" DESC");
                }
            }
        }

        public virtual string GetOwnerAccessType()
        {
            return (HasFlag(QueryFormat.OWNER_ACCESS_SELECT_TYPE) ? "WITH OWNERACCESS OPTION"
                 : QueryFormat.DEFAULT_TYPE);
        }

        public virtual bool HasFlag(int flagMask)
        {
            return HasFlag(GetFlagRow(), flagMask);
        }

        public virtual bool SupportsStandardClauses()
        {
            return true;
        }

        /// <summary>Returns the actual SQL string which this query data represents.</summary>
        /// <remarks>Returns the actual SQL string which this query data represents.</remarks>
        public virtual string ToSQLString()
        {
            StringBuilder builder = new StringBuilder();
            if (SupportsStandardClauses())
            {
                ToSQLParameterString(builder);
            }
            ToSQLString(builder);
            if (SupportsStandardClauses())
            {
                string accessType = GetOwnerAccessType();
                if (!QueryFormat.DEFAULT_TYPE.Equals(accessType))
                {
                    builder.Append(QueryFormat.NEWLINE).Append(accessType);
                }
                builder.Append(';');
            }
            return builder.ToString();
        }

        public override string ToString()
        {
            return ToSQLString();
        }

        /// <summary>Creates a concrete Query instance from the given query data.</summary>
        /// <remarks>Creates a concrete Query instance from the given query data.</remarks>
        /// <param name="objectFlag">the flag indicating the type of the query</param>
        /// <param name="name">the name of the query</param>
        /// <param name="rows">
        /// the rows from the system query table containing the data
        /// describing this query
        /// </param>
        /// <param name="objectId">the unique object id of this query</param>
        /// <returns>a Query instance for the given query data</returns>
        public static HealthMarketScience.Jackcess.Query.Query Create(int objectFlag, string
             name, IList<Query.Row> rows, int objectId)
        {
            try
            {
                switch (objectFlag)
                {
                    case QueryFormat.SELECT_QUERY_OBJECT_FLAG:
                        {
                            return new SelectQuery(name, rows, objectId);
                        }

                    case QueryFormat.MAKE_TABLE_QUERY_OBJECT_FLAG:
                        {
                            return new MakeTableQuery(name, rows, objectId);
                        }

                    case QueryFormat.APPEND_QUERY_OBJECT_FLAG:
                        {
                            return new AppendQuery(name, rows, objectId);
                        }

                    case QueryFormat.UPDATE_QUERY_OBJECT_FLAG:
                        {
                            return new UpdateQuery(name, rows, objectId);
                        }

                    case QueryFormat.DELETE_QUERY_OBJECT_FLAG:
                        {
                            return new DeleteQuery(name, rows, objectId);
                        }

                    case QueryFormat.CROSS_TAB_QUERY_OBJECT_FLAG:
                        {
                            return new CrossTabQuery(name, rows, objectId);
                        }

                    case QueryFormat.DATA_DEF_QUERY_OBJECT_FLAG:
                        {
                            return new DataDefinitionQuery(name, rows, objectId);
                        }

                    case QueryFormat.PASSTHROUGH_QUERY_OBJECT_FLAG:
                        {
                            return new PassthroughQuery(name, rows, objectId);
                        }

                    case QueryFormat.UNION_QUERY_OBJECT_FLAG:
                        {
                            return new UnionQuery(name, rows, objectId);
                        }

                    default:
                        {
                            // unknown querytype
                            throw new InvalidOperationException("unknown query object flag " + objectFlag);
                        }
                }
            }
            catch (InvalidOperationException e)
            {
                System.Console.Error.WriteLine("Failed parsing query");
                Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
            }
            // return unknown query
            return new Query.UnknownQuery(name, rows, objectId, objectFlag);
        }

        private static short GetQueryType(IList<Query.Row> rows)
        {
            return GetUniqueRow(GetRowsByAttribute(rows, QueryFormat.TYPE_ATTRIBUTE)).flag;
        }

        private static IList<Query.Row> GetRowsByAttribute(IList<Query.Row> rows, byte attribute
            )
        {
            IList<Query.Row> result = new AList<Query.Row>();
            foreach (Query.Row row in rows)
            {
                if (attribute.Equals(row.attribute))
                {
                    result.AddItem(row);
                }
            }
            return result;
        }

        protected internal static Query.Row GetUniqueRow(IList<Query.Row> rows)
        {
            if (rows.Count == 1)
            {
                return rows[0];
            }
            if (rows.IsEmpty())
            {
                return EMPTY_ROW;
            }
            throw new InvalidOperationException("Unexpected number of rows for" + rows);
        }

        protected internal static IList<Query.Row> FilterRowsByFlag(IList<Query.Row> rows
            , short flag)
        {
            return new _RowFilter_500(flag).Filter(rows);
        }

        private sealed class _RowFilter_500 : Query.RowFilter
        {
            public _RowFilter_500(short flag)
            {
                this.flag = flag;
            }

            protected internal override bool Keep(Query.Row row)
            {
                return HealthMarketScience.Jackcess.Query.Query.HasFlag(row, flag);
            }

            private readonly short flag;
        }

        protected internal static IList<Query.Row> FilterRowsByNotFlag(IList<Query.Row> rows
            , short flag)
        {
            return new _RowFilter_510(flag).Filter(rows);
        }

        private sealed class _RowFilter_510 : Query.RowFilter
        {
            public _RowFilter_510(short flag)
            {
                this.flag = flag;
            }

            protected internal override bool Keep(Query.Row row)
            {
                return !HealthMarketScience.Jackcess.Query.Query.HasFlag(row, flag);
            }

            private readonly short flag;
        }

        protected internal static bool HasFlag(Query.Row row, int flagMask)
        {
            return ((GetShortValue(row.flag, 0) & flagMask) != 0);
        }

        protected internal static short GetShortValue(short? s, int def)
        {
            return ((s != null) ? (short)s : (short)def);
        }

        protected internal static int GetIntValue(int? i, int def)
        {
            return ((i != null) ? (int)i : def);
        }

        protected internal static StringBuilder ToOptionalQuotedExpr(StringBuilder builder
            , string fullExpr, bool isIdentifier)
        {
            string[] exprs = (isIdentifier ? QueryFormat.IDENTIFIER_SEP_PAT.Split(fullExpr) :
                new string[] { fullExpr });
            for (int i = 0; i < exprs.Length; ++i)
            {
                string expr = exprs[i];
                if (QueryFormat.QUOTABLE_CHAR_PAT.Matcher(expr).Find())
                {
                    ToQuotedExpr(builder, expr);
                }
                else
                {
                    builder.Append(expr);
                }
                if (i < (exprs.Length - 1))
                {
                    builder.Append(QueryFormat.IDENTIFIER_SEP_CHAR);
                }
            }
            return builder;
        }

        protected internal static StringBuilder ToQuotedExpr(StringBuilder builder, string
             expr)
        {
            return builder.Append('[').Append(expr).Append(']');
        }

        protected internal static StringBuilder ToRemoteDb(StringBuilder builder, string
            remoteDbPath, string remoteDbType)
        {
            if ((remoteDbPath != null) || (remoteDbType != null))
            {
                // note, always include path string, even if empty
                builder.Append(" IN '");
                if (remoteDbPath != null)
                {
                    builder.Append(remoteDbPath);
                }
                builder.Append('\'');
                if (remoteDbType != null)
                {
                    builder.Append(" [").Append(remoteDbType).Append(']');
                }
            }
            return builder;
        }

        protected internal static StringBuilder ToAlias(StringBuilder builder, string alias
            )
        {
            if (alias != null)
            {
                ToOptionalQuotedExpr(builder.Append(" AS "), alias, false);
            }
            return builder;
        }

        internal sealed class UnknownQuery : HealthMarketScience.Jackcess.Query.Query
        {
            private readonly int _objectFlag;

            internal UnknownQuery(string name, IList<Query.Row> rows, int objectId, int objectFlag
                ) : base(name, rows, objectId, Query.Type.UNKNOWN)
            {
                _objectFlag = objectFlag;
            }

            public override int GetObjectFlag()
            {
                return _objectFlag;
            }

            protected internal override void ToSQLString(StringBuilder builder)
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Struct containing the information from a single row of the system query
        /// table.
        /// </summary>
        /// <remarks>
        /// Struct containing the information from a single row of the system query
        /// table.
        /// </remarks>
        public sealed class Row
        {
            public readonly byte attribute;

            public readonly string expression;

            public readonly short flag;

            public readonly int extra;

            public readonly string name1;

            public readonly string name2;

            public readonly int objectId;

            public readonly byte[] order;

            public Row(IDictionary<string, object> tableRow) : this((byte)tableRow.Get(QueryFormat
                .COL_ATTRIBUTE), (string)tableRow.Get(QueryFormat.COL_EXPRESSION), (short)tableRow
                .Get(QueryFormat.COL_FLAG), (int)tableRow.Get(QueryFormat.COL_EXTRA), (string)tableRow
                .Get(QueryFormat.COL_NAME1), (string)tableRow.Get(QueryFormat.COL_NAME2), (int)tableRow
                .Get(QueryFormat.COL_OBJECTID), (byte[])tableRow.Get(QueryFormat.COL_ORDER))
            {
            }

            public Row(byte attribute, string expression, short flag, int extra, string name1
                , string name2, int objectId, byte[] order)
            {
                this.attribute = attribute;
                this.expression = expression;
                this.flag = flag;
                this.extra = extra;
                this.name1 = name1;
                this.name2 = name2;
                this.objectId = objectId;
                this.order = order;
            }

            public IDictionary<string, object> ToTableRow()
            {
                IDictionary<string, object> tableRow = new LinkedHashMap<string, object>();
                tableRow.Put(QueryFormat.COL_ATTRIBUTE, attribute);
                tableRow.Put(QueryFormat.COL_EXPRESSION, expression);
                tableRow.Put(QueryFormat.COL_FLAG, flag);
                tableRow.Put(QueryFormat.COL_EXTRA, extra);
                tableRow.Put(QueryFormat.COL_NAME1, name1);
                tableRow.Put(QueryFormat.COL_NAME2, name2);
                tableRow.Put(QueryFormat.COL_OBJECTID, objectId);
                tableRow.Put(QueryFormat.COL_ORDER, order);
                return tableRow;
            }

            public override string ToString()
            {
                return ToTableRow().ToString();
            }
        }

        protected internal abstract class RowFormatter
        {
            private readonly IList<Query.Row> _list;

            protected internal RowFormatter(IList<Query.Row> list)
            {
                _list = list;
            }

            public virtual IList<string> Format()
            {
                return Format(new Query.AppendableList<string>());
            }

            public virtual IList<string> Format(IList<string> strs)
            {
                foreach (Query.Row row in _list)
                {
                    StringBuilder builder = new StringBuilder();
                    Format(builder, row);
                    strs.AddItem(builder.ToString());
                }
                return strs;
            }

            protected internal abstract void Format(StringBuilder builder, Query.Row row);
        }

        protected internal abstract class RowFilter
        {
            public RowFilter()
            {
            }

            public virtual IList<Query.Row> Filter(IList<Query.Row> list)
            {
                for (Iterator<Query.Row> iter = list.Iterator(); iter.HasNext();)
                {
                    if (!Keep(iter.Next()))
                    {
                        iter.Remove();
                    }
                }
                return list;
            }

            protected internal abstract bool Keep(Query.Row row);
        }

        [System.Serializable]
        protected internal class AppendableList<E> : AList<E>
        {
            private const long serialVersionUID = 0L;

            public AppendableList()
            {
            }

            public AppendableList(ICollection<E> c) : base(c)
            {
            }

            protected internal virtual string GetSeparator()
            {
                return ", ";
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                for (Iterator<E> iter = this.Iterator(); iter.HasNext();)
                {
                    builder.Append(iter.Next().ToString());
                    if (iter.HasNext())
                    {
                        builder.Append(GetSeparator());
                    }
                }
                return builder.ToString();
            }
        }

        internal sealed class Join
        {
            public readonly IList<string> tables = new AList<string>();

            public bool isJoin;

            public string expression;

            internal Join(string table, string expr)
            {
                tables.AddItem(table);
                expression = expr;
            }

            public void Add(Query.Join other, string newExpr)
            {
                Sharpen.Collections.AddAll(tables, other.tables);
                isJoin = true;
                expression = newExpr;
            }

            public override string ToString()
            {
                return (isJoin ? ("(" + expression + ")") : expression);
            }
        }
    }
}
