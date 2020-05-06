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
	/// <summary>Encapsulates constants describing a specific version of the Access Jet format
	/// 	</summary>
	/// <author>Tim McCune</author>
	public abstract class JetFormat
	{
		/// <summary>Maximum size of a record minus OLE objects and Memo fields</summary>
		public const int MAX_RECORD_SIZE = 1900;

		/// <summary>the "unit" size for text fields</summary>
		public const short TEXT_FIELD_UNIT_SIZE = 2;

		/// <summary>Maximum size of a text field</summary>
		public const short TEXT_FIELD_MAX_LENGTH = 255 * TEXT_FIELD_UNIT_SIZE;

		public enum CodecType
		{
			NONE,
			JET,
			MSISAM
		}

		/// <summary>
		/// Offset in the file that holds the byte describing the Jet format
		/// version
		/// </summary>
		private const int OFFSET_VERSION = 20;

		/// <summary>Version code for Jet version 3</summary>
		private const byte CODE_VERSION_3 = unchecked((int)(0x0));

		/// <summary>Version code for Jet version 4</summary>
		private const byte CODE_VERSION_4 = unchecked((int)(0x1));

		/// <summary>Version code for Jet version 12</summary>
		private const byte CODE_VERSION_12 = unchecked((int)(0x2));

		/// <summary>Version code for Jet version 14</summary>
		private const byte CODE_VERSION_14 = unchecked((int)(0x3));

		/// <summary>location of the engine name in the header</summary>
		internal const int OFFSET_ENGINE_NAME = unchecked((int)(0x4));

		/// <summary>length of the engine name in the header</summary>
		internal const int LENGTH_ENGINE_NAME = unchecked((int)(0xF));

		/// <summary>amount of initial data to be read to determine database type</summary>
		private const int HEADER_LENGTH = 21;

		private static readonly byte[] MSISAM_ENGINE = new byte[] { (byte)('M'), (byte)('S'
			), (byte)('I'), (byte)('S'), (byte)('A'), (byte)('M'), (byte)(' '), (byte)('D'), 
			(byte)('a'), (byte)('t'), (byte)('a'), (byte)('b'), (byte)('a'), (byte)('s'), (byte
			)('e') };

		/// <summary>mask used to obfuscate the db header</summary>
		private static readonly byte[] BASE_HEADER_MASK = new byte[] { unchecked((byte)unchecked(
			(int)(0xB5))), unchecked((byte)unchecked((int)(0x6F))), unchecked((byte)unchecked(
			(int)(0x03))), unchecked((byte)unchecked((int)(0x62))), unchecked((byte)unchecked(
			(int)(0x61))), unchecked((byte)unchecked((int)(0x08))), unchecked((byte)unchecked(
			(int)(0xC2))), unchecked((byte)unchecked((int)(0x55))), unchecked((byte)unchecked(
			(int)(0xEB))), unchecked((byte)unchecked((int)(0xA9))), unchecked((byte)unchecked(
			(int)(0x67))), unchecked((byte)unchecked((int)(0x72))), unchecked((byte)unchecked(
			(int)(0x43))), unchecked((byte)unchecked((int)(0x3F))), unchecked((byte)unchecked(
			(int)(0x00))), unchecked((byte)unchecked((int)(0x9C))), unchecked((byte)unchecked(
			(int)(0x7A))), unchecked((byte)unchecked((int)(0x9F))), unchecked((byte)unchecked(
			(int)(0x90))), unchecked((byte)unchecked((int)(0xFF))), unchecked((byte)unchecked(
			(int)(0x80))), unchecked((byte)unchecked((int)(0x9A))), unchecked((byte)unchecked(
			(int)(0x31))), unchecked((byte)unchecked((int)(0xC5))), unchecked((byte)unchecked(
			(int)(0x79))), unchecked((byte)unchecked((int)(0xBA))), unchecked((byte)unchecked(
			(int)(0xED))), unchecked((byte)unchecked((int)(0x30))), unchecked((byte)unchecked(
			(int)(0xBC))), unchecked((byte)unchecked((int)(0xDF))), unchecked((byte)unchecked(
			(int)(0xCC))), unchecked((byte)unchecked((int)(0x9D))), unchecked((byte)unchecked(
			(int)(0x63))), unchecked((byte)unchecked((int)(0xD9))), unchecked((byte)unchecked(
			(int)(0xE4))), unchecked((byte)unchecked((int)(0xC3))), unchecked((byte)unchecked(
			(int)(0x7B))), unchecked((byte)unchecked((int)(0x42))), unchecked((byte)unchecked(
			(int)(0xFB))), unchecked((byte)unchecked((int)(0x8A))), unchecked((byte)unchecked(
			(int)(0xBC))), unchecked((byte)unchecked((int)(0x4E))), unchecked((byte)unchecked(
			(int)(0x86))), unchecked((byte)unchecked((int)(0xFB))), unchecked((byte)unchecked(
			(int)(0xEC))), unchecked((byte)unchecked((int)(0x37))), unchecked((byte)unchecked(
			(int)(0x5D))), unchecked((byte)unchecked((int)(0x44))), unchecked((byte)unchecked(
			(int)(0x9C))), unchecked((byte)unchecked((int)(0xFA))), unchecked((byte)unchecked(
			(int)(0xC6))), unchecked((byte)unchecked((int)(0x5E))), unchecked((byte)unchecked(
			(int)(0x28))), unchecked((byte)unchecked((int)(0xE6))), unchecked((byte)unchecked(
			(int)(0x13))), unchecked((byte)unchecked((int)(0xB6))), unchecked((byte)unchecked(
			(int)(0x8A))), unchecked((byte)unchecked((int)(0x60))), unchecked((byte)unchecked(
			(int)(0x54))), unchecked((byte)unchecked((int)(0x94))), unchecked((byte)unchecked(
			(int)(0x7B))), unchecked((byte)unchecked((int)(0x36))), unchecked((byte)unchecked(
			(int)(0xF5))), unchecked((byte)unchecked((int)(0x72))), unchecked((byte)unchecked(
			(int)(0xDF))), unchecked((byte)unchecked((int)(0xB1))), unchecked((byte)unchecked(
			(int)(0x77))), unchecked((byte)unchecked((int)(0xF4))), unchecked((byte)unchecked(
			(int)(0x13))), unchecked((byte)unchecked((int)(0x43))), unchecked((byte)unchecked(
			(int)(0xCF))), unchecked((byte)unchecked((int)(0xAF))), unchecked((byte)unchecked(
			(int)(0xB1))), unchecked((byte)unchecked((int)(0x33))), unchecked((byte)unchecked(
			(int)(0x34))), unchecked((byte)unchecked((int)(0x61))), unchecked((byte)unchecked(
			(int)(0x79))), unchecked((byte)unchecked((int)(0x5B))), unchecked((byte)unchecked(
			(int)(0x92))), unchecked((byte)unchecked((int)(0xB5))), unchecked((byte)unchecked(
			(int)(0x7C))), unchecked((byte)unchecked((int)(0x2A))), unchecked((byte)unchecked(
			(int)(0x05))), unchecked((byte)unchecked((int)(0xF1))), unchecked((byte)unchecked(
			(int)(0x7C))), unchecked((byte)unchecked((int)(0x99))), unchecked((byte)unchecked(
			(int)(0x01))), unchecked((byte)unchecked((int)(0x1B))), unchecked((byte)unchecked(
			(int)(0x98))), unchecked((byte)unchecked((int)(0xFD))), unchecked((byte)unchecked(
			(int)(0x12))), unchecked((byte)unchecked((int)(0x4F))), unchecked((byte)unchecked(
			(int)(0x4A))), unchecked((byte)unchecked((int)(0x94))), unchecked((byte)unchecked(
			(int)(0x6C))), unchecked((byte)unchecked((int)(0x3E))), unchecked((byte)unchecked(
			(int)(0x60))), unchecked((byte)unchecked((int)(0x26))), unchecked((byte)unchecked(
			(int)(0x5F))), unchecked((byte)unchecked((int)(0x95))), unchecked((byte)unchecked(
			(int)(0xF8))), unchecked((byte)unchecked((int)(0xD0))), unchecked((byte)unchecked(
			(int)(0x89))), unchecked((byte)unchecked((int)(0x24))), unchecked((byte)unchecked(
			(int)(0x85))), unchecked((byte)unchecked((int)(0x67))), unchecked((byte)unchecked(
			(int)(0xC6))), unchecked((byte)unchecked((int)(0x1F))), unchecked((byte)unchecked(
			(int)(0x27))), unchecked((byte)unchecked((int)(0x44))), unchecked((byte)unchecked(
			(int)(0xD2))), unchecked((byte)unchecked((int)(0xEE))), unchecked((byte)unchecked(
			(int)(0xCF))), unchecked((byte)unchecked((int)(0x65))), unchecked((byte)unchecked(
			(int)(0xED))), unchecked((byte)unchecked((int)(0xFF))), unchecked((byte)unchecked(
			(int)(0x07))), unchecked((byte)unchecked((int)(0xC7))), unchecked((byte)unchecked(
			(int)(0x46))), unchecked((byte)unchecked((int)(0xA1))), unchecked((byte)unchecked(
			(int)(0x78))), unchecked((byte)unchecked((int)(0x16))), unchecked((byte)unchecked(
			(int)(0x0C))), unchecked((byte)unchecked((int)(0xED))), unchecked((byte)unchecked(
			(int)(0xE9))), unchecked((byte)unchecked((int)(0x2D))), unchecked((byte)unchecked(
			(int)(0x62))), unchecked((byte)unchecked((int)(0xD4))) };

		/// <summary>
		/// value of the "AccessVersion" property for access 2000 dbs:
		/// <code>"08.50"</code>
		/// 
		/// </summary>
		private static readonly string ACCESS_VERSION_2000 = "08.50";

		/// <summary>
		/// value of the "AccessVersion" property for access 2002/2003 dbs
		/// <code>"09.50"</code>
		/// 
		/// </summary>
		private static readonly string ACCESS_VERSION_2003 = "09.50";

		/// <summary>known intro bytes for property maps</summary>
		internal static readonly byte[][] PROPERTY_MAP_TYPES = new byte[][] { new byte[] 
			{ (byte)('M'), (byte)('R'), (byte)('2'), (byte)('\0') }, new byte[] { (byte)('K'
			), (byte)('K'), (byte)('D'), (byte)('\0') } };

		public sealed class PossibleFileFormats
		{
			public static readonly IDictionary<string, Database.FileFormat> POSSIBLE_VERSION_3
				 = Sharpen.Collections.SingletonMap((string)null, Database.FileFormat.V1997);

			public static readonly IDictionary<string, Database.FileFormat> POSSIBLE_VERSION_4
				 = new Dictionary<string, Database.FileFormat>();

			public static readonly IDictionary<string, Database.FileFormat> POSSIBLE_VERSION_12
				 = Sharpen.Collections.SingletonMap((string)null, Database.FileFormat.V2007);

			public static readonly IDictionary<string, Database.FileFormat> POSSIBLE_VERSION_14
				 = Sharpen.Collections.SingletonMap((string)null, Database.FileFormat.V2010);

			public static readonly IDictionary<string, Database.FileFormat> POSSIBLE_VERSION_MSISAM
				 = Sharpen.Collections.SingletonMap((string)null, Database.FileFormat.MSISAM);

			static PossibleFileFormats()
			{
				//2kb minus some overhead
				// access 2000+
				// access 97
				// use nested inner class to avoid problematic static init loops
				POSSIBLE_VERSION_4.Put(ACCESS_VERSION_2000, Database.FileFormat.V2000);
				POSSIBLE_VERSION_4.Put(ACCESS_VERSION_2003, Database.FileFormat.V2003);
			}
		}

		/// <summary>the JetFormat constants for the Jet database version "3"</summary>
		public static readonly JetFormat VERSION_3 = new JetFormat.Jet3Format();

		/// <summary>the JetFormat constants for the Jet database version "4"</summary>
		public static readonly JetFormat VERSION_4 = new JetFormat.Jet4Format();

		/// <summary>the JetFormat constants for the MSISAM database</summary>
		public static readonly JetFormat VERSION_MSISAM = new JetFormat.MSISAMFormat();

		/// <summary>the JetFormat constants for the Jet database version "12"</summary>
		public static readonly JetFormat VERSION_12 = new JetFormat.Jet12Format();

		/// <summary>the JetFormat constants for the Jet database version "14"</summary>
		public static readonly JetFormat VERSION_14 = new JetFormat.Jet14Format();

		/// <summary>the name of this format</summary>
		private readonly string _name;

		/// <summary>the read/write mode of this format</summary>
		public readonly bool READ_ONLY;

		/// <summary>whether or not we can use indexes in this format</summary>
		public readonly bool INDEXES_SUPPORTED;

		/// <summary>type of page encoding supported</summary>
		public readonly JetFormat.CodecType CODEC_TYPE;

		/// <summary>Database page size in bytes</summary>
		public readonly int PAGE_SIZE;

		public readonly long MAX_DATABASE_SIZE;

		public readonly int MAX_ROW_SIZE;

		public readonly int DATA_PAGE_INITIAL_FREE_SPACE;

		public readonly int OFFSET_MASKED_HEADER;

		public readonly byte[] HEADER_MASK;

		public readonly int OFFSET_HEADER_DATE;

		public readonly int OFFSET_PASSWORD;

		public readonly int SIZE_PASSWORD;

		public readonly int OFFSET_SORT_ORDER;

		public readonly int SIZE_SORT_ORDER;

		public readonly int OFFSET_CODE_PAGE;

		public readonly int OFFSET_ENCODING_KEY;

		public readonly int OFFSET_NEXT_TABLE_DEF_PAGE;

		public readonly int OFFSET_NUM_ROWS;

		public readonly int OFFSET_NEXT_AUTO_NUMBER;

		public readonly int OFFSET_TABLE_TYPE;

		public readonly int OFFSET_MAX_COLS;

		public readonly int OFFSET_NUM_VAR_COLS;

		public readonly int OFFSET_NUM_COLS;

		public readonly int OFFSET_NUM_INDEX_SLOTS;

		public readonly int OFFSET_NUM_INDEXES;

		public readonly int OFFSET_OWNED_PAGES;

		public readonly int OFFSET_FREE_SPACE_PAGES;

		public readonly int OFFSET_INDEX_DEF_BLOCK;

		public readonly int SIZE_INDEX_COLUMN_BLOCK;

		public readonly int SIZE_INDEX_INFO_BLOCK;

		public readonly int OFFSET_COLUMN_TYPE;

		public readonly int OFFSET_COLUMN_NUMBER;

		public readonly int OFFSET_COLUMN_PRECISION;

		public readonly int OFFSET_COLUMN_SCALE;

		public readonly int OFFSET_COLUMN_SORT_ORDER;

		public readonly int OFFSET_COLUMN_CODE_PAGE;

		public readonly int OFFSET_COLUMN_FLAGS;

		public readonly int OFFSET_COLUMN_COMPRESSED_UNICODE;

		public readonly int OFFSET_COLUMN_LENGTH;

		public readonly int OFFSET_COLUMN_VARIABLE_TABLE_INDEX;

		public readonly int OFFSET_COLUMN_FIXED_DATA_OFFSET;

		public readonly int OFFSET_COLUMN_FIXED_DATA_ROW_OFFSET;

		public readonly int OFFSET_TABLE_DEF_LOCATION;

		public readonly int OFFSET_ROW_START;

		public readonly int OFFSET_USAGE_MAP_START;

		public readonly int OFFSET_USAGE_MAP_PAGE_DATA;

		public readonly int OFFSET_REFERENCE_MAP_PAGE_NUMBERS;

		public readonly int OFFSET_FREE_SPACE;

		public readonly int OFFSET_NUM_ROWS_ON_DATA_PAGE;

		public readonly int MAX_NUM_ROWS_ON_DATA_PAGE;

		public readonly int OFFSET_INDEX_COMPRESSED_BYTE_COUNT;

		public readonly int OFFSET_INDEX_ENTRY_MASK;

		public readonly int OFFSET_PREV_INDEX_PAGE;

		public readonly int OFFSET_NEXT_INDEX_PAGE;

		public readonly int OFFSET_CHILD_TAIL_INDEX_PAGE;

		public readonly int SIZE_INDEX_DEFINITION;

		public readonly int SIZE_COLUMN_HEADER;

		public readonly int SIZE_ROW_LOCATION;

		public readonly int SIZE_LONG_VALUE_DEF;

		public readonly int MAX_INLINE_LONG_VALUE_SIZE;

		public readonly int MAX_LONG_VALUE_ROW_SIZE;

		public readonly int SIZE_TDEF_HEADER;

		public readonly int SIZE_TDEF_TRAILER;

		public readonly int SIZE_COLUMN_DEF_BLOCK;

		public readonly int SIZE_INDEX_ENTRY_MASK;

		public readonly int SKIP_BEFORE_INDEX_FLAGS;

		public readonly int SKIP_AFTER_INDEX_FLAGS;

		public readonly int SKIP_BEFORE_INDEX_SLOT;

		public readonly int SKIP_AFTER_INDEX_SLOT;

		public readonly int SKIP_BEFORE_INDEX;

		public readonly int SIZE_NAME_LENGTH;

		public readonly int SIZE_ROW_COLUMN_COUNT;

		public readonly int SIZE_ROW_VAR_COL_OFFSET;

		public readonly int USAGE_MAP_TABLE_BYTE_LENGTH;

		public readonly int MAX_COLUMNS_PER_TABLE;

		public readonly int MAX_TABLE_NAME_LENGTH;

		public readonly int MAX_COLUMN_NAME_LENGTH;

		public readonly int MAX_INDEX_NAME_LENGTH;

		public readonly bool LEGACY_NUMERIC_INDEXES;

		public readonly Encoding CHARSET;

		public readonly Column.SortOrder DEFAULT_SORT_ORDER;

		//These constants are populated by this class's constructor.  They can't be
		//populated by the subclass's constructor because they are final, and Java
		//doesn't allow this; hence all the abstract defineXXX() methods.
		/// <param name="channel">the database file.</param>
		/// <returns>The Jet Format represented in the passed-in file</returns>
		/// <exception cref="System.IO.IOException">if the database file format is unsupported.
		/// 	</exception>
		public static JetFormat GetFormat(FileChannel channel)
		{
			ByteBuffer buffer = ByteBuffer.Allocate(HEADER_LENGTH);
			int bytesRead = channel.Read(buffer, 0L);
			if (bytesRead < HEADER_LENGTH)
			{
				throw new IOException("Empty database file");
			}
			buffer.Flip();
			byte version = buffer.Get(OFFSET_VERSION);
			if (version == CODE_VERSION_3)
			{
				return VERSION_3;
			}
			else
			{
				if (version == CODE_VERSION_4)
				{
					if (ByteUtil.MatchesRange(buffer, OFFSET_ENGINE_NAME, MSISAM_ENGINE))
					{
						return VERSION_MSISAM;
					}
					return VERSION_4;
				}
				else
				{
					if (version == CODE_VERSION_12)
					{
						return VERSION_12;
					}
					else
					{
						if (version == CODE_VERSION_14)
						{
							return VERSION_14;
						}
					}
				}
			}
			throw new IOException("Unsupported " + ((((sbyte)version) < CODE_VERSION_3) ? "older"
				 : "newer") + " version: " + version);
		}

		private JetFormat(string name)
		{
			_name = name;
			READ_ONLY = DefineReadOnly();
			INDEXES_SUPPORTED = DefineIndexesSupported();
			CODEC_TYPE = DefineCodecType();
			PAGE_SIZE = DefinePageSize();
			MAX_DATABASE_SIZE = DefineMaxDatabaseSize();
			MAX_ROW_SIZE = DefineMaxRowSize();
			DATA_PAGE_INITIAL_FREE_SPACE = DefineDataPageInitialFreeSpace();
			OFFSET_MASKED_HEADER = DefineOffsetMaskedHeader();
			HEADER_MASK = DefineHeaderMask();
			OFFSET_HEADER_DATE = DefineOffsetHeaderDate();
			OFFSET_PASSWORD = DefineOffsetPassword();
			SIZE_PASSWORD = DefineSizePassword();
			OFFSET_SORT_ORDER = DefineOffsetSortOrder();
			SIZE_SORT_ORDER = DefineSizeSortOrder();
			OFFSET_CODE_PAGE = DefineOffsetCodePage();
			OFFSET_ENCODING_KEY = DefineOffsetEncodingKey();
			OFFSET_NEXT_TABLE_DEF_PAGE = DefineOffsetNextTableDefPage();
			OFFSET_NUM_ROWS = DefineOffsetNumRows();
			OFFSET_NEXT_AUTO_NUMBER = DefineOffsetNextAutoNumber();
			OFFSET_TABLE_TYPE = DefineOffsetTableType();
			OFFSET_MAX_COLS = DefineOffsetMaxCols();
			OFFSET_NUM_VAR_COLS = DefineOffsetNumVarCols();
			OFFSET_NUM_COLS = DefineOffsetNumCols();
			OFFSET_NUM_INDEX_SLOTS = DefineOffsetNumIndexSlots();
			OFFSET_NUM_INDEXES = DefineOffsetNumIndexes();
			OFFSET_OWNED_PAGES = DefineOffsetOwnedPages();
			OFFSET_FREE_SPACE_PAGES = DefineOffsetFreeSpacePages();
			OFFSET_INDEX_DEF_BLOCK = DefineOffsetIndexDefBlock();
			SIZE_INDEX_COLUMN_BLOCK = DefineSizeIndexColumnBlock();
			SIZE_INDEX_INFO_BLOCK = DefineSizeIndexInfoBlock();
			OFFSET_COLUMN_TYPE = DefineOffsetColumnType();
			OFFSET_COLUMN_NUMBER = DefineOffsetColumnNumber();
			OFFSET_COLUMN_PRECISION = DefineOffsetColumnPrecision();
			OFFSET_COLUMN_SCALE = DefineOffsetColumnScale();
			OFFSET_COLUMN_SORT_ORDER = DefineOffsetColumnSortOrder();
			OFFSET_COLUMN_CODE_PAGE = DefineOffsetColumnCodePage();
			OFFSET_COLUMN_FLAGS = DefineOffsetColumnFlags();
			OFFSET_COLUMN_COMPRESSED_UNICODE = DefineOffsetColumnCompressedUnicode();
			OFFSET_COLUMN_LENGTH = DefineOffsetColumnLength();
			OFFSET_COLUMN_VARIABLE_TABLE_INDEX = DefineOffsetColumnVariableTableIndex();
			OFFSET_COLUMN_FIXED_DATA_OFFSET = DefineOffsetColumnFixedDataOffset();
			OFFSET_COLUMN_FIXED_DATA_ROW_OFFSET = DefineOffsetColumnFixedDataRowOffset();
			OFFSET_TABLE_DEF_LOCATION = DefineOffsetTableDefLocation();
			OFFSET_ROW_START = DefineOffsetRowStart();
			OFFSET_USAGE_MAP_START = DefineOffsetUsageMapStart();
			OFFSET_USAGE_MAP_PAGE_DATA = DefineOffsetUsageMapPageData();
			OFFSET_REFERENCE_MAP_PAGE_NUMBERS = DefineOffsetReferenceMapPageNumbers();
			OFFSET_FREE_SPACE = DefineOffsetFreeSpace();
			OFFSET_NUM_ROWS_ON_DATA_PAGE = DefineOffsetNumRowsOnDataPage();
			MAX_NUM_ROWS_ON_DATA_PAGE = DefineMaxNumRowsOnDataPage();
			OFFSET_INDEX_COMPRESSED_BYTE_COUNT = DefineOffsetIndexCompressedByteCount();
			OFFSET_INDEX_ENTRY_MASK = DefineOffsetIndexEntryMask();
			OFFSET_PREV_INDEX_PAGE = DefineOffsetPrevIndexPage();
			OFFSET_NEXT_INDEX_PAGE = DefineOffsetNextIndexPage();
			OFFSET_CHILD_TAIL_INDEX_PAGE = DefineOffsetChildTailIndexPage();
			SIZE_INDEX_DEFINITION = DefineSizeIndexDefinition();
			SIZE_COLUMN_HEADER = DefineSizeColumnHeader();
			SIZE_ROW_LOCATION = DefineSizeRowLocation();
			SIZE_LONG_VALUE_DEF = DefineSizeLongValueDef();
			MAX_INLINE_LONG_VALUE_SIZE = DefineMaxInlineLongValueSize();
			MAX_LONG_VALUE_ROW_SIZE = DefineMaxLongValueRowSize();
			SIZE_TDEF_HEADER = DefineSizeTdefHeader();
			SIZE_TDEF_TRAILER = DefineSizeTdefTrailer();
			SIZE_COLUMN_DEF_BLOCK = DefineSizeColumnDefBlock();
			SIZE_INDEX_ENTRY_MASK = DefineSizeIndexEntryMask();
			SKIP_BEFORE_INDEX_FLAGS = DefineSkipBeforeIndexFlags();
			SKIP_AFTER_INDEX_FLAGS = DefineSkipAfterIndexFlags();
			SKIP_BEFORE_INDEX_SLOT = DefineSkipBeforeIndexSlot();
			SKIP_AFTER_INDEX_SLOT = DefineSkipAfterIndexSlot();
			SKIP_BEFORE_INDEX = DefineSkipBeforeIndex();
			SIZE_NAME_LENGTH = DefineSizeNameLength();
			SIZE_ROW_COLUMN_COUNT = DefineSizeRowColumnCount();
			SIZE_ROW_VAR_COL_OFFSET = DefineSizeRowVarColOffset();
			USAGE_MAP_TABLE_BYTE_LENGTH = DefineUsageMapTableByteLength();
			MAX_COLUMNS_PER_TABLE = DefineMaxColumnsPerTable();
			MAX_TABLE_NAME_LENGTH = DefineMaxTableNameLength();
			MAX_COLUMN_NAME_LENGTH = DefineMaxColumnNameLength();
			MAX_INDEX_NAME_LENGTH = DefineMaxIndexNameLength();
			LEGACY_NUMERIC_INDEXES = DefineLegacyNumericIndexes();
			CHARSET = DefineCharset();
			DEFAULT_SORT_ORDER = DefineDefaultSortOrder();
		}

		protected internal abstract bool DefineReadOnly();

		protected internal abstract bool DefineIndexesSupported();

		protected internal abstract JetFormat.CodecType DefineCodecType();

		protected internal abstract int DefinePageSize();

		protected internal abstract long DefineMaxDatabaseSize();

		protected internal abstract int DefineMaxRowSize();

		protected internal abstract int DefineDataPageInitialFreeSpace();

		protected internal abstract int DefineOffsetMaskedHeader();

		protected internal abstract byte[] DefineHeaderMask();

		protected internal abstract int DefineOffsetHeaderDate();

		protected internal abstract int DefineOffsetPassword();

		protected internal abstract int DefineSizePassword();

		protected internal abstract int DefineOffsetSortOrder();

		protected internal abstract int DefineSizeSortOrder();

		protected internal abstract int DefineOffsetCodePage();

		protected internal abstract int DefineOffsetEncodingKey();

		protected internal abstract int DefineOffsetNextTableDefPage();

		protected internal abstract int DefineOffsetNumRows();

		protected internal abstract int DefineOffsetNextAutoNumber();

		protected internal abstract int DefineOffsetTableType();

		protected internal abstract int DefineOffsetMaxCols();

		protected internal abstract int DefineOffsetNumVarCols();

		protected internal abstract int DefineOffsetNumCols();

		protected internal abstract int DefineOffsetNumIndexSlots();

		protected internal abstract int DefineOffsetNumIndexes();

		protected internal abstract int DefineOffsetOwnedPages();

		protected internal abstract int DefineOffsetFreeSpacePages();

		protected internal abstract int DefineOffsetIndexDefBlock();

		protected internal abstract int DefineSizeIndexColumnBlock();

		protected internal abstract int DefineSizeIndexInfoBlock();

		protected internal abstract int DefineOffsetColumnType();

		protected internal abstract int DefineOffsetColumnNumber();

		protected internal abstract int DefineOffsetColumnPrecision();

		protected internal abstract int DefineOffsetColumnScale();

		protected internal abstract int DefineOffsetColumnSortOrder();

		protected internal abstract int DefineOffsetColumnCodePage();

		protected internal abstract int DefineOffsetColumnFlags();

		protected internal abstract int DefineOffsetColumnCompressedUnicode();

		protected internal abstract int DefineOffsetColumnLength();

		protected internal abstract int DefineOffsetColumnVariableTableIndex();

		protected internal abstract int DefineOffsetColumnFixedDataOffset();

		protected internal abstract int DefineOffsetColumnFixedDataRowOffset();

		protected internal abstract int DefineOffsetTableDefLocation();

		protected internal abstract int DefineOffsetRowStart();

		protected internal abstract int DefineOffsetUsageMapStart();

		protected internal abstract int DefineOffsetUsageMapPageData();

		protected internal abstract int DefineOffsetReferenceMapPageNumbers();

		protected internal abstract int DefineOffsetFreeSpace();

		protected internal abstract int DefineOffsetNumRowsOnDataPage();

		protected internal abstract int DefineMaxNumRowsOnDataPage();

		protected internal abstract int DefineOffsetIndexCompressedByteCount();

		protected internal abstract int DefineOffsetIndexEntryMask();

		protected internal abstract int DefineOffsetPrevIndexPage();

		protected internal abstract int DefineOffsetNextIndexPage();

		protected internal abstract int DefineOffsetChildTailIndexPage();

		protected internal abstract int DefineSizeIndexDefinition();

		protected internal abstract int DefineSizeColumnHeader();

		protected internal abstract int DefineSizeRowLocation();

		protected internal abstract int DefineSizeLongValueDef();

		protected internal abstract int DefineMaxInlineLongValueSize();

		protected internal abstract int DefineMaxLongValueRowSize();

		protected internal abstract int DefineSizeTdefHeader();

		protected internal abstract int DefineSizeTdefTrailer();

		protected internal abstract int DefineSizeColumnDefBlock();

		protected internal abstract int DefineSizeIndexEntryMask();

		protected internal abstract int DefineSkipBeforeIndexFlags();

		protected internal abstract int DefineSkipAfterIndexFlags();

		protected internal abstract int DefineSkipBeforeIndexSlot();

		protected internal abstract int DefineSkipAfterIndexSlot();

		protected internal abstract int DefineSkipBeforeIndex();

		protected internal abstract int DefineSizeNameLength();

		protected internal abstract int DefineSizeRowColumnCount();

		protected internal abstract int DefineSizeRowVarColOffset();

		protected internal abstract int DefineUsageMapTableByteLength();

		protected internal abstract int DefineMaxColumnsPerTable();

		protected internal abstract int DefineMaxTableNameLength();

		protected internal abstract int DefineMaxColumnNameLength();

		protected internal abstract int DefineMaxIndexNameLength();

		protected internal abstract Encoding DefineCharset();

		protected internal abstract Column.SortOrder DefineDefaultSortOrder();

		protected internal abstract bool DefineLegacyNumericIndexes();

		protected internal abstract IDictionary<string, Database.FileFormat> GetPossibleFileFormats
			();

		public override string ToString()
		{
			return _name;
		}

		private class Jet3Format : JetFormat
		{
			public Jet3Format() : base("VERSION_3")
			{
			}

			protected internal override bool DefineReadOnly()
			{
				return true;
			}

			protected internal override bool DefineIndexesSupported()
			{
				return false;
			}

			protected internal override JetFormat.CodecType DefineCodecType()
			{
				return JetFormat.CodecType.JET;
			}

			protected internal override int DefinePageSize()
			{
				return 2048;
			}

			protected internal override long DefineMaxDatabaseSize()
			{
				return (1L * 1024L * 1024L * 1024L);
			}

			protected internal override int DefineMaxRowSize()
			{
				return 2012;
			}

			protected internal override int DefineDataPageInitialFreeSpace()
			{
				return PAGE_SIZE - 14;
			}

			protected internal override int DefineOffsetMaskedHeader()
			{
				return 24;
			}

			protected internal override byte[] DefineHeaderMask()
			{
				return ByteUtil.CopyOf(BASE_HEADER_MASK, BASE_HEADER_MASK.Length - 2);
			}

			protected internal override int DefineOffsetHeaderDate()
			{
				return -1;
			}

			protected internal override int DefineOffsetPassword()
			{
				return 66;
			}

			protected internal override int DefineSizePassword()
			{
				return 20;
			}

			protected internal override int DefineOffsetSortOrder()
			{
				return 58;
			}

			protected internal override int DefineSizeSortOrder()
			{
				return 2;
			}

			protected internal override int DefineOffsetCodePage()
			{
				return 60;
			}

			protected internal override int DefineOffsetEncodingKey()
			{
				return 62;
			}

			protected internal override int DefineOffsetNextTableDefPage()
			{
				return 4;
			}

			protected internal override int DefineOffsetNumRows()
			{
				return 12;
			}

			protected internal override int DefineOffsetNextAutoNumber()
			{
				return 20;
			}

			protected internal override int DefineOffsetTableType()
			{
				return 20;
			}

			protected internal override int DefineOffsetMaxCols()
			{
				return 21;
			}

			protected internal override int DefineOffsetNumVarCols()
			{
				return 23;
			}

			protected internal override int DefineOffsetNumCols()
			{
				return 25;
			}

			protected internal override int DefineOffsetNumIndexSlots()
			{
				return 27;
			}

			protected internal override int DefineOffsetNumIndexes()
			{
				return 31;
			}

			protected internal override int DefineOffsetOwnedPages()
			{
				return 35;
			}

			protected internal override int DefineOffsetFreeSpacePages()
			{
				return 39;
			}

			protected internal override int DefineOffsetIndexDefBlock()
			{
				return 43;
			}

			protected internal override int DefineSizeIndexColumnBlock()
			{
				return 39;
			}

			protected internal override int DefineSizeIndexInfoBlock()
			{
				return 20;
			}

			protected internal override int DefineOffsetColumnType()
			{
				return 0;
			}

			protected internal override int DefineOffsetColumnNumber()
			{
				return 1;
			}

			protected internal override int DefineOffsetColumnPrecision()
			{
				return 11;
			}

			protected internal override int DefineOffsetColumnScale()
			{
				return 12;
			}

			protected internal override int DefineOffsetColumnSortOrder()
			{
				return 9;
			}

			protected internal override int DefineOffsetColumnCodePage()
			{
				return 11;
			}

			protected internal override int DefineOffsetColumnFlags()
			{
				return 13;
			}

			protected internal override int DefineOffsetColumnCompressedUnicode()
			{
				return 16;
			}

			protected internal override int DefineOffsetColumnLength()
			{
				return 16;
			}

			protected internal override int DefineOffsetColumnVariableTableIndex()
			{
				return 3;
			}

			protected internal override int DefineOffsetColumnFixedDataOffset()
			{
				return 14;
			}

			protected internal override int DefineOffsetColumnFixedDataRowOffset()
			{
				return 1;
			}

			protected internal override int DefineOffsetTableDefLocation()
			{
				return 4;
			}

			protected internal override int DefineOffsetRowStart()
			{
				return 10;
			}

			protected internal override int DefineOffsetUsageMapStart()
			{
				return 5;
			}

			protected internal override int DefineOffsetUsageMapPageData()
			{
				return 4;
			}

			protected internal override int DefineOffsetReferenceMapPageNumbers()
			{
				return 1;
			}

			protected internal override int DefineOffsetFreeSpace()
			{
				return 2;
			}

			protected internal override int DefineOffsetNumRowsOnDataPage()
			{
				return 8;
			}

			protected internal override int DefineMaxNumRowsOnDataPage()
			{
				return 255;
			}

			protected internal override int DefineOffsetIndexCompressedByteCount()
			{
				return 20;
			}

			protected internal override int DefineOffsetIndexEntryMask()
			{
				return 22;
			}

			protected internal override int DefineOffsetPrevIndexPage()
			{
				return 8;
			}

			protected internal override int DefineOffsetNextIndexPage()
			{
				return 12;
			}

			protected internal override int DefineOffsetChildTailIndexPage()
			{
				return 16;
			}

			protected internal override int DefineSizeIndexDefinition()
			{
				return 8;
			}

			protected internal override int DefineSizeColumnHeader()
			{
				return 18;
			}

			protected internal override int DefineSizeRowLocation()
			{
				return 2;
			}

			protected internal override int DefineSizeLongValueDef()
			{
				return 12;
			}

			protected internal override int DefineMaxInlineLongValueSize()
			{
				return 64;
			}

			protected internal override int DefineMaxLongValueRowSize()
			{
				return 2032;
			}

			protected internal override int DefineSizeTdefHeader()
			{
				return 63;
			}

			protected internal override int DefineSizeTdefTrailer()
			{
				return 2;
			}

			protected internal override int DefineSizeColumnDefBlock()
			{
				return 25;
			}

			protected internal override int DefineSizeIndexEntryMask()
			{
				return 226;
			}

			protected internal override int DefineSkipBeforeIndexFlags()
			{
				return 0;
			}

			protected internal override int DefineSkipAfterIndexFlags()
			{
				return 0;
			}

			protected internal override int DefineSkipBeforeIndexSlot()
			{
				return 0;
			}

			protected internal override int DefineSkipAfterIndexSlot()
			{
				return 0;
			}

			protected internal override int DefineSkipBeforeIndex()
			{
				return 0;
			}

			protected internal override int DefineSizeNameLength()
			{
				return 1;
			}

			protected internal override int DefineSizeRowColumnCount()
			{
				return 1;
			}

			protected internal override int DefineSizeRowVarColOffset()
			{
				return 1;
			}

			protected internal override int DefineUsageMapTableByteLength()
			{
				return 128;
			}

			protected internal override int DefineMaxColumnsPerTable()
			{
				return 255;
			}

			protected internal override int DefineMaxTableNameLength()
			{
				return 64;
			}

			protected internal override int DefineMaxColumnNameLength()
			{
				return 64;
			}

			protected internal override int DefineMaxIndexNameLength()
			{
				return 64;
			}

			protected internal override bool DefineLegacyNumericIndexes()
			{
				return true;
			}

			protected internal override Encoding DefineCharset()
			{
				return Encoding.Default;
			}

			protected internal override Column.SortOrder DefineDefaultSortOrder()
			{
				return Column.GENERAL_LEGACY_SORT_ORDER;
			}

			protected internal override IDictionary<string, Database.FileFormat> GetPossibleFileFormats
				()
			{
				return JetFormat.PossibleFileFormats.POSSIBLE_VERSION_3;
			}
		}

		public class Jet4Format : JetFormat
		{
			public Jet4Format() : this("VERSION_4")
			{
			}

			public Jet4Format(string name) : base(name)
			{
			}

			protected internal override bool DefineReadOnly()
			{
				return false;
			}

			protected internal override bool DefineIndexesSupported()
			{
				return true;
			}

			protected internal override JetFormat.CodecType DefineCodecType()
			{
				return JetFormat.CodecType.JET;
			}

			protected internal override int DefinePageSize()
			{
				return 4096;
			}

			protected internal override long DefineMaxDatabaseSize()
			{
				return (2L * 1024L * 1024L * 1024L);
			}

			protected internal override int DefineMaxRowSize()
			{
				return 4060;
			}

			protected internal override int DefineDataPageInitialFreeSpace()
			{
				return PAGE_SIZE - 14;
			}

			protected internal override int DefineOffsetMaskedHeader()
			{
				return 24;
			}

			protected internal override byte[] DefineHeaderMask()
			{
				return BASE_HEADER_MASK;
			}

			protected internal override int DefineOffsetHeaderDate()
			{
				return 114;
			}

			protected internal override int DefineOffsetPassword()
			{
				return 66;
			}

			protected internal override int DefineSizePassword()
			{
				return 40;
			}

			protected internal override int DefineOffsetSortOrder()
			{
				return 110;
			}

			protected internal override int DefineSizeSortOrder()
			{
				return 4;
			}

			protected internal override int DefineOffsetCodePage()
			{
				return 60;
			}

			protected internal override int DefineOffsetEncodingKey()
			{
				return 62;
			}

			protected internal override int DefineOffsetNextTableDefPage()
			{
				return 4;
			}

			protected internal override int DefineOffsetNumRows()
			{
				return 16;
			}

			protected internal override int DefineOffsetNextAutoNumber()
			{
				return 20;
			}

			protected internal override int DefineOffsetTableType()
			{
				return 40;
			}

			protected internal override int DefineOffsetMaxCols()
			{
				return 41;
			}

			protected internal override int DefineOffsetNumVarCols()
			{
				return 43;
			}

			protected internal override int DefineOffsetNumCols()
			{
				return 45;
			}

			protected internal override int DefineOffsetNumIndexSlots()
			{
				return 47;
			}

			protected internal override int DefineOffsetNumIndexes()
			{
				return 51;
			}

			protected internal override int DefineOffsetOwnedPages()
			{
				return 55;
			}

			protected internal override int DefineOffsetFreeSpacePages()
			{
				return 59;
			}

			protected internal override int DefineOffsetIndexDefBlock()
			{
				return 63;
			}

			protected internal override int DefineSizeIndexColumnBlock()
			{
				return 52;
			}

			protected internal override int DefineSizeIndexInfoBlock()
			{
				return 28;
			}

			protected internal override int DefineOffsetColumnType()
			{
				return 0;
			}

			protected internal override int DefineOffsetColumnNumber()
			{
				return 5;
			}

			protected internal override int DefineOffsetColumnPrecision()
			{
				return 11;
			}

			protected internal override int DefineOffsetColumnScale()
			{
				return 12;
			}

			protected internal override int DefineOffsetColumnSortOrder()
			{
				return 11;
			}

			protected internal override int DefineOffsetColumnCodePage()
			{
				return -1;
			}

			protected internal override int DefineOffsetColumnFlags()
			{
				return 15;
			}

			protected internal override int DefineOffsetColumnCompressedUnicode()
			{
				return 16;
			}

			protected internal override int DefineOffsetColumnLength()
			{
				return 23;
			}

			protected internal override int DefineOffsetColumnVariableTableIndex()
			{
				return 7;
			}

			protected internal override int DefineOffsetColumnFixedDataOffset()
			{
				return 21;
			}

			protected internal override int DefineOffsetColumnFixedDataRowOffset()
			{
				return 2;
			}

			protected internal override int DefineOffsetTableDefLocation()
			{
				return 4;
			}

			protected internal override int DefineOffsetRowStart()
			{
				return 14;
			}

			protected internal override int DefineOffsetUsageMapStart()
			{
				return 5;
			}

			protected internal override int DefineOffsetUsageMapPageData()
			{
				return 4;
			}

			protected internal override int DefineOffsetReferenceMapPageNumbers()
			{
				return 1;
			}

			protected internal override int DefineOffsetFreeSpace()
			{
				return 2;
			}

			protected internal override int DefineOffsetNumRowsOnDataPage()
			{
				return 12;
			}

			protected internal override int DefineMaxNumRowsOnDataPage()
			{
				return 255;
			}

			protected internal override int DefineOffsetIndexCompressedByteCount()
			{
				return 24;
			}

			protected internal override int DefineOffsetIndexEntryMask()
			{
				return 27;
			}

			protected internal override int DefineOffsetPrevIndexPage()
			{
				return 12;
			}

			protected internal override int DefineOffsetNextIndexPage()
			{
				return 16;
			}

			protected internal override int DefineOffsetChildTailIndexPage()
			{
				return 20;
			}

			protected internal override int DefineSizeIndexDefinition()
			{
				return 12;
			}

			protected internal override int DefineSizeColumnHeader()
			{
				return 25;
			}

			protected internal override int DefineSizeRowLocation()
			{
				return 2;
			}

			protected internal override int DefineSizeLongValueDef()
			{
				return 12;
			}

			protected internal override int DefineMaxInlineLongValueSize()
			{
				return 64;
			}

			protected internal override int DefineMaxLongValueRowSize()
			{
				return 4076;
			}

			protected internal override int DefineSizeTdefHeader()
			{
				return 63;
			}

			protected internal override int DefineSizeTdefTrailer()
			{
				return 2;
			}

			protected internal override int DefineSizeColumnDefBlock()
			{
				return 25;
			}

			protected internal override int DefineSizeIndexEntryMask()
			{
				return 453;
			}

			protected internal override int DefineSkipBeforeIndexFlags()
			{
				return 4;
			}

			protected internal override int DefineSkipAfterIndexFlags()
			{
				return 5;
			}

			protected internal override int DefineSkipBeforeIndexSlot()
			{
				return 4;
			}

			protected internal override int DefineSkipAfterIndexSlot()
			{
				return 4;
			}

			protected internal override int DefineSkipBeforeIndex()
			{
				return 4;
			}

			protected internal override int DefineSizeNameLength()
			{
				return 2;
			}

			protected internal override int DefineSizeRowColumnCount()
			{
				return 2;
			}

			protected internal override int DefineSizeRowVarColOffset()
			{
				return 2;
			}

			protected internal override int DefineUsageMapTableByteLength()
			{
				return 64;
			}

			protected internal override int DefineMaxColumnsPerTable()
			{
				return 255;
			}

			protected internal override int DefineMaxTableNameLength()
			{
				return 64;
			}

			protected internal override int DefineMaxColumnNameLength()
			{
				return 64;
			}

			protected internal override int DefineMaxIndexNameLength()
			{
				return 64;
			}

			protected internal override bool DefineLegacyNumericIndexes()
			{
				return true;
			}

			protected internal override Encoding DefineCharset()
			{
				return Sharpen.Extensions.GetEncoding("UTF-16LE");
			}

			protected internal override Column.SortOrder DefineDefaultSortOrder()
			{
				return Column.GENERAL_LEGACY_SORT_ORDER;
			}

			protected internal override IDictionary<string, Database.FileFormat> GetPossibleFileFormats
				()
			{
				return JetFormat.PossibleFileFormats.POSSIBLE_VERSION_4;
			}
		}

		private sealed class MSISAMFormat : JetFormat.Jet4Format
		{
			public MSISAMFormat() : base("MSISAM")
			{
			}

			protected internal override JetFormat.CodecType DefineCodecType()
			{
				return JetFormat.CodecType.MSISAM;
			}

			protected internal override IDictionary<string, Database.FileFormat> GetPossibleFileFormats
				()
			{
				return JetFormat.PossibleFileFormats.POSSIBLE_VERSION_MSISAM;
			}
		}

		private class Jet12Format : JetFormat.Jet4Format
		{
			public Jet12Format() : base("VERSION_12")
			{
			}

			public Jet12Format(string name) : base(name)
			{
			}

			protected internal override bool DefineLegacyNumericIndexes()
			{
				return false;
			}

			protected internal override IDictionary<string, Database.FileFormat> GetPossibleFileFormats
				()
			{
				return JetFormat.PossibleFileFormats.POSSIBLE_VERSION_12;
			}
		}

		private sealed class Jet14Format : JetFormat.Jet12Format
		{
			public Jet14Format() : base("VERSION_14")
			{
			}

			protected internal override Column.SortOrder DefineDefaultSortOrder()
			{
				return Column.GENERAL_SORT_ORDER;
			}

			protected internal override IDictionary<string, Database.FileFormat> GetPossibleFileFormats
				()
			{
				return JetFormat.PossibleFileFormats.POSSIBLE_VERSION_14;
			}
		}
	}
}
