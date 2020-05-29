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
using System.Reflection;
using System.Text;

namespace HealthMarketScience.Jackcess
{
    /// <summary>An Access database.</summary>
    /// <remarks>
    /// An Access database.
    /// <p>
    /// There is optional support for large indexes (enabled by default).  This
    /// optional support can be disabled via a few different means:
    /// <ul>
    /// <li>Setting the system property
    /// <value>#USE_BIG_INDEX_PROPERTY</value>
    /// to
    /// <code>"false"</code>
    /// will disable "large" index support across the jvm</li>
    /// <li>Calling
    /// <see cref="SetUseBigIndex(bool)">SetUseBigIndex(bool)</see>
    /// on a Database instance will override
    /// any system property setting for "large" index support for all tables
    /// subsequently created from that instance</li>
    /// <li>Calling
    /// <see cref="GetTable(string, bool)">GetTable(string, bool)</see>
    /// can selectively
    /// enable/disable "large" index support on a per-table basis (overriding
    /// any Database or system property setting)</li>
    /// </ul>
    /// </remarks>
    /// <author>Tim McCune</author>
    /// <usage>_general_class_</usage>
    public class Database : Iterable<Table>, IDisposable, Flushable
    {
        /// <summary>
        /// default value for the auto-sync value (
        /// <code>true</code>
        /// ).  this is slower,
        /// but leaves more chance of a useable database in the face of failures.
        /// </summary>
        /// <usage>_general_field_</usage>
        public const bool DEFAULT_AUTO_SYNC = true;

        /// <summary>
        /// the default value for the resource path used to load classpath
        /// resources.
        /// </summary>
        /// <remarks>
        /// the default value for the resource path used to load classpath
        /// resources.
        /// </remarks>
        /// <usage>_general_field_</usage>
        public static readonly string DEFAULT_RESOURCE_PATH = "jackcess-net.Resources."; //"com/healthmarketscience/jackcess/";

        /// <summary>the default sort order for table columns.</summary>
        /// <remarks>the default sort order for table columns.</remarks>
        /// <usage>_intermediate_field_</usage>
        public static readonly Table.ColumnOrder DEFAULT_COLUMN_ORDER = Table.ColumnOrder
            .DATA;

        /// <summary>
        /// (boolean) system property which can be used to disable the default big
        /// index support.
        /// </summary>
        /// <remarks>
        /// (boolean) system property which can be used to disable the default big
        /// index support.
        /// </remarks>
        /// <usage>_general_field_</usage>
        public static readonly string USE_BIG_INDEX_PROPERTY = "com.healthmarketscience.jackcess.bigIndex";

        /// <summary>
        /// system property which can be used to set the default TimeZone used for
        /// date calculations.
        /// </summary>
        /// <remarks>
        /// system property which can be used to set the default TimeZone used for
        /// date calculations.
        /// </remarks>
        /// <usage>_general_field_</usage>
        public static readonly string TIMEZONE_PROPERTY = "com.healthmarketscience.jackcess.timeZone";

        /// <summary>
        /// system property prefix which can be used to set the default Charset
        /// used for text data (full property includes the JetFormat version).
        /// </summary>
        /// <remarks>
        /// system property prefix which can be used to set the default Charset
        /// used for text data (full property includes the JetFormat version).
        /// </remarks>
        /// <usage>_general_field_</usage>
        public static readonly string CHARSET_PROPERTY_PREFIX = "com.healthmarketscience.jackcess.charset.";

        /// <summary>
        /// system property which can be used to set the path from which classpath
        /// resources are loaded (must end with a "/" if non-empty).
        /// </summary>
        /// <remarks>
        /// system property which can be used to set the path from which classpath
        /// resources are loaded (must end with a "/" if non-empty).  Default value
        /// is
        /// <see cref="DEFAULT_RESOURCE_PATH">DEFAULT_RESOURCE_PATH</see>
        /// if unspecified.
        /// </remarks>
        /// <usage>_general_field_</usage>
        public static readonly string RESOURCE_PATH_PROPERTY = "com.healthmarketscience.jackcess.resourcePath";

        /// <summary>
        /// (boolean) system property which can be used to indicate that the current
        /// vm has a poor nio implementation (specifically for
        /// FileChannel.transferFrom)
        /// </summary>
        /// <usage>_intermediate_field_</usage>
        public static readonly string BROKEN_NIO_PROPERTY = "com.healthmarketscience.jackcess.brokenNio";

        /// <summary>
        /// system property which can be used to set the default sort order for
        /// table columns.
        /// </summary>
        /// <remarks>
        /// system property which can be used to set the default sort order for
        /// table columns.  Value should be one
        /// <see cref="ColumnOrder">ColumnOrder</see>
        /// enum
        /// values.
        /// </remarks>
        /// <usage>_intermediate_field_</usage>
        public static readonly string COLUMN_ORDER_PROPERTY = "com.healthmarketscience.jackcess.columnOrder";

        private sealed class _ErrorHandler_157 : ErrorHandler
        {
            public _ErrorHandler_157()
            {
            }

            /// <exception cref="System.IO.IOException"></exception>
            public object HandleRowError(Column column, byte[] columnData, Table.RowState rowState
                , Exception error)
            {
                // really can only be RuntimeException or IOException
                if (error is IOException)
                {
                    throw (IOException)error;
                }
                throw (RuntimeException)error;
            }
        }

        /// <summary>default error handler used if none provided (just rethrows exception)</summary>
        /// <usage>_general_field_</usage>
        public static readonly ErrorHandler DEFAULT_ERROR_HANDLER = new _ErrorHandler_157
            ();

        /// <summary>the resource path to be used when loading classpath resources</summary>
        internal static readonly string RESOURCE_PATH = Runtime.GetProperty(RESOURCE_PATH_PROPERTY
            , DEFAULT_RESOURCE_PATH);

        /// <summary>whether or not this jvm has "broken" nio support</summary>
        internal static readonly bool BROKEN_NIO = Sharpen.Runtime.EqualsIgnoreCase(true.
            ToString(), Runtime.GetProperty(BROKEN_NIO_PROPERTY));

        /// <summary>System catalog always lives on page 2</summary>
        private const int PAGE_SYSTEM_CATALOG = 2;

        /// <summary>Name of the system catalog</summary>
        private static readonly string TABLE_SYSTEM_CATALOG = "MSysObjects";

        /// <summary>this is the access control bit field for created tables.</summary>
        /// <remarks>
        /// this is the access control bit field for created tables.  the value used
        /// is equivalent to full access (Visual Basic DAO PermissionEnum constant:
        /// dbSecFullAccess)
        /// </remarks>
        private static readonly int SYS_FULL_ACCESS_ACM = 1048575;

        /// <summary>ACE table column name of the actual access control entry</summary>
        private static readonly string ACE_COL_ACM = "ACM";

        /// <summary>ACE table column name of the inheritable attributes flag</summary>
        private static readonly string ACE_COL_F_INHERITABLE = "FInheritable";

        /// <summary>ACE table column name of the relevant objectId</summary>
        private static readonly string ACE_COL_OBJECT_ID = "ObjectId";

        /// <summary>ACE table column name of the relevant userId</summary>
        private static readonly string ACE_COL_SID = "SID";

        /// <summary>Relationship table column name of the column count</summary>
        private static readonly string REL_COL_COLUMN_COUNT = "ccolumn";

        /// <summary>Relationship table column name of the flags</summary>
        private static readonly string REL_COL_FLAGS = "grbit";

        /// <summary>Relationship table column name of the index of the columns</summary>
        private static readonly string REL_COL_COLUMN_INDEX = "icolumn";

        /// <summary>Relationship table column name of the "to" column name</summary>
        private static readonly string REL_COL_TO_COLUMN = "szColumn";

        /// <summary>Relationship table column name of the "to" table name</summary>
        private static readonly string REL_COL_TO_TABLE = "szObject";

        /// <summary>Relationship table column name of the "from" column name</summary>
        private static readonly string REL_COL_FROM_COLUMN = "szReferencedColumn";

        /// <summary>Relationship table column name of the "from" table name</summary>
        private static readonly string REL_COL_FROM_TABLE = "szReferencedObject";

        /// <summary>Relationship table column name of the relationship</summary>
        private static readonly string REL_COL_NAME = "szRelationship";

        /// <summary>
        /// System catalog column name of the page on which system object definitions
        /// are stored
        /// </summary>
        private static readonly string CAT_COL_ID = "Id";

        /// <summary>System catalog column name of the name of a system object</summary>
        private static readonly string CAT_COL_NAME = "Name";

        private static readonly string CAT_COL_OWNER = "Owner";

        /// <summary>System catalog column name of a system object's parent's id</summary>
        private static readonly string CAT_COL_PARENT_ID = "ParentId";

        /// <summary>System catalog column name of the type of a system object</summary>
        private static readonly string CAT_COL_TYPE = "Type";

        /// <summary>System catalog column name of the date a system object was created</summary>
        private static readonly string CAT_COL_DATE_CREATE = "DateCreate";

        /// <summary>System catalog column name of the date a system object was updated</summary>
        private static readonly string CAT_COL_DATE_UPDATE = "DateUpdate";

        /// <summary>System catalog column name of the flags column</summary>
        private static readonly string CAT_COL_FLAGS = "Flags";

        /// <summary>System catalog column name of the properties column</summary>
        private static readonly string CAT_COL_PROPS = "LvProp";

        /// <summary>top-level parentid for a database</summary>
        private const int DB_PARENT_ID = unchecked((int)(0xF000000));

        /// <summary>the maximum size of any of the included "empty db" resources</summary>
        private const long MAX_EMPTYDB_SIZE = 350000L;

        /// <summary>this object is a "system" object</summary>
        internal const int SYSTEM_OBJECT_FLAG = unchecked((int)(0x80000000));

        /// <summary>this object is another type of "system" object</summary>
        internal const int ALT_SYSTEM_OBJECT_FLAG = unchecked((int)(0x02));

        /// <summary>this object is hidden</summary>
        internal const int HIDDEN_OBJECT_FLAG = unchecked((int)(0x08));

        /// <summary>all flags which seem to indicate some type of system object</summary>
        internal const int SYSTEM_OBJECT_FLAGS = SYSTEM_OBJECT_FLAG | ALT_SYSTEM_OBJECT_FLAG;

        /// <summary>Enum which indicates which version of Access created the database.</summary>
        /// <remarks>Enum which indicates which version of Access created the database.</remarks>
        /// <usage>_general_class_</usage>
        public class FileFormat
        {
            public static readonly Database.FileFormat V1997 = new Database.FileFormat(null,
                JetFormat.VERSION_3);

            public static readonly Database.FileFormat V2000 = new Database.FileFormat(RESOURCE_PATH + "empty.mdb", JetFormat.VERSION_4);

            public static readonly Database.FileFormat V2003 = new Database.FileFormat(RESOURCE_PATH + "empty2003.mdb", JetFormat.VERSION_4);

            public static readonly Database.FileFormat V2007 = new Database.FileFormat(RESOURCE_PATH + "empty2007.accdb", JetFormat.VERSION_12, ".accdb");

            public static readonly Database.FileFormat V2010 = new Database.FileFormat(RESOURCE_PATH + "empty2010.accdb", JetFormat.VERSION_14, ".accdb");

            public static readonly Database.FileFormat MSISAM = new Database.FileFormat(null,
                JetFormat.VERSION_MSISAM, ".mny");

            public readonly string emptyDBFile;

            public readonly JetFormat jetFormat;

            public readonly string fileExtension;

