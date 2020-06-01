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
using System.Collections.Generic;

namespace HealthMarketScience.Jackcess
{
    /// <summary>
    /// Interface which allows customization of the behavior of the
    /// <code>Database<&lt;/code> import/copy methods.
    /// </summary>
    /// <remarks>
    /// Interface which allows customization of the behavior of the
    /// <code>Database<&lt;/code> import/copy methods.
    /// </remarks>
    /// <author>James Ahlborn</author>
    public interface ImportFilter
    {
        /// <summary>The columns that should be used to create the imported table.</summary>
        /// <remarks>The columns that should be used to create the imported table.</remarks>
        /// <param name="destColumns">
        /// the columns as determined by the import code, may be
        /// directly modified and returned
        /// </param>
        /// <param name="srcColumns">
        /// the sql metadata, only available if importing from a
        /// JDBC source
        /// </param>
        /// <returns>the columns to use when creating the import table</returns>
        /// <exception cref="Sharpen.SQLException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        IList<Column> FilterColumns(IList<Column> destColumns, ResultSetMetaData srcColumns
            );

        /// <summary>The desired values for the row.</summary>
        /// <remarks>The desired values for the row.</remarks>
        /// <param name="row">
        /// the row data as determined by the import code, may be directly
        /// modified
        /// </param>
        /// <returns>the row data as it should be written to the import table</returns>
        /// <exception cref="Sharpen.SQLException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        object[] FilterRow(object[] row);
    }
}
