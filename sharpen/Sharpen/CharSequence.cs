namespace Sharpen
{
    public abstract class CharSequence
    {
        public static implicit operator CharSequence(string str)
        {
            return new StringCharSequence(str);
        }

        public static implicit operator CharSequence(System.Text.StringBuilder str)
        {
            return new StringCharSequence(str.ToString());
        }

        public abstract char this[int i] { get; }
        public abstract int Length { get; }
    }

    class StringCharSequence : CharSequence
    {
        string str;

        public StringCharSequence(string str)
        {
            this.str = str;
        }

        public override string ToString()
        {
            return str;
        }

        public override char this[int i] 
        { 
            get
            {
                return this.str[i];
            }
        }

        public override int Length
        {
            get
            {
                return str.Length;
            }
        }
    }
}