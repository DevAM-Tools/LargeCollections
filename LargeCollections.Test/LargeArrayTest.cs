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

using System.Collections;
using TUnit.Core;

namespace LargeCollections.Test;

public class LargeArrayTest
{
    private static long[] _capacities =
    [
        0L,
        5L,
        10L,
        30L,

        /* Running tests with following capacities requires a lot of time and memory */

        //Constants.MaxStandardArrayCapacity / 2L,
        //Constants.MaxStandardArrayCapacity,
        //2L * Constants.MaxStandardArrayCapacity,
        //3L * Constants.MaxStandardArrayCapacity
    ];

    private static long[] _offsets =
    [
        0L,
        1L,
        2L,
    ];

    public static IEnumerable<long> CapacitiesTestCasesArguments()
    {
        foreach (long capacity in _capacities)
        {
            yield return capacity - 2L;
            yield return capacity - 1L;
            yield return capacity;
            yield return capacity + 1L;
            yield return capacity + 2L;
        }
    }

    public static IEnumerable<(long capacity, long offset)> CapacitiesWithOffsetTestCasesArguments()
    {
        foreach (long capacity in _capacities)
        {
            foreach (long offset in _offsets)
            {
                yield return (capacity - 2L, offset);
                yield return (capacity - 1L, offset);
                yield return (capacity, offset);
                yield return (capacity + 1L, offset);
                yield return (capacity + 2L, offset);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesTestCasesArguments))]
    public async Task Create(long capacity)
    {
        LargeArray<long> largeArray;
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            await Assert.That(() => largeArray = new LargeArray<long>(capacity)).Throws<ArgumentOutOfRangeException>();
            return;
        }

