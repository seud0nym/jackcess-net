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

using HealthMarketScience.Jackcess.Scsu;
using Sharpen;

namespace HealthMarketScience.Jackcess.Scsu
{
	/// <summary>
	/// Encoding text data in Unicode often requires more storage than using
	/// an existing 8-bit character set and limited to the subset of characters
	/// actually found in the text.
	/// </summary>
	/// <remarks>
	/// Encoding text data in Unicode often requires more storage than using
	/// an existing 8-bit character set and limited to the subset of characters
	/// actually found in the text. The Unicode Compression Algorithm reduces
	/// the necessary storage while retaining the universality of Unicode.
	/// A full description of the algorithm can be found in document
	/// http://www.unicode.org/unicode/reports/tr6.html
	/// Summary
	/// The goal of the Unicode Compression Algorithm is the abilty to
	/// Express all code points in Unicode
	/// Approximate storage size for traditional character sets
	/// Work well for short strings
	/// Provide transparency for Latin-1 data
	/// Support very simple decoders
	/// Support simple as well as sophisticated encoders
	/// If needed, further compression can be achieved by layering standard
	/// file or disk-block based compression algorithms on top.
	/// <H2>Features</H2>
	/// Languages using small alphabets would contain runs of characters that
	/// are coded close together in Unicode. These runs are interrupted only
	/// by punctuation characters, which are themselves coded in proximity to
	/// each other in Unicode (usually in the ASCII range).
	/// Two basic mechanisms in the compression algorithm account for these two
	/// cases, sliding windows and static windows. A window is an area of 128
	/// consecutive characters in Unicode. In the compressed data stream, each
	/// character from a sliding window would be represented as a byte between
	/// 0x80 and 0xFF, while a byte from 0x20 to 0x7F (as well as CR, LF, and
	/// TAB) would always mean an ASCII character (or control).
	/// <H2>Notes on the Java implementation</H2>
	/// A limitation of Java is the exclusive use of a signed byte data type.
	/// The following work arounds are required:
	/// Copying a byte to an integer variable and adding 256 for 'negative'
	/// bytes gives an integer in the range 0-255.
	/// Values of char are between 0x0000 and 0xFFFF in Java. Arithmetic on
	/// char values is unsigned.
	/// Extended characters require an int to store them. The sign is not an
	/// issue because only 1024*1024 + 65536 extended characters exist.
	/// </remarks>
	public abstract class SCSU
	{
		/// <summary>SQ<i>n</i> Quote from Window .</summary>
		/// <remarks>
		/// SQ<i>n</i> Quote from Window . <p>
		/// If the following byte is less than 0x80, quote from
		/// static window <i>n</i>, else quote from dynamic window <i>n</i>.
		/// </remarks>
		internal const byte SQ0 = unchecked((int)(0x01));

		internal const byte SQ1 = unchecked((int)(0x02));

		internal const byte SQ2 = unchecked((int)(0x03));

		internal const byte SQ3 = unchecked((int)(0x04));

		internal const byte SQ4 = unchecked((int)(0x05));

		internal const byte SQ5 = unchecked((int)(0x06));

		internal const byte SQ6 = unchecked((int)(0x07));

		internal const byte SQ7 = unchecked((int)(0x08));

		internal const byte SDX = unchecked((int)(0x0B));

		internal const byte Srs = unchecked((int)(0x0C));

		internal const byte SQU = unchecked((int)(0x0E));

		internal const byte SCU = unchecked((int)(0x0F));

		/// <summary>SC<i>n</i> Change to Window <i>n</i>.</summary>
		/// <remarks>
		/// SC<i>n</i> Change to Window <i>n</i>. <p>
		/// If the following bytes are less than 0x80, interpret them
		/// as command bytes or pass them through, else add the offset
		/// for dynamic window <i>n</i>.
		/// </remarks>
		internal const byte SC0 = unchecked((int)(0x10));

