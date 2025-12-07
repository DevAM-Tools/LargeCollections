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

public class LargeBPlusTreeTest
{
    public static IEnumerable<int> Orders() => new[] { 3, 4, 5, 8, 16, 32, 64, 128 };
    public static IEnumerable<int> ItemCounts() => new[] { 0, 1, 2, 10, 100, 1000, 10000 };

    private const long KeyMarkerBase = 100_000L;
    private const long ValueMarkerBase = 200_000L;

    #region Constructor / Properties

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Constructor_SetsDefaults(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            await Assert.That(() => LargeBPlusTree.Create<long, long>(order)).Throws<Exception>();
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        await Assert.That(tree.Count).IsEqualTo(0L);
        await Assert.That(tree.Order).IsEqualTo(order);
        await Assert.That(tree.Keys.Any()).IsFalse();
        await Assert.That(tree.Values.Any()).IsFalse();
    }

    [Test]
    public async Task Constructor_ThrowsOnInvalidOrder()
    {
        await Assert.That(() => LargeBPlusTree.Create<long, long>(0)).Throws<Exception>();
        await Assert.That(() => LargeBPlusTree.Create<long, long>(1)).Throws<Exception>();
        await Assert.That(() => LargeBPlusTree.Create<long, long>(2)).Throws<Exception>();
    }



    [Test]
    public async Task Factory_CreateDescending_Works()
    {
        LargeBPlusTree<long, long, DescendingComparer<long>> tree = LargeBPlusTree.CreateDescending<long, long>();

        tree.Set(1L, 100L);
        tree.Set(2L, 200L);
        tree.Set(3L, 300L);

        List<long> keys = tree.Keys.ToList();
        await Assert.That(keys).IsEquivalentTo(new[] { 3L, 2L, 1L });
    }

    [Test]
    public async Task Factory_CreateWithDelegateComparer_Works()
    {
        LargeBPlusTree<string, int, DelegateComparer<string>> tree = LargeBPlusTree.Create<string, int>(
            (a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));

        tree.Set("Apple", 1);
        tree.Set("banana", 2);
        tree.Set("CHERRY", 3);

        List<string> keys = tree.Keys.ToList();
        await Assert.That(keys[0]).IsEqualTo("Apple");
        await Assert.That(keys[1]).IsEqualTo("banana");
        await Assert.That(keys[2]).IsEqualTo("CHERRY");
    }

    [Test]
    public async Task Factory_CreateWithStructComparer_Works()
    {
        DefaultComparer<long> comparer = new DefaultComparer<long>();
        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long, DefaultComparer<long>>(comparer);

        tree.Set(1L, 100L);
        await Assert.That(tree[1L]).IsEqualTo(100L);
    }

    #endregion

    #region Indexer / Set / Get

