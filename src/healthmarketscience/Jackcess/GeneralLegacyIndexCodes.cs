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

using Sharpen;
using System;
using System.Collections.Generic;
using System.IO;

namespace HealthMarketScience.Jackcess
{
    /// <summary>
    /// Various constants used for creating "general legacy" (access 2000-2007)
    /// sort order text index entries.
    /// </summary>
    /// <remarks>
    /// Various constants used for creating "general legacy" (access 2000-2007)
    /// sort order text index entries.
    /// </remarks>
    /// <author>James Ahlborn</author>
    public class GeneralLegacyIndexCodes
    {
        internal const int MAX_TEXT_INDEX_CHAR_LENGTH = (JetFormat.TEXT_FIELD_MAX_LENGTH
            / JetFormat.TEXT_FIELD_UNIT_SIZE);

        internal const byte END_TEXT = unchecked((byte)unchecked((int)(0x01)));

        internal const byte END_EXTRA_TEXT = unchecked((byte)unchecked((int)(0x00)));

        internal const int UNPRINTABLE_COUNT_START = 7;

        internal const int UNPRINTABLE_COUNT_MULTIPLIER = 4;

        internal const int UNPRINTABLE_OFFSET_FLAGS = unchecked((int)(0x8000));

        internal const byte UNPRINTABLE_MIDFIX = unchecked((byte)unchecked((int)(0x06)));

        internal const byte INTERNATIONAL_EXTRA_PLACEHOLDER = unchecked((byte)unchecked((
            int)(0x02)));

        internal const byte CRAZY_CODE_START = unchecked((byte)unchecked((int)(0x80)));

        internal const byte CRAZY_CODE_1 = unchecked((byte)unchecked((int)(0x02)));

        internal const byte CRAZY_CODE_2 = unchecked((byte)unchecked((int)(0x03)));

