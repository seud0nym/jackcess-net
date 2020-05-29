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
using System.IO;

namespace HealthMarketScience.Jackcess
{
    public class DataTypeUtil
    {
        /// <summary>Map of SQL types to Access data types</summary>
        private static IDictionary<int, DataType> SQL_TYPES = new Dictionary<int, DataType
            >();

        /// <summary>Alternate map of SQL types to Access data types</summary>
        private static IDictionary<int, DataType> ALT_SQL_TYPES = new Dictionary<int, DataType
            >();

        private static IDictionary<byte, DataTypeProperties> DATA_TYPES = new Dictionary<
            byte, DataTypeProperties>();

        static DataTypeUtil()
        {
            foreach (DataType type in DataType.GetValues(typeof(DataType)))
            {
                int? sqlType = DataTypeProperties.Get(type).sqlType;
                if (sqlType != null)
                {
                    SQL_TYPES.Put(sqlType.Value, type);
                }
            }
            SQL_TYPES.Put(Types.BIT, DataType.BYTE);
            SQL_TYPES.Put(Types.BLOB, DataType.OLE);
            SQL_TYPES.Put(Types.CLOB, DataType.MEMO);
            SQL_TYPES.Put(Types.BIGINT, DataType.LONG);
            SQL_TYPES.Put(Types.CHAR, DataType.TEXT);
            SQL_TYPES.Put(Types.DATE, DataType.SHORT_DATE_TIME);
            SQL_TYPES.Put(Types.REAL, DataType.DOUBLE);
            SQL_TYPES.Put(Types.TIME, DataType.SHORT_DATE_TIME);
            SQL_TYPES.Put(Types.VARBINARY, DataType.BINARY);
            // the "alternate" types allow for larger values
            ALT_SQL_TYPES.Put(Types.VARCHAR, DataType.MEMO);
            ALT_SQL_TYPES.Put(Types.VARBINARY, DataType.OLE);
            ALT_SQL_TYPES.Put(Types.BINARY, DataType.OLE);
            foreach (DataType type_1 in DataType.GetValues(typeof(DataType)))
            {
                if (IsUnsupported(type_1))
                {
                    continue;
                }
                DataTypeProperties prop = DataTypeProperties.Get(type_1);
                DATA_TYPES.Put(prop.value, prop);
            }
        }

        private static bool IsWithinRange(int value, int? minValue, int? maxValue)
        {
            return minValue.HasValue && maxValue.HasValue && ((value >= minValue) && (value <= maxValue));
        }

        private static int ToValidRange(int value, int? minValue, int? maxValue)
        {
            return (minValue.HasValue && maxValue.HasValue && ((value > maxValue.Value)) ? maxValue.Value : (minValue.HasValue && maxValue.HasValue && ((value < minValue.Value)) ? minValue.Value : value));
        }

        /// <exception cref="System.IO.IOException"></exception>
        public static DataType FromByte(byte b)
        {
            DataType? rtn = DATA_TYPES.Get(b).type;
            if (rtn != null)
            {
                return rtn.Value;
            }
            throw new IOException("Unrecognized data type: " + b);
        }

        /// <exception cref="Sharpen.SQLException"></exception>
        public static DataType FromSQLType(int sqlType)
        {
            return FromSQLType(sqlType, 0);
        }

        /// <exception cref="Sharpen.SQLException"></exception>
        public static DataType FromSQLType(int sqlType, int lengthInUnits)
        {
            DataType? rtn = SQL_TYPES.Get(sqlType);
            if (rtn == null)
            {
                throw new Exception("Unsupported SQL type: " + sqlType);
            }
            DataTypeProperties prop = DataTypeProperties.Get(rtn.Value);
            // make sure size is reasonable
            int size = lengthInUnits * prop.unitSize;
            if (prop.variableLength && !IsValidSize(rtn.Value, size))
            {
                // try alternate type. we always accept alternate "long value" types
                // regardless of the given lengthInUnits
                DataType? altRtn = ALT_SQL_TYPES.Get(sqlType);
                if ((altRtn != null) && (DataTypeProperties.Get(altRtn.Value).longValue || IsValidSize(
                    altRtn.Value, size)))
                {
                    // use alternate type
                    rtn = altRtn;
                }
            }
            return rtn.Value;
        }

        public static int GetFixedSize(DataType type)
        {
            return GetFixedSize(type, null);
        }

        public static int GetFixedSize(DataType type, short? colLength)
        {
            DataTypeProperties prop = DataTypeProperties.Get(type);
            if (prop.fixedSize != null)
            {
                if (colLength != null)
                {
                    return Math.Max(prop.fixedSize.Value, colLength.Value);
                }
                return prop.fixedSize.Value;
            }
            if (colLength != null)
            {
                return colLength.Value;
            }
            throw new ArgumentException("Unexpected fixed length column " + type);
        }

        public static int ToUnitSize(DataType type, int size)
        {
            return (size / DataTypeProperties.Get(type).unitSize);
        }

        public static int FromUnitSize(DataType type, int unitSize)
        {
            return (unitSize * DataTypeProperties.Get(type).unitSize);
        }

        public static bool IsValidSize(DataType type, int size)
        {
            DataTypeProperties prop = DataTypeProperties.Get(type);
            return DataTypeUtil.IsWithinRange(size, prop.minSize, prop.maxSize);
        }

        public static bool IsValidScale(DataType type, int scale)
        {
            DataTypeProperties prop = DataTypeProperties.Get(type);
            return DataTypeUtil.IsWithinRange(scale, prop.minScale, prop.maxScale);
        }

        public static bool IsValidPrecision(DataType type, int precision)
        {
            DataTypeProperties prop = DataTypeProperties.Get(type);
            return DataTypeUtil.IsWithinRange(precision, prop.minPrecision, prop.maxPrecision
                );
        }

        public static int ToValidSize(DataType type, int size)
        {
            DataTypeProperties prop = DataTypeProperties.Get(type);
            return DataTypeUtil.ToValidRange(size, prop.minSize, prop.maxSize);
        }

        public static int ToValidScale(DataType type, int scale)
        {
            DataTypeProperties prop = DataTypeProperties.Get(type);
            return DataTypeUtil.ToValidRange(scale, prop.minScale, prop.maxScale);
        }

        public static int ToValidPrecision(DataType type, int precision)
        {
            DataTypeProperties prop = DataTypeProperties.Get(type);
            return DataTypeUtil.ToValidRange(precision, prop.minPrecision, prop.maxPrecision);
        }

        public static bool IsTextual(DataType? type)
        {
            return type.HasValue && ((type.Value == DataType.TEXT) || (type.Value == DataType.MEMO));
        }

        public static bool MayBeAutoNumber(DataType? type)
        {
            return type.HasValue && ((type == DataType.LONG) || (type == DataType.GUID));
        }

        public static bool IsUnsupported(DataType? type)
        {
            return type.HasValue && ((type == DataType.UNSUPPORTED_FIXEDLEN) || (type == DataType.UNSUPPORTED_VARLEN
                ));
        }
    }
}
