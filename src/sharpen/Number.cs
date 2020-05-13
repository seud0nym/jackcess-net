namespace Sharpen
{
    using System;
    using System.ComponentModel;
    using System.Data.SqlTypes;
    using System.Numerics;

    public abstract class Number
    {
        /**
         * Returns the value of the specified number as an <code>int</code>.
         * This may involve rounding or truncation.
         *
         * @return  the numeric value represented by this object after conversion
         *          to type <code>int</code>.
         */
        public abstract int IntValue();

        /**
         * Returns the value of the specified number as a <code>long</code>.
         * This may involve rounding or truncation.
         *
         * @return  the numeric value represented by this object after conversion
         *          to type <code>long</code>.
         */
        public abstract long LongValue();

        /**
         * Returns the value of the specified number as a <code>float</code>.
         * This may involve rounding.
         *
         * @return  the numeric value represented by this object after conversion
         *          to type <code>float</code>.
         */
        public abstract float FloatValue();

        /**
         * Returns the value of the specified number as a <code>double</code>.
         * This may involve rounding.
         *
         * @return  the numeric value represented by this object after conversion
         *          to type <code>double</code>.
         */
        public abstract double DoubleValue();

        /**
         * Returns the value of the specified number as a <code>byte</code>.
         * This may involve rounding or truncation.
         *
         * @return  the numeric value represented by this object after conversion
         *          to type <code>byte</code>.
         * @since   JDK1.1
         */
        public virtual byte ByteValue()
        {
            return (byte)IntValue();
        }

        /**
         * Returns the value of the specified number as a <code>short</code>.
         * This may involve rounding or truncation.
         *
         * @return  the numeric value represented by this object after conversion
         *          to type <code>short</code>.
         * @since   JDK1.1
         */
        public virtual short ShortValue()
        {
            return (short)IntValue();
        }

        public override string ToString()
        {
            return Convert.ToString(this.DoubleValue());
        }
    }

    public sealed class BigDecimal : Number, IComparable<BigDecimal>
    {
        private readonly SqlDecimal value;

        public static readonly BigDecimal ONE = new BigDecimal(BigInteger.One);
        public static readonly BigDecimal ZERO = new BigDecimal(BigInteger.Zero);

        public BigDecimal(BigInteger value)
        {
            this.value = new SqlDecimal(Convert.ToDecimal(value.ToString()));
        }

        public BigDecimal(BigInteger value, int scale)
        {
            this.value = new SqlDecimal(Convert.ToDecimal(value.ToString()) * (decimal)Math.Pow(10, -scale));
        }

        public BigDecimal(decimal value)
        {
            this.value = new SqlDecimal(value);
        }

        public BigDecimal(double value)
        {
            this.value = new SqlDecimal(Convert.ToDecimal(value));
        }

        public BigDecimal(String s)
        {
            this.value = new SqlDecimal(Convert.ToDecimal(s));
        }

        public BigDecimal Abs()
        {
            return (Signum() < 0 ? Negate() : this);
        }

        public int CompareTo(BigDecimal other)
        {
            return this.value.CompareTo(other.value);
        }

        public BigDecimal Negate()
        {
            return new BigDecimal(Decimal.Negate(this.value.Value));
        }

        public int Precision()
        {
            return this.value.Precision;
        }

        public long LongValueExact()
        {
            if (this.value.Scale == 0)
                return this.LongValue();
            if ((this.value.Precision - this.value.Scale) > 19) // [OK for negative scale too]
                throw new ArithmeticException("Overflow");
            if (this.value.Value == 0)
                return 0;
            if ((this.value.Precision - this.value.Scale) <= 0)
                throw new ArithmeticException("Rounding necessary");
            return this.SetScale(0).LongValue();
        }

        public BigDecimal MovePointRight(int n)
        {
            return new BigDecimal(this.UnscaledValue(), this.value.Scale - n);
        }

        public BigDecimal SetScale(int newScale)
        {
            if ((this.value.Precision - this.value.Scale) <= 0)
                throw new ArithmeticException("Rounding necessary");
            return new BigDecimal(this.UnscaledValue(), newScale);
        }

        public int Signum()
        {
            return this.value.IsPositive ? 1 : this.value == 0 ? 0 : -1;
        }

        public BigInteger UnscaledValue()
        {
            return new BigInteger(this.value.Value * (decimal)Math.Pow(10, this.value.Scale));
        }

        public override byte ByteValue()
        {
            return value.ToSqlByte().Value;
        }

        public override short ShortValue()
        {
            return value.ToSqlInt16().Value;
        }

        public override int IntValue()
        {
            return value.ToSqlInt32().Value;
        }

        public override long LongValue()
        {
            return value.ToSqlInt64().Value;
        }

        public override float FloatValue()
        {
            return value.ToSqlSingle().Value;
        }

        public override double DoubleValue()
        {
            return value.ToSqlDouble().Value;
        }

        public static implicit operator decimal(BigDecimal d) => d == null ? (decimal)0 : d.value.Value;
        public static implicit operator decimal?(BigDecimal d) => d == null ? (decimal?)null : d.value.Value;
    }

    public sealed class Byte : Number
    {
        private readonly byte value;

        public Byte(byte value)
        {
            this.value = value;
        }

        public Byte(String s)
        {
            this.value = Convert.ToByte(s, 10);
        }

        public override byte ByteValue()
        {
            return value;
        }

        public override short ShortValue()
        {
            return (short)value;
        }

        public override int IntValue()
        {
            return (int)value;
        }

        public override long LongValue()
        {
            return (long)value;
        }

        public override float FloatValue()
        {
            return (float)value;
        }

        public override double DoubleValue()
        {
            return (double)value;
        }
    }

    public sealed class Double : Number
    {
        private readonly double value;

        public Double(double value)
        {
            this.value = value;
        }

        public Double(String s)
        {
            this.value = (Convert.ToDouble(s));
        }

        public override byte ByteValue()
        {
            return (byte)value;
        }

        public override short ShortValue()
        {
            return (short)value;
        }

        public override int IntValue()
        {
            return (int)value;
        }

        public override long LongValue()
        {
            return (long)value;
        }

        public override float FloatValue()
        {
            return (float)value;
        }

        public override double DoubleValue()
        {
            return value;
        }
    }

    public sealed class Float : Number
    {
        private readonly float value;

        public Float(float value)
        {
            this.value = value;
        }

        public Float(String s)
        {
            this.value = (Convert.ToSingle(s));
        }

        public override byte ByteValue()
        {
            return (byte)value;
        }

        public override short ShortValue()
        {
            return (short)value;
        }

        public override int IntValue()
        {
            return (int)value;
        }

        public override long LongValue()
        {
            return (long)value;
        }

        public override float FloatValue()
        {
            return value;
        }

        public override double DoubleValue()
        {
            return (double)value;
        }
    }

    public sealed class Integer : Number
    {
        private readonly int value;

        public Integer(int value)
        {
            this.value = value;
        }

        public Integer(String s)
        {
            this.value = (Convert.ToInt32(s));
        }

        public override byte ByteValue()
        {
            return (byte)value;
        }

        public override short ShortValue()
        {
            return (short)value;
        }

        public override int IntValue()
        {
            return value;
        }

        public override long LongValue()
        {
            return (long)value;
        }

        public override float FloatValue()
        {
            return (float)value;
        }

        public override double DoubleValue()
        {
            return (double)value;
        }
    }

    public sealed class Long : Number
    {
        private readonly long value;

        public Long(long value)
        {
            this.value = value;
        }

        public Long(String s)
        {
            this.value = (Convert.ToInt64(s));
        }

        public override byte ByteValue()
        {
            return (byte)value;
        }

        public override short ShortValue()
        {
            return (short)value;
        }

        public override int IntValue()
        {
            return (int)value;
        }

        public override long LongValue()
        {
            return value;
        }

        public override float FloatValue()
        {
            return (float)value;
        }

        public override double DoubleValue()
        {
            return (double)value;
        }
    }

    public sealed class Short : Number
    {
        private readonly short value;

        public Short(short value)
        {
            this.value = value;
        }

        public Short(String s)
        {
            this.value = (Convert.ToInt16(s));
        }

        public override byte ByteValue()
        {
            return (byte)value;
        }

        public override short ShortValue()
        {
            return value;
        }

        public override int IntValue()
        {
            return (int)value;
        }

        public override long LongValue()
        {
            return (long)value;
        }

        public override float FloatValue()
        {
            return (float)value;
        }

        public override double DoubleValue()
        {
            return (double)value;
        }
    }
}
