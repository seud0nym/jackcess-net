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
using System.Text;
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>Manager of the index pages for a BigIndex.</summary>
	/// <remarks>Manager of the index pages for a BigIndex.</remarks>
	/// <author>James Ahlborn</author>
	public class IndexPageCache
	{
		private enum UpdateType
		{
			ADD,
			REMOVE,
			REPLACE
		}

		/// <summary>the index whose pages this cache is managing</summary>
		private readonly BigIndexData _indexData;

		/// <summary>the root page for the index</summary>
		private IndexPageCache.DataPageMain _rootPage;

		/// <summary>the currently loaded pages for this index, pageNumber -&gt; page</summary>
		private readonly IDictionary<int, IndexPageCache.DataPageMain> _dataPages = new Dictionary
			<int, IndexPageCache.DataPageMain>();

		/// <summary>the currently modified index pages</summary>
		private readonly IList<IndexPageCache.CacheDataPage> _modifiedPages = new AList<IndexPageCache.CacheDataPage
			>();

		public IndexPageCache(BigIndexData indexData)
		{
			_indexData = indexData;
		}

		public virtual BigIndexData GetIndexData()
		{
			return _indexData;
		}

		public virtual PageChannel GetPageChannel()
		{
			return GetIndexData().GetPageChannel();
		}

		/// <summary>Sets the root page for this index, must be called before normal usage.</summary>
		/// <remarks>Sets the root page for this index, must be called before normal usage.</remarks>
		/// <param name="pageNumber">the root page number</param>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void SetRootPageNumber(int pageNumber)
		{
			_rootPage = GetDataPage(pageNumber);
			// root page has no parent
			_rootPage.InitParentPage(IndexData.INVALID_INDEX_PAGE_NUMBER, false);
		}

		/// <summary>Writes any outstanding changes for this index to the file.</summary>
		/// <remarks>Writes any outstanding changes for this index to the file.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Write()
		{
			// first discard any empty pages
			HandleEmptyPages();
			// next, handle any necessary page splitting
			PreparePagesForWriting();
			// finally, write all the modified pages (which are not being deleted)
			WriteDataPages();
		}

		/// <summary>
		/// Handles any modified pages which are empty as the first pass during a
		/// <see cref="Write()">Write()</see>
		/// call.  All empty pages are removed from the _modifiedPages
		/// collection by this method.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		private void HandleEmptyPages()
		{
			for (Iterator<IndexPageCache.CacheDataPage> iter = _modifiedPages.Iterator(); iter
				.HasNext(); )
			{
				IndexPageCache.CacheDataPage cacheDataPage = iter.Next();
				if (cacheDataPage._extra._entryView.IsEmpty())
				{
					if (!cacheDataPage._main.IsRoot())
					{
						DeleteDataPage(cacheDataPage);
					}
					else
					{
						WriteDataPage(cacheDataPage);
					}
					iter.Remove();
				}
			}
		}

		/// <summary>
		/// Prepares any non-empty modified pages for writing as the second pass
		/// during a
		/// <see cref="Write()">Write()</see>
		/// call.  Updates entry prefixes, promotes/demotes
		/// tail pages, and splits pages as needed.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		private void PreparePagesForWriting()
		{
			bool splitPages = false;
			int maxPageEntrySize = GetIndexData().GetMaxPageEntrySize();
			do
			{
				// we need to continue looping through all the pages until we do not split
				// any pages (because a split may cascade up the tree)
				splitPages = false;
				// we might be adding to this list while iterating, so we can't use an
				// iterator
				for (int i = 0; i < _modifiedPages.Count; ++i)
				{
					IndexPageCache.CacheDataPage cacheDataPage = _modifiedPages[i];
					if (!cacheDataPage.IsLeaf())
					{
						// see if we need to update any child tail status
						IndexPageCache.DataPageMain dpMain = cacheDataPage._main;
						int size = cacheDataPage._extra._entryView.Count;
						if (dpMain.HasChildTail())
						{
							if (size == 1)
							{
								DemoteTail(cacheDataPage);
							}
						}
						else
						{
							if (size > 1)
							{
								PromoteTail(cacheDataPage);
							}
						}
					}
					// look for pages with more entries than can fit on a page
					if (cacheDataPage.GetTotalEntrySize() > maxPageEntrySize)
					{
						// make sure the prefix is up-to-date (this may have gotten
						// discarded by one of the update entry methods)
						cacheDataPage._extra.UpdateEntryPrefix();
						// now, see if the page will fit when compressed
						if (cacheDataPage.GetCompressedEntrySize() > maxPageEntrySize)
						{
							// need to split this page
							splitPages = true;
							SplitDataPage(cacheDataPage);
						}
					}
				}
			}
			while (splitPages);
		}

		/// <summary>
		/// Writes any non-empty modified pages as the last pass during a
		/// <see cref="Write()">Write()</see>
		/// call.  Clears the _modifiedPages collection when finised.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		private void WriteDataPages()
		{
			foreach (IndexPageCache.CacheDataPage cacheDataPage in _modifiedPages)
			{
				if (cacheDataPage._extra._entryView.IsEmpty())
				{
					throw new InvalidOperationException("Unexpected empty page " + cacheDataPage);
				}
				WriteDataPage(cacheDataPage);
			}
			_modifiedPages.Clear();
		}

		/// <summary>
		/// Returns a CacheDataPage for the given page number, may be
		/// <code>null</code>
		/// if
		/// the given page number is invalid.  Loads the given page if necessary.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual IndexPageCache.CacheDataPage GetCacheDataPage(int pageNumber)
		{
			IndexPageCache.DataPageMain main = GetDataPage(pageNumber);
			return ((main != null) ? new IndexPageCache.CacheDataPage(main) : null);
		}

		/// <summary>
		/// Returns a DataPageMain for the given page number, may be
		/// <code>null</code>
		/// if
		/// the given page number is invalid.  Loads the given page if necessary.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		private IndexPageCache.DataPageMain GetDataPage(int? pageNumber)
		{
			IndexPageCache.DataPageMain dataPage = null;
			if (pageNumber.HasValue)
			{
				dataPage = _dataPages.Get(pageNumber.Value);
				if ((dataPage == null) && (pageNumber.Value > IndexData.INVALID_INDEX_PAGE_NUMBER))
				{
					dataPage = ReadDataPage(pageNumber.Value)._main;
					_dataPages.Put(pageNumber.Value, dataPage);
				}
			}
			return dataPage;
		}

		/// <summary>Writes the given index page to the file.</summary>
		/// <remarks>Writes the given index page to the file.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void WriteDataPage(IndexPageCache.CacheDataPage cacheDataPage)
		{
			GetIndexData().WriteDataPage(cacheDataPage);
			// lastly, mark the page as no longer modified
			cacheDataPage._extra._modified = false;
		}

		/// <summary>Deletes the given index page from the file (clears the page).</summary>
		/// <remarks>Deletes the given index page from the file (clears the page).</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void DeleteDataPage(IndexPageCache.CacheDataPage cacheDataPage)
		{
			// free this database page
			GetPageChannel().DeallocatePage(cacheDataPage._main._pageNumber);
			// discard from our cache
			Sharpen.Collections.Remove(_dataPages, cacheDataPage._main._pageNumber);
			// lastly, mark the page as no longer modified
			cacheDataPage._extra._modified = false;
		}

		/// <summary>Reads the given index page from the file.</summary>
		/// <remarks>Reads the given index page from the file.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private IndexPageCache.CacheDataPage ReadDataPage(int pageNumber)
		{
			IndexPageCache.DataPageMain dataPage = new IndexPageCache.DataPageMain(this, pageNumber
				);
			IndexPageCache.DataPageExtra extra = new IndexPageCache.DataPageExtra();
			IndexPageCache.CacheDataPage cacheDataPage = new IndexPageCache.CacheDataPage(dataPage
				, extra);
			GetIndexData().ReadDataPage(cacheDataPage);
			// associate the extra info with the main data page
			dataPage.SetExtra(extra);
			return cacheDataPage;
		}

		/// <summary>Removes the entry with the given index from the given page.</summary>
		/// <remarks>Removes the entry with the given index from the given page.</remarks>
		/// <param name="cacheDataPage">the page from which to remove the entry</param>
		/// <param name="entryIdx">the index of the entry to remove</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void RemoveEntry(IndexPageCache.CacheDataPage cacheDataPage, int entryIdx
			)
		{
			UpdateEntry(cacheDataPage, entryIdx, null, IndexPageCache.UpdateType.REMOVE);
		}

		/// <summary>Adds the entry to the given page at the given index.</summary>
		/// <remarks>Adds the entry to the given page at the given index.</remarks>
		/// <param name="cacheDataPage">the page to which to add the entry</param>
		/// <param name="entryIdx">the index at which to add the entry</param>
		/// <param name="newEntry">the entry to add</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void AddEntry(IndexPageCache.CacheDataPage cacheDataPage, int entryIdx, IndexData.Entry
			 newEntry)
		{
			UpdateEntry(cacheDataPage, entryIdx, newEntry, IndexPageCache.UpdateType.ADD);
		}

		/// <summary>Updates the entries on the given page according to the given updateType.
		/// 	</summary>
		/// <remarks>Updates the entries on the given page according to the given updateType.
		/// 	</remarks>
		/// <param name="cacheDataPage">the page to update</param>
		/// <param name="entryIdx">the index at which to add/remove/replace the entry</param>
		/// <param name="newEntry">the entry to add/replace</param>
		/// <param name="upType">the type of update to make</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void UpdateEntry(IndexPageCache.CacheDataPage cacheDataPage, int entryIdx
			, IndexData.Entry newEntry, IndexPageCache.UpdateType upType)
		{
			IndexPageCache.DataPageMain dpMain = cacheDataPage._main;
			IndexPageCache.DataPageExtra dpExtra = cacheDataPage._extra;
			if (newEntry != null)
			{
				ValidateEntryForPage(dpMain, newEntry);
			}
			// note, it's slightly ucky, but we need to load the parent page before we
			// start mucking with our entries because our parent may use our entries.
			IndexPageCache.CacheDataPage parentDataPage = (!dpMain.IsRoot() ? new IndexPageCache.CacheDataPage
				(dpMain.GetParentPage()) : null);
			IndexData.Entry oldLastEntry = dpExtra._entryView.GetLast();
			IndexData.Entry oldEntry = null;
			int entrySizeDiff = 0;
			switch (upType)
			{
				case IndexPageCache.UpdateType.ADD:
				{
					dpExtra._entryView.Add(entryIdx, newEntry);
					entrySizeDiff += newEntry.Size();
					break;
				}

				case IndexPageCache.UpdateType.REPLACE:
				{
					oldEntry = dpExtra._entryView.Set(entryIdx, newEntry);
					entrySizeDiff += newEntry.Size() - oldEntry.Size();
					break;
				}

				case IndexPageCache.UpdateType.REMOVE:
				{
					oldEntry = dpExtra._entryView.Remove(entryIdx);
					entrySizeDiff -= oldEntry.Size();
					break;
				}

				default:
				{
					throw new RuntimeException("unknown update type " + upType);
				}
			}
			bool updateLast = (oldLastEntry != dpExtra._entryView.GetLast());
			// child tail entry updates do not modify the page
			if (!updateLast || !dpMain.HasChildTail())
			{
				dpExtra._totalEntrySize += entrySizeDiff;
				SetModified(cacheDataPage);
				// for now, just clear the prefix, we'll fix it later
				dpExtra._entryPrefix = IndexData.EMPTY_PREFIX;
			}
			if (dpExtra._entryView.IsEmpty())
			{
				// this page is dead
				RemoveDataPage(parentDataPage, cacheDataPage, oldLastEntry);
				return;
			}
			// determine if we need to update our parent page 
			if (!updateLast || dpMain.IsRoot())
			{
				// no parent
				return;
			}
			// the update to the last entry needs to be propagated to our parent
			ReplaceParentEntry(parentDataPage, cacheDataPage, oldLastEntry);
		}

		/// <summary>Removes an index page which has become empty.</summary>
		/// <remarks>
		/// Removes an index page which has become empty.  If this page is the root
		/// page, just clears it.
		/// </remarks>
		/// <param name="parentDataPage">the parent of the removed page</param>
		/// <param name="cacheDataPage">the page to remove</param>
		/// <param name="oldLastEntry">the last entry for this page (before it was removed)</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void RemoveDataPage(IndexPageCache.CacheDataPage parentDataPage, IndexPageCache.CacheDataPage
			 cacheDataPage, IndexData.Entry oldLastEntry)
		{
			IndexPageCache.DataPageMain dpMain = cacheDataPage._main;
			IndexPageCache.DataPageExtra dpExtra = cacheDataPage._extra;
			if (dpMain.HasChildTail())
			{
				throw new InvalidOperationException("Still has child tail?");
			}
			if (dpExtra._totalEntrySize != 0)
			{
				throw new InvalidOperationException("Empty page but size is not 0? " + dpExtra._totalEntrySize
					 + ", " + cacheDataPage);
			}
			if (dpMain.IsRoot())
			{
				// clear out this page (we don't actually remove it)
				dpExtra._entryPrefix = IndexData.EMPTY_PREFIX;
				// when the root page becomes empty, it becomes a leaf page again
				dpMain._leaf = true;
				return;
			}
			// remove this page from its parent page
			UpdateParentEntry(parentDataPage, cacheDataPage, oldLastEntry, null, IndexPageCache.UpdateType
				.REMOVE);
			// remove this page from any next/prev pages
			RemoveFromPeers(cacheDataPage);
		}

		/// <summary>Removes a now empty index page from its next and previous peers.</summary>
		/// <remarks>Removes a now empty index page from its next and previous peers.</remarks>
		/// <param name="cacheDataPage">the page to remove</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void RemoveFromPeers(IndexPageCache.CacheDataPage cacheDataPage)
		{
			IndexPageCache.DataPageMain dpMain = cacheDataPage._main;
			int prevPageNumber = dpMain._prevPageNumber;
			int nextPageNumber = dpMain._nextPageNumber;
			IndexPageCache.DataPageMain prevMain = dpMain.GetPrevPage();
			if (prevMain != null)
			{
				SetModified(new IndexPageCache.CacheDataPage(prevMain));
				prevMain._nextPageNumber = nextPageNumber;
			}
			IndexPageCache.DataPageMain nextMain = dpMain.GetNextPage();
			if (nextMain != null)
			{
				SetModified(new IndexPageCache.CacheDataPage(nextMain));
				nextMain._prevPageNumber = prevPageNumber;
			}
		}

		/// <summary>Adds an entry for the given child page to the given parent page.</summary>
		/// <remarks>Adds an entry for the given child page to the given parent page.</remarks>
		/// <param name="parentDataPage">the parent page to which to add the entry</param>
		/// <param name="childDataPage">the child from which to get the entry to add</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void AddParentEntry(IndexPageCache.CacheDataPage parentDataPage, IndexPageCache.CacheDataPage
			 childDataPage)
		{
			IndexPageCache.DataPageExtra childExtra = childDataPage._extra;
			UpdateParentEntry(parentDataPage, childDataPage, null, childExtra._entryView.GetLast
				(), IndexPageCache.UpdateType.ADD);
		}

		/// <summary>Replaces the entry for the given child page in the given parent page.</summary>
		/// <remarks>Replaces the entry for the given child page in the given parent page.</remarks>
		/// <param name="parentDataPage">the parent page in which to replace the entry</param>
		/// <param name="childDataPage">the child for which the entry is being replaced</param>
		/// <param name="oldEntry">the old child entry for the child page</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void ReplaceParentEntry(IndexPageCache.CacheDataPage parentDataPage, IndexPageCache.CacheDataPage
			 childDataPage, IndexData.Entry oldEntry)
		{
			IndexPageCache.DataPageExtra childExtra = childDataPage._extra;
			UpdateParentEntry(parentDataPage, childDataPage, oldEntry, childExtra._entryView.
				GetLast(), IndexPageCache.UpdateType.REPLACE);
		}

		/// <summary>
		/// Updates the entry for the given child page in the given parent page
		/// according to the given updateType.
		/// </summary>
		/// <remarks>
		/// Updates the entry for the given child page in the given parent page
		/// according to the given updateType.
		/// </remarks>
		/// <param name="parentDataPage">the parent page in which to update the entry</param>
		/// <param name="childDataPage">the child for which the entry is being updated</param>
		/// <param name="oldEntry">the old child entry to remove/replace</param>
		/// <param name="newEntry">the new child entry to replace/add</param>
		/// <param name="upType">the type of update to make</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void UpdateParentEntry(IndexPageCache.CacheDataPage parentDataPage, IndexPageCache.CacheDataPage
			 childDataPage, IndexData.Entry oldEntry, IndexData.Entry newEntry, IndexPageCache.UpdateType
			 upType)
		{
			IndexPageCache.DataPageMain childMain = childDataPage._main;
			IndexPageCache.DataPageExtra parentExtra = parentDataPage._extra;
			if (childMain.IsTail() && (upType != IndexPageCache.UpdateType.REMOVE))
			{
				// for add or replace, update the child tail info before updating the
				// parent entries
				UpdateParentTail(parentDataPage, childDataPage, upType);
			}
			if (oldEntry != null)
			{
				oldEntry = oldEntry.AsNodeEntry(childMain._pageNumber);
			}
			if (newEntry != null)
			{
				newEntry = newEntry.AsNodeEntry(childMain._pageNumber);
			}
			bool expectFound = true;
			int idx = 0;
			switch (upType)
			{
				case IndexPageCache.UpdateType.ADD:
				{
					expectFound = false;
					idx = parentExtra._entryView.Find(newEntry);
					break;
				}

				case IndexPageCache.UpdateType.REPLACE:
				case IndexPageCache.UpdateType.REMOVE:
				{
					idx = parentExtra._entryView.Find(oldEntry);
					break;
				}

				default:
				{
					throw new RuntimeException("unknown update type " + upType);
				}
			}
			if (idx < 0)
			{
				if (expectFound)
				{
					throw new InvalidOperationException("Could not find child entry in parent; childEntry "
						 + oldEntry + "; parent " + parentDataPage);
				}
				idx = IndexData.MissingIndexToInsertionPoint(idx);
			}
			else
			{
				if (!expectFound)
				{
					throw new InvalidOperationException("Unexpectedly found child entry in parent; childEntry "
						 + newEntry + "; parent " + parentDataPage);
				}
			}
			UpdateEntry(parentDataPage, idx, newEntry, upType);
			if (childMain.IsTail() && (upType == IndexPageCache.UpdateType.REMOVE))
			{
				// for remove, update the child tail info after updating the parent
				// entries
				UpdateParentTail(parentDataPage, childDataPage, upType);
			}
		}

		/// <summary>
		/// Updates the child tail info in the given parent page according to the
		/// given updateType.
		/// </summary>
		/// <remarks>
		/// Updates the child tail info in the given parent page according to the
		/// given updateType.
		/// </remarks>
		/// <param name="parentDataPage">the parent page in which to update the child tail</param>
		/// <param name="childDataPage">the child to add/replace</param>
		/// <param name="upType">the type of update to make</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void UpdateParentTail(IndexPageCache.CacheDataPage parentDataPage, IndexPageCache.CacheDataPage
			 childDataPage, IndexPageCache.UpdateType upType)
		{
			IndexPageCache.DataPageMain parentMain = parentDataPage._main;
			int newChildTailPageNumber = ((upType == IndexPageCache.UpdateType.REMOVE) ? IndexData.INVALID_INDEX_PAGE_NUMBER
				 : childDataPage._main._pageNumber);
			if (!parentMain.IsChildTailPageNumber(newChildTailPageNumber))
			{
				SetModified(parentDataPage);
				parentMain._childTailPageNumber = newChildTailPageNumber;
			}
		}

		/// <summary>
		/// Verifies that the given entry type (node/leaf) is valid for the given
		/// page (node/leaf).
		/// </summary>
		/// <remarks>
		/// Verifies that the given entry type (node/leaf) is valid for the given
		/// page (node/leaf).
		/// </remarks>
		/// <param name="dpMain">the page to which the entry will be added</param>
		/// <param name="entry">the entry being added</param>
		/// <exception cref="System.InvalidOperationException">
		/// if the entry type does not match the page
		/// type
		/// </exception>
		private void ValidateEntryForPage(IndexPageCache.DataPageMain dpMain, IndexData.Entry
			 entry)
		{
			if (dpMain._leaf != entry.IsLeafEntry())
			{
				throw new InvalidOperationException("Trying to update page with wrong entry type; pageLeaf "
					 + dpMain._leaf + ", entryLeaf " + entry.IsLeafEntry());
			}
		}

		/// <summary>Splits an index page which has too many entries on it.</summary>
		/// <remarks>Splits an index page which has too many entries on it.</remarks>
		/// <param name="origDataPage">the page to split</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void SplitDataPage(IndexPageCache.CacheDataPage origDataPage)
		{
			IndexPageCache.DataPageMain origMain = origDataPage._main;
			IndexPageCache.DataPageExtra origExtra = origDataPage._extra;
			SetModified(origDataPage);
			int numEntries = origExtra._entries.Count;
			if (numEntries < 2)
			{
				throw new InvalidOperationException("Cannot split page with less than 2 entries "
					 + origDataPage);
			}
			if (origMain.IsRoot())
			{
				// we can't split the root page directly, so we need to put another page
				// between the root page and its sub-pages, and then split that page.
				IndexPageCache.CacheDataPage newDataPage = NestRootDataPage(origDataPage);
				// now, split this new page instead
				origDataPage = newDataPage;
				origMain = newDataPage._main;
				origExtra = newDataPage._extra;
			}
			// note, it's slightly ucky, but we need to load the parent page before we
			// start mucking with our entries because our parent may use our entries.
			IndexPageCache.DataPageMain parentMain = origMain.GetParentPage();
			IndexPageCache.CacheDataPage parentDataPage = new IndexPageCache.CacheDataPage(parentMain
				);
			// note, there are many, many ways this could be improved/tweaked.  for
			// now, we just want it to be functional...
			// so, we will naively move half the entries from one page to a new page.
			IndexPageCache.CacheDataPage newDataPage_1 = AllocateNewCacheDataPage(parentMain.
				_pageNumber, origMain._leaf);
			IndexPageCache.DataPageMain newMain = newDataPage_1._main;
			IndexPageCache.DataPageExtra newExtra = newDataPage_1._extra;
			IList<IndexData.Entry> headEntries = origExtra._entries.SubList(0, ((numEntries +
				 1) / 2));
			// move first half of the entries from old page to new page (so we do not
			// need to muck with any tail entries)
			foreach (IndexData.Entry headEntry in headEntries)
			{
				newExtra._totalEntrySize += headEntry.Size();
				newExtra._entries.AddItem(headEntry);
			}
			newExtra.SetEntryView(newMain);
			// remove the moved entries from the old page
			headEntries.Clear();
			origExtra._entryPrefix = IndexData.EMPTY_PREFIX;
			origExtra._totalEntrySize -= newExtra._totalEntrySize;
			// insert this new page between the old page and any previous page
			AddToPeersBefore(newDataPage_1, origDataPage);
			if (!newMain._leaf)
			{
				// reparent the children pages of the new page
				ReparentChildren(newDataPage_1);
				// if the children of this page are also node pages, then the next/prev
				// links should not cross parent boundaries (the leaf pages are linked
				// from beginning to end, but child node pages are only linked within
				// the same parent)
				IndexPageCache.DataPageMain childMain = newMain.GetChildPage(newExtra._entryView.
					GetLast());
				if (!childMain._leaf)
				{
					SeparateFromNextPeer(new IndexPageCache.CacheDataPage(childMain));
				}
			}
			// lastly, we need to add the new page to the parent page's entries
			AddParentEntry(parentDataPage, newDataPage_1);
		}

		/// <summary>
		/// Copies the current root page info into a new page and nests this page
		/// under the root page.
		/// </summary>
		/// <remarks>
		/// Copies the current root page info into a new page and nests this page
		/// under the root page.  This must be done when the root page needs to be
		/// split.
		/// </remarks>
		/// <param name="rootDataPage">the root data page</param>
		/// <returns>the newly created page nested under the root page</returns>
		/// <exception cref="System.IO.IOException"></exception>
		private IndexPageCache.CacheDataPage NestRootDataPage(IndexPageCache.CacheDataPage
			 rootDataPage)
		{
			IndexPageCache.DataPageMain rootMain = rootDataPage._main;
			IndexPageCache.DataPageExtra rootExtra = rootDataPage._extra;
			if (!rootMain.IsRoot())
			{
				throw new ArgumentException("should be called with root, duh");
			}
			IndexPageCache.CacheDataPage newDataPage = AllocateNewCacheDataPage(rootMain._pageNumber
				, rootMain._leaf);
			IndexPageCache.DataPageMain newMain = newDataPage._main;
			IndexPageCache.DataPageExtra newExtra = newDataPage._extra;
			// move entries to new page
			newMain._childTailPageNumber = rootMain._childTailPageNumber;
			newExtra._entries = rootExtra._entries;
			newExtra._entryPrefix = rootExtra._entryPrefix;
			newExtra._totalEntrySize = rootExtra._totalEntrySize;
			newExtra.SetEntryView(newMain);
			if (!newMain._leaf)
			{
				// we need to re-parent all the child pages
				ReparentChildren(newDataPage);
			}
			// clear the root page
			rootMain._leaf = false;
			rootMain._childTailPageNumber = IndexData.INVALID_INDEX_PAGE_NUMBER;
			rootExtra._entries = new AList<IndexData.Entry>();
			rootExtra._entryPrefix = IndexData.EMPTY_PREFIX;
			rootExtra._totalEntrySize = 0;
			rootExtra.SetEntryView(rootMain);
			// add the new page as the first child of the root page
			AddParentEntry(rootDataPage, newDataPage);
			return newDataPage;
		}

		/// <summary>Allocates a new index page with the given parent page and type.</summary>
		/// <remarks>Allocates a new index page with the given parent page and type.</remarks>
		/// <param name="parentPageNumber">the parent page for the new page</param>
		/// <param name="isLeaf">whether or not the new page is a leaf page</param>
		/// <returns>the newly created page</returns>
		/// <exception cref="System.IO.IOException"></exception>
		private IndexPageCache.CacheDataPage AllocateNewCacheDataPage(int parentPageNumber
			, bool isLeaf)
		{
			IndexPageCache.DataPageMain dpMain = new IndexPageCache.DataPageMain(this, GetPageChannel
				().AllocateNewPage());
			IndexPageCache.DataPageExtra dpExtra = new IndexPageCache.DataPageExtra();
			dpMain.InitParentPage(parentPageNumber, false);
			dpMain._leaf = isLeaf;
			dpMain._prevPageNumber = IndexData.INVALID_INDEX_PAGE_NUMBER;
			dpMain._nextPageNumber = IndexData.INVALID_INDEX_PAGE_NUMBER;
			dpMain._childTailPageNumber = IndexData.INVALID_INDEX_PAGE_NUMBER;
			dpExtra._entries = new AList<IndexData.Entry>();
			dpExtra._entryPrefix = IndexData.EMPTY_PREFIX;
			dpMain.SetExtra(dpExtra);
			// add to our page cache
			_dataPages.Put(dpMain._pageNumber, dpMain);
			// update owned pages cache
			_indexData.AddOwnedPage(dpMain._pageNumber);
			// needs to be written out
			IndexPageCache.CacheDataPage cacheDataPage = new IndexPageCache.CacheDataPage(dpMain
				, dpExtra);
			SetModified(cacheDataPage);
			return cacheDataPage;
		}

		/// <summary>
		/// Inserts the new page as a peer between the given original page and any
		/// previous peer page.
		/// </summary>
		/// <remarks>
		/// Inserts the new page as a peer between the given original page and any
		/// previous peer page.
		/// </remarks>
		/// <param name="newDataPage">the new index page</param>
		/// <param name="origDataPage">the current index page</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void AddToPeersBefore(IndexPageCache.CacheDataPage newDataPage, IndexPageCache.CacheDataPage
			 origDataPage)
		{
			IndexPageCache.DataPageMain origMain = origDataPage._main;
			IndexPageCache.DataPageMain newMain = newDataPage._main;
			IndexPageCache.DataPageMain prevMain = origMain.GetPrevPage();
			newMain._nextPageNumber = origMain._pageNumber;
			newMain._prevPageNumber = origMain._prevPageNumber;
			origMain._prevPageNumber = newMain._pageNumber;
			if (prevMain != null)
			{
				SetModified(new IndexPageCache.CacheDataPage(prevMain));
				prevMain._nextPageNumber = newMain._pageNumber;
			}
		}

		/// <summary>Separates the given index page from any next peer page.</summary>
		/// <remarks>Separates the given index page from any next peer page.</remarks>
		/// <param name="cacheDataPage">the index page to be separated</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void SeparateFromNextPeer(IndexPageCache.CacheDataPage cacheDataPage)
		{
			IndexPageCache.DataPageMain dpMain = cacheDataPage._main;
			SetModified(cacheDataPage);
			IndexPageCache.DataPageMain nextMain = dpMain.GetNextPage();
			SetModified(new IndexPageCache.CacheDataPage(nextMain));
			nextMain._prevPageNumber = IndexData.INVALID_INDEX_PAGE_NUMBER;
			dpMain._nextPageNumber = IndexData.INVALID_INDEX_PAGE_NUMBER;
		}

		/// <summary>
		/// Sets the parent info for the children of the given page to the given
		/// page.
		/// </summary>
		/// <remarks>
		/// Sets the parent info for the children of the given page to the given
		/// page.
		/// </remarks>
		/// <param name="cacheDataPage">the page whose children need to be updated</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void ReparentChildren(IndexPageCache.CacheDataPage cacheDataPage)
		{
			IndexPageCache.DataPageMain dpMain = cacheDataPage._main;
			IndexPageCache.DataPageExtra dpExtra = cacheDataPage._extra;
			// note, the "parent" page number is not actually persisted, so we do not
			// need to mark any updated pages as modified.  for the same reason, we
			// don't need to load the pages if not already loaded
			foreach (IndexData.Entry entry in dpExtra._entryView)
			{
				int childPageNumber = entry.GetSubPageNumber();
				IndexPageCache.DataPageMain childMain = _dataPages.Get(childPageNumber);
				if (childMain != null)
				{
					childMain.SetParentPage(dpMain._pageNumber, dpMain.IsChildTailPageNumber(childPageNumber
						));
				}
			}
		}

		/// <summary>
		/// Makes the tail entry of the given page a normal entry on that page, done
		/// when there is only one entry left on a page, and it is the tail.
		/// </summary>
		/// <remarks>
		/// Makes the tail entry of the given page a normal entry on that page, done
		/// when there is only one entry left on a page, and it is the tail.
		/// </remarks>
		/// <param name="cacheDataPage">the page whose tail must be updated</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void DemoteTail(IndexPageCache.CacheDataPage cacheDataPage)
		{
			// there's only one entry on the page, and it's the tail.  make it a
			// normal entry
			IndexPageCache.DataPageMain dpMain = cacheDataPage._main;
			IndexPageCache.DataPageExtra dpExtra = cacheDataPage._extra;
			SetModified(cacheDataPage);
			IndexPageCache.DataPageMain tailMain = dpMain.GetChildTailPage();
			IndexPageCache.CacheDataPage tailDataPage = new IndexPageCache.CacheDataPage(tailMain
				);
			// move the tail entry to the last normal entry
			UpdateParentTail(cacheDataPage, tailDataPage, IndexPageCache.UpdateType.REMOVE);
			IndexData.Entry tailEntry = dpExtra._entryView.DemoteTail();
			dpExtra._totalEntrySize += tailEntry.Size();
			dpExtra._entryPrefix = IndexData.EMPTY_PREFIX;
			tailMain.SetParentPage(dpMain._pageNumber, false);
		}

		/// <summary>
		/// Makes the last normal entry of the given page the tail entry on that
		/// page, done when there are multiple entries on a page and no tail entry.
		/// </summary>
		/// <remarks>
		/// Makes the last normal entry of the given page the tail entry on that
		/// page, done when there are multiple entries on a page and no tail entry.
		/// </remarks>
		/// <param name="cacheDataPage">the page whose tail must be updated</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void PromoteTail(IndexPageCache.CacheDataPage cacheDataPage)
		{
			// there's not tail currently on this page, make last entry a tail
			IndexPageCache.DataPageMain dpMain = cacheDataPage._main;
			IndexPageCache.DataPageExtra dpExtra = cacheDataPage._extra;
			SetModified(cacheDataPage);
			IndexPageCache.DataPageMain lastMain = dpMain.GetChildPage(dpExtra._entryView.GetLast
				());
			IndexPageCache.CacheDataPage lastDataPage = new IndexPageCache.CacheDataPage(lastMain
				);
			// move the "last" normal entry to the tail entry
			UpdateParentTail(cacheDataPage, lastDataPage, IndexPageCache.UpdateType.ADD);
			IndexData.Entry lastEntry = dpExtra._entryView.PromoteTail();
			dpExtra._totalEntrySize -= lastEntry.Size();
			dpExtra._entryPrefix = IndexData.EMPTY_PREFIX;
			lastMain.SetParentPage(dpMain._pageNumber, true);
		}

		/// <summary>Finds the index page on which the given entry does or should reside.</summary>
		/// <remarks>Finds the index page on which the given entry does or should reside.</remarks>
		/// <param name="e">the entry to find</param>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual IndexPageCache.CacheDataPage FindCacheDataPage(IndexData.Entry e)
		{
			IndexPageCache.DataPageMain curPage = _rootPage;
			while (true)
			{
				if (curPage._leaf)
				{
					// nowhere to go from here
					return new IndexPageCache.CacheDataPage(curPage);
				}
				IndexPageCache.DataPageExtra extra = curPage.GetExtra();
				// need to descend
				int idx = extra._entryView.Find(e);
				if (idx < 0)
				{
					idx = IndexData.MissingIndexToInsertionPoint(idx);
					if (idx == extra._entryView.Count)
					{
						// just move to last child page
						--idx;
					}
				}
				IndexData.Entry nodeEntry = extra._entryView[idx];
				curPage = curPage.GetChildPage(nodeEntry);
			}
		}

		/// <summary>
		/// Marks the given index page as modified and saves it for writing, if
		/// necessary (if the page is already marked, does nothing).
		/// </summary>
		/// <remarks>
		/// Marks the given index page as modified and saves it for writing, if
		/// necessary (if the page is already marked, does nothing).
		/// </remarks>
		/// <param name="cacheDataPage">the modified index page</param>
		private void SetModified(IndexPageCache.CacheDataPage cacheDataPage)
		{
			if (!cacheDataPage._extra._modified)
			{
				_modifiedPages.AddItem(cacheDataPage);
				cacheDataPage._extra._modified = true;
			}
		}

		/// <summary>
		/// Finds the valid entry prefix given the first/last entries on an index
		/// page.
		/// </summary>
		/// <remarks>
		/// Finds the valid entry prefix given the first/last entries on an index
		/// page.
		/// </remarks>
		/// <param name="e1">the first entry on the page</param>
		/// <param name="e2">the last entry on the page</param>
		/// <returns>a valid entry prefix for the page</returns>
		private static byte[] FindCommonPrefix(IndexData.Entry e1, IndexData.Entry e2)
		{
			byte[] b1 = e1.GetEntryBytes();
			byte[] b2 = e2.GetEntryBytes();
			int maxLen = b1.Length;
			byte[] prefix = b1;
			if (b1.Length > b2.Length)
			{
				maxLen = b2.Length;
				prefix = b2;
			}
			int len = 0;
			while ((len < maxLen) && (b1[len] == b2[len]))
			{
				++len;
			}
			if (len < prefix.Length)
			{
				if (len == 0)
				{
					return IndexData.EMPTY_PREFIX;
				}
				// need new prefix
				prefix = ByteUtil.CopyOf(prefix, len);
			}
			return prefix;
		}

		/// <summary>Used by unit tests to validate the internal status of the index.</summary>
		/// <remarks>Used by unit tests to validate the internal status of the index.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Validate()
		{
			foreach (IndexPageCache.DataPageMain dpMain in _dataPages.Values)
			{
				IndexPageCache.DataPageExtra dpExtra = dpMain.GetExtra();
				ValidateEntries(dpExtra);
				ValidateChildren(dpMain, dpExtra);
				ValidatePeers(dpMain);
			}
		}

		/// <summary>Validates the entries for an index page</summary>
		/// <param name="dpExtra">the entries to validate</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void ValidateEntries(IndexPageCache.DataPageExtra dpExtra)
		{
			int entrySize = 0;
			IndexData.Entry prevEntry = IndexData.FIRST_ENTRY;
			foreach (IndexData.Entry e in dpExtra._entries)
			{
				entrySize += e.Size();
				if (prevEntry.CompareTo(e) >= 0)
				{
					throw new IOException("Unexpected order in index entries, " + prevEntry + " >= " 
						+ e);
				}
				prevEntry = e;
			}
			if (entrySize != dpExtra._totalEntrySize)
			{
				throw new InvalidOperationException("Expected size " + entrySize + " but was " + 
					dpExtra._totalEntrySize);
			}
		}

		/// <summary>Validates the children for an index page</summary>
		/// <param name="dpMain">the index page</param>
		/// <param name="dpExtra">the child entries to validate</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void ValidateChildren(IndexPageCache.DataPageMain dpMain, IndexPageCache.DataPageExtra
			 dpExtra)
		{
			int childTailPageNumber = dpMain._childTailPageNumber;
			if (dpMain._leaf)
			{
				if (childTailPageNumber != IndexData.INVALID_INDEX_PAGE_NUMBER)
				{
					throw new InvalidOperationException("Leaf page has tail " + dpMain);
				}
				return;
			}
			if ((dpExtra._entryView.Count == 1) && dpMain.HasChildTail())
			{
				throw new InvalidOperationException("Single child is tail " + dpMain);
			}
			foreach (IndexData.Entry e in dpExtra._entryView)
			{
				ValidateEntryForPage(dpMain, e);
				int subPageNumber = e.GetSubPageNumber();
				IndexPageCache.DataPageMain childMain = _dataPages.Get(subPageNumber);
				if (childMain != null)
				{
					if (childMain._parentPageNumber != null)
					{
						if ((int)childMain._parentPageNumber != dpMain._pageNumber)
						{
							throw new InvalidOperationException("Child's parent is incorrect " + childMain);
						}
						bool expectTail = ((int)subPageNumber == childTailPageNumber);
						if (expectTail != childMain._tail)
						{
							throw new InvalidOperationException("Child tail status incorrect " + childMain);
						}
					}
					IndexData.Entry lastEntry = childMain.GetExtra()._entryView.GetLast();
					if (e.CompareTo(lastEntry) != 0)
					{
						throw new InvalidOperationException("Invalid entry " + e + " but child is " + lastEntry
							);
					}
				}
			}
		}

		/// <summary>Validates the peer pages for an index page.</summary>
		/// <remarks>Validates the peer pages for an index page.</remarks>
		/// <param name="dpMain">the index page</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void ValidatePeers(IndexPageCache.DataPageMain dpMain)
		{
			IndexPageCache.DataPageMain prevMain = _dataPages.Get(dpMain._prevPageNumber);
			if (prevMain != null)
			{
				if ((int)prevMain._nextPageNumber != dpMain._pageNumber)
				{
					throw new InvalidOperationException("Prev page " + prevMain + " does not ref " + 
						dpMain);
				}
				ValidatePeerStatus(dpMain, prevMain);
			}
			IndexPageCache.DataPageMain nextMain = _dataPages.Get(dpMain._nextPageNumber);
			if (nextMain != null)
			{
				if ((int)nextMain._prevPageNumber != dpMain._pageNumber)
				{
					throw new InvalidOperationException("Next page " + nextMain + " does not ref " + 
						dpMain);
				}
				ValidatePeerStatus(dpMain, nextMain);
			}
		}

		/// <summary>Validates the given peer page against the given index page</summary>
		/// <param name="dpMain">the index page</param>
		/// <param name="peerMain">the peer index page</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void ValidatePeerStatus(IndexPageCache.DataPageMain dpMain, IndexPageCache.DataPageMain
			 peerMain)
		{
			if (dpMain._leaf != peerMain._leaf)
			{
				throw new InvalidOperationException("Mismatched peer status " + dpMain._leaf + " "
					 + peerMain._leaf);
			}
			if (!dpMain._leaf)
			{
				if ((dpMain._parentPageNumber != null) && (peerMain._parentPageNumber != null) &&
					 ((int)dpMain._parentPageNumber != (int)peerMain._parentPageNumber))
				{
					throw new InvalidOperationException("Mismatched node parents " + dpMain._parentPageNumber
						 + " " + peerMain._parentPageNumber);
				}
			}
		}

		/// <summary>Dumps the given index page to a StringBuilder</summary>
		/// <param name="rtn">the StringBuilder to update</param>
		/// <param name="dpMain">the index page to dump</param>
		private void DumpPage(StringBuilder rtn, IndexPageCache.DataPageMain dpMain)
		{
			try
			{
				IndexPageCache.CacheDataPage cacheDataPage = new IndexPageCache.CacheDataPage(dpMain
					);
				rtn.Append(cacheDataPage).Append("\n");
				if (!dpMain._leaf)
				{
					foreach (IndexData.Entry e in cacheDataPage._extra._entryView)
					{
						IndexPageCache.DataPageMain childMain = dpMain.GetChildPage(e);
						DumpPage(rtn, childMain);
					}
				}
			}
			catch (IOException e)
			{
				rtn.Append("Page[" + dpMain._pageNumber + "]: " + e);
			}
		}

		public override string ToString()
		{
			if (_rootPage == null)
			{
				return "Cache: (uninitialized)";
			}
			StringBuilder rtn = new StringBuilder("Cache: \n");
			DumpPage(rtn, _rootPage);
			return rtn.ToString();
		}

		/// <summary>Keeps track of the main info for an index page.</summary>
		/// <remarks>Keeps track of the main info for an index page.</remarks>
		public class DataPageMain
		{
			public readonly int _pageNumber;

			public int _prevPageNumber;

			public int _nextPageNumber;

			public int _childTailPageNumber;

			public int? _parentPageNumber;

			public bool _leaf;

			public bool _tail;

			private Reference<IndexPageCache.DataPageExtra> _extra;

			public DataPageMain(IndexPageCache _enclosing, int pageNumber)
			{
				this._enclosing = _enclosing;
				this._pageNumber = pageNumber;
			}

			public virtual IndexPageCache GetCache()
			{
				return this._enclosing;
			}

			public virtual bool IsRoot()
			{
				return (this == this._enclosing._rootPage);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual bool IsTail()
			{
				this.ResolveParent();
				return this._tail;
			}

			public virtual bool HasChildTail()
			{
				return ((int)this._childTailPageNumber != IndexData.INVALID_INDEX_PAGE_NUMBER);
			}

			public virtual bool IsChildTailPageNumber(int pageNumber)
			{
				return ((int)this._childTailPageNumber == pageNumber);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual IndexPageCache.DataPageMain GetParentPage()
			{
				this.ResolveParent();
				return this._enclosing.GetDataPage(this._parentPageNumber);
			}

			public virtual void InitParentPage(int parentPageNumber, bool isTail)
			{
				// only set if not already set
				if (this._parentPageNumber == null)
				{
					this.SetParentPage(parentPageNumber, isTail);
				}
			}

			public virtual void SetParentPage(int parentPageNumber, bool isTail)
			{
				this._parentPageNumber = parentPageNumber;
				this._tail = isTail;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual IndexPageCache.DataPageMain GetPrevPage()
			{
				return this._enclosing.GetDataPage(this._prevPageNumber);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual IndexPageCache.DataPageMain GetNextPage()
			{
				return this._enclosing.GetDataPage(this._nextPageNumber);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual IndexPageCache.DataPageMain GetChildPage(IndexData.Entry e)
			{
				int childPageNumber = e.GetSubPageNumber();
				return this.GetChildPage(childPageNumber, this.IsChildTailPageNumber(childPageNumber
					));
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual IndexPageCache.DataPageMain GetChildTailPage()
			{
				return this.GetChildPage(this._childTailPageNumber, true);
			}

			/// <summary>
			/// Returns a child page for the given page number, updating its parent
			/// info if necessary.
			/// </summary>
			/// <remarks>
			/// Returns a child page for the given page number, updating its parent
			/// info if necessary.
			/// </remarks>
			/// <exception cref="System.IO.IOException"></exception>
			private IndexPageCache.DataPageMain GetChildPage(int childPageNumber, bool isTail
				)
			{
				IndexPageCache.DataPageMain child = this._enclosing.GetDataPage(childPageNumber);
				if (child != null)
				{
					// set the parent info for this child (if necessary)
					child.InitParentPage(this._pageNumber, isTail);
				}
				return child;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual IndexPageCache.DataPageExtra GetExtra()
			{
				IndexPageCache.DataPageExtra extra = this._extra.Get();
				if (extra == null)
				{
					extra = this._enclosing.ReadDataPage(this._pageNumber)._extra;
					this.SetExtra(extra);
				}
				return extra;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void SetExtra(IndexPageCache.DataPageExtra extra)
			{
				extra.SetEntryView(this);
				this._extra = new SoftReference<IndexPageCache.DataPageExtra>(extra);
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void ResolveParent()
			{
				if (this._parentPageNumber == null)
				{
					// the act of searching for the last entry should resolve any parent
					// pages along the path
					this._enclosing.FindCacheDataPage(this.GetExtra()._entryView.GetLast());
					if (this._parentPageNumber == null)
					{
						throw new InvalidOperationException("Parent was not resolved");
					}
				}
			}

			public override string ToString()
			{
				return (this._leaf ? "Leaf" : "Node") + "DPMain[" + this._pageNumber + "] " + this
					._prevPageNumber + ", " + this._nextPageNumber + ", (" + this._childTailPageNumber
					 + ")";
			}

			private readonly IndexPageCache _enclosing;
		}

		/// <summary>Keeps track of the extra info for an index page.</summary>
		/// <remarks>
		/// Keeps track of the extra info for an index page.  This info (if
		/// unmodified) may be re-read from disk as necessary.
		/// </remarks>
		public class DataPageExtra
		{
			/// <summary>sorted collection of index entries.</summary>
			/// <remarks>
			/// sorted collection of index entries.  this is kept in a list instead of
			/// a SortedSet because the SortedSet has lame traversal utilities
			/// </remarks>
			public IList<IndexData.Entry> _entries;

			public IndexPageCache.EntryListView _entryView;

			public byte[] _entryPrefix;

			public int _totalEntrySize;

			public bool _modified;

			public DataPageExtra()
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void SetEntryView(IndexPageCache.DataPageMain main)
			{
				_entryView = new IndexPageCache.EntryListView(main, this);
			}

			public virtual void UpdateEntryPrefix()
			{
				if (_entryPrefix.Length == 0)
				{
					// prefix is only related to *real* entries, tail not included
					_entryPrefix = FindCommonPrefix(_entries[0], _entries[_entries.Count - 1]);
				}
			}

			public override string ToString()
			{
				return "DPExtra: " + _entryView;
			}
		}

		/// <summary>
		/// IndexPageCache implementation of an Index
		/// <see cref="DataPage">DataPage</see>
		/// .
		/// </summary>
		public sealed class CacheDataPage : IndexData.DataPage
		{
			public readonly IndexPageCache.DataPageMain _main;

			public readonly IndexPageCache.DataPageExtra _extra;

			/// <exception cref="System.IO.IOException"></exception>
			public CacheDataPage(IndexPageCache.DataPageMain dataPage) : this(dataPage, dataPage
				.GetExtra())
			{
			}

			public CacheDataPage(IndexPageCache.DataPageMain dataPage, IndexPageCache.DataPageExtra
				 extra)
			{
				_main = dataPage;
				_extra = extra;
			}

			public override int GetPageNumber()
			{
				return _main._pageNumber;
			}

			public override bool IsLeaf()
			{
				return _main._leaf;
			}

			public override void SetLeaf(bool isLeaf)
			{
				_main._leaf = isLeaf;
			}

			public override int GetPrevPageNumber()
			{
				return _main._prevPageNumber;
			}

			public override void SetPrevPageNumber(int pageNumber)
			{
				_main._prevPageNumber = pageNumber;
			}

			public override int GetNextPageNumber()
			{
				return _main._nextPageNumber;
			}

			public override void SetNextPageNumber(int pageNumber)
			{
				_main._nextPageNumber = pageNumber;
			}

			public override int GetChildTailPageNumber()
			{
				return _main._childTailPageNumber;
			}

			public override void SetChildTailPageNumber(int pageNumber)
			{
				_main._childTailPageNumber = pageNumber;
			}

			public override int GetTotalEntrySize()
			{
				return _extra._totalEntrySize;
			}

			public override void SetTotalEntrySize(int totalSize)
			{
				_extra._totalEntrySize = totalSize;
			}

			public override byte[] GetEntryPrefix()
			{
				return _extra._entryPrefix;
			}

			public override void SetEntryPrefix(byte[] entryPrefix)
			{
				_extra._entryPrefix = entryPrefix;
			}

			public override IList<IndexData.Entry> GetEntries()
			{
				return _extra._entries;
			}

			public override void SetEntries(IList<IndexData.Entry> entries)
			{
				_extra._entries = entries;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddEntry(int idx, IndexData.Entry entry)
			{
				_main.GetCache().AddEntry(this, idx, entry);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void RemoveEntry(int idx)
			{
				_main.GetCache().RemoveEntry(this, idx);
			}
		}

		/// <summary>
		/// A view of an index page's entries which combines the normal entries and
		/// tail entry into one collection.
		/// </summary>
		/// <remarks>
		/// A view of an index page's entries which combines the normal entries and
		/// tail entry into one collection.
		/// </remarks>
		public class EntryListView : AbstractList<IndexData.Entry>, RandomAccess
		{
			private readonly IndexPageCache.DataPageExtra _extra;

			private IndexData.Entry _childTailEntry;

			/// <exception cref="System.IO.IOException"></exception>
			public EntryListView(IndexPageCache.DataPageMain main, IndexPageCache.DataPageExtra
				 extra)
			{
				if (main.HasChildTail())
				{
					_childTailEntry = main.GetChildTailPage().GetExtra()._entryView.GetLast().AsNodeEntry
						(main._childTailPageNumber);
				}
				_extra = extra;
			}

			private IList<IndexData.Entry> GetEntries()
			{
				return _extra._entries;
			}

			public override int Count
			{
				get
				{
					int size = GetEntries().Count;
					if (HasChildTail())
					{
						++size;
					}
					return size;
				}
			}

			public override IndexData.Entry Get(int idx)
			{
				return (IsCurrentChildTailIndex(idx) ? _childTailEntry : GetEntries()[idx]);
			}

			public override IndexData.Entry Set(int idx, IndexData.Entry newEntry)
			{
				return (IsCurrentChildTailIndex(idx) ? SetChildTailEntry(newEntry) : GetEntries()
					.Set(idx, newEntry));
			}

			public override void Add(int idx, IndexData.Entry newEntry)
			{
				// note, we will never add to the "tail" entry, that will always be
				// handled through promoteTail
				GetEntries().Add(idx, newEntry);
			}

			public override IndexData.Entry Remove(int idx)
			{
				return (IsCurrentChildTailIndex(idx) ? SetChildTailEntry(null) : GetEntries().Remove
					(idx));
			}

			public virtual IndexData.Entry SetChildTailEntry(IndexData.Entry newEntry)
			{
				IndexData.Entry old = _childTailEntry;
				_childTailEntry = newEntry;
				return old;
			}

			public virtual IndexData.Entry GetChildTailEntry()
			{
				return _childTailEntry;
			}

			private bool HasChildTail()
			{
				return (_childTailEntry != null);
			}

			private bool IsCurrentChildTailIndex(int idx)
			{
				return (idx == GetEntries().Count);
			}

			public virtual IndexData.Entry GetLast()
			{
				return (HasChildTail() ? _childTailEntry : (!GetEntries().IsEmpty() ? GetEntries(
					)[GetEntries().Count - 1] : null));
			}

			public virtual IndexData.Entry DemoteTail()
			{
				IndexData.Entry tail = _childTailEntry;
				_childTailEntry = null;
				GetEntries().AddItem(tail);
				return tail;
			}

			public virtual IndexData.Entry PromoteTail()
			{
				IndexData.Entry last = GetEntries().Remove(GetEntries().Count - 1);
				_childTailEntry = last;
				return last;
			}

			public virtual int Find(IndexData.Entry e)
			{
				return Sharpen.Collections.BinarySearch(this, e);
			}
		}
	}
}