            private FileFormat(string emptyDBFile, JetFormat jetFormat) : this(emptyDBFile, jetFormat
                , ".mdb")
            {
            }

            private FileFormat(string emptyDBFile, JetFormat jetFormat, string ext)
            {
                this.emptyDBFile = emptyDBFile;
                this.jetFormat = jetFormat;
                this.fileExtension = ext;
            }
        }

        /// <summary>Prefix for column or table names that are reserved words</summary>
        private static readonly string ESCAPE_PREFIX = "x";

        /// <summary>Name of the system object that is the parent of all tables</summary>
        private static readonly string SYSTEM_OBJECT_NAME_TABLES = "Tables";

        /// <summary>Name of the system object that is the parent of all databases</summary>
        private static readonly string SYSTEM_OBJECT_NAME_DATABASES = "Databases";

        /// <summary>Name of the system object that is the parent of all relationships</summary>
        private static readonly string SYSTEM_OBJECT_NAME_RELATIONSHIPS = "Relationships";

        /// <summary>Name of the table that contains system access control entries</summary>
        private static readonly string TABLE_SYSTEM_ACES = "MSysACEs";

        /// <summary>Name of the table that contains table relationships</summary>
        private static readonly string TABLE_SYSTEM_RELATIONSHIPS = "MSysRelationships";

        /// <summary>Name of the table that contains queries</summary>
        private static readonly string TABLE_SYSTEM_QUERIES = "MSysQueries";

        /// <summary>Name of the table that contains complex type information</summary>
        private static readonly string TABLE_SYSTEM_COMPLEX_COLS = "MSysComplexColumns";

        /// <summary>Name of the main database properties object</summary>
        private static readonly string OBJECT_NAME_DB_PROPS = "MSysDb";

        /// <summary>Name of the summary properties object</summary>
        private static readonly string OBJECT_NAME_SUMMARY_PROPS = "SummaryInfo";

        /// <summary>Name of the user-defined properties object</summary>
        private static readonly string OBJECT_NAME_USERDEF_PROPS = "UserDefined";

        /// <summary>System object type for table definitions</summary>
        private static readonly short TYPE_TABLE = (short)1;

        /// <summary>System object type for query definitions</summary>
        private static readonly short TYPE_QUERY = (short)5;

        /// <summary>max number of table lookups to cache</summary>
        private const int MAX_CACHED_LOOKUP_TABLES = 50;

        /// <summary>the columns to read when reading system catalog normally</summary>
        private static ICollection<string> SYSTEM_CATALOG_COLUMNS = new HashSet<string>(Arrays
            .AsList(CAT_COL_NAME, CAT_COL_TYPE, CAT_COL_ID, CAT_COL_FLAGS));

        /// <summary>the columns to read when finding table names</summary>
        private static ICollection<string> SYSTEM_CATALOG_TABLE_NAME_COLUMNS = new HashSet
            <string>(Arrays.AsList(CAT_COL_NAME, CAT_COL_TYPE, CAT_COL_ID, CAT_COL_FLAGS, CAT_COL_PARENT_ID
            ));

        /// <summary>the columns to read when getting object propertyes</summary>
        private static ICollection<string> SYSTEM_CATALOG_PROPS_COLUMNS = new HashSet<string
            >(Arrays.AsList(CAT_COL_ID, CAT_COL_PROPS));

        /// <summary>this is the default "userId" used if we cannot find existing info.</summary>
        /// <remarks>
        /// this is the default "userId" used if we cannot find existing info.  this
        /// seems to be some standard "Admin" userId for access files
        /// </remarks>
        private static readonly byte[] SYS_DEFAULT_SID = new byte[2];

        /// <summary>
        /// All of the reserved words in Access that should be escaped when creating
        /// table or column names
        /// </summary>
        private static readonly ICollection<string> RESERVED_WORDS = new HashSet<string>(
            );

        static Database()
        {
            SYS_DEFAULT_SID[0] = unchecked((byte)unchecked((int)(0xA6)));
            SYS_DEFAULT_SID[1] = unchecked((byte)unchecked((int)(0x33)));
            //Yup, there's a lot.
            Sharpen.Collections.AddAll(RESERVED_WORDS, Arrays.AsList("add", "all", "alphanumeric"
                , "alter", "and", "any", "application", "as", "asc", "assistant", "autoincrement"
                , "avg", "between", "binary", "bit", "boolean", "by", "byte", "char", "character"
                , "column", "compactdatabase", "constraint", "container", "count", "counter", "create"
                , "createdatabase", "createfield", "creategroup", "createindex", "createobject",
                "createproperty", "createrelation", "createtabledef", "createuser", "createworkspace"
                , "currency", "currentuser", "database", "date", "datetime", "delete", "desc", "description"
                , "disallow", "distinct", "distinctrow", "document", "double", "drop", "echo", "else"
                , "end", "eqv", "error", "exists", "exit", "false", "field", "fields", "fillcache"
                , "float", "float4", "float8", "foreign", "form", "forms", "from", "full", "function"
                , "general", "getobject", "getoption", "gotopage", "group", "group by", "guid",
                "having", "idle", "ieeedouble", "ieeesingle", "if", "ignore", "imp", "in", "index"
                , "indexes", "inner", "insert", "inserttext", "int", "integer", "integer1", "integer2"
                , "integer4", "into", "is", "join", "key", "lastmodified", "left", "level", "like"
                , "logical", "logical1", "long", "longbinary", "longtext", "macro", "match", "max"
                , "min", "mod", "memo", "module", "money", "move", "name", "newpassword", "no",
                "not", "null", "number", "numeric", "object", "oleobject", "off", "on", "openrecordset"
                , "option", "or", "order", "outer", "owneraccess", "parameter", "parameters", "partial"
                , "percent", "pivot", "primary", "procedure", "property", "queries", "query", "quit"
                , "real", "recalc", "recordset", "references", "refresh", "refreshlink", "registerdatabase"
                , "relation", "repaint", "repairdatabase", "report", "reports", "requery", "right"
                , "screen", "section", "select", "set", "setfocus", "setoption", "short", "single"
                , "smallint", "some", "sql", "stdev", "stdevp", "string", "sum", "table", "tabledef"
                , "tabledefs", "tableid", "text", "time", "timestamp", "top", "transform", "true"
                , "type", "union", "unique", "update", "user", "value", "values", "var", "varp",
                "varbinary", "varchar", "where", "with", "workspace", "xor", "year", "yes", "yesno"
                ));
        }

        /// <summary>Buffer to hold database pages</summary>
        private ByteBuffer _buffer;

        /// <summary>ID of the Tables system object</summary>
        private int? _tableParentId;

        /// <summary>Format that the containing database is in</summary>
        private readonly JetFormat _format;

        private class _LinkedHashMap_383 : LinkedHashMap<string, Database.TableInfo
            >
        {
            public _LinkedHashMap_383(Database _enclosing)
            {
                this._enclosing = _enclosing;
            }

            protected bool RemoveEldestEntry(KeyValuePair<string, Database.TableInfo
                > e)
            {
                return (this.Count > Database.MAX_CACHED_LOOKUP_TABLES);
            }

            private readonly Database _enclosing;
        }

        /// <summary>
        /// Cache map of UPPERCASE table names to page numbers containing their
        /// definition and their stored table name (max size
        /// MAX_CACHED_LOOKUP_TABLES).
        /// </summary>
        /// <remarks>
        /// Cache map of UPPERCASE table names to page numbers containing their
        /// definition and their stored table name (max size
        /// MAX_CACHED_LOOKUP_TABLES).
        /// </remarks>
        private readonly IDictionary<string, Database.TableInfo> _tableLookup;

        /// <summary>set of table names as stored in the mdb file, created on demand</summary>
        private ICollection<string> _tableNames;

        /// <summary>Reads and writes database pages</summary>
        private readonly PageChannel _pageChannel;

        /// <summary>System catalog table</summary>
        private Table _systemCatalog;

        /// <summary>utility table finder</summary>
        private Database.TableFinder _tableFinder;

        /// <summary>System access control entries table (initialized on first use)</summary>
        private Table _accessControlEntries;

        /// <summary>System relationships table (initialized on first use)</summary>
        private Table _relationships;

        /// <summary>System queries table (initialized on first use)</summary>
        private Table _queries;

        /// <summary>System complex columns table (initialized on first use)</summary>
        private Table _complexCols;

        /// <summary>SIDs to use for the ACEs added for new tables</summary>
        private readonly IList<byte[]> _newTableSIDs = new AList<byte[]>();

        /// <summary>"big index support" is optional, but enabled by default</summary>
        private bool? _useBigIndex = true;

        /// <summary>optional error handler to use when row errors are encountered</summary>
        private ErrorHandler _dbErrorHandler;

        /// <summary>the file format of the database</summary>
        private Database.FileFormat _fileFormat;

        /// <summary>charset to use when handling text</summary>
        private Encoding _charset;

        /// <summary>timezone to use when handling dates</summary>
        private TimeZoneInfo _timeZone;

        /// <summary>language sort order to be used for textual columns</summary>
        private Column.SortOrder _defaultSortOrder;

        /// <summary>default code page to be used for textual columns (in some dbs)</summary>
        private short? _defaultCodePage;

        /// <summary>the ordering used for table columns</summary>
        private Table.ColumnOrder _columnOrder;

        /// <summary>cache of in-use tables</summary>
        private readonly Database.TableCache _tableCache = new Database.TableCache();

        /// <summary>handler for reading/writing properteies</summary>
        private PropertyMaps.Handler _propsHandler;

        /// <summary>ID of the Databases system object</summary>
        private int? _dbParentId;

        /// <summary>core database properties</summary>
        private PropertyMaps _dbPropMaps;

        /// <summary>summary properties</summary>
        private PropertyMaps _summaryPropMaps;

        /// <summary>user-defined properties</summary>
        private PropertyMaps _userDefPropMaps;

        /// <summary>Open an existing Database.</summary>
        /// <remarks>
        /// Open an existing Database.  If the existing file is not writeable, the
        /// file will be opened read-only.  Auto-syncing is enabled for the returned
        /// Database.
        /// <p>
        /// Equivalent to:
        /// <code>open(mdbFile, false);</code>
        /// </remarks>
        /// <param name="mdbFile">File containing the database</param>
        /// <seealso cref="Open(Sharpen.FilePath, bool)">Open(Sharpen.FilePath, bool)</seealso>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static Database Open(FilePath mdbFile)
        {
            return Open(mdbFile, false);
        }

        /// <summary>Open an existing Database.</summary>
        /// <remarks>
        /// Open an existing Database.  If the existing file is not writeable or the
        /// readOnly flag is <code>true</code>, the file will be opened read-only.
        /// Auto-syncing is enabled for the returned Database.
        /// <p>
        /// Equivalent to:
        /// <code>open(mdbFile, readOnly, DEFAULT_AUTO_SYNC);</code>
        /// </remarks>
        /// <param name="mdbFile">File containing the database</param>
        /// <param name="readOnly">
        /// iff <code>true</code>, force opening file in read-only
        /// mode
        /// </param>
        /// <seealso cref="Open(Sharpen.FilePath, bool, bool)">Open(Sharpen.FilePath, bool, bool)
        /// 	</seealso>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static Database Open(FilePath mdbFile, bool readOnly)
        {
            return Open(mdbFile, readOnly, DEFAULT_AUTO_SYNC);
        }

