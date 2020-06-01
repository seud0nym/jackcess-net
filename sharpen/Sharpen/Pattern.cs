namespace Sharpen
{
	using System;
    using System.Collections.Generic;
	using System.Linq;
	using System.Text;
    using System.Text.RegularExpressions;

	public class Pattern
	{
		public const int CASE_INSENSITIVE = 1;
		public const int DOTALL = 2;
		public const int MULTILINE = 4;
		private Regex regex;

		private Pattern (Regex r)
		{
			this.regex = r;
		}

		public static Pattern Compile (string pattern)
		{
			return new Pattern (new Regex (pattern, RegexOptions.Compiled));
		}

		public static Pattern Compile (string pattern, int flags)
		{
			RegexOptions compiled = RegexOptions.Compiled;
			if ((flags & 1) != CASE_INSENSITIVE) {
				compiled |= RegexOptions.IgnoreCase;
			}
			if ((flags & 2) != DOTALL) {
				compiled |= RegexOptions.Singleline;
			}
			if ((flags & 4) != MULTILINE) {
				compiled |= RegexOptions.Multiline;
			}
			return new Pattern (new Regex (pattern, compiled));
		}

		public static String Quote(String s)
		{
			int slashEIndex = s.IndexOf("\\E");
			if (slashEIndex == -1)
				return "\\Q" + s + "\\E";

			StringBuilder sb = new StringBuilder(s.Length * 2);
			sb.Append("\\Q");
			slashEIndex = 0;
			int current = 0;
			while ((slashEIndex = s.IndexOf("\\E", current)) != -1)
			{
				sb.Append(s.Substring(current, slashEIndex));
				current = slashEIndex + 2;
				sb.Append("\\E\\\\E\\Q");
			}
			sb.Append(s.Substring(current, s.Length));
			sb.Append("\\E");
			return sb.ToString();
		}

		public Sharpen.Matcher Matcher (string txt)
		{
			return new Sharpen.Matcher (this.regex, txt);
		}

		public string[] Split(string input)
		{ 
			return Split(input, 0);
		}

		public string[] Split(string input, int limit)
		{
			int index = 0;
			bool matchLimited = limit > 0;
			List<string> matchList = new List<string>();
			Matcher m = Matcher(input);

			// Add segments before each match found
			while (m.Find())
			{
				if (!matchLimited || matchList.Count < limit - 1)
				{
					String match = input.Substring(index, m.Start()).ToString();
					matchList.Add(match);
					index = m.End();
				}
				else if (matchList.Count == limit - 1)
				{ // last one
					String match = input.Substring(index,
													 input.Length).ToString();
					matchList.Add(match);
					index = m.End();
				}
			}

			// If no match was found, return this
			if (index == 0)
				return new String[] { input.ToString() };

			// Add remaining segment
			if (!matchLimited || matchList.Count < limit)
				matchList.Add(input.Substring(index, input.Length).ToString());

			// Construct result
			int resultSize = matchList.Count;
			if (limit == 0)
				while (resultSize > 0 && matchList[resultSize - 1].Equals(""))
					resultSize--;
			return matchList.SubList(0, resultSize).ToArray();
		}
	}
}
