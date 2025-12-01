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
using System.Linq;
using System.Threading.Tasks;
using LargeCollections.Test.Helpers;
using TUnit.Core;

namespace LargeCollections.Test;

public class LargeArrayTest
{
    public static IEnumerable<long> Capacities() => Parameters.Capacities;

    #region Constructor, Count, Indexer

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Constructor_SetsCount_And_Validates(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            await Assert.That(() => new LargeArray<long>(capacity)).Throws<Exception>();
            return;
        }

        LargeArray<long> array = new(capacity);
        await Assert.That(array.Count).IsEqualTo(capacity);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Indexer_Get_Set_And_Boundaries(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> array = new(capacity);
        if (capacity > 0)
        {
            foreach (long index in new long[] { 0L, capacity / 2, Math.Max(0L, capacity - 1L) }.Distinct().Where(i => i < capacity))
            {
                long value = index + 123;
                array[index] = value;
                await Assert.That(array[index]).IsEqualTo(value);
            }
        }

        await Assert.That(() => array[-1]).Throws<Exception>();
        await Assert.That(() => array[-1] = 0).Throws<Exception>();
        await Assert.That(() => array[capacity]).Throws<Exception>();
        await Assert.That(() => array[capacity] = 0).Throws<Exception>();
    }

    #endregion

    #region Resize

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Resize_Grows_Shrinks_And_PreservesData(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> array = CreateSequentialArray(capacity);

        long growTarget = Math.Min(Constants.MaxLargeCollectionCount, capacity + 3);
        array.Resize(growTarget);
        await Assert.That(array.Count).IsEqualTo(growTarget);

        long preserved = Math.Min(capacity, growTarget);
        for (long i = 0; i < preserved; i++)
        {
            await Assert.That(array[i]).IsEqualTo(i);
        }

        long shrinkTarget = Math.Max(0, growTarget - 2);
        array.Resize(shrinkTarget);
        await Assert.That(array.Count).IsEqualTo(shrinkTarget);
        for (long i = 0; i < Math.Min(preserved, shrinkTarget); i++)
        {
            await Assert.That(array[i]).IsEqualTo(i);
        }
    }

    [Test]
    [Arguments(-1L)]
    [Arguments(Constants.MaxLargeCollectionCount + 1L)]
    public async Task Resize_InvalidCapacity_Throws(long invalidCapacity)
    {
        LargeArray<long> array = new(1);
        await Assert.That(() => array.Resize(invalidCapacity)).Throws<Exception>();
    }

    #endregion

    #region Get, Set, GetRef

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Get_Set_GetRef_Work_And_Validate(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> array = new(capacity);
        if (capacity == 0)
        {
            await Assert.That(() => array.Get(0)).Throws<Exception>();
            await Assert.That(() => array.Set(0, 1)).Throws<Exception>();
            await Assert.That(() => array.GetRef(0)).Throws<Exception>();
            return;
        }

        long index = Math.Max(0, capacity / 2);
        array.Set(index, 99);
        await Assert.That(array.Get(index)).IsEqualTo(99);

        ref long reference = ref array.GetRef(index);
        reference = 1234;
        await Assert.That(array.Get(index)).IsEqualTo(1234);

        await Assert.That(() => array.Get(-1)).Throws<Exception>();
        await Assert.That(() => array.Set(-1, 1)).Throws<Exception>();
        await Assert.That(() => array.GetRef(-1)).Throws<Exception>();
        await Assert.That(() => array.Get(capacity)).Throws<Exception>();
        await Assert.That(() => array.Set(capacity, 1)).Throws<Exception>();
        await Assert.That(() => array.GetRef(capacity)).Throws<Exception>();
    }

    #endregion