        /// <summary>Open an existing Database.</summary>
        /// <remarks>
        /// Open an existing Database.  If the existing file is not writeable or the
        /// readOnly flag is <code>true</code>, the file will be opened read-only.
        /// </remarks>
        /// <param name="mdbFile">File containing the database</param>
        /// <param name="readOnly">
        /// iff <code>true</code>, force opening file in read-only
        /// mode
        /// </param>
        /// <param name="autoSync">
        /// whether or not to enable auto-syncing on write.  if
        /// <code>true</code>
        /// , writes will be immediately flushed to disk.
        /// This leaves the database in a (fairly) consistent state
        /// on each write, but can be very inefficient for many
        /// updates.  if
        /// <code>false</code>
        /// , flushing to disk happens at
        /// the jvm's leisure, which can be much faster, but may
        /// leave the database in an inconsistent state if failures
        /// are encountered during writing.
        /// </param>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static Database Open(FilePath mdbFile, bool readOnly, bool autoSync)
        {
            return Open(mdbFile, readOnly, autoSync, null, null);
        }

        /// <summary>Open an existing Database.</summary>
        /// <remarks>
        /// Open an existing Database.  If the existing file is not writeable or the
        /// readOnly flag is <code>true</code>, the file will be opened read-only.
        /// </remarks>
        /// <param name="mdbFile">File containing the database</param>
        /// <param name="readOnly">
        /// iff <code>true</code>, force opening file in read-only
        /// mode
        /// </param>
        /// <param name="autoSync">
        /// whether or not to enable auto-syncing on write.  if
        /// <code>true</code>
        /// , writes will be immediately flushed to disk.
        /// This leaves the database in a (fairly) consistent state
        /// on each write, but can be very inefficient for many
        /// updates.  if
        /// <code>false</code>
        /// , flushing to disk happens at
        /// the jvm's leisure, which can be much faster, but may
        /// leave the database in an inconsistent state if failures
        /// are encountered during writing.
        /// </param>
        /// <param name="charset">
        /// Charset to use, if
        /// <code>null</code>
        /// , uses default
        /// </param>
        /// <param name="timeZone">
        /// TimeZone to use, if
        /// <code>null</code>
        /// , uses default
        /// </param>
        /// <usage>_intermediate_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static Database Open(FilePath mdbFile, bool readOnly, bool autoSync, Encoding
             charset, TimeZoneInfo timeZone)
        {
            return Open(mdbFile, readOnly, autoSync, charset, timeZone, null);
        }

