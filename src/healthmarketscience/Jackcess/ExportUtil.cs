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
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <author>Frank Gerbig</author>
	public class ExportUtil
	{
		public static readonly string DEFAULT_DELIMITER = ",";

		public const char DEFAULT_QUOTE_CHAR = '"';

		public static readonly string DEFAULT_FILE_EXT = "csv";

		public ExportUtil()
		{
		}

		/// <summary>
		/// Copy all tables into new delimited text files <br />
		/// Equivalent to:
		/// <code>exportAll(db, dir, "csv");</code>
		/// </summary>
		/// <param name="db">Database the table to export belongs to</param>
		/// <param name="dir">The directory where the new files will be created</param>
		/// <seealso cref="ExportAll(Database, Sharpen.FilePath, string)">ExportAll(Database, Sharpen.FilePath, string)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static void ExportAll(Database db, FilePath dir)
		{
			ExportAll(db, dir, DEFAULT_FILE_EXT);
		}

		/// <summary>
		/// Copy all tables into new delimited text files <br />
		/// Equivalent to:
		/// <code>
		/// exportFile(db, name, f, false, null, '"',
		/// SimpleExportFilter.INSTANCE);
		/// </code>
		/// </summary>
		/// <param name="db">Database the table to export belongs to</param>
		/// <param name="dir">The directory where the new files will be created</param>
		/// <param name="ext">The file extension of the new files</param>
		/// <seealso cref="ExportFile(Database, string, Sharpen.FilePath, bool, string, char, ExportFilter)
		/// 	">ExportFile(Database, string, Sharpen.FilePath, bool, string, char, ExportFilter)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static void ExportAll(Database db, FilePath dir, string ext)
		{
			foreach (string tableName in db.GetTableNames())
			{
				ExportFile(db, tableName, new FilePath(dir, tableName + "." + ext), false, DEFAULT_DELIMITER
					, DEFAULT_QUOTE_CHAR, SimpleExportFilter.INSTANCE);
			}
		}

		/// <summary>
		/// Copy all tables into new delimited text files <br />
		/// Equivalent to:
		/// <code>
		/// exportFile(db, name, f, false, null, '"',
		/// SimpleExportFilter.INSTANCE);
		/// </code>
		/// </summary>
		/// <param name="db">Database the table to export belongs to</param>
		/// <param name="dir">The directory where the new files will be created</param>
		/// <param name="ext">The file extension of the new files</param>
		/// <param name="header">If <code>true</code> the first line contains the column names
		/// 	</param>
		/// <seealso cref="ExportFile(Database, string, Sharpen.FilePath, bool, string, char, ExportFilter)
		/// 	">ExportFile(Database, string, Sharpen.FilePath, bool, string, char, ExportFilter)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static void ExportAll(Database db, FilePath dir, string ext, bool header)
		{
			foreach (string tableName in db.GetTableNames())
			{
				ExportFile(db, tableName, new FilePath(dir, tableName + "." + ext), header, DEFAULT_DELIMITER
					, DEFAULT_QUOTE_CHAR, SimpleExportFilter.INSTANCE);
			}
		}

		/// <summary>
		/// Copy all tables into new delimited text files <br />
		/// Equivalent to:
		/// <code>
		/// exportFile(db, name, f, false, null, '"',
		/// SimpleExportFilter.INSTANCE);
		/// </code>
		/// </summary>
		/// <param name="db">Database the table to export belongs to</param>
		/// <param name="dir">The directory where the new files will be created</param>
		/// <param name="ext">The file extension of the new files</param>
		/// <param name="header">If <code>true</code> the first line contains the column names
		/// 	</param>
		/// <param name="delim">The column delimiter, <code>null</code> for default (comma)</param>
		/// <param name="quote">The quote character</param>
		/// <param name="filter">valid export filter</param>
		/// <seealso cref="ExportFile(Database, string, Sharpen.FilePath, bool, string, char, ExportFilter)
		/// 	">ExportFile(Database, string, Sharpen.FilePath, bool, string, char, ExportFilter)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static void ExportAll(Database db, FilePath dir, string ext, bool header, 
			string delim, char quote, ExportFilter filter)
		{
			foreach (string tableName in db.GetTableNames())
			{
				ExportFile(db, tableName, new FilePath(dir, tableName + "." + ext), header, delim
					, quote, filter);
			}
		}

		/// <summary>
		/// Copy a table into a new delimited text file <br />
		/// Equivalent to:
		/// <code>
		/// exportFile(db, name, f, false, null, '"',
		/// SimpleExportFilter.INSTANCE);
		/// </code>
		/// </summary>
		/// <param name="db">Database the table to export belongs to</param>
		/// <param name="tableName">Name of the table to export</param>
		/// <param name="f">New file to create</param>
		/// <seealso cref="ExportFile(Database, string, Sharpen.FilePath, bool, string, char, ExportFilter)
		/// 	">ExportFile(Database, string, Sharpen.FilePath, bool, string, char, ExportFilter)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static void ExportFile(Database db, string tableName, FilePath f)
		{
			ExportFile(db, tableName, f, false, DEFAULT_DELIMITER, DEFAULT_QUOTE_CHAR, SimpleExportFilter
				.INSTANCE);
		}

		/// <summary>
		/// Copy a table into a new delimited text file <br />
		/// Nearly equivalent to:
		/// <code>
		/// exportWriter(db, name, new BufferedWriter(f),
		/// header, delim, quote, filter);
		/// </code>
		/// </summary>
		/// <param name="db">Database the table to export belongs to</param>
		/// <param name="tableName">Name of the table to export</param>
		/// <param name="f">New file to create</param>
		/// <param name="header">If <code>true</code> the first line contains the column names
		/// 	</param>
		/// <param name="delim">The column delimiter, <code>null</code> for default (comma)</param>
		/// <param name="quote">The quote character</param>
		/// <param name="filter">valid export filter</param>
		/// <seealso cref="ExportWriter(Database, string, System.IO.BufferedWriter, bool, string, char, ExportFilter)
		/// 	">ExportWriter(Database, string, System.IO.BufferedWriter, bool, string, char, ExportFilter)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static void ExportFile(Database db, string tableName, FilePath f, bool header
			, string delim, char quote, ExportFilter filter)
		{
			BufferedWriter @out = null;
			try
			{
				@out = new BufferedWriter(new FileWriter(f));
				ExportWriter(db, tableName, @out, header, delim, quote, filter);
				@out.Close();
			}
			finally
			{
				if (@out != null)
				{
					try
					{
						@out.Close();
					}
					catch (Exception ex)
					{
						System.Console.Error.WriteLine("Could not close file " + f.GetAbsolutePath());
						Sharpen.Runtime.PrintStackTrace(ex, System.Console.Error);
					}
				}
			}
		}

		/// <summary>
		/// Copy a table in this database into a new delimited text file <br />
		/// Equivalent to:
		/// <code>
		/// exportWriter(db, name, out, false, null, '"',
		/// SimpleExportFilter.INSTANCE);
		/// </code>
		/// </summary>
		/// <param name="db">Database the table to export belongs to</param>
		/// <param name="tableName">Name of the table to export</param>
		/// <param name="out">Writer to export to</param>
		/// <seealso cref="ExportWriter(Database, string, System.IO.BufferedWriter, bool, string, char, ExportFilter)
		/// 	">ExportWriter(Database, string, System.IO.BufferedWriter, bool, string, char, ExportFilter)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static void ExportWriter(Database db, string tableName, BufferedWriter @out
			)
		{
			ExportWriter(db, tableName, @out, false, DEFAULT_DELIMITER, DEFAULT_QUOTE_CHAR, SimpleExportFilter
				.INSTANCE);
		}

		/// <summary>Copy a table in this database into a new delimited text file.</summary>
		/// <remarks>
		/// Copy a table in this database into a new delimited text file. <br />
		/// Equivalent to:
		/// <code>exportWriter(Cursor.createCursor(db.getTable(tableName)), out, header, delim, quote, filter);
		/// 	</code>
		/// </remarks>
		/// <param name="db">Database the table to export belongs to</param>
		/// <param name="tableName">Name of the table to export</param>
		/// <param name="out">Writer to export to</param>
		/// <param name="header">If <code>true</code> the first line contains the column names
		/// 	</param>
		/// <param name="delim">The column delimiter, <code>null</code> for default (comma)</param>
		/// <param name="quote">The quote character</param>
		/// <param name="filter">valid export filter</param>
		/// <seealso cref="ExportWriter(Cursor, System.IO.BufferedWriter, bool, string, char, ExportFilter)
		/// 	">ExportWriter(Cursor, System.IO.BufferedWriter, bool, string, char, ExportFilter)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static void ExportWriter(Database db, string tableName, BufferedWriter @out
			, bool header, string delim, char quote, ExportFilter filter)
		{
			ExportWriter(Cursor.CreateCursor(db.GetTable(tableName)), @out, header, delim, quote
				, filter);
		}

		/// <summary>Copy a table in this database into a new delimited text file.</summary>
		/// <remarks>Copy a table in this database into a new delimited text file.</remarks>
		/// <param name="cursor">Cursor to export</param>
		/// <param name="out">Writer to export to</param>
		/// <param name="header">If <code>true</code> the first line contains the column names
		/// 	</param>
		/// <param name="delim">The column delimiter, <code>null</code> for default (comma)</param>
		/// <param name="quote">The quote character</param>
		/// <param name="filter">valid export filter</param>
		/// <exception cref="System.IO.IOException"></exception>
		public static void ExportWriter(Cursor cursor, BufferedWriter @out, bool header, 
			string delim, char quote, ExportFilter filter)
		{
			string delimiter = (delim == null) ? DEFAULT_DELIMITER : delim;
			// create pattern which will indicate whether or not a value needs to be
			// quoted or not (contains delimiter, separator, or newline)
			Sharpen.Pattern needsQuotePattern = Sharpen.Pattern.Compile("(?:" + Sharpen.Pattern
				.Quote(delimiter) + ")|(?:" + Sharpen.Pattern.Quote(string.Empty + quote) + ")|(?:[\n\r])"
				);
			IList<Column> origCols = cursor.GetTable().GetColumns();
			IList<Column> columns = new AList<Column>(origCols);
			columns = filter.FilterColumns(columns);
			ICollection<string> columnNames = null;
			if (!origCols.Equals(columns))
			{
				// columns have been filtered
				columnNames = new HashSet<string>();
				foreach (Column c in columns)
				{
					columnNames.AddItem(c.GetName());
				}
			}
			// print the header row (if desired)
			if (header)
			{
				for (Iterator<Column> iter = columns.Iterator(); iter.HasNext(); )
				{
					WriteValue(@out, iter.Next().GetName(), quote, needsQuotePattern);
					if (iter.HasNext())
					{
						@out.Write(delimiter);
					}
				}
				@out.NewLine();
			}
			// print the data rows
			IDictionary<string, object> row;
			object[] unfilteredRowData = new object[columns.Count];
			while ((row = cursor.GetNextRow(columnNames)) != null)
			{
				// fill raw row data in array
				for (int i = 0; i < columns.Count; i++)
				{
					unfilteredRowData[i] = row.Get(columns[i].GetName());
				}
				// apply filter
				object[] rowData = filter.FilterRow(unfilteredRowData);
				// print row
				for (int i_1 = 0; i_1 < columns.Count; i_1++)
				{
					object obj = rowData[i_1];
					if (obj != null)
					{
						string value = null;
						if (obj is byte[])
						{
							value = ByteUtil.ToHexString((byte[])obj);
						}
						else
						{
							value = rowData[i_1].ToString();
						}
						WriteValue(@out, value, quote, needsQuotePattern);
					}
					if (i_1 < columns.Count - 1)
					{
						@out.Write(delimiter);
					}
				}
				@out.NewLine();
			}
			@out.Flush();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static void WriteValue(BufferedWriter @out, string value, char quote, Sharpen.Pattern
			 needsQuotePattern)
		{
			if (!needsQuotePattern.Matcher(value).Find())
			{
				// no quotes necessary
				@out.Write(value);
				return;
			}
			// wrap the value in quotes and handle internal quotes
			@out.Write(quote);
			for (int i = 0; i < value.Length; ++i)
			{
				char c = value[i];
				if (c == quote)
				{
					@out.Write(quote);
				}
				@out.Write(c);
			}
			@out.Write(quote);
		}
	}
}
