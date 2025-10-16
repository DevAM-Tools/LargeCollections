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

namespace LargeCollections.Test;

public class LargeListTest
{
    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task Create(long capacity)
    {
        LargeList<long> largeList;
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            await Assert.That(() => largeList = new LargeList<long>(capacity)).Throws<ArgumentOutOfRangeException>();
            return;
        }

        // Test 1: Constructor with capacity
        largeList = new LargeList<long>(capacity);
        await Assert.That(largeList.Count).IsEqualTo(0L);
        await Assert.That(largeList.Capacity).IsEqualTo(capacity);

        // Test 2: Default constructor
        LargeList<long> defaultList = [];
        await Assert.That(defaultList.Count).IsEqualTo(0L);
        await Assert.That(defaultList.Capacity).IsGreaterThanOrEqualTo(0L);

        // Test 3: Constructor with collection
        if (capacity <= int.MaxValue)
        {
            long[] sourceArray = new long[capacity];
            LargeList<long> listFromArray = new(sourceArray);
            await Assert.That(listFromArray.Count).IsEqualTo(capacity);
            await Assert.That(listFromArray.Capacity).IsGreaterThanOrEqualTo(capacity);
        }

        // Test 4: Null collection throws exception
        await Assert.That(() => new LargeList<long>((IEnumerable<long>)null)).Throws<ArgumentNullException>();
        _ = new LargeList<long>((ReadOnlySpan<long>)null);
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task AddAndAddRange(long capacity)
    {
        if (capacity < 0L || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeList<long> largeList = new(capacity);

        // Test 1: Individual Add operations
        for (long i = 0; i < capacity; i++)
        {
            largeList.Add(i);
            await Assert.That(largeList.Count).IsEqualTo(i + 1L);
            await Assert.That(largeList[i]).IsEqualTo(i);
        }

        // Clear for next tests
        largeList.Clear();

        // Test 2: AddRange with IEnumerable (array)
        if (capacity <= int.MaxValue)
        {
            long[] sourceArray = new long[capacity];
            for (long i = 0; i < capacity; i++)
            {
                sourceArray[i] = i * 2;
            }
            largeList.AddRange(sourceArray);
            await Assert.That(largeList.Count).IsEqualTo(capacity);
            for (long i = 0; i < capacity; i++)
            {
                await Assert.That(largeList[i]).IsEqualTo(i * 2);
            }
        }

        // Clear for next tests
        largeList.Clear();

        // Test 3: AddRange with LargeEnumerable
        largeList.AddRange(LargeEnumerable.Range(capacity));
        await Assert.That(largeList.Count).IsEqualTo(capacity);
        for (long i = 0; i < capacity; i++)
        {
            await Assert.That(largeList[i]).IsEqualTo(i);
        }

        // Test 4: AddRange with null IEnumerable
        await Assert.That(() => largeList.AddRange((IEnumerable<long>)null)).Throws<ArgumentNullException>();

        // Clear for next tests
        largeList.Clear();

        // Test 5: AddRange with empty collection
        largeList.AddRange(Array.Empty<long>());
        await Assert.That(largeList.Count).IsEqualTo(0L);

        // Test 6: AddRange with empty LargeEnumerable
        largeList.AddRange(LargeEnumerable.Range(0L));
        await Assert.That(largeList.Count).IsEqualTo(0L);

        // Clear for next tests
        largeList.Clear();

        // Test 7: Multiple AddRange operations
        if (capacity > 0)
        {
            long halfCapacity = capacity / 2;
            largeList.AddRange(LargeEnumerable.Range(halfCapacity));
            largeList.AddRange(LargeEnumerable.Range(halfCapacity, capacity - halfCapacity));
            await Assert.That(largeList.Count).IsEqualTo(capacity);
            for (long i = 0; i < capacity; i++)
            {
                await Assert.That(largeList[i]).IsEqualTo(i);
            }
        }

        // Clear for next tests
        largeList.Clear();

        // Test 8: Add beyond initial capacity to test auto-expansion
        for (long i = 0; i < 10; i++)
        {
            largeList.Add(i);
        }
        await Assert.That(largeList.Count).IsEqualTo(10L);
        await Assert.That(largeList.Capacity).IsGreaterThanOrEqualTo(10L);
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task Remove(long capacity)
    {
        if (capacity < 0L || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeList<long> largeList = new(capacity);

        // Fill list for testing removal
        for (long i = 0; i < capacity; i++)
        {
            largeList.Add(i);
        }

        // Test 1: RemoveAt from different positions
        for (long i = 0; i < capacity; i++)
        {
            if (i % 2 == 0)
            {
                largeList.RemoveAt(largeList.Count - 1L);
            }
            else
            {
                largeList.RemoveAt(0L);
            }

            long expectedValue = capacity - 1L - i;
            await Assert.That(largeList.Count).IsEqualTo(expectedValue);
        }

        // Test 2: RemoveAt on empty list throws exception
        await Assert.That(() => largeList.RemoveAt(0L)).Throws<IndexOutOfRangeException>();

        // Test 3: RemoveAt with invalid index throws exception
        largeList.Add(42L);
        await Assert.That(() => largeList.RemoveAt(-1L)).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeList.RemoveAt(1L)).Throws<IndexOutOfRangeException>();
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task ClearShrinkAndCapacity(long capacity)
    {
        if (capacity < 0L || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeList<long> largeList = new(capacity);

        // Fill list for testing
        for (long i = 0; i < capacity; i++)
        {
            largeList.Add(i);
        }

        // Test 1: Clear operation
        largeList.Clear();
        await Assert.That(largeList.Count).IsEqualTo(0L);

        // Test 2: Shrink operation
        largeList.Shrink();
        await Assert.That(largeList.Capacity).IsEqualTo(0L);

        // Test 3: Index access on empty list throws exception
        await Assert.That(() => largeList[0L]).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeList[-1L]).Throws<IndexOutOfRangeException>();

        // Test 4: Set operation on empty list throws exception
        await Assert.That(() => largeList[0L] = 42L).Throws<IndexOutOfRangeException>();

        // Test 5: Get operation on empty list throws exception
        await Assert.That(() => largeList.Get(0L)).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeList.Get(-1L)).Throws<IndexOutOfRangeException>();

        // Test 6: GetRef operation on empty list throws exception (if IRefAccessLargeList is implemented)
        if (largeList is IRefAccessLargeList<long> refAccess)
        {
            await Assert.That(() => refAccess.GetRef(0L)).Throws<IndexOutOfRangeException>();
        }

        // Test 7: Set operation on empty list throws exception
        await Assert.That(() => largeList.Set(0L, 42L)).Throws<IndexOutOfRangeException>();

        // Test 8: Add one item and test boundary conditions
        largeList.Add(42L);
        await Assert.That(largeList.Count).IsEqualTo(1L);

        // Test 9: Valid access should work
        await Assert.That(largeList[0L]).IsEqualTo(42L);
        await Assert.That(largeList.Get(0L)).IsEqualTo(42L);

        // Test 10: Invalid indices should throw
        await Assert.That(() => largeList[1L]).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeList[-1L]).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeList.Get(1L)).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeList.Get(-1L)).Throws<IndexOutOfRangeException>();
        // Test GetRef bounds with IRefAccessLargeList interface
        if (largeList is IRefAccessLargeList<long> refAccess2)
        {
            await Assert.That(() => refAccess2.GetRef(1L)).Throws<IndexOutOfRangeException>();
            await Assert.That(() => refAccess2.GetRef(-1L)).Throws<IndexOutOfRangeException>();
        }
        await Assert.That(() => largeList.Set(1L, 99L)).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeList.Set(-1L, 99L)).Throws<IndexOutOfRangeException>();

        // Test 11: Setter with invalid indices
        await Assert.That(() => largeList[1L] = 99L).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeList[-1L] = 99L).Throws<IndexOutOfRangeException>();

        // Test 12: EnsureRemainingCapacity with valid value
        largeList.Clear();
        largeList.Add(42L);
        largeList.Shrink();
        largeList.EnsureRemainingCapacity(10L);
        await Assert.That(largeList.Capacity).IsGreaterThanOrEqualTo(largeList.Count + 10L);

        // Test 13: EnsureRemainingCapacity with negative value
        await Assert.That(() => largeList.EnsureRemainingCapacity(-1L)).Throws<ArgumentOutOfRangeException>();

        // Test 14: Shrink operation after EnsureRemainingCapacity
        largeList.Shrink();
        await Assert.That(largeList.Capacity).IsEqualTo(1L);
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesWithOffsetTestCasesArguments))]
    public async Task DoForEach(long capacity, long offset)
    {
        // input check
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> largeArray = new(capacity);

        await LargeArrayTest.DoForEachTest(largeArray, offset);
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesWithOffsetTestCasesArguments))]
    public async Task SetGet(long capacity, long offset)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeList<long> largeList = new(capacity);
        largeList.AddRange(LargeEnumerable.Range(capacity));

        await LargeArrayTest.SetGetTest(largeList, offset);
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesWithOffsetTestCasesArguments))]
    public async Task Enumeration(long capacity, long offset)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeList<long> largeList = new(capacity);
        largeList.AddRange(LargeEnumerable.Range(capacity));

        await LargeArrayTest.EnumerationTest(largeList, offset);
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesWithOffsetTestCasesArguments))]
    public async Task Sort(long capacity, long offset)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeList<long> largeList = new(capacity);
        largeList.AddRange(LargeEnumerable.Range(capacity));

        await LargeArrayTest.SortTest(largeList, offset);
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesWithOffsetTestCasesArguments))]
    public async Task BinarySearch(long capacity, long offset)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeList<long> largeList = new(capacity);
        largeList.AddRange(LargeEnumerable.Range(capacity));

        await LargeArrayTest.BinarySearchTest(largeList, offset);
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesWithOffsetTestCasesArguments))]
    public async Task Contains(long capacity, long offset)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeList<long> largeList = new(capacity);
        largeList.AddRange(LargeEnumerable.Range(capacity));

        await LargeArrayTest.ContainsTest(largeList, offset);
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesWithOffsetTestCasesArguments))]
    public async Task Copy(long capacity, long offset)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeList<long> largeList = new(capacity);
        largeList.AddRange(LargeEnumerable.Range(capacity));

        // Test with LargeArray as target
        await LargeArrayTest.CopyTest(largeList, offset, capacity => new LargeArray<long>(capacity));

        // Test with LargeList as target
        await LargeArrayTest.CopyTest(largeList, offset, capacity => new LargeList<long>(LargeEnumerable.Repeat(1L, capacity), capacity));
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesWithOffsetTestCasesArguments))]
    public async Task RefAccess(long capacity, long offset)
    {
        // input check
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeList<long> largeList = new(capacity);

        // Fill the list with test data
        for (long i = 0; i < capacity; i++)
        {
            largeList.Add(i);
        }

        // Test IRefAccess functionality
        await LargeArrayTest.TestAllRefAccess(largeList, capacity, offset);
    }

    [Test]
    public async Task MinLoadFactorShrinking()
    {
        // Test 1: Create list with MinLoadFactor 0.5 
        LargeList<int> largeList = new(capacity: 100L, minLoadFactor: 0.5);

        await Assert.That(largeList.MinLoadFactor).IsEqualTo(0.5);
        await Assert.That(largeList.Capacity).IsEqualTo(100L);
        await Assert.That(largeList.Count).IsEqualTo(0L);

        // Test 2: Fill list to capacity
        for (int i = 0; i < 80; i++)
        {
            largeList.Add(i);
        }

        long capacityAfterFill = largeList.Capacity;
        await Assert.That(largeList.Count).IsEqualTo(80L);

        // Test 3: Remove many items to trigger shrinking
        // Remove 60 items, leaving 20 items
        // Load factor will be 20/100 = 0.2, which is < 0.5 MinLoadFactor
        for (int i = 0; i < 60; i++)
        {
            largeList.RemoveAt(largeList.Count - 1);
        }

        // Test 4: Verify shrinking occurred
        await Assert.That(largeList.Count).IsEqualTo(20L);
        await Assert.That(largeList.Capacity).IsLessThan(capacityAfterFill);

        // Test 5: Verify remaining items are correct
        for (int i = 0; i < 20; i++)
        {
            await Assert.That(largeList[i]).IsEqualTo(i);
        }

        // Test 6: Test with MinLoadFactor = 0 (disabled)
        LargeList<int> noShrinkList = new(capacity: 100L, minLoadFactor: 0.0);

        for (int i = 0; i < 80; i++)
        {
            noShrinkList.Add(i);
        }

        long capacityBeforeRemoval = noShrinkList.Capacity;

        // Remove many items
        for (int i = 0; i < 60; i++)
        {
            noShrinkList.RemoveAt(noShrinkList.Count - 1);
        }

        // Capacity should remain unchanged since MinLoadFactor is 0
        await Assert.That(noShrinkList.Capacity).IsEqualTo(capacityBeforeRemoval);
        await Assert.That(noShrinkList.Count).IsEqualTo(20L);
    }

    [Test]
    public async Task MinLoadFactorConstructorValidation()
    {
        // Test 1: Valid MinLoadFactor values
        LargeList<int> validList1 = new(minLoadFactor: 0.0);
        await Assert.That(validList1.MinLoadFactor).IsEqualTo(0.0);

        LargeList<int> validList2 = new(minLoadFactor: 0.5);
        await Assert.That(validList2.MinLoadFactor).IsEqualTo(0.5);

        LargeList<int> validList3 = new(minLoadFactor: 0.99);
        await Assert.That(validList3.MinLoadFactor).IsEqualTo(0.99);

        // Test 2: Invalid MinLoadFactor values should throw ArgumentOutOfRangeException
        await Assert.That(() => new LargeList<int>(minLoadFactor: -0.1))
            .Throws<ArgumentOutOfRangeException>();

        await Assert.That(() => new LargeList<int>(minLoadFactor: 1.0))
            .Throws<ArgumentOutOfRangeException>();

        await Assert.That(() => new LargeList<int>(minLoadFactor: 1.5))
            .Throws<ArgumentOutOfRangeException>();

        // Test 3: Test constructors with IEnumerable
        int[] items = [1, 2, 3, 4, 5];
        LargeList<int> listFromEnum = new(items, minLoadFactor: 0.3);
        await Assert.That(listFromEnum.MinLoadFactor).IsEqualTo(0.3);
        await Assert.That(listFromEnum.Count).IsEqualTo(5L);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        // Test 4: Test constructors with ReadOnlySpan
        ReadOnlySpan<int> span = items.AsSpan();
        LargeList<int> listFromSpan = new(span, minLoadFactor: 0.4);
        await Assert.That(listFromSpan.MinLoadFactor).IsEqualTo(0.4);
        await Assert.That(listFromSpan.Count).IsEqualTo(5L);
#endif
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task GetAllMethods(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return; // Skip invalid capacities
        }

        LargeList<long> largeList = new LargeList<long>(capacity);

        // Add test data
        for (long i = 0; i < Math.Min(capacity, 10L); i++)
        {
            largeList.Add(i);
        }

        // Test 1: GetAll() - returns all elements
        List<long> allElements = largeList.GetAll().ToList();
        await Assert.That(allElements.Count).IsEqualTo((int)largeList.Count);

        if (largeList.Count > 0)
        {
            await Assert.That(allElements).IsEquivalentTo(LargeEnumerable.Range(largeList.Count));
        }

        // Test 2: GetAll(offset, count) - returns range
        if (largeList.Count >= 3)
        {
            List<long> rangeElements = largeList.GetAll(1, 2).ToList();
            await Assert.That(rangeElements).IsEquivalentTo(new[] { 1L, 2L });
        }

        // Test 3: GetAll with empty range
        if (largeList.Count > 0)
        {
            List<long> emptyRange = largeList.GetAll(0, 0).ToList();
            await Assert.That(emptyRange).IsEmpty();
        }

        // Test 4: GetAll exception handling
        if (largeList.Count > 0)
        {
            await Assert.That(() => largeList.GetAll(-1, 1).ToList()).ThrowsExactly<ArgumentException>();
            await Assert.That(() => largeList.GetAll(0, -1).ToList()).ThrowsExactly<ArgumentException>();
            await Assert.That(() => largeList.GetAll(largeList.Count, 1).ToList()).ThrowsExactly<ArgumentException>();
        }
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesWithOffsetTestCasesArguments))]
    public async Task RemoveVariants(long capacity, long offset)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount || offset < 0 || offset >= Math.Max(capacity, 1L))
        {
            return; // Skip invalid parameters
        }

        // Test 1: Remove with preserveOrder = false
        LargeList<long> list1 = new LargeList<long>();
        for (long i = 0; i < Math.Min(capacity, 10L); i++)
        {
            list1.Add(i);
        }

        if (list1.Count > 0)
        {
            long itemToRemove = list1[0];
            bool removed1 = list1.Remove(itemToRemove, preserveOrder: false);
            await Assert.That(removed1).IsTrue();
            await Assert.That(list1.Contains(itemToRemove)).IsFalse();
        }

        // Test 2: Remove with preserveOrder = true
        LargeList<long> list2 = new LargeList<long>();
        for (long i = 0; i < Math.Min(capacity, 10L); i++)
        {
            list2.Add(i);
        }

        if (list2.Count > 2)
        {
            long originalCount = list2.Count;
            bool removed2 = list2.Remove(1L, preserveOrder: true);
            await Assert.That(removed2).IsTrue();
            await Assert.That(list2.Count).IsEqualTo(originalCount - 1);
            // Order should be preserved
            if (list2.Count > 1)
            {
                await Assert.That(list2[0]).IsEqualTo(0L);
                await Assert.That(list2[1]).IsEqualTo(2L);
            }
        }

        // Test 3: Remove with out parameter
        LargeList<long> list3 = new LargeList<long>();
        for (long i = 0; i < Math.Min(capacity, 5L); i++)
        {
            list3.Add(i * 10);
        }

        if (list3.Count > 1) // Need at least 2 elements to test removal of second element
        {
            bool removed3 = list3.Remove(10L, out long removedItem);
            await Assert.That(removed3).IsTrue();
            await Assert.That(removedItem).IsEqualTo(10L);
        }

        // Test 4: Remove with preserveOrder and out parameter
        LargeList<long> list4 = new LargeList<long>();
        for (long i = 0; i < Math.Min(capacity, 5L); i++)
        {
            list4.Add(i * 100);
        }

        if (list4.Count > 1)
        {
            bool removed4 = list4.Remove(100L, preserveOrder: true, out long removedItem2);
            await Assert.That(removed4).IsTrue();
            await Assert.That(removedItem2).IsEqualTo(100L);
        }
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task RemoveAtVariants(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return; // Skip invalid capacities
        }

        // Test 1: RemoveAt with preserveOrder = false
        LargeList<long> list1 = new LargeList<long>();
        for (long i = 0; i < Math.Min(capacity, 10L); i++)
        {
            list1.Add(i);
        }

        if (list1.Count > 0)
        {
            long originalCount = list1.Count;
            long removedItem1 = list1.RemoveAt(0, preserveOrder: false);
            await Assert.That(removedItem1).IsEqualTo(0L);
            await Assert.That(list1.Count).IsEqualTo(originalCount - 1);
        }

        // Test 2: RemoveAt with preserveOrder = true
        LargeList<long> list2 = new LargeList<long>();
        for (long i = 0; i < Math.Min(capacity, 10L); i++)
        {
            list2.Add(i * 10);
        }

        if (list2.Count > 2)
        {
            long originalCount = list2.Count;
            long removedItem2 = list2.RemoveAt(1, preserveOrder: true);
            await Assert.That(removedItem2).IsEqualTo(10L);
            await Assert.That(list2.Count).IsEqualTo(originalCount - 1);

            // Verify order is preserved
            await Assert.That(list2[0]).IsEqualTo(0L);
            if (list2.Count > 1)
            {
                await Assert.That(list2[1]).IsEqualTo(20L);
            }
        }
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesWithOffsetTestCasesArguments))]
    public async Task RefActionDoForEach(long capacity, long offset)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount || offset < 0 || offset >= Math.Max(capacity, 1L))
        {
            return; // Skip invalid parameters
        }

        LargeList<long> largeList = new LargeList<long>();
        for (long i = 0; i < Math.Min(capacity, 10L); i++)
        {
            largeList.Add(i);
        }

        if (largeList.Count == 0)
        {
            return; // Skip empty lists
        }

        // Cast to IRefAccessLargeList to access RefAction methods
        IRefAccessLargeList<long> refAccessList = largeList as IRefAccessLargeList<long>;
        if (refAccessList == null)
        {
            return; // Skip if not implementing IRefAccessLargeList
        }

        // Test 1: DoForEach with RefAction<T> - modify elements
        refAccessList.DoForEach((ref long item) => item *= 2);

        for (long i = 0; i < largeList.Count; i++)
        {
            await Assert.That(largeList[i]).IsEqualTo(i * 2);
        }

        // Test 2: DoForEach with RefAction<T> and range
        if (largeList.Count > 2)
        {
            long rangeOffset = Math.Min(offset, largeList.Count - 2);
            long rangeCount = Math.Min(2L, largeList.Count - rangeOffset);

            refAccessList.DoForEach((ref long item) => item += 100, rangeOffset, rangeCount);

            // Verify only range elements were modified
            for (long i = rangeOffset; i < rangeOffset + rangeCount; i++)
            {
                await Assert.That(largeList[i]).IsEqualTo((i * 2) + 100);
            }
        }

        // Test 3: DoForEach with RefActionWithUserData<T, TUserData>
        long multiplier = 3;
        refAccessList.DoForEach((ref long item, ref long mult) => item *= mult, ref multiplier);

        for (long i = 0; i < largeList.Count; i++)
        {
            long expectedValue = (i * 2) * 3;
            if (largeList.Count > 2 && i >= Math.Min(offset, largeList.Count - 2) && i < Math.Min(offset + 2, largeList.Count))
            {
                expectedValue = ((i * 2) + 100) * 3;
            }
            await Assert.That(largeList[i]).IsEqualTo(expectedValue);
        }
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task AddRangeSpanVariants(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return; // Skip invalid capacities
        }

        LargeList<long> largeList = new LargeList<long>(capacity);

        // Test 1: AddRange with ReadOnlySpan<T>
        ReadOnlySpan<long> spanData = new long[] { 10L, 20L, 30L };
        largeList.AddRange(spanData);

        await Assert.That(largeList.Count).IsEqualTo(3L);
        await Assert.That(largeList[0]).IsEqualTo(10L);
        await Assert.That(largeList[1]).IsEqualTo(20L);
        await Assert.That(largeList[2]).IsEqualTo(30L);

        // Test 2: AddRange with empty span
        ReadOnlySpan<long> emptySpan = ReadOnlySpan<long>.Empty;
        long countBefore = largeList.Count;
        largeList.AddRange(emptySpan);
        await Assert.That(largeList.Count).IsEqualTo(countBefore);

        // Test 3: AddRange with array offset and count
        long[] sourceArray = { 100L, 200L, 300L, 400L, 500L };
        largeList.AddRange(sourceArray, 1, 3); // Add 200, 300, 400

        await Assert.That(largeList.Count).IsEqualTo(6L);
        await Assert.That(largeList[3]).IsEqualTo(200L);
        await Assert.That(largeList[4]).IsEqualTo(300L);
        await Assert.That(largeList[5]).IsEqualTo(400L);
    }
}