    #region Contains / IndexOf / LastIndexOf

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Contains_Overloads(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> array = CreateSequentialArray(capacity);
        long existing = capacity > 0 ? array[Math.Max(0, capacity / 2)] : 42;
        long missing = existing + 1000;

        await Assert.That(array.Contains(existing)).IsEqualTo(capacity > 0);
        await Assert.That(array.Contains(missing)).IsFalse();

        // Test with generic comparer
        DefaultEqualityComparer<long> comparer = new();
        await Assert.That(array.Contains(existing, ref comparer)).IsEqualTo(capacity > 0);
        DefaultEqualityComparer<long> comparer1b = new();
        await Assert.That(array.Contains(missing, ref comparer1b)).IsFalse();

        long offset = Math.Min(1, Math.Max(0, capacity - 1));
        long length = Math.Max(0, capacity - offset);
        bool expectedInRange = capacity > 0 && length > 0 && existing >= offset && existing < offset + length;
        await Assert.That(array.Contains(existing, offset, length)).IsEqualTo(expectedInRange);
        await Assert.That(array.Contains(missing, offset, length)).IsFalse();
        DefaultEqualityComparer<long> comparer2 = new();
        await Assert.That(array.Contains(existing, ref comparer2, offset, length)).IsEqualTo(expectedInRange);
        DefaultEqualityComparer<long> comparer3 = new();
        await Assert.That(array.Contains(missing, ref comparer3, offset, length)).IsFalse();

        await Assert.That(() => array.Contains(existing, -1, 1)).Throws<Exception>();
        await Assert.That(() => array.Contains(existing, 0, capacity + 1)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task IndexOf_And_LastIndexOf_Overloads(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> array = CreateSequentialArray(capacity);
        if (capacity > 0)
        {
            long baseIndex = Math.Max(0, capacity / 2);
            long marker = capacity + 1000;
            array[baseIndex] = marker;
            if (baseIndex + 1 < capacity)
            {
                array[baseIndex + 1] = marker;
            }

            await Assert.That(array.IndexOf(marker)).IsEqualTo(baseIndex);
            DefaultEqualityComparer<long> comparer = new();
            await Assert.That(array.IndexOf(marker, ref comparer)).IsEqualTo(baseIndex);
            await Assert.That(array.LastIndexOf(marker)).IsEqualTo(baseIndex + (baseIndex + 1 < capacity ? 1 : 0));
            DefaultEqualityComparer<long> comparer2 = new();
            await Assert.That(array.LastIndexOf(marker, ref comparer2)).IsEqualTo(baseIndex + (baseIndex + 1 < capacity ? 1 : 0));

            long offset = baseIndex;
            long length = Math.Max(1, Math.Min(2, capacity - offset));
            await Assert.That(array.IndexOf(marker, offset, length)).IsEqualTo(offset);
            DefaultEqualityComparer<long> comparer3 = new();
            await Assert.That(array.IndexOf(marker, ref comparer3, offset, length)).IsEqualTo(offset);
            await Assert.That(array.LastIndexOf(marker, offset, length)).IsEqualTo(offset + Math.Min(1, length - 1));
            DefaultEqualityComparer<long> comparer4 = new();
            await Assert.That(array.LastIndexOf(marker, ref comparer4, offset, length)).IsEqualTo(offset + Math.Min(1, length - 1));
        }

        long missingValue = capacity + 2000;
        await Assert.That(array.IndexOf(missingValue)).IsEqualTo(-1L);
        await Assert.That(array.LastIndexOf(missingValue)).IsEqualTo(-1L);
        await Assert.That(() => array.IndexOf(1, -1, 1)).Throws<Exception>();
        await Assert.That(() => array.IndexOf(1, 0, capacity + 1)).Throws<Exception>();
        await Assert.That(() => array.LastIndexOf(1, -1, 1)).Throws<Exception>();
        await Assert.That(() => array.LastIndexOf(1, 0, capacity + 1)).Throws<Exception>();
    }

    #endregion

    #region BinarySearch

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task BinarySearch_Overloads(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> array = CreateSequentialArray(capacity);
        for (long i = 0; i < capacity; i++)
        {
            array[i] = i * 2;
        }

        long existing = capacity > 0 ? array[Math.Max(0, capacity / 2)] : 0;
        long missing = existing + 1;

        DefaultComparer<long> comparer = new();
        long result = array.BinarySearch(existing, comparer);
        if (capacity > 0)
        {
            await Assert.That(result).IsEqualTo(Math.Max(0, capacity / 2));
        }
        else
        {
            await Assert.That(result).IsEqualTo(~0L);
        }

        long missingResult = array.BinarySearch(missing, comparer);
        await Assert.That(missingResult).IsEqualTo(-1L);

        long offset = Math.Min(1, Math.Max(0, capacity - 1));
        long length = Math.Max(0, capacity - offset);
        if (length > 0)
        {
            long value = array[offset + length / 2];
            long rangeResult = array.BinarySearch(value, comparer, offset, length);
            await Assert.That(rangeResult).IsEqualTo(offset + length / 2);
        }

        await Assert.That(() => array.BinarySearch(0, comparer, -1, 1)).Throws<Exception>();
        await Assert.That(() => array.BinarySearch(0, comparer, 0, capacity + 1)).Throws<Exception>();
    }

    #endregion

    #region ParallelSort

    [Test]
    public async Task ParallelSort_SortsCorrectly()
    {
        // Use the maximum capacity that adapts to unit test constants
        long capacity = Constants.MaxLargeCollectionCount;
        LargeArray<long> array = new(capacity);
        Random random = new(42); // Seeded for reproducibility
        
        for (long i = 0; i < capacity; i++)
        {
            array[i] = random.NextInt64();
        }

        array.ParallelSort(new DefaultComparer<long>());

        // Verify sorted
        for (long i = 1; i < capacity; i++)
        {
            await Assert.That(array[i - 1] <= array[i]).IsTrue();
        }
    }

    [Test]
    public async Task ParallelSort_WithComparer_SortsDescending()
    {
        long capacity = Constants.MaxLargeCollectionCount;
        LargeArray<long> array = new(capacity);
        Random random = new(123);
        
        for (long i = 0; i < capacity; i++)
        {
            array[i] = random.NextInt64();
        }

        // Sort descending
        array.ParallelSort(new DescendingComparer<long>());

        // Verify sorted descending
        for (long i = 1; i < capacity; i++)
        {
            await Assert.That(array[i - 1] >= array[i]).IsTrue();
        }
    }

    [Test]
    public async Task ParallelSort_WithRange_OnlySortsRange()
    {
        long capacity = Constants.MaxLargeCollectionCount;
        LargeArray<long> array = new(capacity);
        Random random = new(456);
        
        for (long i = 0; i < capacity; i++)
        {
            array[i] = random.NextInt64();
        }

        // Remember values outside the range
        long firstValue = array[0];
        long lastValue = array[capacity - 1];

        // Sort only middle portion (middle 80%)
        long offset = capacity / 10;
        long count = capacity * 8 / 10;
        array.ParallelSort(new DefaultComparer<long>(), offset, count);

        // Verify only range is sorted
        for (long i = offset + 1; i < offset + count; i++)
        {
            await Assert.That(array[i - 1] <= array[i]).IsTrue();
        }

        // First and last should be unchanged (unless they happened to be in sorted positions)
        await Assert.That(array[0]).IsEqualTo(firstValue);
        await Assert.That(array[capacity - 1]).IsEqualTo(lastValue);
    }

    [Test]
    public async Task ParallelSort_WithMaxDegreeOfParallelism_Works()
    {
        long capacity = Constants.MaxLargeCollectionCount;
        LargeArray<long> array = new(capacity);
        
        for (long i = 0; i < capacity; i++)
        {
            array[i] = capacity - i; // Reverse order
        }

        // Force single-threaded parallel sort
        array.ParallelSort(new DefaultComparer<long>(), maxDegreeOfParallelism: 1);

        // Verify sorted
        for (long i = 1; i < capacity; i++)
        {
            await Assert.That(array[i - 1] <= array[i]).IsTrue();
        }
    }

    [Test]
    public async Task ParallelSort_SmallArray_FallsBackToRegularSort()
    {
        // Small arrays should use regular sort
        LargeArray<long> array = new(100);
        for (long i = 0; i < 100; i++)
        {
            array[i] = 100 - i;
        }

        array.ParallelSort(new DefaultComparer<long>());

        for (long i = 1; i < 100; i++)
        {
            await Assert.That(array[i - 1] <= array[i]).IsTrue();
        }
    }

    [Test]
    public async Task ParallelSort_EmptyArray_DoesNotThrow()
    {
        LargeArray<long> array = new(0);
        await Assert.That(() => array.ParallelSort(new DefaultComparer<long>())).ThrowsNothing();
    }

    [Test]
    public async Task ParallelSort_SingleElement_DoesNotThrow()
    {
        LargeArray<long> array = new(1);
        array[0] = 42;
        await Assert.That(() => array.ParallelSort(new DefaultComparer<long>())).ThrowsNothing();
        await Assert.That(array[0]).IsEqualTo(42);
    }

    #endregion

    #region StructComparerSort

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Sort_WithStructComparer_ProducesSameResultAsDelegate(long capacity)
    {
        if (capacity <= 0)
        {
            return;
        }

        Random random = new(42);

        // Create two arrays with identical random data
        LargeArray<long> arrayDelegate = new(capacity);
        LargeArray<long> arrayStruct = new(capacity);

        for (long i = 0; i < capacity; i++)
        {
            long value = random.NextInt64();
            arrayDelegate[i] = value;
            arrayStruct[i] = value;
        }

        // Sort with default comparer
        arrayDelegate.Sort(new DefaultComparer<long>());

        // Sort with struct comparer
        arrayStruct.Sort(new DefaultComparer<long>());

        // Verify results are identical
        for (long i = 0; i < capacity; i++)
        {
            await Assert.That(arrayStruct[i]).IsEqualTo(arrayDelegate[i]);
        }
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Sort_WithDescendingStructComparer_SortsDescending(long capacity)
    {
        if (capacity <= 0)
        {
            return;
        }

        LargeArray<long> array = new(capacity);
        Random random = new(123);

        for (long i = 0; i < capacity; i++)
        {
            array[i] = random.NextInt64();
        }

        array.Sort(new DescendingComparer<long>());

        for (long i = 1; i < capacity; i++)
        {
            await Assert.That(array[i - 1] >= array[i]).IsTrue();
        }
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task BinarySearch_WithStructComparer_FindsItems(long capacity)
    {
        if (capacity <= 0)
        {
            return;
        }

        LargeArray<long> array = new(capacity);
        for (long i = 0; i < capacity; i++)
        {
            array[i] = i * 2; // Even numbers: 0, 2, 4, 6, ...
        }

        // Search for existing item
        long searchItem = (capacity / 2) * 2;
        long foundIndex = array.BinarySearch(searchItem, new DefaultComparer<long>());
        await Assert.That(foundIndex).IsEqualTo(capacity / 2);

        // Search for non-existing item
        long notFoundIndex = array.BinarySearch(-1, new DefaultComparer<long>());
        await Assert.That(notFoundIndex).IsEqualTo(-1L);
    }

    #endregion

    #region StructAction DoForEach

    /// <summary>
    /// A struct action that increments each element by a fixed value.
    /// </summary>
    private struct IncrementAction : ILargeRefAction<long>
    {
        public long IncrementBy;
        public long ModifiedCount;

        public void Invoke(ref long item)
        {
            item += IncrementBy;
            ModifiedCount++;
        }
    }

    /// <summary>
    /// A struct action that tracks the sum (by-value action with state in struct).
    /// </summary>
    private struct SumAction : ILargeAction<long>
    {
        public long Sum;

        public void Invoke(long item)
        {
            Sum += item;
        }
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task DoForEach_WithStructAction_ByValue_AccumulatesSum(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> array = CreateSequentialArray(capacity);

        // With ref parameter, state changes are preserved!
        SumAction action = new() { Sum = 0 };
        array.DoForEach(ref action);

        // Sum of 0 to n-1 = n*(n-1)/2
        long expected = capacity * (capacity - 1) / 2;
        await Assert.That(action.Sum).IsEqualTo(expected);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task DoForEachRef_WithStructAction_ModifiesElements(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> array = CreateSequentialArray(capacity);
        IncrementAction action = new() { IncrementBy = 10 };

        array.DoForEachRef(ref action);

        for (long i = 0; i < capacity; i++)
        {
            await Assert.That(array[i]).IsEqualTo(i + 10);
        }

        // Verify the action's state was updated
        await Assert.That(action.ModifiedCount).IsEqualTo(capacity);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task DoForEachRef_WithStructAction_Range_ModifiesOnlyRange(long capacity)
    {
        if (capacity <= 2 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> array = CreateSequentialArray(capacity);
        IncrementAction action = new() { IncrementBy = 100 };

        long offset = 1;
        long count = capacity - 2;
        array.DoForEachRef(ref action, offset, count);

        // First element should be unchanged
        await Assert.That(array[0]).IsEqualTo(0L);

        // Middle elements should be incremented
        for (long i = offset; i < offset + count; i++)
        {
            await Assert.That(array[i]).IsEqualTo(i + 100);
        }

        // Last element should be unchanged
        await Assert.That(array[capacity - 1]).IsEqualTo(capacity - 1);

        // Verify the action tracked the right number of modifications
        await Assert.That(action.ModifiedCount).IsEqualTo(count);
    }

    #endregion

    #region CopyFrom

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyFrom_IReadOnlyLargeArray_Branches(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> target = CreateSequentialArray(capacity);
        LargeArray<long> sourceArray = CreateSequentialArray(capacity);
        LargeList<long> sourceList = CreateListWithSequence(capacity);
        ReadOnlyLargeArrayStub fallbackSource = new(sourceArray);

        long copyCount = Math.Min(capacity, 5);
        long sourceOffset = 0;
        long targetOffset = Math.Max(0, capacity - copyCount);

        target.CopyFrom(sourceArray, sourceOffset, targetOffset, copyCount);
        await VerifyRangeEquals(target, sourceArray, targetOffset, sourceOffset, copyCount);

        target.CopyFrom(sourceList, sourceOffset, targetOffset, copyCount);
        await VerifyRangeEquals(target, sourceList, targetOffset, sourceOffset, copyCount);

        target.CopyFrom(fallbackSource, sourceOffset, targetOffset, copyCount);
        await VerifyRangeEquals(target, fallbackSource, targetOffset, sourceOffset, copyCount);

        await Assert.That(() => target.CopyFrom(sourceArray, -1, 0, 1)).Throws<Exception>();
        await Assert.That(() => target.CopyFrom(sourceArray, 0, -1, 1)).Throws<Exception>();
        await Assert.That(() => target.CopyFrom(sourceArray, 0, 0, capacity + 1)).Throws<Exception>();
        await Assert.That(() => target.CopyFrom((IReadOnlyLargeArray<long>)null, 0, 0, 1)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyFrom_ReadOnlyLargeSpan_And_Span(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> target = CreateSequentialArray(capacity);
        LargeArray<long> arraySource = CreateSequentialArray(capacity);
        LargeList<long> listSource = CreateListWithSequence(capacity);

        long copyCount = Math.Min(capacity, 5);
        long targetOffset = Math.Max(0, capacity - copyCount);

        ReadOnlyLargeSpan<long> arraySpan = new(arraySource, 0, arraySource.Count);
        target.CopyFrom(arraySpan, targetOffset, copyCount);
        await VerifyRangeEquals(target, arraySource, targetOffset, arraySpan.Start, copyCount);

        ReadOnlyLargeSpan<long> listSpan = new(listSource, 0, listSource.Count);
        target.CopyFrom(listSpan, targetOffset, copyCount);
        await VerifyRangeEquals(target, listSource, targetOffset, listSpan.Start, copyCount);

        long[] raw = arraySource.GetAll().ToArray();
        int spanCount = (int)Math.Min(copyCount, raw.Length);
        ReadOnlySpan<long> span = new(raw, 0, spanCount);
        target.CopyFromSpan(span, targetOffset, spanCount);
        await VerifyRangeEquals(target, raw, targetOffset, 0, spanCount);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyFromArray_Validates(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        int length = (int)Math.Min(capacity, 10);
        long[] source = Enumerable.Range(0, length).Select(i => (long)i).ToArray();
        LargeArray<long> target = CreateSequentialArray(capacity);

        target.CopyFromArray(source, 0, 0, length);
        await VerifyRangeEquals(target, source, 0, 0, length);

        if (length > 0)
        {
            await Assert.That(() => target.CopyFromArray(source, -1, 0, 1)).Throws<Exception>();
            await Assert.That(() => target.CopyFromArray(source, 0, -1, 1)).Throws<Exception>();
            await Assert.That(() => target.CopyFromArray(source, 0, 0, source.Length + 1)).Throws<Exception>();
        }
        await Assert.That(() => target.CopyFromArray(null!, 0, 0, 1)).Throws<Exception>();
    }

    #endregion

    #region CopyTo

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyTo_ILargeArray_Branches(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> source = CreateSequentialArray(capacity);
        LargeArray<long> arrayTarget = CreateSequentialArray(capacity);
        LargeList<long> listTarget = CreateListWithSequence(capacity);
        LargeArrayFacade fallbackTarget = new(capacity);

        long copyCount = Math.Min(capacity, 5);
        long sourceOffset = Math.Max(0, capacity - copyCount);

        source.CopyTo(arrayTarget, sourceOffset, 0, copyCount);
        await VerifyRangeEquals(arrayTarget, source, 0, sourceOffset, copyCount);

        source.CopyTo(listTarget, sourceOffset, 0, copyCount);
        await VerifyRangeEquals(listTarget, source, 0, sourceOffset, copyCount);

        source.CopyTo(fallbackTarget, sourceOffset, 0, copyCount);
        await VerifyRangeEquals(fallbackTarget, source, 0, sourceOffset, copyCount);

        await Assert.That(() => source.CopyTo(arrayTarget, -1, 0, 1)).Throws<Exception>();
        await Assert.That(() => source.CopyTo(arrayTarget, 0, -1, 1)).Throws<Exception>();
        await Assert.That(() => source.CopyTo(arrayTarget, 0, 0, capacity + 1)).Throws<Exception>();
        await Assert.That(() => source.CopyTo((ILargeArray<long>)null!, 0, 0, 1)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyTo_Array_Span_And_LargeSpan(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> source = CreateSequentialArray(capacity);
        LargeArray<long> spanTargetArray = CreateSequentialArray(capacity);
        LargeSpan<long> spanTarget = new(spanTargetArray, 0, spanTargetArray.Count);

        long copyCount = Math.Min(capacity, 5);
        long sourceOffset = Math.Max(0, capacity - copyCount);
        source.CopyTo(spanTarget, sourceOffset, copyCount);
        await VerifyRangeEquals(spanTargetArray, source, 0, sourceOffset, copyCount);

        int arrayLength = (int)Math.Max(1, Math.Min(capacity, 10));
        long[] targetArray = new long[arrayLength];
        int copyLength = Math.Min(arrayLength, (int)Math.Min(copyCount, arrayLength));
        source.CopyToArray(targetArray, sourceOffset, 0, copyLength);
        await VerifyRangeEquals(targetArray, source, 0, sourceOffset, copyLength);

        Span<long> span = targetArray.AsSpan();
        int spanLength = Math.Min(copyLength, span.Length);
        source.CopyToSpan(span, sourceOffset, spanLength);
        await VerifyRangeEquals(targetArray, source, 0, sourceOffset, spanLength);

        if (arrayLength > 0)
        {
            await Assert.That(() => source.CopyToArray(targetArray, -1, 0, 1)).Throws<Exception>();
            await Assert.That(() => source.CopyToArray(targetArray, 0, -1, 1)).Throws<Exception>();
            await Assert.That(() => source.CopyToArray(targetArray, 0, 0, arrayLength + 1)).Throws<Exception>();
        }
    }

    #endregion

    #region DoForEach

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task DoForEach_AllOverloads(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        long offset = Math.Min(1, Math.Max(0, capacity - 1));
        long length = Math.Max(0, capacity - offset);

        LargeArray<long> sumArray = CreateSequentialArray(capacity);
        long sum = 0;
        sumArray.DoForEach(item => sum += item);
        await Assert.That(sum).IsEqualTo(sumArray.GetAll().Sum());

        LargeArray<long> rangeSumArray = CreateSequentialArray(capacity);
        long rangeSum = 0;
        rangeSumArray.DoForEach(item => rangeSum += item, offset, length);
        await Assert.That(rangeSum).IsEqualTo(rangeSumArray.GetAll(offset, length).Sum());

        // Struct-based action with user data
        LargeArray<long> userDataArray = CreateSequentialArray(capacity);
        SumAction sumAction = new ();
        userDataArray.DoForEach(ref sumAction);
        await Assert.That(sumAction.Sum).IsEqualTo(userDataArray.GetAll().Sum());

        // Struct-based action with user data and range
        LargeArray<long> userDataRangeArray = CreateSequentialArray(capacity);
        SumAction rangeSumAction = new ();
        userDataRangeArray.DoForEach(ref rangeSumAction, offset, length);
        await Assert.That(rangeSumAction.Sum).IsEqualTo(userDataRangeArray.GetAll(offset, length).Sum());

        // Struct-based ref action
        LargeArray<long> refArray = CreateSequentialArray(capacity);
        if (refArray.Count > 0)
        {
            long original = refArray[0];
            IncrementAction incrementAction = new () { IncrementBy = 1 };
            refArray.DoForEachRef(ref incrementAction);
            await Assert.That(refArray[0]).IsEqualTo(original + 1);
        }
        else
        {
            IncrementAction incrementAction = new () { IncrementBy = 1 };
            refArray.DoForEachRef(ref incrementAction);
        }

        // Struct-based ref action with range
        LargeArray<long> refRangeArray = CreateSequentialArray(capacity);
        if (length > 0)
        {
            long original = refRangeArray[offset];
            IncrementAction incrementAction = new () { IncrementBy = 1 };
            refRangeArray.DoForEachRef(ref incrementAction, offset, length);
            await Assert.That(refRangeArray[offset]).IsEqualTo(original + 1);
        }
        else
        {
            IncrementAction incrementAction = new () { IncrementBy = 1 };
            refRangeArray.DoForEachRef(ref incrementAction, offset, length);
        }

        // Struct-based ref action with user data
        LargeArray<long> refUserDataArray = CreateSequentialArray(capacity);
        long delta = 5;
        if (refUserDataArray.Count > 0)
        {
            long original = refUserDataArray[0];
            IncrementAction addAction = new () { IncrementBy = delta };
            refUserDataArray.DoForEachRef(ref addAction);
            await Assert.That(refUserDataArray[0]).IsEqualTo(original + delta);
        }
        else
        {
            IncrementAction addAction = new () { IncrementBy = delta };
            refUserDataArray.DoForEachRef(ref addAction);
        }

        // Struct-based ref action with user data and range
        LargeArray<long> refUserDataRangeArray = CreateSequentialArray(capacity);
        long rangeDelta = 7;
        if (length > 0)
        {
            long original = refUserDataRangeArray[offset];
            IncrementAction addAction = new () { IncrementBy = rangeDelta };
            refUserDataRangeArray.DoForEachRef(ref addAction, offset, length);
            await Assert.That(refUserDataRangeArray[offset]).IsEqualTo(original + rangeDelta);
        }
        else
        {
            IncrementAction addAction = new () { IncrementBy = rangeDelta };
            refUserDataRangeArray.DoForEachRef(ref addAction, offset, length);
        }

        // Validation tests
        LargeArray<long> validationArray = CreateSequentialArray(Math.Max(1, capacity));
        await Assert.That(() => validationArray.DoForEach((Action<long>)null!)).Throws<Exception>();
        await Assert.That(() => validationArray.DoForEach(static _ => { }, -1, 1)).Throws<Exception>();
    }

    #endregion

    #region GetAll / Enumeration

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task GetAll_And_Enumerators(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> array = CreateSequentialArray(capacity);
        List<long> all = array.GetAll().ToList();
        await Assert.That(all.Count).IsEqualTo((int)array.Count);

        long offset = Math.Min(1, Math.Max(0, array.Count - 1));
        long length = Math.Max(0, array.Count - offset);
        List<long> range = array.GetAll(offset, length).ToList();
        List<long> expectedRange = all.Skip((int)offset).Take((int)length).ToList();
        await Assert.That(range.SequenceEqual(expectedRange)).IsTrue();

        await Assert.That(() => array.GetAll(-1, 1).ToList()).Throws<Exception>();
        await Assert.That(() => array.GetAll(0, array.Count + 1).ToList()).Throws<Exception>();

        List<long> enumerated = new();
        foreach (long item in array)
        {
            enumerated.Add(item);
        }
        await Assert.That(enumerated.SequenceEqual(all)).IsTrue();

        List<long> enumeratedExplicit = new();
        IEnumerator enumerator = ((IEnumerable)array).GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumeratedExplicit.Add((long)enumerator.Current);
        }
        await Assert.That(enumeratedExplicit.SequenceEqual(all)).IsTrue();
    }

    #endregion

    #region Sort / Swap

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Sort_And_Swap_Work(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        LargeArray<long> array = CreateSequentialArray(capacity);
        for (long i = 0; i < array.Count; i++)
        {
            array[i] = array.Count - i;
        }

        array.Sort(new DefaultComparer<long>());
        List<long> sorted = array.GetAll().ToList();
        await Assert.That(sorted.SequenceEqual(sorted.OrderBy(x => x))).IsTrue();

        if (array.Count > 1)
        {
            long offset = 0;
            long length = Math.Min(array.Count, 5);
            array.Sort(new DescendingComparer<long>(), offset, length);
            List<long> segment = array.GetAll(offset, length).ToList();
            await Assert.That(segment.SequenceEqual(segment.OrderByDescending(x => x))).IsTrue();

            long left = 0;
            long right = array.Count - 1;
            long leftValue = array[left];
            long rightValue = array[right];
            array.Swap(left, right);
            await Assert.That(array[left]).IsEqualTo(rightValue);
            await Assert.That(array[right]).IsEqualTo(leftValue);
        }

        DefaultComparer<long> comparer = new();
        await Assert.That(() => array.Sort(comparer, -1, 1)).Throws<Exception>();
        await Assert.That(() => array.Swap(-1, 0)).Throws<Exception>();
        await Assert.That(() => array.Swap(0, array.Count)).Throws<Exception>();
    }

    #endregion

    #region Helper Methods / Types

    private static LargeArray<long> CreateSequentialArray(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        LargeArray<long> array = new(capacity);
        for (long i = 0; i < capacity; i++)
        {
            array[i] = i;
        }
        return array;
    }

    private static LargeList<long> CreateListWithSequence(long capacity)
    {
        LargeList<long> list = new(capacity);
        for (long i = 0; i < capacity; i++)
        {
            list.Add(i);
        }
        return list;
    }

    private static async Task VerifyRangeEquals(IReadOnlyLargeArray<long> target, IReadOnlyLargeArray<long> source, long targetOffset, long sourceOffset, long count)
    {
        for (long i = 0; i < count; i++)
        {
            if (targetOffset + i < target.Count && sourceOffset + i < source.Count)
            {
                await Assert.That(target[targetOffset + i]).IsEqualTo(source[sourceOffset + i]);
            }
        }
    }

    private static async Task VerifyRangeEquals(IList<long> target, IReadOnlyLargeArray<long> source, long targetOffset, long sourceOffset, long count)
    {
        for (long i = 0; i < count && targetOffset + i < target.Count && sourceOffset + i < source.Count; i++)
        {
            await Assert.That(target[(int)(targetOffset + i)]).IsEqualTo(source[sourceOffset + i]);
        }
    }

    private static async Task VerifyRangeEquals(long[] target, IReadOnlyLargeArray<long> source, long targetOffset, long sourceOffset, long count)
    {
        for (long i = 0; i < count && targetOffset + i < target.LongLength && sourceOffset + i < source.Count; i++)
        {
            await Assert.That(target[targetOffset + i]).IsEqualTo(source[sourceOffset + i]);
        }
    }

    private static async Task VerifyRangeEquals(IReadOnlyLargeArray<long> target, long[] source, long targetOffset, long sourceOffset, long count)
    {
        for (long i = 0; i < count && targetOffset + i < target.Count && sourceOffset + i < source.LongLength; i++)
        {
            await Assert.That(target[targetOffset + i]).IsEqualTo(source[sourceOffset + i]);
        }
    }

    private static async Task VerifyRangeEquals(long[] target, long[] source, long targetOffset, long sourceOffset, long count)
    {
        for (long i = 0; i < count && targetOffset + i < target.LongLength && sourceOffset + i < source.LongLength; i++)
        {
            await Assert.That(target[targetOffset + i]).IsEqualTo(source[sourceOffset + i]);
        }
    }

    private sealed class ReadOnlyLargeArrayStub : IReadOnlyLargeArray<long>
    {
        private readonly long[] _data;

        public ReadOnlyLargeArrayStub(IReadOnlyLargeArray<long> source)
        {
            _data = source.GetAll().ToArray();
        }

        public long Count => _data.LongLength;

        public long this[long index] => _data[index];

        public bool Contains(long item) => Contains(item, 0L, Count);

        public bool Contains(long item, long offset, long count)
        {
            StorageExtensions.CheckRange(offset, count, Count);
            for (long i = 0; i < count; i++)
            {
                if (_data[offset + i] == item)
                {
                    return true;
                }
            }
            return false;
        }

        public bool Contains<TComparer>(long item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<long>
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? (Count - actualOffset);
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);
            for (long i = 0; i < actualCount; i++)
            {
                if (comparer.Equals(_data[actualOffset + i], item))
                {
                    return true;
                }
            }
            return false;
        }

        public long BinarySearch<TComparer>(long item, TComparer comparer, long? offset = null, long? count = null) where TComparer : IComparer<long>
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? (Count - actualOffset);
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);
            long low = actualOffset;
            long high = actualOffset + actualCount - 1;
            while (low <= high)
            {
                long mid = low + ((high - low) / 2);
                int cmp = comparer.Compare(_data[mid], item);
                if (cmp == 0)
                {
                    return mid;
                }
                if (cmp < 0)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
            return ~low;
        }

        public long BinarySearch(long item, long? offset = null, long? count = null)
            => BinarySearch(item, Comparer<long>.Default, offset, count);

        public long IndexOf<TComparer>(long item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<long>
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? (Count - actualOffset);
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);
            for (long i = 0; i < actualCount; i++)
            {
                if (comparer.Equals(_data[actualOffset + i], item))
                {
                    return actualOffset + i;
                }
            }
            return -1;
        }

        public long LastIndexOf<TComparer>(long item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<long>
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? (Count - actualOffset);
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);
            for (long i = actualCount - 1; i >= 0; i--)
            {
                if (comparer.Equals(_data[actualOffset + i], item))
                {
                    return actualOffset + i;
                }
            }
            return -1;
        }

        public IEnumerator<long> GetEnumerator()
        {
            for (long i = 0; i < Count; i++)
            {
                yield return _data[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<long> GetAll()
        {
            for (long i = 0; i < Count; i++)
            {
                yield return _data[i];
            }
        }

        public IEnumerable<long> GetAll(long offset, long count)
        {
            StorageExtensions.CheckRange(offset, count, Count);
            for (long i = 0; i < count; i++)
            {
                yield return _data[offset + i];
            }
        }

        public long Get(long index) => _data[index];

        public long IndexOf(long item, long? offset = null, long? count = null)
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? (Count - actualOffset);
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);
            for (long i = 0; i < actualCount; i++)
            {
                if (_data[actualOffset + i] == item)
                {
                    return actualOffset + i;
                }
            }
            return -1;
        }

        public long LastIndexOf(long item, long? offset = null, long? count = null)
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? (Count - actualOffset);
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);
            for (long i = actualCount - 1; i >= 0; i--)
            {
                if (_data[actualOffset + i] == item)
                {
                    return actualOffset + i;
                }
            }
            return -1;
        }

        public void DoForEach(Action<long> action) => DoForEach(action, 0L, Count);

        public void DoForEach(Action<long> action, long offset, long count)
        {
            ArgumentNullException.ThrowIfNull(action);
            if (count == 0L) return;
            StorageExtensions.CheckRange(offset, count, Count);
            for (long i = 0; i < count; i++)
            {
                action(_data[offset + i]);
            }
        }

        public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<long> => DoForEach(ref action, 0L, Count);

        public void DoForEach<TAction>(ref TAction action, long offset, long count) where TAction : ILargeAction<long>
        {
            if (count == 0L) return;
            StorageExtensions.CheckRange(offset, count, Count);
            for (long i = 0; i < count; i++)
            {
                action.Invoke(_data[offset + i]);
            }
        }

        public void CopyTo(ILargeArray<long> target, long sourceOffset, long targetOffset, long count)
        {
            StorageExtensions.CheckRange(sourceOffset, count, Count);
            ArgumentNullException.ThrowIfNull(target);
            StorageExtensions.CheckRange(targetOffset, count, target.Count);
            for (long i = 0; i < count; i++)
            {
                target[targetOffset + i] = _data[sourceOffset + i];
            }
        }

        public void CopyTo(LargeSpan<long> target, long sourceOffset, long count)
        {
            StorageExtensions.CheckRange(sourceOffset, count, Count);
            StorageExtensions.CheckRange(0, count, target.Count);
            for (long i = 0; i < count; i++)
            {
                target[i] = _data[sourceOffset + i];
            }
        }

        public void CopyToArray(long[] target, long sourceOffset, int targetOffset, int count)
        {
            StorageExtensions.CheckRange(sourceOffset, count, Count);
            ArgumentNullException.ThrowIfNull(target);
            StorageExtensions.CheckRange(targetOffset, count, target.Length);
            for (int i = 0; i < count; i++)
            {
                target[targetOffset + i] = _data[sourceOffset + i];
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        public void CopyToSpan(Span<long> target, long sourceOffset, int count)
        {
            StorageExtensions.CheckRange(sourceOffset, count, Count);
            StorageExtensions.CheckRange(0, count, target.Length);
            for (int i = 0; i < count; i++)
            {
                target[i] = _data[sourceOffset + i];
            }
        }
#endif
    }

    private sealed class LargeArrayFacade : ILargeArray<long>
    {
        private readonly LargeArray<long> _inner;

        public LargeArrayFacade(long capacity)
        {
            _inner = new LargeArray<long>(capacity);
        }

        public long Count => _inner.Count;

        public long this[long index]
        {
            get => _inner[index];
            set => _inner[index] = value;
        }

        public bool Contains(long item) 
            => _inner.Contains(item);

        public bool Contains(long item, long offset, long count) 
            => _inner.Contains(item, offset, count);

        public bool Contains<TComparer>(long item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<long>
            => _inner.Contains(item, ref comparer, offset, count);

        public long BinarySearch(long item, long? offset = null, long? count = null)
            => _inner.BinarySearch(item, offset, count);

        public long BinarySearch<TComparer>(long item, TComparer comparer, long? offset = null, long? count = null) where TComparer : IComparer<long>
            => _inner.BinarySearch(item, comparer, offset, count);

        public IEnumerator<long> GetEnumerator() => _inner.GetAll().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<long> GetAll() => _inner.GetAll();

        public IEnumerable<long> GetAll(long offset, long count) => _inner.GetAll(offset, count);

        public long Get(long index) => _inner.Get(index);

        public long IndexOf(long item, long? offset = null, long? count = null) 
            => _inner.IndexOf(item, offset, count);

        public long IndexOf<TComparer>(long item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<long>
            => _inner.IndexOf(item, ref comparer, offset, count);

        public long LastIndexOf(long item, long? offset = null, long? count = null) 
            => _inner.LastIndexOf(item, offset, count);

        public long LastIndexOf<TComparer>(long item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<long>
            => _inner.LastIndexOf(item, ref comparer, offset, count);

        public void DoForEach(Action<long> action) => _inner.DoForEach(action);

        public void DoForEach(Action<long> action, long offset, long count) => _inner.DoForEach(action, offset, count);

        public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<long>
            => _inner.DoForEach(ref action);

        public void DoForEach<TAction>(ref TAction action, long offset, long count) where TAction : ILargeAction<long>
            => _inner.DoForEach(ref action, offset, count);

        public void CopyTo(ILargeArray<long> target, long sourceOffset, long targetOffset, long count)
            => _inner.CopyTo(target, sourceOffset, targetOffset, count);

        public void CopyTo(LargeSpan<long> target, long sourceOffset, long count)
            => _inner.CopyTo(target, sourceOffset, count);

        public void CopyToArray(long[] target, long sourceOffset, int targetOffset, int count)
            => _inner.CopyToArray(target, sourceOffset, targetOffset, count);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        public void CopyToSpan(Span<long> target, long sourceOffset, int count)
            => _inner.CopyToSpan(target, sourceOffset, count);
#endif

        public void Set(long index, long item) => _inner.Set(index, item);

        public void Sort<TComparer>(TComparer comparer, long? offset = null, long? count = null) where TComparer : IComparer<long>
            => _inner.Sort(comparer, offset, count);

        public void Swap(long leftIndex, long rightIndex) => _inner.Swap(leftIndex, rightIndex);

        public void CopyFrom(IReadOnlyLargeArray<long> source, long sourceOffset, long targetOffset, long count)
            => _inner.CopyFrom(source, sourceOffset, targetOffset, count);

        public void CopyFrom(ReadOnlyLargeSpan<long> source, long targetOffset, long count)
            => _inner.CopyFrom(source, targetOffset, count);

        public void CopyFromArray(long[] source, int sourceOffset, long targetOffset, int count)
            => _inner.CopyFromArray(source, sourceOffset, targetOffset, count);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        public void CopyFromSpan(ReadOnlySpan<long> source, long targetOffset, int count)
            => _inner.CopyFromSpan(source, targetOffset, count);
#endif
    }

    #endregion
}
