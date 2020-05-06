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

using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>
	/// Interface for a handler which can encode/decode a specific access page
	/// encoding.
	/// </summary>
	/// <remarks>
	/// Interface for a handler which can encode/decode a specific access page
	/// encoding.
	/// </remarks>
	/// <author>James Ahlborn</author>
	public interface CodecHandler
	{
		/// <summary>Decodes the given page buffer inline.</summary>
		/// <remarks>Decodes the given page buffer inline.</remarks>
		/// <param name="page">the page to be decoded</param>
		/// <param name="pageNumber">the page number of the given page</param>
		/// <exception cref="System.IO.IOException">if an exception occurs during decoding</exception>
		void DecodePage(ByteBuffer page, int pageNumber);

		/// <summary>Encodes the given page buffer into a new page buffer and returns it.</summary>
		/// <remarks>
		/// Encodes the given page buffer into a new page buffer and returns it.  The
		/// returned page buffer will be used immediately and discarded so that it
		/// may be re-used for subsequent page encodings.
		/// </remarks>
		/// <param name="page">the page to be encoded, should not be modified</param>
		/// <param name="pageNumber">the page number of the given page</param>
		/// <param name="pageOffset">
		/// offset within the page at which to start writing the
		/// page data
		/// </param>
		/// <exception cref="System.IO.IOException">if an exception occurs during decoding</exception>
		/// <returns>the properly encoded page buffer for the given page buffer</returns>
		ByteBuffer EncodePage(ByteBuffer page, int pageNumber, int pageOffset);
	}
}
