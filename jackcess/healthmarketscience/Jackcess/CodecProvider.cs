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

namespace HealthMarketScience.Jackcess
{
    /// <summary>
    /// Interface for a provider which can generate CodecHandlers for various types
    /// of database encodings.
    /// </summary>
    /// <remarks>
    /// Interface for a provider which can generate CodecHandlers for various types
    /// of database encodings.  The
    /// <see cref="DefaultCodecProvider">DefaultCodecProvider</see>
    /// is the default
    /// implementation of this inferface, but it does not have any actual
    /// encoding/decoding support (due to possible export issues with calling
    /// encryption APIs).  See the separate
    /// <a href="https://sourceforge.net/projects/jackcessencrypt/">Jackcess
    /// Encrypt</a> project for an implementation of this interface which supports
    /// various access database encryption types.
    /// </remarks>
    /// <author>James Ahlborn</author>
    public interface CodecProvider
    {
        /// <summary>
        /// Returns a new CodecHandler for the database associated with the given
        /// PageChannel.
        /// </summary>
        /// <remarks>
        /// Returns a new CodecHandler for the database associated with the given
        /// PageChannel.
        /// </remarks>
        /// <param name="channel">the PageChannel for a Database</param>
        /// <param name="charset">the Charset for the Database</param>
        /// <returns>
        /// a new CodecHandler, may not be
        /// <code>null</code>
        /// </returns>
        /// <exception cref="System.IO.IOException"></exception>
        CodecHandler CreateHandler(PageChannel channel, Encoding charset);
    }
}
