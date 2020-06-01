namespace Sharpen
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class LinkedHashMap<T, U> : AbstractMap<T, U>
    {
        private readonly List<KeyValuePair<T, U>> list;
        private readonly Dictionary<T, U> table;

        public LinkedHashMap()
        {
            this.table = new Dictionary<T, U>();
            this.list = new List<KeyValuePair<T, U>>();
        }

        public LinkedHashMap(int initialCapacity)
        {
            this.table = new Dictionary<T, U>(initialCapacity);
            this.list = new List<KeyValuePair<T, U>>(initialCapacity);
        }

        public override void Clear()
        {
            table.Clear();
            list.Clear();
        }

        public override int Count
        {
            get
            {
                return list.Count;
            }
        }

        public override bool ContainsKey(object name)
        {
            return table.ContainsKey((T)name);
        }

        public override ICollection<KeyValuePair<T, U>> EntrySet()
        {
            return this;
        }

        public override U Get(object key)
        {
            table.TryGetValue((T)key, out U local);
            return local;
        }

        protected override IEnumerator<KeyValuePair<T, U>> InternalGetEnumerator()
        {
            return list.GetEnumerator();
        }

        public override bool IsEmpty()
        {
            return (table.Count == 0);
        }

        public override U Put(T key, U value)
        {
            if (table.TryGetValue(key, out U old))
            {
                int index = list.FindIndex(p => p.Key.Equals(key));
                if (index != -1)
                    list.RemoveAt(index);
            }
            table[key] = value;
            list.Add(new KeyValuePair<T, U>(key, value));
            return old;
        }

        public override U Remove(object key)
        {
            if (table.TryGetValue((T)key, out U local))
            {
                int index = list.FindIndex(p => p.Key.Equals(key));
                if (index != -1)
                    list.RemoveAt(index);
                table.Remove((T)key);
            }
            return local;
        }

        public override IEnumerable<T> Keys
        {
            get { return list.Select(p => p.Key); }
        }

        public override IEnumerable<U> Values
        {
            get { return list.Select(p => p.Value); }
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            var c = "";
            s.Append("[");
            foreach (var pair in table)
            {
                s.Append(c).Append(pair.Key.ToString()).Append("={").Append(pair.Value?.ToString()).Append("}");
                c = ",";
            }
            s.Append("]");
            return s.ToString();
        }
    }
}
