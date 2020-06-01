namespace Sharpen
{
    using System;
    using System.Collections.Generic;

    public interface ResultSetMetaData
    {

        /**
         * Returns the number of columns in this <code>ResultSet</code> object.
         *
         * @return the number of columns
         * @exception SQLException if a database access error occurs
         */
        int GetColumnCount();

        /**
         * Indicates whether the designated column is automatically numbered.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return <code>true</code> if so; <code>false</code> otherwise
         * @exception SQLException if a database access error occurs
         */
        bool IsAutoIncrement(int column);

        /**
         * Indicates whether a column's case matters.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return <code>true</code> if so; <code>false</code> otherwise
         * @exception SQLException if a database access error occurs
         */
        bool IsCaseSensitive(int column);

        /**
         * Indicates whether the designated column can be used in a where clause.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return <code>true</code> if so; <code>false</code> otherwise
         * @exception SQLException if a database access error occurs
         */
        bool IsSearchable(int column);

        /**
         * Indicates whether the designated column is a cash value.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return <code>true</code> if so; <code>false</code> otherwise
         * @exception SQLException if a database access error occurs
         */
        bool IsCurrency(int column);

        /**
         * Indicates the nullability of values in the designated column.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return the nullability status of the given column; one of <code>columnNoNulls</code>,
         *          <code>columnNullable</code> or <code>columnNullableUnknown</code>
         * @exception SQLException if a database access error occurs
         */
        int IsNullable(int column);

        /**
         * The constant indicating that a
         * column does not allow <code>NULL</code> values.
         */
        //const int ColumnNoNulls = 0;

        /**
         * The constant indicating that a
         * column allows <code>NULL</code> values.
         */
        //const int ColumnNullable = 1;

        /**
         * The constant indicating that the
         * nullability of a column's values is unknown.
         */
        //const int ColumnNullableUnknown = 2;

        /**
         * Indicates whether values in the designated column are signed numbers.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return <code>true</code> if so; <code>false</code> otherwise
         * @exception SQLException if a database access error occurs
         */
        bool IsSigned(int column);

        /**
         * Indicates the designated column's normal maximum width in characters.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return the normal maximum number of characters allowed as the width
         *          of the designated column
         * @exception SQLException if a database access error occurs
         */
        int GetColumnDisplaySize(int column);

        /**
         * Gets the designated column's suggested title for use in printouts and
         * displays. The suggested title is usually specified by the SQL <code>AS</code>
         * clause.  If a SQL <code>AS</code> is not specified, the value returned from
         * <code>getColumnLabel</code> will be the same as the value returned by the
         * <code>getColumnName</code> method.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return the suggested column title
         * @exception SQLException if a database access error occurs
         */
        string GetColumnLabel(int column);

        /**
         * Get the designated column's name.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return column name
         * @exception SQLException if a database access error occurs
         */
        string GetColumnName(int column);

        /**
         * Get the designated column's table's schema.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return schema name or "" if not applicable
         * @exception SQLException if a database access error occurs
         */
        string GetSchemaName(int column);

        /**
         * Get the designated column's specified column size.
         * For numeric data, this is the maximum precision.  For character data, this is the length in characters.
         * For datetime datatypes, this is the length in characters of the string representation (assuming the
         * maximum allowed precision of the fractional seconds component). For binary data, this is the length in bytes.  For the ROWID datatype,
         * this is the length in bytes. 0 is returned for data types where the
         * column size is not applicable.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return precision
         * @exception SQLException if a database access error occurs
         */
        int GetPrecision(int column);

        /**
         * Gets the designated column's number of digits to right of the decimal point.
         * 0 is returned for data types where the scale is not applicable.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return scale
         * @exception SQLException if a database access error occurs
         */
        int GetScale(int column);

        /**
         * Gets the designated column's table name.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return table name or "" if not applicable
         * @exception SQLException if a database access error occurs
         */
        string GetTableName(int column);

        /**
         * Gets the designated column's table's catalog name.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return the name of the catalog for the table in which the given column
         *          appears or "" if not applicable
         * @exception SQLException if a database access error occurs
         */
        string GetCatalogName(int column);

        /**
         * Retrieves the designated column's SQL type.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return SQL type from java.sql.Types
         * @exception SQLException if a database access error occurs
         * @see Types
         */
        int GetColumnType(int column);

        /**
         * Retrieves the designated column's database-specific type name.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return type name used by the database. If the column type is
         * a user-defined type, then a fully-qualified type name is returned.
         * @exception SQLException if a database access error occurs
         */
        string GetColumnTypeName(int column);

        /**
         * Indicates whether the designated column is definitely not writable.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return <code>true</code> if so; <code>false</code> otherwise
         * @exception SQLException if a database access error occurs
         */
        bool IsReadOnly(int column);

        /**
         * Indicates whether it is possible for a write on the designated column to succeed.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return <code>true</code> if so; <code>false</code> otherwise
         * @exception SQLException if a database access error occurs
         */
        bool IsWritable(int column);

        /**
         * Indicates whether a write on the designated column will definitely succeed.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return <code>true</code> if so; <code>false</code> otherwise
         * @exception SQLException if a database access error occurs
         */
        bool IsDefinitelyWritable(int column);

        //--------------------------JDBC 2.0-----------------------------------

        /**
         * <p>Returns the fully-qualified name of the Java class whose instances
         * are manufactured if the method <code>ResultSet.getObject</code>
         * is called to retrieve a value
         * from the column.  <code>ResultSet.getObject</code> may return a subclass of the
         * class returned by this method.
         *
         * @param column the first column is 1, the second is 2, ...
         * @return the fully-qualified name of the class in the Java programming
         *         language that would be used by the method
         * <code>ResultSet.getObject</code> to retrieve the value in the specified
         * column. This is the class name used for custom mapping.
         * @exception SQLException if a database access error occurs
         * @since 1.2
         */
        string GetColumnClassName(int column);
    }
}
