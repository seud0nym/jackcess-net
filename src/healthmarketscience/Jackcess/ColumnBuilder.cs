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
	/// <summary>Builder style class for constructing a Column.</summary>
	/// <remarks>Builder style class for constructing a Column.</remarks>
	/// <author>James Ahlborn</author>
	public class ColumnBuilder
	{
		/// <summary>name of the new column</summary>
		private string _name;

		/// <summary>the type of the new column</summary>
		private DataType _type;

		private DataTypeProperties _typeProperties;

		/// <summary>optional length for the new column</summary>
		private int? _length;

		/// <summary>optional precision for the new column</summary>
		private int? _precision;

		/// <summary>optional scale for the new column</summary>
		private int? _scale;

		/// <summary>whether or not the column is auto-number</summary>
		private bool _autoNumber;

		/// <summary>whether or not the column allows compressed unicode</summary>
		private bool? _compressedUnicode;

		//public ColumnBuilder(string name) : this(name, null)
		//{
		//}

		public ColumnBuilder(string name, DataType type)
		{
			_name = name;
			_type = type;
			_typeProperties = DataTypeProperties.Get(type);
		}

		/// <summary>Sets the type for the new column.</summary>
		/// <remarks>Sets the type for the new column.</remarks>
		public virtual HealthMarketScience.Jackcess.ColumnBuilder SetType(DataType type)
		{
			_type = type;
			_typeProperties = DataTypeProperties.Get(type);
			return this;
		}

		/// <summary>Sets the type for the new column based on the given SQL type.</summary>
		/// <remarks>Sets the type for the new column based on the given SQL type.</remarks>
		/// <exception cref="Sharpen.SQLException"></exception>
		public virtual HealthMarketScience.Jackcess.ColumnBuilder SetSQLType(int type)
		{
			return SetSQLType(type, 0);
		}

		/// <summary>
		/// Sets the type for the new column based on the given SQL type and target
		/// data length (in type specific units).
		/// </summary>
		/// <remarks>
		/// Sets the type for the new column based on the given SQL type and target
		/// data length (in type specific units).
		/// </remarks>
		/// <exception cref="Sharpen.SQLException"></exception>
		public virtual HealthMarketScience.Jackcess.ColumnBuilder SetSQLType(int type, int
			 lengthInUnits)
		{
			return SetType(DataTypeUtil.FromSQLType(type, lengthInUnits));
		}

		/// <summary>Sets the precision for the new column.</summary>
		/// <remarks>Sets the precision for the new column.</remarks>
		public virtual HealthMarketScience.Jackcess.ColumnBuilder SetPrecision(int newPrecision
			)
		{
			_precision = newPrecision;
			return this;
		}

		/// <summary>Sets the scale for the new column.</summary>
		/// <remarks>Sets the scale for the new column.</remarks>
		public virtual HealthMarketScience.Jackcess.ColumnBuilder SetScale(int newScale)
		{
			_scale = newScale;
			return this;
		}

		/// <summary>Sets the length (in bytes) for the new column.</summary>
		/// <remarks>Sets the length (in bytes) for the new column.</remarks>
		public virtual HealthMarketScience.Jackcess.ColumnBuilder SetLength(int length)
		{
			_length = length;
			return this;
		}

		/// <summary>Sets the length (in type specific units) for the new column.</summary>
		/// <remarks>Sets the length (in type specific units) for the new column.</remarks>
		public virtual HealthMarketScience.Jackcess.ColumnBuilder SetLengthInUnits(int unitLength
			)
		{
			return SetLength(_typeProperties.unitSize * unitLength);
		}

		/// <summary>Sets the length for the new column to the max length for the type.</summary>
		/// <remarks>Sets the length for the new column to the max length for the type.</remarks>
		public virtual HealthMarketScience.Jackcess.ColumnBuilder SetMaxLength()
		{
			return SetLength(_typeProperties.maxSize.Value);
		}

		/// <summary>Sets whether of not the new column is an auto-number column.</summary>
		/// <remarks>Sets whether of not the new column is an auto-number column.</remarks>
		public virtual HealthMarketScience.Jackcess.ColumnBuilder SetAutoNumber(bool autoNumber
			)
		{
			_autoNumber = autoNumber;
			return this;
		}

		/// <summary>Sets whether of not the new column allows unicode compression.</summary>
		/// <remarks>Sets whether of not the new column allows unicode compression.</remarks>
		public virtual HealthMarketScience.Jackcess.ColumnBuilder SetCompressedUnicode(bool
			 compressedUnicode)
		{
			_compressedUnicode = compressedUnicode;
			return this;
		}

		/// <summary>Sets all attributes except name from the given Column template.</summary>
		/// <remarks>Sets all attributes except name from the given Column template.</remarks>
		public virtual HealthMarketScience.Jackcess.ColumnBuilder SetFromColumn(Column template
			)
		{
			DataType type = template.GetDataType();
			SetType(type);
			SetLength(template.GetLength());
			SetAutoNumber(template.IsAutoNumber());
			if (_typeProperties.hasScalePrecision)
			{
				SetScale(template.GetScale());
				SetPrecision(template.GetPrecision());
			}
			return this;
		}

		/// <summary>
		/// Escapes the new column's name using
		/// <see cref="Database.EscapeIdentifier(string)">Database.EscapeIdentifier(string)</see>
		/// .
		/// </summary>
		public virtual HealthMarketScience.Jackcess.ColumnBuilder EscapeName()
		{
			_name = Database.EscapeIdentifier(_name);
			return this;
		}

		/// <summary>Creates a new Column with the currently configured attributes.</summary>
		/// <remarks>Creates a new Column with the currently configured attributes.</remarks>
		public virtual Column ToColumn()
		{
			Column col = new Column();
			col.SetName(_name);
			col.SetType(_type);
			if (_length != null)
			{
				col.SetLength((short)_length.Value);
			}
			if (_precision != null)
			{
				col.SetPrecision((byte)_precision.Value);
			}
			if (_scale != null)
			{
				col.SetScale((byte)_scale.Value);
			}
			if (_autoNumber)
			{
				col.SetAutoNumber(true);
			}
			if (_compressedUnicode != null)
			{
				col.SetCompressedUnicode(_compressedUnicode.Value);
			}
			return col;
		}
	}
}
