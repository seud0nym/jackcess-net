namespace Sharpen
{
	using System;

	public abstract class Reference<T>
	{
		public Reference ()
		{
		}

		public abstract T Get ();
		public abstract void Clear();
	}
}
