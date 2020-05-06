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
using System.Text;
using HealthMarketScience.Jackcess.Scsu;
using Sharpen;

namespace HealthMarketScience.Jackcess.Scsu
{
	/// <summary>A number of helpful output routines for debugging.</summary>
	/// <remarks>
	/// A number of helpful output routines for debugging. Output can be centrally
	/// enabled or disabled by calling Debug.set(true/false); All methods are
	/// statics;
	/// </remarks>
	public class Debug
	{
		private static bool ENABLED = false;

		public static bool IsDebugEnabled()
		{
			return ENABLED;
		}

		// debugging helper
		public static void Out(char[] chars)
		{
			Out(chars, 0);
		}

		public static void Out(char[] chars, int iStart)
		{
			if (!ENABLED)
			{
				return;
			}
			StringBuilder msg = new StringBuilder();
			for (int i = iStart; i < chars.Length; i++)
			{
				if (chars[i] >= 0 && chars[i] <= 26)
				{
					msg.Append("^" + (char)(chars[i] + unchecked((int)(0x40))));
				}
				else
				{
					if (chars[i] <= 255)
					{
						msg.Append(chars[i]);
					}
					else
					{
						msg.Append("\\u" + Sharpen.Extensions.ToString(chars[i], 16));
					}
				}
			}
			System.Console.Out.WriteLine(msg.ToString());
		}

		public static void Out(byte[] bytes)
		{
			Out(bytes, 0);
		}

		public static void Out(byte[] bytes, int iStart)
		{
			if (!ENABLED)
			{
				return;
			}
			StringBuilder msg = new StringBuilder();
			for (int i = iStart; i < bytes.Length; i++)
			{
				msg.Append(bytes[i] + ",");
			}
			System.Console.Out.WriteLine(msg.ToString());
		}

		public static void Out(string str)
		{
			if (!ENABLED)
			{
				return;
			}
			System.Console.Out.WriteLine(str);
		}

		public static void Out(string msg, int iData)
		{
			if (!ENABLED)
			{
				return;
			}
			System.Console.Out.WriteLine(msg + iData);
		}

		public static void Out(string msg, char ch)
		{
			if (!ENABLED)
			{
				return;
			}
			System.Console.Out.WriteLine(msg + "[U+" + Sharpen.Extensions.ToString(ch, 16) + 
				"]" + ch);
		}

		public static void Out(string msg, byte bData)
		{
			if (!ENABLED)
			{
				return;
			}
			System.Console.Out.WriteLine(msg + bData);
		}

		public static void Out(string msg, string str)
		{
			if (!ENABLED)
			{
				return;
			}
			System.Console.Out.WriteLine(msg + str);
		}

		public static void Out(string msg, char[] data)
		{
			if (!ENABLED)
			{
				return;
			}
			System.Console.Out.WriteLine(msg);
			Out(data);
		}

		public static void Out(string msg, byte[] data)
		{
			if (!ENABLED)
			{
				return;
			}
			System.Console.Out.WriteLine(msg);
			Out(data);
		}

		public static void Out(string msg, char[] data, int iStart)
		{
			if (!ENABLED)
			{
				return;
			}
			System.Console.Out.WriteLine(msg + "(" + iStart + "): ");
			Out(data, iStart);
		}

		public static void Out(string msg, byte[] data, int iStart)
		{
			if (!ENABLED)
			{
				return;
			}
			System.Console.Out.WriteLine(msg + "(" + iStart + "): ");
			Out(data, iStart);
		}

		public static void Set(bool on)
		{
			ENABLED = on;
		}

		public static void Out(string msg, Exception error)
		{
			if (!ENABLED)
			{
				return;
			}
			System.Console.Out.WriteLine(msg);
			Sharpen.Runtime.PrintStackTrace(error, System.Console.Out);
		}
	}
}
