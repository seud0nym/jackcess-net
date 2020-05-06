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
using System.Collections.Generic;
using System.IO;
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Simple implementation of an Access table index</summary>
	/// <author>Tim McCune</author>
	public class SimpleIndexData : IndexData
	{
		internal static readonly IndexData.DataPage NEW_ROOT_DATA_PAGE = new SimpleIndexData.SimpleDataPage
			(0, true, Collections.EmptyList<IndexData.Entry>());

		/// <summary>data for the single index page.</summary>
		/// <remarks>
		/// data for the single index page.  if this data came from multiple pages,
		/// the index is read-only.
		/// </remarks>
		private SimpleIndexData.SimpleDataPage _dataPage;

		protected internal SimpleIndexData(Table table, int number, int uniqueEntryCount, 
			int uniqueEntryCountOffset) : base(table, number, uniqueEntryCount, uniqueEntryCountOffset
			)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void UpdateImpl()
		{
			WriteDataPage(_dataPage);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void ReadIndexEntries()
		{
			// find first leaf page
			int nextPageNumber = GetRootPageNumber();
			SimpleIndexData.SimpleDataPage indexPage = null;
			while (true)
			{
				indexPage = new SimpleIndexData.SimpleDataPage(nextPageNumber);
				ReadDataPage(indexPage);
				if (!indexPage.IsLeaf())
				{
					// FIXME we can't modify this index at this point in time
					SetReadOnly();
					// found another node page
					if (!indexPage.GetEntries().IsEmpty())
					{
						nextPageNumber = indexPage.GetEntries()[0].GetSubPageNumber();
					}
					else
					{
						// try tail page
						nextPageNumber = indexPage.GetChildTailPageNumber();
					}
					indexPage = null;
				}
				else
				{
					// found first leaf
					break;
				}
			}
			// save the first leaf page
			_dataPage = indexPage;
			nextPageNumber = indexPage.GetNextPageNumber();
			_dataPage.SetNextPageNumber(INVALID_INDEX_PAGE_NUMBER);
			indexPage = null;
			// read all leaf pages.
			while (nextPageNumber != INVALID_INDEX_PAGE_NUMBER)
			{
				// FIXME we can't modify this index at this point in time
				SetReadOnly();
				// found another one
				indexPage = new SimpleIndexData.SimpleDataPage(nextPageNumber);
				ReadDataPage(indexPage);
				// since we read all the entries in sort order, we can insert them
				// directly into the entries list
				Sharpen.Collections.AddAll(_dataPage.GetEntries(), indexPage.GetEntries());
				int totalSize = (_dataPage.GetTotalEntrySize() + indexPage.GetTotalEntrySize());
				_dataPage.SetTotalEntrySize(totalSize);
				nextPageNumber = indexPage.GetNextPageNumber();
			}
			// check the entry order, just to be safe
			IList<IndexData.Entry> entries = _dataPage.GetEntries();
			for (int i = 0; i < (entries.Count - 1); ++i)
			{
				IndexData.Entry e1 = entries[i];
				IndexData.Entry e2 = entries[i + 1];
				if (e1.CompareTo(e2) > 0)
				{
					throw new IOException("Unexpected order in index entries, " + e1 + " is greater than "
						 + e2);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override IndexData.DataPage FindDataPage(IndexData.Entry entry
			)
		{
			return _dataPage;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override IndexData.DataPage GetDataPage(int pageNumber)
		{
			throw new NotSupportedException();
		}

		/// <summary>Simple implementation of a DataPage</summary>
		internal sealed class SimpleDataPage : IndexData.DataPage
		{
			private readonly int _pageNumber;

			private bool _leaf;

			private int _nextPageNumber;

			private int _totalEntrySize;

			private int _childTailPageNumber;

			private IList<IndexData.Entry> _entries;

			internal SimpleDataPage(int pageNumber) : this(pageNumber, false, null)
			{
			}

			internal SimpleDataPage(int pageNumber, bool leaf, IList<IndexData.Entry> entries)
			{
				_pageNumber = pageNumber;
				_leaf = leaf;
				_entries = entries;
			}

			public override int GetPageNumber()
			{
				return _pageNumber;
			}

			public override bool IsLeaf()
			{
				return _leaf;
			}

			public override void SetLeaf(bool isLeaf)
			{
				_leaf = isLeaf;
			}

			public override int GetPrevPageNumber()
			{
				return 0;
			}

			public override void SetPrevPageNumber(int pageNumber)
			{
			}

			// ignored
			public override int GetNextPageNumber()
			{
				return _nextPageNumber;
			}

			public override void SetNextPageNumber(int pageNumber)
			{
				_nextPageNumber = pageNumber;
			}

			public override int GetChildTailPageNumber()
			{
				return _childTailPageNumber;
			}

			public override void SetChildTailPageNumber(int pageNumber)
			{
				_childTailPageNumber = pageNumber;
			}

			public override int GetTotalEntrySize()
			{
				return _totalEntrySize;
			}

			public override void SetTotalEntrySize(int totalSize)
			{
				_totalEntrySize = totalSize;
			}

			public override byte[] GetEntryPrefix()
			{
				return EMPTY_PREFIX;
			}

			public override void SetEntryPrefix(byte[] entryPrefix)
			{
			}

			// ignored
			public override IList<IndexData.Entry> GetEntries()
			{
				return _entries;
			}

			public override void SetEntries(IList<IndexData.Entry> entries)
			{
				_entries = entries;
			}

			public override void AddEntry(int idx, IndexData.Entry entry)
			{
				_entries.Add(idx, entry);
				_totalEntrySize += entry.Size();
			}

			public override void RemoveEntry(int idx)
			{
				IndexData.Entry oldEntry = _entries.Remove(idx);
				_totalEntrySize -= oldEntry.Size();
			}
		}
	}
}
