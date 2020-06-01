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

namespace HealthMarketScience.Jackcess.Query
{
    /// <summary>Constants used by the query data parsing.</summary>
    /// <remarks>Constants used by the query data parsing.</remarks>
    /// <author>James Ahlborn</author>
    public class QueryFormat
    {
        public QueryFormat()
        {
        }

        public const int SELECT_QUERY_OBJECT_FLAG = 0;

        public const int MAKE_TABLE_QUERY_OBJECT_FLAG = 80;

        public const int APPEND_QUERY_OBJECT_FLAG = 64;

        public const int UPDATE_QUERY_OBJECT_FLAG = 48;

        public const int DELETE_QUERY_OBJECT_FLAG = 32;

        public const int CROSS_TAB_QUERY_OBJECT_FLAG = 16;

        public const int DATA_DEF_QUERY_OBJECT_FLAG = 96;

        public const int PASSTHROUGH_QUERY_OBJECT_FLAG = 112;

        public const int UNION_QUERY_OBJECT_FLAG = 128;

        public static readonly string COL_ATTRIBUTE = "Attribute";

        public static readonly string COL_EXPRESSION = "Expression";

        public static readonly string COL_FLAG = "Flag";

        public static readonly string COL_EXTRA = "LvExtra";

        public static readonly string COL_NAME1 = "Name1";

        public static readonly string COL_NAME2 = "Name2";

        public static readonly string COL_OBJECTID = "ObjectId";

        public static readonly string COL_ORDER = "Order";

        public static readonly byte START_ATTRIBUTE = 0;

        public static readonly byte TYPE_ATTRIBUTE = 1;

        public static readonly byte PARAMETER_ATTRIBUTE = 2;

        public static readonly byte FLAG_ATTRIBUTE = 3;

        public static readonly byte REMOTEDB_ATTRIBUTE = 4;

        public static readonly byte TABLE_ATTRIBUTE = 5;

        public static readonly byte COLUMN_ATTRIBUTE = 6;

        public static readonly byte JOIN_ATTRIBUTE = 7;

        public static readonly byte WHERE_ATTRIBUTE = 8;

        public static readonly byte GROUPBY_ATTRIBUTE = 9;

        public static readonly byte HAVING_ATTRIBUTE = 10;

        public static readonly byte ORDERBY_ATTRIBUTE = 11;

        public static readonly byte END_ATTRIBUTE = unchecked((byte)255);

        public const short UNION_FLAG = unchecked((int)(0x02));

        public static readonly short TEXT_FLAG = (short)DataTypeProperties.TEXT.value;

        public static readonly string DESCENDING_FLAG = "D";

        public const short SELECT_STAR_SELECT_TYPE = unchecked((int)(0x01));

        public const short DISTINCT_SELECT_TYPE = unchecked((int)(0x02));

        public const short OWNER_ACCESS_SELECT_TYPE = unchecked((int)(0x04));

        public const short DISTINCT_ROW_SELECT_TYPE = unchecked((int)(0x08));

        public const short TOP_SELECT_TYPE = unchecked((int)(0x10));

        public const short PERCENT_SELECT_TYPE = unchecked((int)(0x20));

        public const short APPEND_VALUE_FLAG = (short)unchecked((short)(0x8000));

        public const short CROSSTAB_PIVOT_FLAG = unchecked((int)(0x01));

        public const short CROSSTAB_NORMAL_FLAG = unchecked((int)(0x02));

        public static readonly string UNION_PART1 = "X7YZ_____1";

        public static readonly string UNION_PART2 = "X7YZ_____2";

        public static readonly string DEFAULT_TYPE = string.Empty;

        public static readonly Sharpen.Pattern QUOTABLE_CHAR_PAT = Sharpen.Pattern.Compile
            ("\\W");

        public static readonly Sharpen.Pattern IDENTIFIER_SEP_PAT = Sharpen.Pattern.Compile
            ("\\.");

        public const char IDENTIFIER_SEP_CHAR = '.';

        public static readonly string NEWLINE = Runtime.LineSeparator();

        public static readonly IDictionary<short, string> PARAM_TYPE_MAP = new Dictionary
            <short, string>();

        public static readonly IDictionary<short, string> JOIN_TYPE_MAP = new Dictionary<
            short, string>();

        static QueryFormat()
        {
            // dbQSPTBulk = 144
            // dbQCompound = 160
            // dbQProcedure = 224
            // dbQAction = 240
            PARAM_TYPE_MAP.Put((short)0, "Value");
            PARAM_TYPE_MAP.Put((short)DataTypeProperties.BOOLEAN.value, "Bit");
            PARAM_TYPE_MAP.Put((short)DataTypeProperties.TEXT.value, "Text");
            PARAM_TYPE_MAP.Put((short)DataTypeProperties.BYTE.value, "Byte");
            PARAM_TYPE_MAP.Put((short)DataTypeProperties.INT.value, "Short");
            PARAM_TYPE_MAP.Put((short)DataTypeProperties.LONG.value, "Long");
            PARAM_TYPE_MAP.Put((short)DataTypeProperties.MONEY.value, "Currency");
            PARAM_TYPE_MAP.Put((short)DataTypeProperties.FLOAT.value, "IEEESingle");
            PARAM_TYPE_MAP.Put((short)DataTypeProperties.DOUBLE.value, "IEEEDouble");
            PARAM_TYPE_MAP.Put((short)DataTypeProperties.SHORT_DATE_TIME.value, "DateTime");
            PARAM_TYPE_MAP.Put((short)DataTypeProperties.BINARY.value, "Binary");
            PARAM_TYPE_MAP.Put((short)DataTypeProperties.OLE.value, "LongBinary");
            PARAM_TYPE_MAP.Put((short)DataTypeProperties.GUID.value, "Guid");
            JOIN_TYPE_MAP.Put((short)1, " INNER JOIN ");
            JOIN_TYPE_MAP.Put((short)2, " LEFT JOIN ");
            JOIN_TYPE_MAP.Put((short)3, " RIGHT JOIN ");
        }
    }
}
