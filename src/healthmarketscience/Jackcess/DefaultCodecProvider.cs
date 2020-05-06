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
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>
	/// Default implementation of CodecProvider which does not have any actual
	/// encoding/decoding support.
	/// </summary>
	/// <remarks>
	/// Default implementation of CodecProvider which does not have any actual
	/// encoding/decoding support.  See
	/// <see cref="CodecProvider">CodecProvider</see>
	/// for details on a more
	/// useful implementation.
	/// </remarks>
	/// <author>James Ahlborn</author>
	public class DefaultCodecProvider : CodecProvider
	{
		/// <summary>common instance of DefaultCodecProvider</summary>
		public static readonly CodecProvider INSTANCE = new DefaultCodecProvider();

		/// <summary>
		/// common instance of
		/// <see cref="DummyHandler">DummyHandler</see>
		/// 
		/// </summary>
		public static readonly CodecHandler DUMMY_HANDLER = new DefaultCodecProvider.DummyHandler
			();

		/// <summary>
		/// common instance of
		/// <see cref="UnsupportedHandler">UnsupportedHandler</see>
		/// 
		/// </summary>
		public static readonly CodecHandler UNSUPPORTED_HANDLER = new DefaultCodecProvider.UnsupportedHandler
			();

		/// <summary>
		/// <inheritDoc></inheritDoc>
		/// <p>
		/// This implementation returns DUMMY_HANDLER for databases with no encoding
		/// and UNSUPPORTED_HANDLER for databases with any encoding.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual CodecHandler CreateHandler(PageChannel channel, Encoding charset)
		{
			JetFormat format = channel.GetFormat();
			switch (format.CODEC_TYPE)
			{
				case JetFormat.CodecType.NONE:
				{
					// no encoding, all good
					return DUMMY_HANDLER;
				}

				case JetFormat.CodecType.JET:
				{
					// check for an encode key.  if 0, not encoded
					ByteBuffer bb = channel.CreatePageBuffer();
					channel.ReadPage(bb, 0);
					int codecKey = bb.GetInt(format.OFFSET_ENCODING_KEY);
					return ((codecKey == 0) ? DUMMY_HANDLER : UNSUPPORTED_HANDLER);
				}

				case JetFormat.CodecType.MSISAM:
				{
					// always encoded, we don't handle it
					return UNSUPPORTED_HANDLER;
				}

				default:
				{
					throw new RuntimeException("Unknown codec type " + format.CODEC_TYPE);
				}
			}
		}

		/// <summary>
		/// CodecHandler implementation which does nothing, useful for databases with
		/// no extra encoding.
		/// </summary>
		/// <remarks>
		/// CodecHandler implementation which does nothing, useful for databases with
		/// no extra encoding.
		/// </remarks>
		public class DummyHandler : CodecHandler
		{
			/// <exception cref="System.IO.IOException"></exception>
			public virtual void DecodePage(ByteBuffer page, int pageNumber)
			{
			}

			// does nothing
			/// <exception cref="System.IO.IOException"></exception>
			public virtual ByteBuffer EncodePage(ByteBuffer page, int pageNumber, int pageOffset
				)
			{
				// does nothing
				return page;
			}
		}

		/// <summary>
		/// CodecHandler implementation which always throws
		/// UnsupportedOperationException, useful for databases with unsupported
		/// encodings.
		/// </summary>
		/// <remarks>
		/// CodecHandler implementation which always throws
		/// UnsupportedOperationException, useful for databases with unsupported
		/// encodings.
		/// </remarks>
		public class UnsupportedHandler : CodecHandler
		{
			/// <exception cref="System.IO.IOException"></exception>
			public virtual void DecodePage(ByteBuffer page, int pageNumber)
			{
				throw new NotSupportedException("Decoding not supported");
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual ByteBuffer EncodePage(ByteBuffer page, int pageNumber, int pageOffset
				)
			{
				throw new NotSupportedException("Encoding not supported");
			}
		}
	}
}
