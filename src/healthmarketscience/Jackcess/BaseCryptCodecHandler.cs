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
using HealthMarketScience.Jackcess;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto;
using Sharpen;
using Org.BouncyCastle.Crypto.Parameters;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Base CodecHandler support for RC4 encryption based CodecHandlers.</summary>
	/// <remarks>Base CodecHandler support for RC4 encryption based CodecHandlers.</remarks>
	/// <author>Vladimir Berezniker</author>
	public abstract class BaseCryptCodecHandler : CodecHandler
	{
		private RC4Engine _engine;

		public BaseCryptCodecHandler()
		{
		}

		protected internal RC4Engine GetEngine()
		{
			if (_engine == null)
			{
				_engine = new RC4Engine();
			}
			return _engine;
		}

		/// <summary>
		/// Decodes the page in the given buffer (in place) using RC4 decryption with
		/// the given params.
		/// </summary>
		/// <remarks>
		/// Decodes the page in the given buffer (in place) using RC4 decryption with
		/// the given params.
		/// </remarks>
		/// <param name="buffer">encoded page buffer</param>
		/// <param name="params">RC4 decryption parameters</param>
		protected internal virtual void DecodePage(ByteBuffer buffer, KeyParameter @params
			)
		{
			RC4Engine engine = GetEngine();
			engine.Init(false, @params);
			byte[] array = ((byte[])buffer.Array());
			engine.ProcessBytes(array, 0, array.Length, array, 0);
		}

		public virtual ByteBuffer EncodePage(ByteBuffer buffer, int pageNumber, int pageOffset
			)
		{
			throw new NotSupportedException("Encryption is currently not supported");
		}

		/// <summary>Reads and returns the header page (page 0) from the given pageChannel.</summary>
		/// <remarks>Reads and returns the header page (page 0) from the given pageChannel.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal static ByteBuffer ReadHeaderPage(PageChannel pageChannel)
		{
			ByteBuffer buffer = pageChannel.CreatePageBuffer();
			pageChannel.ReadPage(buffer, 0);
			return buffer;
		}

		/// <summary>
		/// Returns a copy of the given key with the bytes of the given pageNumber
		/// applied at the given offset using XOR.
		/// </summary>
		/// <remarks>
		/// Returns a copy of the given key with the bytes of the given pageNumber
		/// applied at the given offset using XOR.
		/// </remarks>
		protected internal static byte[] ApplyPageNumber(byte[] key, int offset, int pageNumber
			)
		{
			byte[] tmp = ByteUtil.CopyOf(key, key.Length);
			ByteBuffer bb = ByteBuffer.Wrap(tmp).Order(PageChannel.DEFAULT_BYTE_ORDER);
			bb.Position(offset);
			bb.PutInt(pageNumber);
			for (int i = offset; i < (offset + 4); ++i)
			{
				tmp[i] ^= key[i];
			}
			return tmp;
		}

		public abstract void DecodePage(ByteBuffer arg1, int arg2);
	}
}
