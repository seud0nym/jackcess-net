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
using System.IO;
using HealthMarketScience.Jackcess;
using HealthMarketScience.Jackcess.Scsu;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Reads and writes individual pages in a database file</summary>
	/// <author>Tim McCune</author>
	public class PageChannel : Channel, Flushable
	{
		internal const int INVALID_PAGE_NUMBER = -1;

		internal static readonly ByteOrder DEFAULT_BYTE_ORDER = ByteOrder.LITTLE_ENDIAN;

		/// <summary>invalid page header, used when deallocating old pages.</summary>
		/// <remarks>
		/// invalid page header, used when deallocating old pages.  data pages
		/// generally have 4 interesting bytes at the beginning which we want to
		/// reset.
		/// </remarks>
		private static readonly byte[] INVALID_PAGE_BYTE_HEADER = new byte[] { PageTypes.
			INVALID, unchecked((byte)0), unchecked((byte)0), unchecked((byte)0) };

		/// <summary>Global usage map always lives on page 1</summary>
		internal const int PAGE_GLOBAL_USAGE_MAP = 1;

		/// <summary>Global usage map always lives at row 0</summary>
		internal const int ROW_GLOBAL_USAGE_MAP = 0;

		/// <summary>Channel containing the database</summary>
		private readonly FileChannel _channel;

		/// <summary>Format of the database in the channel</summary>
		private readonly JetFormat _format;

		/// <summary>whether or not to force all writes to disk immediately</summary>
		private readonly bool _autoSync;

		/// <summary>buffer used when deallocating old pages.</summary>
		/// <remarks>
		/// buffer used when deallocating old pages.  data pages generally have 4
		/// interesting bytes at the beginning which we want to reset.
		/// </remarks>
		private readonly ByteBuffer _invalidPageBytes = ByteBuffer.Wrap(INVALID_PAGE_BYTE_HEADER
			);

		/// <summary>dummy buffer used when allocating new pages</summary>
		private readonly ByteBuffer _forceBytes = ByteBuffer.Allocate(1);

		/// <summary>Tracks free pages in the database.</summary>
		/// <remarks>Tracks free pages in the database.</remarks>
		private UsageMap _globalUsageMap;

		/// <summary>handler for the current database encoding type</summary>
		private CodecHandler _codecHandler = DefaultCodecProvider.DUMMY_HANDLER;

		/// <param name="channel">Channel containing the database</param>
		/// <param name="format">Format of the database in the channel</param>
		/// <exception cref="System.IO.IOException"></exception>
		public PageChannel(FileChannel channel, JetFormat format, bool autoSync)
		{
			_channel = channel;
			_format = format;
			_autoSync = autoSync;
		}

		/// <summary>Does second-stage initialization, must be called after construction.</summary>
		/// <remarks>Does second-stage initialization, must be called after construction.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Initialize(Database database, CodecProvider codecProvider)
		{
			// initialize page en/decoding support
			_codecHandler = codecProvider.CreateHandler(this, database.GetCharset());
			// note the global usage map is a special map where any page outside of
			// the current range is assumed to be "on"
			_globalUsageMap = UsageMap.Read(database, PAGE_GLOBAL_USAGE_MAP, ROW_GLOBAL_USAGE_MAP
				, true);
		}

		/// <summary>Only used by unit tests</summary>
		internal PageChannel(bool testing)
		{
			if (!testing)
			{
				throw new ArgumentException();
			}
			_channel = null;
			_format = JetFormat.VERSION_4;
			_autoSync = false;
		}

		public virtual JetFormat GetFormat()
		{
			return _format;
		}

		/// <summary>Returns the next page number based on the given file size.</summary>
		/// <remarks>Returns the next page number based on the given file size.</remarks>
		private int GetNextPageNumber(long size)
		{
			return (int)(size / GetFormat().PAGE_SIZE);
		}

		/// <summary>Returns the offset for a page within the file.</summary>
		/// <remarks>Returns the offset for a page within the file.</remarks>
		private long GetPageOffset(int pageNumber)
		{
			return ((long)pageNumber * (long)GetFormat().PAGE_SIZE);
		}

		/// <summary>Validates that the given pageNumber is valid for this database.</summary>
		/// <remarks>Validates that the given pageNumber is valid for this database.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void ValidatePageNumber(int pageNumber)
		{
			int nextPageNumber = GetNextPageNumber(_channel.Size());
			if ((pageNumber <= INVALID_PAGE_NUMBER) || (pageNumber >= nextPageNumber))
			{
				throw new InvalidOperationException("invalid page number " + pageNumber);
			}
		}

		/// <param name="buffer">Buffer to read the page into</param>
		/// <param name="pageNumber">Number of the page to read in (starting at 0)</param>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void ReadPage(ByteBuffer buffer, int pageNumber)
		{
			ValidatePageNumber(pageNumber);
			if (Debug.IsDebugEnabled())
			{
				Debug.Out("Reading in page " + Sharpen.Extensions.ToHexString(pageNumber));
			}
			buffer.Clear();
			int bytesRead = _channel.Read(buffer, (long)pageNumber * (long)GetFormat().PAGE_SIZE
				);
			buffer.Flip();
			if (bytesRead != GetFormat().PAGE_SIZE)
			{
				throw new IOException("Failed attempting to read " + GetFormat().PAGE_SIZE + " bytes from page "
					 + pageNumber + ", only read " + bytesRead);
			}
			if (pageNumber == 0)
			{
				// de-mask header (note, page 0 never has additional encoding)
				ApplyHeaderMask(buffer);
			}
			else
			{
				_codecHandler.DecodePage(buffer, pageNumber);
			}
		}

		/// <summary>Write a page to disk</summary>
		/// <param name="page">Page to write</param>
		/// <param name="pageNumber">Page number to write the page to</param>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void WritePage(ByteBuffer page, int pageNumber)
		{
			WritePage(page, pageNumber, 0);
		}

		/// <summary>Write a page (or part of a page) to disk</summary>
		/// <param name="page">Page to write</param>
		/// <param name="pageNumber">Page number to write the page to</param>
		/// <param name="pageOffset">
		/// offset within the page at which to start writing the
		/// page data
		/// </param>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void WritePage(ByteBuffer page, int pageNumber, int pageOffset)
		{
			ValidatePageNumber(pageNumber);
			page.Rewind();
			if ((page.Remaining() - pageOffset) > GetFormat().PAGE_SIZE)
			{
				throw new ArgumentException("Page buffer is too large, size " + (page.Remaining()
					 - pageOffset));
			}
			ByteBuffer encodedPage = page;
			if (pageNumber == 0)
			{
				// re-mask header
				ApplyHeaderMask(page);
			}
			else
			{
				// re-encode page
				encodedPage = _codecHandler.EncodePage(page, pageNumber, pageOffset);
			}
			try
			{
				encodedPage.Position(pageOffset);
				_channel.Write(encodedPage, (GetPageOffset(pageNumber) + pageOffset));
				if (_autoSync)
				{
					Flush();
				}
			}
			finally
			{
				if (pageNumber == 0)
				{
					// de-mask header
					ApplyHeaderMask(page);
				}
			}
		}

		/// <summary>Write a page to disk as a new page, appending it to the database</summary>
		/// <param name="page">Page to write</param>
		/// <returns>Page number at which the page was written</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual int WriteNewPage(ByteBuffer page)
		{
			long size = _channel.Size();
			if (size >= GetFormat().MAX_DATABASE_SIZE)
			{
				throw new IOException("Database is at maximum size " + GetFormat().MAX_DATABASE_SIZE
					);
			}
			if ((size % GetFormat().PAGE_SIZE) != 0L)
			{
				throw new IOException("Database corrupted, file size " + size + " is not multiple of page size "
					 + GetFormat().PAGE_SIZE);
			}
			page.Rewind();
			if (page.Remaining() > GetFormat().PAGE_SIZE)
			{
				throw new ArgumentException("Page buffer is too large, size " + page.Remaining());
			}
			// push the buffer to the end of the page, so that a full page's worth of
			// data is written regardless of the incoming buffer size (we use a tiny
			// buffer in allocateNewPage)
			int pageOffset = (GetFormat().PAGE_SIZE - page.Remaining());
			long offset = size + pageOffset;
			int pageNumber = GetNextPageNumber(size);
			_channel.Write(_codecHandler.EncodePage(page, pageNumber, pageOffset), offset);
			// note, we "force" page removal because we know that this is an unused
			// page (since we just added it to the file)
			_globalUsageMap.RemovePageNumber(pageNumber, true);
			return pageNumber;
		}

		/// <summary>Allocates a new page in the database.</summary>
		/// <remarks>
		/// Allocates a new page in the database.  Data in the page is undefined
		/// until it is written in a call to
		/// <see cref="WritePage(Sharpen.ByteBuffer, int)">WritePage(Sharpen.ByteBuffer, int)
		/// 	</see>
		/// .
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual int AllocateNewPage()
		{
			// this will force the file to be extended with mostly undefined bytes
			return WriteNewPage(_forceBytes);
		}

		/// <summary>Deallocate a previously used page in the database.</summary>
		/// <remarks>Deallocate a previously used page in the database.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void DeallocatePage(int pageNumber)
		{
			ValidatePageNumber(pageNumber);
			// don't write the whole page, just wipe out the header (which should be
			// enough to let us know if we accidentally try to use an invalid page)
			_invalidPageBytes.Rewind();
			_channel.Write(_invalidPageBytes, GetPageOffset(pageNumber));
			_globalUsageMap.AddPageNumber(pageNumber);
		}

		//force is done here
		/// <returns>A newly-allocated buffer that can be passed to readPage</returns>
		public virtual ByteBuffer CreatePageBuffer()
		{
			return CreateBuffer(GetFormat().PAGE_SIZE);
		}

		/// <returns>
		/// A newly-allocated buffer of the given size and DEFAULT_BYTE_ORDER
		/// byte order
		/// </returns>
		public virtual ByteBuffer CreateBuffer(int size)
		{
			return CreateBuffer(size, DEFAULT_BYTE_ORDER);
		}

		/// <returns>A newly-allocated buffer of the given size and byte order</returns>
		public virtual ByteBuffer CreateBuffer(int size, ByteOrder order)
		{
			return ByteBuffer.Allocate(size).Order(order);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Flush()
		{
			_channel.Force(true);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Close()
		{
			Flush();
			_channel.Close();
		}

		public virtual bool IsOpen()
		{
			return _channel.IsOpen();
		}

		/// <summary>Applies the XOR mask to the database header in the given buffer.</summary>
		/// <remarks>Applies the XOR mask to the database header in the given buffer.</remarks>
		private void ApplyHeaderMask(ByteBuffer buffer)
		{
			// de/re-obfuscate the header
			byte[] headerMask = _format.HEADER_MASK;
			for (int idx = 0; idx < headerMask.Length; ++idx)
			{
				int pos = idx + _format.OFFSET_MASKED_HEADER;
				byte b = unchecked((byte)(buffer.Get(pos) ^ headerMask[idx]));
				buffer.Put(pos, b);
			}
		}

		/// <returns>
		/// a duplicate of the current buffer narrowed to the given position
		/// and limit.  mark will be set at the current position.
		/// </returns>
		public static ByteBuffer NarrowBuffer(ByteBuffer buffer, int position, int limit)
		{
			return (ByteBuffer)buffer.Duplicate().Order(buffer.Order()).Clear().Limit(limit).
				Position(position).Mark();
		}
	}
}
