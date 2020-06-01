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

using System.Text;

namespace HealthMarketScience.Jackcess.Scsu
{
    /// <summary>
    /// Reference decoder for the Standard Compression Scheme for Unicode (SCSU)
    /// <H2>Notes on the Java implementation</H2>
    /// A limitation of Java is the exclusive use of a signed byte data type.
    /// </summary>
    /// <remarks>
    /// Reference decoder for the Standard Compression Scheme for Unicode (SCSU)
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
    public class Expand : SCSU
    {
        /// <summary>
        /// (re-)define (and select) a dynamic window
        /// A sliding window position cannot start at any Unicode value,
        /// so rather than providing an absolute offset, this function takes
        /// an index value which selects among the possible starting values.
        /// </summary>
        /// <remarks>
        /// (re-)define (and select) a dynamic window
        /// A sliding window position cannot start at any Unicode value,
        /// so rather than providing an absolute offset, this function takes
        /// an index value which selects among the possible starting values.
        /// Most scripts in Unicode start on or near a half-block boundary
        /// so the default behaviour is to multiply the index by 0x80. Han,
        /// Hangul, Surrogates and other scripts between 0x3400 and 0xDFFF
        /// show very poor locality--therefore no sliding window can be set
        /// there. A jumpOffset is added to the index value to skip that region,
        /// and only 167 index values total are required to select all eligible
        /// half-blocks.
        /// Finally, a few scripts straddle half block boundaries. For them, a
        /// table of fixed offsets is used, and the index values from 0xF9 to
        /// 0xFF are used to select these special offsets.
        /// After (re-)defining a windows location it is selected so it is ready
        /// for use.
        /// Recall that all Windows are of the same length (128 code positions).
        /// </remarks>
        /// <param name="iWindow">- index of the window to be (re-)defined</param>
        /// <param name="bOffset">- index for the new offset value</param>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.IllegalInputException"></exception>
        protected internal virtual void DefineWindow(int iWindow, byte bOffset)
        {
            // @005 protected <-- private here and elsewhere
            int iOffset = (((sbyte)bOffset) < 0 ? bOffset + 256 : bOffset);
            // 0 is a reserved value
            if (iOffset == 0)
            {
                throw new IllegalInputException();
            }
            else
            {
                if (iOffset < gapThreshold)
                {
                    dynamicOffset[iWindow] = iOffset << 7;
                }
                else
                {
                    if (iOffset < reservedStart)
                    {
                        dynamicOffset[iWindow] = (iOffset << 7) + gapOffset;
                    }
                    else
                    {
                        if (iOffset < fixedThreshold)
                        {
                            // more reserved values
                            throw new IllegalInputException("iOffset == " + iOffset);
                        }
                        else
                        {
                            dynamicOffset[iWindow] = fixedOffset[iOffset - fixedThreshold];
                        }
                    }
                }
            }
            // make the redefined window the active one
            SelectWindow(iWindow);
        }

        /// <summary>
        /// (re-)define (and select) a window as an extended dynamic window
        /// The surrogate area in Unicode allows access to 2**20 codes beyond the
        /// first 64K codes by combining one of 1024 characters from the High
        /// Surrogate Area with one of 1024 characters from the Low Surrogate
        /// Area (see Unicode 2.0 for the details).
        /// </summary>
        /// <remarks>
        /// (re-)define (and select) a window as an extended dynamic window
        /// The surrogate area in Unicode allows access to 2**20 codes beyond the
        /// first 64K codes by combining one of 1024 characters from the High
        /// Surrogate Area with one of 1024 characters from the Low Surrogate
        /// Area (see Unicode 2.0 for the details).
        /// The tags SDX and UDX set the window such that each subsequent byte in
        /// the range 80 to FF represents a surrogate pair. The following diagram
        /// shows how the bits in the two bytes following the SDX or UDX, and a
        /// subsequent data byte, map onto the bits in the resulting surrogate pair.
        /// hbyte         lbyte          data
        /// nnnwwwww      zzzzzyyy      1xxxxxxx
        /// high-surrogate     low-surrogate
        /// 110110wwwwwzzzzz   110111yyyxxxxxxx
        /// </remarks>
        /// <param name="chOffset">
        /// - Since the three top bits of chOffset are not needed to
        /// set the location of the extended Window, they are used instead
        /// to select the window, thereby reducing the number of needed command codes.
        /// The bottom 13 bits of chOffset are used to calculate the offset relative to
        /// a 7 bit input data byte to yield the 20 bits expressed by each surrogate pair.
        /// </param>
        protected internal virtual void DefineExtendedWindow(char chOffset)
        {
            // The top 3 bits of iOffsetHi are the window index
            int iWindow = (char)(((char)chOffset) >> 13);
            // Calculate the new offset
            dynamicOffset[iWindow] = ((chOffset & unchecked((int)(0x1FFF))) << 7) + (1 << 16);
            // make the redefined window the active one
            SelectWindow(iWindow);
        }

