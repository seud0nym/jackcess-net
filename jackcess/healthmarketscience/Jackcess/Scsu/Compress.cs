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

namespace HealthMarketScience.Jackcess.Scsu
{
    /// <summary>This class implements a simple compression algorithm</summary>
    public class Compress : SCSU
    {
        /// <summary>next input character to be read</summary>
        private int iIn;

        /// <summary>next output byte to be written</summary>
        private int iOut;

        /// <summary>start index of Unicode mode in output array, or -1 if in single byte mode
        /// 	</summary>
        private int iSCU = -1;

        /// <summary>true if the next command byte is of the Uxx family</summary>
        private bool fUnicodeMode = false;

        /// <summary>locate a window for a character given a table of offsets</summary>
        /// <param name="ch">- character</param>
        /// <param name="offsetTable">- table of window offsets</param>
        /// <returns>true if the character fits a window from the table of windows</returns>
        private bool LocateWindow(int ch, int[] offsetTable)
        {
            // always try the current window first
            int iWin = GetCurrentWindow();
            // if the character fits the current window
            // just use the current window
            if (iWin != -1 && ch >= offsetTable[iWin] && ch < offsetTable[iWin] + unchecked((
                int)(0x80)))
            {
                return true;
            }
            // try all windows in order
            for (iWin = 0; iWin < offsetTable.Length; iWin++)
            {
                if (ch >= offsetTable[iWin] && ch < offsetTable[iWin] + unchecked((int)(0x80)))
                {
                    SelectWindow(iWin);
                    return true;
                }
            }
            // none found
            return false;
        }

        /// <summary>returns true if the character is ASCII, but not a control other than CR, LF and TAB
        /// 	</summary>
        public static bool IsAsciiCrLfOrTab(int ch)
        {
            return (ch >= unchecked((int)(0x20)) && ch <= unchecked((int)(0x7F))) || ch == unchecked(
                (int)(0x09)) || ch == unchecked((int)(0x0A)) || ch == unchecked((int)(0x0D));
        }

        // ASCII
        // CR/LF or TAB
        /// <summary>
        /// output a run of characters in single byte mode
        /// In single byte mode pass through characters in the ASCII range, but
        /// quote characters overlapping with compression command codes.
        /// </summary>
        /// <remarks>
        /// output a run of characters in single byte mode
        /// In single byte mode pass through characters in the ASCII range, but
        /// quote characters overlapping with compression command codes. Runs
        /// of characters fitting the current window are output as runs of bytes
        /// in the range 0x80-0xFF. Checks for and validates Surrogate Pairs.
        /// Uses and updates the current input and output cursors store in
        /// the instance variables <i>iIn</i> and <i>iOut</i>.
        /// </remarks>
        /// <param name="in">- input character array</param>
        /// <param name="out">- output byte array</param>
        /// <returns>the next chaacter to be processed. This may be an extended character.</returns>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.EndOfOutputException"></exception>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.EndOfInputException"></exception>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.IllegalInputException"></exception>
        public virtual int OutputSingleByteRun(char[] @in, byte[] @out)
        {
            int iWin = GetCurrentWindow();
            while (iIn < @in.Length)
            {
                int outlen = 0;
                byte byte1 = 0;
                byte byte2 = 0;
                // get the input character
                int ch = @in[iIn];
                int inlen = 1;
                // Check input for Surrogate pair
                if ((ch & unchecked((int)(0xF800))) == unchecked((int)(0xD800)))
                {
                    if ((ch & unchecked((int)(0xFC00))) == unchecked((int)(0xDC00)))
                    {
                        // low surrogate out of order
                        throw new IllegalInputException("Unpaired low surrogate: " + iIn);
                    }
                    else
                    {
                        // have high surrogate now get low surrogate
                        if (iIn >= @in.Length - 1)
                        {
                            // premature end of input
                            throw new EndOfInputException();
                        }
                        // get the char
                        int ch2 = @in[iIn + 1];
                        // make sure it's a low surrogate
                        if ((ch2 & unchecked((int)(0xFC00))) != unchecked((int)(0xDC00)))
                        {
                            // a low surrogate was required
                            throw new IllegalInputException("Unpaired high surrogate: " + (iIn + 1));
                        }
                        // combine the two values
                        ch = ((ch - unchecked((int)(0xD800))) << 10 | (ch2 - unchecked((int)(0xDC00)))) +
                             unchecked((int)(0x10000));
                        // ch = ch<<10 + ch2 - 0x36F0000;
                        inlen = 2;
                    }
                }
                // ASCII Letter, NUL, CR, LF and TAB are always passed through
                if (IsAsciiCrLfOrTab(ch) || ch == 0)
                {
                    // pass through directcly
                    byte2 = unchecked((byte)(ch & unchecked((int)(0x7F))));
                    outlen = 1;
                }
                else
                {
                    // All other control codes must be quoted
                    if (ch < unchecked((int)(0x20)))
                    {
                        byte1 = SQ0;
                        byte2 = unchecked((byte)(ch));
                        outlen = 2;
                    }
                    else
                    {
                        // Letters that fit the current dynamic window
                        if (ch >= dynamicOffset[iWin] && ch < dynamicOffset[iWin] + unchecked((int)(0x80)
                            ))
                        {
                            ch -= dynamicOffset[iWin];
                            byte2 = unchecked((byte)(ch | unchecked((int)(0x80))));
                            outlen = 1;
                        }
                    }
                }
                // check for room in the output array
                if (iOut + outlen >= @out.Length)
                {
                    throw new EndOfOutputException();
                }
                switch (outlen)
                {
                    default:
                        {
                            // need to use some other compression mode for this
                            // character so we terminate this loop
                            return ch;
                        }

                    case 2:
                        {
                            // input not finished
                            // output the characters
                            @out[iOut++] = byte1;
                            goto case 1;
                        }

                    case 1:
                        {
                            // fall through
                            @out[iOut++] = byte2;
                            break;
                        }
                }
                // advance input pointer
                iIn += inlen;
            }
            return 0;
        }