        internal static readonly byte[] CRAZY_CODES_SUFFIX = new byte[] { unchecked((byte
            )unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0x02))), unchecked((byte
            )unchecked((int)(0x80))), unchecked((byte)unchecked((int)(0xFF))), unchecked((byte
            )unchecked((int)(0x80))) };

        internal const byte CRAZY_CODES_UNPRINT_SUFFIX = unchecked((byte)unchecked((int)(
            0xFF)));

        private static readonly string CODES_FILE = Database.RESOURCE_PATH + "index_codes_genleg.txt";

        private static readonly string EXT_CODES_FILE = Database.RESOURCE_PATH + "index_codes_ext_genleg.txt";

        /// <summary>
        /// Enum which classifies the types of char encoding strategies used when
        /// creating text index entries.
        /// </summary>
        /// <remarks>
        /// Enum which classifies the types of char encoding strategies used when
        /// creating text index entries.
        /// </remarks>
        internal enum Type
        {
            SIMPLE,
            INTERNATIONAL,
            UNPRINTABLE,
            UNPRINTABLE_EXT,
            INTERNATIONAL_EXT,
            IGNORED
        }

        /// <summary>
        /// Base class for the handlers which hold the text index character encoding
        /// information.
        /// </summary>
        /// <remarks>
        /// Base class for the handlers which hold the text index character encoding
        /// information.
        /// </remarks>
        internal abstract class CharHandler
        {
            // unprintable char is removed from normal text.
            // pattern for unprintable chars in the extra bytes:
            // 01 01 01 <pos> 06  <code> )
            // <pos> = 7 + (4 * char_pos) | 0x8000 (as short)
            // <code> = char code
            // international char is replaced with ascii char.
            // pattern for international chars in the extra bytes:
            // [ 02 (for each normal char) ] [ <symbol_code> (for each inat char) ]
            // see Index.writeCrazyCodes for details on writing crazy codes
            // stash the codes in some resource files
            public abstract GeneralLegacyIndexCodes.Type GetEncodingType();

            public virtual byte[] GetInlineBytes()
            {
                return null;
            }

            public virtual byte[] GetExtraBytes()
            {
                return null;
            }

            public virtual byte[] GetUnprintableBytes()
            {
                return null;
            }

            public virtual byte GetExtraByteModifier()
            {
                return 0;
            }

            public virtual byte GetCrazyFlag()
            {
                return 0;
            }
        }

        /// <summary>CharHandler for Type.SIMPLE</summary>
        internal sealed class SimpleCharHandler : GeneralLegacyIndexCodes.CharHandler
        {
            private byte[] _bytes;

            internal SimpleCharHandler(byte[] bytes)
            {
                _bytes = bytes;
            }

            public override GeneralLegacyIndexCodes.Type GetEncodingType()
            {
                return GeneralLegacyIndexCodes.Type.SIMPLE;
            }

            public override byte[] GetInlineBytes()
            {
                return _bytes;
            }
        }

        /// <summary>CharHandler for Type.INTERNATIONAL</summary>
        internal sealed class InternationalCharHandler : GeneralLegacyIndexCodes.CharHandler
        {
            private byte[] _bytes;

            private byte[] _extraBytes;

            internal InternationalCharHandler(byte[] bytes, byte[] extraBytes)
            {
                _bytes = bytes;
                _extraBytes = extraBytes;
            }

            public override GeneralLegacyIndexCodes.Type GetEncodingType()
            {
                return GeneralLegacyIndexCodes.Type.INTERNATIONAL;
            }

            public override byte[] GetInlineBytes()
            {
                return _bytes;
            }

            public override byte[] GetExtraBytes()
            {
                return _extraBytes;
            }
        }

        /// <summary>CharHandler for Type.UNPRINTABLE</summary>
        internal sealed class UnprintableCharHandler : GeneralLegacyIndexCodes.CharHandler
        {
            private byte[] _unprintBytes;

            internal UnprintableCharHandler(byte[] unprintBytes)
            {
                _unprintBytes = unprintBytes;
            }

            public override GeneralLegacyIndexCodes.Type GetEncodingType()
            {
                return GeneralLegacyIndexCodes.Type.UNPRINTABLE;
            }

            public override byte[] GetUnprintableBytes()
            {
                return _unprintBytes;
            }
        }

        /// <summary>CharHandler for Type.UNPRINTABLE_EXT</summary>
        internal sealed class UnprintableExtCharHandler : GeneralLegacyIndexCodes.CharHandler
        {
            private byte _extraByteMod;

            internal UnprintableExtCharHandler(byte extraByteMod)
            {
                _extraByteMod = extraByteMod;
            }

            public override GeneralLegacyIndexCodes.Type GetEncodingType()
            {
                return GeneralLegacyIndexCodes.Type.UNPRINTABLE_EXT;
            }

            public override byte GetExtraByteModifier()
            {
                return _extraByteMod;
            }
        }

        /// <summary>CharHandler for Type.INTERNATIONAL_EXT</summary>
        internal sealed class InternationalExtCharHandler : GeneralLegacyIndexCodes.CharHandler
        {
            private byte[] _bytes;

            private byte[] _extraBytes;

            private byte _crazyFlag;

            internal InternationalExtCharHandler(byte[] bytes, byte[] extraBytes, byte crazyFlag
                )
            {
                _bytes = bytes;
                _extraBytes = extraBytes;
                _crazyFlag = crazyFlag;
            }

            public override GeneralLegacyIndexCodes.Type GetEncodingType()
            {
                return GeneralLegacyIndexCodes.Type.INTERNATIONAL_EXT;
            }

            public override byte[] GetInlineBytes()
            {
                return _bytes;
            }

            public override byte[] GetExtraBytes()
            {
                return _extraBytes;
            }

            public override byte GetCrazyFlag()
            {
                return _crazyFlag;
            }
        }

        private sealed class _CharHandler_221 : GeneralLegacyIndexCodes.CharHandler
        {
            public _CharHandler_221()
            {
            }

            public override GeneralLegacyIndexCodes.Type GetEncodingType()
            {
                return GeneralLegacyIndexCodes.Type.IGNORED;
            }
        }

        /// <summary>shared CharHandler instance for Type.IGNORED</summary>
        internal static readonly GeneralLegacyIndexCodes.CharHandler IGNORED_CHAR_HANDLER
             = new _CharHandler_221();

        private sealed class _CharHandler_229 : GeneralLegacyIndexCodes.CharHandler
        {
            public _CharHandler_229()
            {
            }

            public override GeneralLegacyIndexCodes.Type GetEncodingType()
            {
                return GeneralLegacyIndexCodes.Type.IGNORED;
            }

            public override byte[] GetInlineBytes()
            {
                throw new InvalidOperationException("Surrogate pair chars are not handled");
            }
        }

        /// <summary>
        /// alternate shared CharHandler instance for "surrogate" chars (which we do
        /// not handle)
        /// </summary>
        internal static readonly GeneralLegacyIndexCodes.CharHandler SURROGATE_CHAR_HANDLER
             = new _CharHandler_229();

        internal const char FIRST_CHAR = (char)unchecked((int)(0x0000));

        internal const char LAST_CHAR = (char)unchecked((int)(0x00FF));

        internal const char FIRST_EXT_CHAR = (char)(LAST_CHAR + 1);

        internal const char LAST_EXT_CHAR = (char)unchecked((int)(0xFFFF));

        internal sealed class Codes
        {
            /// <summary>handlers for the first 256 chars.</summary>
            /// <remarks>
            /// handlers for the first 256 chars.  use nested class to lazy load the
            /// handlers
            /// </remarks>
            internal static readonly GeneralLegacyIndexCodes.CharHandler[] _values = LoadCodes
                (CODES_FILE, FIRST_CHAR, LAST_CHAR);
        }

        internal sealed class ExtCodes
        {
            /// <summary>handlers for the rest of the chars in BMP 0.</summary>
            /// <remarks>
            /// handlers for the rest of the chars in BMP 0.  use nested class to
            /// lazy load the handlers
            /// </remarks>
            internal static readonly GeneralLegacyIndexCodes.CharHandler[] _values = LoadCodes
                (EXT_CODES_FILE, FIRST_EXT_CHAR, LAST_EXT_CHAR);
        }

        internal static readonly GeneralLegacyIndexCodes GEN_LEG_INSTANCE = new GeneralLegacyIndexCodes
            ();

        public GeneralLegacyIndexCodes()
        {
        }

        /// <summary>Returns the CharHandler for the given character.</summary>
        /// <remarks>Returns the CharHandler for the given character.</remarks>
        internal virtual GeneralLegacyIndexCodes.CharHandler GetCharHandler(char c)
        {
            if (c <= LAST_CHAR)
            {
                return GeneralLegacyIndexCodes.Codes._values[c];
            }
            int extOffset = AsUnsignedChar(c) - AsUnsignedChar(FIRST_EXT_CHAR);
            return GeneralLegacyIndexCodes.ExtCodes._values[extOffset];
        }

        /// <summary>
        /// Loads the CharHandlers for the given range of characters from the
        /// resource file with the given name.
        /// </summary>
        /// <remarks>
        /// Loads the CharHandlers for the given range of characters from the
        /// resource file with the given name.
        /// </remarks>
        internal static GeneralLegacyIndexCodes.CharHandler[] LoadCodes(string codesFilePath
            , char firstChar, char lastChar)
        {
            int numCodes = (AsUnsignedChar(lastChar) - AsUnsignedChar(firstChar)) + 1;
            GeneralLegacyIndexCodes.CharHandler[] values = new GeneralLegacyIndexCodes.CharHandler
                [numCodes];
            IDictionary<string, GeneralLegacyIndexCodes.Type> prefixMap = new Dictionary<string
                , GeneralLegacyIndexCodes.Type>();
            prefixMap.Put("S", GeneralLegacyIndexCodes.Type.SIMPLE);
            prefixMap.Put("I", GeneralLegacyIndexCodes.Type.INTERNATIONAL);
            prefixMap.Put("U", GeneralLegacyIndexCodes.Type.UNPRINTABLE);
            prefixMap.Put("P", GeneralLegacyIndexCodes.Type.UNPRINTABLE_EXT);
            prefixMap.Put("Z", GeneralLegacyIndexCodes.Type.INTERNATIONAL_EXT);
            prefixMap.Put("X", GeneralLegacyIndexCodes.Type.IGNORED);

            InputStreamReader reader = null;
            try
            {
                reader = new InputStreamReader(Database.GetResourceAsStream(codesFilePath
                    ), "US-ASCII");
                int start = AsUnsignedChar(firstChar);
                int end = AsUnsignedChar(lastChar);
                for (int i = start; i <= end; ++i)
                {
                    char c = (char)i;
                    GeneralLegacyIndexCodes.CharHandler ch = null;
                    if (char.IsHighSurrogate(c) || char.IsLowSurrogate(c))
                    {
                        // surrogate chars are not included in the codes files
                        ch = SURROGATE_CHAR_HANDLER;
                    }
                    else
                    {
                        string codeLine = reader.ReadLine();
                        ch = ParseCodes(prefixMap, codeLine);
                    }
                    values[(i - start)] = ch;
                }
            }
            catch (IOException e)
            {
                throw new RuntimeException("failed loading index codes file " + codesFilePath, e);
            }
            finally
            {
                if (reader != null)
                {
                    try
                    {
                        reader.Close();
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            // ignored
            return values;
        }

        /// <summary>
        /// Returns a CharHandler parsed from the given line from an index codes
        /// file.
        /// </summary>
        /// <remarks>
        /// Returns a CharHandler parsed from the given line from an index codes
        /// file.
        /// </remarks>
        private static GeneralLegacyIndexCodes.CharHandler ParseCodes(IDictionary<string,
            GeneralLegacyIndexCodes.Type> prefixMap, string codeLine)
        {
            string prefix = Sharpen.Runtime.Substring(codeLine, 0, 1);
            string suffix = ((codeLine.Length > 1) ? Sharpen.Runtime.Substring(codeLine, 1) :
                string.Empty);
            string[] codeStrings = suffix.Split(",", -1);
            switch (prefixMap.Get(prefix))
            {
                case GeneralLegacyIndexCodes.Type.SIMPLE:
                    {
                        return ParseSimpleCodes(codeStrings);
                    }

                case GeneralLegacyIndexCodes.Type.INTERNATIONAL:
                    {
                        return ParseInternationalCodes(codeStrings);
                    }

                case GeneralLegacyIndexCodes.Type.UNPRINTABLE:
                    {
                        return ParseUnprintableCodes(codeStrings);
                    }

                case GeneralLegacyIndexCodes.Type.UNPRINTABLE_EXT:
                    {
                        return ParseUnprintableExtCodes(codeStrings);
                    }

                case GeneralLegacyIndexCodes.Type.INTERNATIONAL_EXT:
                    {
                        return ParseInternationalExtCodes(codeStrings);
                    }

                default:
                    {
                        // IGNORED
                        return IGNORED_CHAR_HANDLER;
                    }
            }
        }

        /// <summary>Returns a SimpleCharHandler parsed from the given index code strings.</summary>
        /// <remarks>Returns a SimpleCharHandler parsed from the given index code strings.</remarks>
        private static GeneralLegacyIndexCodes.CharHandler ParseSimpleCodes(string[] codeStrings
            )
        {
            if (codeStrings.Length != 1)
            {
                throw new InvalidOperationException("Unexpected code strings " + Arrays.AsList(codeStrings
                    ));
            }
            return new GeneralLegacyIndexCodes.SimpleCharHandler(CodesToBytes(codeStrings[0],
                true));
        }

        /// <summary>
        /// Returns an InternationalCharHandler parsed from the given index code
        /// strings.
        /// </summary>
        /// <remarks>
        /// Returns an InternationalCharHandler parsed from the given index code
        /// strings.
        /// </remarks>
        private static GeneralLegacyIndexCodes.CharHandler ParseInternationalCodes(string
            [] codeStrings)
        {
            if (codeStrings.Length != 2)
            {
                throw new InvalidOperationException("Unexpected code strings " + Arrays.AsList(codeStrings
                    ));
            }
            return new GeneralLegacyIndexCodes.InternationalCharHandler(CodesToBytes(codeStrings
                [0], true), CodesToBytes(codeStrings[1], true));
        }

        /// <summary>
        /// Returns a UnprintableCharHandler parsed from the given index code
        /// strings.
        /// </summary>
        /// <remarks>
        /// Returns a UnprintableCharHandler parsed from the given index code
        /// strings.
        /// </remarks>
        private static GeneralLegacyIndexCodes.CharHandler ParseUnprintableCodes(string[]
             codeStrings)
        {
            if (codeStrings.Length != 1)
            {
                throw new InvalidOperationException("Unexpected code strings " + Arrays.AsList(codeStrings
                    ));
            }
            return new GeneralLegacyIndexCodes.UnprintableCharHandler(CodesToBytes(codeStrings
                [0], true));
        }

        /// <summary>
        /// Returns a UnprintableExtCharHandler parsed from the given index code
        /// strings.
        /// </summary>
        /// <remarks>
        /// Returns a UnprintableExtCharHandler parsed from the given index code
        /// strings.
        /// </remarks>
        private static GeneralLegacyIndexCodes.CharHandler ParseUnprintableExtCodes(string
            [] codeStrings)
        {
            if (codeStrings.Length != 1)
            {
                throw new InvalidOperationException("Unexpected code strings " + Arrays.AsList(codeStrings
                    ));
            }
            byte[] bytes = CodesToBytes(codeStrings[0], true);
            if (bytes.Length != 1)
            {
                throw new InvalidOperationException("Unexpected code strings " + Arrays.AsList(codeStrings
                    ));
            }
            return new GeneralLegacyIndexCodes.UnprintableExtCharHandler(bytes[0]);
        }

        /// <summary>
        /// Returns a InternationalExtCharHandler parsed from the given index code
        /// strings.
        /// </summary>
        /// <remarks>
        /// Returns a InternationalExtCharHandler parsed from the given index code
        /// strings.
        /// </remarks>
        private static GeneralLegacyIndexCodes.CharHandler ParseInternationalExtCodes(string
            [] codeStrings)
        {
            if (codeStrings.Length != 3)
            {
                throw new InvalidOperationException("Unexpected code strings " + Arrays.AsList(codeStrings
                    ));
            }
            byte crazyFlag = ("1".Equals(codeStrings[2]) ? CRAZY_CODE_1 : CRAZY_CODE_2);
            return new GeneralLegacyIndexCodes.InternationalExtCharHandler(CodesToBytes(codeStrings
                [0], true), CodesToBytes(codeStrings[1], false), crazyFlag);
        }

        /// <summary>
        /// Converts a string of hex encoded bytes to a byte[], optionally throwing
        /// an exception if no codes are given.
        /// </summary>
        /// <remarks>
        /// Converts a string of hex encoded bytes to a byte[], optionally throwing
        /// an exception if no codes are given.
        /// </remarks>
        private static byte[] CodesToBytes(string codes, bool required)
        {
            if (codes.Length == 0)
            {
                if (required)
                {
                    throw new InvalidOperationException("empty code bytes");
                }
                return null;
            }
            if ((codes.Length % 2) != 0)
            {
                // stripped a leading 0
                codes = "0" + codes;
            }
            byte[] bytes = new byte[codes.Length / 2];
            for (int i = 0; i < bytes.Length; ++i)
            {
                int charIdx = i * 2;
                bytes[i] = unchecked((byte)(System.Convert.ToInt32(Sharpen.Runtime.Substring(codes
                    , charIdx, charIdx + 2), 16)));
            }
            return bytes;
        }

        /// <summary>Returns an the char value converted to an unsigned char value.</summary>
        /// <remarks>
        /// Returns an the char value converted to an unsigned char value.  Note, I
        /// think this is unnecessary (I think java treats chars as unsigned), but I
        /// did this just to be on the safe side.
        /// </remarks>
        internal static int AsUnsignedChar(char c)
        {
            return c & unchecked((int)(0xFFFF));
        }

        /// <summary>
        /// Converts an index value for a text column into the entry value (which
        /// is based on a variety of nifty codes).
        /// </summary>
        /// <remarks>
        /// Converts an index value for a text column into the entry value (which
        /// is based on a variety of nifty codes).
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        internal virtual void WriteNonNullIndexTextValue(object value, ByteUtil.ByteStream
             bout, bool isAscending)
        {
            // first, convert to string
            string str = Column.ToCharSequence(value).ToString();
            // all text columns (including memos) are only indexed up to the max
            // number of chars in a VARCHAR column
            if (str.Length > MAX_TEXT_INDEX_CHAR_LENGTH)
            {
                str = Sharpen.Runtime.Substring(str, 0, MAX_TEXT_INDEX_CHAR_LENGTH);
            }
            // record pprevious entry length so we can do any post-processing
            // necessary for this entry (handling descending)
            int prevLength = bout.GetLength();
            // now, convert each character to a "code" of one or more bytes
            GeneralLegacyIndexCodes.ExtraCodesStream extraCodes = null;
            ByteUtil.ByteStream unprintableCodes = null;
            ByteUtil.ByteStream crazyCodes = null;
            int charOffset = 0;
            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];
                GeneralLegacyIndexCodes.CharHandler ch = GetCharHandler(c);
                int curCharOffset = charOffset;
                byte[] bytes = ch.GetInlineBytes();
                if (bytes != null)
                {
                    // write the "inline" codes immediately
                    bout.Write(bytes);
                    // only increment the charOffset for chars with inline codes
                    ++charOffset;
                }
                if (ch.GetEncodingType() == GeneralLegacyIndexCodes.Type.SIMPLE)
                {
                    // common case, skip further code handling
                    continue;
                }
                bytes = ch.GetExtraBytes();
                byte extraCodeModifier = ch.GetExtraByteModifier();
                if ((bytes != null) || (extraCodeModifier != 0))
                {
                    if (extraCodes == null)
                    {
                        extraCodes = new GeneralLegacyIndexCodes.ExtraCodesStream(str.Length);
                    }
                    // keep track of the extra codes for later
                    WriteExtraCodes(curCharOffset, bytes, extraCodeModifier, extraCodes);
                }
                bytes = ch.GetUnprintableBytes();
                if (bytes != null)
                {
                    if (unprintableCodes == null)
                    {
                        unprintableCodes = new ByteUtil.ByteStream();
                    }
                    // keep track of the unprintable codes for later
                    WriteUnprintableCodes(curCharOffset, bytes, unprintableCodes, extraCodes);
                }
                byte crazyFlag = ch.GetCrazyFlag();
                if (crazyFlag != 0)
                {
                    if (crazyCodes == null)
                    {
                        crazyCodes = new ByteUtil.ByteStream();
                    }
                    // keep track of the crazy flags for later
                    crazyCodes.Write(crazyFlag);
                }
            }
            // write end text flag
            bout.Write(END_TEXT);
            bool hasExtraCodes = TrimExtraCodes(extraCodes, unchecked((byte)0), INTERNATIONAL_EXTRA_PLACEHOLDER
                );
            bool hasUnprintableCodes = (unprintableCodes != null);
            bool hasCrazyCodes = (crazyCodes != null);
            if (hasExtraCodes || hasUnprintableCodes || hasCrazyCodes)
            {
                // we write all the international extra bytes first
                if (hasExtraCodes)
                {
                    extraCodes.WriteTo(bout);
                }
                if (hasCrazyCodes || hasUnprintableCodes)
                {
                    // write 2 more end flags
                    bout.Write(END_TEXT);
                    bout.Write(END_TEXT);
                    // next come the crazy flags
                    if (hasCrazyCodes)
                    {
                        WriteCrazyCodes(crazyCodes, bout);
                        // if we are writing unprintable codes after this, tack on another
                        // code
                        if (hasUnprintableCodes)
                        {
                            bout.Write(CRAZY_CODES_UNPRINT_SUFFIX);
                        }
                    }
                    // then we write all the unprintable extra bytes
                    if (hasUnprintableCodes)
                    {
                        // write another end flag
                        bout.Write(END_TEXT);
                        unprintableCodes.WriteTo(bout);
                    }
                }
            }
            // handle descending order by inverting the bytes
            if (!isAscending)
            {
                // we actually write the end byte before flipping the bytes, and write
                // another one after flipping
                bout.Write(END_EXTRA_TEXT);
                // flip the bytes that we have written thus far for this text value
                IndexData.FlipBytes(bout.GetBytes(), prevLength, (bout.GetLength() - prevLength));
            }
            // write end extra text
            bout.Write(END_EXTRA_TEXT);
        }

        /// <summary>Encodes the given extra code info in the given stream.</summary>
        /// <remarks>Encodes the given extra code info in the given stream.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private static void WriteExtraCodes(int charOffset, byte[] bytes, byte extraCodeModifier
            , GeneralLegacyIndexCodes.ExtraCodesStream extraCodes)
        {
            // we fill in a placeholder value for any chars w/out extra codes
            int numChars = extraCodes.GetNumChars();
            if (numChars < charOffset)
            {
                int fillChars = charOffset - numChars;
                extraCodes.WriteFill(fillChars, INTERNATIONAL_EXTRA_PLACEHOLDER);
                extraCodes.IncrementNumChars(fillChars);
            }
            if (bytes != null)
            {
                // write the actual extra codes and update the number of chars
                extraCodes.Write(bytes);
                extraCodes.IncrementNumChars(1);
            }
            else
            {
                // extra code modifiers modify the existing extra code bytes and do not
                // count as additional extra code chars
                int lastIdx = extraCodes.GetLength() - 1;
                if (lastIdx >= 0)
                {
                    // the extra code modifier is added to the last extra code written
                    byte lastByte = extraCodes.Get(lastIdx);
                    lastByte += extraCodeModifier;
                    extraCodes.Set(lastIdx, lastByte);
                }
                else
                {
                    // there is no previous extra code, add a new code (but keep track of
                    // this "unprintable code" prefix)
                    extraCodes.Write(extraCodeModifier);
                    extraCodes.SetUnprintablePrefixLen(1);
                }
            }
        }

        /// <summary>
        /// Trims any bytes in the given range off of the end of the given stream,
        /// returning whether or not there are any bytes left in the given stream
        /// after trimming.
        /// </summary>
        /// <remarks>
        /// Trims any bytes in the given range off of the end of the given stream,
        /// returning whether or not there are any bytes left in the given stream
        /// after trimming.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private static bool TrimExtraCodes(ByteUtil.ByteStream extraCodes, byte minTrimCode
            , byte maxTrimCode)
        {
            if (extraCodes == null)
            {
                return false;
            }
            extraCodes.TrimTrailing(minTrimCode, maxTrimCode);
            // anything left?
            return (extraCodes.GetLength() > 0);
        }

        /// <summary>Encodes the given unprintable char codes in the given stream.</summary>
        /// <remarks>Encodes the given unprintable char codes in the given stream.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private static void WriteUnprintableCodes(int charOffset, byte[] bytes, ByteUtil.ByteStream
             unprintableCodes, GeneralLegacyIndexCodes.ExtraCodesStream extraCodes)
        {
            // the offset seems to be calculated based on the number of bytes in the
            // "extra codes" part of the entry (even if there are no extra codes bytes
            // actually written in the final entry).
            int unprintCharOffset = charOffset;
            if (extraCodes != null)
            {
                // we need to account for some extra codes which have not been written
                // yet.  additionally, any unprintable bytes added to the beginning of
                // the extra codes are ignored.
                unprintCharOffset = extraCodes.GetLength() + (charOffset - extraCodes.GetNumChars
                    ()) - extraCodes.GetUnprintablePrefixLen();
            }
            // we write a whacky combo of bytes for each unprintable char which
            // includes a funky offset and extra char itself
            int offset = (UNPRINTABLE_COUNT_START + (UNPRINTABLE_COUNT_MULTIPLIER * unprintCharOffset
                )) | UNPRINTABLE_OFFSET_FLAGS;
            // write offset as big-endian short
            unprintableCodes.Write((offset >> 8) & unchecked((int)(0xFF)));
            unprintableCodes.Write(offset & unchecked((int)(0xFF)));
            unprintableCodes.Write(UNPRINTABLE_MIDFIX);
            unprintableCodes.Write(bytes);
        }

        /// <summary>Encode the given crazy code bytes into the given byte stream.</summary>
        /// <remarks>Encode the given crazy code bytes into the given byte stream.</remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private static void WriteCrazyCodes(ByteUtil.ByteStream crazyCodes, ByteUtil.ByteStream
             bout)
        {
            // CRAZY_CODE_2 flags at the end are ignored, so ditch them
            TrimExtraCodes(crazyCodes, CRAZY_CODE_2, CRAZY_CODE_2);
            if (crazyCodes.GetLength() > 0)
            {
                // the crazy codes get encoded into 6 bit sequences where each code is 2
                // bits (where the first 2 bits in the byte are a common prefix).
                byte curByte = CRAZY_CODE_START;
                int idx = 0;
                for (int i = 0; i < crazyCodes.GetLength(); ++i)
                {
                    byte nextByte = crazyCodes.Get(i);
                    nextByte <<= ((2 - idx) * 2);
                    curByte |= nextByte;
                    ++idx;
                    if (idx == 3)
                    {
                        // write current byte and reset
                        bout.Write(curByte);
                        curByte = CRAZY_CODE_START;
                        idx = 0;
                    }
                }
                // write last byte
                if (idx > 0)
                {
                    bout.Write(curByte);
                }
            }
            // write crazy code suffix (note, we write this even if all the codes are
            // trimmed
            bout.Write(CRAZY_CODES_SUFFIX);
        }

        /// <summary>
        /// Extension of ByteStream which keeps track of an additional char count and
        /// the length of any "unprintable" code prefix.
        /// </summary>
        /// <remarks>
        /// Extension of ByteStream which keeps track of an additional char count and
        /// the length of any "unprintable" code prefix.
        /// </remarks>
        private sealed class ExtraCodesStream : ByteUtil.ByteStream
        {
            private int _numChars;

            private int _unprintablePrefixLen;

            public ExtraCodesStream(int length) : base(length)
            {
            }

            public int GetNumChars()
            {
                return _numChars;
            }

            public void IncrementNumChars(int inc)
            {
                _numChars += inc;
            }

            public int GetUnprintablePrefixLen()
            {
                return _unprintablePrefixLen;
            }

            public void SetUnprintablePrefixLen(int len)
            {
                _unprintablePrefixLen = len;
            }
        }
    }
}
