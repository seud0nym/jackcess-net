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

using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Access data type properties</summary>
	public class DataTypeProperties
	{
		/// <summary>Corresponds to a java Boolean.</summary>
		/// <remarks>
		/// Corresponds to a java Boolean.  Accepts Boolean or
		/// <code>null</code>
		/// (which is
		/// considered
		/// <code>false</code>
		/// ).  Equivalent to SQL
		/// <see cref="Sharpen.Types.BOOLEAN">Sharpen.Types.BOOLEAN</see>
		/// .
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties BOOLEAN = 
			new HealthMarketScience.Jackcess.DataTypeProperties(DataType.BOOLEAN, unchecked(
			(byte)unchecked((int)(0x01))), Types.BOOLEAN, 0);

		/// <summary>Corresponds to a java Byte.</summary>
		/// <remarks>
		/// Corresponds to a java Byte.  Accepts any Number (using
		/// <see cref="Sharpen.Number.()">Sharpen.Number.()</see>
		/// ), Boolean as 1 or 0, any Object converted to a
		/// String and parsed as Double, or
		/// <code>null</code>
		/// .  Equivalent to SQL
		/// <see cref="Sharpen.Types.TINYINT">Sharpen.Types.TINYINT</see>
		/// ,
		/// <see cref="Sharpen.Types.BIT">Sharpen.Types.BIT</see>
		/// .
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties BYTE = new 
			HealthMarketScience.Jackcess.DataTypeProperties(DataType.BYTE, unchecked((byte)unchecked(
			(int)(0x02))), Types.TINYINT, 1);

		/// <summary>Corresponds to a java Short.</summary>
		/// <remarks>
		/// Corresponds to a java Short.  Accepts any Number (using
		/// <see cref="Sharpen.Number.()">Sharpen.Number.()</see>
		/// ), Boolean as 1 or 0, any Object converted to a
		/// String and parsed as Double, or
		/// <code>null</code>
		/// .  Equivalent to SQL
		/// <see cref="Sharpen.Types.SMALLINT">Sharpen.Types.SMALLINT</see>
		/// .
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties INT = new 
			HealthMarketScience.Jackcess.DataTypeProperties(DataType.INT, unchecked((byte)unchecked(
			(int)(0x03))), Types.SMALLINT, 2);

		/// <summary>Corresponds to a java Integer.</summary>
		/// <remarks>
		/// Corresponds to a java Integer.  Accepts any Number (using
		/// <see cref="Sharpen.Number.()">Sharpen.Number.()</see>
		/// ), Boolean as 1 or 0, any Object converted to a
		/// String and parsed as Double, or
		/// <code>null</code>
		/// .  Equivalent to SQL
		/// <see cref="Sharpen.Types.INTEGER">Sharpen.Types.INTEGER</see>
		/// ,
		/// <see cref="Sharpen.Types.BIGINT">Sharpen.Types.BIGINT</see>
		/// .
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties LONG = new 
			HealthMarketScience.Jackcess.DataTypeProperties(DataType.LONG, unchecked((byte)unchecked(
			(int)(0x04))), Types.INTEGER, 4);

		/// <summary>Corresponds to a java BigDecimal with at most 4 decimal places.</summary>
		/// <remarks>
		/// Corresponds to a java BigDecimal with at most 4 decimal places.  Accepts
		/// any Number (using
		/// <see cref="Sharpen.Number.()">Sharpen.Number.()</see>
		/// ), a BigInteger, a BigDecimal
		/// (with at most 4 decimal places), Boolean as 1 or 0, any Object converted
		/// to a String and parsed as BigDecimal, or
		/// <code>null</code>
		/// .  Equivalent to SQL
		/// <see cref="Sharpen.Types.DECIMAL">Sharpen.Types.DECIMAL</see>
		/// .
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties MONEY = new 
			HealthMarketScience.Jackcess.DataTypeProperties(DataType.MONEY, unchecked((byte)
			unchecked((int)(0x05))), Types.DECIMAL, 8);

		/// <summary>Corresponds to a java Float.</summary>
		/// <remarks>
		/// Corresponds to a java Float.  Accepts any Number (using
		/// <see cref="Sharpen.Number.()">Sharpen.Number.()</see>
		/// ), Boolean as 1 or 0, any Object converted to a
		/// String and parsed as Double, or
		/// <code>null</code>
		/// .  Equivalent to SQL
		/// <see cref="Sharpen.Types.FLOAT">Sharpen.Types.FLOAT</see>
		/// .
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties FLOAT = new 
			HealthMarketScience.Jackcess.DataTypeProperties(DataType.FLOAT, unchecked((byte)
			unchecked((int)(0x06))), Types.FLOAT, 4);

		/// <summary>Corresponds to a java Double.</summary>
		/// <remarks>
		/// Corresponds to a java Double.  Accepts any Number (using
		/// <see cref="Sharpen.Number.()">Sharpen.Number.()</see>
		/// ), Boolean as 1 or 0, any Object converted to a
		/// String and parsed as Double, or
		/// <code>null</code>
		/// .  Equivalent to SQL
		/// <see cref="Sharpen.Types.DOUBLE">Sharpen.Types.DOUBLE</see>
		/// ,
		/// <see cref="Sharpen.Types.REAL">Sharpen.Types.REAL</see>
		/// .
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties DOUBLE = new 
			HealthMarketScience.Jackcess.DataTypeProperties(DataType.DOUBLE, unchecked((byte
			)unchecked((int)(0x07))), Types.DOUBLE, 8);

		/// <summary>Corresponds to a java Date.</summary>
		/// <remarks>
		/// Corresponds to a java Date.  Accepts a Date, any Number (using
		/// <see cref="Sharpen.Number.()">Sharpen.Number.()</see>
		/// ), or
		/// <code>null</code>
		/// .  Equivalent to SQL
		/// <see cref="Sharpen.Types.TIMESTAMP">Sharpen.Types.TIMESTAMP</see>
		/// ,
		/// <see cref="Sharpen.Types.DATE">Sharpen.Types.DATE</see>
		/// ,
		/// <see cref="Sharpen.Types.TIME">Sharpen.Types.TIME</see>
		/// .
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties SHORT_DATE_TIME
			 = new HealthMarketScience.Jackcess.DataTypeProperties(DataType.SHORT_DATE_TIME, 
			unchecked((byte)unchecked((int)(0x08))), Types.TIMESTAMP, 8);

		/// <summary>
		/// Corresponds to a java
		/// <code>byte[]</code>
		/// of max length 255 bytes.  Accepts a
		/// <code>byte[]</code>
		/// , or
		/// <code>null</code>
		/// .  Equivalent to SQL
		/// <see cref="Sharpen.Types.BINARY">Sharpen.Types.BINARY</see>
		/// ,
		/// <see cref="Sharpen.Types.VARBINARY">Sharpen.Types.VARBINARY</see>
		/// .
		/// </summary>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties BINARY = new 
			HealthMarketScience.Jackcess.DataTypeProperties(DataType.BINARY, unchecked((byte
			)unchecked((int)(0x09))), Types.BINARY, null, true, false, 0, 255, 255, 1);

		/// <summary>Corresponds to a java String of max length 255 chars.</summary>
		/// <remarks>
		/// Corresponds to a java String of max length 255 chars.  Accepts any
		/// CharSequence, any Object converted to a String , or
		/// <code>null</code>
		/// .
		/// Equivalent to SQL
		/// <see cref="Sharpen.Types.VARCHAR">Sharpen.Types.VARCHAR</see>
		/// ,
		/// <see cref="Sharpen.Types.CHAR">Sharpen.Types.CHAR</see>
		/// .
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties TEXT = new 
			HealthMarketScience.Jackcess.DataTypeProperties(DataType.TEXT, unchecked((byte)unchecked(
			(int)(0x0A))), Types.VARCHAR, null, true, false, 0, 50 * JetFormat.TEXT_FIELD_UNIT_SIZE
			, (int)JetFormat.TEXT_FIELD_MAX_LENGTH, JetFormat.TEXT_FIELD_UNIT_SIZE);

		/// <summary>
		/// Corresponds to a java
		/// <code>byte[]</code>
		/// of max length 16777215 bytes.
		/// Accepts a
		/// <code>byte[]</code>
		/// , or
		/// <code>null</code>
		/// .  Equivalent to SQL
		/// <see cref="Sharpen.Types.LONGVARBINARY">Sharpen.Types.LONGVARBINARY</see>
		/// ,
		/// <see cref="Sharpen.Types.BLOB">Sharpen.Types.BLOB</see>
		/// .
		/// </summary>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties OLE = new 
			HealthMarketScience.Jackcess.DataTypeProperties(DataType.OLE, unchecked((byte)unchecked(
			(int)(0x0B))), Types.LONGVARBINARY, null, true, true, 0, null, unchecked((int)(0x3FFFFFFF
			)), 1);

		/// <summary>Corresponds to a java String of max length 8388607 chars.</summary>
		/// <remarks>
		/// Corresponds to a java String of max length 8388607 chars.  Accepts any
		/// CharSequence, any Object converted to a String , or
		/// <code>null</code>
		/// .
		/// Equivalent to SQL
		/// <see cref="Sharpen.Types.LONGVARCHAR">Sharpen.Types.LONGVARCHAR</see>
		/// ,
		/// <see cref="Sharpen.Types.CLOB">Sharpen.Types.CLOB</see>
		/// .
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties MEMO = new 
			HealthMarketScience.Jackcess.DataTypeProperties(DataType.MEMO, unchecked((byte)unchecked(
			(int)(0x0C))), Types.LONGVARCHAR, null, true, true, 0, null, unchecked((int)(0x3FFFFFFF
			)), JetFormat.TEXT_FIELD_UNIT_SIZE);

		/// <summary>Unknown data.</summary>
		/// <remarks>Unknown data.  Handled like BINARY.</remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties UNKNOWN_0D
			 = new HealthMarketScience.Jackcess.DataTypeProperties(DataType.UNKNOWN_0D, unchecked(
			(byte)unchecked((int)(0x0D))), null, null, true, false, 0, 255, 255, 1);

		/// <summary>
		/// Corresponds to a java String with the pattern
		/// <code>"{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}"</code>, also known as a
		/// "Replication ID" in Access.
		/// </summary>
		/// <remarks>
		/// Corresponds to a java String with the pattern
		/// <code>"{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}"</code>, also known as a
		/// "Replication ID" in Access.  Accepts any
		/// Object converted to a String matching this pattern (surrounding "{}" are
		/// optional, so
		/// <see cref="Sharpen.UUID">Sharpen.UUID</see>
		/// s are supported), or
		/// <code>null</code>
		/// .
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties GUID = new 
			HealthMarketScience.Jackcess.DataTypeProperties(DataType.GUID, unchecked((byte)unchecked(
			(int)(0x0F))), null, 16);

		/// <summary>Corresponds to a java BigDecimal.</summary>
		/// <remarks>
		/// Corresponds to a java BigDecimal.  Accepts any Number (using
		/// <see cref="Sharpen.Number.()">Sharpen.Number.()</see>
		/// ), a BigInteger, a BigDecimal, Boolean as 1 or
		/// 0, any Object converted to a String and parsed as BigDecimal, or
		/// <code>null</code>
		/// .  Equivalent to SQL
		/// <see cref="Sharpen.Types.NUMERIC">Sharpen.Types.NUMERIC</see>
		/// .
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties NUMERIC = 
			new HealthMarketScience.Jackcess.DataTypeProperties(DataType.NUMERIC, unchecked(
			(byte)unchecked((int)(0x10))), Types.NUMERIC, 17, true, false, 17, 17, 17, true, 
			0, 0, 28, 1, 18, 28, 1);

		/// <summary>
		/// Unknown data (seems to be an alternative OLE type, used by
		/// MSysAccessObjects table).
		/// </summary>
		/// <remarks>
		/// Unknown data (seems to be an alternative OLE type, used by
		/// MSysAccessObjects table).  Handled like a fixed length BINARY/OLE.
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties UNKNOWN_11
			 = new HealthMarketScience.Jackcess.DataTypeProperties(DataType.UNKNOWN_11, unchecked(
			(byte)unchecked((int)(0x11))), null, 3992);

		/// <summary>Dummy type for a fixed length type which is not currently supported.</summary>
		/// <remarks>
		/// Dummy type for a fixed length type which is not currently supported.
		/// Handled like a fixed length BINARY.
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties UNSUPPORTED_FIXEDLEN
			 = new HealthMarketScience.Jackcess.DataTypeProperties(DataType.UNSUPPORTED_FIXEDLEN
			, unchecked((byte)unchecked((int)(0xFE))), null, null);

		/// <summary>Placeholder type for a variable length type which is not currently supported.
		/// 	</summary>
		/// <remarks>
		/// Placeholder type for a variable length type which is not currently supported.
		/// Handled like BINARY.
		/// </remarks>
		public static readonly HealthMarketScience.Jackcess.DataTypeProperties UNSUPPORTED_VARLEN
			 = new HealthMarketScience.Jackcess.DataTypeProperties(DataType.UNSUPPORTED_VARLEN
			, unchecked((byte)unchecked((int)(0xFF))), null, null, true, false, 0, null, unchecked(
			(int)(0x3FFFFFFF)), 1);

		// for some reason numeric is "var len" even though it has a fixed size...
		public static HealthMarketScience.Jackcess.DataTypeProperties Get(DataType? type)
		{
			switch (type)
			{
				case DataType.BOOLEAN:
				{
					return BOOLEAN;
				}

				case DataType.BYTE:
				{
					return BYTE;
				}

				case DataType.INT:
				{
					return INT;
				}

				case DataType.LONG:
				{
					return LONG;
				}

				case DataType.MONEY:
				{
					return MONEY;
				}

				case DataType.FLOAT:
				{
					return FLOAT;
				}

				case DataType.DOUBLE:
				{
					return DOUBLE;
				}

				case DataType.SHORT_DATE_TIME:
				{
					return SHORT_DATE_TIME;
				}

				case DataType.BINARY:
				{
					return BINARY;
				}

				case DataType.TEXT:
				{
					return TEXT;
				}

				case DataType.OLE:
				{
					return OLE;
				}

				case DataType.MEMO:
				{
					return MEMO;
				}

				case DataType.UNKNOWN_0D:
				{
					return UNKNOWN_0D;
				}

				case DataType.GUID:
				{
					return GUID;
				}

				case DataType.NUMERIC:
				{
					return NUMERIC;
				}

				case DataType.UNKNOWN_11:
				{
					return UNKNOWN_11;
				}

				case DataType.UNSUPPORTED_FIXEDLEN:
				{
					return UNSUPPORTED_FIXEDLEN;
				}

				case DataType.UNSUPPORTED_VARLEN:
				{
					return UNSUPPORTED_VARLEN;
				}

				default:
				{
					return null;
				}
			}
		}

		public readonly DataType type;

		/// <summary>is this a variable length field</summary>
		public readonly bool variableLength;

		/// <summary>is this a long value field</summary>
		public readonly bool longValue;

		/// <summary>does this field have scale/precision</summary>
		public readonly bool hasScalePrecision;

		/// <summary>Internal Access value</summary>
		public readonly byte value;

		/// <summary>Size in bytes of fixed length columns</summary>
		public readonly int? fixedSize;

		/// <summary>min in bytes size for var length columns</summary>
		public readonly int? minSize;

		/// <summary>default size in bytes for var length columns</summary>
		public readonly int? defaultSize;

		/// <summary>Max size in bytes for var length columns</summary>
		public readonly int? maxSize;

		/// <summary>SQL type equivalent, or null if none defined</summary>
		public readonly int? sqlType;

		/// <summary>min scale value</summary>
		public readonly int? minScale;

		/// <summary>the default scale value</summary>
		public readonly int? defaultScale;

		/// <summary>max scale value</summary>
		public readonly int? maxScale;

		/// <summary>min precision value</summary>
		public readonly int? minPrecision;

		/// <summary>the default precision value</summary>
		public readonly int? defaultPrecision;

		/// <summary>max precision value</summary>
		public readonly int? maxPrecision;

		/// <summary>the number of bytes per "unit" for this data type</summary>
		public readonly int unitSize;

		public readonly bool isTrueVariableLength;

		private DataTypeProperties(DataType type, byte value) : this(type, value, null, null
			)
		{
		}

		private DataTypeProperties(DataType type, byte value, int? sqlType, int? fixedSize)
			 : this(type, value, sqlType, fixedSize, false, false, null, null, null, 1)
		{
		}

		private DataTypeProperties(DataType type, byte value, int? sqlType, int? fixedSize, 
			bool variableLength, bool longValue, int? minSize, int? defaultSize, int? maxSize, 
			int unitSize) : this(type, value, sqlType, fixedSize, variableLength, longValue, 
			minSize, defaultSize, maxSize, false, null, null, null, null, null, null, unitSize
			)
		{
		}

		private DataTypeProperties(DataType type, byte value, int? sqlType, int? fixedSize, 
			bool variableLength, bool longValue, int? minSize, int? defaultSize, int? maxSize, 
			bool hasScalePrecision, int? minScale, int? defaultScale, int? maxScale, int? minPrecision
			, int? defaultPrecision, int? maxPrecision, int unitSize)
		{
			this.type = type;
			this.value = value;
			this.sqlType = sqlType;
			this.fixedSize = fixedSize;
			this.variableLength = variableLength;
			this.longValue = longValue;
			this.minSize = minSize;
			this.defaultSize = defaultSize;
			this.maxSize = maxSize;
			this.hasScalePrecision = hasScalePrecision;
			this.minScale = minScale;
			this.defaultScale = defaultScale;
			this.maxScale = maxScale;
			this.minPrecision = minPrecision;
			this.defaultPrecision = defaultPrecision;
			this.maxPrecision = maxPrecision;
			this.unitSize = unitSize;
			this.isTrueVariableLength = (variableLength && (minSize != maxSize));
		}
	}
}
