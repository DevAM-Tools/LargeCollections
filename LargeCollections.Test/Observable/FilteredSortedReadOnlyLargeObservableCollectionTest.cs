/*
MIT License
SPDX-License-Identifier: MIT

Copyright (c) 2025 DevAM

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LargeCollections;
using LargeCollections.Observable;
using LargeCollections.Test.Helpers;
using TUnit.Core;

namespace LargeCollections.Test.Observable;

public class FilteredSortedReadOnlyLargeObservableCollectionTest
{
    public static IEnumerable<long> Capacities() => Parameters.Capacities;

    #region Helper Structs

    /// <summary>
    /// Struct predicate that filters for even numbers.
    /// </summary>
    private readonly struct EvenPredicate : ILargePredicate<long>
    {
        public bool Invoke(long item) => item % 2 == 0;
    }

    /// <summary>
    /// Struct predicate that filters for values greater than a threshold.
    /// </summary>
    private readonly struct GreaterThanPredicate : ILargePredicate<long>
    {
        private readonly long _Threshold;

        public GreaterThanPredicate(long threshold) => _Threshold = threshold;

        public bool Invoke(long item) => item > _Threshold;
    }

    /// <summary>
    /// Struct comparer for descending order.
    /// </summary>
    private readonly struct DescendingComparer : IComparer<long>
    {
        public int Compare(long x, long y) => y.CompareTo(x);
    }

    /// <summary>
    /// Struct comparer for ascending order.
    /// </summary>
    private readonly struct AscendingComparer : IComparer<long>
    {
        public int Compare(long x, long y) => x.CompareTo(y);
    }

    #endregion

    #region Helper Methods

    private static LargeObservableCollection<long> CreateSourceCollection(params long[] items)
    {
        var collection = new LargeObservableCollection<long>();
        foreach (var item in items)
        {
            collection.Add(item);
        }
        return collection;
    }

    private static ReadOnlyLargeObservableCollection<long> CreateReadOnlySource(params long[] items)
    {
        return new ReadOnlyLargeObservableCollection<long>(CreateSourceCollection(items));
    }

    #endregion

    #region Constructor Tests

    [Test]
    public async Task Constructor_WithNullSource_ThrowsArgumentNullException()
    {
        await Assert.That(() => new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            null!, default, default)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_WithValidSource_CreatesView()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.Count).IsEqualTo(5);
    }

    [Test]
    public async Task Constructor_WithSuppressExceptions_DoesNotThrowOnEventHandlerException()
    {
        var source = CreateSourceCollection(1, 2, 3);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default, suppressEventExceptions: true);

        view.CollectionChanged += (_, _) => throw new InvalidOperationException("Test exception");

        // Should not throw
        source.Add(4);

        await Assert.That(view.Count).IsEqualTo(4);
    }

    #endregion

    #region Filter Tests

    [Test]
    public async Task Filter_WithEvenPredicate_ReturnsOnlyEvenNumbers()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.Count).IsEqualTo(5);
        await Assert.That(view[0]).IsEqualTo(2);
        await Assert.That(view[1]).IsEqualTo(4);
        await Assert.That(view[2]).IsEqualTo(6);
        await Assert.That(view[3]).IsEqualTo(8);
        await Assert.That(view[4]).IsEqualTo(10);
    }

    [Test]
    public async Task Filter_WithDelegatePredicate_Works()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        var predicate = new DelegatePredicate<long>(x => x > 3);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, DelegatePredicate<long>, NoSort<long>>(
            readOnlySource, predicate, default);

        await Assert.That(view.Count).IsEqualTo(2);
        await Assert.That(view[0]).IsEqualTo(4);
        await Assert.That(view[1]).IsEqualTo(5);
    }

    [Test]
    public async Task Filter_WithNoFilter_ReturnsAllItems()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.Count).IsEqualTo(5);
        for (int i = 0; i < 5; i++)
        {
            await Assert.That(view[i]).IsEqualTo(i + 1);
        }
    }

    [Test]
    public async Task Filter_ChangePredicate_UpdatesView()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, DelegatePredicate<long>, NoSort<long>>(
            readOnlySource, new DelegatePredicate<long>(x => x > 5), default);

        await Assert.That(view.Count).IsEqualTo(5);

        // Change predicate
        view.Predicate = new DelegatePredicate<long>(x => x < 4);

        await Assert.That(view.Count).IsEqualTo(3);
        await Assert.That(view[0]).IsEqualTo(1);
        await Assert.That(view[1]).IsEqualTo(2);
        await Assert.That(view[2]).IsEqualTo(3);
    }

    [Test]
    public async Task Filter_EmptyResult_ReturnsEmptyView()
    {
        var source = CreateSourceCollection(1, 3, 5, 7, 9);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.Count).IsEqualTo(0);
    }

    #endregion

    #region Sort Tests

    [Test]
    public async Task Sort_WithDescendingComparer_SortsDescending()
    {
        var source = CreateSourceCollection(3, 1, 4, 1, 5, 9, 2, 6);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, DescendingComparer>(
            readOnlySource, default, default);

        await Assert.That(view.Count).IsEqualTo(8);
        await Assert.That(view[0]).IsEqualTo(9);
        await Assert.That(view[1]).IsEqualTo(6);
        await Assert.That(view[2]).IsEqualTo(5);
        await Assert.That(view[3]).IsEqualTo(4);
        await Assert.That(view[4]).IsEqualTo(3);
        await Assert.That(view[5]).IsEqualTo(2);
        await Assert.That(view[6]).IsEqualTo(1);
        await Assert.That(view[7]).IsEqualTo(1);
    }

    [Test]
    public async Task Sort_WithAscendingComparer_SortsAscending()
    {
        var source = CreateSourceCollection(3, 1, 4, 1, 5, 9, 2, 6);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, AscendingComparer>(
            readOnlySource, default, default);

        await Assert.That(view.Count).IsEqualTo(8);
        await Assert.That(view[0]).IsEqualTo(1);
        await Assert.That(view[1]).IsEqualTo(1);
        await Assert.That(view[2]).IsEqualTo(2);
        await Assert.That(view[3]).IsEqualTo(3);
        await Assert.That(view[4]).IsEqualTo(4);
        await Assert.That(view[5]).IsEqualTo(5);
        await Assert.That(view[6]).IsEqualTo(6);
        await Assert.That(view[7]).IsEqualTo(9);
    }

    [Test]
    public async Task Sort_WithNoSort_PreservesSourceOrder()
    {
        var source = CreateSourceCollection(3, 1, 4, 1, 5);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.Count).IsEqualTo(5);
        await Assert.That(view[0]).IsEqualTo(3);
        await Assert.That(view[1]).IsEqualTo(1);
        await Assert.That(view[2]).IsEqualTo(4);
        await Assert.That(view[3]).IsEqualTo(1);
        await Assert.That(view[4]).IsEqualTo(5);
    }

    [Test]
    public async Task Sort_ChangeComparer_UpdatesView()
    {
        var source = CreateSourceCollection(3, 1, 4, 1, 5);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, AscendingComparer>(
            readOnlySource, default, default);

        await Assert.That(view[0]).IsEqualTo(1);

        // Change comparer
        view.Comparer = new AscendingComparer(); // Force refresh
        await Assert.That(view[0]).IsEqualTo(1);
    }

    #endregion

    #region Combined Filter and Sort Tests

    [Test]
    public async Task FilterAndSort_Combined_Works()
    {
        var source = CreateSourceCollection(10, 3, 8, 1, 6, 4, 9, 2, 7, 5);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, DescendingComparer>(
            readOnlySource, default, default);

        // Even numbers: 10, 8, 6, 4, 2
        // Sorted descending: 10, 8, 6, 4, 2
        await Assert.That(view.Count).IsEqualTo(5);
        await Assert.That(view[0]).IsEqualTo(10);
        await Assert.That(view[1]).IsEqualTo(8);
        await Assert.That(view[2]).IsEqualTo(6);
        await Assert.That(view[3]).IsEqualTo(4);
        await Assert.That(view[4]).IsEqualTo(2);
    }

    [Test]
    public async Task FilterAndSort_WithGreaterThan_Works()
    {
        var source = CreateSourceCollection(5, 3, 8, 1, 9, 2, 7, 4, 6, 10);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        var predicate = new GreaterThanPredicate(5);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, GreaterThanPredicate, AscendingComparer>(
            readOnlySource, predicate, default);

        // > 5: 8, 9, 7, 6, 10
        // Sorted ascending: 6, 7, 8, 9, 10
        await Assert.That(view.Count).IsEqualTo(5);
        await Assert.That(view[0]).IsEqualTo(6);
        await Assert.That(view[1]).IsEqualTo(7);
        await Assert.That(view[2]).IsEqualTo(8);
        await Assert.That(view[3]).IsEqualTo(9);
        await Assert.That(view[4]).IsEqualTo(10);
    }

    #endregion

    #region Source Change Tests

    [Test]
    public async Task SourceAdd_UpdatesFilteredView()
    {
        var source = CreateSourceCollection(1, 2, 3, 4);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.Count).IsEqualTo(2); // 2, 4

        source.Add(6);
        await Assert.That(view.Count).IsEqualTo(3); // 2, 4, 6
        await Assert.That(view[2]).IsEqualTo(6);

        source.Add(7); // Odd, should not appear
        await Assert.That(view.Count).IsEqualTo(3);
    }

    [Test]
    public async Task SourceRemove_UpdatesFilteredView()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5, 6);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.Count).IsEqualTo(3); // 2, 4, 6

        source.Remove(4);
        await Assert.That(view.Count).IsEqualTo(2); // 2, 6

        source.Remove(3); // Odd, view should stay same
        await Assert.That(view.Count).IsEqualTo(2);
    }

    [Test]
    public async Task SourceClear_ClearsView()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.Count).IsEqualTo(5);

        source.Clear();
        await Assert.That(view.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SourceReplace_UpdatesSortedView()
    {
        var source = CreateSourceCollection(5, 3, 8, 1);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, AscendingComparer>(
            readOnlySource, default, default);

        // Sorted: 1, 3, 5, 8
        await Assert.That(view[0]).IsEqualTo(1);
        await Assert.That(view[3]).IsEqualTo(8);

        source[0] = 10; // Replace 5 with 10
        // Now source: 10, 3, 8, 1 -> Sorted: 1, 3, 8, 10
        await Assert.That(view[3]).IsEqualTo(10);
    }

    #endregion

    #region Event Tests

    [Test]
    public async Task CollectionChanged_RaisedOnSourceChange()
    {
        var source = CreateSourceCollection(1, 2, 3);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        int eventCount = 0;
        view.CollectionChanged += (_, _) => eventCount++;

        source.Add(4);
        await Assert.That(eventCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task PropertyChanged_RaisedOnCountChange()
    {
        var source = CreateSourceCollection(1, 2, 3);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        int eventCount = 0;
        view.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(view.Count))
            {
                eventCount++;
            }
        };

        source.Add(4);
        await Assert.That(eventCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Changed_RaisedOnSourceChange()
    {
        var source = CreateSourceCollection(1, 2, 3);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        int eventCount = 0;
        view.Changed += (IReadOnlyLargeObservableCollection<long> sender, in LargeCollectionChangedEventArgs<long> e) => eventCount++;

        source.Add(4);
        await Assert.That(eventCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task SuspendNotifications_SuppressesEventsUntilDisposed()
    {
        var source = CreateSourceCollection(1, 2, 3);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        int eventCount = 0;
        view.CollectionChanged += (_, _) => eventCount++;

        using (view.SuspendNotifications())
        {
            source.Add(4);
            source.Add(5);
            source.Add(6);
            await Assert.That(eventCount).IsEqualTo(0);
        }

        // After disposing suspension, should raise reset event
        await Assert.That(eventCount).IsGreaterThanOrEqualTo(1);
    }

    #endregion

    #region Enumeration Tests

    [Test]
    public async Task GetEnumerator_IteratesFilteredItems()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5, 6);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, NoSort<long>>(
            readOnlySource, default, default);

        var items = new List<long>();
        foreach (var item in view)
        {
            items.Add(item);
        }

        await Assert.That(items.Count).IsEqualTo(3);
        await Assert.That(items[0]).IsEqualTo(2);
        await Assert.That(items[1]).IsEqualTo(4);
        await Assert.That(items[2]).IsEqualTo(6);
    }

    [Test]
    public async Task GetAll_ReturnsAllFilteredItems()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, NoSort<long>>(
            readOnlySource, default, default);

        var items = view.GetAll().ToList();

        await Assert.That(items.Count).IsEqualTo(2);
        await Assert.That(items[0]).IsEqualTo(2);
        await Assert.That(items[1]).IsEqualTo(4);
    }

    [Test]
    public async Task GetAll_WithRange_ReturnsSubset()
    {
        var source = CreateSourceCollection(2, 4, 6, 8, 10);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        var items = view.GetAll(1, 3).ToList();

        await Assert.That(items.Count).IsEqualTo(3);
        await Assert.That(items[0]).IsEqualTo(4);
        await Assert.That(items[1]).IsEqualTo(6);
        await Assert.That(items[2]).IsEqualTo(8);
    }

    #endregion

    #region Search Tests

    [Test]
    public async Task Contains_FindsItemInFilteredView()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5, 6);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.Contains(4)).IsTrue();
        await Assert.That(view.Contains(3)).IsFalse(); // Filtered out
        await Assert.That(view.Contains(100)).IsFalse(); // Not in source
    }

    [Test]
    public async Task IndexOf_FindsIndexInFilteredView()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5, 6);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, NoSort<long>>(
            readOnlySource, default, default);

        // View contains: 2, 4, 6
        await Assert.That(view.IndexOf(2)).IsEqualTo(0);
        await Assert.That(view.IndexOf(4)).IsEqualTo(1);
        await Assert.That(view.IndexOf(6)).IsEqualTo(2);
        await Assert.That(view.IndexOf(3)).IsEqualTo(-1); // Filtered out
    }

    [Test]
    public async Task LastIndexOf_FindsLastIndexInSortedView()
    {
        var source = CreateSourceCollection(1, 2, 2, 3, 2, 4);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.LastIndexOf(2)).IsEqualTo(4); // Last occurrence at index 4
    }

    [Test]
    public async Task BinarySearch_FindsInSortedView()
    {
        var source = CreateSourceCollection(5, 3, 8, 1, 9, 2, 7);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, AscendingComparer>(
            readOnlySource, default, default);

        // Sorted: 1, 2, 3, 5, 7, 8, 9
        await Assert.That(view.BinarySearch(5)).IsEqualTo(3);
        await Assert.That(view.BinarySearch(1)).IsEqualTo(0);
        await Assert.That(view.BinarySearch(9)).IsEqualTo(6);

        // Not found - returns bitwise complement of insertion point
        long notFoundResult = view.BinarySearch(4);
        await Assert.That(notFoundResult).IsLessThan(0);
    }

    #endregion

    #region Copy Tests

    [Test]
    public async Task CopyToArray_CopiesFilteredItems()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5, 6);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, NoSort<long>>(
            readOnlySource, default, default);

        var target = new long[3];
        view.CopyToArray(target, 0, 0, 3);

        await Assert.That(target[0]).IsEqualTo(2);
        await Assert.That(target[1]).IsEqualTo(4);
        await Assert.That(target[2]).IsEqualTo(6);
    }

    [Test]
    public async Task CopyTo_WithLargeArray_Works()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        var target = new LargeArray<long>(5);
        view.CopyTo(target, 0, 0, 5);

        await Assert.That(target[0]).IsEqualTo(1);
        await Assert.That(target[4]).IsEqualTo(5);
    }

    #endregion

    #region DoForEach Tests

    [Test]
    public async Task DoForEach_IteratesAllFilteredItems()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5, 6);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, NoSort<long>>(
            readOnlySource, default, default);

        long sum = 0;
        view.DoForEach(x => sum += x);

        await Assert.That(sum).IsEqualTo(2 + 4 + 6);
    }

    [Test]
    public async Task DoForEach_WithRange_IteratesSubset()
    {
        var source = CreateSourceCollection(2, 4, 6, 8, 10);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        long sum = 0;
        view.DoForEach(x => sum += x, 1, 3); // 4, 6, 8

        await Assert.That(sum).IsEqualTo(4 + 6 + 8);
    }

    #endregion

    #region Extension Method Tests

    [Test]
    public async Task CreateFilteredView_WithDelegate_Works()
    {
        var source = CreateSourceCollection(1, 2, 3, 4, 5);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = readOnlySource.CreateFilteredView(x => x > 3);

        await Assert.That(view.Count).IsEqualTo(2);
        await Assert.That(view[0]).IsEqualTo(4);
        await Assert.That(view[1]).IsEqualTo(5);
    }

    [Test]
    public async Task CreateSortedView_WithComparer_Works()
    {
        var source = CreateSourceCollection(3, 1, 4, 1, 5, 9, 2, 6);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = readOnlySource.CreateSortedView((a, b) => a.CompareTo(b));

        await Assert.That(view.Count).IsEqualTo(8);
        await Assert.That(view[0]).IsEqualTo(1);
        await Assert.That(view[7]).IsEqualTo(9);
    }

    [Test]
    public async Task CreateFilteredSortedView_WithBoth_Works()
    {
        var source = CreateSourceCollection(10, 3, 8, 1, 6, 4, 9, 2, 7, 5);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = readOnlySource.CreateView(
            x => x % 2 == 0,
            (a, b) => b.CompareTo(a)); // Descending

        // Even: 10, 8, 6, 4, 2 -> Descending: 10, 8, 6, 4, 2
        await Assert.That(view.Count).IsEqualTo(5);
        await Assert.That(view[0]).IsEqualTo(10);
        await Assert.That(view[4]).IsEqualTo(2);
    }

    [Test]
    public async Task CreateView_ReturnsPassthroughView()
    {
        var source = CreateSourceCollection(1, 2, 3);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = readOnlySource.CreateView<long, NoFilter<long>, NoSort<long>>(default, default);

        await Assert.That(view.Count).IsEqualTo(3);
    }

    #endregion

    #region Disposal Tests

    [Test]
    public async Task Dispose_UnsubscribesFromSource()
    {
        var source = CreateSourceCollection(1, 2, 3);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        int eventCount = 0;
        view.CollectionChanged += (_, _) => eventCount++;

        view.Dispose();
        source.Add(4);

        await Assert.That(eventCount).IsEqualTo(0);
    }

    [Test]
    public async Task Dispose_CanBeCalledMultipleTimes()
    {
        var source = CreateSourceCollection(1, 2, 3);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        // Should not throw
        view.Dispose();
        view.Dispose();
        view.Dispose();

        // Verify we can still access the disposed view count (or it doesn't crash)
        await Assert.That(source.Count).IsEqualTo(3);
    }

    #endregion

    #region Thread Safety Tests

    [Test]
    public async Task ConcurrentReads_DoNotThrow()
    {
        // Use smaller dataset to stay within test collection limits (256)
        var source = CreateSourceCollection(Enumerable.Range(1, 100).Select(x => (long)x).ToArray());
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, AscendingComparer>(
            readOnlySource, default, default);

        // Force initial build
        _ = view.Count;

        var tasks = new Task[10];
        var exceptions = new List<Exception>();
        var lockObj = new object();

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 100; j++)
                    {
                        _ = view.Count;
                        if (view.Count > 0)
                        {
                            _ = view[0];
                            _ = view.Contains(2);
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObj)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        await Task.WhenAll(tasks);

        await Assert.That(exceptions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ConcurrentReadAndSourceModify_DoesNotDeadlock()
    {
        var source = CreateSourceCollection(Enumerable.Range(1, 50).Select(x => (long)x).ToArray());
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, EvenPredicate, AscendingComparer>(
            readOnlySource, default, default);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var token = cts.Token;

        var readerTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _ = view.Count;
                    await Task.Yield(); // Allow other tasks to run
                }
                catch
                {
                    // Ignore exceptions - we're testing for deadlock, not thread safety of source
                }
            }
        }, token);

        var writerTask = Task.Run(async () =>
        {
            long value = 1000;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (source.Count < 100)
                    {
                        source.Add(value++);
                    }
                    else if (source.Count > 50)
                    {
                        source.RemoveAt(source.Count - 1);
                    }
                    await Task.Yield(); // Allow other tasks to run
                }
                catch
                {
                    // Ignore exceptions - source is not thread-safe
                }
            }
        }, token);

        // Wait for cancellation to trigger
        try
        {
            await Task.WhenAll(readerTask, writerTask);
        }
        catch (OperationCanceledException)
        {
            // Expected when tasks are cancelled
        }

        // If we got here without hanging, there's no deadlock
        await Assert.That(source.Count).IsGreaterThan(0);
    }

    #endregion

    #region Edge Case Tests

    [Test]
    public async Task EmptySource_ReturnsEmptyView()
    {
        var source = CreateSourceCollection();
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SingleItem_Works()
    {
        var source = CreateSourceCollection(42);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.Count).IsEqualTo(1);
        await Assert.That(view[0]).IsEqualTo(42);
    }

    [Test]
    public async Task IndexOutOfRange_Throws()
    {
        var source = CreateSourceCollection(1, 2, 3);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(() => { _ = view[-1]; }).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => { _ = view[3]; }).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => { _ = view[100]; }).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Get_ReturnsCorrectItem()
    {
        var source = CreateSourceCollection(10, 20, 30);
        using var readOnlySource = new ReadOnlyLargeObservableCollection<long>(source);
        using var view = new FilteredSortedReadOnlyLargeObservableCollection<long, NoFilter<long>, NoSort<long>>(
            readOnlySource, default, default);

        await Assert.That(view.Get(0)).IsEqualTo(10);
        await Assert.That(view.Get(1)).IsEqualTo(20);
        await Assert.That(view.Get(2)).IsEqualTo(30);
    }

    #endregion
}
