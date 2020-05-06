namespace Sharpen
{
	using System;
	using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    internal class ObjectOutputStream : OutputStream
	{
		private BinaryWriter bw;

		public ObjectOutputStream (OutputStream os)
		{
			this.bw = new BinaryWriter (os.GetWrappedStream ());
		}

		public virtual void WriteInt(int i)
		{
			this.bw.Write(i);
		}

		public virtual void WriteObject(object value)
		{
			var formatter = new BinaryFormatter();
			using (var stream = new MemoryStream())
			{
				formatter.Serialize(stream, value);
				stream.Seek(0, SeekOrigin.Begin);
				this.bw.Write(stream.ToArray());
			}
		}
	}
}
