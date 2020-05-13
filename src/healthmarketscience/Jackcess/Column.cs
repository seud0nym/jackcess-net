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
using System.IO;
using System.Numerics;
using System.Text;
using HealthMarketScience.Jackcess;
using HealthMarketScience.Jackcess.Scsu;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Access database column definition</summary>
	/// <author>Tim McCune</author>
	/// <usage>_general_class_</usage>
	public class Column : IComparable<HealthMarketScience.Jackcess.Column>
	{
		/// <summary>
		/// Meaningless placeholder object for inserting values in an autonumber
		/// column.
		/// </summary>
		/// <remarks>
		/// Meaningless placeholder object for inserting values in an autonumber
		/// column.  it is not required that this value be used (any passed in value
		/// is ignored), but using this placeholder may make code more obvious.
		/// </remarks>
		/// <usage>_general_field_</usage>
		public static readonly object AUTO_NUMBER = "<AUTO_NUMBER>";

		/// <summary>
		/// Meaningless placeholder object for updating rows which indicates that a
		/// given column should keep its existing value.
		/// </summary>
		/// <remarks>
		/// Meaningless placeholder object for updating rows which indicates that a
		/// given column should keep its existing value.
		/// </remarks>
		/// <usage>_general_field_</usage>
		public static readonly object KEEP_VALUE = "<KEEP_VALUE>";

		/// <summary>Access stores numeric dates in days.</summary>
		/// <remarks>Access stores numeric dates in days.  Java stores them in milliseconds.</remarks>
		private const double MILLISECONDS_PER_DAY = (24L * 60L * 60L * 1000L);

		/// <summary>Access starts counting dates at Jan 1, 1900.</summary>
		/// <remarks>
		/// Access starts counting dates at Jan 1, 1900.  Java starts counting
		/// at Jan 1, 1970.  This is the # of millis between them for conversion.
		/// </remarks>
		private const long MILLIS_BETWEEN_EPOCH_AND_1900 = 25569L * (long)MILLISECONDS_PER_DAY;

		/// <summary>
		/// Long value (LVAL) type that indicates that the value is stored on the
		/// same page
		/// </summary>
		private const byte LONG_VALUE_TYPE_THIS_PAGE = unchecked((byte)unchecked((int)(0x80
			)));

		/// <summary>
		/// Long value (LVAL) type that indicates that the value is stored on another
		/// page
		/// </summary>
		private const byte LONG_VALUE_TYPE_OTHER_PAGE = unchecked((byte)unchecked((int)(0x40
			)));

		/// <summary>
		/// Long value (LVAL) type that indicates that the value is stored on
		/// multiple other pages
		/// </summary>
		private const byte LONG_VALUE_TYPE_OTHER_PAGES = unchecked((byte)unchecked((int)(
			0x00)));

		/// <summary>
		/// Mask to apply the long length in order to get the flag bits (only the
		/// first 2 bits are type flags).
		/// </summary>
		/// <remarks>
		/// Mask to apply the long length in order to get the flag bits (only the
		/// first 2 bits are type flags).
		/// </remarks>
		private const int LONG_VALUE_TYPE_MASK = unchecked((int)(0xC0000000));

		/// <summary>mask for the fixed len bit</summary>
		/// <usage>_advanced_field_</usage>
		public const byte FIXED_LEN_FLAG_MASK = unchecked((byte)unchecked((int)(0x01)));

		/// <summary>mask for the auto number bit</summary>
		/// <usage>_advanced_field_</usage>
		public const byte AUTO_NUMBER_FLAG_MASK = unchecked((byte)unchecked((int)(0x04)));

		/// <summary>mask for the auto number guid bit</summary>
		/// <usage>_advanced_field_</usage>
		public const byte AUTO_NUMBER_GUID_FLAG_MASK = unchecked((byte)unchecked((int)(0x40
			)));

		/// <summary>mask for the unknown bit (possible "can be null"?)</summary>
		/// <usage>_advanced_field_</usage>
		public const byte UNKNOWN_FLAG_MASK = unchecked((byte)unchecked((int)(0x02)));

		/// <summary>the value for the "general" sort order</summary>
		private const short GENERAL_SORT_ORDER_VALUE = 1033;

		/// <summary>the "general" text sort order, legacy version (access 2000-2007)</summary>
		/// <usage>_intermediate_field_</usage>
		public static readonly Column.SortOrder GENERAL_LEGACY_SORT_ORDER = new Column.SortOrder
			(GENERAL_SORT_ORDER_VALUE, unchecked((byte)0));

		/// <summary>the "general" text sort order, latest version (access 2010+)</summary>
		/// <usage>_intermediate_field_</usage>
		public static readonly Column.SortOrder GENERAL_SORT_ORDER = new Column.SortOrder
			(GENERAL_SORT_ORDER_VALUE, unchecked((byte)1));

		/// <summary>
		/// pattern matching textual guid strings (allows for optional surrounding
		/// '{' and '}')
		/// </summary>
		private static readonly Sharpen.Pattern GUID_PATTERN = Sharpen.Pattern.Compile("([a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}){1}"); //"\\s *[{]?([\\p{XDigit}]{8})-([\\p{XDigit}]{4})-([\\p{XDigit}]{4})-([\\p{XDigit}]{4})-([\\p{XDigit}]{12})[}]?\\s*");

		/// <summary>header used to indicate unicode text compression</summary>
		private static readonly byte[] TEXT_COMPRESSION_HEADER = new byte[] { unchecked((
			byte)unchecked((int)(0xFF))), unchecked((byte)0XFE) };

		/// <summary>placeholder for column which is not numeric</summary>
		private static readonly Column.NumericInfo DEFAULT_NUMERIC_INFO = new Column.NumericInfo
			();

		/// <summary>placeholder for column which is not textual</summary>
		private static readonly Column.TextInfo DEFAULT_TEXT_INFO = new Column.TextInfo();

		/// <summary>owning table</summary>
		private readonly Table _table;

		/// <summary>Whether or not the column is of variable length</summary>
		private bool _variableLength;

		/// <summary>Whether or not the column is an autonumber column</summary>
		private bool _autoNumber;

		/// <summary>Data type</summary>
		private DataType? _type;

		private DataTypeProperties _typeProperties;

		/// <summary>Maximum column length</summary>
		private short _columnLength;

		/// <summary>0-based column number</summary>
		private short _columnNumber;

		/// <summary>index of the data for this column within a list of row data</summary>
		private int _columnIndex;

		/// <summary>display index of the data for this column</summary>
		private int _displayIndex;

		/// <summary>Column name</summary>
		private string _name;

		/// <summary>the offset of the fixed data in the row</summary>
		private int _fixedDataOffset;

		/// <summary>the index of the variable length data in the var len offset table</summary>
		private int _varLenTableIndex;

		/// <summary>information specific to numeric columns</summary>
		private Column.NumericInfo _numericInfo = DEFAULT_NUMERIC_INFO;

		/// <summary>information specific to text columns</summary>
		private Column.TextInfo _textInfo = DEFAULT_TEXT_INFO;

		/// <summary>the auto number generator for this column (if autonumber column)</summary>
		private Column.AutoNumberGenerator _autoNumberGenerator;

		/// <summary>properties for this column, if any</summary>
		private PropertyMap _props;

		/// <usage>_general_method_</usage>
		public Column() : this(null)
		{
		}

		/// <usage>_advanced_method_</usage>
		public Column(JetFormat format)
		{
			// some other flags?
			// 0x10: replication related field (or hidden?)
			// 0x80: hyperlink (some memo based thing)
			_table = null;
		}

		/// <summary>Only used by unit tests</summary>
		internal Column(bool testing, Table table)
		{
			if (!testing)
			{
				throw new ArgumentException();
			}
			_table = table;
		}

		/// <summary>Read a column definition in from a buffer</summary>
		/// <param name="table">owning table</param>
		/// <param name="buffer">Buffer containing column definition</param>
		/// <param name="offset">Offset in the buffer at which the column definition starts</param>
		/// <usage>_advanced_method_</usage>
		/// <exception cref="System.IO.IOException"></exception>
		public Column(Table table, ByteBuffer buffer, int offset, int displayIndex)
		{
			_table = table;
			_displayIndex = displayIndex;
			byte colType = buffer.Get(offset + GetFormat().OFFSET_COLUMN_TYPE);
			_columnNumber = buffer.GetShort(offset + GetFormat().OFFSET_COLUMN_NUMBER);
			_columnLength = buffer.GetShort(offset + GetFormat().OFFSET_COLUMN_LENGTH);
			byte flags = buffer.Get(offset + GetFormat().OFFSET_COLUMN_FLAGS);
			_variableLength = ((flags & FIXED_LEN_FLAG_MASK) == 0);
			_autoNumber = ((flags & (AUTO_NUMBER_FLAG_MASK | AUTO_NUMBER_GUID_FLAG_MASK)) != 
				0);
			try
			{
				_type = DataTypeUtil.FromByte(colType);
			}
			catch (IOException)
			{
				System.Console.Error.WriteLine("Unsupported column type " + colType);
				_type = (_variableLength ? DataType.UNSUPPORTED_VARLEN : DataType.UNSUPPORTED_FIXEDLEN
					);
				SetUnknownDataType(colType);
			}
			_typeProperties = DataTypeProperties.Get(_type);
			if (_typeProperties.hasScalePrecision)
			{
				ModifyNumericInfo();
				_numericInfo._precision = buffer.Get(offset + GetFormat().OFFSET_COLUMN_PRECISION
					);
				_numericInfo._scale = buffer.Get(offset + GetFormat().OFFSET_COLUMN_SCALE);
			}
			else
			{
				if (DataTypeUtil.IsTextual(_type))
				{
					ModifyTextInfo();
					// co-located w/ precision/scale
					_textInfo._sortOrder = ReadSortOrder(buffer, offset + GetFormat().OFFSET_COLUMN_SORT_ORDER
						, GetFormat());
					int cpOffset = GetFormat().OFFSET_COLUMN_CODE_PAGE;
					if (cpOffset >= 0)
					{
						_textInfo._codePage = buffer.GetShort(offset + cpOffset);
					}
					_textInfo._compressedUnicode = ((buffer.Get(offset + GetFormat().OFFSET_COLUMN_COMPRESSED_UNICODE
						) & 1) == 1);
				}
			}
			SetAutoNumberGenerator();
			if (_variableLength)
			{
				_varLenTableIndex = buffer.GetShort(offset + GetFormat().OFFSET_COLUMN_VARIABLE_TABLE_INDEX
					);
			}
			else
			{
				_fixedDataOffset = buffer.GetShort(offset + GetFormat().OFFSET_COLUMN_FIXED_DATA_OFFSET
					);
			}
		}

		/// <usage>_general_method_</usage>
		public virtual Table GetTable()
		{
			return _table;
		}

		/// <usage>_general_method_</usage>
		public virtual Database GetDatabase()
		{
			return GetTable().GetDatabase();
		}

		/// <usage>_advanced_method_</usage>
		public virtual JetFormat GetFormat()
		{
			return GetDatabase().GetFormat();
		}

		/// <usage>_advanced_method_</usage>
		public virtual PageChannel GetPageChannel()
		{
			return GetDatabase().GetPageChannel();
		}

		/// <usage>_general_method_</usage>
		public virtual string GetName()
		{
			return _name;
		}

		/// <usage>_advanced_method_</usage>
		public virtual void SetName(string name)
		{
			_name = name;
		}

		/// <usage>_advanced_method_</usage>
		public virtual bool IsVariableLength()
		{
			return _variableLength;
		}

		/// <usage>_advanced_method_</usage>
		public virtual void SetVariableLength(bool variableLength)
		{
			_variableLength = variableLength;
		}

		/// <usage>_general_method_</usage>
		public virtual bool IsAutoNumber()
		{
			return _autoNumber;
		}

		/// <usage>_general_method_</usage>
		public virtual void SetAutoNumber(bool autoNumber)
		{
			_autoNumber = autoNumber;
			SetAutoNumberGenerator();
		}

		/// <usage>_advanced_method_</usage>
		public virtual short GetColumnNumber()
		{
			return _columnNumber;
		}

		/// <usage>_advanced_method_</usage>
		public virtual void SetColumnNumber(short newColumnNumber)
		{
			_columnNumber = newColumnNumber;
		}

		/// <usage>_advanced_method_</usage>
		public virtual int GetColumnIndex()
		{
			return _columnIndex;
		}

		/// <usage>_advanced_method_</usage>
		public virtual void SetColumnIndex(int newColumnIndex)
		{
			_columnIndex = newColumnIndex;
		}

		/// <usage>_advanced_method_</usage>
		public virtual int GetDisplayIndex()
		{
			return _displayIndex;
		}

		/// <summary>
		/// Also sets the length and the variable length flag, inferred from the
		/// type.
		/// </summary>
		/// <remarks>
		/// Also sets the length and the variable length flag, inferred from the
		/// type.  For types with scale/precision, sets the scale and precision to
		/// default values.
		/// </remarks>
		/// <usage>_general_method_</usage>
		public virtual void SetType(DataType type)
		{
			_type = type;
			DataTypeProperties prop = DataTypeProperties.Get(type);
			if (!prop.variableLength)
			{
				SetLength((short)DataTypeUtil.GetFixedSize(type));
			}
			else
			{
				if (!prop.longValue)
				{
					SetLength((short)prop.defaultSize);
				}
			}
			SetVariableLength(prop.variableLength);
			if (prop.hasScalePrecision)
			{
				SetScale(unchecked((byte)prop.defaultScale));
				SetPrecision(unchecked((byte)prop.defaultPrecision));
			}
		}

		/// <usage>_general_method_</usage>
		public virtual DataType GetDataType()
		{
			if (!_type.HasValue)
				throw new ArgumentException("must have type");

			return _type.Value;
		}

		/// <usage>_general_method_</usage>
		/// <exception cref="Sharpen.SQLException"></exception>
		public virtual int? GetSQLType()
		{
			return _typeProperties.sqlType;
		}

		/// <usage>_general_method_</usage>
		/// <exception cref="Sharpen.SQLException"></exception>
		public virtual void SetSQLType(int type)
		{
			SetSQLType(type, 0);
		}

		/// <usage>_general_method_</usage>
		/// <exception cref="Sharpen.SQLException"></exception>
		public virtual void SetSQLType(int type, int lengthInUnits)
		{
			SetType(DataTypeUtil.FromSQLType(type, lengthInUnits));
		}

		/// <usage>_general_method_</usage>
		public virtual bool IsCompressedUnicode()
		{
			return _textInfo._compressedUnicode;
		}

		/// <usage>_general_method_</usage>
		public virtual void SetCompressedUnicode(bool newCompessedUnicode)
		{
			ModifyTextInfo();
			_textInfo._compressedUnicode = newCompessedUnicode;
		}

		/// <usage>_general_method_</usage>
		public virtual byte GetPrecision()
		{
			return _numericInfo._precision;
		}

		/// <usage>_general_method_</usage>
		public virtual void SetPrecision(byte newPrecision)
		{
			ModifyNumericInfo();
			_numericInfo._precision = newPrecision;
		}

		/// <usage>_general_method_</usage>
		public virtual byte GetScale()
		{
			return _numericInfo._scale;
		}

		/// <usage>_general_method_</usage>
		public virtual void SetScale(byte newScale)
		{
			ModifyNumericInfo();
			_numericInfo._scale = newScale;
		}

		/// <usage>_intermediate_method_</usage>
		public virtual Column.SortOrder GetTextSortOrder()
		{
			return _textInfo._sortOrder;
		}

		/// <usage>_advanced_method_</usage>
		public virtual void SetTextSortOrder(Column.SortOrder newTextSortOrder)
		{
			ModifyTextInfo();
			_textInfo._sortOrder = newTextSortOrder;
		}

		/// <usage>_intermediate_method_</usage>
		public virtual short GetTextCodePage()
		{
			return _textInfo._codePage;
		}

		/// <usage>_general_method_</usage>
		public virtual void SetLength(short length)
		{
			_columnLength = length;
		}

		/// <usage>_general_method_</usage>
		public virtual short GetLength()
		{
			return _columnLength;
		}

		/// <usage>_general_method_</usage>
		public virtual void SetLengthInUnits(short unitLength)
		{
			SetLength((short)DataTypeUtil.FromUnitSize(GetDataType(), unitLength));
		}

		/// <usage>_general_method_</usage>
		public virtual short GetLengthInUnits()
		{
			return (short)DataTypeUtil.ToUnitSize(GetDataType(), GetLength());
		}

		/// <usage>_advanced_method_</usage>
		public virtual void SetVarLenTableIndex(int idx)
		{
			_varLenTableIndex = idx;
		}

		/// <usage>_advanced_method_</usage>
		public virtual int GetVarLenTableIndex()
		{
			return _varLenTableIndex;
		}

		/// <usage>_advanced_method_</usage>
		public virtual void SetFixedDataOffset(int newOffset)
		{
			_fixedDataOffset = newOffset;
		}

		/// <usage>_advanced_method_</usage>
		public virtual int GetFixedDataOffset()
		{
			return _fixedDataOffset;
		}

		protected internal virtual Encoding GetCharset()
		{
			return GetDatabase().GetCharset();
		}

		protected internal virtual TimeZoneInfo GetTimeZone()
		{
			return GetDatabase().GetTimeZone();
		}

		private void SetUnknownDataType(byte type)
		{
			// slight hack, stash the original type in the _scale
			ModifyNumericInfo();
			_numericInfo._scale = type;
		}

		private byte GetUnknownDataType()
		{
			// slight hack, we stashed the real type in the _scale
			return _numericInfo._scale;
		}

		private void SetAutoNumberGenerator()
		{
			if (!_autoNumber || (_type == null))
			{
				_autoNumberGenerator = null;
				return;
			}
			if ((_autoNumberGenerator != null) && (_autoNumberGenerator.GetDataType() == _type))
			{
				// keep existing
				return;
			}
			switch (_type)
			{
				case DataType.LONG:
				{
					_autoNumberGenerator = new Column.LongAutoNumberGenerator(this);
					break;
				}

				case DataType.GUID:
				{
					_autoNumberGenerator = new Column.GuidAutoNumberGenerator(this);
					break;
				}

				default:
				{
					System.Console.Error.WriteLine("Unknown auto number column type " + _type);
					_autoNumberGenerator = new Column.UnsupportedAutoNumberGenerator(this, _type);
					break;
				}
			}
		}

		/// <summary>
		/// Returns the AutoNumberGenerator for this column if this is an autonumber
		/// column,
		/// <code>null</code>
		/// otherwise.
		/// </summary>
		/// <usage>_advanced_method_</usage>
		public virtual Column.AutoNumberGenerator GetAutoNumberGenerator()
		{
			return _autoNumberGenerator;
		}

		/// <returns>the properties for this column</returns>
		/// <usage>_general_method_</usage>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual PropertyMap GetProperties()
		{
			if (_props == null)
			{
				_props = GetTable().GetPropertyMaps().Get(GetName());
			}
			return _props;
		}

		private void ModifyNumericInfo()
		{
			if (_numericInfo == DEFAULT_NUMERIC_INFO)
			{
				_numericInfo = new Column.NumericInfo();
			}
		}

		private void ModifyTextInfo()
		{
			if (_textInfo == DEFAULT_TEXT_INFO)
			{
				_textInfo = new Column.TextInfo();
			}
		}

		/// <summary>Checks that this column definition is valid.</summary>
		/// <remarks>Checks that this column definition is valid.</remarks>
		/// <exception cref="System.ArgumentException">if this column definition is invalid.</exception>
		/// <usage>_advanced_method_</usage>
		public virtual void Validate(JetFormat format)
		{
			DataType t = GetDataType();
			Database.ValidateIdentifierName(GetName(), format.MAX_COLUMN_NAME_LENGTH, "column"
				);
			if (DataTypeUtil.IsUnsupported(t))
			{
				throw new ArgumentException("Cannot create column with unsupported type " + t);
			}
			if (IsVariableLength() != _typeProperties.variableLength)
			{
				throw new ArgumentException("invalid variable length setting");
			}
			if (!IsVariableLength())
			{
				int fixedSize = DataTypeUtil.GetFixedSize(t);
				if (GetLength() != fixedSize)
				{
					if (GetLength() < fixedSize)
					{
						throw new ArgumentException("invalid fixed length size");
					}
					System.Console.Error.WriteLine("Column length " + GetLength() + " longer than expected fixed size "
						 + fixedSize);
				}
			}
			else
			{
				if (!_typeProperties.longValue)
				{
					if (!DataTypeUtil.IsValidSize(t, GetLength()))
					{
						throw new ArgumentException("var length out of range");
					}
				}
			}
			if (_typeProperties.hasScalePrecision)
			{
				if (!DataTypeUtil.IsValidScale(t, GetScale()))
				{
					throw new ArgumentException("Scale must be from " + _typeProperties.minScale + " to "
						 + _typeProperties.maxScale + " inclusive");
				}
				if (!DataTypeUtil.IsValidPrecision(t, GetPrecision()))
				{
					throw new ArgumentException("Precision must be from " + _typeProperties.minPrecision
						 + " to " + _typeProperties.maxPrecision + " inclusive");
				}
			}
			if (IsAutoNumber())
			{
				if (!DataTypeUtil.MayBeAutoNumber(t))
				{
					throw new ArgumentException("Auto number column must be long integer or guid");
				}
			}
			if (IsCompressedUnicode())
			{
				if (!DataTypeUtil.IsTextual(t))
				{
					throw new ArgumentException("Only textual columns allow unicode compression (text/memo)"
						);
				}
			}
		}

		/// <summary>Deserialize a raw byte value for this column into an Object</summary>
		/// <param name="data">The raw byte value</param>
		/// <returns>The deserialized Object</returns>
		/// <usage>_advanced_method_</usage>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual object Read(byte[] data)
		{
			return Read(data, PageChannel.DEFAULT_BYTE_ORDER);
		}

		/// <summary>Deserialize a raw byte value for this column into an Object</summary>
		/// <param name="data">The raw byte value</param>
		/// <param name="order">Byte order in which the raw value is stored</param>
		/// <returns>The deserialized Object</returns>
		/// <usage>_advanced_method_</usage>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual object Read(byte[] data, ByteOrder order)
		{
			ByteBuffer buffer = ByteBuffer.Wrap(data);
			buffer.Order(order);
			if (_type == DataType.BOOLEAN)
			{
				throw new IOException("Tried to read a boolean from data instead of null mask.");
			}
			else
			{
				if (_type == DataType.BYTE)
				{
					return Sharpen.Extensions.ValueOf(buffer.Get());
				}
				else
				{
					if (_type == DataType.INT)
					{
						return Sharpen.Extensions.ValueOf(buffer.GetShort());
					}
					else
					{
						if (_type == DataType.LONG)
						{
							return Sharpen.Extensions.ValueOf(buffer.GetInt());
						}
						else
						{
							if (_type == DataType.DOUBLE)
							{
								return Sharpen.Extensions.ValueOf(buffer.GetDouble());
							}
							else
							{
								if (_type == DataType.FLOAT)
								{
									return Sharpen.Extensions.ValueOf(buffer.GetFloat());
								}
								else
								{
									if (_type == DataType.SHORT_DATE_TIME)
									{
										return ReadDateValue(buffer);
									}
									else
									{
										if (_type == DataType.BINARY)
										{
											return data;
										}
										else
										{
											if (_type == DataType.TEXT)
											{
												return DecodeTextValue(data);
											}
											else
											{
												if (_type == DataType.MONEY)
												{
													return ReadCurrencyValue(buffer);
												}
												else
												{
													if (_type == DataType.OLE)
													{
														if (data.Length > 0)
														{
															return ReadLongValue(data);
														}
														return null;
													}
													else
													{
														if (_type == DataType.MEMO)
														{
															if (data.Length > 0)
															{
																return ReadLongStringValue(data);
															}
															return null;
														}
														else
														{
															if (_type == DataType.NUMERIC)
															{
																return ReadNumericValue(buffer);
															}
															else
															{
																if (_type == DataType.GUID)
																{
																	return ReadGUIDValue(buffer, order);
																}
																else
																{
																	if ((_type == DataType.UNKNOWN_0D) || (_type == DataType.UNKNOWN_11))
																	{
																		// treat like "binary" data
																		return data;
																	}
																	else
																	{
																		if (DataTypeUtil.IsUnsupported(_type))
																		{
																			return RawDataWrapper(data);
																		}
																		else
																		{
																			throw new IOException("Unrecognized data type: " + _type);
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		/// <param name="lvalDefinition">Column value that points to an LVAL record</param>
		/// <returns>The LVAL data</returns>
		/// <exception cref="System.IO.IOException"></exception>
		private byte[] ReadLongValue(byte[] lvalDefinition)
		{
			ByteBuffer def = ByteBuffer.Wrap(lvalDefinition).Order(PageChannel.DEFAULT_BYTE_ORDER
				);
			int lengthWithFlags = def.GetInt();
			int length = lengthWithFlags & (~LONG_VALUE_TYPE_MASK);
			byte[] rtn = new byte[length];
			byte type = unchecked((byte)((int)(((uint)(lengthWithFlags & LONG_VALUE_TYPE_MASK
				)) >> 24)));
			if (type == LONG_VALUE_TYPE_THIS_PAGE)
			{
				// inline long value
				def.GetInt();
				//Skip over lval_dp
				def.GetInt();
				//Skip over unknown
				def.Get(rtn);
			}
			else
			{
				// long value on other page(s)
				if (lvalDefinition.Length != GetFormat().SIZE_LONG_VALUE_DEF)
				{
					throw new IOException("Expected " + GetFormat().SIZE_LONG_VALUE_DEF + " bytes in long value definition, but found "
						 + lvalDefinition.Length);
				}
				int rowNum = ByteUtil.GetUnsignedByte(def);
				int pageNum = ByteUtil.Get3ByteInt(def, def.Position());
				ByteBuffer lvalPage = GetPageChannel().CreatePageBuffer();
				switch (type)
				{
					case LONG_VALUE_TYPE_OTHER_PAGE:
					{
						GetPageChannel().ReadPage(lvalPage, pageNum);
						short rowStart = Table.FindRowStart(lvalPage, rowNum, GetFormat());
						short rowEnd = Table.FindRowEnd(lvalPage, rowNum, GetFormat());
						if ((rowEnd - rowStart) != length)
						{
							throw new IOException("Unexpected lval row length");
						}
						lvalPage.Position(rowStart);
						lvalPage.Get(rtn);
						break;
					}

					case LONG_VALUE_TYPE_OTHER_PAGES:
					{
						ByteBuffer rtnBuf = ByteBuffer.Wrap(rtn);
						int remainingLen = length;
						while (remainingLen > 0)
						{
							lvalPage.Clear();
							GetPageChannel().ReadPage(lvalPage, pageNum);
							short rowStart = Table.FindRowStart(lvalPage, rowNum, GetFormat());
							short rowEnd = Table.FindRowEnd(lvalPage, rowNum, GetFormat());
							// read next page information
							lvalPage.Position(rowStart);
							rowNum = ByteUtil.GetUnsignedByte(lvalPage);
							pageNum = ByteUtil.Get3ByteInt(lvalPage);
							// update rowEnd and remainingLen based on chunkLength
							int chunkLength = (rowEnd - rowStart) - 4;
							if (chunkLength > remainingLen)
							{
								rowEnd = (short)(rowEnd - (chunkLength - remainingLen));
								chunkLength = remainingLen;
							}
							remainingLen -= chunkLength;
							lvalPage.Limit(rowEnd);
							rtnBuf.Put(lvalPage.Array());
						}
						break;
					}

					default:
					{
						throw new IOException("Unrecognized long value type: " + type);
					}
				}
			}
			return rtn;
		}

		/// <param name="lvalDefinition">Column value that points to an LVAL record</param>
		/// <returns>The LVAL data</returns>
		/// <exception cref="System.IO.IOException"></exception>
		private string ReadLongStringValue(byte[] lvalDefinition)
		{
			byte[] binData = ReadLongValue(lvalDefinition);
			if (binData == null)
			{
				return null;
			}
			return DecodeTextValue(binData);
		}

		/// <summary>Decodes "Currency" values.</summary>
		/// <remarks>Decodes "Currency" values.</remarks>
		/// <param name="buffer">Column value that points to currency data</param>
		/// <returns>BigDecimal representing the monetary value</returns>
		/// <exception cref="System.IO.IOException">if the value cannot be parsed</exception>
		private static BigDecimal ReadCurrencyValue(ByteBuffer buffer)
		{
			if (buffer.Remaining() != 8)
			{
				throw new IOException("Invalid money value.");
			}
			return new BigDecimal(Sharpen.Extensions.ValueOf(buffer.GetLong(0)), 4);
		}

		/// <summary>Writes "Currency" values.</summary>
		/// <remarks>Writes "Currency" values.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private static void WriteCurrencyValue(ByteBuffer buffer, object value)
		{
			try
			{
				BigDecimal decVal = ToBigDecimal(value);
				// adjust scale (will cause the an ArithmeticException if number has too
				// many decimal places)
				decVal = decVal.SetScale(4);
				// now, remove scale and convert to long (this will throw if the value is
				// too big)
				buffer.PutLong(decVal.MovePointRight(4).LongValueExact());
			}
			catch (ArithmeticException e)
			{
				throw new IOException("Currency value out of range", e);
			}
		}

		/// <summary>Decodes a NUMERIC field.</summary>
		/// <remarks>Decodes a NUMERIC field.</remarks>
		private BigDecimal ReadNumericValue(ByteBuffer buffer)
		{
			bool negate = (buffer.Get() != 0);
			byte[] tmpArr = new byte[16];
			buffer.Get(tmpArr);
			if (buffer.Order() != ByteOrder.BIG_ENDIAN)
			{
				FixNumericByteOrder(tmpArr);
			}
			BigInteger intVal = new BigInteger(tmpArr);
			if (negate)
			{
				intVal = BigInteger.Negate(intVal);
			}
			return new BigDecimal(intVal, GetScale());
		}

		/// <summary>Writes a numeric value.</summary>
		/// <remarks>Writes a numeric value.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void WriteNumericValue(ByteBuffer buffer, object value)
		{
			try
			{
				BigDecimal decVal = ToBigDecimal(value);
				bool negative = (decVal.CompareTo(BigDecimal.ZERO) < 0);
				if (negative)
				{
					decVal = decVal.Negate();
				}
				// write sign byte
				buffer.Put(negative ? unchecked((byte)unchecked((int)(0x80))) : unchecked((byte)0
					));
				// adjust scale according to this column type (will cause the an
				// ArithmeticException if number has too many decimal places)
				decVal = decVal.SetScale(GetScale());
				// check precision
				if (decVal.Precision() > GetPrecision())
				{
					throw new IOException("Numeric value is too big for specified precision " + GetPrecision
						() + ": " + decVal);
				}
				// convert to unscaled BigInteger, big-endian bytes
				byte[] intValBytes = decVal.UnscaledValue().ToByteArray();
				int maxByteLen = DataTypeUtil.GetFixedSize(GetDataType()) - 1;
				if (intValBytes.Length > maxByteLen)
				{
					throw new IOException("Too many bytes for valid BigInteger?");
				}
				if (intValBytes.Length < maxByteLen)
				{
					byte[] tmpBytes = new byte[maxByteLen];
					System.Array.Copy(intValBytes, 0, tmpBytes, (maxByteLen - intValBytes.Length), intValBytes
						.Length);
					intValBytes = tmpBytes;
				}
				if (buffer.Order() != ByteOrder.BIG_ENDIAN)
				{
					FixNumericByteOrder(intValBytes);
				}
				buffer.Put(intValBytes);
			}
			catch (ArithmeticException e)
			{
				throw new IOException("Numeric value out of range", e);
			}
		}

		/// <summary>Decodes a date value.</summary>
		/// <remarks>Decodes a date value.</remarks>
		private Column.Date ReadDateValue(ByteBuffer buffer)
		{
			// seems access stores dates in the local timezone.  guess you just hope
			// you read it in the same timezone in which it was written!
			long dateBits = buffer.GetLong();
			long time = FromDateDouble(BitConverter.Int64BitsToDouble(dateBits));
			return new Column.Date(time, dateBits);
		}

		/// <summary>Returns a java long time value converted from an access date double.</summary>
		/// <remarks>Returns a java long time value converted from an access date double.</remarks>
		private long FromDateDouble(double value)
		{
			long time = (long)Math.Round(value * MILLISECONDS_PER_DAY);
			time -= MILLIS_BETWEEN_EPOCH_AND_1900;
			time -= GetTimeZoneOffset(time);
			return time;
		}

		/// <summary>Writes a date value.</summary>
		/// <remarks>Writes a date value.</remarks>
		private void WriteDateValue(ByteBuffer buffer, object value)
		{
			if (value == null)
			{
				buffer.PutDouble(0d);
			}
			else
			{
				if (value is Column.Date)
				{
					// this is a Date value previously read from readDateValue().  use the
					// original bits to store the value so we don't lose any precision
					buffer.PutLong(((Column.Date)value).GetDateBits());
				}
				else
				{
					buffer.PutDouble(ToDateDouble(value));
				}
			}
		}

		/// <summary>
		/// Returns an access date double converted from a java Date/Calendar/Number
		/// time value.
		/// </summary>
		/// <remarks>
		/// Returns an access date double converted from a java Date/Calendar/Number
		/// time value.
		/// </remarks>
		private double ToDateDouble(object value)
		{
			// seems access stores dates in the local timezone.  guess you just
			// hope you read it in the same timezone in which it was written!
			long time = value is DateTime ? ((DateTime)value).GetTime() : ((Number)value).LongValue();
			time += GetTimeZoneOffset(time);
			time += MILLIS_BETWEEN_EPOCH_AND_1900;
			return time / MILLISECONDS_PER_DAY;
		}

		/// <summary>Gets the timezone offset from UTC for the given time (including DST).</summary>
		/// <remarks>Gets the timezone offset from UTC for the given time (including DST).</remarks>
		private long GetTimeZoneOffset(long time)
		{
			return 0L; // (long)TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.FromUnixTimeMilliseconds(time).DateTime).TotalMilliseconds;
		}

		/// <summary>Decodes a GUID value.</summary>
		/// <remarks>Decodes a GUID value.</remarks>
		private static string ReadGUIDValue(ByteBuffer buffer, ByteOrder order)
		{
			if (order != ByteOrder.BIG_ENDIAN)
			{
				byte[] tmpArr = new byte[16];
				buffer.Get(tmpArr);
				// the first 3 guid components are integer components which need to
				// respect endianness, so swap 4-byte int, 2-byte int, 2-byte int
				ByteUtil.Swap4Bytes(tmpArr, 0);
				ByteUtil.Swap2Bytes(tmpArr, 4);
				ByteUtil.Swap2Bytes(tmpArr, 6);
				buffer = ByteBuffer.Wrap(tmpArr);
			}
			StringBuilder sb = new StringBuilder(22);
			sb.Append("{");
			sb.Append(ByteUtil.ToHexString(buffer, 0, 4, false));
			sb.Append("-");
			sb.Append(ByteUtil.ToHexString(buffer, 4, 2, false));
			sb.Append("-");
			sb.Append(ByteUtil.ToHexString(buffer, 6, 2, false));
			sb.Append("-");
			sb.Append(ByteUtil.ToHexString(buffer, 8, 2, false));
			sb.Append("-");
			sb.Append(ByteUtil.ToHexString(buffer, 10, 6, false));
			sb.Append("}");
			return (sb.ToString());
		}

		/// <summary>Writes a GUID value.</summary>
		/// <remarks>Writes a GUID value.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private static void WriteGUIDValue(ByteBuffer buffer, object value, ByteOrder order
			)
		{
			Matcher m = GUID_PATTERN.Matcher(ToCharSequence(value).ToString());
			if (m.Matches())
			{
				ByteBuffer origBuffer = null;
				byte[] tmpBuf = null;
				if (order != ByteOrder.BIG_ENDIAN)
				{
					// write to a temp buf so we can do some swapping below
					origBuffer = buffer;
					tmpBuf = new byte[16];
					buffer = ByteBuffer.Wrap(tmpBuf);
				}
				ByteUtil.WriteHexString(buffer, m.Group(1));
				ByteUtil.WriteHexString(buffer, m.Group(2));
				ByteUtil.WriteHexString(buffer, m.Group(3));
				ByteUtil.WriteHexString(buffer, m.Group(4));
				ByteUtil.WriteHexString(buffer, m.Group(5));
				if (tmpBuf != null)
				{
					// the first 3 guid components are integer components which need to
					// respect endianness, so swap 4-byte int, 2-byte int, 2-byte int
					ByteUtil.Swap4Bytes(tmpBuf, 0);
					ByteUtil.Swap2Bytes(tmpBuf, 4);
					ByteUtil.Swap2Bytes(tmpBuf, 6);
					origBuffer.Put(tmpBuf);
				}
			}
			else
			{
				throw new IOException("Invalid GUID: " + value);
			}
		}

		/// <summary>
		/// Write an LVAL column into a ByteBuffer inline if it fits, otherwise in
		/// other data page(s).
		/// </summary>
		/// <remarks>
		/// Write an LVAL column into a ByteBuffer inline if it fits, otherwise in
		/// other data page(s).
		/// </remarks>
		/// <param name="value">Value of the LVAL column</param>
		/// <returns>
		/// A buffer containing the LVAL definition and (possibly) the column
		/// value (unless written to other pages)
		/// </returns>
		/// <usage>_advanced_method_</usage>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual ByteBuffer WriteLongValue(byte[] value, int remainingRowLength)
		{
			if (value.Length > _typeProperties.maxSize)
			{
				throw new IOException("value too big for column, max " + _typeProperties.maxSize 
					+ ", got " + value.Length);
			}
			// determine which type to write
			byte type = 0;
			int lvalDefLen = GetFormat().SIZE_LONG_VALUE_DEF;
			if (((GetFormat().SIZE_LONG_VALUE_DEF + value.Length) <= remainingRowLength) && (
				value.Length <= GetFormat().MAX_INLINE_LONG_VALUE_SIZE))
			{
				type = LONG_VALUE_TYPE_THIS_PAGE;
				lvalDefLen += value.Length;
			}
			else
			{
				if (value.Length <= GetFormat().MAX_LONG_VALUE_ROW_SIZE)
				{
					type = LONG_VALUE_TYPE_OTHER_PAGE;
				}
				else
				{
					type = LONG_VALUE_TYPE_OTHER_PAGES;
				}
			}
			ByteBuffer def = GetPageChannel().CreateBuffer(lvalDefLen);
			// take length and apply type to first byte
			int lengthWithFlags = value.Length | (type << 24);
			def.PutInt(lengthWithFlags);
			if (type == LONG_VALUE_TYPE_THIS_PAGE)
			{
				// write long value inline
				def.PutInt(0);
				def.PutInt(0);
				//Unknown
				def.Put(value);
			}
			else
			{
				TempPageHolder lvalBufferH = GetTable().GetLongValueBuffer();
				ByteBuffer lvalPage = null;
				int firstLvalPageNum = PageChannel.INVALID_PAGE_NUMBER;
				byte firstLvalRow = 0;
				switch (type)
				{
					case LONG_VALUE_TYPE_OTHER_PAGE:
					{
						// write other page(s)
						lvalPage = GetLongValuePage(value.Length, lvalBufferH);
						firstLvalPageNum = lvalBufferH.GetPageNumber();
						firstLvalRow = unchecked((byte)Table.AddDataPageRow(lvalPage, value.Length, GetFormat
							(), 0));
						lvalPage.Put(value);
						GetPageChannel().WritePage(lvalPage, firstLvalPageNum);
						break;
					}

					case LONG_VALUE_TYPE_OTHER_PAGES:
					{
						ByteBuffer buffer = ByteBuffer.Wrap(value);
						int remainingLen = buffer.Remaining();
						buffer.Limit(0);
						lvalPage = GetLongValuePage(GetFormat().MAX_LONG_VALUE_ROW_SIZE, lvalBufferH);
						firstLvalPageNum = lvalBufferH.GetPageNumber();
						int lvalPageNum = firstLvalPageNum;
						ByteBuffer nextLvalPage = null;
						int nextLvalPageNum = 0;
						while (remainingLen > 0)
						{
							lvalPage.Clear();
							// figure out how much we will put in this page (we need 4 bytes for
							// the next page pointer)
							int chunkLength = Math.Min(GetFormat().MAX_LONG_VALUE_ROW_SIZE - 4, remainingLen);
							// figure out if we will need another page, and if so, allocate it
							if (chunkLength < remainingLen)
							{
								// force a new page to be allocated
								lvalBufferH.Clear();
								nextLvalPage = GetLongValuePage(GetFormat().MAX_LONG_VALUE_ROW_SIZE, lvalBufferH);
								nextLvalPageNum = lvalBufferH.GetPageNumber();
							}
							else
							{
								nextLvalPage = null;
								nextLvalPageNum = 0;
							}
							// add row to this page
							byte lvalRow = unchecked((byte)Table.AddDataPageRow(lvalPage, chunkLength + 4, GetFormat
								(), 0));
							// write next page info (we'll always be writing into row 0 for
							// newly created pages)
							lvalPage.Put(unchecked((byte)0));
							// row number
							ByteUtil.Put3ByteInt(lvalPage, nextLvalPageNum);
							// page number
							// write this page's chunk of data
							buffer.Limit(buffer.Limit() + chunkLength);
							lvalPage.Put(buffer);
							remainingLen -= chunkLength;
							// write new page to database
							GetPageChannel().WritePage(lvalPage, lvalPageNum);
							if (lvalPageNum == firstLvalPageNum)
							{
								// save initial row info
								firstLvalRow = lvalRow;
							}
							else
							{
								// check assertion that we wrote to row 0 for all subsequent pages
								if (lvalRow != unchecked((byte)0))
								{
									throw new InvalidOperationException("Expected row 0, but was " + lvalRow);
								}
							}
							// move to next page
							lvalPage = nextLvalPage;
							lvalPageNum = nextLvalPageNum;
						}
						break;
					}

					default:
					{
						throw new IOException("Unrecognized long value type: " + type);
					}
				}
				// update def
				def.Put(firstLvalRow);
				ByteUtil.Put3ByteInt(def, firstLvalPageNum);
				def.PutInt(0);
			}
			//Unknown
			def.Flip();
			return def;
		}

		/// <summary>Writes the header info for a long value page.</summary>
		/// <remarks>Writes the header info for a long value page.</remarks>
		private void WriteLongValueHeader(ByteBuffer lvalPage)
		{
			lvalPage.Put(PageTypes.DATA);
			//Page type
			lvalPage.Put(unchecked((byte)1));
			//Unknown
			lvalPage.PutShort((short)GetFormat().DATA_PAGE_INITIAL_FREE_SPACE);
			//Free space
			lvalPage.Put(unchecked((byte)'L'));
			lvalPage.Put(unchecked((byte)'V'));
			lvalPage.Put(unchecked((byte)'A'));
			lvalPage.Put(unchecked((byte)'L'));
			lvalPage.PutInt(0);
			//unknown
			lvalPage.PutShort((short)0);
		}

		// num rows in page
		/// <summary>Returns a long value data page with space for data of the given length.</summary>
		/// <remarks>Returns a long value data page with space for data of the given length.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private ByteBuffer GetLongValuePage(int dataLength, TempPageHolder lvalBufferH)
		{
			ByteBuffer lvalPage = null;
			if (lvalBufferH.GetPageNumber() != PageChannel.INVALID_PAGE_NUMBER)
			{
				lvalPage = lvalBufferH.GetPage(GetPageChannel());
				if (Table.RowFitsOnDataPage(dataLength, lvalPage, GetFormat()))
				{
					// the current page has space
					return lvalPage;
				}
			}
			// need new page
			lvalPage = lvalBufferH.SetNewPage(GetPageChannel());
			WriteLongValueHeader(lvalPage);
			return lvalPage;
		}

		/// <summary>
		/// Serialize an Object into a raw byte value for this column in little
		/// endian order
		/// </summary>
		/// <param name="obj">Object to serialize</param>
		/// <returns>A buffer containing the bytes</returns>
		/// <usage>_advanced_method_</usage>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual ByteBuffer Write(object obj, int remainingRowLength)
		{
			return Write(obj, remainingRowLength, PageChannel.DEFAULT_BYTE_ORDER);
		}

		/// <summary>Serialize an Object into a raw byte value for this column</summary>
		/// <param name="obj">Object to serialize</param>
		/// <param name="order">Order in which to serialize</param>
		/// <returns>A buffer containing the bytes</returns>
		/// <usage>_advanced_method_</usage>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual ByteBuffer Write(object obj, int remainingRowLength, ByteOrder order
			)
		{
			if (IsRawData(obj))
			{
				// just slap it right in (not for the faint of heart!)
				return ByteBuffer.Wrap(((Column.RawData)obj).GetBytes());
			}
			if (!IsVariableLength() || !_typeProperties.variableLength)
			{
				return WriteFixedLengthField(obj, order);
			}
			// var length column
			if (!_typeProperties.longValue)
			{
				switch (GetDataType())
				{
					case DataType.NUMERIC:
					{
						// this is an "inline" var length field
						// don't ask me why numerics are "var length" columns...
						ByteBuffer buffer = GetPageChannel().CreateBuffer(DataTypeUtil.GetFixedSize(GetDataType
							()), order);
						WriteNumericValue(buffer, obj);
						buffer.Flip();
						return buffer;
					}

					case DataType.TEXT:
					{
						byte[] encodedData = ((byte[])EncodeTextValue(obj, 0, GetLengthInUnits(), false).
							Array());
						obj = encodedData;
						break;
					}

					case DataType.BINARY:
					case DataType.UNKNOWN_0D:
					case DataType.UNSUPPORTED_VARLEN:
					{
						// should already be "encoded"
						break;
					}

					default:
					{
						throw new RuntimeException("unexpected inline var length type: " + GetDataType());
					}
				}
				ByteBuffer buffer_1 = ByteBuffer.Wrap(ToByteArray(obj));
				buffer_1.Order(order);
				return buffer_1;
			}
			switch (GetDataType())
			{
				case DataType.OLE:
				{
					// var length, long value column
					// should already be "encoded"
					break;
				}

				case DataType.MEMO:
				{
					int maxMemoChars = DataTypeUtil.ToUnitSize(DataType.MEMO, DataTypeProperties.MEMO
						.maxSize.Value);
					obj = ((byte[])EncodeTextValue(obj, 0, maxMemoChars, false).Array());
					break;
				}

				default:
				{
					throw new RuntimeException("unexpected var length, long value type: " + GetDataType()
						);
				}
			}
			// create long value buffer
			return WriteLongValue(ToByteArray(obj), remainingRowLength);
		}

		/// <summary>Serialize an Object into a raw byte value for this column</summary>
		/// <param name="obj">Object to serialize</param>
		/// <param name="order">Order in which to serialize</param>
		/// <returns>A buffer containing the bytes</returns>
		/// <usage>_advanced_method_</usage>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual ByteBuffer WriteFixedLengthField(object obj, ByteOrder order)
		{
			int size = DataTypeUtil.GetFixedSize(GetDataType(), _columnLength);
			// create buffer for data
			ByteBuffer buffer = GetPageChannel().CreateBuffer(size, order);
			// since booleans are not written by this method, it's safe to convert any
			// incoming boolean into an integer.
			obj = BooleanToInteger(obj);
			switch (GetDataType())
			{
				case DataType.BOOLEAN:
				{
					//Do nothing
					break;
				}

				case DataType.BYTE:
				{
					buffer.Put(ToNumber(obj).ByteValue());
					break;
				}

				case DataType.INT:
				{
					buffer.PutShort(ToNumber(obj).ShortValue());
					break;
				}

				case DataType.LONG:
				{
					buffer.PutInt(ToNumber(obj).IntValue());
					break;
				}

				case DataType.MONEY:
				{
					WriteCurrencyValue(buffer, obj);
					break;
				}

				case DataType.FLOAT:
				{
					buffer.PutFloat(ToNumber(obj).FloatValue());
					break;
				}

				case DataType.DOUBLE:
				{
					buffer.PutDouble(ToNumber(obj).DoubleValue());
					break;
				}

				case DataType.SHORT_DATE_TIME:
				{
					WriteDateValue(buffer, obj);
					break;
				}

				case DataType.TEXT:
				{
					// apparently text numeric values are also occasionally written as fixed
					// length...
					int numChars = GetLengthInUnits();
					// force uncompressed encoding for fixed length text
					buffer.Put(EncodeTextValue(obj, numChars, numChars, true));
					break;
				}

				case DataType.GUID:
				{
					WriteGUIDValue(buffer, obj, order);
					break;
				}

				case DataType.NUMERIC:
				{
					// yes, that's right, occasionally numeric values are written as fixed
					// length...
					WriteNumericValue(buffer, obj);
					break;
				}

				case DataType.BINARY:
				case DataType.UNKNOWN_0D:
				case DataType.UNKNOWN_11:
				case DataType.UNSUPPORTED_FIXEDLEN:
				{
					byte[] bytes = ToByteArray(obj);
					if (bytes.Length != GetLength())
					{
						throw new IOException("Invalid fixed size binary data, size " + GetLength() + ", got "
							 + bytes.Length);
					}
					buffer.Put(bytes);
					break;
				}

				default:
				{
					throw new IOException("Unsupported data type: " + GetDataType());
				}
			}
			buffer.Flip();
			return buffer;
		}

		/// <summary>Decodes a compressed or uncompressed text value.</summary>
		/// <remarks>Decodes a compressed or uncompressed text value.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private string DecodeTextValue(byte[] data)
		{
			try
			{
				// see if data is compressed.  the 0xFF, 0xFE sequence indicates that
				// compression is used (sort of, see algorithm below)
				bool isCompressed = ((data.Length > 1) && (data[0] == TEXT_COMPRESSION_HEADER[0])
					 && (data[1] == TEXT_COMPRESSION_HEADER[1]));
				if (isCompressed)
				{
					Expand expander = new Expand();
					// this is a whacky compression combo that switches back and forth
					// between compressed/uncompressed using a 0x00 byte (starting in
					// compressed mode)
					StringBuilder textBuf = new StringBuilder(data.Length);
					// start after two bytes indicating compression use
					int dataStart = TEXT_COMPRESSION_HEADER.Length;
					int dataEnd = dataStart;
					bool inCompressedMode = true;
					while (dataEnd < data.Length)
					{
						if (data[dataEnd] == unchecked((byte)unchecked((int)(0x00))))
						{
							// handle current segment
							DecodeTextSegment(data, dataStart, dataEnd, inCompressedMode, expander, textBuf);
							inCompressedMode = !inCompressedMode;
							++dataEnd;
							dataStart = dataEnd;
						}
						else
						{
							++dataEnd;
						}
					}
					// handle last segment
					DecodeTextSegment(data, dataStart, dataEnd, inCompressedMode, expander, textBuf);
					return textBuf.ToString();
				}
				return DecodeUncompressedText(data, GetCharset());
			}
			catch (IllegalInputException e)
			{
				throw new IOException("Can't expand text column", e);
			}
			catch (EndOfInputException e)
			{
				throw new IOException("Can't expand text column", e);
			}
		}

		/// <summary>
		/// Decodes a segnment of a text value into the given buffer according to the
		/// given status of the segment (compressed/uncompressed).
		/// </summary>
		/// <remarks>
		/// Decodes a segnment of a text value into the given buffer according to the
		/// given status of the segment (compressed/uncompressed).
		/// </remarks>
		/// <exception cref="HealthMarketScience.Jackcess.Scsu.IllegalInputException"></exception>
		/// <exception cref="HealthMarketScience.Jackcess.Scsu.EndOfInputException"></exception>
		private void DecodeTextSegment(byte[] data, int dataStart, int dataEnd, bool inCompressedMode
			, Expand expander, StringBuilder textBuf)
		{
			if (dataEnd <= dataStart)
			{
				// no data
				return;
			}
			int dataLength = dataEnd - dataStart;
			if (inCompressedMode)
			{
				// handle compressed data
				byte[] tmpData = ByteUtil.CopyOf(data, dataStart, dataLength);
				expander.Reset();
				textBuf.Append(expander.ExpandArray(tmpData));
			}
			else
			{
				// handle uncompressed data
				textBuf.Append(DecodeUncompressedText(data, dataStart, dataLength, GetCharset()));
			}
		}

		/// <param name="textBytes">bytes of text to decode</param>
		/// <returns>the decoded string</returns>
		private static CharBuffer DecodeUncompressedText(byte[] textBytes, int startPos, 
			int length, Encoding charset)
		{
			return CharBuffer.Wrap(charset.Decode(ByteBuffer.Wrap(textBytes, startPos, length)));
		}

		/// <summary>Encodes a text value, possibly compressing.</summary>
		/// <remarks>Encodes a text value, possibly compressing.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private ByteBuffer EncodeTextValue(object obj, int minChars, int maxChars, bool forceUncompressed
			)
		{
			CharSequence text = ToCharSequence(obj);
			if ((text.Length > maxChars) || (text.Length < minChars))
			{
				throw new IOException("Text is wrong length for " + GetDataType() + " column, max " +
					 maxChars + ", min " + minChars + ", got " + text.Length);
			}
			// may only compress if column type allows it
			if (!forceUncompressed && IsCompressedUnicode())
			{
				// for now, only do very simple compression (only compress text which is
				// all ascii text)
				if (IsAsciiCompressible(text))
				{
					byte[] encodedChars = new byte[TEXT_COMPRESSION_HEADER.Length + text.Length];
					encodedChars[0] = TEXT_COMPRESSION_HEADER[0];
					encodedChars[1] = TEXT_COMPRESSION_HEADER[1];
					for (int i = 0; i < text.Length; ++i)
					{
						encodedChars[i + TEXT_COMPRESSION_HEADER.Length] = unchecked((byte)text[i]);
					}
					return ByteBuffer.Wrap(encodedChars);
				}
			}
			return EncodeUncompressedText(text, GetCharset());
		}

		/// <summary>
		/// Returns
		/// <code>true</code>
		/// if the given text can be compressed using simple
		/// ASCII encoding,
		/// <code>false</code>
		/// otherwise.
		/// </summary>
		private static bool IsAsciiCompressible(CharSequence text)
		{
			// only attempt to compress > 2 chars (compressing less than 3 chars would
			// not result in a space savings due to the 2 byte compression header)
			if (text.Length <= TEXT_COMPRESSION_HEADER.Length)
			{
				return false;
			}
			// now, see if it is all printable ASCII
			for (int i = 0; i < text.Length; ++i)
			{
				char c = text[i];
				if (!Compress.IsAsciiCrLfOrTab(c))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Constructs a byte containing the flags for this column.</summary>
		/// <remarks>Constructs a byte containing the flags for this column.</remarks>
		private byte GetColumnBitFlags()
		{
			byte flags = HealthMarketScience.Jackcess.Column.UNKNOWN_FLAG_MASK;
			if (!IsVariableLength())
			{
				flags |= HealthMarketScience.Jackcess.Column.FIXED_LEN_FLAG_MASK;
			}
			if (IsAutoNumber())
			{
				flags |= GetAutoNumberGenerator().GetColumnFlags();
			}
			return flags;
		}

		public override string ToString()
		{
			StringBuilder rtn = new StringBuilder();
			rtn.Append("\tName: (" + _table.GetName() + ") " + _name);
			byte typeValue = _typeProperties.value;
			if (DataTypeUtil.IsUnsupported(_type))
			{
				typeValue = GetUnknownDataType();
			}
			rtn.Append("\n\tType: 0x" + Sharpen.Extensions.ToHexString(typeValue) + " (" + _type
				 + ")");
			rtn.Append("\n\tNumber: " + _columnNumber);
			rtn.Append("\n\tLength: " + _columnLength);
			rtn.Append("\n\tVariable length: " + _variableLength);
			if (DataTypeUtil.IsTextual(_type))
			{
				rtn.Append("\n\tCompressed Unicode: " + _textInfo._compressedUnicode);
				rtn.Append("\n\tText Sort order: " + _textInfo._sortOrder);
				if (_textInfo._codePage > 0)
				{
					rtn.Append("\n\tText Code Page: " + _textInfo._codePage);
				}
			}
			if (_autoNumber)
			{
				rtn.Append("\n\tLast AutoNumber: " + _autoNumberGenerator.GetLast());
			}
			rtn.Append("\n\n");
			return rtn.ToString();
		}

		/// <param name="textBytes">bytes of text to decode</param>
		/// <param name="charset">relevant charset</param>
		/// <returns>the decoded string</returns>
		/// <usage>_advanced_method_</usage>
		public static string DecodeUncompressedText(byte[] textBytes, Encoding charset)
		{
			return DecodeUncompressedText(textBytes, 0, textBytes.Length, charset).ToString();
		}

		/// <param name="text">Text to encode</param>
		/// <param name="charset">database charset</param>
		/// <returns>A buffer with the text encoded</returns>
		/// <usage>_advanced_method_</usage>
		public static ByteBuffer EncodeUncompressedText(CharSequence text, Encoding charset
			)
		{
			CharBuffer cb = ((text is CharBuffer) ? (CharBuffer)text : CharBuffer.Wrap(text.ToString()));
			return charset.Encode(cb);
		}

		/// <summary>Orders Columns by column number.</summary>
		/// <remarks>Orders Columns by column number.</remarks>
		/// <usage>_general_method_</usage>
		public virtual int CompareTo(HealthMarketScience.Jackcess.Column other)
		{
			if (_columnNumber > other.GetColumnNumber())
			{
				return 1;
			}
			else
			{
				if (_columnNumber < other.GetColumnNumber())
				{
					return -1;
				}
				else
				{
					return 0;
				}
			}
		}

		/// <param name="columns">A list of columns in a table definition</param>
		/// <returns>The number of variable length columns found in the list</returns>
		/// <usage>_advanced_method_</usage>
		public static short CountVariableLength(IList<HealthMarketScience.Jackcess.Column
			> columns)
		{
			short rtn = 0;
			foreach (HealthMarketScience.Jackcess.Column col in columns)
			{
				if (col.IsVariableLength())
				{
					rtn++;
				}
			}
			return rtn;
		}

		/// <param name="columns">A list of columns in a table definition</param>
		/// <returns>
		/// The number of variable length columns which are not long values
		/// found in the list
		/// </returns>
		/// <usage>_advanced_method_</usage>
		public static short CountNonLongVariableLength(IList<HealthMarketScience.Jackcess.Column
			> columns)
		{
			short rtn = 0;
			foreach (HealthMarketScience.Jackcess.Column col in columns)
			{
				if (col.IsVariableLength() && !col._typeProperties.longValue)
				{
					rtn++;
				}
			}
			return rtn;
		}

		/// <returns>
		/// an appropriate BigDecimal representation of the given object.
		/// <code>null</code> is returned as 0 and Numbers are converted
		/// using their double representation.
		/// </returns>
		private static BigDecimal ToBigDecimal(object value)
		{
			if (value == null)
			{
				return BigDecimal.ZERO;
			}
			else
			{
				if (value is BigDecimal)
				{
					return (BigDecimal)value;
				}
				else
				{
					if (value is BigInteger)
					{
						return new BigDecimal((BigInteger)value);
					}
					else
					{
						if (value is Number)
						{
							return new BigDecimal(((Number)value).DoubleValue());
						}
					}
				}
			}
			return new BigDecimal(value.ToString());
		}

		/// <returns>
		/// an appropriate Number representation of the given object.
		/// <code>null</code> is returned as 0 and Strings are parsed as
		/// Doubles.
		/// </returns>
		private static Number ToNumber(object value)
		{
			if (value == null)
			{
				return BigDecimal.ZERO;
			}
			if (value is Number)
			{
				return (Number)value;
			}
			return new Sharpen.Double(Convert.ToDouble(value.ToString()));
		}

		/// <returns>an appropriate CharSequence representation of the given object.</returns>
		/// <usage>_advanced_method_</usage>
		/// <exception cref="System.IO.IOException"></exception>
		public static CharSequence ToCharSequence(object value)
		{
			if (value == null)
			{
				return null;
			}
			else
			{
				if (value is CharSequence)
				{
					return (CharSequence)value;
				}
				//else
				//{
				//	if (value is SQLClob)
				//	{
				//		try
				//		{
				//			Clob c = (Clob)value;
				//			// note, start pos is 1-based
				//			return c.GetSubString(1L, (int)c.Length());
				//		}
				//		catch (SQLException e)
				//		{
				//			throw (IOException)Sharpen.Extensions.InitCause((new IOException(e.Message)), e);
				//		}
				//	}
					else
					{
						if (value is StreamReader)
						{
							char[] buf = new char[8 * 1024];
							StringBuilder sout = new StringBuilder();
							StreamReader @in = (StreamReader)value;
							int read = 0;
							while ((read = @in.Read(buf)) != -1)
							{
								sout.Append(buf, 0, read);
							}
							return sout;
						}
					}
				//}
			}
			return value.ToString();
		}

		/// <returns>an appropriate byte[] representation of the given object.</returns>
		/// <usage>_advanced_method_</usage>
		/// <exception cref="System.IO.IOException"></exception>
		public static byte[] ToByteArray(object value)
		{
			if (value == null)
			{
				return null;
			}
			else
			{
				if (value is byte[])
				{
					return (byte[])value;
				}
				//else
				//{
				//	if (value is Blob)
				//	{
				//		try
				//		{
				//			Blob b = (Blob)value;
				//			// note, start pos is 1-based
				//			return b.GetBytes(1L, (int)b.Length());
				//		}
				//		catch (SQLException e)
				//		{
				//			throw (IOException)Sharpen.Extensions.InitCause((new IOException(e.Message)), e);
				//		}
				//	}
				//}
			}
			ByteArrayOutputStream bout = new ByteArrayOutputStream();
			if (value is InputStream)
			{
				byte[] buf = new byte[8 * 1024];
				InputStream @in = (InputStream)value;
				int read = 0;
				while ((read = @in.Read(buf)) != -1)
				{
					bout.Write(buf, 0, read);
				}
			}
			else
			{
				// if all else fails, serialize it
				ObjectOutputStream oos = new ObjectOutputStream(bout);
				oos.WriteObject(value);
				oos.Close();
			}
			return bout.ToByteArray();
		}

		/// <summary>Interpret a boolean value (null == false)</summary>
		/// <usage>_advanced_method_</usage>
		public static bool ToBooleanValue(object obj)
		{
			return ((obj != null) && ((bool)obj));
		}

		/// <summary>Swaps the bytes of the given numeric in place.</summary>
		/// <remarks>Swaps the bytes of the given numeric in place.</remarks>
		private static void FixNumericByteOrder(byte[] bytes)
		{
			// fix endianness of each 4 byte segment
			for (int i = 0; i < 4; ++i)
			{
				ByteUtil.Swap4Bytes(bytes, i * 4);
			}
		}

		/// <summary>Treat booleans as integers (C-style).</summary>
		/// <remarks>Treat booleans as integers (C-style).</remarks>
		protected internal static object BooleanToInteger(object obj)
		{
			if (obj is bool)
			{
				obj = ((bool)obj) ? 1 : 0;
			}
			return obj;
		}

		/// <summary>
		/// Returns a wrapper for raw column data that can be written without
		/// understanding the data.
		/// </summary>
		/// <remarks>
		/// Returns a wrapper for raw column data that can be written without
		/// understanding the data.  Useful for wrapping unparseable data for
		/// re-writing.
		/// </remarks>
		internal static Column.RawData RawDataWrapper(byte[] bytes)
		{
			return new Column.RawData(bytes);
		}

		/// <summary>
		/// Returs
		/// <code>true</code>
		/// if the given value is "raw" column data,
		/// <code>false</code>
		/// otherwise.
		/// </summary>
		internal static bool IsRawData(object value)
		{
			return (value is Column.RawData);
		}

		/// <summary>Writes the column definitions into a table definition buffer.</summary>
		/// <remarks>Writes the column definitions into a table definition buffer.</remarks>
		/// <param name="buffer">Buffer to write to</param>
		/// <param name="columns">List of Columns to write definitions for</param>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal static void WriteDefinitions(ByteBuffer buffer, IList<HealthMarketScience.Jackcess.Column
			> columns, JetFormat format, Encoding charset)
		{
			short columnNumber = (short)0;
			short fixedOffset = (short)0;
			short variableOffset = (short)0;
			// we specifically put the "long variable" values after the normal
			// variable length values so that we have a better chance of fitting it
			// all (because "long variable" values can go in separate pages)
			short longVariableOffset = HealthMarketScience.Jackcess.Column.CountNonLongVariableLength
				(columns);
			foreach (HealthMarketScience.Jackcess.Column col in columns)
			{
				// record this for later use when writing indexes
				col.SetColumnNumber(columnNumber);
				int position = buffer.Position();
				buffer.Put(col._typeProperties.value);
				buffer.PutInt(Table.MAGIC_TABLE_NUMBER);
				//constant magic number
				buffer.PutShort(columnNumber);
				//Column Number
				if (col.IsVariableLength())
				{
					if (!col._typeProperties.longValue)
					{
						buffer.PutShort(variableOffset++);
					}
					else
					{
						buffer.PutShort(longVariableOffset++);
					}
				}
				else
				{
					buffer.PutShort((short)0);
				}
				buffer.PutShort(columnNumber);
				//Column Number again
				if (DataTypeUtil.IsTextual(col.GetDataType()))
				{
					// this will write 4 bytes (note we don't support writing dbs which
					// use the text code page)
					WriteSortOrder(buffer, col.GetTextSortOrder(), format);
				}
				else
				{
					if (col._typeProperties.hasScalePrecision)
					{
						buffer.Put(col.GetPrecision());
						// numeric precision
						buffer.Put(col.GetScale());
					}
					else
					{
						// numeric scale
						buffer.Put(unchecked((byte)unchecked((int)(0x00))));
						//unused
						buffer.Put(unchecked((byte)unchecked((int)(0x00))));
					}
					//unused
					buffer.PutShort((short)0);
				}
				//Unknown
				buffer.Put(col.GetColumnBitFlags());
				// misc col flags
				if (col.IsCompressedUnicode())
				{
					//Compressed
					buffer.Put(unchecked((byte)1));
				}
				else
				{
					buffer.Put(unchecked((byte)0));
				}
				buffer.PutInt(0);
				//Unknown, but always 0.
				//Offset for fixed length columns
				if (col.IsVariableLength())
				{
					buffer.PutShort((short)0);
				}
				else
				{
					buffer.PutShort(fixedOffset);
					fixedOffset += (short)DataTypeUtil.GetFixedSize(col.GetDataType(), col.GetLength());
				}
				if (!col._typeProperties.longValue)
				{
					buffer.PutShort(col.GetLength());
				}
				else
				{
					//Column length
					buffer.PutShort((short)unchecked((int)(0x0000)));
				}
				// unused
				columnNumber++;
			}
			foreach (HealthMarketScience.Jackcess.Column col_1 in columns)
			{
				Table.WriteName(buffer, col_1.GetName(), charset);
			}
		}

		/// <summary>Reads the sort order info from the given buffer from the given position.
		/// 	</summary>
		/// <remarks>Reads the sort order info from the given buffer from the given position.
		/// 	</remarks>
		internal static Column.SortOrder ReadSortOrder(ByteBuffer buffer, int position, JetFormat
			 format)
		{
			short value = buffer.GetShort(position);
			byte version = 0;
			if (format.SIZE_SORT_ORDER == 4)
			{
				version = buffer.Get(position + 3);
			}
			if (value == 0)
			{
				// probably a file we wrote, before handling sort order
				return format.DEFAULT_SORT_ORDER;
			}
			if (value == GENERAL_SORT_ORDER_VALUE)
			{
				if (version == GENERAL_LEGACY_SORT_ORDER.GetVersion())
				{
					return GENERAL_LEGACY_SORT_ORDER;
				}
				if (version == GENERAL_SORT_ORDER.GetVersion())
				{
					return GENERAL_SORT_ORDER;
				}
			}
			return new Column.SortOrder(value, version);
		}

		/// <summary>Writes the sort order info to the given buffer at the current position.</summary>
		/// <remarks>Writes the sort order info to the given buffer at the current position.</remarks>
		private static void WriteSortOrder(ByteBuffer buffer, Column.SortOrder sortOrder, 
			JetFormat format)
		{
			if (sortOrder == null)
			{
				sortOrder = format.DEFAULT_SORT_ORDER;
			}
			buffer.PutShort(sortOrder.GetValue());
			if (format.SIZE_SORT_ORDER == 4)
			{
				buffer.Put(unchecked((byte)unchecked((int)(0x00))));
				// unknown
				buffer.Put(sortOrder.GetVersion());
			}
		}

		/// <summary>
		/// Date subclass which stashes the original date bits, in case we attempt to
		/// re-write the value (will not lose precision).
		/// </summary>
		/// <remarks>
		/// Date subclass which stashes the original date bits, in case we attempt to
		/// re-write the value (will not lose precision).
		/// </remarks>
		public sealed class Date
		{
			/// <summary>cached bits of the original date value</summary>
			[System.NonSerialized]
			private readonly DateTime _date;

			[System.NonSerialized]
			private readonly long _dateBits;

			public Date(long time, long dateBits)
			{
				//extends Date
				_date = Sharpen.Extensions.CreateDate(time);
				_dateBits = dateBits;
			}

			public long GetDateBits()
			{
				return _dateBits;
			}

			/// <exception cref="System.IO.ObjectStreamException"></exception>
			private object WriteReplace()
			{
				// if we are going to serialize this Date, convert it back to a normal
				// Date (in case it is restored outside of the context of jackcess)
				return Sharpen.Extensions.CreateDate(_date.GetTime());
			}

			public static implicit operator DateTime(Date d) => d._date;
			public static explicit operator DateTime?(Date d) => d == null ? (DateTime?)null : d._date;
		}

		/// <summary>Wrapper for raw column data which can be re-written.</summary>
		/// <remarks>Wrapper for raw column data which can be re-written.</remarks>
		[System.Serializable]
		internal class RawData
		{
			private const long serialVersionUID = 0L;

			private readonly byte[] _bytes;

			internal RawData(byte[] bytes)
			{
				_bytes = bytes;
			}

			internal virtual byte[] GetBytes()
			{
				return _bytes;
			}

			public override string ToString()
			{
				return "RawData: " + ByteUtil.ToHexString(GetBytes());
			}

			/// <exception cref="System.IO.ObjectStreamException"></exception>
			internal virtual object WriteReplace()
			{
				// if we are going to serialize this, convert it back to a normal
				// byte[] (in case it is restored outside of the context of jackcess)
				return GetBytes();
			}
		}

		/// <summary>Base class for the supported autonumber types.</summary>
		/// <remarks>Base class for the supported autonumber types.</remarks>
		/// <usage>_advanced_class_</usage>
		public abstract class AutoNumberGenerator
		{
			public AutoNumberGenerator(Column _enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <summary>Returns the last autonumber generated by this generator.</summary>
			/// <remarks>
			/// Returns the last autonumber generated by this generator.  Only valid
			/// after a call to
			/// <see cref="Table.AddRow(object[])">Table.AddRow(object[])</see>
			/// , otherwise undefined.
			/// </remarks>
			public abstract object GetLast();

			/// <summary>Returns the next autonumber for this generator.</summary>
			/// <remarks>
			/// Returns the next autonumber for this generator.
			/// <p>
			/// <i>Warning, calling this externally will result in this value being
			/// "lost" for the table.</i>
			/// </remarks>
			public abstract object GetNext();

			/// <summary>Returns the flags used when writing this column.</summary>
			/// <remarks>Returns the flags used when writing this column.</remarks>
			public abstract byte GetColumnFlags();

			/// <summary>Returns the type of values generated by this generator.</summary>
			/// <remarks>Returns the type of values generated by this generator.</remarks>
			public abstract DataType GetDataType();

			private readonly Column _enclosing;
		}

		public sealed class LongAutoNumberGenerator : Column.AutoNumberGenerator
		{
			public LongAutoNumberGenerator(Column _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override object GetLast()
			{
				// the table stores the last long autonumber used
				return this._enclosing.GetTable().GetLastLongAutoNumber();
			}

			public override object GetNext()
			{
				// the table stores the last long autonumber used
				return this._enclosing.GetTable().GetNextLongAutoNumber();
			}

			public override byte GetColumnFlags()
			{
				return Column.AUTO_NUMBER_FLAG_MASK;
			}

			public override DataType GetDataType()
			{
				return DataType.LONG;
			}

			private readonly Column _enclosing;
		}

		public sealed class GuidAutoNumberGenerator : Column.AutoNumberGenerator
		{
			private object _lastAutoNumber;

			public GuidAutoNumberGenerator(Column _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override object GetLast()
			{
				return this._lastAutoNumber;
			}

			public override object GetNext()
			{
				// format guids consistently w/ Column.readGUIDValue()
				this._lastAutoNumber = "{" + Guid.NewGuid().ToString() + "}";
				return this._lastAutoNumber;
			}

			public override byte GetColumnFlags()
			{
				return Column.AUTO_NUMBER_GUID_FLAG_MASK;
			}

			public override DataType GetDataType()
			{
				return DataType.GUID;
			}

			private readonly Column _enclosing;
		}

		public sealed class UnsupportedAutoNumberGenerator : Column.AutoNumberGenerator
		{
			private readonly DataType? _genType;

			public UnsupportedAutoNumberGenerator(Column _enclosing, DataType? genType) : base
				(_enclosing)
			{
				this._enclosing = _enclosing;
				this._genType = genType;
			}

			public override object GetLast()
			{
				return null;
			}

			public override object GetNext()
			{
				throw new NotSupportedException();
			}

			public override byte GetColumnFlags()
			{
				throw new NotSupportedException();
			}

			public override DataType GetDataType()
			{
				return this._genType.Value;
			}

			private readonly Column _enclosing;
		}

		/// <summary>Information about the sort order (collation) for a textual column.</summary>
		/// <remarks>Information about the sort order (collation) for a textual column.</remarks>
		/// <usage>_intermediate_class_</usage>
		public sealed class SortOrder
		{
			private readonly short _value;

			private readonly byte _version;

			public SortOrder(short value, byte version)
			{
				_value = value;
				_version = version;
			}

			public short GetValue()
			{
				return _value;
			}

			public byte GetVersion()
			{
				return _version;
			}

			public override int GetHashCode()
			{
				return _value;
			}

			public override bool Equals(object o)
			{
				return ((this == o) || ((o != null) && (GetType() == o.GetType()) && (_value == (
					(Column.SortOrder)o)._value) && (_version == ((Column.SortOrder)o)._version)));
			}

			public override string ToString()
			{
				return _value + "(" + _version + ")";
			}
		}

		/// <summary>Information specific to numeric types.</summary>
		/// <remarks>Information specific to numeric types.</remarks>
		public sealed class NumericInfo
		{
			/// <summary>Numeric precision</summary>
			public byte _precision;

			/// <summary>Numeric scale</summary>
			public byte _scale;
		}

		/// <summary>Information specific to textual types.</summary>
		/// <remarks>Information specific to textual types.</remarks>
		public sealed class TextInfo
		{
			/// <summary>whether or not they are compressed</summary>
			public bool _compressedUnicode;

			/// <summary>the collating sort order for a text field</summary>
			public Column.SortOrder _sortOrder;

			/// <summary>the code page for a text field (for certain db versions)</summary>
			public short _codePage;
		}
	}
}
