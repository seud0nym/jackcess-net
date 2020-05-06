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
using System.Text;
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <author>James Ahlborn</author>
	public class ImportUtil
	{
		/// <summary>Batch commit size for copying other result sets into this database</summary>
		private const int COPY_TABLE_BATCH_SIZE = 200;

		/// <summary>the platform line separator</summary>
		internal static readonly string LINE_SEPARATOR = Runtime.GetProperty("line.separator"
			);

		public ImportUtil()
		{
		}

		/// <summary>Copy a delimited text file into a new table in this database.</summary>
		/// <remarks>
		/// Copy a delimited text file into a new table in this database.
		/// <p>
		/// Equivalent to:
		/// <code>importFile(f, name, db, delim, SimpleImportFilter.INSTANCE);</code>
		/// </remarks>
		/// <param name="name">Name of the new table to create</param>
		/// <param name="f">Source file to import</param>
		/// <param name="delim">Regular expression representing the delimiter string.</param>
		/// <returns>the name of the imported table</returns>
		/// <seealso cref="ImportFile(Sharpen.FilePath, Database, string, string, ImportFilter)
		/// 	">ImportFile(Sharpen.FilePath, Database, string, string, ImportFilter)</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static string ImportFile(FilePath f, Database db, string name, string delim
			)
		{
			return ImportFile(f, db, name, delim, SimpleImportFilter.INSTANCE);
		}

		/// <summary>Copy a delimited text file into a new table in this database.</summary>
		/// <remarks>
		/// Copy a delimited text file into a new table in this database.
		/// <p>
		/// Equivalent to:
		/// <code>importFile(f, name, db, delim, "'", filter, false);</code>
		/// </remarks>
		/// <param name="name">Name of the new table to create</param>
		/// <param name="f">Source file to import</param>
		/// <param name="delim">Regular expression representing the delimiter string.</param>
		/// <param name="filter">valid import filter</param>
		/// <returns>the name of the imported table</returns>
		/// <seealso cref="ImportReader(System.IO.BufferedReader, Database, string, string, ImportFilter)
		/// 	">ImportReader(System.IO.BufferedReader, Database, string, string, ImportFilter)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static string ImportFile(FilePath f, Database db, string name, string delim
			, ImportFilter filter)
		{
			return ImportFile(f, db, name, delim, ExportUtil.DEFAULT_QUOTE_CHAR, filter, false
				);
		}

		/// <summary>Copy a delimited text file into a new table in this database.</summary>
		/// <remarks>
		/// Copy a delimited text file into a new table in this database.
		/// <p>
		/// Equivalent to:
		/// <code>importReader(new BufferedReader(new FileReader(f)), db, name, delim, "'", filter, false);
		/// 	</code>
		/// </remarks>
		/// <param name="name">Name of the new table to create</param>
		/// <param name="f">Source file to import</param>
		/// <param name="delim">Regular expression representing the delimiter string.</param>
		/// <param name="quote">the quote character</param>
		/// <param name="filter">valid import filter</param>
		/// <param name="useExistingTable">
		/// if
		/// <code>true</code>
		/// use current table if it already
		/// exists, otherwise, create new table with unique
		/// name
		/// </param>
		/// <returns>the name of the imported table</returns>
		/// <seealso cref="ImportReader(System.IO.BufferedReader, Database, string, string, ImportFilter)
		/// 	">ImportReader(System.IO.BufferedReader, Database, string, string, ImportFilter)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static string ImportFile(FilePath f, Database db, string name, string delim
			, char quote, ImportFilter filter, bool useExistingTable)
		{
			BufferedReader @in = null;
			try
			{
				@in = new BufferedReader(new FileReader(f));
				return ImportReader(@in, db, name, delim, quote, filter, useExistingTable);
			}
			finally
			{
				if (@in != null)
				{
					try
					{
						@in.Close();
					}
					catch (IOException ex)
					{
						System.Console.Error.WriteLine("Could not close file " + f.GetAbsolutePath());
						Sharpen.Runtime.PrintStackTrace(ex, System.Console.Error);
					}
				}
			}
		}

		/// <summary>Copy a delimited text file into a new table in this database.</summary>
		/// <remarks>
		/// Copy a delimited text file into a new table in this database.
		/// <p>
		/// Equivalent to:
		/// <code>importReader(in, db, name, delim, SimpleImportFilter.INSTANCE);</code>
		/// </remarks>
		/// <param name="name">Name of the new table to create</param>
		/// <param name="in">Source reader to import</param>
		/// <param name="delim">Regular expression representing the delimiter string.</param>
		/// <returns>the name of the imported table</returns>
		/// <seealso cref="ImportReader(System.IO.BufferedReader, Database, string, string, ImportFilter)
		/// 	">ImportReader(System.IO.BufferedReader, Database, string, string, ImportFilter)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static string ImportReader(BufferedReader @in, Database db, string name, string
			 delim)
		{
			return ImportReader(@in, db, name, delim, SimpleImportFilter.INSTANCE);
		}

		/// <summary>Copy a delimited text file into a new table in this database.</summary>
		/// <remarks>
		/// Copy a delimited text file into a new table in this database.
		/// <p>
		/// Equivalent to:
		/// <code>importReader(in, db, name, delim, filter, false);</code>
		/// </remarks>
		/// <param name="name">Name of the new table to create</param>
		/// <param name="in">Source reader to import</param>
		/// <param name="delim">Regular expression representing the delimiter string.</param>
		/// <param name="filter">valid import filter</param>
		/// <returns>the name of the imported table</returns>
		/// <seealso cref="ImportReader(System.IO.BufferedReader, Database, string, string, ImportFilter, bool)
		/// 	">ImportReader(System.IO.BufferedReader, Database, string, string, ImportFilter, bool)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static string ImportReader(BufferedReader @in, Database db, string name, string
			 delim, ImportFilter filter)
		{
			return ImportReader(@in, db, name, delim, filter, false);
		}

		/// <summary>
		/// Copy a delimited text file into a new (or optionally exixsting) table in
		/// this database.
		/// </summary>
		/// <remarks>
		/// Copy a delimited text file into a new (or optionally exixsting) table in
		/// this database.
		/// <p>
		/// Equivalent to:
		/// <code>importReader(in, db, name, delim, '"', filter, false);</code>
		/// </remarks>
		/// <param name="name">Name of the new table to create</param>
		/// <param name="in">Source reader to import</param>
		/// <param name="delim">Regular expression representing the delimiter string.</param>
		/// <param name="filter">valid import filter</param>
		/// <param name="useExistingTable">
		/// if
		/// <code>true</code>
		/// use current table if it already
		/// exists, otherwise, create new table with unique
		/// name
		/// </param>
		/// <returns>the name of the imported table</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public static string ImportReader(BufferedReader @in, Database db, string name, string
			 delim, ImportFilter filter, bool useExistingTable)
		{
			return ImportReader(@in, db, name, delim, ExportUtil.DEFAULT_QUOTE_CHAR, filter, 
				useExistingTable);
		}

		/// <summary>
		/// Copy a delimited text file into a new (or optionally exixsting) table in
		/// this database.
		/// </summary>
		/// <remarks>
		/// Copy a delimited text file into a new (or optionally exixsting) table in
		/// this database.
		/// </remarks>
		/// <param name="name">Name of the new table to create</param>
		/// <param name="in">Source reader to import</param>
		/// <param name="delim">Regular expression representing the delimiter string.</param>
		/// <param name="quote">the quote character</param>
		/// <param name="filter">valid import filter</param>
		/// <param name="useExistingTable">
		/// if
		/// <code>true</code>
		/// use current table if it already
		/// exists, otherwise, create new table with unique
		/// name
		/// </param>
		/// <returns>the name of the imported table</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public static string ImportReader(BufferedReader @in, Database db, string name, string
			 delim, char quote, ImportFilter filter, bool useExistingTable)
		{
			string line = @in.ReadLine();
			if (line == null || line.Trim().Length == 0)
			{
				return null;
			}
			Sharpen.Pattern delimPat = Sharpen.Pattern.Compile(delim);
				name = Database.EscapeIdentifier(name);
				Table table = null;
				if (!useExistingTable || ((table = db.GetTable(name)) == null))
				{
					IList<Column> columns = new List<Column>();
					string[] columnNames = SplitLine(line, delimPat, quote, @in, 0);
					for (int i = 0; i < columnNames.Length; i++)
					{
						columns.AddItem(new ColumnBuilder(columnNames[i], DataType.TEXT).EscapeName().SetLength
							(DataTypeProperties.TEXT.maxSize.Value).ToColumn());
					}
					table = CreateUniqueTable(db, name, columns, null, filter);
				}
				IList<object[]> rows = new AList<object[]>(COPY_TABLE_BATCH_SIZE);
				int numColumns = table.GetColumnCount();
				while ((line = @in.ReadLine()) != null)
				{
					object[] data = SplitLine(line, delimPat, quote, @in, numColumns);
					rows.AddItem(filter.FilterRow(data));
					if (rows.Count == COPY_TABLE_BATCH_SIZE)
					{
						table.AddRows(rows);
						rows.Clear();
					}
				}
				if (rows.Count > 0)
				{
					table.AddRows(rows);
				}
				return table.GetName();
		}

		/// <summary>
		/// Splits the given line using the given delimiter pattern and quote
		/// character.
		/// </summary>
		/// <remarks>
		/// Splits the given line using the given delimiter pattern and quote
		/// character.  May read additional lines for quotes spanning newlines.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private static string[] SplitLine(string line, Sharpen.Pattern delim, char quote, 
			BufferedReader @in, int numColumns)
		{
			IList<string> tokens = new AList<string>();
			StringBuilder sb = new StringBuilder();
			Matcher m = delim.Matcher(line);
			int idx = 0;
			while (idx < line.Length)
			{
				if (line[idx] == quote)
				{
					// find quoted value
					sb.Length = 0;
					++idx;
					while (true)
					{
						int endIdx = line.IndexOf(quote, idx);
						if (endIdx >= 0)
						{
							sb.AppendRange(line, idx, endIdx);
							++endIdx;
							if ((endIdx < line.Length) && (line[endIdx] == quote))
							{
								// embedded quote
								sb.Append(quote);
								// keep searching
								idx = endIdx + 1;
							}
							else
							{
								// done
								idx = endIdx;
								break;
							}
						}
						else
						{
							// line wrap
							sb.AppendRange(line, idx, line.Length);
							sb.Append(LINE_SEPARATOR);
							idx = 0;
							line = @in.ReadLine();
							if (line == null)
							{
								throw new EOFException("Missing end of quoted value " + sb);
							}
						}
					}
					tokens.AddItem(sb.ToString());
					// skip next delim
					idx = (m.Find(idx) ? m.End() : line.Length);
				}
				else
				{
					if (m.Find(idx))
					{
						// next unquoted value
						tokens.AddItem(Sharpen.Runtime.Substring(line, idx, m.Start()));
						idx = m.End();
					}
					else
					{
						// trailing token
						tokens.AddItem(Sharpen.Runtime.Substring(line, idx));
						idx = line.Length;
					}
				}
			}
			return Sharpen.Collections.ToArray(tokens, new string[Math.Max(tokens.Count, numColumns
				)]);
		}

		/// <summary>Returns a new table with a unique name and the given table definition.</summary>
		/// <remarks>Returns a new table with a unique name and the given table definition.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.SQLException"></exception>
		private static Table CreateUniqueTable(Database db, string name, IList<Column> columns
			, ResultSetMetaData md, ImportFilter filter)
		{
			// otherwise, find unique name and create new table
			string baseName = name;
			int counter = 2;
			while (db.GetTable(name) != null)
			{
				name = baseName + (counter++);
			}
			db.CreateTable(name, filter.FilterColumns(columns, md));
			return db.GetTable(name);
		}
	}
}