        largeArray = new LargeArray<long>(capacity);
        await Assert.That(largeArray.Count).IsEqualTo(capacity);
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesWithOffsetTestCasesArguments))]
    public async Task SetGet(long capacity, long offset)
    {
        // input check
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> largeArray = new(capacity);

        await SetGetTest(largeArray, offset);
    }

    public static async Task SetGetTest(ILargeArray<long> largeArray, long offset)
    {
        long capacity = largeArray.Count;
        long count = capacity - 2L * offset;

        if (count < 0L || offset + count > capacity)
        {
            return;
        }

        // create and verify array with ascending order using indexer
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
            await Assert.That(largeArray[i]).IsEqualTo(i);
        }

        // Test indexer bounds checking
        long dummy = 0L;
        await Assert.That(() => dummy = largeArray[-1]).Throws<IndexOutOfRangeException>();
        await Assert.That(() => dummy = largeArray[capacity]).Throws<IndexOutOfRangeException>();
        await Assert.That(() => dummy = largeArray[capacity + 1L]).Throws<IndexOutOfRangeException>();

        await Assert.That(() => largeArray[-1] = 0L).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeArray[capacity] = 0L).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeArray[capacity + 1L] = 0L).Throws<IndexOutOfRangeException>();

        // Test Get method
        for (long i = 0; i < capacity; i++)
        {
            long value = largeArray.Get(i);
            await Assert.That(value).IsEqualTo(i);
        }

        // Test Get method bounds checking
        await Assert.That(() => largeArray.Get(-1)).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeArray.Get(capacity)).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeArray.Get(capacity + 1L)).Throws<IndexOutOfRangeException>();

        // Test Set method
        for (long i = 0; i < capacity; i++)
        {
            long newValue = -i;
            largeArray.Set(i, newValue);
            await Assert.That(largeArray[i]).IsEqualTo(newValue);
            await Assert.That(largeArray.Get(i)).IsEqualTo(newValue);
        }

        // Test Set method bounds checking
        await Assert.That(() => largeArray.Set(-1, 0L)).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeArray.Set(capacity, 0L)).Throws<IndexOutOfRangeException>();
        await Assert.That(() => largeArray.Set(capacity + 1L, 0L)).Throws<IndexOutOfRangeException>();

        // Test IRefAccess functionality if the array implements it
        await TestAllRefAccess(largeArray, capacity, offset);

        // Reset array to original ascending order for consistency
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
        }
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesWithOffsetTestCasesArguments))]
    public async Task Enumeration(long capacity, long offset)
    {
        // input check
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> largeArray = new(capacity);

        await EnumerationTest(largeArray, offset);
    }

    public static async Task EnumerationTest(ILargeArray<long> largeArray, long offset)
    {
        long capacity = largeArray.Count;
        long count = capacity - 2L * offset;

        // Test argument validation for ranged GetAll
        await Assert.That(() => largeArray.GetAll(-1L, count).FirstOrDefault()).Throws<ArgumentException>();
        await Assert.That(() => largeArray.GetAll(0L, -1L).FirstOrDefault()).Throws<ArgumentException>();
        await Assert.That(() => largeArray.GetAll(1L, capacity).FirstOrDefault()).Throws<ArgumentException>();

        if (count < 0L || offset + count > capacity)
        {
            return;
        }

        // Test 1: Empty array enumeration
        if (capacity == 0L)
        {
            await Assert.That(largeArray).IsEmpty();
            await Assert.That(largeArray.GetAll(0L, 0L)).IsEmpty();
            return;
        }

        // Test 2: Empty range enumeration
        if (count == 0L)
        {
            await Assert.That(largeArray.GetAll(offset, count)).IsEmpty();
            return;
        }

        // Initialize array with ascending order
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
            await Assert.That(largeArray[i]).IsEqualTo(i);
        }

        // Test 3: Full array enumeration via GetAll()
        await Assert.That(largeArray).IsEquivalentTo(LargeEnumerable.Range(capacity));

        // Test 4: Ranged GetAll enumeration
        await Assert.That(largeArray.GetAll(offset, count)).IsEquivalentTo(LargeEnumerable.Range(offset, count));

        // Test 5: Single element range
        if (count == 1L)
        {
            await Assert.That(largeArray.GetAll(offset, 1L)).IsEquivalentTo(new[] { offset });
        }

        // Test 6: Multiple enumerations (should be repeatable)
        long[] firstEnumeration = largeArray.GetAll(offset, count).ToArray();
        long[] secondEnumeration = largeArray.GetAll(offset, count).ToArray();
        await Assert.That(firstEnumeration).IsEquivalentTo(secondEnumeration);

        // Test 7: Enumeration during modification (if supported)
        IEnumerable<long> enumerable = largeArray.GetAll(offset, count);
        IEnumerator<long> enumerator = enumerable.GetEnumerator();

        // Start enumeration
        bool hasFirst = enumerator.MoveNext();
        if (hasFirst)
        {
            long firstValue = enumerator.Current;
            await Assert.That(firstValue).IsEqualTo(offset);

            // Modify array during enumeration
            if (offset < capacity)
            {
                largeArray[offset] = -999L;
            }

            // Continue enumeration - behavior may vary depending on implementation
            // Some implementations might reflect changes, others might not
            while (enumerator.MoveNext())
            {
                // Just consume the enumeration to test it doesn't crash
            }
        }
        enumerator.Dispose();

        // Reset array for remaining tests
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
        }

        // Test 8: IEnumerable interface (non-generic)
        if (largeArray is IEnumerable nonGenericEnumerable)
        {
            long expectedValue = 0L;
            foreach (object item in nonGenericEnumerable)
            {
                await Assert.That(item).IsEqualTo(expectedValue);
                expectedValue++;
            }
        }

        // Test 9: LINQ operations on enumeration
        if (capacity > 0)
        {
            long firstElement = largeArray.FirstOrDefault();
            await Assert.That(firstElement).IsEqualTo(0L);

            long lastElement = largeArray.LastOrDefault();
            await Assert.That(lastElement).IsEqualTo(capacity - 1L);

            if (count > 0)
            {
                long rangedFirst = largeArray.GetAll(offset, count).FirstOrDefault();
                await Assert.That(rangedFirst).IsEqualTo(offset);

                long rangedLast = largeArray.GetAll(offset, count).LastOrDefault();
                await Assert.That(rangedLast).IsEqualTo(offset + count - 1L);
            }
        }

        // Test 10: Count property consistency
        await Assert.That(largeArray.Count).IsEqualTo(capacity);
        await Assert.That((long)largeArray.Count()).IsEqualTo(capacity); // LINQ Count()
        if (count > 0)
        {
            await Assert.That((long)largeArray.GetAll(offset, count).Count()).IsEqualTo(count);
        }

        // Test 11: ToArray() if available
        if (capacity <= int.MaxValue)
        {
            long[] array = largeArray.ToArray();
            await Assert.That(array.Length).IsEqualTo((int)capacity);
            await Assert.That(array).IsEquivalentTo(LargeEnumerable.Range(capacity));
        }

        // Test 12: Boundary cases for ranged enumeration
        if (capacity > 0)
        {
            // First element only
            await Assert.That(largeArray.GetAll(0L, 1L)).IsEquivalentTo(new[] { 0L });

            // Last element only
            await Assert.That(largeArray.GetAll(capacity - 1L, 1L)).IsEquivalentTo(new[] { capacity - 1L });

            // Full range via GetAll
            await Assert.That(largeArray.GetAll(0L, capacity)).IsEquivalentTo(LargeEnumerable.Range(capacity));
        }

        // Test 13: Enumeration with extreme values
        if (capacity >= 3L)
        {
            largeArray[0] = long.MinValue;
            largeArray[1] = 0L;
            largeArray[capacity - 1] = long.MaxValue;

            long[] enumerated = largeArray.ToArray();
            await Assert.That(enumerated[0]).IsEqualTo(long.MinValue);
            await Assert.That(enumerated[1]).IsEqualTo(0L);
            await Assert.That(enumerated[capacity - 1]).IsEqualTo(long.MaxValue);

            // Reset array
            for (long i = 0; i < capacity; i++)
            {
                largeArray[i] = i;
            }
        }

        // Test 14: DoForEach enumeration (moved from original test)
        long currentExpectedI = 0L;
        largeArray.DoForEach(async i =>
        {
            await Assert.That(i).IsEqualTo(currentExpectedI);
            currentExpectedI++;
        });

        // Test 15: Ranged DoForEach enumeration
        if (count > 0)
        {
            currentExpectedI = offset;
            largeArray.DoForEach(async i =>
            {
                await Assert.That(i).IsEqualTo(currentExpectedI);
                currentExpectedI++;
            }, offset, count);
        }

        // Test 16: Performance consideration - large enumeration shouldn't cause stack overflow
        // This is mainly a structural test
        if (capacity > 1000L)
        {
            long elementCount = 0L;
            foreach (long element in largeArray)
            {
                elementCount++;
                if (elementCount > 1000L) break; // Don't run too long in tests
            }
            await Assert.That(elementCount).IsGreaterThan(1000L);
        }

        // Test 17: Nested enumeration
        if (capacity > 0 && count > 0)
        {
            long outerCount = 0L;
            foreach (long outerElement in largeArray.GetAll(offset, Math.Min(count, 3L)))
            {
                long innerCount = 0L;
                foreach (long innerElement in largeArray.GetAll(offset, Math.Min(count, 2L)))
                {
                    await Assert.That(innerElement).IsGreaterThanOrEqualTo(offset);
                    innerCount++;
                }
                await Assert.That(innerCount).IsEqualTo(Math.Min(count, 2L));
                outerCount++;
            }
            await Assert.That(outerCount).IsEqualTo(Math.Min(count, 3L));
        }

        // Test 18: Dispose behavior of enumerators
        using (IEnumerator<long> fullEnumerator = largeArray.GetEnumerator())
        {
            // Test that we can dispose without issues
        }

        using (IEnumerator<long> rangedEnumerator = largeArray.GetAll(offset, count).GetEnumerator())
        {
            // Test that we can dispose ranged enumerator without issues
        }
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesWithOffsetTestCasesArguments))]
    public async Task Swap(long capacity, long offset)
    {
        // input check
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> largeArray = new(capacity);

        await SwapTest(largeArray, offset);
    }

    public static async Task SwapTest(ILargeArray<long> largeArray, long offset)
    {
        long capacity = largeArray.Count;

        // Test argument validation
        if (capacity > 0)
        {
            await Assert.That(() => largeArray.Swap(-1L, 0L)).Throws<IndexOutOfRangeException>();
            await Assert.That(() => largeArray.Swap(0L, -1L)).Throws<IndexOutOfRangeException>();
            await Assert.That(() => largeArray.Swap(capacity, 0L)).Throws<IndexOutOfRangeException>();
            await Assert.That(() => largeArray.Swap(0L, capacity)).Throws<IndexOutOfRangeException>();
            await Assert.That(() => largeArray.Swap(capacity + 1L, 0L)).Throws<IndexOutOfRangeException>();
            await Assert.That(() => largeArray.Swap(0L, capacity + 1L)).Throws<IndexOutOfRangeException>();
        }

        if (capacity < 2)
        {
            return; // Need at least 2 elements to test swap
        }

        // Initialize array with ascending order
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
        }

        // Test 1: Swap different elements
        long leftIndex = 0L;
        long rightIndex = capacity - 1L;
        long leftValue = largeArray[leftIndex];
        long rightValue = largeArray[rightIndex];

        largeArray.Swap(leftIndex, rightIndex);
        await Assert.That(largeArray[leftIndex]).IsEqualTo(rightValue);
        await Assert.That(largeArray[rightIndex]).IsEqualTo(leftValue);

        // Test 2: Swap same element (should be no-op)
        long originalValue = largeArray[leftIndex];
        largeArray.Swap(leftIndex, leftIndex);
        await Assert.That(largeArray[leftIndex]).IsEqualTo(originalValue);

        // Test 3: Swap adjacent elements
        if (capacity >= 3)
        {
            long index1 = 1L;
            long index2 = 2L;
            long value1 = largeArray[index1];
            long value2 = largeArray[index2];

            largeArray.Swap(index1, index2);
            await Assert.That(largeArray[index1]).IsEqualTo(value2);
            await Assert.That(largeArray[index2]).IsEqualTo(value1);
        }

        // Test 4: Multiple swaps
        if (capacity >= 4)
        {
            // Reset array
            for (long i = 0; i < capacity; i++)
            {
                largeArray[i] = i;
            }

            // Perform multiple swaps to reverse array
            for (long i = 0; i < capacity / 2; i++)
            {
                largeArray.Swap(i, capacity - 1 - i);
            }

            // Verify array is reversed
            for (long i = 0; i < capacity; i++)
            {
                await Assert.That(largeArray[i]).IsEqualTo(capacity - 1 - i);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesWithOffsetTestCasesArguments))]
    public async Task DoForEach(long capacity, long offset)
    {
        // input check
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> largeArray = new(capacity);

        await DoForEachTest(largeArray, offset);
    }

    public static async Task DoForEachTest(ILargeArray<long> largeArray, long offset)
    {
        long capacity = largeArray.Count;
        long count = capacity - 2L * offset;

        if (count < 0L || offset + count > capacity)
        {
            return;
        }

        // Initialize array with ascending values
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
        }

        // Test 1: DoForEach with Action<T> (read-only)
        long expectedValue = 0L;
        largeArray.DoForEach(async value =>
        {
            await Assert.That(value).IsEqualTo(expectedValue);
            expectedValue++;
        });

        // Test 2: DoForEach with ActionWithUserData<T, TUserData>
        long sum = 0L;
        largeArray.DoForEach(static (long value, ref long userData) =>
        {
            userData += value;
        }, ref sum);

        long expectedSum = largeArray.Sum();
        await Assert.That(sum).IsEqualTo(expectedSum);

        // Test 3: Ranged DoForEach with Action<T>
        expectedValue = offset;
        largeArray.DoForEach(async value =>
        {
            await Assert.That(value).IsEqualTo(expectedValue);
            expectedValue++;
        }, offset, count);

        // Test 4: Ranged DoForEach with ActionWithUserData<T, TUserData>
        sum = 0L;
        largeArray.DoForEach(static (long value, ref long userData) =>
        {
            userData += value;
        }, offset, count, ref sum);

        expectedSum = LargeEnumerable.Range(offset, count).Sum();
        await Assert.That(sum).IsEqualTo(expectedSum);

        // Test IRefAccess functionality if the array implements it
        await TestAllRefAccess(largeArray, capacity, offset);
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesWithOffsetTestCasesArguments))]
    public async Task RefAccess(long capacity, long offset)
    {
        // input check
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> largeArray = new(capacity);

        // Test IRefAccess functionality
        await TestAllRefAccess(largeArray, capacity, offset);
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesTestCasesArguments))]
    public async Task Resize(long capacity)
    {
        // input check
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> largeArray = new(capacity);

        // Test 1: Resize empty array
        if (capacity == 0)
        {
            largeArray.Resize(5);
            await Assert.That(largeArray.Count).IsEqualTo(5);
            // Only check first/last elements for performance
            await Assert.That(largeArray[0]).IsEqualTo(default(long));
            await Assert.That(largeArray[4]).IsEqualTo(default(long));

            // Reset for other tests
            largeArray.Resize(0);
            await Assert.That(largeArray.Count).IsEqualTo(0);
        }

        // Only initialize a few key elements instead of the entire array
        long elementsToTest = Math.Min(capacity, 10);
        for (long i = 0; i < elementsToTest; i++)
        {
            largeArray[i] = i + 1000; // Use distinctive values
        }

        // Test 2: Resize to same capacity (no-op)
        largeArray.Resize(capacity);
        await Assert.That(largeArray.Count).IsEqualTo(capacity);
        // Verify only the test elements are preserved
        for (long i = 0; i < elementsToTest; i++)
        {
            await Assert.That(largeArray[i]).IsEqualTo(i + 1000);
        }

        // Test 3: Resize to larger capacity
        long newLargerCapacity = Math.Min(capacity + 10, Constants.MaxLargeCollectionCount);
        if (newLargerCapacity > capacity && newLargerCapacity <= Constants.MaxLargeCollectionCount)
        {
            largeArray.Resize(newLargerCapacity);
            await Assert.That(largeArray.Count).IsEqualTo(newLargerCapacity);

            // Verify original elements are preserved
            for (long i = 0; i < elementsToTest; i++)
            {
                await Assert.That(largeArray[i]).IsEqualTo(i + 1000);
            }

            // Verify new elements are default
            for (long i = capacity; i < Math.Min(newLargerCapacity, capacity + 5); i++)
            {
                await Assert.That(largeArray[i]).IsEqualTo(default(long));
            }
        }
        else if (capacity * 2 > Constants.MaxLargeCollectionCount)
        {
            // Test exception for too large capacity
            await Assert.That(() => largeArray.Resize(Constants.MaxLargeCollectionCount + 1L)).Throws<ArgumentOutOfRangeException>();
        }

        // Test 4: Resize to smaller capacity
        if (capacity > 5)
        {
            long newSmallerCapacity = Math.Max(0, capacity - 5);
            largeArray.Resize(newSmallerCapacity);
            await Assert.That(largeArray.Count).IsEqualTo(newSmallerCapacity);

            // Verify preserved elements
            long preservedElements = Math.Min(newSmallerCapacity, elementsToTest);
            for (long i = 0; i < preservedElements; i++)
            {
                await Assert.That(largeArray[i]).IsEqualTo(i + 1000);
            }
        }

        // Test 5: Resize to zero
        largeArray.Resize(0);
        await Assert.That(largeArray.Count).IsEqualTo(0);

        // Test 6: Multiple consecutive resizes
        largeArray.Resize(3);
        largeArray[0] = 10;
        largeArray[1] = 20;
        largeArray[2] = 30;

        largeArray.Resize(1);
        await Assert.That(largeArray.Count).IsEqualTo(1);
        await Assert.That(largeArray[0]).IsEqualTo(10);

        largeArray.Resize(3);
        await Assert.That(largeArray.Count).IsEqualTo(3);
        await Assert.That(largeArray[0]).IsEqualTo(10);
        await Assert.That(largeArray[1]).IsEqualTo(default(long));
        await Assert.That(largeArray[2]).IsEqualTo(default(long));

        // Test 7: Invalid arguments
        await Assert.That(() => largeArray.Resize(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => largeArray.Resize(Constants.MaxLargeCollectionCount + 1L)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesWithOffsetTestCasesArguments))]
    public async Task Sort(long capacity, long offset)
    {
        // input check
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> largeArray = new(capacity);

        await SortTest(largeArray, offset);
    }

    public static async Task SortTest(ILargeArray<long> largeArray, long offset)
    {
        long capacity = largeArray.Count;
        long count = capacity - 2L * offset;
        Func<long, long, int> comparer = Comparer<long>.Default.Compare;

        // Test argument validation
        await Assert.That(() => largeArray.Sort(comparer, -1L, count)).Throws<ArgumentException>();
        await Assert.That(() => largeArray.Sort(comparer, 0L, -1L)).Throws<ArgumentException>();
        await Assert.That(() => largeArray.Sort(comparer, 1L, capacity)).Throws<ArgumentException>();

        if (count < 0L || offset + count > capacity)
        {
            return;
        }

        // Test 1: Sort empty range
        if (count == 0L)
        {
            largeArray.Sort(comparer, offset, count);
            return; // Nothing to verify for empty range
        }

        // Test 2: Sort single element
        if (count == 1L)
        {
            largeArray[offset] = 42L;
            largeArray.Sort(comparer, offset, count);
            await Assert.That(largeArray[offset]).IsEqualTo(42L);
            return;
        }

        // Test 3: Sort already sorted array (ascending)
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
        }
        largeArray.Sort(comparer, offset, count);
        for (long i = offset; i < offset + count; i++)
        {
            await Assert.That(largeArray[i]).IsEqualTo(i);
        }

        // Test 4: Sort reverse sorted array (descending)
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = capacity - 1L - i;
        }
        largeArray.Sort(comparer, offset, count);

        // Verify elements outside range are unchanged
        for (long i = 0; i < offset; i++)
        {
            await Assert.That(largeArray[i]).IsEqualTo(capacity - 1L - i);
        }
        for (long i = offset + count; i < capacity; i++)
        {
            await Assert.That(largeArray[i]).IsEqualTo(capacity - 1L - i);
        }
        // Verify sorted range is ascending
        for (long i = offset; i < offset + count; i++)
        {
            long expectedValue = capacity - 1L - (offset + count - 1L - (i - offset));
            await Assert.That(largeArray[i]).IsEqualTo(expectedValue);
        }

        // Test 5: Sort array with duplicates
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i % 3; // Creates pattern: 0,1,2,0,1,2,...
        }
        largeArray.Sort(comparer, offset, count);

        // Verify sorted range has duplicates in correct order
        long previousValue = long.MinValue;
        for (long i = offset; i < offset + count; i++)
        {
            await Assert.That(largeArray[i]).IsGreaterThanOrEqualTo(previousValue);
            previousValue = largeArray[i];
        }

        // Test 6: Sort with all identical elements
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = 100L;
        }
        largeArray.Sort(comparer, offset, count);
        for (long i = offset; i < offset + count; i++)
        {
            await Assert.That(largeArray[i]).IsEqualTo(100L);
        }

        // Test 7: Sort entire array
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = capacity - 1L - i;
        }
        largeArray.Sort(comparer);
        await Assert.That(largeArray).IsEquivalentTo(LargeEnumerable.Range(capacity));

        // Test 8: Sort with custom comparer (reverse order)
        static int reverseComparer(long x, long y) => y.CompareTo(x);
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
        }
        largeArray.Sort(reverseComparer, offset, count);

        // Verify reverse sorted range
        for (long i = offset; i < offset + count; i++)
        {
            long expectedValue = offset + count - 1L - (i - offset);
            await Assert.That(largeArray[i]).IsEqualTo(expectedValue);
        }
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesWithOffsetTestCasesArguments))]
    public async Task BinarySearch(long capacity, long offset)
    {
        // input check
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> largeArray = new(capacity);

        await BinarySearchTest(largeArray, offset);
    }

    public static async Task BinarySearchTest(ILargeArray<long> largeArray, long offset)
    {
        long capacity = largeArray.Count;
        long count = capacity - 2L * offset;
        Func<long, long, int> comparer = Comparer<long>.Default.Compare;

        // Test argument validation
        await Assert.That(() => largeArray.BinarySearch(0L, comparer, -1L, count)).Throws<ArgumentException>();
        await Assert.That(() => largeArray.BinarySearch(0L, comparer, 0L, -1L)).Throws<ArgumentException>();
        await Assert.That(() => largeArray.BinarySearch(0L, comparer, 1L, capacity)).Throws<ArgumentException>();

        if (count < 0L || offset + count > capacity)
        {
            return;
        }

        // Test 1: Empty range
        if (count == 0L)
        {
            long index = largeArray.BinarySearch(0L, comparer, offset, count);
            await Assert.That(index).IsEqualTo(-1L);
            return;
        }

        // Test 2: Single element range
        if (count == 1L)
        {
            largeArray[offset] = 42L;

            // Element found
            long index = largeArray.BinarySearch(42L, comparer, offset, count);
            await Assert.That(index).IsEqualTo(offset);

            // Element not found (smaller)
            index = largeArray.BinarySearch(41L, comparer, offset, count);
            await Assert.That(index).IsEqualTo(-1L); // LargeArray returns -1 for not found

            // Element not found (larger)
            index = largeArray.BinarySearch(43L, comparer, offset, count);
            await Assert.That(index).IsEqualTo(-1L); // LargeArray returns -1 for not found

            return;
        }

        // Create sorted array with ascending order
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i; // Use consecutive numbers for simpler testing
        }

        // Test 3: Search for existing elements in range
        for (long i = offset; i < offset + count; i++)
        {
            long searchValue = i;
            long index = largeArray.BinarySearch(searchValue, comparer, offset, count);
            await Assert.That(index).IsEqualTo(i);
        }

        // Test 4: Search for non-existing elements (smaller than range)
        if (offset > 0)
        {
            long searchValueSmaller = offset - 1;
            long index = largeArray.BinarySearch(searchValueSmaller, comparer, offset, count);
            await Assert.That(index).IsEqualTo(-1L); // LargeArray returns -1 for not found
        }

        // Test 5: Search for non-existing elements (larger than range)
        long searchValueLarger = offset + count;
        long indexLarger = largeArray.BinarySearch(searchValueLarger, comparer, offset, count);
        await Assert.That(indexLarger).IsEqualTo(-1L); // LargeArray returns -1 for not found

        // Test 6: Search for elements not in the specified range but in array
        if (offset > 0 && capacity > offset + count)
        {
            // Element before range
            long beforeRangeValue = 0;
            long index = largeArray.BinarySearch(beforeRangeValue, comparer, offset, count);
            await Assert.That(index).IsEqualTo(-1L);

            // Element after range  
            long afterRangeValue = capacity - 1;
            index = largeArray.BinarySearch(afterRangeValue, comparer, offset, count);
            await Assert.That(index).IsEqualTo(-1L);
        }

        // Test 7: Search entire array without range parameters
        for (long i = 0; i < Math.Min(capacity, 10); i++) // Limit to first 10 for performance
        {
            long searchValue = i;
            long index = largeArray.BinarySearch(searchValue, comparer);
            await Assert.That(index).IsEqualTo(i);
        }

        // Test 8: Search for value not in entire array
        long notFoundValue = capacity + 100;
        long notFoundIndex = largeArray.BinarySearch(notFoundValue, comparer);
        await Assert.That(notFoundIndex).IsEqualTo(-1L);

        // Test 9: Array with duplicates
        if (count >= 3)
        {
            // Create array with some duplicates
            for (long i = 0; i < capacity; i++)
            {
                largeArray[i] = i / 3; // Creates groups of identical values
            }

            // Search for a duplicate value that should exist in the range
            long duplicateValue = offset / 3;
            long index = largeArray.BinarySearch(duplicateValue, comparer, offset, count);

            // Should find the value (any valid index is acceptable for duplicates)
            if (largeArray[offset] <= duplicateValue && duplicateValue <= largeArray[offset + count - 1])
            {
                await Assert.That(index).IsGreaterThanOrEqualTo(0L);
                await Assert.That(largeArray[index]).IsEqualTo(duplicateValue);
            }
            else
            {
                await Assert.That(index).IsEqualTo(-1L);
            }
        }

        // Test 10: Edge case - search at boundaries
        if (count > 0)
        {
            // Reset to ascending order
            for (long i = 0; i < capacity; i++)
            {
                largeArray[i] = i;
            }

            // Search for first element in range
            long firstIndex = largeArray.BinarySearch(offset, comparer, offset, count);
            await Assert.That(firstIndex).IsEqualTo(offset);

            // Search for last element in range
            long lastIndex = largeArray.BinarySearch(offset + count - 1, comparer, offset, count);
            await Assert.That(lastIndex).IsEqualTo(offset + count - 1);
        }

        // Test 11: Null comparer (should use default comparer)
        if (count > 0)
        {
            long index = largeArray.BinarySearch(offset, null, offset, count);
            await Assert.That(index).IsEqualTo(offset);
        }
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesWithOffsetTestCasesArguments))]
    public async Task Contains(long capacity, long offset)
    {
        // input check
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> largeArray = new(capacity);

        await ContainsTest(largeArray, offset);
    }

    public static async Task ContainsTest(ILargeArray<long> largeArray, long offset)
    {
        long capacity = largeArray.Count;
        long count = capacity - 2L * offset;

        // Test argument validation for ranged Contains
        await Assert.That(() => largeArray.Contains(0L, -1L, count)).Throws<ArgumentException>();
        await Assert.That(() => largeArray.Contains(0L, 0L, -1L)).Throws<ArgumentException>();
        await Assert.That(() => largeArray.Contains(0L, 1L, capacity)).Throws<ArgumentException>();

        if (count < 0L || offset + count > capacity)
        {
            return;
        }

        // Test 1: Empty array
        if (capacity == 0L)
        {
            await Assert.That(largeArray.Contains(0L)).IsFalse();
            await Assert.That(largeArray.Contains(long.MinValue)).IsFalse();
            await Assert.That(largeArray.Contains(long.MaxValue)).IsFalse();
            return;
        }

        // Test 2: Empty range
        if (count == 0L)
        {
            await Assert.That(largeArray.Contains(0L, offset, count)).IsFalse();
            await Assert.That(largeArray.Contains(long.MinValue, offset, count)).IsFalse();
            await Assert.That(largeArray.Contains(long.MaxValue, offset, count)).IsFalse();
            return;
        }

        // Initialize array with ascending order
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
            await Assert.That(largeArray[i]).IsEqualTo(i);
        }

        // Test 3: Contains in entire array
        for (long i = 0; i < capacity; i++)
        {
            await Assert.That(largeArray.Contains(i)).IsTrue();
        }

        // Test 4: Does not contain values outside range
        await Assert.That(largeArray.Contains(-1L)).IsFalse();
        await Assert.That(largeArray.Contains(capacity)).IsFalse();
        await Assert.That(largeArray.Contains(capacity + 1L)).IsFalse();
        await Assert.That(largeArray.Contains(long.MinValue)).IsFalse();
        await Assert.That(largeArray.Contains(long.MaxValue)).IsFalse();

        // Test 5: Contains in specified range
        for (long i = 0; i < offset; i++)
        {
            bool result = largeArray.Contains(i, offset, count);
            await Assert.That(result).IsFalse();
        }
        for (long i = offset; i < offset + count; i++)
        {
            bool result = largeArray.Contains(i, offset, count);
            await Assert.That(result).IsTrue();
        }
        for (long i = offset + count; i < capacity; i++)
        {
            bool result = largeArray.Contains(i, offset, count);
            await Assert.That(result).IsFalse();
        }

        // Test 6: Contains values outside array in range
        await Assert.That(largeArray.Contains(-1L, offset, count)).IsFalse();
        await Assert.That(largeArray.Contains(capacity, offset, count)).IsFalse();
        await Assert.That(largeArray.Contains(capacity + 1L, offset, count)).IsFalse();

        // Test 7: Array with duplicates
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i % 3; // Creates pattern: 0,1,2,0,1,2,...
        }

        // Should find duplicates in range
        if (count > 0L)
        {
            long firstValueInRange = (offset % 3);
            await Assert.That(largeArray.Contains(firstValueInRange, offset, count)).IsTrue();

            // Test values that might or might not be in the range
            for (long testValue = 0L; testValue <= 2L; testValue++)
            {
                bool expectedInRange = false;
                for (long i = offset; i < offset + count; i++)
                {
                    if (largeArray[i] == testValue)
                    {
                        expectedInRange = true;
                        break;
                    }
                }
                bool actualInRange = largeArray.Contains(testValue, offset, count);
                await Assert.That(actualInRange).IsEqualTo(expectedInRange);
            }
        }

        // Test 8: Array with all identical elements
        long identicalValue = 42L;
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = identicalValue;
        }

        await Assert.That(largeArray.Contains(identicalValue)).IsTrue();
        await Assert.That(largeArray.Contains(identicalValue + 1L)).IsFalse();
        await Assert.That(largeArray.Contains(identicalValue - 1L)).IsFalse();

        if (count > 0L)
        {
            await Assert.That(largeArray.Contains(identicalValue, offset, count)).IsTrue();
            await Assert.That(largeArray.Contains(identicalValue + 1L, offset, count)).IsFalse();
            await Assert.That(largeArray.Contains(identicalValue - 1L, offset, count)).IsFalse();
        }

        // Test 9: Array with extreme values
        if (capacity >= 3L)
        {
            largeArray[0] = long.MinValue;
            largeArray[1] = 0L;
            largeArray[capacity - 1] = long.MaxValue;

            await Assert.That(largeArray.Contains(long.MinValue)).IsTrue();
            await Assert.That(largeArray.Contains(0L)).IsTrue();
            await Assert.That(largeArray.Contains(long.MaxValue)).IsTrue();

            // Test range contains with extreme values
            if (offset == 0L && count >= 2L)
            {
                await Assert.That(largeArray.Contains(long.MinValue, offset, count)).IsTrue();
                await Assert.That(largeArray.Contains(0L, offset, count)).IsTrue();
            }
            if (offset + count == capacity && count >= 1L)
            {
                await Assert.That(largeArray.Contains(long.MaxValue, offset, count)).IsTrue();
            }
        }

        // Test 10: Single element array
        if (capacity == 1L)
        {
            largeArray[0] = 100L;
            await Assert.That(largeArray.Contains(100L)).IsTrue();
            await Assert.That(largeArray.Contains(99L)).IsFalse();
            await Assert.That(largeArray.Contains(101L)).IsFalse();

            if (count == 1L)
            {
                await Assert.That(largeArray.Contains(100L, offset, count)).IsTrue();
                await Assert.That(largeArray.Contains(99L, offset, count)).IsFalse();
                await Assert.That(largeArray.Contains(101L, offset, count)).IsFalse();
            }
        }

        // Test 11: Contains with null/default values (for reference types, this tests default(T))
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = default(long);
        }

        await Assert.That(largeArray.Contains(default)).IsTrue();
        await Assert.That(largeArray.Contains(1L)).IsFalse();

        if (count > 0L)
        {
            await Assert.That(largeArray.Contains(default, offset, count)).IsTrue();
            await Assert.That(largeArray.Contains(1L, offset, count)).IsFalse();
        }

        // Test 12: Edge cases with boundary indices
        if (capacity > 2L)
        {
            // Reset to ascending order
            for (long i = 0; i < capacity; i++)
            {
                largeArray[i] = i;
            }

            // Test at range boundaries
            if (count > 0L)
            {
                // First element in range
                await Assert.That(largeArray.Contains(offset, offset, count)).IsTrue();

                // Last element in range
                if (count > 1L)
                {
                    await Assert.That(largeArray.Contains(offset + count - 1L, offset, count)).IsTrue();
                }

                // Just before range
                if (offset > 0L)
                {
                    await Assert.That(largeArray.Contains(offset - 1L, offset, count)).IsFalse();
                }

                // Just after range
                if (offset + count < capacity)
                {
                    await Assert.That(largeArray.Contains(offset + count, offset, count)).IsFalse();
                }
            }
        }

        // Test 13: Contains with single element range
        if (capacity > 0L && count == 1L)
        {
            largeArray[offset] = 999L;
            await Assert.That(largeArray.Contains(999L, offset, count)).IsTrue();
            await Assert.That(largeArray.Contains(998L, offset, count)).IsFalse();
            await Assert.That(largeArray.Contains(1000L, offset, count)).IsFalse();
        }
    }


    [Test]
    [MethodDataSource(nameof(CapacitiesWithOffsetTestCasesArguments))]
    public async Task Copy(long capacity, long offset)
    {
        // input check
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> largeArray = new(capacity);

        // Test with LargeArray as target
        await CopyTest(largeArray, offset, capacity => new LargeArray<long>(capacity));

        // Test with LargeList as target
        await CopyTest(largeArray, offset, capacity =>
        {
            LargeList<long> list = new();
            list.AddRange(LargeEnumerable.Repeat(0L, capacity));
            return list;
        });
    }

    public static async Task CopyTest(ILargeArray<long> largeArray, long offset, Func<long, ILargeArray<long>> getTarget)
    {
        long capacity = largeArray.Count;
        long count = capacity - 2L * offset;

        // Test argument validation for copy operations
        if (capacity > 0)
        {
            ILargeArray<long> target = getTarget(capacity);
            await Assert.That(() => largeArray.CopyTo(target, -1L, 0L, count)).Throws<ArgumentException>();
            await Assert.That(() => largeArray.CopyTo(target, 0L, -1L, count)).Throws<ArgumentException>();
            await Assert.That(() => largeArray.CopyTo(target, 0L, 0L, -1L)).Throws<ArgumentException>();
            await Assert.That(() => largeArray.CopyTo(target, 1L, 0L, capacity)).Throws<ArgumentException>();
            await Assert.That(() => largeArray.CopyTo(target, 0L, 1L, capacity)).Throws<ArgumentException>();
        }

        if (count < 0L || offset + count > capacity)
        {
            return;
        }

        // Initialize array with ascending order
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
            await Assert.That(largeArray[i]).IsEqualTo(i);
        }

        // Test 1: CopyTo ILargeArray variants
        if (capacity > 0)
        {
            // Full array copy
            ILargeArray<long> targetLargeArray = getTarget(capacity);
            largeArray.CopyTo(targetLargeArray, 0L, 0L, capacity);
            await Assert.That(targetLargeArray).IsEquivalentTo(largeArray);

            // Partial copy from source
            if (count > 0)
            {
                targetLargeArray = getTarget(capacity);
                largeArray.CopyTo(targetLargeArray, offset, 0L, count);
                await Assert.That(targetLargeArray.GetAll(0L, count)).IsEquivalentTo(largeArray.GetAll(offset, count));
            }

            // Partial copy to target
            if (count > 0)
            {
                targetLargeArray = getTarget(capacity);
                largeArray.CopyTo(targetLargeArray, 0L, offset, count);
                await Assert.That(targetLargeArray.GetAll(offset, count)).IsEquivalentTo(largeArray.GetAll(0L, count));
            }

            // Copy to different sized target
            if (capacity > 1)
            {
                ILargeArray<long> smallerTarget = getTarget(capacity / 2);
                largeArray.CopyTo(smallerTarget, 0L, 0L, capacity / 2);
                await Assert.That(smallerTarget).IsEquivalentTo(largeArray.GetAll(0L, capacity / 2));

                ILargeArray<long> largerTarget = getTarget(capacity * 2);
                largeArray.CopyTo(largerTarget, 0L, 0L, capacity);
                await Assert.That(largerTarget.GetAll(0L, capacity)).IsEquivalentTo(largeArray);
            }
        }

        // Test 2: CopyTo LargeList variants
        if (capacity > 0)
        {
            // Full array copy to LargeList
            LargeList<long> targetLargeList = new(capacity);
            targetLargeList.AddRange(LargeEnumerable.Repeat(0L, capacity));
            largeArray.CopyTo(targetLargeList, 0L, 0L, capacity);
            await Assert.That(targetLargeList).IsEquivalentTo(largeArray);

            // Partial copy from source to LargeList
            if (count > 0)
            {
                targetLargeList = new(capacity);
                targetLargeList.AddRange(LargeEnumerable.Repeat(0L, capacity));
                largeArray.CopyTo(targetLargeList, offset, 0L, count);
                await Assert.That(targetLargeList.GetAll(0L, count)).IsEquivalentTo(largeArray.GetAll(offset, count));
            }

            // Partial copy to LargeList
            if (count > 0)
            {
                targetLargeList = new(capacity);
                targetLargeList.AddRange(LargeEnumerable.Repeat(0L, capacity));
                largeArray.CopyTo(targetLargeList, 0L, offset, count);
                await Assert.That(targetLargeList.GetAll(offset, count)).IsEquivalentTo(largeArray.GetAll(0L, count));
            }
        }

        // Test 3: CopyToArray variants
        if (capacity > 0 && capacity <= int.MaxValue)
        {
            // Full array copy to Array
            long[] targetArray = new long[capacity];
            largeArray.CopyToArray(targetArray, 0L, 0, (int)capacity);
            await Assert.That(targetArray).IsEquivalentTo(largeArray);

            // Partial copy from source to Array
            if (count > 0)
            {
                targetArray = new long[capacity];
                largeArray.CopyToArray(targetArray, offset, 0, (int)count);
                await Assert.That(targetArray.Take((int)count)).IsEquivalentTo(largeArray.GetAll(offset, count));
            }

            // Copy to Array with destination offset
            if (count > 0 && capacity > count)
            {
                targetArray = new long[capacity];
                largeArray.CopyToArray(targetArray, 0L, 0, (int)count);
                await Assert.That(targetArray.Take((int)count)).IsEquivalentTo(largeArray.GetAll(0L, count));
            }

            // Copy to Array span
            if (count > 0)
            {
                targetArray = new long[capacity];
                largeArray.CopyToSpan(targetArray.AsSpan((int)offset, (int)count), 0L, (int)count);
                await Assert.That(targetArray.Skip((int)offset).Take((int)count)).IsEquivalentTo(largeArray.GetAll(0L, count));
            }
        }

        // Test 4: CopyToSpan variants
        if (capacity > 0 && capacity <= int.MaxValue)
        {
            // Full array copy to Span
            Span<long> targetSpan = new long[capacity];
            largeArray.CopyToSpan(targetSpan, 0L, (int)capacity);
            await Assert.That(targetSpan.ToArray()).IsEquivalentTo(largeArray);

            // Partial copy from source to Span
            if (count > 0)
            {
                targetSpan = new long[capacity];
                largeArray.CopyToSpan(targetSpan, offset, (int)count);
                await Assert.That(targetSpan.Slice(0, (int)count).ToArray()).IsEquivalentTo(largeArray.GetAll(offset, count));
            }

            // Copy to partial Span
            if (count > 0)
            {
                long[] backingArray = new long[capacity];
                Span<long> partialSpan = backingArray.AsSpan((int)offset, (int)count);
                largeArray.CopyToSpan(partialSpan, 0L, (int)count);
                await Assert.That(backingArray.Skip((int)offset).Take((int)count)).IsEquivalentTo(largeArray.GetAll(0L, count));
            }
        }

        // Test 5: CopyFrom Array variants
        if (capacity > 0 && capacity <= int.MaxValue)
        {
            long[] sourceArray = LargeEnumerable.Range(capacity).Select(x => x * 10).ToArray();

            // Full array copy from Array
            ILargeArray<long> target = getTarget(capacity);
            target.CopyFromArray(sourceArray, 0, 0L, (int)capacity);
            await Assert.That(target).IsEquivalentTo(sourceArray);

            // Partial copy from Array
            if (count > 0)
            {
                target = getTarget(capacity);
                target.CopyFromArray(sourceArray, (int)offset, 0L, (int)count);
                await Assert.That(target.GetAll(0L, count)).IsEquivalentTo(sourceArray.Skip((int)offset).Take((int)count));
            }

            // Copy from Array to target offset
            if (count > 0)
            {
                target = getTarget(capacity);
                target.CopyFromArray(sourceArray, 0, offset, (int)count);
                await Assert.That(target.GetAll(offset, count)).IsEquivalentTo(sourceArray.Take((int)count));
            }

            // Copy from Array span
            if (count > 0)
            {
                target = getTarget(capacity);
                target.CopyFromSpan(sourceArray.AsSpan((int)offset, (int)count), 0L, (int)count);
                await Assert.That(target.GetAll(0L, count)).IsEquivalentTo(sourceArray.Skip((int)offset).Take((int)count));
            }
        }

        // Test 6: CopyFromSpan variants
        if (capacity > 0 && capacity <= int.MaxValue)
        {
            long[] sourceArray = LargeEnumerable.Range(capacity).Select(x => x * 100).ToArray();

            // Full array copy from Span
            ILargeArray<long> target = getTarget(capacity);
            target.CopyFromSpan(sourceArray.AsSpan(), 0L, (int)capacity);
            await Assert.That(target).IsEquivalentTo(sourceArray);

            // Partial copy from Span
            if (count > 0)
            {
                target = getTarget(capacity);
                ReadOnlySpan<long> partialSourceSpan = sourceArray.AsSpan().Slice((int)offset, (int)count);
                target.CopyFromSpan(partialSourceSpan, 0L, (int)count);
                await Assert.That(target.GetAll(0L, count)).IsEquivalentTo(sourceArray.Skip((int)offset).Take((int)count));
            }

            // Copy from Span to target offset
            if (count > 0)
            {
                target = getTarget(capacity);
                ReadOnlySpan<long> partialSourceSpan = sourceArray.AsSpan().Slice(0, (int)count);
                target.CopyFromSpan(partialSourceSpan, offset, (int)count);
                await Assert.That(target.GetAll(offset, count)).IsEquivalentTo(sourceArray.Take((int)count));
            }
        }

        // Test 7: CopyFrom ILargeArray variants
        if (capacity > 0)
        {
            ILargeArray<long> sourceArray = getTarget(capacity);
            for (long i = 0; i < capacity; i++)
            {
                sourceArray[i] = i * 1000;
            }

            // Full array copy from ILargeArray
            ILargeArray<long> target = getTarget(capacity);
            target.CopyFrom(sourceArray, 0L, 0L, capacity);
            await Assert.That(target).IsEquivalentTo(sourceArray);

            // Partial copy from ILargeArray
            if (count > 0)
            {
                target = getTarget(capacity);
                target.CopyFrom(sourceArray, offset, 0L, count);
                await Assert.That(target.GetAll(0L, count)).IsEquivalentTo(sourceArray.GetAll(offset, count));
            }

            // Copy from ILargeArray to target offset
            if (count > 0)
            {
                target = getTarget(capacity);
                target.CopyFrom(sourceArray, 0L, offset, count);
                await Assert.That(target.GetAll(offset, count)).IsEquivalentTo(sourceArray.GetAll(0L, count));
            }
        }

        // Test 8: CopyFrom LargeList variants
        if (capacity > 0)
        {
            LargeList<long> sourceLargeList = new();
            sourceLargeList.AddRange(LargeEnumerable.Range(capacity).Select(x => x * 10000));

            // Full array copy from LargeList
            ILargeArray<long> target = getTarget(capacity);
            target.CopyFrom(sourceLargeList, 0L, 0L, capacity);
            await Assert.That(target).IsEquivalentTo(sourceLargeList);

            // Partial copy from LargeList
            if (count > 0)
            {
                target = getTarget(capacity);
                target.CopyFrom(sourceLargeList, offset, 0L, count);
                await Assert.That(target.GetAll(0L, count)).IsEquivalentTo(sourceLargeList.GetAll(offset, count));
            }

            // Copy from LargeList to target offset
            if (count > 0)
            {
                target = getTarget(capacity);
                target.CopyFrom(sourceLargeList, 0L, offset, count);
                await Assert.That(target.GetAll(offset, count)).IsEquivalentTo(sourceLargeList.GetAll(0L, count));
            }
        }

        // Test 9: Edge cases and error conditions
        if (capacity > 0)
        {
            ILargeArray<long> target = getTarget(capacity);

            // Test null array/span arguments
            await Assert.That(() => target.CopyFromArray(null, 0, 0L, 0)).Throws<ArgumentNullException>();

            // Test array bounds violations
            if (capacity <= int.MaxValue)
            {
                long[] smallArray = new long[capacity / 2];
                await Assert.That(() => target.CopyFromArray(smallArray, 0, 0L, (int)capacity)).Throws<ArgumentException>();
                await Assert.That(() => largeArray.CopyToArray(smallArray, 0L, 0, (int)capacity)).Throws<ArgumentException>();
            }

            // Test ILargeArray size mismatches
            ILargeArray<long> smallTarget = getTarget(capacity / 2);
            if (capacity > 1)
            {
                await Assert.That(() => largeArray.CopyTo(smallTarget, 0L, 0L, capacity)).Throws<ArgumentException>();
            }
        }

        // Test 10: Zero-length copies (should not throw)
        if (capacity > 0)
        {
            ILargeArray<long> target = getTarget(capacity);

            // Zero-length copy operations should succeed
            target.CopyFrom(largeArray, 0L, 0L, 0L);
            largeArray.CopyTo(target, 0L, 0L, 0L);

            if (capacity <= int.MaxValue)
            {
                long[] array = new long[capacity];
                target.CopyFromArray(array, 0, 0L, 0);
                largeArray.CopyToArray(array, 0L, 0, 0);

                Span<long> span = array.AsSpan();
                target.CopyFromSpan(span.Slice(0, 0), 0L, 0);
                largeArray.CopyToSpan(span.Slice(0, 0), 0L, 0);
            }
        }
    }

    #region RefAccess Static Test Methods

    /// <summary>
    /// Tests GetRef functionality including bounds checking if the array implements IRefAccessLargeArray.
    /// </summary>
    /// <param name="largeArray">The array to test</param>
    /// <param name="capacity">The capacity/count of the array</param>
    public static async Task TestGetRef(ILargeArray<long> largeArray, long capacity)
    {
        if (largeArray is not IRefAccessLargeArray<long> refAccessArray)
        {
            return; // Skip if not supported
        }

        // Test GetRef method
        for (long i = 0; i < capacity; i++)
        {
            ref long refValue = ref refAccessArray.GetRef(i);
            // Modify through reference
            refValue = i * 10; // Use different value to verify ref access

            // Verify the change was applied
            await Assert.That(largeArray[i]).IsEqualTo(i * 10);
        }

        // Test GetRef method bounds checking
        await Assert.That(() => refAccessArray.GetRef(-1)).Throws<IndexOutOfRangeException>();
        await Assert.That(() => refAccessArray.GetRef(capacity)).Throws<IndexOutOfRangeException>();
        await Assert.That(() => refAccessArray.GetRef(capacity + 1L)).Throws<IndexOutOfRangeException>();
    }

    /// <summary>
    /// Tests DoForEach with RefAction functionality if the array implements IRefAccessLargeArray.
    /// </summary>
    /// <param name="largeArray">The array to test</param>
    /// <param name="capacity">The capacity/count of the array</param>
    public static async Task TestDoForEachRefAction(ILargeArray<long> largeArray, long capacity)
    {
        if (largeArray is not IRefAccessLargeArray<long> refAccessArray)
        {
            return; // Skip if not supported
        }

        // Initialize array with sequential values
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
        }

        // Test DoForEach with RefAction<T> (can modify)
        refAccessArray.DoForEach(static (ref long i) =>
        {
            i = i * 2; // Double each value
        });

        // Verify all values were doubled
        for (long i = 0; i < capacity; i++)
        {
            await Assert.That(largeArray[i]).IsEqualTo(i * 2);
        }
    }

    /// <summary>
    /// Tests DoForEach with RefActionWithUserData functionality if the array implements IRefAccessLargeArray.
    /// </summary>
    /// <param name="largeArray">The array to test</param>
    /// <param name="capacity">The capacity/count of the array</param>
    public static async Task TestDoForEachRefActionWithUserData(ILargeArray<long> largeArray, long capacity)
    {
        if (largeArray is not IRefAccessLargeArray<long> refAccessArray)
        {
            return; // Skip if not supported
        }

        // Initialize array with sequential values
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
        }

        // Test DoForEach with RefActionWithUserData<T, TUserData>
        long sum = 0L;
        refAccessArray.DoForEach(static (ref long value, ref long userData) =>
        {
            userData += value;
            value = -value; // Negate the value
        }, ref sum);

        long expectedSum = LargeEnumerable.Range(capacity).Sum();
        await Assert.That(sum).IsEqualTo(expectedSum);

        // Verify all values were negated
        for (long i = 0; i < capacity; i++)
        {
            await Assert.That(largeArray[i]).IsEqualTo(-i);
        }
    }

    /// <summary>
    /// Tests ranged DoForEach with RefAction functionality if the array implements IRefAccessLargeArray.
    /// </summary>
    /// <param name="largeArray">The array to test</param>
    /// <param name="capacity">The capacity/count of the array</param>
    /// <param name="offset">The offset for the range</param>
    /// <param name="count">The count for the range</param>
    public static async Task TestDoForEachRangedRefAction(ILargeArray<long> largeArray, long capacity, long offset, long count)
    {
        if (largeArray is not IRefAccessLargeArray<long> refAccessArray || count <= 0 || offset + count > capacity)
        {
            return; // Skip if not supported or invalid range
        }

        // Initialize array with sequential values
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
        }

        // Test argument validation for ranged DoForEach
        await Assert.That(() => refAccessArray.DoForEach((ref long i) => { }, -1L, count)).Throws<ArgumentException>();
        await Assert.That(() => refAccessArray.DoForEach((ref long i) => { }, 0L, -1L)).Throws<ArgumentException>();
        await Assert.That(() => refAccessArray.DoForEach((ref long i) => { }, 1L, capacity)).Throws<ArgumentException>();

        // Test ranged DoForEach with RefAction<T>
        refAccessArray.DoForEach((ref long i) =>
        {
            i = -i;
        }, offset, count);

        // Verify ranged modification
        for (long i = 0; i < capacity; i++)
        {
            if (i >= offset && i < offset + count)
            {
                await Assert.That(largeArray[i]).IsEqualTo(-i);
            }
            else
            {
                await Assert.That(largeArray[i]).IsEqualTo(i);
            }
        }
    }

    /// <summary>
    /// Tests ranged DoForEach with RefActionWithUserData functionality if the array implements IRefAccessLargeArray.
    /// </summary>
    /// <param name="largeArray">The array to test</param>
    /// <param name="capacity">The capacity/count of the array</param>
    /// <param name="offset">The offset for the range</param>
    /// <param name="count">The count for the range</param>
    public static async Task TestDoForEachRangedRefActionWithUserData(ILargeArray<long> largeArray, long capacity, long offset, long count)
    {
        if (largeArray is not IRefAccessLargeArray<long> refAccessArray || count <= 0 || offset + count > capacity)
        {
            return; // Skip if not supported or invalid range
        }

        // Initialize array with sequential values
        for (long i = 0; i < capacity; i++)
        {
            largeArray[i] = i;
        }

        // Test ranged DoForEach with RefActionWithUserData<T, TUserData>
        long sum = 0L;
        refAccessArray.DoForEach(static (ref long value, ref long userData) =>
        {
            userData += value;
            value = value * 3; // Triple the value
        }, offset, count, ref sum);

        long expectedSum = LargeEnumerable.Range(offset, count).Sum();
        await Assert.That(sum).IsEqualTo(expectedSum);

        // Verify ranged modification
        for (long i = 0; i < capacity; i++)
        {
            if (i >= offset && i < offset + count)
            {
                await Assert.That(largeArray[i]).IsEqualTo(i * 3);
            }
            else
            {
                await Assert.That(largeArray[i]).IsEqualTo(i);
            }
        }
    }

    /// <summary>
    /// Runs all ref access tests on the provided array if it implements IRefAccessLargeArray.
    /// </summary>
    /// <param name="largeArray">The array to test</param>
    /// <param name="capacity">The capacity/count of the array</param>
    /// <param name="offset">Optional offset for ranged tests (default: 1)</param>
    public static async Task TestAllRefAccess(ILargeArray<long> largeArray, long capacity, long offset = 1L)
    {
        if (capacity == 0 || largeArray is not IRefAccessLargeArray<long>)
        {
            return; // Skip tests for empty arrays or arrays without ref access
        }

        await TestGetRef(largeArray, capacity);
        await TestDoForEachRefAction(largeArray, capacity);
        await TestDoForEachRefActionWithUserData(largeArray, capacity);

        long rangeCount = Math.Max(0, capacity - 2L * offset);
        if (rangeCount > 0)
        {
            await TestDoForEachRangedRefAction(largeArray, capacity, offset, rangeCount);
            await TestDoForEachRangedRefActionWithUserData(largeArray, capacity, offset, rangeCount);
        }
    }

    #endregion
}
