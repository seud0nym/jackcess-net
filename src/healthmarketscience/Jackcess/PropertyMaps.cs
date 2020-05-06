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
using System.IO;
using System.Text;
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Collection of PropertyMap instances read from a single property data block.
	/// 	</summary>
	/// <remarks>Collection of PropertyMap instances read from a single property data block.
	/// 	</remarks>
	/// <author>James Ahlborn</author>
	public class PropertyMaps : Iterable<PropertyMap>
	{
		/// <summary>the name of the "default" properties for a PropertyMaps instance</summary>
		public static readonly string DEFAULT_NAME = string.Empty;

		private const short PROPERTY_NAME_LIST = unchecked((int)(0x80));

		private const short DEFAULT_PROPERTY_VALUE_LIST = unchecked((int)(0x00));

		private const short COLUMN_PROPERTY_VALUE_LIST = unchecked((int)(0x01));

		/// <summary>
		/// maps the PropertyMap name (case-insensitive) to the PropertyMap
		/// instance
		/// </summary>
		private readonly IDictionary<string, PropertyMap> _maps = new LinkedHashMap<string
			, PropertyMap>();

		private readonly int _objectId;

		public PropertyMaps(int objectId)
		{
			_objectId = objectId;
		}

		public virtual int GetObjectId()
		{
			return _objectId;
		}

		public virtual int GetSize()
		{
			return _maps.Count;
		}

		public virtual bool IsEmpty()
		{
			return _maps.IsEmpty();
		}

		/// <returns>
		/// the unnamed "default" PropertyMap in this group, creating if
		/// necessary.
		/// </returns>
		public virtual PropertyMap GetDefault()
		{
			return Get(DEFAULT_NAME, DEFAULT_PROPERTY_VALUE_LIST);
		}

		/// <returns>
		/// the PropertyMap with the given name in this group, creating if
		/// necessary
		/// </returns>
		public virtual PropertyMap Get(string name)
		{
			return Get(name, COLUMN_PROPERTY_VALUE_LIST);
		}

		/// <returns>
		/// the PropertyMap with the given name and type in this group,
		/// creating if necessary
		/// </returns>
		private PropertyMap Get(string name, short type)
		{
			string lookupName = Database.ToLookupName(name);
			PropertyMap map = _maps.Get(lookupName);
			if (map == null)
			{
				map = new PropertyMap(name, type);
				_maps.Put(lookupName, map);
			}
			return map;
		}

		/// <summary>Adds the given PropertyMap to this group.</summary>
		/// <remarks>Adds the given PropertyMap to this group.</remarks>
		public virtual void Put(PropertyMap map)
		{
			_maps.Put(Database.ToLookupName(map.GetName()), map);
		}

		public override Sharpen.Iterator<PropertyMap> Iterator()
		{
			return _maps.Values.Iterator();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			for (Sharpen.Iterator<PropertyMap> iter = Iterator(); iter.HasNext(); )
			{
				sb.Append(iter.Next());
				if (iter.HasNext())
				{
					sb.Append("\n");
				}
			}
			return sb.ToString();
		}

		/// <summary>Utility class for reading/writing property blocks.</summary>
		/// <remarks>Utility class for reading/writing property blocks.</remarks>
		internal sealed class Handler
		{
			/// <summary>the current database</summary>
			private readonly Database _database;

			/// <summary>cache of PropColumns used to read/write property values</summary>
			private readonly IDictionary<DataType, PropertyMaps.Handler.PropColumn> _columns = 
				new Dictionary<DataType, PropertyMaps.Handler.PropColumn>();

			internal Handler(Database database)
			{
				_database = database;
			}

			/// <returns>
			/// a PropertyMaps instance decoded from the given bytes (always
			/// returns non-
			/// <code>null</code>
			/// result).
			/// </returns>
			/// <exception cref="System.IO.IOException"></exception>
			public PropertyMaps Read(byte[] propBytes, int objectId)
			{
				PropertyMaps maps = new PropertyMaps(objectId);
				if ((propBytes == null) || (propBytes.Length == 0))
				{
					return maps;
				}
				ByteBuffer bb = ByteBuffer.Wrap(propBytes).Order(PageChannel.DEFAULT_BYTE_ORDER);
				// check for known header
				bool knownType = false;
				foreach (byte[] tmpType in JetFormat.PROPERTY_MAP_TYPES)
				{
					if (ByteUtil.MatchesRange(bb, bb.Position(), tmpType))
					{
						ByteUtil.Forward(bb, tmpType.Length);
						knownType = true;
						break;
					}
				}
				if (!knownType)
				{
					throw new IOException("Uknown property map type " + ByteUtil.ToHexString(bb, 4));
				}
				// parse each data "chunk"
				IList<string> propNames = null;
				while (bb.HasRemaining())
				{
					int len = bb.GetInt();
					short type = bb.GetShort();
					int endPos = bb.Position() + len - 6;
					ByteBuffer bbBlock = PageChannel.NarrowBuffer(bb, bb.Position(), endPos);
					if (type == PROPERTY_NAME_LIST)
					{
						propNames = ReadPropertyNames(bbBlock);
					}
					else
					{
						if ((type == DEFAULT_PROPERTY_VALUE_LIST) || (type == COLUMN_PROPERTY_VALUE_LIST))
						{
							maps.Put(ReadPropertyValues(bbBlock, propNames, type));
						}
						else
						{
							throw new IOException("Unknown property block type " + type);
						}
					}
					bb.Position(endPos);
				}
				return maps;
			}

			/// <returns>the property names parsed from the given data chunk</returns>
			private IList<string> ReadPropertyNames(ByteBuffer bbBlock)
			{
				IList<string> names = new AList<string>();
				while (bbBlock.HasRemaining())
				{
					names.AddItem(ReadPropName(bbBlock));
				}
				return names;
			}

			/// <returns>
			/// the PropertyMap created from the values parsed from the given
			/// data chunk combined with the given property names
			/// </returns>
			/// <exception cref="System.IO.IOException"></exception>
			private PropertyMap ReadPropertyValues(ByteBuffer bbBlock, IList<string> propNames
				, short blockType)
			{
				string mapName = DEFAULT_NAME;
				if (bbBlock.HasRemaining())
				{
					// read the map name, if any
					int nameBlockLen = bbBlock.GetInt();
					int endPos = bbBlock.Position() + nameBlockLen - 4;
					if (nameBlockLen > 6)
					{
						mapName = ReadPropName(bbBlock);
					}
					bbBlock.Position(endPos);
				}
				PropertyMap map = new PropertyMap(mapName, blockType);
				// read the values
				while (bbBlock.HasRemaining())
				{
					int valLen = bbBlock.GetShort();
					int endPos = bbBlock.Position() + valLen - 2;
					byte flag = bbBlock.Get();
					DataType dataType = DataTypeUtil.FromByte(bbBlock.Get());
					int nameIdx = bbBlock.GetShort();
					int dataSize = bbBlock.GetShort();
					string propName = propNames[nameIdx];
					PropertyMaps.Handler.PropColumn col = GetColumn(dataType, propName, dataSize);
					byte[] data = new byte[dataSize];
					bbBlock.Get(data);
					object value = col.Read(data);
					map.Put(propName, dataType, flag, value);
					bbBlock.Position(endPos);
				}
				return map;
			}

			/// <summary>Reads a property name from the given data block</summary>
			private string ReadPropName(ByteBuffer buffer)
			{
				int nameLength = buffer.GetShort();
				byte[] nameBytes = new byte[nameLength];
				buffer.Get(nameBytes);
				return Column.DecodeUncompressedText(nameBytes, _database.GetCharset());
			}

			/// <summary>
			/// Gets a PropColumn capable of reading/writing a property of the given
			/// DataType
			/// </summary>
			private PropertyMaps.Handler.PropColumn GetColumn(DataType dataType, string propName
				, int dataSize)
			{
				if (IsPseudoGuidColumn(dataType, propName, dataSize))
				{
					dataType = DataType.GUID;
				}
				PropertyMaps.Handler.PropColumn col = _columns.Get(dataType);
				if (col == null)
				{
					// translate long value types into simple types
					DataType colType = dataType;
					if (dataType == DataType.MEMO)
					{
						colType = DataType.TEXT;
					}
					else
					{
						if (dataType == DataType.OLE)
						{
							colType = DataType.BINARY;
						}
					}
					// create column with ability to read/write the given data type
					col = ((colType == DataType.BOOLEAN) ? new PropertyMaps.Handler.BooleanPropColumn
						(this) : new PropertyMaps.Handler.PropColumn(this));
					col.SetType(colType);
					if (col.IsVariableLength())
					{
						col.SetLength((short)DataTypeProperties.Get(colType).maxSize.Value);
					}
				}
				return col;
			}

			private bool IsPseudoGuidColumn(DataType dataType, string propName, int dataSize)
			{
				// guids seem to be marked as "binary" fields
				return ((dataType == DataType.BINARY) && (dataSize == DataTypeUtil.GetFixedSize(DataType
					.GUID)) && Sharpen.Runtime.EqualsIgnoreCase(PropertyMap.GUID_PROP, propName));
			}

			/// <summary>Column adapted to work w/out a Table.</summary>
			/// <remarks>Column adapted to work w/out a Table.</remarks>
			private class PropColumn : Column
			{
				public override Database GetDatabase()
				{
					return this._enclosing._database;
				}

				internal PropColumn(Handler _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly Handler _enclosing;
			}

			/// <summary>
			/// Normal boolean columns do not write into the actual row data, so we
			/// need to do a little extra work.
			/// </summary>
			/// <remarks>
			/// Normal boolean columns do not write into the actual row data, so we
			/// need to do a little extra work.
			/// </remarks>
			private sealed class BooleanPropColumn : PropertyMaps.Handler.PropColumn
			{
				/// <exception cref="System.IO.IOException"></exception>
				public override object Read(byte[] data)
				{
					return ((data[0] != 0) ? true : false);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override ByteBuffer Write(object obj, int remainingRowLength)
				{
					ByteBuffer buffer = this.GetPageChannel().CreateBuffer(1);
					buffer.Put(((Number)Column.BooleanToInteger(obj)));
					buffer.Flip();
					return buffer;
				}

				internal BooleanPropColumn(Handler _enclosing) : base(_enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly Handler _enclosing;
			}
		}
	}
}
