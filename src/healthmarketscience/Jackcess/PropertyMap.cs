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
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Map of properties for a given database object.</summary>
	/// <remarks>Map of properties for a given database object.</remarks>
	/// <author>James Ahlborn</author>
	public class PropertyMap : Iterable<PropertyMap.Property>
	{
		public static readonly string ACCESS_VERSION_PROP = "AccessVersion";

		public static readonly string TITLE_PROP = "Title";

		public static readonly string AUTHOR_PROP = "Author";

		public static readonly string COMPANY_PROP = "Company";

		public static readonly string DEFAULT_VALUE_PROP = "DefaultValue";

		public static readonly string REQUIRED_PROP = "Required";

		public static readonly string ALLOW_ZERO_LEN_PROP = "AllowZeroLength";

		public static readonly string DECIMAL_PLACES_PROP = "DecimalPlaces";

		public static readonly string FORMAT_PROP = "Format";

		public static readonly string INPUT_MASK_PROP = "InputMask";

		public static readonly string CAPTION_PROP = "Caption";

		public static readonly string VALIDATION_RULE_PROP = "ValidationRule";

		public static readonly string VALIDATION_TEXT_PROP = "ValidationText";

		public static readonly string GUID_PROP = "GUID";

		public static readonly string DESCRIPTION_PROP = "Description";

		private readonly string _mapName;

		private readonly short _mapType;

		private readonly IDictionary<string, PropertyMap.Property> _props = new LinkedHashMap
			<string, PropertyMap.Property>();

		internal PropertyMap(string name, short type)
		{
			_mapName = name;
			_mapType = type;
		}

		public virtual string GetName()
		{
			return _mapName;
		}

		public virtual short GetMapType()
		{
			return _mapType;
		}

		public virtual int GetSize()
		{
			return _props.Count;
		}

		public virtual bool IsEmpty()
		{
			return _props.IsEmpty();
		}

		/// <returns>the property with the given name, if any</returns>
		public virtual PropertyMap.Property Get(string name)
		{
			return _props.Get(Database.ToLookupName(name));
		}

		/// <returns>the value of the property with the given name, if any</returns>
		public virtual object GetValue(string name)
		{
			return GetValue(name, null);
		}

		/// <returns>
		/// the value of the property with the given name, if any, otherwise
		/// the given defaultValue
		/// </returns>
		public virtual object GetValue(string name, object defaultValue)
		{
			PropertyMap.Property prop = Get(name);
			object value = defaultValue;
			if ((prop != null) && (prop.GetValue() != null))
			{
				value = prop.GetValue();
			}
			return value;
		}

		/// <summary>Puts a property into this map with the given information.</summary>
		/// <remarks>Puts a property into this map with the given information.</remarks>
		public virtual void Put(string name, DataType type, byte flag, object value)
		{
			_props.Put(Database.ToLookupName(name), new PropertyMap.Property(name, type, flag
				, value));
		}

		public override Sharpen.Iterator<PropertyMap.Property> Iterator()
		{
			return _props.Values.Iterator();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(PropertyMaps.DEFAULT_NAME.Equals(GetName()) ? "<DEFAULT>" : GetName()).
				Append(" {");
			for (Sharpen.Iterator<PropertyMap.Property> iter = Iterator(); iter.HasNext(); )
			{
				sb.Append(iter.Next());
				if (iter.HasNext())
				{
					sb.Append(",");
				}
			}
			sb.Append("}");
			return sb.ToString();
		}

		/// <summary>Info about a property defined in a PropertyMap.</summary>
		/// <remarks>Info about a property defined in a PropertyMap.</remarks>
		public sealed class Property
		{
			private readonly string _name;

			private readonly DataType _type;

			private readonly byte _flag;

			private readonly object _value;

			public Property(string name, DataType type, byte flag, object value)
			{
				_name = name;
				_type = type;
				_flag = flag;
				_value = value;
			}

			public string GetName()
			{
				return _name;
			}

			public DataType GetDataType()
			{
				return _type;
			}

			public object GetValue()
			{
				return _value;
			}

			public override string ToString()
			{
				object val = GetValue();
				if (val is byte[])
				{
					val = ByteUtil.ToHexString((byte[])val);
				}
				return GetName() + "[" + GetDataType() + ":" + _flag + "]=" + val;
			}
		}
	}
}