		internal const byte SC1 = unchecked((int)(0x11));

		internal const byte SC2 = unchecked((int)(0x12));

		internal const byte SC3 = unchecked((int)(0x13));

		internal const byte SC4 = unchecked((int)(0x14));

		internal const byte SC5 = unchecked((int)(0x15));

		internal const byte SC6 = unchecked((int)(0x16));

		internal const byte SC7 = unchecked((int)(0x17));

		internal const byte SD0 = unchecked((int)(0x18));

		internal const byte SD1 = unchecked((int)(0x19));

		internal const byte SD2 = unchecked((int)(0x1A));

		internal const byte SD3 = unchecked((int)(0x1B));

		internal const byte SD4 = unchecked((int)(0x1C));

		internal const byte SD5 = unchecked((int)(0x1D));

		internal const byte SD6 = unchecked((int)(0x1E));

		internal const byte SD7 = unchecked((int)(0x1F));

		internal const byte UC0 = unchecked((byte)unchecked((int)(0xE0)));

		internal const byte UC1 = unchecked((byte)unchecked((int)(0xE1)));

		internal const byte UC2 = unchecked((byte)unchecked((int)(0xE2)));

		internal const byte UC3 = unchecked((byte)unchecked((int)(0xE3)));

		internal const byte UC4 = unchecked((byte)unchecked((int)(0xE4)));

		internal const byte UC5 = unchecked((byte)unchecked((int)(0xE5)));

		internal const byte UC6 = unchecked((byte)unchecked((int)(0xE6)));

		internal const byte UC7 = unchecked((byte)unchecked((int)(0xE7)));

		internal const byte UD0 = unchecked((byte)unchecked((int)(0xE8)));

		internal const byte UD1 = unchecked((byte)unchecked((int)(0xE9)));

		internal const byte UD2 = unchecked((byte)unchecked((int)(0xEA)));

		internal const byte UD3 = unchecked((byte)unchecked((int)(0xEB)));

		internal const byte UD4 = unchecked((byte)unchecked((int)(0xEC)));

		internal const byte UD5 = unchecked((byte)unchecked((int)(0xED)));

		internal const byte UD6 = unchecked((byte)unchecked((int)(0xEE)));

		internal const byte UD7 = unchecked((byte)unchecked((int)(0xEF)));

		internal const byte UQU = unchecked((byte)unchecked((int)(0xF0)));

		internal const byte UDX = unchecked((byte)unchecked((int)(0xF1)));

		internal const byte Urs = unchecked((byte)unchecked((int)(0xF2)));

		/// <summary>constant offsets for the 8 static windows</summary>
		internal static readonly int[] staticOffset = new int[] { unchecked((int)(0x0000)), 
			unchecked((int)(0x0080)), unchecked((int)(0x0100)), unchecked((int)(0x0300)), unchecked(
			(int)(0x2000)), unchecked((int)(0x2080)), unchecked((int)(0x2100)), unchecked((int
			)(0x3000)) };

		/// <summary>initial offsets for the 8 dynamic (sliding) windows</summary>
		internal static readonly int[] initialDynamicOffset = new int[] { unchecked((int)(0x0080
			)), unchecked((int)(0x00C0)), unchecked((int)(0x0400)), unchecked((int)(0x0600))
			, unchecked((int)(0x0900)), unchecked((int)(0x3040)), unchecked((int)(0x30A0)), 
			unchecked((int)(0xFF00)) };

		/// <summary>dynamic window offsets, intitialize to default values.</summary>
		/// <remarks>dynamic window offsets, intitialize to default values.</remarks>
		internal int[] dynamicOffset = new int[] { initialDynamicOffset[0], initialDynamicOffset
			[1], initialDynamicOffset[2], initialDynamicOffset[3], initialDynamicOffset[4], 
			initialDynamicOffset[5], initialDynamicOffset[6], initialDynamicOffset[7] };

