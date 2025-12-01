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

public class LargeDictionaryTest
{
    public static IEnumerable<long> Capacities() => Parameters.Capacities;

    private const long KeyMarkerBase = 100_000L;
    private const long ValueMarkerBase = 200_000L;

    #region Constructor / Properties

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Constructor_SetsDefaults(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            await Assert.That(() => LargeDictionary.Create<long, long>(capacity: capacity)).Throws<Exception>();
            return;
        }

        var dictionary = LargeDictionary.Create<long, long>(capacity: capacity);

        await Assert.That(dictionary.Count).IsEqualTo(0L);
        await Assert.That(dictionary.Capacity).IsEqualTo(capacity);
        await Assert.That(dictionary.CapacityGrowFactor).IsEqualTo(Constants.DefaultCapacityGrowFactor);
        await Assert.That(dictionary.FixedCapacityGrowAmount).IsEqualTo(Constants.DefaultFixedCapacityGrowAmount);
        await Assert.That(dictionary.FixedCapacityGrowLimit).IsEqualTo(Constants.DefaultFixedCapacityGrowLimit);
        await Assert.That(dictionary.MinLoadFactor).IsEqualTo(Constants.DefaultMinLoadFactor);
        await Assert.That(dictionary.MaxLoadFactor).IsEqualTo(Constants.DefaultMaxLoadFactor);
        await Assert.That(dictionary.MinLoadFactorTolerance).IsEqualTo(Constants.DefaultMinLoadFactorTolerance);
        await Assert.That(dictionary.Keys.Any()).IsFalse();
        await Assert.That(dictionary.Values.Any()).IsFalse();
        await Assert.That(dictionary.LoadFactor).IsEqualTo(0d);
    }

    [Test]
    public async Task Constructor_ThrowsOnInvalidParameters()
    {
        await Assert.That(() => LargeDictionary.Create<string, int>(capacity: 0)).Throws<Exception>();
        await Assert.That(() => LargeDictionary.Create<string, int>(capacity: Constants.MaxLargeCollectionCount + 1)).Throws<Exception>();
        await Assert.That(() => LargeDictionary.Create<string, int>(capacityGrowFactor: 1.0)).Throws<Exception>();
        await Assert.That(() => LargeDictionary.Create<string, int>(capacityGrowFactor: Constants.MaxCapacityGrowFactor + 0.1)).Throws<Exception>();
        await Assert.That(() => LargeDictionary.Create<string, int>(fixedCapacityGrowAmount: 0)).Throws<Exception>();
        await Assert.That(() => LargeDictionary.Create<string, int>(fixedCapacityGrowLimit: 0)).Throws<Exception>();
        await Assert.That(() => LargeDictionary.Create<string, int>(minLoadFactor: -0.1)).Throws<Exception>();
        await Assert.That(() => LargeDictionary.Create<string, int>(minLoadFactor: 0.9, maxLoadFactor: 0.8)).Throws<Exception>();
        await Assert.That(() => LargeDictionary.Create<string, int>(maxLoadFactor: 0.0)).Throws<Exception>();
        await Assert.That(() => LargeDictionary.Create<string, int>(maxLoadFactor: 1.0, minLoadFactor: 1.0)).Throws<Exception>();
        await Assert.That(() => LargeDictionary.Create<string, int>(minLoadFactorTolerance: -0.1)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task LoadFactor_TracksCountAndCapacity(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity, maxLoadFactor: 1.0 + double.Epsilon);

        long addCount = Math.Min(actualCapacity, 3L);
        for (long i = 0; i < addCount; i++)
        {
            dictionary.Add(new KeyValuePair<long, long>(KeyMarkerBase + i, ValueMarkerBase + i));
        }

        double expected = dictionary.Count / (double)dictionary.Capacity;
        await Assert.That(Math.Abs(dictionary.LoadFactor - expected) < 1e-9).IsTrue();
    }

    #endregion

    #region Indexer / Set / Add

    [Test]
    public async Task Indexer_ThrowsOnNullKey()
    {
        var dictionary = LargeDictionary.Create<string, int>(capacity: 2);

        await Assert.That(() => dictionary[null!]).Throws<Exception>();
        await Assert.That(() => dictionary[null!] = 1).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Indexer_GetAndSet_StoresValues(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        dictionary[KeyMarkerBase] = ValueMarkerBase;
        await Assert.That(dictionary[KeyMarkerBase]).IsEqualTo(ValueMarkerBase);

        dictionary[KeyMarkerBase] = ValueMarkerBase + 1;
        await Assert.That(dictionary.Get(KeyMarkerBase)).IsEqualTo(ValueMarkerBase + 1);
    }

    [Test]
    public async Task Set_ThrowsOnNullKey()
    {
        var dictionary = LargeDictionary.Create<string, int>(capacity: 2);

        await Assert.That(() => dictionary.Set(null!, 0)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Set_ReplacesExistingValue(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        dictionary.Set(KeyMarkerBase, ValueMarkerBase);
        dictionary.Set(KeyMarkerBase, ValueMarkerBase + 10L);

        await Assert.That(dictionary.Count).IsEqualTo(1L);
        await Assert.That(dictionary.Get(KeyMarkerBase)).IsEqualTo(ValueMarkerBase + 10L);
    }

    [Test]
    public async Task Add_ThrowsOnNullKey()
    {
        var dictionary = LargeDictionary.Create<string, int>(capacity: 2);
        await Assert.That(() => dictionary.Add(new KeyValuePair<string, int>(null!, 0))).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Add_AddsUniqueKeys(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        long addCount = Math.Min(actualCapacity, 5L);
        for (long i = 0; i < addCount; i++)
        {
            dictionary.Add(new KeyValuePair<long, long>(KeyMarkerBase + i, ValueMarkerBase + i));
        }

        await Assert.That(dictionary.Count).IsEqualTo(addCount);

        // Re-add an existing key with a different value - should replace, not add
        long existingKeyIndex = Math.Min(1L, addCount - 1);
        dictionary.Add(new KeyValuePair<long, long>(KeyMarkerBase + existingKeyIndex, ValueMarkerBase + 999L));
        await Assert.That(dictionary.Count).IsEqualTo(addCount);
        await Assert.That(dictionary.Get(KeyMarkerBase + existingKeyIndex)).IsEqualTo(ValueMarkerBase + 999L);
    }

    [Test]
    public async Task Add_Throws_WhenExceedingMaxCapacity()
    {
        var dictionary = LargeDictionary.Create<long, long>(capacity: 1, maxLoadFactor: double.MaxValue);

        for (long i = 0; i < Constants.MaxLargeCollectionCount; i++)
        {
            dictionary.Add(new KeyValuePair<long, long>(KeyMarkerBase + i, ValueMarkerBase + i));
        }

        await Assert.That(dictionary.Count).IsEqualTo(Constants.MaxLargeCollectionCount);
        await Assert.That(() => dictionary.Add(new KeyValuePair<long, long>(KeyMarkerBase + Constants.MaxLargeCollectionCount, 0L))).Throws<Exception>();
    }

    #endregion

    #region AddRange

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task AddRange_IEnumerable_Branches(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        long listCount = Math.Max(1L, Math.Min(actualCapacity, 5L));
        List<KeyValuePair<long, long>> listItems = CreatePairList(listCount, KeyMarkerBase, ValueMarkerBase);
        dictionary.AddRange(listItems);

        IEnumerable<KeyValuePair<long, long>> enumerableItems = Enumerable.Range(0, (int)listCount)
            .Select(i => new KeyValuePair<long, long>(KeyMarkerBase + 100 + i, ValueMarkerBase + 200 + i));
        dictionary.AddRange(enumerableItems);

        foreach (KeyValuePair<long, long> kvp in listItems.Concat(enumerableItems))
        {
            await Assert.That(dictionary.TryGetValue(kvp.Key, out long value)).IsTrue();
            await Assert.That(value).IsEqualTo(kvp.Value);
        }

        await Assert.That(() => dictionary.AddRange((IEnumerable<KeyValuePair<long, long>>)null!)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task AddRange_IReadOnlyLargeArray_Overloads(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        LargeArray<KeyValuePair<long, long>> array = CreateSequentialPairs(actualCapacity);

        dictionary.AddRange(array);
        await Assert.That(dictionary.Count).IsEqualTo(array.Count);

        long offset = Math.Min(1L, Math.Max(0L, array.Count - 1L));
        dictionary.AddRange(array, offset);
        await Assert.That(dictionary.ContainsKey(array[offset].Key)).IsTrue();

        long rangeCount = Math.Max(0L, array.Count - offset);
        long limitedCount = Math.Min(2L, rangeCount);
        if (limitedCount > 0)
        {
            dictionary.AddRange(array, offset, limitedCount);
            await Assert.That(dictionary.ContainsKey(array[offset].Key)).IsTrue();
        }

        await Assert.That(() => dictionary.AddRange((IReadOnlyLargeArray<KeyValuePair<long, long>>)null!)).Throws<Exception>();
        await Assert.That(() => dictionary.AddRange(array, -1L)).Throws<Exception>();
        await Assert.That(() => dictionary.AddRange(array, 0L, array.Count + 1L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task AddRange_ReadOnlyLargeSpan_AddsItems(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        LargeArray<KeyValuePair<long, long>> array = CreateSequentialPairs(actualCapacity);
        ReadOnlyLargeSpan<KeyValuePair<long, long>> span = new(array, 0L, array.Count);

        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);
        dictionary.AddRange(span);

        Dictionary<long, long> expected = ToDictionary(array.GetAll());
        Dictionary<long, long> actual = ToDictionary(dictionary.GetAll());
        await Assert.That(DictionariesEqual(actual, expected)).IsTrue();

        dictionary.AddRange(default(ReadOnlyLargeSpan<KeyValuePair<long, long>>));
        await Assert.That(dictionary.Count).IsEqualTo(array.Count);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task AddRange_ReadOnlySpan_AddsItems(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        int length = Math.Max(1, (int)Math.Min(5L, actualCapacity));
        KeyValuePair<long, long>[] buffer = Enumerable.Range(0, length)
            .Select(i => new KeyValuePair<long, long>(KeyMarkerBase + i, ValueMarkerBase + i))
            .ToArray();

        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);
        dictionary.AddRange(buffer.AsSpan());

        Dictionary<long, long> expected = buffer.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Dictionary<long, long> actual = ToDictionary(dictionary.GetAll());
        await Assert.That(DictionariesEqual(actual, expected)).IsTrue();

        dictionary.AddRange(ReadOnlySpan<KeyValuePair<long, long>>.Empty);
        await Assert.That(dictionary.Count).IsEqualTo(expected.Count);
    }
#endif

    [Test]
    public async Task Extend_Occurs_WhenLoadFactorExceeded()
    {
        var dictionary = LargeDictionary.Create<long, long>(capacity: 1, capacityGrowFactor: 1.5, fixedCapacityGrowAmount: 1, fixedCapacityGrowLimit: 2, maxLoadFactor: 0.5, minLoadFactor: 0.4, minLoadFactorTolerance: 0.5);

        long initialCapacity = dictionary.Capacity;
        dictionary.Add(new KeyValuePair<long, long>(KeyMarkerBase, ValueMarkerBase));

        await Assert.That(dictionary.Capacity >= initialCapacity).IsTrue();
    }

    #endregion

    #region Remove / Clear

    [Test]
    public async Task Remove_ThrowsOnNullKey()
    {
        var dictionary = LargeDictionary.Create<string, int>(capacity: 2);
        await Assert.That(() => dictionary.Remove(null!)).Throws<Exception>();
        await Assert.That(() => dictionary.Remove(null!, out _)).Throws<Exception>();
        await Assert.That(() => dictionary.Remove(new KeyValuePair<string, int>(null!, 0))).Throws<Exception>();
        await Assert.That(() => dictionary.Remove(new KeyValuePair<string, int>(null!, 0), out _)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Remove_ByKey_RemovesItem(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        dictionary.Set(KeyMarkerBase, ValueMarkerBase);
        bool removed = dictionary.Remove(KeyMarkerBase);

        await Assert.That(removed).IsTrue();
        await Assert.That(dictionary.ContainsKey(KeyMarkerBase)).IsFalse();
    }

    [Test]
    public async Task Remove_ByKey_ReturnsRemovedValue()
    {
        var dictionary = LargeDictionary.Create<long, long>(capacity: 2);
        dictionary.Set(KeyMarkerBase, ValueMarkerBase);

        bool removed = dictionary.Remove(KeyMarkerBase, out long value);

        await Assert.That(removed).IsTrue();
        await Assert.That(value).IsEqualTo(ValueMarkerBase);
    }

    [Test]
    public async Task Remove_KeyValuePair_RemovesRegardlessOfValue()
    {
        var dictionary = LargeDictionary.Create<long, long>(capacity: 2);
        dictionary.Set(KeyMarkerBase, ValueMarkerBase);

        bool removed = dictionary.Remove(new KeyValuePair<long, long>(KeyMarkerBase, ValueMarkerBase + 1));

        await Assert.That(removed).IsTrue();
        await Assert.That(dictionary.ContainsKey(KeyMarkerBase)).IsFalse();
    }

    [Test]
    public async Task Remove_KeyValuePair_OutParameter_ReturnsRemovedItem()
    {
        var dictionary = LargeDictionary.Create<long, long>(capacity: 2);
        dictionary.Set(KeyMarkerBase, ValueMarkerBase);

        bool removed = dictionary.Remove(new KeyValuePair<long, long>(KeyMarkerBase, 0L), out KeyValuePair<long, long> removedItem);

        await Assert.That(removed).IsTrue();
        await Assert.That(removedItem.Key).IsEqualTo(KeyMarkerBase);
        await Assert.That(removedItem.Value).IsEqualTo(ValueMarkerBase);
    }

    [Test]
    public async Task Remove_ReturnsFalse_WhenMissing()
    {
        var dictionary = LargeDictionary.Create<long, long>(capacity: 2);

        bool removed = dictionary.Remove(KeyMarkerBase);
        await Assert.That(removed).IsFalse();

        bool removedWithValue = dictionary.Remove(KeyMarkerBase, out long value);
        await Assert.That(removedWithValue).IsFalse();
        await Assert.That(value).IsEqualTo(0L);
    }

    [Test]
    public async Task Remove_TriggersShrink_WhenBelowThreshold()
    {
        var dictionary = LargeDictionary.Create<long, long>(capacity: 4, capacityGrowFactor: 1.5, fixedCapacityGrowAmount: 1, fixedCapacityGrowLimit: 2, minLoadFactor: 0.75, maxLoadFactor: 0.95, minLoadFactorTolerance: 0.9);

        for (int i = 0; i < 4; i++)
        {
            dictionary.Set(KeyMarkerBase + i, ValueMarkerBase + i);
        }

        long capacityBefore = dictionary.Capacity;
        dictionary.Remove(KeyMarkerBase);
        dictionary.Remove(KeyMarkerBase + 1);
        dictionary.Remove(KeyMarkerBase + 2);

        await Assert.That(dictionary.Capacity < capacityBefore).IsTrue();
        await Assert.That(dictionary.Count).IsEqualTo(1L);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Clear_EmptiesDictionary(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);
        dictionary.Set(KeyMarkerBase, ValueMarkerBase);
        dictionary.Set(KeyMarkerBase + 1, ValueMarkerBase + 1);

        dictionary.Clear();

        await Assert.That(dictionary.Count).IsEqualTo(0L);
        await Assert.That(dictionary.Keys.Any()).IsFalse();
        await Assert.That(dictionary.Values.Any()).IsFalse();
    }

    #endregion

    #region Lookup / Contains

    [Test]
    public async Task ContainsKey_ThrowsOnNullKey()
    {
        var dictionary = LargeDictionary.Create<string, int>(capacity: 2);
        await Assert.That(() => dictionary.ContainsKey(null!)).Throws<Exception>();
    }

    [Test]
    public async Task TryGetValue_ThrowsOnNullKey()
    {
        var dictionary = LargeDictionary.Create<string, int>(capacity: 2);
        await Assert.That(() => dictionary.TryGetValue(null!, out _)).Throws<Exception>();
    }

    [Test]
    public async Task Get_ThrowsWhenKeyMissing()
    {
        var dictionary = LargeDictionary.Create<long, long>(capacity: 2);
        await Assert.That(() => dictionary.Get(KeyMarkerBase)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ContainsKey_ReturnsExpected(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        dictionary.Set(KeyMarkerBase, ValueMarkerBase);

        await Assert.That(dictionary.ContainsKey(KeyMarkerBase)).IsTrue();
        await Assert.That(dictionary.ContainsKey(KeyMarkerBase + 1)).IsFalse();
    }

    [Test]
    public async Task Contains_KeyValuePair_UsesValueEquality()
    {
        var dictionary = LargeDictionary.Create<long, long>(capacity: 2);
        dictionary.Set(KeyMarkerBase, ValueMarkerBase);

        await Assert.That(dictionary.Contains(new KeyValuePair<long, long>(KeyMarkerBase, ValueMarkerBase))).IsTrue();
        await Assert.That(dictionary.Contains(new KeyValuePair<long, long>(KeyMarkerBase, ValueMarkerBase + 1))).IsFalse();
        await Assert.That(dictionary.Contains(new KeyValuePair<long, long>(0, 0))).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task TryGetValue_ReturnsExpected(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        dictionary.Set(KeyMarkerBase, ValueMarkerBase);

        bool found = dictionary.TryGetValue(KeyMarkerBase, out long value);
        await Assert.That(found).IsTrue();
        await Assert.That(value).IsEqualTo(ValueMarkerBase);

        bool missing = dictionary.TryGetValue(KeyMarkerBase + 1, out long missingValue);
        await Assert.That(missing).IsFalse();
        await Assert.That(missingValue).IsEqualTo(0L);
    }

    [Test]
    public async Task TryGetOrSetDefault_AddsDefaultValue()
    {
        var dictionary = LargeDictionary.Create<long, long>(capacity: 2);

        bool existed = dictionary.TryGetOrSetDefault(KeyMarkerBase, out long value1);
        await Assert.That(existed).IsFalse();
        await Assert.That(value1).IsEqualTo(0L);
        await Assert.That(dictionary.ContainsKey(KeyMarkerBase)).IsTrue();

        existed = dictionary.TryGetOrSetDefault(KeyMarkerBase, out long value2);
        await Assert.That(existed).IsTrue();
        await Assert.That(value2).IsEqualTo(0L);
    }

    [Test]
    public async Task TryGetOrSetDefault_ThrowsOnNullKey()
    {
        var dictionary = LargeDictionary.Create<string, int>(capacity: 2);
        await Assert.That(() => dictionary.TryGetOrSetDefault(null!, out _)).Throws<Exception>();
    }

    [Test]
    public async Task TryGetOrSet_AddsOrReturnsExisting()
    {
        var dictionary = LargeDictionary.Create<long, long>(capacity: 2);

        bool existed = dictionary.TryGetOrSet(KeyMarkerBase, ValueMarkerBase, out long value1);
        await Assert.That(existed).IsFalse();
        await Assert.That(value1).IsEqualTo(ValueMarkerBase);

        existed = dictionary.TryGetOrSet(KeyMarkerBase, ValueMarkerBase + 1, out long value2);
        await Assert.That(existed).IsTrue();
        await Assert.That(value2).IsEqualTo(ValueMarkerBase);
    }

    [Test]
    public async Task TryGetOrSetValue_ThrowsOnNullKey()
    {
        var dictionary = LargeDictionary.Create<string, int>(capacity: 2);
        await Assert.That(() => dictionary.TryGetOrSet(null!, 42, out _)).Throws<Exception>();
    }

    [Test]
    public async Task TryGetOrSetFactory_UsesFactoryOnce()
    {
        var dictionary = LargeDictionary.Create<string, int>(static (l, r) => string.Equals(l, r, StringComparison.OrdinalIgnoreCase), static key => key.ToUpperInvariant().GetHashCode(), capacity: 2);

        int factoryCalls = 0;
        bool existed = dictionary.TryGetOrSet("foo", () =>
        {
            factoryCalls++;
            return 42;
        }, out int value1);

        await Assert.That(existed).IsFalse();
        await Assert.That(value1).IsEqualTo(42);
        await Assert.That(factoryCalls).IsEqualTo(1);

        existed = dictionary.TryGetOrSet("FOO", () =>
        {
            factoryCalls++;
            return 84;
        }, out int value2);

        await Assert.That(existed).IsTrue();
        await Assert.That(value2).IsEqualTo(42);
        await Assert.That(factoryCalls).IsEqualTo(1);

        await Assert.That(() => dictionary.TryGetOrSet("bar", (Func<int>)null!, out _)).Throws<Exception>();
    }

    [Test]
    public async Task TryGetOrSetFactory_ThrowsOnNullKey()
    {
        var dictionary = LargeDictionary.Create<string, int>(capacity: 2);
        await Assert.That(() => dictionary.TryGetOrSet(null!, () => 1, out _)).Throws<Exception>();
    }

    [Test]
    public async Task TryGetOrSet_ThrowsWhenFull()
    {
        var dictionary = LargeDictionary.Create<long, long>(capacity: 1, maxLoadFactor: double.MaxValue);

        for (long i = 0; i < Constants.MaxLargeCollectionCount; i++)
        {
            dictionary.Add(new KeyValuePair<long, long>(KeyMarkerBase + i, ValueMarkerBase + i));
        }

        await Assert.That(() => dictionary.TryGetOrSetDefault(KeyMarkerBase - 1, out _)).Throws<Exception>();
    }

    #endregion

    #region Enumeration / Aggregation

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Keys_ReturnAllKeys(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        long addCount = Math.Max(1L, Math.Min(actualCapacity, 5L));
        for (long i = 0; i < addCount; i++)
        {
            dictionary.Set(KeyMarkerBase + i, ValueMarkerBase + i);
        }

        HashSet<long> keys = dictionary.Keys.ToHashSet();
        await Assert.That((long)keys.Count).IsEqualTo(addCount);
        await Assert.That(keys.SetEquals(Enumerable.Range(0, (int)addCount).Select(i => KeyMarkerBase + i))).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Values_ReturnAllValues(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        long addCount = Math.Max(1L, Math.Min(actualCapacity, 5L));
        for (long i = 0; i < addCount; i++)
        {
            dictionary.Set(KeyMarkerBase + i, ValueMarkerBase + i);
        }

        HashSet<long> values = dictionary.Values.ToHashSet();
        await Assert.That((long)values.Count).IsEqualTo(addCount);
        await Assert.That(values.SetEquals(Enumerable.Range(0, (int)addCount).Select(i => ValueMarkerBase + i))).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task GetAll_ReturnsAllPairs(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        long addCount = Math.Max(1L, Math.Min(actualCapacity, 5L));
        for (long i = 0; i < addCount; i++)
        {
            dictionary.Set(KeyMarkerBase + i, ValueMarkerBase + i);
        }

        Dictionary<long, long> roundtrip = ToDictionary(dictionary.GetAll());
        await Assert.That((long)roundtrip.Count).IsEqualTo(addCount);
        await Assert.That(roundtrip.All(pair => dictionary.TryGetValue(pair.Key, out long value) && value == pair.Value)).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Enumerator_YieldsAllItems(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        long addCount = Math.Max(1L, Math.Min(actualCapacity, 5L));
        for (long i = 0; i < addCount; i++)
        {
            dictionary.Set(KeyMarkerBase + i, ValueMarkerBase + i);
        }

        List<KeyValuePair<long, long>> enumerated = new();
        foreach (KeyValuePair<long, long> item in dictionary)
        {
            enumerated.Add(item);
        }

        Dictionary<long, long> fromEnumeration = ToDictionary(enumerated);
        await Assert.That(DictionariesEqual(fromEnumeration, ToDictionary(dictionary.GetAll()))).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task DoForEach_AppliesActions(long capacity)
    {
        long actualCapacity = Math.Max(1L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        var dictionary = LargeDictionary.Create<long, long>(capacity: actualCapacity);

        long addCount = Math.Max(1L, Math.Min(actualCapacity, 5L));
        for (long i = 0; i < addCount; i++)
        {
            dictionary.Set(KeyMarkerBase + i, ValueMarkerBase + i);
        }

        long sum = 0;
        dictionary.DoForEach(pair => sum += pair.Value);
        await Assert.That(sum).IsEqualTo(dictionary.GetAll().Sum(pair => pair.Value));

        SumValueAction sumAction = new ();
        dictionary.DoForEach(ref sumAction);
        await Assert.That(sumAction.Sum).IsEqualTo(dictionary.GetAll().Sum(pair => pair.Value));

        await Assert.That(() => dictionary.DoForEach((Action<KeyValuePair<long, long>>)null!)).Throws<Exception>();
    }

    #endregion

    #region Default helpers

    [Test]
    public async Task DefaultEqualsFunction_ComparesNullAndValues()
    {
        await Assert.That(DefaultFunctions<string>.DefaultEqualsFunction(null!, null!)).IsTrue();
        await Assert.That(DefaultFunctions<string>.DefaultEqualsFunction("a", null!)).IsFalse();
        await Assert.That(DefaultFunctions<string>.DefaultEqualsFunction(null!, "b")).IsFalse();
        await Assert.That(DefaultFunctions<string>.DefaultEqualsFunction("foo", "foo")).IsTrue();
        await Assert.That(DefaultFunctions<string>.DefaultEqualsFunction("foo", "FOO")).IsFalse();
    }

    [Test]
    public async Task DefaultHashCodeFunction_ReturnsExpectedValues()
    {
        await Assert.That(DefaultFunctions<string>.DefaultHashCodeFunction(null!)).IsEqualTo(0);
        await Assert.That(DefaultFunctions<string>.DefaultHashCodeFunction("foo")).IsEqualTo("foo".GetHashCode());
    }

    #endregion

    #region Helpers

    private static LargeArray<KeyValuePair<long, long>> CreateSequentialPairs(long capacity, long keyStart = KeyMarkerBase, long valueStart = ValueMarkerBase)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        long actual = Math.Max(1L, capacity);
        LargeArray<KeyValuePair<long, long>> array = new(actual);
        for (long i = 0; i < array.Count; i++)
        {
            array[i] = new KeyValuePair<long, long>(keyStart + i, valueStart + i);
        }
        return array;
    }

    private static List<KeyValuePair<long, long>> CreatePairList(long count, long keyStart = KeyMarkerBase, long valueStart = ValueMarkerBase)
    {
        List<KeyValuePair<long, long>> list = new();
        for (long i = 0; i < count; i++)
        {
            list.Add(new KeyValuePair<long, long>(keyStart + i, valueStart + i));
        }
        return list;
    }

    private static Dictionary<long, long> ToDictionary(IEnumerable<KeyValuePair<long, long>> items)
    {
        return items.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static bool DictionariesEqual(IDictionary<long, long> left, IDictionary<long, long> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (KeyValuePair<long, long> kvp in left)
        {
            if (!right.TryGetValue(kvp.Key, out long value) || value != kvp.Value)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task CustomHashFunction_WithCollisions_ResolvesCorrectly()
    {
        // Use a hash function that always returns the same value (worst case)
        var dict = LargeDictionary.Create<long, long>(
            keyEqualsFunction: (a, b) => a == b,
            keyHashCodeFunction: _ => 42,  // All keys hash to same bucket
            capacity: 10
        );

        dict.Set(KeyMarkerBase, ValueMarkerBase);
        dict.Set(KeyMarkerBase + 1, ValueMarkerBase + 1);
        dict.Set(KeyMarkerBase + 2, ValueMarkerBase + 2);

        await Assert.That(dict.Count).IsEqualTo(3L);
        await Assert.That(dict.Get(KeyMarkerBase)).IsEqualTo(ValueMarkerBase);
        await Assert.That(dict.Get(KeyMarkerBase + 1)).IsEqualTo(ValueMarkerBase + 1);
        await Assert.That(dict.Get(KeyMarkerBase + 2)).IsEqualTo(ValueMarkerBase + 2);
    }

    [Test]
    public async Task Set_UpdatesExistingValue()
    {
        var dict = LargeDictionary.Create<long, long>(capacity: 2);

        dict.Set(KeyMarkerBase, ValueMarkerBase);
        dict.Set(KeyMarkerBase, ValueMarkerBase + 100);

        await Assert.That(dict.Count).IsEqualTo(1L);
        await Assert.That(dict.Get(KeyMarkerBase)).IsEqualTo(ValueMarkerBase + 100);
    }

    [Test]
    public async Task Remove_NonExistentKey_ReturnsFalse()
    {
        var dict = LargeDictionary.Create<long, long>(capacity: 2);
        dict.Set(KeyMarkerBase, ValueMarkerBase);

        bool removed = dict.Remove(KeyMarkerBase + 100);

        await Assert.That(removed).IsFalse();
        await Assert.That(dict.Count).IsEqualTo(1L);
    }

    [Test]
    public async Task Remove_WithValueOut_ReturnsRemovedValue()
    {
        var dict = LargeDictionary.Create<long, long>(capacity: 2);
        dict.Set(KeyMarkerBase, ValueMarkerBase);

        bool removed = dict.Remove(KeyMarkerBase, out long removedValue);

        await Assert.That(removed).IsTrue();
        await Assert.That(removedValue).IsEqualTo(ValueMarkerBase);
    }

    [Test]
    public async Task Shrink_ReducesCapacity()
    {
        var dict = LargeDictionary.Create<long, long>(capacity: 100, minLoadFactor: 0.25);

        for (long i = 0; i < 10; i++)
        {
            dict.Set(KeyMarkerBase + i, ValueMarkerBase + i);
        }

        long capacityBefore = dict.Capacity;
        dict.Shrink();
        long capacityAfter = dict.Capacity;

        await Assert.That(capacityAfter).IsLessThanOrEqualTo(capacityBefore);
        await Assert.That(dict.Count).IsEqualTo(10L);  // Items preserved
    }

    [Test]
    public async Task CaseInsensitiveStringKeys_WorkCorrectly()
    {
        var dict = LargeDictionary.Create<string, int>(
            keyEqualsFunction: (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase),
            keyHashCodeFunction: key => key?.ToUpperInvariant().GetHashCode() ?? 0,
            capacity: 10
        );

        dict.Set("Hello", 1);
        dict.Set("HELLO", 2);  // Should update existing

        await Assert.That(dict.Count).IsEqualTo(1L);
        await Assert.That(dict.Get("hello")).IsEqualTo(2);
        await Assert.That(dict.Get("HELLO")).IsEqualTo(2);
        await Assert.That(dict.Get("Hello")).IsEqualTo(2);
    }

    #endregion

    #region Helper Structs

    private struct SumValueAction : ILargeAction<KeyValuePair<long, long>>
    {
        public long Sum;
        public void Invoke(KeyValuePair<long, long> pair) => Sum += pair.Value;
    }

    #endregion
}



