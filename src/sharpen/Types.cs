namespace Sharpen
{
    public class Types
    {

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>BIT</code>.
         */
        public const int BIT = -7;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>TINYINT</code>.
         */
        public const int TINYINT = -6;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>SMALLINT</code>.
         */
        public const int SMALLINT = 5;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>INTEGER</code>.
         */
        public const int INTEGER = 4;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>BIGINT</code>.
         */
        public const int BIGINT = -5;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>FLOAT</code>.
         */
        public const int FLOAT = 6;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>REAL</code>.
         */
        public const int REAL = 7;


        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>DOUBLE</code>.
         */
        public const int DOUBLE = 8;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>NUMERIC</code>.
         */
        public const int NUMERIC = 2;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>DECIMAL</code>.
         */
        public const int DECIMAL = 3;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>CHAR</code>.
         */
        public const int CHAR = 1;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>VARCHAR</code>.
         */
        public const int VARCHAR = 12;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>LONGVARCHAR</code>.
         */
        public const int LONGVARCHAR = -1;


        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>DATE</code>.
         */
        public const int DATE = 91;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>TIME</code>.
         */
        public const int TIME = 92;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>TIMESTAMP</code>.
         */
        public const int TIMESTAMP = 93;


        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>BINARY</code>.
         */
        public const int BINARY = -2;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>VARBINARY</code>.
         */
        public const int VARBINARY = -3;

        /**
         * <P>The constant in the Java programming language, sometimes referred
         * to as a type code, that identifies the generic SQL type
         * <code>LONGVARBINARY</code>.
         */
        public const int LONGVARBINARY = -4;

        /**
         * <P>The constant in the Java programming language
         * that identifies the generic SQL value
         * <code>NULL</code>.
         */
        public const int NULL = 0;

        /**
         * The constant in the Java programming language that indicates
         * that the SQL type is database-specific and
         * gets mapped to a Java object that can be accessed via
         * the methods <code>getObject</code> and <code>setObject</code>.
         */
        public const int OTHER = 1111;



        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type
         * <code>JAVA_OBJECT</code>.
         * @since 1.2
         */
        public const int JAVA_OBJECT = 2000;

        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type
         * <code>DISTINCT</code>.
         * @since 1.2
         */
        public const int DISTINCT = 2001;

        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type
         * <code>STRUCT</code>.
         * @since 1.2
         */
        public const int STRUCT = 2002;

        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type
         * <code>ARRAY</code>.
         * @since 1.2
         */
        public const int ARRAY = 2003;

        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type
         * <code>BLOB</code>.
         * @since 1.2
         */
        public const int BLOB = 2004;

        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type
         * <code>CLOB</code>.
         * @since 1.2
         */
        public const int CLOB = 2005;

        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type
         * <code>REF</code>.
         * @since 1.2
         */
        public const int REF = 2006;

        /**
         * The constant in the Java programming language, somtimes referred to
         * as a type code, that identifies the generic SQL type <code>DATALINK</code>.
         *
         * @since 1.4
         */
        public const int DATALINK = 70;

        /**
         * The constant in the Java programming language, somtimes referred to
         * as a type code, that identifies the generic SQL type <code>BOOLEAN</code>.
         *
         * @since 1.4
         */
        public const int BOOLEAN = 16;

        //------------------------- JDBC 4.0 -----------------------------------

        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type <code>ROWID</code>
         *
         * @since 1.6
         *
         */
        public const int ROWID = -8;

        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type <code>NCHAR</code>
         *
         * @since 1.6
         */
        public const int NCHAR = -15;

        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type <code>NVARCHAR</code>.
         *
         * @since 1.6
         */
        public const int NVARCHAR = -9;

        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type <code>LONGNVARCHAR</code>.
         *
         * @since 1.6
         */
        public const int LONGNVARCHAR = -16;

        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type <code>NCLOB</code>.
         *
         * @since 1.6
         */
        public const int NCLOB = 2011;

        /**
         * The constant in the Java programming language, sometimes referred to
         * as a type code, that identifies the generic SQL type <code>XML</code>.
         *
         * @since 1.6
         */
        public const int SQLXML = 2009;
    }
}
