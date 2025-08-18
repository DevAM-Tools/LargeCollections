/*
MIT License
SPDX-License-Identifier: MIT

Copyright (c) 2025 DevAM

Permission is hereby granted, free of charge, to any person obtaining a copy
of this soft        largeList.Clear();

        // Test 6: EnsureRemainingCapacity with negative value
        await Assert.That(() => largeList.EnsureRemainingCapacity(-1L)).Throws<ArgumentOutOfRangeException>();

        // Test 7: AddRange with null IEnumerableiated documentation files (the "Software"), to deal
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
        await Assert.That(() => new LargeList<long>(null)).Throws<ArgumentNullException>();
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

        // Test 6: GetRef operation on empty list throws exception
        await Assert.That(() => largeList.GetRef(0L)).Throws<IndexOutOfRangeException>();

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
        await Assert.That(() => largeList.GetRef(1L)).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeList.GetRef(-1L)).Throws<IndexOutOfRangeException>();
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
}
