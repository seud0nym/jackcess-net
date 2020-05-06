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
using Apache.Commons.Lang;
using HealthMarketScience.Jackcess;
using Sharpen;

namespace HealthMarketScience.Jackcess
{
	/// <summary>The RowFilter class encapsulates a filter test for a table row.</summary>
	/// <remarks>
	/// The RowFilter class encapsulates a filter test for a table row.  This can
	/// be used by the
	/// <see cref="Apply(Sharpen.Iterable{T})">Apply(Sharpen.Iterable&lt;T&gt;)</see>
	/// method to create an Iterable over a
	/// table which returns only rows matching some criteria.
	/// </remarks>
	/// <author>Patricia Donaldson, Xerox Corporation</author>
	public abstract class RowFilter
	{
		/// <summary>
		/// Returns
		/// <code>true</code>
		/// if the given table row matches the Filter criteria,
		/// <code>false</code>
		/// otherwise.
		/// </summary>
		/// <param name="row">current row to test for inclusion in the filter</param>
		public abstract bool Matches(IDictionary<string, object> row);

		/// <summary>
		/// Returns an iterable which filters the given iterable based on this
		/// filter.
		/// </summary>
		/// <remarks>
		/// Returns an iterable which filters the given iterable based on this
		/// filter.
		/// </remarks>
		/// <param name="iterable">row iterable to filter</param>
		/// <returns>a filtering iterable</returns>
		public virtual Iterable<IDictionary<string, object>> Apply(Iterable<IDictionary<string
			, object>> iterable)
		{
			return new RowFilter.FilterIterable(this, iterable);
		}

		/// <summary>Creates a filter based on a row pattern.</summary>
		/// <remarks>Creates a filter based on a row pattern.</remarks>
		/// <param name="rowPattern">
		/// Map from column names to the values to be matched.
		/// A table row will match the target if
		/// <code>ObjectUtils.equals(rowPattern.get(s), row.get(s))</code>
		/// for all column names in the pattern map.
		/// </param>
		/// <returns>
		/// a filter which matches table rows which match the values in the
		/// row pattern
		/// </returns>
		public static RowFilter MatchPattern(IDictionary<string, object> rowPattern)
		{
			return new _RowFilter_80(rowPattern);
		}

		private sealed class _RowFilter_80 : RowFilter
		{
			public _RowFilter_80(IDictionary<string, object> rowPattern)
			{
				this.rowPattern = rowPattern;
			}

			public override bool Matches(IDictionary<string, object> row)
			{
				foreach (KeyValuePair<string, object> e in rowPattern.EntrySet())
				{
					if (!ObjectUtils.Equals(e.Value, row.Get(e.Key)))
					{
						return false;
					}
				}
				return true;
			}

			private readonly IDictionary<string, object> rowPattern;
		}

		/// <summary>Creates a filter based on a single value row pattern.</summary>
		/// <remarks>Creates a filter based on a single value row pattern.</remarks>
		/// <param name="columnPattern">column to be matched</param>
		/// <param name="valuePattern">
		/// value to be matched.
		/// A table row will match the target if
		/// <code>ObjectUtils.equals(valuePattern, row.get(columnPattern.getName()))</code>
		/// .
		/// </param>
		/// <returns>
		/// a filter which matches table rows which match the value in the
		/// row pattern
		/// </returns>
		public static RowFilter MatchPattern(Column columnPattern, object valuePattern)
		{
			return new _RowFilter_106(valuePattern, columnPattern);
		}

		private sealed class _RowFilter_106 : RowFilter
		{
			public _RowFilter_106(object valuePattern, Column columnPattern)
			{
				this.valuePattern = valuePattern;
				this.columnPattern = columnPattern;
			}

			public override bool Matches(IDictionary<string, object> row)
			{
				return ObjectUtils.Equals(valuePattern, row.Get(columnPattern.GetName()));
			}

			private readonly object valuePattern;

			private readonly Column columnPattern;
		}

		/// <summary>
		/// Creates a filter which inverts the sense of the given filter (rows which
		/// are matched by the given filter will not be matched by the returned
		/// filter, and vice versa).
		/// </summary>
		/// <remarks>
		/// Creates a filter which inverts the sense of the given filter (rows which
		/// are matched by the given filter will not be matched by the returned
		/// filter, and vice versa).
		/// </remarks>
		/// <param name="filter">filter which to invert</param>
		/// <returns>a RowFilter which matches rows not matched by the given filter</returns>
		public static RowFilter Invert(RowFilter filter)
		{
			return new _RowFilter_126(filter);
		}

		private sealed class _RowFilter_126 : RowFilter
		{
			public _RowFilter_126(RowFilter filter)
			{
				this.filter = filter;
			}

			public override bool Matches(IDictionary<string, object> row)
			{
				return !filter.Matches(row);
			}

			private readonly RowFilter filter;
		}

		/// <summary>
		/// Returns an iterable which filters the given iterable based on the given
		/// rowFilter.
		/// </summary>
		/// <remarks>
		/// Returns an iterable which filters the given iterable based on the given
		/// rowFilter.
		/// </remarks>
		/// <param name="rowFilter">
		/// the filter criteria, may be
		/// <code>null</code>
		/// </param>
		/// <param name="iterable">row iterable to filter</param>
		/// <returns>
		/// a filtering iterable (or the given iterable if a
		/// <code>null</code>
		/// filter was given)
		/// </returns>
		public static Iterable<IDictionary<string, object>> Apply(RowFilter rowFilter, Iterable
			<IDictionary<string, object>> iterable)
		{
			return ((rowFilter != null) ? rowFilter.Apply(iterable) : iterable);
		}

		/// <summary>Iterable which creates a filtered view of a another row iterable.</summary>
		/// <remarks>Iterable which creates a filtered view of a another row iterable.</remarks>
		internal class FilterIterable : Iterable<IDictionary<string, object>>
		{
			private readonly Iterable<IDictionary<string, object>> _iterable;

			internal FilterIterable(RowFilter _enclosing, Iterable<IDictionary<string, object>
				> iterable)
			{
				this._enclosing = _enclosing;
				this._iterable = iterable;
			}

			/// <summary>
			/// Returns an iterator which iterates through the rows of the underlying
			/// iterable, returning only rows for which the
			/// <see cref="RowFilter.Matches(System.Collections.Generic.IDictionary{K, V})">RowFilter.Matches(System.Collections.Generic.IDictionary&lt;K, V&gt;)
			/// 	</see>
			/// method returns
			/// <code>true</code>
			/// </summary>
			public override Sharpen.Iterator<IDictionary<string, object>> Iterator()
			{
				return new _Iterator_174(this);
			}

			private sealed class _Iterator_174 : Sharpen.Iterator<IDictionary<string, object>
				>
			{
				public _Iterator_174(FilterIterable _enclosing)
				{
					this._enclosing = _enclosing;
					this._iter = this._enclosing._iterable.Iterator();
				}

				private readonly Sharpen.Iterator<IDictionary<string, object>> _iter;

				private IDictionary<string, object> _next;

				public override bool HasNext()
				{
					while (this._iter.HasNext())
					{
						this._next = this._iter.Next();
						if (this._enclosing._enclosing.Matches(this._next))
						{
							return true;
						}
					}
					this._next = null;
					return false;
				}

				public override IDictionary<string, object> Next()
				{
					return this._next;
				}

				public override void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly FilterIterable _enclosing;
			}

			private readonly RowFilter _enclosing;
		}
	}
}