        // input all used up
        /// <summary>
        /// quote a single character in single byte mode
        /// Quoting a character (aka 'non-locking shift') gives efficient access
        /// to characters that occur in isolation--usually punctuation characters.
        /// </summary>
        /// <remarks>
        /// quote a single character in single byte mode
        /// Quoting a character (aka 'non-locking shift') gives efficient access
        /// to characters that occur in isolation--usually punctuation characters.
        /// When quoting a character from a dynamic window use 0x80 - 0xFF, when
        /// quoting a character from a static window use 0x00-0x7f.
        /// </remarks>
        /// <param name="ch">- character to be quoted</param>
        /// <param name="out">- output byte array</param>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.EndOfOutputException"></exception>
        private void QuoteSingleByte(int ch, byte[] @out)
        {
            Debug.Out("Quoting SingleByte ", ch);
            int iWin = GetCurrentWindow();
            // check for room in the output array
            if (iOut >= @out.Length - 2)
            {
                throw new EndOfOutputException();
            }
            // Output command byte followed by
            @out[iOut++] = unchecked((byte)(SQ0 + iWin));
            // Letter that fits the current dynamic window
            if (ch >= dynamicOffset[iWin] && ch < dynamicOffset[iWin] + unchecked((int)(0x80)
                ))
            {
                ch -= dynamicOffset[iWin];
                @out[iOut++] = unchecked((byte)(ch | unchecked((int)(0x80))));
            }
            else
            {
                // Letter that fits the current static window
                if (ch >= staticOffset[iWin] && ch < staticOffset[iWin] + unchecked((int)(0x80)))
                {
                    ch -= staticOffset[iWin];
                    @out[iOut++] = unchecked((byte)ch);
                }
                else
                {
                    throw new InvalidOperationException("ch = " + ch + " not valid in quoteSingleByte. Internal Compressor Error"
                        );
                }
            }
            // advance input pointer
            iIn++;
            Debug.Out("New input: ", iIn);
        }

