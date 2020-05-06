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
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Builder style class for constructing a Column.</summary>
	/// <remarks>Builder style class for constructing a Column.</remarks>
	/// <author>James Ahlborn</author>
	public class TableBuilder
	{
		/// <summary>name of the new table</summary>
		private string _name;

		/// <summary>columns for the new table</summary>
		private IList<Column> _columns = new AList<Column>();

		/// <summary>indexes for the new table</summary>
		private IList<IndexBuilder> _indexes = new AList<IndexBuilder>();

		/// <summary>whether or not table/column/index names are automatically escaped</summary>
		private bool _escapeIdentifiers;

		public TableBuilder(string name) : this(name, false)
		{
		}

		public TableBuilder(string name, bool escapeIdentifiers)
		{
			_name = name;
			_escapeIdentifiers = escapeIdentifiers;
			if (_escapeIdentifiers)
			{
				_name = Database.EscapeIdentifier(_name);
			}
		}

		/// <summary>Adds a Column to the new table.</summary>
		/// <remarks>Adds a Column to the new table.</remarks>
		public virtual HealthMarketScience.Jackcess.TableBuilder AddColumn(Column column)
		{
			if (_escapeIdentifiers)
			{
				column.SetName(Database.EscapeIdentifier(column.GetName()));
			}
			_columns.AddItem(column);
			return this;
		}

		/// <summary>Adds a Column to the new table.</summary>
		/// <remarks>Adds a Column to the new table.</remarks>
		public virtual HealthMarketScience.Jackcess.TableBuilder AddColumn(ColumnBuilder 
			columnBuilder)
		{
			return AddColumn(columnBuilder.ToColumn());
		}

		/// <summary>Adds an IndexBuilder to the new table.</summary>
		/// <remarks>Adds an IndexBuilder to the new table.</remarks>
		public virtual HealthMarketScience.Jackcess.TableBuilder AddIndex(IndexBuilder index
			)
		{
			if (_escapeIdentifiers)
			{
				index.SetName(Database.EscapeIdentifier(index.GetName()));
				foreach (IndexBuilder.Column col in index.GetColumns())
				{
					col.SetName(Database.EscapeIdentifier(col.GetName()));
				}
			}
			_indexes.AddItem(index);
			return this;
		}

		/// <summary>
		/// Sets whether or not subsequently added columns will have their names
		/// automatically escaped
		/// </summary>
		public virtual HealthMarketScience.Jackcess.TableBuilder SetEscapeIdentifiers(bool
			 escapeIdentifiers)
		{
			_escapeIdentifiers = escapeIdentifiers;
			return this;
		}

		/// <summary>Sets the names of the primary key columns for this table.</summary>
		/// <remarks>
		/// Sets the names of the primary key columns for this table.  Convenience
		/// method for creating a primary key index on a table.
		/// </remarks>
		public virtual HealthMarketScience.Jackcess.TableBuilder SetPrimaryKey(params string
			[] colNames)
		{
			AddIndex(new IndexBuilder(IndexBuilder.PRIMARY_KEY_NAME).AddColumns(colNames).SetPrimaryKey
				());
			return this;
		}

		/// <summary>
		/// Escapes the new table's name using
		/// <see cref="Database.EscapeIdentifier(string)">Database.EscapeIdentifier(string)</see>
		/// .
		/// </summary>
		public virtual HealthMarketScience.Jackcess.TableBuilder EscapeName()
		{
			_name = Database.EscapeIdentifier(_name);
			return this;
		}

		/// <summary>
		/// Creates a new Table in the given Database with the currently configured
		/// attributes.
		/// </summary>
		/// <remarks>
		/// Creates a new Table in the given Database with the currently configured
		/// attributes.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual Table ToTable(Database db)
		{
			db.CreateTable(_name, _columns, _indexes);
			return db.GetTable(_name);
		}
	}
}