    [Test]
    public async Task Indexer_ThrowsOnNullKey()
    {
        LargeBPlusTree<string, int, DefaultComparer<string>> tree = LargeBPlusTree.Create<string, int>();

        await Assert.That(() => tree[null!]).Throws<Exception>();
        await Assert.That(() => tree[null!] = 1).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Indexer_GetAndSet_StoresValues(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        tree[KeyMarkerBase] = ValueMarkerBase;
        await Assert.That(tree[KeyMarkerBase]).IsEqualTo(ValueMarkerBase);

        tree[KeyMarkerBase] = ValueMarkerBase + 1;
        await Assert.That(tree.Get(KeyMarkerBase)).IsEqualTo(ValueMarkerBase + 1);
    }

    [Test]
    public async Task Get_ThrowsOnNullKey()
    {
        LargeBPlusTree<string, int, DefaultComparer<string>> tree = LargeBPlusTree.Create<string, int>();

        await Assert.That(() => tree.Get(null!)).Throws<Exception>();
    }

    [Test]
    public async Task Get_ThrowsOnMissingKey()
    {
        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>();

        await Assert.That(() => tree.Get(KeyMarkerBase)).Throws<KeyNotFoundException>();
    }

    [Test]
    public async Task Set_ThrowsOnNullKey()
    {
        LargeBPlusTree<string, int, DefaultComparer<string>> tree = LargeBPlusTree.Create<string, int>();

        await Assert.That(() => tree.Set(null!, 0)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Set_ReplacesExistingValue(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        tree.Set(KeyMarkerBase, ValueMarkerBase);
        tree.Set(KeyMarkerBase, ValueMarkerBase + 10L);

        await Assert.That(tree.Count).IsEqualTo(1L);
        await Assert.That(tree.Get(KeyMarkerBase)).IsEqualTo(ValueMarkerBase + 10L);
    }

    #endregion

    #region Add / AddRange

    [Test]
    public async Task Add_ThrowsOnNullKey()
    {
        LargeBPlusTree<string, int, DefaultComparer<string>> tree = LargeBPlusTree.Create<string, int>();

        await Assert.That(() => tree.Add(new KeyValuePair<string, int>(null!, 0))).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Add_AddsUniqueKeys(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        long addCount = 50L;
        for (long i = 0; i < addCount; i++)
        {
            tree.Add(new KeyValuePair<long, long>(KeyMarkerBase + i, ValueMarkerBase + i));
        }

        await Assert.That(tree.Count).IsEqualTo(addCount);

        // Re-add an existing key with a different value - should replace, not add
        tree.Add(new KeyValuePair<long, long>(KeyMarkerBase + 1L, ValueMarkerBase + 999L));
        await Assert.That(tree.Count).IsEqualTo(addCount);
        await Assert.That(tree.Get(KeyMarkerBase + 1L)).IsEqualTo(ValueMarkerBase + 999L);
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task AddRange_IEnumerable_AddsAllItems(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        List<KeyValuePair<long, long>> items = new List<KeyValuePair<long, long>>();
        for (long i = 0; i < 100; i++)
        {
            items.Add(new KeyValuePair<long, long>(KeyMarkerBase + i, ValueMarkerBase + i));
        }

        tree.AddRange(items);

        await Assert.That(tree.Count).IsEqualTo(100L);
        for (long i = 0; i < 100; i++)
        {
            await Assert.That(tree.Get(KeyMarkerBase + i)).IsEqualTo(ValueMarkerBase + i);
        }
    }

    [Test]
    public async Task AddRange_ThrowsOnNull()
    {
        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>();

        await Assert.That(() => tree.AddRange((IEnumerable<KeyValuePair<long, long>>)null!)).Throws<Exception>();
    }

    #endregion

    #region ContainsKey / TryGetValue / Contains

    [Test]
    public async Task ContainsKey_ThrowsOnNullKey()
    {
        LargeBPlusTree<string, int, DefaultComparer<string>> tree = LargeBPlusTree.Create<string, int>();

        await Assert.That(() => tree.ContainsKey(null!)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task ContainsKey_ReturnsCorrectResult(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        tree.Set(KeyMarkerBase, ValueMarkerBase);

        await Assert.That(tree.ContainsKey(KeyMarkerBase)).IsTrue();
        await Assert.That(tree.ContainsKey(KeyMarkerBase + 1)).IsFalse();
    }

    [Test]
    public async Task TryGetValue_ThrowsOnNullKey()
    {
        LargeBPlusTree<string, int, DefaultComparer<string>> tree = LargeBPlusTree.Create<string, int>();

        await Assert.That(() => tree.TryGetValue(null!, out _)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task TryGetValue_ReturnsCorrectResult(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        tree.Set(KeyMarkerBase, ValueMarkerBase);

        bool found1 = tree.TryGetValue(KeyMarkerBase, out long value1);
        await Assert.That(found1).IsTrue();
        await Assert.That(value1).IsEqualTo(ValueMarkerBase);

        bool found2 = tree.TryGetValue(KeyMarkerBase + 1, out long value2);
        await Assert.That(found2).IsFalse();
        await Assert.That(value2).IsEqualTo(default(long));
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Contains_KeyValuePair_ReturnsCorrectResult(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        tree.Set(KeyMarkerBase, ValueMarkerBase);

        await Assert.That(tree.Contains(new KeyValuePair<long, long>(KeyMarkerBase, ValueMarkerBase))).IsTrue();
        await Assert.That(tree.Contains(new KeyValuePair<long, long>(KeyMarkerBase, ValueMarkerBase + 1))).IsFalse();
        await Assert.That(tree.Contains(new KeyValuePair<long, long>(KeyMarkerBase + 1, ValueMarkerBase))).IsFalse();
    }

    #endregion

    #region Remove

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Remove_ByKey_RemovesItem(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        for (long i = 0; i < 20; i++)
        {
            tree.Set(KeyMarkerBase + i, ValueMarkerBase + i);
        }

        await Assert.That(tree.Count).IsEqualTo(20L);

        bool removed = tree.Remove(KeyMarkerBase + 5);
        await Assert.That(removed).IsTrue();
        await Assert.That(tree.Count).IsEqualTo(19L);
        await Assert.That(tree.ContainsKey(KeyMarkerBase + 5)).IsFalse();

        // Verify other items still exist
        await Assert.That(tree.ContainsKey(KeyMarkerBase)).IsTrue();
        await Assert.That(tree.ContainsKey(KeyMarkerBase + 19)).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Remove_ByKey_WithRemovedValue_ReturnsValue(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        tree.Set(KeyMarkerBase, ValueMarkerBase);

        bool removed = tree.Remove(KeyMarkerBase, out long removedValue);
        await Assert.That(removed).IsTrue();
        await Assert.That(removedValue).IsEqualTo(ValueMarkerBase);
        await Assert.That(tree.Count).IsEqualTo(0L);
    }

    [Test]
    public async Task Remove_ThrowsOnNullKey()
    {
        LargeBPlusTree<string, int, DefaultComparer<string>> tree = LargeBPlusTree.Create<string, int>();

        await Assert.That(() => tree.Remove((string)null!)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Remove_NonExistentKey_ReturnsFalse(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        tree.Set(KeyMarkerBase, ValueMarkerBase);

        bool removed = tree.Remove(KeyMarkerBase + 999);
        await Assert.That(removed).IsFalse();
        await Assert.That(tree.Count).IsEqualTo(1L);
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Remove_KeyValuePair_RemovesItem(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        tree.Set(KeyMarkerBase, ValueMarkerBase);

        bool removed = tree.Remove(new KeyValuePair<long, long>(KeyMarkerBase, ValueMarkerBase));
        await Assert.That(removed).IsTrue();
        await Assert.That(tree.Count).IsEqualTo(0L);
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Remove_KeyValuePair_WithRemovedItem_ReturnsItem(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        tree.Set(KeyMarkerBase, ValueMarkerBase);

        bool removed = tree.Remove(new KeyValuePair<long, long>(KeyMarkerBase, ValueMarkerBase), out KeyValuePair<long, long> removedItem);
        await Assert.That(removed).IsTrue();
        await Assert.That(removedItem.Key).IsEqualTo(KeyMarkerBase);
        await Assert.That(removedItem.Value).IsEqualTo(ValueMarkerBase);
    }

    #endregion

    #region Clear

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Clear_RemovesAllItems(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        for (long i = 0; i < 100; i++)
        {
            tree.Set(KeyMarkerBase + i, ValueMarkerBase + i);
        }

        await Assert.That(tree.Count).IsEqualTo(100L);

        tree.Clear();

        await Assert.That(tree.Count).IsEqualTo(0L);
        await Assert.That(tree.Keys.Any()).IsFalse();
        await Assert.That(tree.Values.Any()).IsFalse();
    }

    #endregion

    #region Keys / Values / GetAll / Enumeration

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Keys_ReturnsKeysInOrder(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        // Add in random order
        long[] keysToAdd = { 5, 3, 8, 1, 9, 2, 7, 4, 6, 10 };
        foreach (long key in keysToAdd)
        {
            tree.Set(key, key * 10);
        }

        List<long> keys = tree.Keys.ToList();
        await Assert.That(keys.Count).IsEqualTo(10);

        // Should be in ascending order
        for (int i = 0; i < keys.Count - 1; i++)
        {
            await Assert.That(keys[i] < keys[i + 1]).IsTrue();
        }
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task Values_ReturnsValuesInKeyOrder(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        // Add in random order
        long[] keysToAdd = { 5, 3, 8, 1, 9, 2, 7, 4, 6, 10 };
        foreach (long key in keysToAdd)
        {
            tree.Set(key, key * 10);
        }

        List<long> values = tree.Values.ToList();
        await Assert.That(values.Count).IsEqualTo(10);

        // Values should correspond to sorted keys
        List<long> expectedValues = keysToAdd.OrderBy(k => k).Select(k => k * 10).ToList();
        await Assert.That(values).IsEquivalentTo(expectedValues);
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task GetAll_ReturnsAllPairsInOrder(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        long[] keysToAdd = { 5, 3, 8, 1, 9, 2, 7, 4, 6, 10 };
        foreach (long key in keysToAdd)
        {
            tree.Set(key, key * 10);
        }

        List<KeyValuePair<long, long>> pairs = tree.GetAll().ToList();
        await Assert.That(pairs.Count).IsEqualTo(10);

        for (int i = 0; i < pairs.Count - 1; i++)
        {
            await Assert.That(pairs[i].Key < pairs[i + 1].Key).IsTrue();
        }
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task GetEnumerator_ReturnsAllPairsInOrder(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        long[] keysToAdd = { 5, 3, 8, 1, 9, 2, 7, 4, 6, 10 };
        foreach (long key in keysToAdd)
        {
            tree.Set(key, key * 10);
        }

        List<KeyValuePair<long, long>> pairs = new List<KeyValuePair<long, long>>();
        foreach (KeyValuePair<long, long> pair in tree)
        {
            pairs.Add(pair);
        }

        await Assert.That(pairs.Count).IsEqualTo(10);

        for (int i = 0; i < pairs.Count - 1; i++)
        {
            await Assert.That(pairs[i].Key < pairs[i + 1].Key).IsTrue();
        }
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task DoForEach_Delegate_IteratesAllItems(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        for (long i = 0; i < 50; i++)
        {
            tree.Set(i, i * 10);
        }

        long sum = 0;
        tree.DoForEach(pair => sum += pair.Value);

        long expectedSum = Enumerable.Range(0, 50).Select(i => (long)i * 10).Sum();
        await Assert.That(sum).IsEqualTo(expectedSum);
    }

    [Test]
    public async Task DoForEach_ThrowsOnNull()
    {
        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>();

        await Assert.That(() => tree.DoForEach((Action<KeyValuePair<long, long>>)null!)).Throws<Exception>();
    }

    #endregion

    #region Range Queries

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task GetRange_ReturnsItemsInRange(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        for (long i = 1; i <= 100; i++)
        {
            tree.Set(i, i * 10);
        }

        List<KeyValuePair<long, long>> range = tree.GetRange(25, 75).ToList();

        await Assert.That(range.Count).IsEqualTo(51); // 25 to 75 inclusive
        await Assert.That(range.First().Key).IsEqualTo(25L);
        await Assert.That(range.Last().Key).IsEqualTo(75L);

        // Verify ordering
        for (int i = 0; i < range.Count - 1; i++)
        {
            await Assert.That(range[i].Key < range[i + 1].Key).IsTrue();
        }
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task GetRange_EmptyWhenMinGreaterThanMax(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        for (long i = 1; i <= 100; i++)
        {
            tree.Set(i, i * 10);
        }

        List<KeyValuePair<long, long>> range = tree.GetRange(75, 25).ToList();
        await Assert.That(range.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetRange_ThrowsOnNullKeys()
    {
        LargeBPlusTree<string, int, DefaultComparer<string>> tree = LargeBPlusTree.Create<string, int>();

        await Assert.That(() => tree.GetRange(null!, "z").ToList()).Throws<Exception>();
        await Assert.That(() => tree.GetRange("a", null!).ToList()).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task GetKeysInRange_ReturnsKeysInRange(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        for (long i = 1; i <= 100; i++)
        {
            tree.Set(i, i * 10);
        }

        List<long> keys = tree.GetKeysInRange(10, 20).ToList();

        await Assert.That(keys.Count).IsEqualTo(11); // 10 to 20 inclusive
        await Assert.That(keys.First()).IsEqualTo(10L);
        await Assert.That(keys.Last()).IsEqualTo(20L);
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task GetValuesInRange_ReturnsValuesInRange(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        for (long i = 1; i <= 100; i++)
        {
            tree.Set(i, i * 10);
        }

        List<long> values = tree.GetValuesInRange(10, 20).ToList();

        await Assert.That(values.Count).IsEqualTo(11);
        await Assert.That(values.First()).IsEqualTo(100L); // 10 * 10
        await Assert.That(values.Last()).IsEqualTo(200L);  // 20 * 10
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task CountInRange_ReturnsCorrectCount(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        for (long i = 1; i <= 100; i++)
        {
            tree.Set(i, i * 10);
        }

        await Assert.That(tree.CountInRange(25, 75)).IsEqualTo(51L);
        await Assert.That(tree.CountInRange(1, 100)).IsEqualTo(100L);
        await Assert.That(tree.CountInRange(50, 50)).IsEqualTo(1L);
        await Assert.That(tree.CountInRange(101, 200)).IsEqualTo(0L);
        await Assert.That(tree.CountInRange(75, 25)).IsEqualTo(0L);
    }

    #endregion

    #region Min/Max Key Methods

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task GetMinKey_ReturnsSmallestKey(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        // Add in random order
        long[] keysToAdd = { 50, 30, 80, 10, 90, 20, 70, 40, 60, 100 };
        foreach (long key in keysToAdd)
        {
            tree.Set(key, key * 10);
        }

        await Assert.That(tree.GetMinKey()).IsEqualTo(10L);
    }

    [Test]
    public async Task GetMinKey_ThrowsWhenEmpty()
    {
        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>();

        await Assert.That(() => tree.GetMinKey()).Throws<InvalidOperationException>();
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task GetMaxKey_ReturnsLargestKey(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        // Add in random order
        long[] keysToAdd = { 50, 30, 80, 10, 90, 20, 70, 40, 60, 100 };
        foreach (long key in keysToAdd)
        {
            tree.Set(key, key * 10);
        }

        await Assert.That(tree.GetMaxKey()).IsEqualTo(100L);
    }

    [Test]
    public async Task GetMaxKey_ThrowsWhenEmpty()
    {
        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>();

        await Assert.That(() => tree.GetMaxKey()).Throws<InvalidOperationException>();
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task TryGetMinKey_ReturnsTrueAndKey(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        tree.Set(50, 500);
        tree.Set(25, 250);
        tree.Set(75, 750);

        bool found = tree.TryGetMinKey(out long minKey);
        await Assert.That(found).IsTrue();
        await Assert.That(minKey).IsEqualTo(25L);
    }

    [Test]
    public async Task TryGetMinKey_ReturnsFalseWhenEmpty()
    {
        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>();

        bool found = tree.TryGetMinKey(out long minKey);
        await Assert.That(found).IsFalse();
        await Assert.That(minKey).IsEqualTo(default(long));
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task TryGetMaxKey_ReturnsTrueAndKey(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        tree.Set(50, 500);
        tree.Set(25, 250);
        tree.Set(75, 750);

        bool found = tree.TryGetMaxKey(out long maxKey);
        await Assert.That(found).IsTrue();
        await Assert.That(maxKey).IsEqualTo(75L);
    }

    [Test]
    public async Task TryGetMaxKey_ReturnsFalseWhenEmpty()
    {
        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>();

        bool found = tree.TryGetMaxKey(out long maxKey);
        await Assert.That(found).IsFalse();
        await Assert.That(maxKey).IsEqualTo(default(long));
    }

    #endregion

    #region Large Scale / Stress Tests

    public readonly record struct OrderAndItemCount(int Order, int ItemCount);

    public static IEnumerable<OrderAndItemCount> OrdersAndItemCounts()
    {
        int[] orders = { 3, 4, 5, 8, 16, 32, 64, 128 };
        int[] itemCounts = { 0, 1, 2, 10, 100, 200 };

        foreach (int order in orders)
        {
            foreach (int itemCount in itemCounts)
            {
                yield return new OrderAndItemCount(order, itemCount);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(OrdersAndItemCounts))]
    public async Task LargeScale_InsertAndRetrieve(OrderAndItemCount args)
    {
        if (args.Order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(args.Order);

        // Insert items
        for (long i = 0; i < args.ItemCount; i++)
        {
            tree.Set(KeyMarkerBase + i, ValueMarkerBase + i);
        }

        await Assert.That(tree.Count).IsEqualTo(args.ItemCount);

        // Verify all items exist
        for (long i = 0; i < args.ItemCount; i++)
        {
            await Assert.That(tree.Get(KeyMarkerBase + i)).IsEqualTo(ValueMarkerBase + i);
        }

        // Verify ordering
        List<long> keys = tree.Keys.ToList();
        for (int i = 0; i < keys.Count - 1; i++)
        {
            await Assert.That(keys[i] < keys[i + 1]).IsTrue();
        }
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task LargeScale_RandomOrderInsert(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        // Create random order keys (limit to 200 to fit in UNIT_TEST builds)
        List<long> keysToInsert = Enumerable.Range(0, 200).Select(i => (long)i).ToList();
        Random random = new Random(42); // Fixed seed for reproducibility
        for (int i = keysToInsert.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            long temp = keysToInsert[i];
            keysToInsert[i] = keysToInsert[j];
            keysToInsert[j] = temp;
        }

        // Insert in random order
        foreach (long key in keysToInsert)
        {
            tree.Set(key, key * 10);
        }

        await Assert.That(tree.Count).IsEqualTo(200L);

        // Verify keys are in sorted order
        List<long> sortedKeys = tree.Keys.ToList();
        for (int i = 0; i < sortedKeys.Count; i++)
        {
            await Assert.That(sortedKeys[i]).IsEqualTo(i);
        }
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task LargeScale_InsertAndRemove(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        // Insert items (limit to 200 to fit in UNIT_TEST builds)
        int itemCount = 200;
        for (long i = 0; i < itemCount; i++)
        {
            tree.Set(i, i * 10);
        }

        // Remove every other item
        for (long i = 0; i < itemCount; i += 2)
        {
            bool removed = tree.Remove(i);
            await Assert.That(removed).IsTrue();
        }

        await Assert.That(tree.Count).IsEqualTo(itemCount / 2);

        // Verify remaining items
        for (long i = 1; i < itemCount; i += 2)
        {
            await Assert.That(tree.ContainsKey(i)).IsTrue();
            await Assert.That(tree.Get(i)).IsEqualTo(i * 10);
        }

        // Verify removed items are gone
        for (long i = 0; i < itemCount; i += 2)
        {
            await Assert.That(tree.ContainsKey(i)).IsFalse();
        }
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task LargeScale_RemoveAll(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        // Limit to 150 to fit in UNIT_TEST builds
        int itemCount = 150;
        for (long i = 0; i < itemCount; i++)
        {
            tree.Set(i, i * 10);
        }

        // Remove all items one by one
        for (long i = 0; i < itemCount; i++)
        {
            bool removed = tree.Remove(i);
            await Assert.That(removed).IsTrue();
        }

        await Assert.That(tree.Count).IsEqualTo(0L);
        await Assert.That(tree.Keys.Any()).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task LargeScale_RandomRemove(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        // Limit to 200 to fit in UNIT_TEST builds
        int itemCount = 200;
        for (long i = 0; i < itemCount; i++)
        {
            tree.Set(i, i * 10);
        }

        // Create random order for removal
        List<long> keysToRemove = Enumerable.Range(0, itemCount).Select(i => (long)i).ToList();
        Random random = new Random(42);
        for (int i = keysToRemove.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            long temp = keysToRemove[i];
            keysToRemove[i] = keysToRemove[j];
            keysToRemove[j] = temp;
        }

        // Remove in random order
        foreach (long key in keysToRemove)
        {
            bool removed = tree.Remove(key);
            await Assert.That(removed).IsTrue();
        }

        await Assert.That(tree.Count).IsEqualTo(0L);
    }

    #endregion

    #region Node Splitting / Merging Tests

    [Test]
    public async Task NodeSplitting_TriggeredWhenLeafFull()
    {
        // Use minimum order to trigger splits quickly
        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(3);

        // Order 3 means leaf can hold 2 keys before split
        tree.Set(1, 10);
        tree.Set(2, 20);
        tree.Set(3, 30); // This should trigger split

        await Assert.That(tree.Count).IsEqualTo(3L);
        await Assert.That(tree.Get(1)).IsEqualTo(10L);
        await Assert.That(tree.Get(2)).IsEqualTo(20L);
        await Assert.That(tree.Get(3)).IsEqualTo(30L);
    }

    [Test]
    public async Task NodeMerging_TriggeredOnUnderflow()
    {
        // Use minimum order to trigger merges
        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(3);

        // Insert enough to create multiple leaves
        for (int i = 1; i <= 10; i++)
        {
            tree.Set(i, i * 10);
        }

        // Remove items to trigger underflow and merging
        for (int i = 1; i <= 8; i++)
        {
            tree.Remove(i);
        }

        await Assert.That(tree.Count).IsEqualTo(2L);
        await Assert.That(tree.Get(9)).IsEqualTo(90L);
        await Assert.That(tree.Get(10)).IsEqualTo(100L);
    }

    #endregion

    #region DoForEach with Struct Action

    private struct SumAction : ILargeAction<KeyValuePair<long, long>>
    {
        public long KeySum;
        public long ValueSum;

        public void Invoke(KeyValuePair<long, long> item)
        {
            KeySum += item.Key;
            ValueSum += item.Value;
        }
    }

    [Test]
    [MethodDataSource(nameof(Orders))]
    public async Task DoForEach_StructAction_IteratesAllItems(int order)
    {
        if (order < LargeBPlusTree.MinOrder)
        {
            return;
        }

        LargeBPlusTree<long, long, DefaultComparer<long>> tree = LargeBPlusTree.Create<long, long>(order);

        for (long i = 1; i <= 50; i++)
        {
            tree.Set(i, i * 10);
        }

        SumAction action = new SumAction();
        tree.DoForEach(ref action);

        long expectedKeySum = Enumerable.Range(1, 50).Select(i => (long)i).Sum();
        long expectedValueSum = Enumerable.Range(1, 50).Select(i => (long)i * 10).Sum();

        await Assert.That(action.KeySum).IsEqualTo(expectedKeySum);
        await Assert.That(action.ValueSum).IsEqualTo(expectedValueSum);
    }

    #endregion
}