        /// <summary>
        /// output a run of characters in Unicode mode
        /// A run of Unicode mode consists of characters which are all in the
        /// range of non-compressible characters or isolated occurrence
        /// of any other characters.
        /// </summary>
        /// <remarks>
        /// output a run of characters in Unicode mode
        /// A run of Unicode mode consists of characters which are all in the
        /// range of non-compressible characters or isolated occurrence
        /// of any other characters. Characters in the range 0xE00-0xF2FF must
        /// be quoted to avoid overlap with the Unicode mode compression command codes.
        /// Uses and updates the current input and output cursors store in
        /// the instance variables <i>iIn</i> and <i>iOut</i>.
        /// NOTE: Characters from surrogate pairs are passed through and unlike single
        /// byte mode no checks are made for unpaired surrogate characters.
        /// </remarks>
        /// <param name="in">- input character array</param>
        /// <param name="out">- output byte array</param>
        /// <returns>the next input character to be processed</returns>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.EndOfOutputException"></exception>
        public virtual char OutputUnicodeRun(char[] @in, byte[] @out)
        {
            // current character
            char ch = (char)0;
            while (iIn < @in.Length)
            {
                // get current input and set default output length
                ch = @in[iIn];
                int outlen = 2;
                // Characters in these ranges could potentially be compressed.
                // We require 2 or more compressible characters to break the run
                if (IsCompressible(ch))
                {
                    // check whether we can look ahead
                    if (iIn < @in.Length - 1)
                    {
                        // DEBUG
                        Debug.Out("is-comp: ", ch);
                        char ch2 = @in[iIn + 1];
                        if (IsCompressible(ch2))
                        {
                            // at least 2 characters are compressible
                            // break the run
                            break;
                        }
                        //DEBUG
                        Debug.Out("no-comp: ", ch2);
                    }
                    // If we get here, the current character is only character
                    // left in the input or it is followed by a non-compressible
                    // character. In neither case do we gain by breaking the
                    // run, so we proceed to output the character.
                    if (ch >= unchecked((int)(0xE000)) && ch <= unchecked((int)(0xF2FF)))
                    {
                        // Characters in this range need to be escaped
                        outlen = 3;
                    }
                }
                // check that there is enough room to output the character
                if (iOut >= @out.Length - outlen)
                {
                    // DEBUG
                    Debug.Out("End of Output @", iOut);
                    // if we got here, we ran out of space in the output array
                    throw new EndOfOutputException();
                }
                // output any characters that cannot be compressed,
                if (outlen == 3)
                {
                    // output the quote character
                    @out[iOut++] = UQU;
                }
                // pass the Unicode character in MSB,LSB order
                @out[iOut++] = unchecked((byte)((char)(((char)ch) >> 8)));
                @out[iOut++] = unchecked((byte)(ch & unchecked((int)(0xFF))));
                // advance input cursor
                iIn++;
            }
            // return the last character
            return ch;
        }

        internal static int iNextWindow = 3;

