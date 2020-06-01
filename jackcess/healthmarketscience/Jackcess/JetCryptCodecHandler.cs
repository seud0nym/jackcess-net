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

using Org.BouncyCastle.Crypto.Parameters;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
    /// <summary>CodecHandler for Jet databases.</summary>
    /// <remarks>CodecHandler for Jet databases.</remarks>
    /// <author>Vladimir Berezniker</author>
    public class JetCryptCodecHandler : BaseCryptCodecHandler
    {
        internal const int ENCODING_KEY_LENGTH = unchecked((int)(0x4));

        private readonly byte[] _encodingKey;

        internal JetCryptCodecHandler(byte[] encodingKey) : base()
        {
            _encodingKey = encodingKey;
        }

        /// <exception cref="System.IO.IOException"></exception>
        public static CodecHandler Create(PageChannel channel)
        {
            ByteBuffer buffer = ReadHeaderPage(channel);
            JetFormat format = channel.GetFormat();
            byte[] encodingKey = new byte[ENCODING_KEY_LENGTH];
            buffer.Position(format.OFFSET_ENCODING_KEY);
            buffer.Get(encodingKey);
            bool clearData = true;
            foreach (byte byteVal in encodingKey)
            {
                if (byteVal != 0)
                {
                    clearData = false;
                }
            }
            return (clearData ? DefaultCodecProvider.DUMMY_HANDLER : new HealthMarketScience.Jackcess.JetCryptCodecHandler
                (encodingKey));
        }

        public override void DecodePage(ByteBuffer buffer, int pageNumber)
        {
            if ((pageNumber == 0) || (pageNumber > GetMaxEncodedPage()))
            {
                // not encoded
                return;
            }
            byte[] key = ApplyPageNumber(_encodingKey, 0, pageNumber);
            DecodePage(buffer, new KeyParameter(key));
        }

        protected internal virtual int GetMaxEncodedPage()
        {
            return int.MaxValue;
        }
    }
}