        /// <summary>string buffer length used by the following functions</summary>
        protected internal int iOut = 0;

        /// <summary>input cursor used by the following functions</summary>
        protected internal int iIn = 0;

        /// <summary>expand input that is in Unicode mode</summary>
        /// <param name="in">input byte array to be expanded</param>
        /// <param name="iCur">starting index</param>
        /// <param name="sb">string buffer to which to append expanded input</param>
        /// <returns>the index for the lastc byte processed</returns>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.IllegalInputException"></exception>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.EndOfInputException"></exception>
        protected internal virtual int ExpandUnicode(byte[] @in, int iCur, StringBuilder
            sb)
        {
            for (; iCur < @in.Length - 1; iCur += 2)
            {
                // step by 2:
                byte b = @in[iCur];
#pragma warning disable CS0652 // Comparison to integral constant is useless; the constant is outside the range of the type
                if (b >= UC0 && ((sbyte)b) <= UC7)
#pragma warning restore CS0652 // Comparison to integral constant is useless; the constant is outside the range of the type
                {
                    Debug.Out("SelectWindow: ", b);
                    SelectWindow(b - UC0);
                    return iCur;
                }
                else
                {
#pragma warning disable CS0652 // Comparison to integral constant is useless; the constant is outside the range of the type
                    if (b >= UD0 && ((sbyte)b) <= UD7)
#pragma warning restore CS0652 // Comparison to integral constant is useless; the constant is outside the range of the type
                    {
                        DefineWindow(b - UD0, @in[iCur + 1]);
                        return iCur + 1;
                    }
                    else
                    {
                        if (b == UDX)
                        {
                            if (iCur >= @in.Length - 2)
                            {
                                break;
                            }
                            // buffer error
                            DefineExtendedWindow(CharFromTwoBytes(@in[iCur + 1], @in[iCur + 2]));
                            return iCur + 2;
                        }
                        else
                        {
                            if (b == UQU)
                            {
                                if (iCur >= @in.Length - 2)
                                {
                                    break;
                                }
                                // error
                                // Skip command byte and output Unicode character
                                iCur++;
                            }
                        }
                    }
                }
                // output a Unicode character
                char ch = CharFromTwoBytes(@in[iCur], @in[iCur + 1]);
                sb.Append(ch);
                iOut++;
            }
            if (iCur == @in.Length)
            {
                return iCur;
            }
            // Error condition
            throw new EndOfInputException();
        }

        /// <summary>
        /// assemble a char from two bytes
        /// In Java bytes are signed quantities, while chars are unsigned
        /// </summary>
        /// <returns>the character</returns>
        /// <param name="hi">most significant byte</param>
        /// <param name="lo">least significant byte</param>
        public static char CharFromTwoBytes(byte hi, byte lo)
        {
            char ch = (char)(lo >= 0 ? lo : 256 + lo);
            return (char)(ch + (char)((hi >= 0 ? hi : 256 + hi) << 8));
        }

