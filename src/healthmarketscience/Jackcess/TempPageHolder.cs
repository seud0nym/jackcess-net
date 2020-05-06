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
	/// <summary>Manages a reference to a page buffer.</summary>
	/// <remarks>Manages a reference to a page buffer.</remarks>
	/// <author>James Ahlborn</author>
	public sealed class TempPageHolder
	{
		private int _pageNumber = PageChannel.INVALID_PAGE_NUMBER;

		private readonly TempBufferHolder _buffer;

		/// <summary>the last "modification" count of the buffer that this holder observed.</summary>
		/// <remarks>
		/// the last "modification" count of the buffer that this holder observed.
		/// this is tracked so that the page data can be re-read if the underlying
		/// buffer has been discarded since the last page read
		/// </remarks>
		private int _bufferModCount;

		private TempPageHolder(TempBufferHolder.Type type)
		{
			_buffer = TempBufferHolder.NewHolder(type, false);
			_bufferModCount = _buffer.GetModCount();
		}

		/// <summary>Creates a new TempPageHolder.</summary>
		/// <remarks>Creates a new TempPageHolder.</remarks>
		/// <param name="type">the type of reference desired for any create page buffers</param>
		public static HealthMarketScience.Jackcess.TempPageHolder NewHolder(TempBufferHolder.Type
			 type)
		{
			return new HealthMarketScience.Jackcess.TempPageHolder(type);
		}

		/// <returns>the currently set page number</returns>
		public int GetPageNumber()
		{
			return _pageNumber;
		}

		/// <returns>
		/// the page for the current page number, reading as necessary,
		/// position and limit are unchanged
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public ByteBuffer GetPage(PageChannel pageChannel)
		{
			return SetPage(pageChannel, _pageNumber, false);
		}

		/// <summary>Sets the current page number and returns that page</summary>
		/// <returns>
		/// the page for the new page number, reading as necessary, resets
		/// position
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public ByteBuffer SetPage(PageChannel pageChannel, int pageNumber)
		{
			return SetPage(pageChannel, pageNumber, true);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ByteBuffer SetPage(PageChannel pageChannel, int pageNumber, bool rewind)
		{
			ByteBuffer buffer = _buffer.GetPageBuffer(pageChannel);
			int modCount = _buffer.GetModCount();
			if ((pageNumber != _pageNumber) || (_bufferModCount != modCount))
			{
				_pageNumber = pageNumber;
				_bufferModCount = modCount;
				pageChannel.ReadPage(buffer, _pageNumber);
			}
			else
			{
				if (rewind)
				{
					buffer.Rewind();
				}
			}
			return buffer;
		}

		/// <summary>
		/// Allocates a new buffer in the database (with undefined data) and returns
		/// a new empty buffer.
		/// </summary>
		/// <remarks>
		/// Allocates a new buffer in the database (with undefined data) and returns
		/// a new empty buffer.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public ByteBuffer SetNewPage(PageChannel pageChannel)
		{
			// ditch any current data
			Clear();
			// allocate a new page in the database
			_pageNumber = pageChannel.AllocateNewPage();
			// return a new buffer
			return _buffer.GetPageBuffer(pageChannel);
		}

		/// <summary>
		/// Forces any current page data to be disregarded (any
		/// <code>getPage</code>/<code>setPage</code> call must reload page data).
		/// </summary>
		/// <remarks>
		/// Forces any current page data to be disregarded (any
		/// <code>getPage</code>/<code>setPage</code> call must reload page data).
		/// Does not necessarily release any memory.
		/// </remarks>
		public void Invalidate()
		{
			PossiblyInvalidate(_pageNumber, null);
		}

		/// <summary>
		/// Forces any current page data to be disregarded if it matches the given
		/// page number (any <code>getPage</code>/<code>setPage</code> call must
		/// reload page data) and is not the given buffer.
		/// </summary>
		/// <remarks>
		/// Forces any current page data to be disregarded if it matches the given
		/// page number (any <code>getPage</code>/<code>setPage</code> call must
		/// reload page data) and is not the given buffer.  Does not necessarily
		/// release any memory.
		/// </remarks>
		public void PossiblyInvalidate(int modifiedPageNumber, ByteBuffer modifiedBuffer)
		{
			if (modifiedBuffer == _buffer.GetExistingBuffer())
			{
				// no worries, our buffer was the one modified (or is null, either way
				// we'll need to reload)
				return;
			}
			if (modifiedPageNumber == _pageNumber)
			{
				_pageNumber = PageChannel.INVALID_PAGE_NUMBER;
			}
		}

		/// <summary>
		/// Forces any current page data to be disregarded (any
		/// <code>getPage</code>/<code>setPage</code> call must reload page data) and
		/// releases any referenced memory.
		/// </summary>
		/// <remarks>
		/// Forces any current page data to be disregarded (any
		/// <code>getPage</code>/<code>setPage</code> call must reload page data) and
		/// releases any referenced memory.
		/// </remarks>
		public void Clear()
		{
			Invalidate();
			_buffer.Clear();
		}
	}
}