        /// <summary>Open an existing Database.</summary>
        /// <remarks>
        /// Open an existing Database.  If the existing file is not writeable or the
        /// readOnly flag is <code>true</code>, the file will be opened read-only.
        /// </remarks>
        /// <param name="mdbFile">File containing the database</param>
        /// <param name="readOnly">
        /// iff <code>true</code>, force opening file in read-only
        /// mode
        /// </param>
        /// <param name="autoSync">
        /// whether or not to enable auto-syncing on write.  if
        /// <code>true</code>
        /// , writes will be immediately flushed to disk.
        /// This leaves the database in a (fairly) consistent state
        /// on each write, but can be very inefficient for many
        /// updates.  if
        /// <code>false</code>
        /// , flushing to disk happens at
        /// the jvm's leisure, which can be much faster, but may
        /// leave the database in an inconsistent state if failures
        /// are encountered during writing.
        /// </param>
        /// <param name="charset">
        /// Charset to use, if
        /// <code>null</code>
        /// , uses default
        /// </param>
        /// <param name="timeZone">
        /// TimeZone to use, if
        /// <code>null</code>
        /// , uses default
        /// </param>
        /// <param name="provider">
        /// CodecProvider for handling page encoding/decoding, may be
        /// <code>null</code>
        /// if no special encoding is necessary
        /// </param>
        /// <usage>_intermediate_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static Database Open(FilePath mdbFile, bool readOnly, bool autoSync, Encoding
             charset, TimeZoneInfo timeZone, CodecProvider provider)
        {
            if (!mdbFile.Exists())
            {
                throw new FileNotFoundException("given file does not exist: " + mdbFile);
            }
            // force read-only for non-writable files
            readOnly |= !mdbFile.CanWrite();
            // open file channel
            FileChannel channel = OpenChannel(mdbFile, readOnly);
            if (!readOnly)
            {
                // verify that format supports writing
                JetFormat jetFormat = JetFormat.GetFormat(channel);
                if (jetFormat.READ_ONLY)
                {
                    // shutdown the channel (quietly)
                    try
                    {
                        channel.Close();
                    }
                    catch (Exception)
                    {
                    }
                    // we don't care
                    throw new IOException("jet format '" + jetFormat + "' does not support writing");
                }
            }
            return new Database(channel, autoSync, null, charset, timeZone, provider);
        }

        /// <summary>
        /// Create a new Access 2000 Database
        /// <p>
        /// Equivalent to:
        /// <code>create(FileFormat.V2000, mdbFile, DEFAULT_AUTO_SYNC);</code>
        /// </summary>
        /// <param name="mdbFile">
        /// Location to write the new database to.  <b>If this file
        /// already exists, it will be overwritten.</b>
        /// </param>
        /// <seealso cref="Create(Sharpen.FilePath, bool)">Create(Sharpen.FilePath, bool)</seealso>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static Database Create(FilePath mdbFile)
        {
            return Create(mdbFile, DEFAULT_AUTO_SYNC);
        }

        /// <summary>
        /// Create a new Database for the given fileFormat
        /// <p>
        /// Equivalent to:
        /// <code>create(fileFormat, mdbFile, DEFAULT_AUTO_SYNC);</code>
        /// </summary>
        /// <param name="fileFormat">version of new database.</param>
        /// <param name="mdbFile">
        /// Location to write the new database to.  <b>If this file
        /// already exists, it will be overwritten.</b>
        /// </param>
        /// <seealso cref="Create(Sharpen.FilePath, bool)">Create(Sharpen.FilePath, bool)</seealso>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static Database Create(Database.FileFormat fileFormat, FilePath mdbFile)
        {
            return Create(fileFormat, mdbFile, DEFAULT_AUTO_SYNC);
        }

        /// <summary>
        /// Create a new Access 2000 Database
        /// <p>
        /// Equivalent to:
        /// <code>create(FileFormat.V2000, mdbFile, DEFAULT_AUTO_SYNC);</code>
        /// </summary>
        /// <param name="mdbFile">
        /// Location to write the new database to.  <b>If this file
        /// already exists, it will be overwritten.</b>
        /// </param>
        /// <param name="autoSync">
        /// whether or not to enable auto-syncing on write.  if
        /// <code>true</code>
        /// , writes will be immediately flushed to disk.
        /// This leaves the database in a (fairly) consistent state
        /// on each write, but can be very inefficient for many
        /// updates.  if
        /// <code>false</code>
        /// , flushing to disk happens at
        /// the jvm's leisure, which can be much faster, but may
        /// leave the database in an inconsistent state if failures
        /// are encountered during writing.
        /// </param>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static Database Create(FilePath mdbFile, bool autoSync)
        {
            return Create(Database.FileFormat.V2000, mdbFile, autoSync);
        }

        /// <summary>Create a new Database for the given fileFormat</summary>
        /// <param name="fileFormat">version of new database.</param>
        /// <param name="mdbFile">
        /// Location to write the new database to.  <b>If this file
        /// already exists, it will be overwritten.</b>
        /// </param>
        /// <param name="autoSync">
        /// whether or not to enable auto-syncing on write.  if
        /// <code>true</code>
        /// , writes will be immediately flushed to disk.
        /// This leaves the database in a (fairly) consistent state
        /// on each write, but can be very inefficient for many
        /// updates.  if
        /// <code>false</code>
        /// , flushing to disk happens at
        /// the jvm's leisure, which can be much faster, but may
        /// leave the database in an inconsistent state if failures
        /// are encountered during writing.
        /// </param>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static Database Create(Database.FileFormat fileFormat, FilePath mdbFile, bool
             autoSync)
        {
            return Create(fileFormat, mdbFile, autoSync, null, null);
        }

        /// <summary>Create a new Database for the given fileFormat</summary>
        /// <param name="fileFormat">version of new database.</param>
        /// <param name="mdbFile">
        /// Location to write the new database to.  <b>If this file
        /// already exists, it will be overwritten.</b>
        /// </param>
        /// <param name="autoSync">
        /// whether or not to enable auto-syncing on write.  if
        /// <code>true</code>
        /// , writes will be immediately flushed to disk.
        /// This leaves the database in a (fairly) consistent state
        /// on each write, but can be very inefficient for many
        /// updates.  if
        /// <code>false</code>
        /// , flushing to disk happens at
        /// the jvm's leisure, which can be much faster, but may
        /// leave the database in an inconsistent state if failures
        /// are encountered during writing.
        /// </param>
        /// <param name="charset">
        /// Charset to use, if
        /// <code>null</code>
        /// , uses default
        /// </param>
        /// <param name="timeZone">
        /// TimeZone to use, if
        /// <code>null</code>
        /// , uses default
        /// </param>
        /// <usage>_intermediate_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public static Database Create(Database.FileFormat fileFormat, FilePath mdbFile, bool
             autoSync, Encoding charset, TimeZoneInfo timeZone)
        {
            if (fileFormat.jetFormat.READ_ONLY)
            {
                throw new IOException("jet format '" + fileFormat.jetFormat + "' does not support writing"
                    );
            }
            FileChannel channel = OpenChannel(mdbFile, false);
            channel.Truncate(0);
            TransferFrom(channel, GetResourceAsStream(fileFormat.emptyDBFile));
            return new Database(channel, autoSync, fileFormat, charset, timeZone, null);
        }

        /// <summary>Package visible only to support unit tests via DatabaseTest.openChannel().
        /// 	</summary>
        /// <remarks>Package visible only to support unit tests via DatabaseTest.openChannel().
        /// 	</remarks>
        /// <param name="mdbFile">file to open</param>
        /// <param name="readOnly">true if read-only</param>
        /// <returns>a FileChannel on the given file.</returns>
        /// <exception>
        /// FileNotFoundException
        /// if the mode is <tt>"r"</tt> but the given file object does
        /// not denote an existing regular file, or if the mode begins
        /// with <tt>"rw"</tt> but the given file object does not denote
        /// an existing, writable regular file and a new regular file of
        /// that name cannot be created, or if some other error occurs
        /// while opening or creating the file
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        internal static FileChannel OpenChannel(FilePath mdbFile, bool readOnly)
        {
            string mode = (readOnly ? "r" : "rw");
            return new RandomAccessFile(mdbFile, mode).GetChannel();
        }

        /// <summary>Create a new database by reading it in from a FileChannel.</summary>
        /// <remarks>Create a new database by reading it in from a FileChannel.</remarks>
        /// <param name="channel">
        /// File channel of the database.  This needs to be a
        /// FileChannel instead of a ReadableByteChannel because we need to
        /// randomly jump around to various points in the file.
        /// </param>
        /// <param name="autoSync">
        /// whether or not to enable auto-syncing on write.  if
        /// <code>true</code>
        /// , writes will be immediately flushed to disk.
        /// This leaves the database in a (fairly) consistent state
        /// on each write, but can be very inefficient for many
        /// updates.  if
        /// <code>false</code>
        /// , flushing to disk happens at
        /// the jvm's leisure, which can be much faster, but may
        /// leave the database in an inconsistent state if failures
        /// are encountered during writing.
        /// </param>
        /// <param name="fileFormat">version of new database (if known)</param>
        /// <param name="charset">
        /// Charset to use, if
        /// <code>null</code>
        /// , uses default
        /// </param>
        /// <param name="timeZone">
        /// TimeZone to use, if
        /// <code>null</code>
        /// , uses default
        /// </param>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal Database(FileChannel channel, bool autoSync, Database.FileFormat
             fileFormat, Encoding charset, TimeZoneInfo timeZone, CodecProvider provider)
        {
            _tableLookup = new _LinkedHashMap_383(this);
            bool success = false;
            try
            {
                _format = JetFormat.GetFormat(channel);
                _charset = ((charset == null) ? GetDefaultCharset(_format) : charset);
                _columnOrder = GetDefaultColumnOrder();
                _fileFormat = fileFormat;
                _pageChannel = new PageChannel(channel, _format, autoSync);
                _timeZone = ((timeZone == null) ? GetDefaultTimeZone() : timeZone);
                if (provider == null)
                {
                    provider = DefaultCodecProvider.INSTANCE;
                }
                // note, it's slighly sketchy to pass ourselves along partially
                // constructed, but only our _format and _pageChannel refs should be
                // needed
                _pageChannel.Initialize(this, provider);
                _buffer = _pageChannel.CreatePageBuffer();
                ReadSystemCatalog();
                success = true;
            }
            finally
            {
                if (!success && (channel != null))
                {
                    // something blew up, shutdown the channel (quietly)
                    try
                    {
                        channel.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        // we don't care
        /// <usage>_advanced_method_</usage>
        public virtual PageChannel GetPageChannel()
        {
            return _pageChannel;
        }

        /// <usage>_advanced_method_</usage>
        public virtual JetFormat GetFormat()
        {
            return _format;
        }

        /// <returns>The system catalog table</returns>
        /// <usage>_advanced_method_</usage>
        public virtual Table GetSystemCatalog()
        {
            return _systemCatalog;
        }

        /// <returns>The system Access Control Entries table (loaded on demand)</returns>
        /// <usage>_advanced_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual Table GetAccessControlEntries()
        {
            if (_accessControlEntries == null)
            {
                _accessControlEntries = GetSystemTable(TABLE_SYSTEM_ACES);
                if (_accessControlEntries == null)
                {
                    throw new IOException("Could not find system table " + TABLE_SYSTEM_ACES);
                }
            }
            return _accessControlEntries;
        }

        /// <returns>the complex column system table (loaded on demand)</returns>
        /// <usage>_advanced_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual Table GetSystemComplexColumns()
        {
            if (_complexCols == null)
            {
                _complexCols = GetSystemTable(TABLE_SYSTEM_COMPLEX_COLS);
                if (_complexCols == null)
                {
                    throw new IOException("Could not find system table " + TABLE_SYSTEM_COMPLEX_COLS);
                }
            }
            return _complexCols;
        }

        /// <summary>Whether or not big index support is enabled for tables.</summary>
        /// <remarks>Whether or not big index support is enabled for tables.</remarks>
        /// <usage>_advanced_method_</usage>
        public virtual bool DoUseBigIndex()
        {
            return (_useBigIndex != null ? _useBigIndex.Value : true);
        }

        /// <summary>Set whether or not big index support is enabled for tables.</summary>
        /// <remarks>Set whether or not big index support is enabled for tables.</remarks>
        /// <usage>_intermediate_method_</usage>
        public virtual void SetUseBigIndex(bool useBigIndex)
        {
            _useBigIndex = useBigIndex;
        }

        /// <summary>
        /// Gets the currently configured ErrorHandler (always non-
        /// <code>null</code>
        /// ).
        /// This will be used to handle all errors unless overridden at the Table or
        /// Cursor level.
        /// </summary>
        /// <usage>_intermediate_method_</usage>
        public virtual ErrorHandler GetErrorHandler()
        {
            return ((_dbErrorHandler != null) ? _dbErrorHandler : DEFAULT_ERROR_HANDLER);
        }

        /// <summary>Sets a new ErrorHandler.</summary>
        /// <remarks>
        /// Sets a new ErrorHandler.  If
        /// <code>null</code>
        /// , resets to the
        /// <see cref="DEFAULT_ERROR_HANDLER">DEFAULT_ERROR_HANDLER</see>
        /// .
        /// </remarks>
        /// <usage>_intermediate_method_</usage>
        public virtual void SetErrorHandler(ErrorHandler newErrorHandler)
        {
            _dbErrorHandler = newErrorHandler;
        }

        /// <summary>
        /// Gets currently configured TimeZone (always non-
        /// <code>null</code>
        /// ).
        /// </summary>
        /// <usage>_intermediate_method_</usage>
        public virtual TimeZoneInfo GetTimeZone()
        {
            return _timeZone;
        }

        /// <summary>Sets a new TimeZone.</summary>
        /// <remarks>
        /// Sets a new TimeZone.  If
        /// <code>null</code>
        /// , resets to the value returned by
        /// <see cref="GetDefaultTimeZone()">GetDefaultTimeZone()</see>
        /// .
        /// </remarks>
        /// <usage>_intermediate_method_</usage>
        public virtual void SetTimeZone(TimeZoneInfo newTimeZone)
        {
            if (newTimeZone == null)
            {
                newTimeZone = GetDefaultTimeZone();
            }
            _timeZone = newTimeZone;
        }

        /// <summary>
        /// Gets currently configured Charset (always non-
        /// <code>null</code>
        /// ).
        /// </summary>
        /// <usage>_intermediate_method_</usage>
        public virtual Encoding GetCharset()
        {
            return _charset;
        }

        /// <summary>Sets a new Charset.</summary>
        /// <remarks>
        /// Sets a new Charset.  If
        /// <code>null</code>
        /// , resets to the value returned by
        /// <see cref="GetDefaultCharset(JetFormat)">GetDefaultCharset(JetFormat)</see>
        /// .
        /// </remarks>
        /// <usage>_intermediate_method_</usage>
        public virtual void SetCharset(Encoding newCharset)
        {
            if (newCharset == null)
            {
                newCharset = GetDefaultCharset(GetFormat());
            }
            _charset = newCharset;
        }

        /// <summary>
        /// Gets currently configured
        /// <see cref="ColumnOrder">ColumnOrder</see>
        /// (always non-
        /// <code>null</code>
        /// ).
        /// </summary>
        /// <usage>_intermediate_method_</usage>
        public virtual Table.ColumnOrder GetColumnOrder()
        {
            return _columnOrder;
        }

        /// <summary>Sets a new Table.ColumnOrder.</summary>
        /// <remarks>
        /// Sets a new Table.ColumnOrder.  If
        /// <code>null</code>
        /// , resets to the value
        /// returned by
        /// <see cref="GetDefaultColumnOrder()">GetDefaultColumnOrder()</see>
        /// .
        /// </remarks>
        /// <usage>_intermediate_method_</usage>
        public virtual void SetColumnOrder(Table.ColumnOrder? newColumnOrder)
        {
            if (newColumnOrder == null)
            {
                newColumnOrder = GetDefaultColumnOrder();
            }
            _columnOrder = newColumnOrder.Value;
        }

        /// <returns>
        /// the current handler for reading/writing properties, creating if
        /// necessary
        /// </returns>
        private PropertyMaps.Handler GetPropsHandler()
        {
            if (_propsHandler == null)
            {
                _propsHandler = new PropertyMaps.Handler(this);
            }
            return _propsHandler;
        }

        /// <summary>
        /// Returns the FileFormat of this database (which may involve inspecting the
        /// database itself).
        /// </summary>
        /// <remarks>
        /// Returns the FileFormat of this database (which may involve inspecting the
        /// database itself).
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">if the file format cannot be determined
        /// 	</exception>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual Database.FileFormat GetFileFormat()
        {
            if (_fileFormat == null)
            {
                IDictionary<string, Database.FileFormat> possibleFileFormats = GetFormat().GetPossibleFileFormats
                    ();
                if (possibleFileFormats.Count == 1)
                {
                    // single possible format (null key), easy enough
                    _fileFormat = possibleFileFormats.Get(null);
                }
                else
                {
                    // need to check the "AccessVersion" property
                    string accessVersion = (string)GetDatabaseProperties().GetValue(PropertyMap.ACCESS_VERSION_PROP
                        );
                    _fileFormat = possibleFileFormats.Get(accessVersion);
                    if (_fileFormat == null)
                    {
                        throw new InvalidOperationException("Could not determine FileFormat");
                    }
                }
            }
            return _fileFormat;
        }

        /// <returns>
        /// a (possibly cached) page ByteBuffer for internal use.  the
        /// returned buffer should be released using
        /// <see cref="ReleaseSharedBuffer(Sharpen.ByteBuffer)">ReleaseSharedBuffer(Sharpen.ByteBuffer)
        /// 	</see>
        /// when no longer in use
        /// </returns>
        private ByteBuffer TakeSharedBuffer()
        {
            // we try to re-use a single shared _buffer, but occassionally, it may be
            // needed by multiple operations at the same time (e.g. loading a
            // secondary table while loading a primary table).  this method ensures
            // that we don't corrupt the _buffer, but instead force the second caller
            // to use a new buffer.
            if (_buffer != null)
            {
                ByteBuffer curBuffer = _buffer;
                _buffer = null;
                return curBuffer;
            }
            return _pageChannel.CreatePageBuffer();
        }

        /// <summary>
        /// Relinquishes use of a page ByteBuffer returned by
        /// <see cref="TakeSharedBuffer()">TakeSharedBuffer()</see>
        /// .
        /// </summary>
        private void ReleaseSharedBuffer(ByteBuffer buffer)
        {
            // we always stuff the returned buffer back into _buffer.  it doesn't
            // really matter if multiple values over-write, at the end of the day, we
            // just need one shared buffer
            _buffer = buffer;
        }

        /// <returns>
        /// the currently configured database default language sort order for
        /// textual columns
        /// </returns>
        /// <usage>_intermediate_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual Column.SortOrder GetDefaultSortOrder()
        {
            if (_defaultSortOrder == null)
            {
                InitRootPageInfo();
            }
            return _defaultSortOrder;
        }

        /// <returns>
        /// the currently configured database default code page for textual
        /// data (may not be relevant to all database versions)
        /// </returns>
        /// <usage>_intermediate_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual short GetDefaultCodePage()
        {
            if (_defaultCodePage == null)
            {
                InitRootPageInfo();
            }
            return _defaultCodePage.Value;
        }

        /// <summary>Reads various config info from the db page 0.</summary>
        /// <remarks>Reads various config info from the db page 0.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private void InitRootPageInfo()
        {
            ByteBuffer buffer = TakeSharedBuffer();
            try
            {
                _pageChannel.ReadPage(buffer, 0);
                _defaultSortOrder = Column.ReadSortOrder(buffer, _format.OFFSET_SORT_ORDER, _format
                    );
                _defaultCodePage = buffer.GetShort(_format.OFFSET_CODE_PAGE);
            }
            finally
            {
                ReleaseSharedBuffer(buffer);
            }
        }

        /// <returns>
        /// a PropertyMaps instance decoded from the given bytes (always
        /// returns non-
        /// <code>null</code>
        /// result).
        /// </returns>
        /// <usage>_intermediate_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual PropertyMaps ReadProperties(byte[] propsBytes, int objectId)
        {
            return GetPropsHandler().Read(propsBytes, objectId);
        }

        /// <summary>Read the system catalog</summary>
        /// <exception cref="System.IO.IOException"></exception>
        private void ReadSystemCatalog()
        {
            _systemCatalog = ReadTable(TABLE_SYSTEM_CATALOG, PAGE_SYSTEM_CATALOG, SYSTEM_OBJECT_FLAGS
                , DefaultUseBigIndex());
            try
            {
                _tableFinder = new Database.DefaultTableFinder(this, new CursorBuilder(_systemCatalog
                    ).SetIndexByColumnNames(CAT_COL_PARENT_ID, CAT_COL_NAME).SetColumnMatcher(CaseInsensitiveColumnMatcher
                    .INSTANCE).ToIndexCursor());
            }
            catch (ArgumentException)
            {
                System.Console.Error.WriteLine("Could not find expected index on table " + _systemCatalog
                    .GetName());
                // use table scan instead
                _tableFinder = new Database.FallbackTableFinder(this, new CursorBuilder(_systemCatalog
                    ).SetColumnMatcher(CaseInsensitiveColumnMatcher.INSTANCE).ToCursor());
            }
            _tableParentId = _tableFinder.FindObjectId(DB_PARENT_ID, SYSTEM_OBJECT_NAME_TABLES
                );
            if (_tableParentId == null)
            {
                throw new IOException("Did not find required parent table id");
            }
        }

        /// <returns>The names of all of the user tables (String)</returns>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual ICollection<string> GetTableNames()
        {
            if (_tableNames == null)
            {
                ICollection<string> tableNames = new TreeSet<string>(StringComparer.OrdinalIgnoreCase);
                _tableFinder.GetTableNames(tableNames, false);
                _tableNames = tableNames;
            }
            return _tableNames;
        }

        /// <returns>
        /// The names of all of the system tables (String).  Note, in order
        /// to read these tables, you must use
        /// <see cref="GetSystemTable(string)">GetSystemTable(string)</see>
        /// .
        /// <i>Extreme care should be taken if modifying these tables
        /// directly!</i>.
        /// </returns>
        /// <usage>_intermediate_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual ICollection<string> GetSystemTableNames()
        {
            ICollection<string> sysTableNames = new TreeSet<string>(StringComparer.OrdinalIgnoreCase);
            _tableFinder.GetTableNames(sysTableNames, true);
            return sysTableNames;
        }

        /// <returns>an unmodifiable Iterator of the user Tables in this Database.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// if an IOException is thrown by one of the
        /// operations, the actual exception will be contained within
        /// </exception>
        /// <exception cref="Sharpen.ConcurrentModificationException">
        /// if a table is added to the
        /// database while an Iterator is in use.
        /// </exception>
        /// <usage>_general_method_</usage>
        public override Iterator<Table> Iterator()
        {
            return new Database.TableIterator(this);
        }

        /// <param name="name">Table name</param>
        /// <returns>The table, or null if it doesn't exist</returns>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual Table GetTable(string name)
        {
            return GetTable(name, DefaultUseBigIndex());
        }

        /// <param name="name">Table name</param>
        /// <param name="useBigIndex">
        /// whether or not "big index support" should be enabled
        /// for the table (this value will override any other
        /// settings)
        /// </param>
        /// <returns>The table, or null if it doesn't exist</returns>
        /// <usage>_intermediate_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual Table GetTable(string name, bool useBigIndex)
        {
            return GetTable(name, false, useBigIndex);
        }

        /// <param name="tableDefPageNumber">the page number of a table definition</param>
        /// <returns>The table, or null if it doesn't exist</returns>
        /// <usage>_advanced_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual Table GetTable(int tableDefPageNumber)
        {
            // first, check for existing table
            Table table = _tableCache.Get(tableDefPageNumber);
            if (table != null)
            {
                return table;
            }
            // lookup table info from system catalog
            IDictionary<string, object> objectRow = _tableFinder.GetObjectRow(tableDefPageNumber
                , SYSTEM_CATALOG_COLUMNS);
            if (objectRow == null)
            {
                return null;
            }
            string name = (string)objectRow.Get(CAT_COL_NAME);
            int flags = (int)objectRow.Get(CAT_COL_FLAGS);
            return ReadTable(name, tableDefPageNumber, flags, DefaultUseBigIndex());
        }

        /// <param name="name">Table name</param>
        /// <param name="includeSystemTables">whether to consider returning a system table</param>
        /// <param name="useBigIndex">
        /// whether or not "big index support" should be enabled
        /// for the table (this value will override any other
        /// settings)
        /// </param>
        /// <returns>The table, or null if it doesn't exist</returns>
        /// <exception cref="System.IO.IOException"></exception>
        private Table GetTable(string name, bool includeSystemTables, bool useBigIndex)
        {
            Database.TableInfo tableInfo = LookupTable(name);
            if (tableInfo == null)
            {
                return null;
            }
            if (!includeSystemTables && IsSystemObject(tableInfo.flags))
            {
                return null;
            }
            return ReadTable(tableInfo.tableName, tableInfo.pageNumber, tableInfo.flags, useBigIndex
                );
        }

        /// <summary>Create a new table in this database</summary>
        /// <param name="name">Name of the table to create</param>
        /// <param name="columns">List of Columns in the table</param>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void CreateTable(string name, IList<Column> columns)
        {
            CreateTable(name, columns, null);
        }

        /// <summary>Create a new table in this database</summary>
        /// <param name="name">Name of the table to create</param>
        /// <param name="columns">List of Columns in the table</param>
        /// <param name="indexes">List of IndexBuilders describing indexes for the table</param>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void CreateTable(string name, IList<Column> columns, IList<IndexBuilder
            > indexes)
        {
            ValidateIdentifierName(name, _format.MAX_TABLE_NAME_LENGTH, "table");
            if (GetTable(name) != null)
            {
                throw new ArgumentException("Cannot create table with name of existing table");
            }
            if (columns.IsEmpty())
            {
                throw new ArgumentException("Cannot create table with no columns");
            }
            if (columns.Count > _format.MAX_COLUMNS_PER_TABLE)
            {
                throw new ArgumentException("Cannot create table with more than " + _format.MAX_COLUMNS_PER_TABLE
                     + " columns");
            }
            Column.SortOrder dbSortOrder = null;
            try
            {
                dbSortOrder = GetDefaultSortOrder();
            }
            catch (IOException)
            {
            }
            // ignored, just use the jet format default
            ICollection<string> colNames = new HashSet<string>();
            // next, validate the column definitions
            foreach (Column column in columns)
            {
                column.Validate(_format);
                if (!colNames.AddItem(column.GetName().ToUpper()))
                {
                    throw new ArgumentException("duplicate column name: " + column.GetName());
                }
                // set the sort order to the db default (if unspecified)
                if (DataTypeUtil.IsTextual(column.GetDataType()) && (column.GetTextSortOrder() == null
                    ))
                {
                    column.SetTextSortOrder(dbSortOrder);
                }
            }
            IList<Column> autoCols = Table.GetAutoNumberColumns(columns);
            if (autoCols.Count > 1)
            {
                // we can have one of each type
                ICollection<DataType> autoTypes = EnumSet.NoneOf<DataType>();
                foreach (Column c in autoCols)
                {
                    if (!autoTypes.AddItem(c.GetDataType()))
                    {
                        throw new ArgumentException("Can have at most one AutoNumber column of type " + c
                            .GetDataType() + " per table");
                    }
                }
            }
            if (indexes == null)
            {
                indexes = Sharpen.Collections.EmptyList<IndexBuilder>();
            }
            if (!indexes.IsEmpty())
            {
                // now, validate the indexes
                ICollection<string> idxNames = new HashSet<string>();
                bool foundPk = false;
                foreach (IndexBuilder index in indexes)
                {
                    index.Validate(colNames);
                    if (!idxNames.AddItem(index.GetName().ToUpper()))
                    {
                        throw new ArgumentException("duplicate index name: " + index.GetName());
                    }
                    if (index.IsPrimaryKey())
                    {
                        if (foundPk)
                        {
                            throw new ArgumentException("found second primary key index: " + index.GetName());
                        }
                        foundPk = true;
                    }
                }
            }
            //Write the tdef page to disk.
            int tdefPageNumber = Table.WriteTableDefinition(columns, indexes, _pageChannel, _format
                , GetCharset());
            //Add this table to our internal list.
            AddTable(name, Sharpen.Extensions.ValueOf(tdefPageNumber));
            //Add this table to system tables
            AddToSystemCatalog(name, tdefPageNumber);
            AddToAccessControlEntries(tdefPageNumber);
        }

        /// <summary>Finds all the relationships in the database between the given tables.</summary>
        /// <remarks>Finds all the relationships in the database between the given tables.</remarks>
        /// <usage>_intermediate_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual IList<Relationship> GetRelationships(Table table1, Table table2)
        {
            // the relationships table does not get loaded until first accessed
            if (_relationships == null)
            {
                _relationships = GetSystemTable(TABLE_SYSTEM_RELATIONSHIPS);
                if (_relationships == null)
                {
                    throw new IOException("Could not find system relationships table");
                }
            }
            int nameCmp = Sharpen.Runtime.CompareOrdinal(table1.GetName(), table2.GetName());
            if (nameCmp == 0)
            {
                throw new ArgumentException("Must provide two different tables");
            }
            if (nameCmp > 0)
            {
                // we "order" the two tables given so that we will return a collection
                // of relationships in the same order regardless of whether we are given
                // (TableFoo, TableBar) or (TableBar, TableFoo).
                Table tmp = table1;
                table1 = table2;
                table2 = tmp;
            }
            IList<Relationship> relationships = new AList<Relationship>();
            Cursor cursor = CreateCursorWithOptionalIndex(_relationships, REL_COL_FROM_TABLE,
                table1.GetName());
            CollectRelationships(cursor, table1, table2, relationships);
            cursor = CreateCursorWithOptionalIndex(_relationships, REL_COL_TO_TABLE, table1.GetName
                ());
            CollectRelationships(cursor, table2, table1, relationships);
            return relationships;
        }

        /// <summary>Finds all the queries in the database.</summary>
        /// <remarks>Finds all the queries in the database.</remarks>
        /// <usage>_intermediate_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual IList<HealthMarketScience.Jackcess.Query.Query> GetQueries()
        {
            // the queries table does not get loaded until first accessed
            if (_queries == null)
            {
                _queries = GetSystemTable(TABLE_SYSTEM_QUERIES);
                if (_queries == null)
                {
                    throw new IOException("Could not find system queries table");
                }
            }
            // find all the queries from the system catalog
            IList<IDictionary<string, object>> queryInfo = new AList<IDictionary<string, object
                >>();
            IDictionary<int, IList<Query.Query.Row>> queryRowMap = new Dictionary<int, IList<Query.Query.Row
                >>();
            foreach (IDictionary<string, object> row in Cursor.CreateCursor(_systemCatalog).Iterable
                (SYSTEM_CATALOG_COLUMNS))
            {
                string name = (string)row.Get(CAT_COL_NAME);
                if (name != null && TYPE_QUERY.Equals(row.Get(CAT_COL_TYPE)))
                {
                    queryInfo.AddItem(row);
                    int id = (int)row.Get(CAT_COL_ID);
                    queryRowMap.Put(id, new AList<Query.Query.Row>());
                }
            }
            // find all the query rows
            foreach (IDictionary<string, object> row_1 in Cursor.CreateCursor(_queries))
            {
                Query.Query.Row queryRow = new Query.Query.Row(row_1);
                IList<Query.Query.Row> queryRows = queryRowMap.Get(queryRow.objectId);
                if (queryRows == null)
                {
                    System.Console.Error.WriteLine("Found rows for query with id " + queryRow.objectId
                         + " missing from system catalog");
                    continue;
                }
                queryRows.AddItem(queryRow);
            }
            // lastly, generate all the queries
            IList<HealthMarketScience.Jackcess.Query.Query> queries = new AList<HealthMarketScience.Jackcess.Query.Query
                >();
            foreach (IDictionary<string, object> row_2 in queryInfo)
            {
                string name = (string)row_2.Get(CAT_COL_NAME);
                int id = (int)row_2.Get(CAT_COL_ID);
                int flags = (int)row_2.Get(CAT_COL_FLAGS);
                IList<Query.Query.Row> queryRows = queryRowMap.Get(id);
                queries.AddItem(HealthMarketScience.Jackcess.Query.Query.Create(flags, name, queryRows
                    , id));
            }
            return queries;
        }

        /// <summary>
        /// Returns a reference to <i>any</i> available table in this access
        /// database, including system tables.
        /// </summary>
        /// <remarks>
        /// Returns a reference to <i>any</i> available table in this access
        /// database, including system tables.
        /// <p>
        /// Warning, this method is not designed for common use, only for the
        /// occassional time when access to a system table is necessary.  Messing
        /// with system tables can strip the paint off your house and give your whole
        /// family a permanent, orange afro.  You have been warned.
        /// </remarks>
        /// <param name="tableName">Table name, may be a system table</param>
        /// <returns>
        /// The table, or
        /// <code>null</code>
        /// if it doesn't exist
        /// </returns>
        /// <usage>_intermediate_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual Table GetSystemTable(string tableName)
        {
            return GetTable(tableName, true, DefaultUseBigIndex());
        }

        /// <returns>the core properties for the database</returns>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual PropertyMap GetDatabaseProperties()
        {
            if (_dbPropMaps == null)
            {
                _dbPropMaps = GetPropertiesForDbObject(OBJECT_NAME_DB_PROPS);
            }
            return _dbPropMaps.GetDefault();
        }

        /// <returns>the summary properties for the database</returns>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual PropertyMap GetSummaryProperties()
        {
            if (_summaryPropMaps == null)
            {
                _summaryPropMaps = GetPropertiesForDbObject(OBJECT_NAME_SUMMARY_PROPS);
            }
            return _summaryPropMaps.GetDefault();
        }

        /// <returns>the user-defined properties for the database</returns>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual PropertyMap GetUserDefinedProperties()
        {
            if (_userDefPropMaps == null)
            {
                _userDefPropMaps = GetPropertiesForDbObject(OBJECT_NAME_USERDEF_PROPS);
            }
            return _userDefPropMaps.GetDefault();
        }

        /// <returns>the PropertyMaps for the object with the given id</returns>
        /// <usage>_advanced_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual PropertyMaps GetPropertiesForObject(int objectId)
        {
            IDictionary<string, object> objectRow = _tableFinder.GetObjectRow(objectId, SYSTEM_CATALOG_PROPS_COLUMNS
                );
            byte[] propsBytes = null;
            if (objectRow != null)
            {
                propsBytes = (byte[])objectRow.Get(CAT_COL_PROPS);
            }
            return ReadProperties(propsBytes, objectId);
        }

        /// <returns>property group for the given "database" object</returns>
        /// <exception cref="System.IO.IOException"></exception>
        private PropertyMaps GetPropertiesForDbObject(string dbName)
        {
            if (_dbParentId == null)
            {
                // need the parent if of the databases objects
                _dbParentId = _tableFinder.FindObjectId(DB_PARENT_ID, SYSTEM_OBJECT_NAME_DATABASES
                    );
                if (_dbParentId == null)
                {
                    throw new IOException("Did not find required parent db id");
                }
            }
            IDictionary<string, object> objectRow = _tableFinder.GetObjectRow(_dbParentId.Value, dbName
                , SYSTEM_CATALOG_PROPS_COLUMNS);
            byte[] propsBytes = null;
            int objectId = -1;
            if (objectRow != null)
            {
                propsBytes = (byte[])objectRow.Get(CAT_COL_PROPS);
                objectId = (int)objectRow.Get(CAT_COL_ID);
            }
            return ReadProperties(propsBytes, objectId);
        }

        /// <returns>
        /// the current database password, or
        /// <code>null</code>
        /// if none set.
        /// </returns>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual string GetDatabasePassword()
        {
            ByteBuffer buffer = TakeSharedBuffer();
            try
            {
                _pageChannel.ReadPage(buffer, 0);
                byte[] pwdBytes = new byte[_format.SIZE_PASSWORD];
                buffer.Position(_format.OFFSET_PASSWORD);
                buffer.Get(pwdBytes);
                // de-mask password using extra password mask if necessary (the extra
                // password mask is generated from the database creation date stored in
                // the header)
                byte[] pwdMask = GetPasswordMask(buffer, _format);
                if (pwdMask != null)
                {
                    for (int i = 0; i < pwdBytes.Length; ++i)
                    {
                        pwdBytes[i] ^= pwdMask[i % pwdMask.Length];
                    }
                }
                bool hasPassword = false;
                for (int i_1 = 0; i_1 < pwdBytes.Length; ++i_1)
                {
                    if (pwdBytes[i_1] != 0)
                    {
                        hasPassword = true;
                        break;
                    }
                }
                if (!hasPassword)
                {
                    return null;
                }
                string pwd = Column.DecodeUncompressedText(pwdBytes, GetCharset());
                // remove any trailing null chars
                int idx = pwd.IndexOf('\0');
                if (idx >= 0)
                {
                    pwd = Sharpen.Runtime.Substring(pwd, 0, idx);
                }
                return pwd;
            }
            finally
            {
                ReleaseSharedBuffer(buffer);
            }
        }

        /// <summary>
        /// Finds the relationships matching the given from and to tables from the
        /// given cursor and adds them to the given list.
        /// </summary>
        /// <remarks>
        /// Finds the relationships matching the given from and to tables from the
        /// given cursor and adds them to the given list.
        /// </remarks>
        private static void CollectRelationships(Cursor cursor, Table fromTable, Table toTable
            , IList<Relationship> relationships)
        {
            foreach (IDictionary<string, object> row in cursor)
            {
                string fromName = (string)row.Get(REL_COL_FROM_TABLE);
                string toName = (string)row.Get(REL_COL_TO_TABLE);
                if (Sharpen.Runtime.EqualsIgnoreCase(fromTable.GetName(), fromName) && Sharpen.Runtime.EqualsIgnoreCase
                    (toTable.GetName(), toName))
                {
                    string relName = (string)row.Get(REL_COL_NAME);
                    // found more info for a relationship.  see if we already have some
                    // info for this relationship
                    Relationship rel = null;
                    foreach (Relationship tmp in relationships)
                    {
                        if (Sharpen.Runtime.EqualsIgnoreCase(tmp.GetName(), relName))
                        {
                            rel = tmp;
                            break;
                        }
                    }
                    if (rel == null)
                    {
                        // new relationship
                        int numCols = (int)row.Get(REL_COL_COLUMN_COUNT);
                        int flags = (int)row.Get(REL_COL_FLAGS);
                        rel = new Relationship(relName, fromTable, toTable, flags, numCols);
                        relationships.AddItem(rel);
                    }
                    // add column info
                    int colIdx = (int)row.Get(REL_COL_COLUMN_INDEX);
                    Column fromCol = fromTable.GetColumn((string)row.Get(REL_COL_FROM_COLUMN));
                    Column toCol = toTable.GetColumn((string)row.Get(REL_COL_TO_COLUMN));
                    rel.GetFromColumns().Set(colIdx, fromCol);
                    rel.GetToColumns().Set(colIdx, toCol);
                }
            }
        }

        /// <summary>Add a new table to the system catalog</summary>
        /// <param name="name">Table name</param>
        /// <param name="pageNumber">Page number that contains the table definition</param>
        /// <exception cref="System.IO.IOException"></exception>
        private void AddToSystemCatalog(string name, int pageNumber)
        {
            object[] catalogRow = new object[_systemCatalog.GetColumnCount()];
            int idx = 0;
            DateTime creationTime = new DateTime();
            for (Iterator<Column> iter = _systemCatalog.GetColumns().Iterator(); iter.HasNext
                (); idx++)
            {
                Column col = iter.Next();
                if (CAT_COL_ID.Equals(col.GetName()))
                {
                    catalogRow[idx] = Sharpen.Extensions.ValueOf(pageNumber);
                }
                else
                {
                    if (CAT_COL_NAME.Equals(col.GetName()))
                    {
                        catalogRow[idx] = name;
                    }
                    else
                    {
                        if (CAT_COL_TYPE.Equals(col.GetName()))
                        {
                            catalogRow[idx] = TYPE_TABLE;
                        }
                        else
                        {
                            if (CAT_COL_DATE_CREATE.Equals(col.GetName()) || CAT_COL_DATE_UPDATE.Equals(col.GetName
                                ()))
                            {
                                catalogRow[idx] = creationTime;
                            }
                            else
                            {
                                if (CAT_COL_PARENT_ID.Equals(col.GetName()))
                                {
                                    catalogRow[idx] = _tableParentId;
                                }
                                else
                                {
                                    if (CAT_COL_FLAGS.Equals(col.GetName()))
                                    {
                                        catalogRow[idx] = Sharpen.Extensions.ValueOf(0);
                                    }
                                    else
                                    {
                                        if (CAT_COL_OWNER.Equals(col.GetName()))
                                        {
                                            byte[] owner = new byte[2];
                                            catalogRow[idx] = owner;
                                            owner[0] = unchecked((byte)unchecked((int)(0xcf)));
                                            owner[1] = unchecked((byte)unchecked((int)(0x5f)));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            _systemCatalog.AddRow(catalogRow);
        }

        /// <summary>Add a new table to the system's access control entries</summary>
        /// <param name="pageNumber">Page number that contains the table definition</param>
        /// <exception cref="System.IO.IOException"></exception>
        private void AddToAccessControlEntries(int pageNumber)
        {
            if (_newTableSIDs.IsEmpty())
            {
                InitNewTableSIDs();
            }
            Table acEntries = GetAccessControlEntries();
            Column acmCol = acEntries.GetColumn(ACE_COL_ACM);
            Column inheritCol = acEntries.GetColumn(ACE_COL_F_INHERITABLE);
            Column objIdCol = acEntries.GetColumn(ACE_COL_OBJECT_ID);
            Column sidCol = acEntries.GetColumn(ACE_COL_SID);
            // construct a collection of ACE entries mimicing those of our parent, the
            // "Tables" system object
            IList<object[]> aceRows = new AList<object[]>(_newTableSIDs.Count);
            foreach (byte[] sid in _newTableSIDs)
            {
                object[] aceRow = new object[acEntries.GetColumnCount()];
                aceRow[acmCol.GetColumnIndex()] = SYS_FULL_ACCESS_ACM;
                aceRow[inheritCol.GetColumnIndex()] = false;
                aceRow[objIdCol.GetColumnIndex()] = Sharpen.Extensions.ValueOf(pageNumber);
                aceRow[sidCol.GetColumnIndex()] = sid;
                aceRows.AddItem(aceRow);
            }
            acEntries.AddRows(aceRows);
        }

        /// <summary>Determines the collection of SIDs which need to be added to new tables.</summary>
        /// <remarks>Determines the collection of SIDs which need to be added to new tables.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private void InitNewTableSIDs()
        {
            // search for ACEs matching the tableParentId.  use the index on the
            // objectId column if found (should be there)
            Cursor cursor = CreateCursorWithOptionalIndex(GetAccessControlEntries(), ACE_COL_OBJECT_ID
                , _tableParentId);
            foreach (IDictionary<string, object> row in cursor)
            {
                int objId = (int)row.Get(ACE_COL_OBJECT_ID);
                if (_tableParentId.Equals(objId))
                {
                    _newTableSIDs.AddItem((byte[])row.Get(ACE_COL_SID));
                }
            }
            if (_newTableSIDs.IsEmpty())
            {
                // if all else fails, use the hard-coded default
                _newTableSIDs.AddItem(SYS_DEFAULT_SID);
            }
        }

        /// <summary>Reads a table with the given name from the given pageNumber.</summary>
        /// <remarks>Reads a table with the given name from the given pageNumber.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private Table ReadTable(string name, int pageNumber, int flags, bool useBigIndex)
        {
            // first, check for existing table
            Table table = _tableCache.Get(pageNumber);
            if (table != null)
            {
                return table;
            }
            ByteBuffer buffer = TakeSharedBuffer();
            try
            {
                // need to load table from db
                _pageChannel.ReadPage(buffer, pageNumber);
                byte pageType = buffer.Get(0);
                if (pageType != PageTypes.TABLE_DEF)
                {
                    throw new IOException("Looking for " + name + " at page " + pageNumber + ", but page type is "
                         + pageType);
                }
                return _tableCache.Put(new Table(this, buffer, pageNumber, name, flags, useBigIndex
                    ));
            }
            finally
            {
                ReleaseSharedBuffer(buffer);
            }
        }

        /// <summary>
        /// Creates a Cursor restricted to the given column value if possible (using
        /// an existing index), otherwise a simple table cursor.
        /// </summary>
        /// <remarks>
        /// Creates a Cursor restricted to the given column value if possible (using
        /// an existing index), otherwise a simple table cursor.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private static Cursor CreateCursorWithOptionalIndex(Table table, string colName,
            object colValue)
        {
            try
            {
                return new CursorBuilder(table).SetIndexByColumns(table.GetColumn(colName)).SetSpecificEntry
                    (colValue).ToCursor();
            }
            catch (ArgumentException)
            {
                System.Console.Error.WriteLine("Could not find expected index on table " + table.
                    GetName());
            }
            // use table scan instead
            return Cursor.CreateCursor(table);
        }

        /// <summary>Copy a delimited text file into a new table in this database</summary>
        /// <param name="name">Name of the new table to create</param>
        /// <param name="f">Source file to import</param>
        /// <param name="delim">Regular expression representing the delimiter string.</param>
        /// <returns>the name of the imported table</returns>
        /// <seealso cref="ImportUtil.ImportFile(Sharpen.FilePath, Database, string, string)"
        /// 	>ImportUtil.ImportFile(Sharpen.FilePath, Database, string, string)</seealso>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual string ImportFile(string name, FilePath f, string delim)
        {
            return ImportUtil.ImportFile(f, this, name, delim);
        }

        /// <summary>Copy a delimited text file into a new table in this database</summary>
        /// <param name="name">Name of the new table to create</param>
        /// <param name="f">Source file to import</param>
        /// <param name="delim">Regular expression representing the delimiter string.</param>
        /// <param name="filter">valid import filter</param>
        /// <returns>the name of the imported table</returns>
        /// <seealso cref="ImportUtil.ImportFile(Sharpen.FilePath, Database, string, string, ImportFilter)
        /// 	">ImportUtil.ImportFile(Sharpen.FilePath, Database, string, string, ImportFilter)
        /// 	</seealso>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual string ImportFile(string name, FilePath f, string delim, ImportFilter
             filter)
        {
            return ImportUtil.ImportFile(f, this, name, delim, filter);
        }

        /// <summary>Copy a delimited text file into a new table in this database</summary>
        /// <param name="name">Name of the new table to create</param>
        /// <param name="in">Source reader to import</param>
        /// <param name="delim">Regular expression representing the delimiter string.</param>
        /// <returns>the name of the imported table</returns>
        /// <seealso cref="ImportUtil.ImportReader(System.IO.BufferedReader, Database, string, string)
        /// 	">ImportUtil.ImportReader(System.IO.BufferedReader, Database, string, string)</seealso>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual string ImportReader(string name, BufferedReader @in, string delim)
        {
            return ImportUtil.ImportReader(@in, this, name, delim);
        }

        /// <summary>Copy a delimited text file into a new table in this database</summary>
        /// <param name="name">Name of the new table to create</param>
        /// <param name="in">Source reader to import</param>
        /// <param name="delim">Regular expression representing the delimiter string.</param>
        /// <param name="filter">valid import filter</param>
        /// <returns>the name of the imported table</returns>
        /// <seealso cref="ImportUtil.ImportReader(System.IO.BufferedReader, Database, string, string, ImportFilter)
        /// 	">ImportUtil.ImportReader(System.IO.BufferedReader, Database, string, string, ImportFilter)
        /// 	</seealso>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual string ImportReader(string name, BufferedReader @in, string delim,
            ImportFilter filter)
        {
            return ImportUtil.ImportReader(@in, this, name, delim, filter);
        }

        /// <summary>Flushes any current changes to the database file to disk.</summary>
        /// <remarks>Flushes any current changes to the database file to disk.</remarks>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void Flush()
        {
            _pageChannel.Flush();
        }

        /// <summary>Close the database file</summary>
        /// <usage>_general_method_</usage>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void Close()
        {
            _pageChannel.Close();
        }

        /// <exception cref="System.IO.IOException"></exception>
        public virtual void Dispose()
        {
            Close();
        }

        /// <returns>A table or column name escaped for Access</returns>
        /// <usage>_general_method_</usage>
        public static string EscapeIdentifier(string s)
        {
            if (IsReservedWord(s))
            {
                return ESCAPE_PREFIX + s;
            }
            return s;
        }

        /// <returns>
        /// 
        /// <code>true</code>
        /// if the given string is a reserved word,
        /// <code>false</code>
        /// otherwise
        /// </returns>
        /// <usage>_general_method_</usage>
        public static bool IsReservedWord(string s)
        {
            return RESERVED_WORDS.Contains(s.ToLower());
        }

        /// <summary>Validates an identifier name.</summary>
        /// <remarks>Validates an identifier name.</remarks>
        /// <usage>_advanced_method_</usage>
        public static void ValidateIdentifierName(string name, int maxLength, string identifierType
            )
        {
            if ((name == null) || (name.Trim().Length == 0))
            {
                throw new ArgumentException(identifierType + " must have non-empty name");
            }
            if (name.Length > maxLength)
            {
                throw new ArgumentException(identifierType + " name is longer than max length of "
                     + maxLength + ": " + name);
            }
        }

        /// <summary>Adds a table to the _tableLookup and resets the _tableNames set</summary>
        private void AddTable(string tableName, int pageNumber)
        {
            _tableLookup.Put(ToLookupName(tableName), new Database.TableInfo(pageNumber, tableName
                , 0));
            // clear this, will be created next time needed
            _tableNames = null;
        }

        /// <returns>the tableInfo of the given table, if any</returns>
        /// <exception cref="System.IO.IOException"></exception>
        private Database.TableInfo LookupTable(string tableName)
        {
            string lookupTableName = ToLookupName(tableName);
            Database.TableInfo tableInfo = _tableLookup.Get(lookupTableName);
            if (tableInfo != null)
            {
                return tableInfo;
            }
            tableInfo = _tableFinder.LookupTable(tableName);
            if (tableInfo != null)
            {
                // cache for later
                _tableLookup.Put(lookupTableName, tableInfo);
            }
            return tableInfo;
        }

        /// <returns>a string usable in the _tableLookup map.</returns>
        internal static string ToLookupName(string name)
        {
            return ((name != null) ? name.ToUpper() : null);
        }

        /// <returns>
        /// 
        /// <code>true</code>
        /// if the given flags indicate that an object is some
        /// sort of system object,
        /// <code>false</code>
        /// otherwise.
        /// </returns>
        private static bool IsSystemObject(int flags)
        {
            return ((flags & SYSTEM_OBJECT_FLAGS) != 0);
        }

        /// <summary>
        /// Returns
        /// <code>false</code>
        /// if "big index support" has been disabled explicity
        /// on the this Database or via a system property,
        /// <code>true</code>
        /// otherwise.
        /// </summary>
        /// <usage>_advanced_method_</usage>
        public virtual bool DefaultUseBigIndex()
        {
            if (_useBigIndex != null)
            {
                return _useBigIndex.Value;
            }
            string prop = Runtime.GetProperty(USE_BIG_INDEX_PROPERTY);
            if (prop != null)
            {
                return Sharpen.Runtime.EqualsIgnoreCase(true.ToString(), prop);
            }
            return true;
        }

        /// <summary>Returns the default TimeZone.</summary>
        /// <remarks>
        /// Returns the default TimeZone.  This is normally the platform default
        /// TimeZone as returned by
        /// <see cref="System.TimeZoneInfo.Local()">System.TimeZoneInfo.Local()</see>
        /// , but can be
        /// overridden using the system property
        /// <value>#TIMEZONE_PROPERTY</value>
        /// .
        /// </remarks>
        /// <usage>_advanced_method_</usage>
        public static TimeZoneInfo GetDefaultTimeZone()
        {
            string tzProp = Runtime.GetProperty(TIMEZONE_PROPERTY);
            if (tzProp != null)
            {
                tzProp = tzProp.Trim();
                if (tzProp.Length > 0)
                {
                    return Sharpen.Extensions.GetTimeZone(tzProp);
                }
            }
            // use system default
            return System.TimeZoneInfo.Local;
        }

        /// <summary>Returns the default Charset for the given JetFormat.</summary>
        /// <remarks>
        /// Returns the default Charset for the given JetFormat.  This may or may not
        /// be platform specific, depending on the format, but can be overridden
        /// using a system property composed of the prefix
        /// <value>#CHARSET_PROPERTY_PREFIX</value>
        /// followed by the JetFormat version to
        /// which the charset should apply, e.g.
        /// <code>"com.healthmarketscience.jackcess.charset.VERSION_3"</code>
        /// .
        /// </remarks>
        /// <usage>_advanced_method_</usage>
        public static Encoding GetDefaultCharset(JetFormat format)
        {
            string csProp = Runtime.GetProperty(CHARSET_PROPERTY_PREFIX + format);
            if (csProp != null)
            {
                csProp = csProp.Trim();
                if (csProp.Length > 0)
                {
                    return Sharpen.Extensions.GetEncoding(csProp);
                }
            }
            // use format default
            return format.CHARSET;
        }

        /// <summary>Returns the default Table.ColumnOrder.</summary>
        /// <remarks>
        /// Returns the default Table.ColumnOrder.  This defaults to
        /// <see cref="DEFAULT_COLUMN_ORDER">DEFAULT_COLUMN_ORDER</see>
        /// , but can be overridden using the system
        /// property
        /// <value>#COLUMN_ORDER_PROPERTY</value>
        /// .
        /// </remarks>
        /// <usage>_advanced_method_</usage>
        public static Table.ColumnOrder GetDefaultColumnOrder()
        {
            string coProp = Runtime.GetProperty(COLUMN_ORDER_PROPERTY);
            if (coProp != null)
            {
                coProp = coProp.Trim();
                if (coProp.Length > 0)
                {
                    return (Table.ColumnOrder)Enum.Parse(typeof(Table.ColumnOrder), coProp);
                }
            }
            // use default order
            return DEFAULT_COLUMN_ORDER;
        }

        /// <summary>
        /// Copies the given InputStream to the given channel using the most
        /// efficient means possible.
        /// </summary>
        /// <remarks>
        /// Copies the given InputStream to the given channel using the most
        /// efficient means possible.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private static void TransferFrom(FileChannel channel, InputStream @in)
        {
            ReadableByteChannel readChannel = Channels.NewChannel(@in);
            if (!BROKEN_NIO)
            {
                // sane implementation
                channel.TransferFrom(readChannel, 0, MAX_EMPTYDB_SIZE);
            }
            else
            {
                // do things the hard way for broken vms
                ByteBuffer bb = ByteBuffer.Allocate(8096);
                while (readChannel.Read(bb) >= 0)
                {
                    bb.Flip();
                    channel.Write(bb);
                    bb.Clear();
                }
            }
        }

        /// <summary>
        /// Returns the password mask retrieved from the given header page and
        /// format, or
        /// <code>null</code>
        /// if this format does not use a password mask.
        /// </summary>
        internal static byte[] GetPasswordMask(ByteBuffer buffer, JetFormat format)
        {
            // get extra password mask if necessary (the extra password mask is
            // generated from the database creation date stored in the header)
            int pwdMaskPos = format.OFFSET_HEADER_DATE;
            if (pwdMaskPos < 0)
            {
                return null;
            }
            buffer.Position(pwdMaskPos);
            double dateVal = BitConverter.Int64BitsToDouble(buffer.GetLong());
            byte[] pwdMask = new byte[4];
            ByteBuffer.Wrap(pwdMask).Order(PageChannel.DEFAULT_BYTE_ORDER).PutInt((int)dateVal
                );
            return pwdMask;
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal static InputStream GetResourceAsStream(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            return stream;
        }

        /// <summary>Utility class for storing table page number and actual name.</summary>
        /// <remarks>Utility class for storing table page number and actual name.</remarks>
        public class TableInfo
        {
            public readonly int pageNumber;

            public readonly string tableName;

            public readonly int flags;

            public TableInfo(int newPageNumber, string newTableName, int newFlags)
            {
                pageNumber = newPageNumber;
                tableName = newTableName;
                flags = newFlags;
            }
        }

        /// <summary>Table iterator for this database, unmodifiable.</summary>
        /// <remarks>Table iterator for this database, unmodifiable.</remarks>
        private class TableIterator : Iterator<Table>
        {
            private Iterator<string> _tableNameIter;

            public TableIterator(Database _enclosing)
            {
                this._enclosing = _enclosing;
                try
                {
                    this._tableNameIter = this._enclosing.GetTableNames().Iterator();
                }
                catch (IOException e)
                {
                    throw new InvalidOperationException(e.ToString());
                }
            }

            public override bool HasNext()
            {
                return this._tableNameIter.HasNext();
            }

            public override void Remove()
            {
                throw new NotSupportedException();
            }

            public override Table Next()
            {
                if (!this.HasNext())
                {
                    throw new NoSuchElementException();
                }
                try
                {
                    return this._enclosing.GetTable(this._tableNameIter.Next());
                }
                catch (IOException e)
                {
                    throw new InvalidOperationException(e.ToString());
                }
            }

            private readonly Database _enclosing;
        }

        /// <summary>Utility class for handling table lookups.</summary>
        /// <remarks>Utility class for handling table lookups.</remarks>
        public abstract class TableFinder
        {
            /// <exception cref="System.IO.IOException"></exception>
            public virtual Nullable<int> FindObjectId(int parentId, string name)
            {
                Cursor cur = this.FindRow(parentId, name);
                if (cur == null)
                {
                    return null;
                }
                Column idCol = this._enclosing._systemCatalog.GetColumn(Database.CAT_COL_ID);
                return (int)cur.GetCurrentRowValue(idCol);
            }

            /// <exception cref="System.IO.IOException"></exception>
            public virtual IDictionary<string, object> GetObjectRow(int parentId, string name
                , ICollection<string> columns)
            {
                Cursor cur = this.FindRow(parentId, name);
                return ((cur != null) ? cur.GetCurrentRow(columns) : null);
            }

            /// <exception cref="System.IO.IOException"></exception>
            public virtual IDictionary<string, object> GetObjectRow(int objectId, ICollection
                <string> columns)
            {
                Cursor cur = this.FindRow(objectId);
                return ((cur != null) ? cur.GetCurrentRow(columns) : null);
            }

            /// <exception cref="System.IO.IOException"></exception>
            public virtual void GetTableNames(ICollection<string> tableNames, bool systemTables
                )
            {
                foreach (IDictionary<string, object> row in this.GetTableNamesCursor().Iterable(Database
                    .SYSTEM_CATALOG_TABLE_NAME_COLUMNS))
                {
                    string tableName = (string)row.Get(Database.CAT_COL_NAME);
                    int flags = (int)row.Get(Database.CAT_COL_FLAGS);
                    short type = (short)row.Get(Database.CAT_COL_TYPE);
                    int parentId = (int)row.Get(Database.CAT_COL_PARENT_ID);
                    if ((parentId == this._enclosing._tableParentId) && Database.TYPE_TABLE.Equals(type
                        ) && (Database.IsSystemObject(flags) == systemTables))
                    {
                        tableNames.AddItem(tableName);
                    }
                }
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract Cursor FindRow(int parentId, string name);

            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract Cursor FindRow(int objectId);

            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract Cursor GetTableNamesCursor();

            /// <exception cref="System.IO.IOException"></exception>
            public abstract Database.TableInfo LookupTable(string tableName);

            internal TableFinder(Database _enclosing)
            {
                this._enclosing = _enclosing;
            }

            private readonly Database _enclosing;
        }

        /// <summary>Normal table lookup handler, using catalog table index.</summary>
        /// <remarks>Normal table lookup handler, using catalog table index.</remarks>
        public sealed class DefaultTableFinder : Database.TableFinder
        {
            private readonly IndexCursor _systemCatalogCursor;

            private IndexCursor _systemCatalogIdCursor;

            public DefaultTableFinder(Database _enclosing, IndexCursor systemCatalogCursor) :
                base(_enclosing)
            {
                this._enclosing = _enclosing;
                this._systemCatalogCursor = systemCatalogCursor;
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override Cursor FindRow(int parentId, string name)
            {
                return (this._systemCatalogCursor.FindRowByEntry(parentId, name) ? this._systemCatalogCursor
                     : null);
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override Cursor FindRow(int objectId)
            {
                if (this._systemCatalogIdCursor == null)
                {
                    this._systemCatalogIdCursor = new CursorBuilder(this._enclosing._systemCatalog).SetIndexByColumnNames
                        (Database.CAT_COL_ID).ToIndexCursor();
                }
                return (this._systemCatalogIdCursor.FindRowByEntry(objectId) ? this._systemCatalogIdCursor
                     : null);
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override Database.TableInfo LookupTable(string tableName)
            {
                if (this.FindRow(this._enclosing._tableParentId.Value, tableName) == null)
                {
                    return null;
                }
                IDictionary<string, object> row = this._systemCatalogCursor.GetCurrentRow(Database
                    .SYSTEM_CATALOG_COLUMNS);
                int pageNumber = (int)row.Get(Database.CAT_COL_ID);
                string realName = (string)row.Get(Database.CAT_COL_NAME);
                int flags = (int)row.Get(Database.CAT_COL_FLAGS);
                short type = (short)row.Get(Database.CAT_COL_TYPE);
                if (!Database.TYPE_TABLE.Equals(type))
                {
                    return null;
                }
                return new Database.TableInfo(pageNumber, realName, flags);
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override Cursor GetTableNamesCursor()
            {
                return new CursorBuilder(this._enclosing._systemCatalog).SetIndex(this._systemCatalogCursor
                    .GetIndex()).SetStartEntry(this._enclosing._tableParentId, IndexData.MIN_VALUE).
                    SetEndEntry(this._enclosing._tableParentId, IndexData.MAX_VALUE).ToIndexCursor();
            }

            private readonly Database _enclosing;
        }

        /// <summary>Fallback table lookup handler, using catalog table scans.</summary>
        /// <remarks>Fallback table lookup handler, using catalog table scans.</remarks>
        public sealed class FallbackTableFinder : Database.TableFinder
        {
            private readonly Cursor _systemCatalogCursor;

            public FallbackTableFinder(Database _enclosing, Cursor systemCatalogCursor) : base
                (_enclosing)
            {
                this._enclosing = _enclosing;
                this._systemCatalogCursor = systemCatalogCursor;
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override Cursor FindRow(int parentId, string name)
            {
                IDictionary<string, object> rowPat = new Dictionary<string, object>();
                rowPat.Put(Database.CAT_COL_PARENT_ID, parentId);
                rowPat.Put(Database.CAT_COL_NAME, name);
                return (this._systemCatalogCursor.FindRow(rowPat) ? this._systemCatalogCursor : null
                    );
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override Cursor FindRow(int objectId)
            {
                Column idCol = this._enclosing._systemCatalog.GetColumn(Database.CAT_COL_ID);
                return (this._systemCatalogCursor.FindRow(idCol, objectId) ? this._systemCatalogCursor
                     : null);
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override Database.TableInfo LookupTable(string tableName)
            {
                foreach (IDictionary<string, object> row in this._systemCatalogCursor.Iterable(Database
                    .SYSTEM_CATALOG_TABLE_NAME_COLUMNS))
                {
                    short type = (short)row.Get(Database.CAT_COL_TYPE);
                    if (!Database.TYPE_TABLE.Equals(type))
                    {
                        continue;
                    }
                    int parentId = (int)row.Get(Database.CAT_COL_PARENT_ID);
                    if (parentId != this._enclosing._tableParentId)
                    {
                        continue;
                    }
                    string realName = (string)row.Get(Database.CAT_COL_NAME);
                    if (!Sharpen.Runtime.EqualsIgnoreCase(tableName, realName))
                    {
                        continue;
                    }
                    int pageNumber = (int)row.Get(Database.CAT_COL_ID);
                    int flags = (int)row.Get(Database.CAT_COL_FLAGS);
                    return new Database.TableInfo(pageNumber, realName, flags);
                }
                return null;
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override Cursor GetTableNamesCursor()
            {
                return this._systemCatalogCursor;
            }

            private readonly Database _enclosing;
        }

        /// <summary>
        /// WeakReference for a Table which holds the table pageNumber (for later
        /// cache purging).
        /// </summary>
        /// <remarks>
        /// WeakReference for a Table which holds the table pageNumber (for later
        /// cache purging).
        /// </remarks>
        public sealed class WeakTableReference : JavaWeakReference<Table>
        {
            private readonly int _pageNumber;

            public WeakTableReference(int pageNumber, Table table) : base(table)
            {
                _pageNumber = pageNumber;
            }

            public int GetPageNumber()
            {
                return _pageNumber;
            }
        }

        /// <summary>Cache of currently in-use tables, allows re-use of existing tables.</summary>
        /// <remarks>Cache of currently in-use tables, allows re-use of existing tables.</remarks>
        private sealed class TableCache
        {
            private readonly IDictionary<int, Database.WeakTableReference> _tables = new Dictionary
                <int, Database.WeakTableReference>();

            //private readonly ReferenceQueue<Table> _queue = new ReferenceQueue<Table>();

            public Table Get(int pageNumber)
            {
                Database.WeakTableReference @ref = _tables.Get(pageNumber);
                return ((@ref != null) ? @ref.Get() : null);
            }

            public Table Put(Table table)
            {
                PurgeOldRefs();
                int pageNumber = table.GetTableDefPageNumber();
                Database.WeakTableReference @ref = new Database.WeakTableReference(pageNumber, table);
                _tables.Put(pageNumber, @ref);
                return table;
            }

            private void PurgeOldRefs()
            {
                //Database.WeakTableReference oldRef = null;
                //while ((oldRef = (Database.WeakTableReference)_queue.Poll()) != null)
                //{
                //	Sharpen.Collections.Remove(_tables, oldRef.GetPageNumber());
                //}
            }
        }
    }
}