        /// <summary>expand portion of the input that is in single byte mode</summary>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.IllegalInputException"></exception>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.EndOfInputException"></exception>
        protected internal virtual string ExpandSingleByte(byte[] @in)
        {
            StringBuilder sb = new StringBuilder(@in.Length);
            iOut = 0;
            // Loop until all input is exhausted or an error occurred
            int iCur;
            for (iCur = 0; iCur < @in.Length; iCur++)
            {
                // DEBUG Debug.out("Expanding: ", iCur);
                // Default behaviour is that ASCII characters are passed through
                // (staticOffset[0] == 0) and characters with the high bit on are
                // offset by the current dynamic (or sliding) window (this.iWindow)
                int iStaticWindow = 0;
                int iDynamicWindow = GetCurrentWindow();
                switch (@in[iCur])
                {
                    case SQ0:
                    case SQ1:
                    case SQ2:
                    case SQ3:
                    case SQ4:
                    case SQ5:
                    case SQ6:
                    case SQ7:
                        {
                            // Quote from a static Window
                            Debug.Out("SQn:", iStaticWindow);
                            // skip the command byte and check for length
                            if (iCur >= @in.Length - 1)
                            {
                                Debug.Out("SQn missing argument: ", @in, iCur);
                                goto Loop_break;
                            }
                            // buffer length error
                            // Select window pair to quote from
                            iDynamicWindow = iStaticWindow = @in[iCur] - SQ0;
                            iCur++;
                            goto default;
                        }

                    default:
                        {
                            // FALL THROUGH
                            // output as character
                            if (@in[iCur] >= 0)
                            {
                                // use static window
                                int ch = @in[iCur] + staticOffset[iStaticWindow];
                                sb.Append((char)ch);
                                iOut++;
                            }
                            else
                            {
                                // use dynamic window
                                int ch = (@in[iCur] + 256);
                                // adjust for signed bytes
                                ch -= unchecked((int)(0x80));
                                // reduce to range 00..7F
                                ch += dynamicOffset[iDynamicWindow];
                                //DEBUG
                                Debug.Out("Dynamic: ", (char)ch);
                                if (ch < 1 << 16)
                                {
                                    // in Unicode range, output directly
                                    sb.Append((char)ch);
                                    iOut++;
                                }
                                else
                                {
                                    // this is an extension character
                                    Debug.Out("Extension character: ", ch);
                                    // compute and append the two surrogates:
                                    // translate from 10000..10FFFF to 0..FFFFF
                                    ch -= unchecked((int)(0x10000));
                                    // high surrogate = top 10 bits added to D800
                                    sb.Append((char)(unchecked((int)(0xD800)) + (ch >> 10)));
                                    iOut++;
                                    // low surrogate = bottom 10 bits added to DC00
                                    sb.Append((char)(unchecked((int)(0xDC00)) + (ch & ~unchecked((int)(0xFC00)))));
                                    iOut++;
                                }
                            }
                            break;
                        }

                    case SDX:
                        {
                            // define a dynamic window as extended
                            iCur += 2;
                            if (iCur >= @in.Length)
                            {
                                Debug.Out("SDn missing argument: ", @in, iCur - 1);
                                goto Loop_break;
                            }
                            // buffer length error
                            DefineExtendedWindow(CharFromTwoBytes(@in[iCur - 1], @in[iCur]));
                            break;
                        }

                    case SD0:
                    case SD1:
                    case SD2:
                    case SD3:
                    case SD4:
                    case SD5:
                    case SD6:
                    case SD7:
                        {
                            // Position a dynamic Window
                            iCur++;
                            if (iCur >= @in.Length)
                            {
                                Debug.Out("SDn missing argument: ", @in, iCur - 1);
                                goto Loop_break;
                            }
                            // buffer length error
                            DefineWindow(@in[iCur - 1] - SD0, @in[iCur]);
                            break;
                        }

                    case SC0:
                    case SC1:
                    case SC2:
                    case SC3:
                    case SC4:
                    case SC5:
                    case SC6:
                    case SC7:
                        {
                            // Select a new dynamic Window
                            SelectWindow(@in[iCur] - SC0);
                            break;
                        }

                    case SCU:
                        {
                            // switch to Unicode mode and continue parsing
                            iCur = ExpandUnicode(@in, iCur + 1, sb);
                            // DEBUG Debug.out("Expanded Unicode range until: ", iCur);
                            break;
                        }

                    case SQU:
                        {
                            // directly extract one Unicode character
                            iCur += 2;
                            if (iCur >= @in.Length)
                            {
                                Debug.Out("SQU missing argument: ", @in, iCur - 2);
                                goto Loop_break;
                            }
                            else
                            {
                                // buffer length error
                                char ch = CharFromTwoBytes(@in[iCur - 1], @in[iCur]);
                                Debug.Out("Quoted: ", ch);
                                sb.Append(ch);
                                iOut++;
                            }
                            break;
                        }

                    case Srs:
                        {
                            throw new IllegalInputException();
                        }
                }
#pragma warning disable CS0164 // This label has not been referenced
            Loop_continue:;
#pragma warning restore CS0164 // This label has not been referenced
            }
        Loop_break:;
            // break;
            if (iCur >= @in.Length)
            {
                //SUCCESS: all input used up
                sb.Length = iOut;
                iIn = iCur;
                return sb.ToString();
            }
            Debug.Out("Length ==" + @in.Length + " iCur =", iCur);
            //ERROR: premature end of input
            throw new EndOfInputException();
        }

        /// <summary>expand a byte array containing compressed Unicode</summary>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.IllegalInputException"></exception>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.EndOfInputException"></exception>
        public virtual string ExpandArray(byte[] @in)
        {
            string str = ExpandSingleByte(@in);
            Debug.Out("expand output: ", str.ToCharArray());
            return str;
        }

        /// <summary>
        /// reset is called to start with new input, w/o creating a new
        /// instance
        /// </summary>
        public override void Reset()
        {
            iOut = 0;
            iIn = 0;
            base.Reset();
        }

        public virtual int CharsWritten()
        {
            return iOut;
        }

        public virtual int BytesRead()
        {
            return iIn;
        }
    }
}