        /// <summary>
        /// redefine a window so it surrounds a given character value
        /// For now, this function uses window 3 exclusively (window 4
        /// for extended windows);
        /// </summary>
        /// <returns>true if a window was successfully defined</returns>
        /// <param name="ch">- character around which window is positioned</param>
        /// <param name="out">- output byte array</param>
        /// <param name="fCurUnicodeMode">- type of window</param>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.IllegalInputException"></exception>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.EndOfOutputException"></exception>
        private bool PositionWindow(int ch, byte[] @out, bool fCurUnicodeMode)
        {
            int iWin = iNextWindow % 8;
            // simple LRU
            int iPosition = 0;
            // iPosition 0 is a reserved value
            if (ch < unchecked((int)(0x80)))
            {
                throw new InvalidOperationException("ch < 0x80");
            }
            //return false;
            // Check the fixed offsets
            for (int i = 0; i < fixedOffset.Length; i++)
            {
                if (ch >= fixedOffset[i] && ch < fixedOffset[i] + unchecked((int)(0x80)))
                {
                    iPosition = i;
                    break;
                }
            }
            if (iPosition != 0)
            {
                // DEBUG
                Debug.Out("FIXED position is ", iPosition + unchecked((int)(0xF9)));
                // ch fits in a fixed offset window position
                dynamicOffset[iWin] = fixedOffset[iPosition];
                iPosition += unchecked((int)(0xF9));
            }
            else
            {
                if (ch < unchecked((int)(0x3400)))
                {
                    // calculate a window position command and set the offset
                    iPosition = (int)(((uint)ch) >> 7);
                    dynamicOffset[iWin] = ch & unchecked((int)(0xFF80));
                    Debug.Out("Offset=" + dynamicOffset[iWin] + ", iPosition=" + iPosition + " for char"
                        , ch);
                }
                else
                {
                    if (ch < unchecked((int)(0xE000)))
                    {
                        // attempt to place a window where none can go
                        return false;
                    }
                    else
                    {
                        if (ch <= unchecked((int)(0xFFFF)))
                        {
                            // calculate a window position command, accounting
                            // for the gap in position values, and set the offset
                            iPosition = ((int)(((uint)(ch - gapOffset)) >> 7));
                            dynamicOffset[iWin] = ch & unchecked((int)(0xFF80));
                            Debug.Out("Offset=" + dynamicOffset[iWin] + ", iPosition=" + iPosition + " for char"
                                , ch);
                        }
                        else
                        {
                            // if we get here, the character is in the extended range.
                            // Always use Window 4 to define an extended window
                            iPosition = (int)(((uint)(ch - unchecked((int)(0x10000)))) >> 7);
                            // DEBUG
                            Debug.Out("Try position Window at ", iPosition);
                            iPosition |= iWin << 13;
                            dynamicOffset[iWin] = ch & unchecked((int)(0x1FFF80));
                        }
                    }
                }
            }
            // Outputting window defintion command for the general cases
            if (iPosition < unchecked((int)(0x100)) && iOut < @out.Length - 1)
            {
                @out[iOut++] = unchecked((byte)((fCurUnicodeMode ? UD0 : SD0) + iWin));
                @out[iOut++] = unchecked((byte)(iPosition & unchecked((int)(0xFF))));
            }
            else
            {
                // Output an extended window definiton command
                if (iPosition >= unchecked((int)(0x100)) && iOut < @out.Length - 2)
                {
                    Debug.Out("Setting extended window at ", iPosition);
                    @out[iOut++] = (fCurUnicodeMode ? UDX : SDX);
                    @out[iOut++] = unchecked((byte)(((int)(((uint)iPosition) >> 8)) & unchecked((int)
                        (0xFF))));
                    @out[iOut++] = unchecked((byte)(iPosition & unchecked((int)(0xFF))));
                }
                else
                {
                    throw new EndOfOutputException();
                }
            }
            SelectWindow(iWin);
            iNextWindow++;
            return true;
        }

