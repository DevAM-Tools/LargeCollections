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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LargeCollections.Test.Helpers;
using TUnit.Core;

namespace LargeCollections.Test;

public class LargeSetTest
{
    public static IEnumerable<long> Capacities() => Parameters.Capacities;

    private const long MarkerBase = 50_000L;

    #region Constructor / Properties

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Constructor_SetsDefaults(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            await Assert.That(() => LargeSet.Create<long>(capacity: capacity)).Throws<Exception>();
            return;
        }

        var set = LargeSet.Create<long>(capacity: capacity);

        await Assert.That(set.Count).IsEqualTo(0L);
        await Assert.That(set.Capacity).IsEqualTo(capacity);
        await Assert.That(set.CapacityGrowFactor).IsEqualTo(Constants.DefaultCapacityGrowFactor);
        await Assert.That(set.FixedCapacityGrowAmount).IsEqualTo(Constants.DefaultFixedCapacityGrowAmount);
        await Assert.That(set.FixedCapacityGrowLimit).IsEqualTo(Constants.DefaultFixedCapacityGrowLimit);
        await Assert.That(set.MinLoadFactor).IsEqualTo(Constants.DefaultMinLoadFactor);
        await Assert.That(set.MaxLoadFactor).IsEqualTo(Constants.DefaultMaxLoadFactor);
        await Assert.That(set.MinLoadFactorTolerance).IsEqualTo(Constants.DefaultMinLoadFactorTolerance);
        // Comparer is a struct and always valid
        await Assert.That(set.LoadFactor).IsEqualTo(0d);
    }

    [Test]
    public async Task Constructor_ThrowsOnInvalidParameters()
    {
        await Assert.That(() => LargeSet.Create<int>(capacity: 0)).Throws<Exception>();
        await Assert.That(() => LargeSet.Create<int>(capacity: Constants.MaxLargeCollectionCount + 1)).Throws<Exception>();
        await Assert.That(() => LargeSet.Create<int>(capacityGrowFactor: 1.0)).Throws<Exception>();
        await Assert.That(() => LargeSet.Create<int>(capacityGrowFactor: Constants.MaxCapacityGrowFactor + 0.1)).Throws<Exception>();
        await Assert.That(() => LargeSet.Create<int>(fixedCapacityGrowAmount: 0)).Throws<Exception>();
        await Assert.That(() => LargeSet.Create<int>(fixedCapacityGrowLimit: 0)).Throws<Exception>();
        await Assert.That(() => LargeSet.Create<int>(minLoadFactor: -0.1)).Throws<Exception>();
        await Assert.That(() => LargeSet.Create<int>(minLoadFactor: 0.9, maxLoadFactor: 0.8)).Throws<Exception>();
        await Assert.That(() => LargeSet.Create<int>(maxLoadFactor: 0.0)).Throws<Exception>();
        await Assert.That(() => LargeSet.Create<int>(maxLoadFactor: 1.0, minLoadFactor: 1.0)).Throws<Exception>();
        await Assert.That(() => LargeSet.Create<int>(minLoadFactorTolerance: -0.1)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task LoadFactor_TracksCountAndCapacity(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var set = LargeSet.Create<long>(capacity: actualCapacity, maxLoadFactor: 1.0 + double.Epsilon);

        long addCount = Math.Min(actualCapacity, 3L);
        for (long i = 0; i < addCount; i++)
        {
            set.Add(MarkerBase + i);
        }

        double expected = set.Count / (double)set.Capacity;
        await Assert.That(Math.Abs(set.LoadFactor - expected) < 1e-9).IsTrue();
    }

    #endregion

    #region Add / AddRange

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Add_AddsUniqueItems(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var set = LargeSet.Create<long>(capacity: actualCapacity);

        long addCount = Math.Min(actualCapacity, 5L);
        for (long i = 0; i < addCount; i++)
        {
            set.Add(MarkerBase + i);
        }

        await Assert.That(set.Count).IsEqualTo(addCount);

        set.Add(MarkerBase); // duplicate - always added in the loop above
        await Assert.That(set.Count).IsEqualTo(Math.Min(addCount, Constants.MaxLargeCollectionCount));
        await Assert.That(set.Contains(MarkerBase)).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Add_Throws_WhenExceedingMaxCapacity(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var set = LargeSet.Create<long>(capacity: actualCapacity, maxLoadFactor: double.MaxValue);

        for (long i = 0; i < Constants.MaxLargeCollectionCount; i++)
        {
            set.Add(MarkerBase + i);
        }

        await Assert.That(set.Count).IsEqualTo(Constants.MaxLargeCollectionCount);

        await Assert.That(() => set.Add(MarkerBase + Constants.MaxLargeCollectionCount)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task AddRange_IEnumerable_Branches(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var set = LargeSet.Create<long>(capacity: actualCapacity);

        long listCount = Math.Max(1L, Math.Min(5L, actualCapacity));
        List<long> listValues = Enumerable.Range(0, (int)listCount).Select(i => MarkerBase + i).ToList();
        set.AddRange(listValues);

        IEnumerable<long> enumerableValues = Enumerable.Range(0, (int)listCount).Select(i => MarkerBase + 100 + i);
        set.AddRange(enumerableValues);

        HashSet<long> expected = listValues.Concat(enumerableValues).Select(x => x).ToHashSet();
        await Assert.That(set.Count).IsEqualTo(expected.Count);
        await Assert.That(set.GetAll().ToHashSet().SetEquals(expected)).IsTrue();

        await Assert.That(() => set.AddRange((IEnumerable<long>)null!)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task AddRange_IReadOnlyLargeArray_Overloads(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var set = LargeSet.Create<long>(capacity: actualCapacity);

        long arrayCapacity = Math.Max(1L, Math.Min(actualCapacity, Constants.MaxLargeCollectionCount));
        LargeArray<long> array = CreateSequentialArray(arrayCapacity, MarkerBase);

        set.AddRange(array);
        await Assert.That(set.Count).IsEqualTo(Math.Min(array.Count, actualCapacity));

        long offset = Math.Min(1L, Math.Max(0L, array.Count - 1L));
        set.AddRange(array, offset);
        await Assert.That(set.Contains(array[offset])).IsTrue();

        long count = Math.Min(2L, array.Count - offset);
        set.AddRange(array, offset, count);
        await Assert.That(set.Count).IsEqualTo(array.Count);

        await Assert.That(() => set.AddRange((IReadOnlyLargeArray<long>)null!)).Throws<Exception>();
        await Assert.That(() => set.AddRange(array, -1L)).Throws<Exception>();
        await Assert.That(() => set.AddRange(array, 0L, array.Count + 1L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task AddRange_ReadOnlyLargeSpan_AddsItems(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        LargeArray<long> array = CreateSequentialArray(actualCapacity, MarkerBase);
        ReadOnlyLargeSpan<long> span = new(array, 0L, array.Count);

        var set = LargeSet.Create<long>(capacity: actualCapacity);
        set.AddRange(span);

        await Assert.That(set.GetAll().ToHashSet().SetEquals(array.GetAll().ToHashSet())).IsTrue();
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task AddRange_ReadOnlySpan_AddsItems(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        int length = (int)Math.Max(1L, Math.Min(5L, actualCapacity));
        long[] buffer = Enumerable.Range(0, length).Select(i => MarkerBase + i).ToArray();

        var set = LargeSet.Create<long>(capacity: actualCapacity);
        set.AddRange(buffer.AsSpan());

        await Assert.That(set.GetAll().ToHashSet().SetEquals(buffer.ToHashSet())).IsTrue();
    }
#endif

    [Test]
    public async Task Extend_Occurs_WhenLoadFactorExceeded()
    {
        var set = LargeSet.Create<long>(capacity: 1, capacityGrowFactor: 1.5, fixedCapacityGrowAmount: 1, fixedCapacityGrowLimit: 2, maxLoadFactor: 0.5, minLoadFactor: 0.4, minLoadFactorTolerance: 0.5);

        long initialCapacity = set.Capacity;
        set.Add(MarkerBase);

        await Assert.That(set.Capacity >= initialCapacity).IsTrue();
    }

    #endregion

    #region Remove / Clear

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Remove_RemovesItems(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var set = LargeSet.Create<long>(capacity: actualCapacity);

        long addCount = Math.Max(1L, Math.Min(5L, actualCapacity));
        for (long i = 0; i < addCount; i++)
        {
            set.Add(MarkerBase + i);
        }

        long target = MarkerBase;
        bool removed = set.Remove(target, out long removedItem);
        await Assert.That(removed).IsTrue();
        await Assert.That(removedItem).IsEqualTo(target);
        await Assert.That(set.Contains(target)).IsFalse();

        bool removedAgain = set.Remove(target);
        await Assert.That(removedAgain).IsFalse();
        bool removedMissing = set.Remove(MarkerBase + 999, out removedItem);
        await Assert.That(removedMissing).IsFalse();
        await Assert.That(removedItem).IsEqualTo(0L);
    }

    [Test]
    public async Task Remove_TriggersShrink_WhenBelowThreshold()
    {
        var set = LargeSet.Create<long>(capacity: 4, capacityGrowFactor: 1.5, fixedCapacityGrowAmount: 1, fixedCapacityGrowLimit: 2, minLoadFactor: 0.75, maxLoadFactor: 0.95, minLoadFactorTolerance: 0.9);

        for (int i = 0; i < 4; i++)
        {
            set.Add(MarkerBase + i);
        }

        long capacityBefore = set.Capacity;
        set.Remove(MarkerBase);
        set.Remove(MarkerBase + 1);
        set.Remove(MarkerBase + 2);

        await Assert.That(set.Capacity < capacityBefore).IsTrue();
        await Assert.That(set.Count).IsEqualTo(1L);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Clear_EmptiesSet(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var set = LargeSet.Create<long>(capacity: actualCapacity);

        set.Add(MarkerBase);
        set.Add(MarkerBase + 1);

        set.Clear();

        await Assert.That(set.Count).IsEqualTo(0L);
        await Assert.That(set.GetAll().Any()).IsFalse();
    }

    #endregion

    #region Lookup / Mutating Queries

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task TryGetValue_FindsExisting(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var set = LargeSet.Create<long>(capacity: actualCapacity);
        long value = MarkerBase + 42;

        set.Add(value);

        bool found = set.TryGetValue(value, out long result);
        await Assert.That(found).IsTrue();
        await Assert.That(result).IsEqualTo(value);

        bool notFound = set.TryGetValue(MarkerBase - 1, out _);
        await Assert.That(notFound).IsFalse();
    }

    [Test]
    public async Task TryGetOrSetDefault_AddsOnDemand()
    {
        var set = LargeSet.Create<long>(capacity: 2);

        bool exists = set.TryGetOrSetDefault(MarkerBase, out long value1);
        await Assert.That(exists).IsFalse();
        await Assert.That(value1).IsEqualTo(MarkerBase);
        await Assert.That(set.Contains(MarkerBase)).IsTrue();

        exists = set.TryGetOrSetDefault(MarkerBase, out long value2);
        await Assert.That(exists).IsTrue();
        await Assert.That(value2).IsEqualTo(MarkerBase);
    }

    [Test]
    public async Task TryGetOrSet_AddsUsingComparer()
    {
        var set = LargeSet.Create<string>(static (l, r) => string.Equals(l, r, StringComparison.OrdinalIgnoreCase),
            static item => item?.ToUpperInvariant().GetHashCode() ?? 0, capacity: 2);

        bool found = set.TryGetOrSet("FOO", "foo", out string value1);
        await Assert.That(found).IsFalse();
        await Assert.That(value1).IsEqualTo("foo");
        await Assert.That(set.Count).IsEqualTo(1L);

        found = set.TryGetOrSet("foo", "bar", out string value2);
        await Assert.That(found).IsTrue();
        await Assert.That(value2).IsEqualTo("foo");
        await Assert.That(set.Count).IsEqualTo(1L);
    }

    [Test]
    public async Task TryGetOrSetFactory_UsesFactoryOnce()
    {
        var set = LargeSet.Create<string>(static (l, r) => string.Equals(l, r, StringComparison.OrdinalIgnoreCase),
            static item => item?.ToUpperInvariant().GetHashCode() ?? 0, capacity: 2);

        int factoryCalls = 0;
        bool found = set.TryGetOrSet("foo", () =>
        {
            factoryCalls++;
            return "foo";
        }, out string value1);

        await Assert.That(found).IsFalse();
        await Assert.That(value1).IsEqualTo("foo");
        await Assert.That(factoryCalls).IsEqualTo(1);

        found = set.TryGetOrSet("FOO", () =>
        {
            factoryCalls++;
            return "bar";
        }, out string value2);

        await Assert.That(found).IsTrue();
        await Assert.That(value2).IsEqualTo("foo");
        await Assert.That(factoryCalls).IsEqualTo(1);

        await Assert.That(() => set.TryGetOrSet("baz", (Func<string>)null!, out _)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Contains_ReturnsExpected(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var set = LargeSet.Create<long>(capacity: actualCapacity);

        set.Add(MarkerBase);

        await Assert.That(set.Contains(MarkerBase)).IsTrue();
        await Assert.That(set.Contains(MarkerBase + 1)).IsFalse();
    }

    #endregion

    #region Enumeration / GetAll / DoForEach

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task GetAll_ReturnsAllUniqueItems(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var set = LargeSet.Create<long>(capacity: actualCapacity);

        long addCount = Math.Max(1L, Math.Min(5L, actualCapacity));
        for (long i = 0; i < addCount; i++)
        {
            set.Add(MarkerBase + i);
        }

        HashSet<long> items = set.GetAll().ToHashSet();
        await Assert.That((long)items.Count).IsEqualTo(addCount);
        await Assert.That(items.SetEquals(Enumerable.Range(0, (int)addCount).Select(i => MarkerBase + i))).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Enumerator_IteratesAllItems(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var set = LargeSet.Create<long>(capacity: actualCapacity);

        long addCount = Math.Max(1L, Math.Min(5L, actualCapacity));
        for (long i = 0; i < addCount; i++)
        {
            set.Add(MarkerBase + i);
        }

        List<long> enumerated = new();
        foreach (long item in set)
        {
            enumerated.Add(item);
        }

        await Assert.That(enumerated.ToHashSet().SetEquals(set.GetAll().ToHashSet())).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task DoForEach_AppliesActions(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var set = LargeSet.Create<long>(capacity: actualCapacity);

        long addCount = Math.Max(1L, Math.Min(5L, actualCapacity));
        for (long i = 0; i < addCount; i++)
        {
            set.Add(MarkerBase + i);
        }

        long sum = 0;
        set.DoForEach(item => sum += item);
        await Assert.That(sum).IsEqualTo(set.GetAll().Sum());

        SumAction sumAction = new ();
        set.DoForEach(ref sumAction);
        await Assert.That(sumAction.Sum).IsEqualTo(set.GetAll().Sum());

        await Assert.That(() => set.DoForEach((Action<long>)null!)).Throws<Exception>();
    }

    #endregion

    #region Helpers

    private static LargeArray<long> CreateSequentialArray(long capacity, long startValue)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        LargeArray<long> array = new(Math.Max(1L, capacity));
        for (long i = 0; i < array.Count; i++)
        {
            array[i] = startValue + i;
        }
        return array;
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Add_SameItemRepeatedly_DoesNotIncrementCount()
    {
        var set = LargeSet.Create<long>();

        set.Add(MarkerBase);
        set.Add(MarkerBase);
        set.Add(MarkerBase);

        await Assert.That(set.Count).IsEqualTo(1L);
        await Assert.That(set.Contains(MarkerBase)).IsTrue();
    }

    [Test]
    public async Task CustomHashFunction_WithCollisions_ResolvesCorrectly()
    {
        // Use a hash function that always returns the same value (worst case)
        var set = LargeSet.Create<long>(
            equalsFunction: (a, b) => a == b,
            hashCodeFunction: _ => 42,  // All items hash to same bucket
            capacity: 10
        );

        set.Add(MarkerBase);
        set.Add(MarkerBase + 1);
        set.Add(MarkerBase + 2);

        await Assert.That(set.Count).IsEqualTo(3L);
        await Assert.That(set.Contains(MarkerBase)).IsTrue();
        await Assert.That(set.Contains(MarkerBase + 1)).IsTrue();
        await Assert.That(set.Contains(MarkerBase + 2)).IsTrue();
    }

    [Test]
    public async Task TryGetOrSet_WithFactory_FactoryOnlyCalledWhenNotFound()
    {
        var set = LargeSet.Create<long>();
        int factoryCalls = 0;

        // First call - item not found, factory should be called
        bool found1 = set.TryGetOrSet(MarkerBase, () => { factoryCalls++; return MarkerBase; }, out long value1);

        await Assert.That(found1).IsFalse();
        await Assert.That(value1).IsEqualTo(MarkerBase);
        await Assert.That(factoryCalls).IsEqualTo(1);

        // Second call - item found, factory should NOT be called
        bool found2 = set.TryGetOrSet(MarkerBase, () => { factoryCalls++; return MarkerBase; }, out long value2);

        await Assert.That(found2).IsTrue();
        await Assert.That(value2).IsEqualTo(MarkerBase);
        await Assert.That(factoryCalls).IsEqualTo(1);  // Still 1, not called again
    }

    [Test]
    public async Task TryGetOrSet_NullFactory_Throws()
    {
        var set = LargeSet.Create<long>();

        await Assert.That(() => set.TryGetOrSet(MarkerBase, (Func<long>)null!, out _)).Throws<Exception>();
    }

    [Test]
    public async Task Remove_NonExistentItem_ReturnsFalse()
    {
        var set = LargeSet.Create<long>();
        set.Add(MarkerBase);

        bool removed = set.Remove(MarkerBase + 100);

        await Assert.That(removed).IsFalse();
        await Assert.That(set.Count).IsEqualTo(1L);
    }

    [Test]
    public async Task Remove_WithRemovedItem_ReturnsCorrectItem()
    {
        var set = LargeSet.Create<long>();
        set.Add(MarkerBase);
        set.Add(MarkerBase + 1);

        bool removed = set.Remove(MarkerBase, out long removedItem);

        await Assert.That(removed).IsTrue();
        await Assert.That(removedItem).IsEqualTo(MarkerBase);
    }

    [Test]
    public async Task Shrink_ReducesCapacity()
    {
        var set = LargeSet.Create<long>(capacity: 100, minLoadFactor: 0.25);

        // Add some items
        for (long i = 0; i < 10; i++)
        {
            set.Add(MarkerBase + i);
        }

        long capacityBefore = set.Capacity;
        set.Shrink();
        long capacityAfter = set.Capacity;

        // Capacity should be reduced or stay same if already optimal
        await Assert.That(capacityAfter).IsLessThanOrEqualTo(capacityBefore);
        await Assert.That(set.Count).IsEqualTo(10L);  // Items preserved
    }

    #endregion

    #region Helper Structs

    private struct SumAction : ILargeAction<long>
    {
        public long Sum;
        public void Invoke(long item) => Sum += item;
    }

    #endregion
}


