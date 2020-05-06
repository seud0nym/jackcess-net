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
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>CodecHandler for MSISAM databases.</summary>
	/// <remarks>CodecHandler for MSISAM databases.</remarks>
	/// <author>Vladimir Berezniker</author>
	public class MSISAMCryptCodecHandler : BaseCryptCodecHandler
	{
		private const int SALT_OFFSET = unchecked((int)(0x72));

		private const int CRYPT_CHECK_START = unchecked((int)(0x2e9));

		private const int ENCRYPTION_FLAGS_OFFSET = unchecked((int)(0x298));

		private const int SALT_LENGTH = unchecked((int)(0x4));

		private const int PASSWORD_LENGTH = unchecked((int)(0x28));

		private const int USE_SHA1 = unchecked((int)(0x20));

		private const int PASSWORD_DIGEST_LENGTH = unchecked((int)(0x10));

		private const int MSISAM_MAX_ENCRYPTED_PAGE = unchecked((int)(0xE));

		private const int NEW_ENCRYPTION = unchecked((int)(0x6));

		private const int TRAILING_PWD_LEN = 20;

		private readonly byte[] _encodingKey;

		/// <exception cref="System.IO.IOException"></exception>
		internal MSISAMCryptCodecHandler(string password, Encoding charset, ByteBuffer buffer
			) : base()
		{
			// Modern encryption using hashing
			byte[] salt = new byte[8];
			buffer.Position(SALT_OFFSET);
			buffer.Get(salt);
			// create decryption key parts
			byte[] pwdDigest = CreatePasswordDigest(buffer, password, charset);
			byte[] baseSalt = Arrays.CopyOf(salt, SALT_LENGTH);
			// check password hash using decryption of a known sequence
			VerifyPassword(buffer, Concat(pwdDigest, salt), baseSalt);
			// create final key
			_encodingKey = Concat(pwdDigest, baseSalt);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static CodecHandler Create(string password, PageChannel channel, Encoding 
			charset)
		{
			ByteBuffer buffer = ReadHeaderPage(channel);
			if ((buffer.Get(ENCRYPTION_FLAGS_OFFSET) & NEW_ENCRYPTION) != 0)
			{
				return new HealthMarketScience.Jackcess.MSISAMCryptCodecHandler(password, charset
					, buffer);
			}
			// old MSISAM dbs use jet-style encryption w/ a different key
			return new _JetCryptCodecHandler_87(GetOldDecryptionKey(buffer, channel.GetFormat
				()));
		}

		private sealed class _JetCryptCodecHandler_87 : JetCryptCodecHandler
		{
			public _JetCryptCodecHandler_87(byte[] baseArg1) : base(baseArg1)
			{
			}

			protected internal override int GetMaxEncodedPage()
			{
				return HealthMarketScience.Jackcess.MSISAMCryptCodecHandler.MSISAM_MAX_ENCRYPTED_PAGE;
			}
		}

		public override void DecodePage(ByteBuffer buffer, int pageNumber)
		{
			if ((pageNumber == 0) || (pageNumber > MSISAM_MAX_ENCRYPTED_PAGE))
			{
				// not encoded
				return;
			}
			byte[] key = ApplyPageNumber(_encodingKey, PASSWORD_DIGEST_LENGTH, pageNumber);
			DecodePage(buffer, new KeyParameter(key));
		}

		private void VerifyPassword(ByteBuffer buffer, byte[] testEncodingKey, byte[] testBytes
			)
		{
			RC4Engine engine = GetEngine();
			engine.Init(false, new KeyParameter(testEncodingKey));
			byte[] encrypted4BytesCheck = GetPasswordTestBytes(buffer);
			byte[] decrypted4BytesCheck = new byte[4];
			engine.ProcessBytes(encrypted4BytesCheck, 0, encrypted4BytesCheck.Length, decrypted4BytesCheck
				, 0);
			if (!Arrays.Equals(decrypted4BytesCheck, testBytes))
			{
				throw new InvalidOperationException("Incorrect password provided");
			}
		}

		private static byte[] CreatePasswordDigest(ByteBuffer buffer, string password, Encoding
			 charset)
		{
			IDigest digest = (((buffer.Get(ENCRYPTION_FLAGS_OFFSET) & USE_SHA1) != 0) ? new Sha1Digest
				() : (IDigest)new MD5Digest());
			byte[] passwordBytes = new byte[PASSWORD_LENGTH];
			if (password != null)
			{
				ByteBuffer bb = Column.EncodeUncompressedText(password.ToUpper(), charset);
				bb.Get(passwordBytes, 0, Math.Min(passwordBytes.Length, bb.Remaining()));
			}
			digest.BlockUpdate(passwordBytes, 0, passwordBytes.Length);
			// Get digest value
			byte[] digestBytes = new byte[digest.GetDigestSize()];
			digest.DoFinal(digestBytes, 0);
			// Truncate to 128 bit to match Max key length as per MSDN
			if (digestBytes.Length != PASSWORD_DIGEST_LENGTH)
			{
				digestBytes = ByteUtil.CopyOf(digestBytes, PASSWORD_DIGEST_LENGTH);
			}
			return digestBytes;
		}

		private static byte[] GetOldDecryptionKey(ByteBuffer buffer, JetFormat format)
		{
			byte[] encodingKey = new byte[JetCryptCodecHandler.ENCODING_KEY_LENGTH];
			buffer.Position(SALT_OFFSET);
			buffer.Get(encodingKey);
			{
				// Hash the salt. Step 1.
				byte[] fullHashData = new byte[format.SIZE_PASSWORD * 2];
				buffer.Position(format.OFFSET_PASSWORD);
				buffer.Get(fullHashData);
				// apply additional mask to header data
				byte[] pwdMask = Database.GetPasswordMask(buffer, format);
				if (pwdMask != null)
				{
					for (int i = 0; i < format.SIZE_PASSWORD; ++i)
					{
						fullHashData[i] ^= pwdMask[i % pwdMask.Length];
					}
					int trailingOffset = fullHashData.Length - TRAILING_PWD_LEN;
					for (int i_1 = 0; i_1 < TRAILING_PWD_LEN; ++i_1)
					{
						fullHashData[trailingOffset + i_1] ^= pwdMask[i_1 % pwdMask.Length];
					}
				}
				byte[] hashData = new byte[format.SIZE_PASSWORD];
				for (int pos = 0; pos < format.SIZE_PASSWORD; pos++)
				{
					hashData[pos] = fullHashData[pos * 2];
				}
				HashSalt(encodingKey, hashData);
			}
			{
				// Hash the salt. Step 2
				byte[] jetHeader = new byte[JetFormat.LENGTH_ENGINE_NAME];
				buffer.Position(JetFormat.OFFSET_ENGINE_NAME);
				buffer.Get(jetHeader);
				HashSalt(encodingKey, jetHeader);
			}
			return encodingKey;
		}

		private static byte[] GetPasswordTestBytes(ByteBuffer buffer)
		{
			byte[] encrypted4BytesCheck = new byte[4];
			int cryptCheckOffset = ByteUtil.GetUnsignedByte(buffer, SALT_OFFSET);
			buffer.Position(CRYPT_CHECK_START + cryptCheckOffset);
			buffer.Get(encrypted4BytesCheck);
			return encrypted4BytesCheck;
		}

		private static byte[] Concat(byte[] b1, byte[] b2)
		{
			byte[] @out = new byte[b1.Length + b2.Length];
			System.Array.Copy(b1, 0, @out, 0, b1.Length);
			System.Array.Copy(b2, 0, @out, b1.Length, b2.Length);
			return @out;
		}

		private static void HashSalt(byte[] salt, byte[] hashData)
		{
			ByteBuffer bb = ByteBuffer.Wrap(salt).Order(PageChannel.DEFAULT_BYTE_ORDER);
			int hash = bb.GetInt();
			for (int pos = 0; pos < hashData.Length; pos++)
			{
				int tmp = hashData[pos] & unchecked((int)(0xFF));
				tmp <<= pos % unchecked((int)(0x18));
				hash ^= tmp;
			}
			bb.Rewind();
			bb.PutInt(hash);
		}
	}
}