        /// <summary>compress a Unicode character array with some simplifying assumptions</summary>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.IllegalInputException"></exception>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.EndOfInputException"></exception>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.EndOfOutputException"></exception>
        public virtual int SimpleCompress(char[] @in, int iStartIn, byte[] @out, int iStartOut
            )
        {
            iIn = iStartIn;
            iOut = iStartOut;
            while (iIn < @in.Length)
            {
                int ch;
                // previously we switched to a Unicode run
                if (iSCU != -1)
                {
                    Debug.Out("Remaining", @in, iIn);
                    Debug.Out("Output until [" + iOut + "]: ", @out);
                    // output characters as Unicode
                    ch = OutputUnicodeRun(@in, @out);
                    // for single character Unicode runs (3 bytes) use quote
                    if (iOut - iSCU == 3)
                    {
                        // go back and fix up the SCU to an SQU instead
                        @out[iSCU] = SQU;
                        iSCU = -1;
                        continue;
                    }
                    else
                    {
                        iSCU = -1;
                        fUnicodeMode = true;
                    }
                }
                else
                {
                    // next, try to output characters as single byte run
                    ch = OutputSingleByteRun(@in, @out);
                }
                // check whether we still have input
                if (iIn == @in.Length)
                {
                    break;
                }
                // no more input
                // if we get here, we have a consistent value for ch, whether or
                // not it is an regular or extended character. Locate or define a
                // Window for the current character
                Debug.Out("Output so far: ", @out);
                Debug.Out("Routing ch=" + ch + " for Input", @in, iIn);
                // Check that we have enough room to output the command byte
                if (iOut >= @out.Length - 1)
                {
                    throw new EndOfOutputException();
                }
                // In order to switch away from Unicode mode, it is necessary
                // to select (or define) a window. If the characters that follow
                // the Unicode range are ASCII characters, we can't use them
                // to decide which window to select, since ASCII characters don't
                // influence window settings. This loop looks ahead until it finds
                // one compressible character that isn't in the ASCII range.
                for (int ich = iIn; ch < unchecked((int)(0x80)); ich++)
                {
                    if (ich == @in.Length || !IsCompressible(@in[ich]))
                    {
                        // if there are only ASCII characters left,
                        ch = @in[iIn];
                        break;
                    }
                    ch = @in[ich];
                }
                // lookahead for next non-ASCII char
                // The character value contained in ch here will only be used to select
                // output modes. Actual output of characters starts with in[iIn] and
                // only takes place near the top of the loop.
                int iprevWindow = GetCurrentWindow();
                // try to locate a dynamic window
                if (ch < unchecked((int)(0x80)) || LocateWindow(ch, dynamicOffset))
                {
                    Debug.Out("located dynamic window " + GetCurrentWindow() + " at ", iOut + 1);
                    // lookahead to use SQn instead of SCn for single
                    // character interruptions of runs in current window
                    if (!fUnicodeMode && iIn < @in.Length - 1)
                    {
                        char ch2 = @in[iIn + 1];
                        if (ch2 >= dynamicOffset[iprevWindow] && ch2 < dynamicOffset[iprevWindow] + unchecked(
                            (int)(0x80)))
                        {
                            QuoteSingleByte(ch, @out);
                            SelectWindow(iprevWindow);
                            continue;
                        }
                    }
                    @out[iOut++] = unchecked((byte)((fUnicodeMode ? UC0 : SC0) + GetCurrentWindow()));
                    fUnicodeMode = false;
                }
                else
                {
                    // try to locate a static window
                    if (!fUnicodeMode && LocateWindow(ch, staticOffset))
                    {
                        // static windows are not accessible from Unicode mode
                        Debug.Out("located a static window", GetCurrentWindow());
                        QuoteSingleByte(ch, @out);
                        SelectWindow(iprevWindow);
                        // restore current Window settings
                        continue;
                    }
                    else
                    {
                        // try to define a window around ch
                        if (PositionWindow(ch, @out, fUnicodeMode))
                        {
                            fUnicodeMode = false;
                        }
                        else
                        {
                            // If all else fails, start a Unicode run
                            iSCU = iOut;
                            @out[iOut++] = SCU;
                            continue;
                        }
                    }
                }
            }
            return iOut - iStartOut;
        }

        /// <exception cref="HealthMarketScience.Jackcess.Scsu.IllegalInputException"></exception>
        /// <exception cref="HealthMarketScience.Jackcess.Scsu.EndOfInputException"></exception>
        public virtual byte[] CompressString(string inStr)
        {
            // Running out of room for output can cause non-optimal
            // compression. In order to not slow down compression too
            // much, not all intermediate state is constantly saved.
            byte[] @out = new byte[inStr.Length * 2];
            char[] @in = inStr.ToCharArray();
            //DEBUG
            Debug.Out("compress input: ", @in);
            Reset();
            while (true)
            {
                try
                {
                    SimpleCompress(@in, CharsRead(), @out, BytesWritten());
                    // if we get here things went fine.
                    break;
                }
                catch (EndOfOutputException)
                {
                    // create a larger output buffer and continue
                    byte[] largerOut = new byte[@out.Length * 2];
                    System.Array.Copy(@out, 0, largerOut, 0, @out.Length);
                    @out = largerOut;
                }
            }
            byte[] trimmedOut = new byte[BytesWritten()];
            System.Array.Copy(@out, 0, trimmedOut, 0, trimmedOut.Length);
            @out = trimmedOut;
            Debug.Out("compress output: ", @out);
            return @out;
        }

        /// <summary>
        /// reset is only needed to bail out after an exception and
        /// restart with new input
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            fUnicodeMode = false;
            iSCU = -1;
        }

        /// <summary>returns the number of bytes written</summary>
        public virtual int BytesWritten()
        {
            return iOut;
        }

        /// <summary>returns the number of bytes written</summary>
        public virtual int CharsRead()
        {
            return iIn;
        }
    }
}
