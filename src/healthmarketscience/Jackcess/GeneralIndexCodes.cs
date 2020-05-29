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

namespace HealthMarketScience.Jackcess
{
    /// <summary>
    /// Various constants used for creating "general" (access 2010+) sort order
    /// text index entries.
    /// </summary>
    /// <remarks>
    /// Various constants used for creating "general" (access 2010+) sort order
    /// text index entries.
    /// </remarks>
    /// <author>James Ahlborn</author>
    public class GeneralIndexCodes : GeneralLegacyIndexCodes
    {
        private static readonly string CODES_FILE = Database.RESOURCE_PATH + "index_codes_gen.txt";

        private static readonly string EXT_CODES_FILE = Database.RESOURCE_PATH + "index_codes_ext_gen.txt";

        internal sealed new class Codes
        {
            /// <summary>handlers for the first 256 chars.</summary>
            /// <remarks>
            /// handlers for the first 256 chars.  use nested class to lazy load the
            /// handlers
            /// </remarks>
            internal static readonly GeneralLegacyIndexCodes.CharHandler[] _values = LoadCodes
                (CODES_FILE, FIRST_CHAR, LAST_CHAR);
            // stash the codes in some resource files
        }

        internal sealed new class ExtCodes
        {
            /// <summary>handlers for the rest of the chars in BMP 0.</summary>
            /// <remarks>
            /// handlers for the rest of the chars in BMP 0.  use nested class to
            /// lazy load the handlers
            /// </remarks>
            internal static readonly GeneralLegacyIndexCodes.CharHandler[] _values = LoadCodes
                (EXT_CODES_FILE, FIRST_EXT_CHAR, LAST_EXT_CHAR);
        }

        internal static readonly GeneralIndexCodes GEN_INSTANCE = new GeneralIndexCodes();

        public GeneralIndexCodes()
        {
        }

        /// <summary>Returns the CharHandler for the given character.</summary>
        /// <remarks>Returns the CharHandler for the given character.</remarks>
        internal override GeneralLegacyIndexCodes.CharHandler GetCharHandler(char c)
        {
            if (c <= LAST_CHAR)
            {
                return GeneralIndexCodes.Codes._values[c];
            }
            int extOffset = AsUnsignedChar(c) - AsUnsignedChar(FIRST_EXT_CHAR);
            return GeneralIndexCodes.ExtCodes._values[extOffset];
        }
    }
}