		private int iWindow = 0;

		// Quote from window pair 0
		// Quote from window pair 1
		// Quote from window pair 2
		// Quote from window pair 3
		// Quote from window pair 4
		// Quote from window pair 5
		// Quote from window pair 6
		// Quote from window pair 7
		// Define a window as extended
		// reserved
		// Quote a single Unicode character
		// Change to Unicode mode
		// Select window 0
		// Select window 1
		// Select window 2
		// Select window 3
		// Select window 4
		// Select window 5
		// Select window 6
		// Select window 7
		// Define and select window 0
		// Define and select window 1
		// Define and select window 2
		// Define and select window 3
		// Define and select window 4
		// Define and select window 5
		// Define and select window 6
		// Define and select window 7
		// Select window 0
		// Select window 1
		// Select window 2
		// Select window 3
		// Select window 4
		// Select window 5
		// Select window 6
		// Select window 7
		// Define and select window 0
		// Define and select window 1
		// Define and select window 2
		// Define and select window 3
		// Define and select window 4
		// Define and select window 5
		// Define and select window 6
		// Define and select window 7
		// Quote a single Unicode character
		// Define a Window as extended
		// reserved
		// ASCII for quoted tags
		// Latin - 1 Supplement (for access to punctuation)
		// Latin Extended-A
		// Combining Diacritical Marks
		// General Punctuation
		// Currency Symbols
		// Letterlike Symbols and Number Forms
		// CJK Symbols and punctuation
		// Latin-1
		// Latin Extended A   //@005 fixed from 0x0100
		// Cyrillic
		// Arabic
		// Devanagari
		// Hiragana
		// Katakana
		// Fullwidth ASCII
		// The following method is common to encoder and decoder
		// current active window
		/// <summary>select the active dynamic window</summary>
		protected internal virtual void SelectWindow(int iWindow)
		{
			this.iWindow = iWindow;
		}

		/// <summary>select the active dynamic window</summary>
		protected internal virtual int GetCurrentWindow()
		{
			return this.iWindow;
		}

		/// <summary>
		/// Unicode code points from 3400 to E000 are not adressible by
		/// dynamic window, since in these areas no short run alphabets are
		/// found.
		/// </summary>
		/// <remarks>
		/// Unicode code points from 3400 to E000 are not adressible by
		/// dynamic window, since in these areas no short run alphabets are
		/// found. Therefore add gapOffset to all values from gapThreshold
		/// </remarks>
		internal const int gapThreshold = unchecked((int)(0x68));

		internal const int gapOffset = unchecked((int)(0xAC00));

		internal const int reservedStart = unchecked((int)(0xA8));

		internal const int fixedThreshold = unchecked((int)(0xF9));

		/// <summary>Table of fixed predefined Offsets, and byte values that index into</summary>
		internal static readonly int[] fixedOffset = new int[] { unchecked((int)(0x00C0)), 
			unchecked((int)(0x0250)), unchecked((int)(0x0370)), unchecked((int)(0x0530)), unchecked(
			(int)(0x3040)), unchecked((int)(0x30A0)), unchecked((int)(0xFF60)) };

		// Latin-1 Letters + half of Latin Extended A
		// IPA extensions
		// Greek
		// Armenian
		// Hiragana
		// Katakana
		// Halfwidth Katakana
		/// <summary>whether a character is compressible</summary>
		public static bool IsCompressible(char ch)
		{
			return (ch < unchecked((int)(0x3400)) || ch >= unchecked((int)(0xE000)));
		}

		/// <summary>
		/// reset is only needed to bail out after an exception and
		/// restart with new input
		/// </summary>
		public virtual void Reset()
		{
			// reset the dynamic windows
			for (int i = 0; i < dynamicOffset.Length; i++)
			{
				dynamicOffset[i] = initialDynamicOffset[i];
			}
			this.iWindow = 0;
		}
	}
}
