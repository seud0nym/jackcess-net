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
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>
	/// Implementation of CodecProvider with support for some forms of Microsoft
	/// Access and Microsoft Money file encryption.
	/// </summary>
	/// <remarks>
	/// Implementation of CodecProvider with support for some forms of Microsoft
	/// Access and Microsoft Money file encryption.
	/// </remarks>
	/// <author>Vladimir Berezniker</author>
	public class CryptCodecProvider : CodecProvider
	{
		private string _password;

		public CryptCodecProvider() : this(null)
		{
		}

		public CryptCodecProvider(string password)
		{
			_password = password;
		}

		public virtual string GetPassword()
		{
			return _password;
		}

		public virtual void SetPassword(string newPassword)
		{
			_password = newPassword;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual CodecHandler CreateHandler(PageChannel channel, Encoding charset)
		{
			JetFormat format = channel.GetFormat();
			switch (format.CODEC_TYPE)
			{
				case JetFormat.CodecType.NONE:
				{
					// no encoding, all good
					return DefaultCodecProvider.DUMMY_HANDLER;
				}

				case JetFormat.CodecType.JET:
				{
					return JetCryptCodecHandler.Create(channel);
				}

				case JetFormat.CodecType.MSISAM:
				{
					return MSISAMCryptCodecHandler.Create(GetPassword(), channel, charset);
				}

				default:
				{
					throw new RuntimeException("Unknown codec type " + format.CODEC_TYPE);
				}
			}
		}
	}
}
