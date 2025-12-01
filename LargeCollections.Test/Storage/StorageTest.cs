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

using TUnit.Core;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LargeCollections.Test.Storage;

/// <summary>
/// Exhaustive tests for the StorageExtensions class, covering all internal storage manipulation logic.
/// These tests focus on boundary conditions related to the jagged array implementation,
/// using various capacities to stress-test chunking logic.
/// </summary>
public class StorageTest
{
    // Capacities chosen to test boundaries of the jagged array storage
    private static readonly long[] TestCapacities =
    [
        0L,
        1L,
        Constants.MaxStorageCapacity - 1L,
        Constants.MaxStorageCapacity,
        Constants.MaxStorageCapacity + 1L,
        Math.Min(2L * Constants.MaxStorageCapacity - 1L, Constants.MaxLargeCollectionCount),
        Math.Min(2L * Constants.MaxStorageCapacity, Constants.MaxLargeCollectionCount),
        Math.Min(2L * Constants.MaxStorageCapacity + 1L, Constants.MaxLargeCollectionCount),
        Math.Max(Constants.MaxLargeCollectionCount - 1L, 0L),
        Constants.MaxLargeCollectionCount,
    ];

    public static IEnumerable<long> CapacitiesProvider()
    {
        HashSet<long> seen = [];

        foreach (long baseCapacity in TestCapacities)
        {
            long[] candidates =
            [
                baseCapacity - 1L,
                baseCapacity,
                baseCapacity + 1L,
            ];

            foreach (long candidate in candidates)
            {
                if (candidate < 0L || candidate > Constants.MaxLargeCollectionCount)
                {
                    continue;
                }

                if (seen.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }
    }

    #region CheckRange & CheckIndex Tests

    [Test]
    [Arguments(100L, 0L, 10L)]
    [Arguments(100L, 90L, 10L)]
    [Arguments(100L, 0L, 100L)]
    [Arguments(0L, 0L, 0L)]
    public async Task CheckRange_ValidParameters_DoesNotThrow(long maxCount, long offset, long count)
    {
        Exception exception = null;
        try
        {
            StorageExtensions.CheckRange(offset, count, maxCount);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        await Assert.That(exception).IsNull();
    }

    [Test]
    [Arguments(100L, -1L, 10L)]  // Negative offset
    [Arguments(100L, 0L, -1L)]   // Negative count
    [Arguments(100L, 91L, 10L)]  // offset + count > maxCount
    [Arguments(100L, 101L, 0L)]  // offset > maxCount
    public async Task CheckRange_InvalidParameters_ThrowsArgumentException(long maxCount, long offset, long count)
    {
        await Assert.That(() => StorageExtensions.CheckRange(offset, count, maxCount)).Throws<Exception>();
    }

    [Test]
    public async Task CheckRange_MaxCountAboveLimit_ThrowsArgumentException()
    {
        long maxCount = Constants.MaxLargeCollectionCount + 1L;
        await Assert.That(() => StorageExtensions.CheckRange(0L, 1L, maxCount)).Throws<Exception>();
    }

    [Test]
    [Arguments(100L, 0L)]
    [Arguments(100L, 99L)]
    [Arguments(1L, 0L)]
    public async Task CheckIndex_ValidParameters_DoesNotThrow(long count, long index)
    {
        Exception exception = null;
        try
        {
            StorageExtensions.CheckIndex(index, count);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        await Assert.That(exception).IsNull();
    }

    [Test]
    [Arguments(100L, -1L)]  // Negative index
    [Arguments(100L, 100L)] // index == count
    [Arguments(0L, 0L)]     // index on empty collection
    public async Task CheckIndex_InvalidParameters_ThrowsIndexOutOfRangeException(long count, long index)
    {
        await Assert.That(() => StorageExtensions.CheckIndex(index, count)).Throws<Exception>();
    }

    [Test]
    public async Task CheckIndex_IndexAboveGlobalMax_ThrowsIndexOutOfRangeException()
    {
        long count = Constants.MaxLargeCollectionCount;
        long index = Constants.MaxLargeCollectionCount + 1L;
        await Assert.That(() => StorageExtensions.CheckIndex(index, count)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageCheckRange_OnArray_ValidAndInvalid(long capacity)
    {
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        // valid
        Exception noThrow = null;
        try
        {
            long validOffset = capacity == 0L ? 0L : capacity / 3L;
            long maxCount = storage.StorageGetCount();
            long validCount = maxCount - validOffset;
            storage.StorageCheckRange(validOffset, validCount);
        }
        catch (Exception ex)
        {
            noThrow = ex;
        }
        await Assert.That(noThrow).IsNull();
        // invalid offset
        await Assert.That(() => storage.StorageCheckRange(-1L, 1L)).Throws<Exception>();
        // invalid count: request more than available
        await Assert.That(() => storage.StorageCheckRange(0L, capacity + 1L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageCheckIndex_OnArray_ValidAndInvalid(long capacity)
    {
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        // valid
        Exception idxEx1 = null;
        Exception idxEx2 = null;
        if (capacity > 0)
        {
            try { storage.StorageCheckIndex(0L); } catch (Exception ex) { idxEx1 = ex; }
        }
        if (capacity > 0)
        {
            try { storage.StorageCheckIndex(capacity - 1L); } catch (Exception ex) { idxEx2 = ex; }
        }
        await Assert.That(idxEx1).IsNull();
        await Assert.That(idxEx2).IsNull();
        // invalid
        await Assert.That(() => storage.StorageCheckIndex(capacity)).Throws<Exception>();
        await Assert.That(() => storage.StorageCheckIndex(-1L)).Throws<Exception>();
    }

    #endregion

    #region StorageCreate, StorageGetCount, StorageGetIndex Tests

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageCreate_And_StorageGetCount_ReturnCorrectValues(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            await Assert.That(() => StorageExtensions.StorageCreate<int>(capacity)).Throws<Exception>();
            return;
        }

        // Act
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        long count = storage.StorageGetCount();

        // Assert
        await Assert.That(count).IsEqualTo(capacity);

        if (capacity == 0L)
        {
            await Assert.That(storage.LongLength).IsEqualTo(0L);
            return;
        }

        // Verify structure
        (int expectedStorageIndex, int expectedItemIndex) = StorageExtensions.StorageGetIndex(capacity - 1L);
        int expectedStorageCount = expectedStorageIndex + 1;
        int expectedLastSegmentLength = expectedItemIndex + 1;

        await Assert.That(storage.Length).IsEqualTo(expectedStorageCount);

        for (int i = 0; i < expectedStorageCount - 1; i++)
        {
            await Assert.That(storage[i].Length).IsEqualTo((int)Constants.MaxStorageCapacity);
        }

        await Assert.That(storage[expectedStorageCount - 1].Length).IsEqualTo(expectedLastSegmentLength);
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageGetCount_WithOffset_ReturnsCapacityMinusOffset(long capacity)
    {
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        long offset = capacity == 0L ? 0L : capacity / 3L;
        long count = storage.StorageGetCount(offset);
        await Assert.That(count).IsEqualTo(Math.Max(0L, capacity - offset));
    }

    [Test]
    [Arguments(0L, 0, 0)]
    [Arguments(1L, 0, 1)]
    [Arguments(Constants.MaxStorageCapacity - 1L, 0, (int)Constants.MaxStorageCapacity - 1)]
    [Arguments(Constants.MaxStorageCapacity, 1, 0)]
    [Arguments(Constants.MaxStorageCapacity + 1L, 1, 1)]
    public async Task StorageGetIndex_ReturnsCorrectTuple(long index, int expectedStorageIndex, int expectedItemIndex)
    {
        // Act
        (int storageIndex, int itemIndex) = StorageExtensions.StorageGetIndex(index);

        // Assert
        await Assert.That(storageIndex).IsEqualTo(expectedStorageIndex);
        await Assert.That(itemIndex).IsEqualTo(expectedItemIndex);
    }

    [Test]
    public async Task StorageCreate_ExactSegmentMultiple_AllocatesFullSegments()
    {
        long segmentSize = Constants.MaxStorageCapacity;
        long capacity = Math.Min(segmentSize, Constants.MaxLargeCollectionCount);

        if (capacity <= 0L)
        {
            return;
        }

        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);

        int expectedSegmentCount = (int)((capacity + segmentSize - 1L) / segmentSize);
        await Assert.That(storage.Length).IsEqualTo(expectedSegmentCount);

        foreach (int[] segment in storage)
        {
            await Assert.That(segment.Length).IsEqualTo((int)segmentSize);
        }
    }

    [Test]
    public async Task StorageCreate_MaximumCapacity_AllocatesFullSegments()
    {
        long maxCapacity = Constants.MaxLargeCollectionCount;

        if (maxCapacity == 0L || maxCapacity > 4096L)
        {
            return;
        }

        int[][] storage = StorageExtensions.StorageCreate<int>(maxCapacity);

        (int expectedStorageIndex, int expectedItemIndex) = StorageExtensions.StorageGetIndex(maxCapacity - 1L);
        int expectedStorageCount = expectedStorageIndex + 1;
        int expectedSegmentLength = expectedItemIndex + 1;

        await Assert.That(storage.Length).IsEqualTo(expectedStorageCount);

        for (int i = 0; i < expectedStorageCount - 1; i++)
        {
            await Assert.That(storage[i].Length).IsEqualTo((int)Constants.MaxStorageCapacity);
        }

        await Assert.That(storage[^1].Length).IsEqualTo(expectedSegmentLength);
    }

    #endregion

    #region StorageGet, StorageSet, StorageGetRef Tests

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageGet_And_StorageSet_WorkCorrectly(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount) return;

        // Arrange
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        long[] indicesToTest = [0, capacity / 2, capacity - 1];

        foreach (long index in indicesToTest)
        {
            int valueToSet = (int)index + 123;

            // Act
            storage.StorageSet(index, valueToSet);
            int retrievedValue = storage.StorageGet(index);

            // Assert
            await Assert.That(retrievedValue).IsEqualTo(valueToSet);
        }
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageGetRef_AllowsModification(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount) return;

        // Arrange
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        long index = capacity / 2;
        int initialValue = 42;
        int modifiedValue = 99;

        storage.StorageSet(index, initialValue);

        // Act
        ref int valueRef = ref storage.StorageGetRef(index);
        valueRef = modifiedValue;

        // Assert
        int finalValue = storage.StorageGet(index);
        await Assert.That(finalValue).IsEqualTo(modifiedValue);
    }

    #endregion

    #region StorageGetAll & StorageDoForEach Tests

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageGetAll_ReturnsAllItemsInRange(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount) return;

        // Arrange
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++)
        {
            storage.StorageSet(i, (int)i);
        }

        long offset = capacity / 4;
        long count = capacity / 2;

        // Act
        List<int> result = storage.StorageGetAll(offset, count).ToList();

        // Assert
        await Assert.That(result.Count).IsEqualTo((int)count);
        for (int i = 0; i < count; i++)
        {
            await Assert.That(result[i]).IsEqualTo((int)(offset + i));
        }
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageGetAll_CountZero_YieldsEmpty(long capacity)
    {
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        List<int> result = storage.StorageGetAll(0L, 0L).ToList();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageDoForEach_ExecutesActionOnAllItems(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount) return;

        // Arrange
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++)
        {
            storage.StorageSet(i, (int)i);
        }

        long sum = 0;
        object dummy = null; // Dummy ref objects

        // Act
        storage.StorageDoForEach<int, object, object>((ref int item, ref object d1, ref object d2) => { sum += item; }, 0, capacity, ref dummy, ref dummy);

        // Assert
        long expectedSum = capacity * (capacity - 1) / 2;
        await Assert.That(sum).IsEqualTo(expectedSum);
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageDoForEach_NullAction_NoOp(long capacity)
    {
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++)
        {
            storage.StorageSet(i, (int)i);
        }
        object x = null;
        object y = null;
        storage.StorageDoForEach<int, object, object>(null, 0L, capacity, ref x, ref y);
        // verify unchanged
        for (long i = 0; i < capacity; i++)
        {
            await Assert.That(storage.StorageGet(i)).IsEqualTo((int)i);
        }
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageDoForEach_SubRange_Works(long capacity)
    {
        if (capacity <= 0) return;
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++)
        {
            storage.StorageSet(i, 1);
        }
        long sum = 0;
        object u = null;
        object v = null;
        long offset = capacity / 4L;
        long count = Math.Max(1L, Math.Min(capacity - offset, capacity / 2L));
        storage.StorageDoForEach<int, object, object>((ref int item, ref object a, ref object b) => { sum += item; }, offset, count, ref u, ref v);
        await Assert.That(sum).IsEqualTo(count);
    }

    #endregion

    #region StorageSwap Tests

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageSwap_SwapsElementsCorrectly(long capacity)
    {
        if (capacity < 2 || capacity > Constants.MaxLargeCollectionCount) return;

        // Arrange
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++)
        {
            storage.StorageSet(i, (int)i);
        }

        long index1 = capacity / 4;
        long index2 = 3 * capacity / 4;

        int value1 = storage.StorageGet(index1);
        int value2 = storage.StorageGet(index2);

        // Act
        storage.StorageSwap(index1, index2);

        // Assert
        await Assert.That(storage.StorageGet(index1)).IsEqualTo(value2);
        await Assert.That(storage.StorageGet(index2)).IsEqualTo(value1);
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageSwap_SameIndex_NoChange(long capacity)
    {
        if (capacity <= 0) return;
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        long idx = capacity / 2L;
        storage.StorageSet(idx, 123);
        storage.StorageSwap(idx, idx);
        await Assert.That(storage.StorageGet(idx)).IsEqualTo(123);
    }

    [Test]
    public async Task StorageSwap_AcrossBoundary_Works()
    {
        long capacity = Constants.MaxStorageCapacity + 2L;
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        storage.StorageSet(Constants.MaxStorageCapacity - 1L, 10);
        storage.StorageSet(Constants.MaxStorageCapacity, 20);
        storage.StorageSwap(Constants.MaxStorageCapacity - 1L, Constants.MaxStorageCapacity);
        await Assert.That(storage.StorageGet(Constants.MaxStorageCapacity - 1L)).IsEqualTo(20);
        await Assert.That(storage.StorageGet(Constants.MaxStorageCapacity)).IsEqualTo(10);
    }

    #endregion

    #region Search Tests (IndexOf, LastIndexOf, Contains)

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageIndexOf_And_Contains_FindsItems(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount) return;

        // Arrange
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++)
        {
            storage.StorageSet(i, (int)(i % 10)); // Repeating pattern 0-9
        }

        long indexToFind = capacity / 2;
        int itemToFind = storage.StorageGet(indexToFind);

        // Act
        long foundIndex = storage.StorageIndexOf(itemToFind, 0, capacity, (a, b) => a == b);
        bool contains = storage.Contains(itemToFind, 0, capacity, (a, b) => a == b);
        bool notContains = storage.Contains(999, 0, capacity, (a, b) => a == b);

        // Assert
        await Assert.That(foundIndex).IsLessThanOrEqualTo(indexToFind); // Should find first or earlier occurrence
        await Assert.That(contains).IsTrue();
        await Assert.That(notContains).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageIndexOf_CountZero_ReturnsMinusOne(long capacity)
    {
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        long idx = storage.StorageIndexOf(5, 0L, 0L, (a, b) => a == b);
        await Assert.That(idx).IsEqualTo(-1L);
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageContains_RangeLimited_Works(long capacity)
    {
        if (capacity <= 0) return;
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++) storage.StorageSet(i, (int)i);
        long mid = capacity / 2L;
        bool inRange = storage.Contains((int)mid, mid, Math.Min(1L, capacity - mid), (a, b) => a == b);
        bool outsideRange = storage.Contains((int)mid, Math.Min(capacity, mid + 1L), Math.Max(0L, capacity - (mid + 1L)), (a, b) => a == b);
        await Assert.That(inRange).IsTrue();
        await Assert.That(outsideRange).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageLastIndexOf_FindsLastItem(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount) return;

        // Arrange
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++)
        {
            storage.StorageSet(i, (int)(i % 10)); // Repeating pattern 0-9
        }

        long indexToFind = capacity / 2;
        int itemToFind = storage.StorageGet(indexToFind);

        // Act
        long foundIndex = storage.StorageLastIndexOf(itemToFind, 0, capacity, (a, b) => a == b);

        // Assert
        await Assert.That(foundIndex).IsGreaterThanOrEqualTo(indexToFind); // Should find last or later occurrence
    }

    #endregion

    #region Sort & BinarySearch Tests

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageSort_And_BinarySearch_WorkCorrectly(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount) return;

        // Arrange
        Random random = new Random(42);
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++)
        {
            storage.StorageSet(i, random.Next());
        }

        long indexToFind = capacity / 2;
        int itemToFind = storage.StorageGet(indexToFind);

        // Act
        storage.StorageSort((a, b) => a.CompareTo(b), 0, capacity);
        long foundIndex = storage.StorageBinarySearch(itemToFind, (a, b) => a.CompareTo(b), 0, capacity);

        // Assert
        for (long i = 1; i < capacity; i++) // Verify sorted
        {
            await Assert.That(storage.StorageGet(i)).IsGreaterThanOrEqualTo(storage.StorageGet(i - 1));
        }
        await Assert.That(foundIndex).IsGreaterThanOrEqualTo(0); // Item should be found
        await Assert.That(storage.StorageGet(foundIndex)).IsEqualTo(itemToFind);
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageSort_NullComparer_ThrowsArgumentNullException(long capacity)
    {
        if (capacity <= 1) return; // Sort with count <= 1 returns early without checking comparer
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        // Create a deterministic pattern
        for (long i = 0; i < capacity; i++) storage.StorageSet(i, (int)((capacity - i) % 997));
        
        // Should throw ArgumentNullException when comparer is null
        await Assert.That(() => storage.StorageSort(null, 0L, capacity)).Throws<ArgumentNullException>();
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageBinarySearch_NotFoundOrNullComparer(long capacity)
    {
        if (capacity < 3) return;
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++) storage.StorageSet(i, (int)i);
        long notFound = storage.StorageBinarySearch((int)(capacity + 89L), (a, b) => a.CompareTo(b), 0L, capacity);
        await Assert.That(notFound).IsEqualTo(-1L);
        
        // Should throw ArgumentNullException when comparer is null
        await Assert.That(() => storage.StorageBinarySearch((int)(capacity / 2L), null, 0L, capacity)).Throws<ArgumentNullException>();
        
        long target = capacity / 2L;
        long negCount = storage.StorageBinarySearch((int)target, (a, b) => a.CompareTo(b), 2L, -1L);
        await Assert.That(negCount).IsEqualTo(target);
    }

    #endregion

    #region Copy & Resize Tests

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageCopyTo_And_CopyFrom_WorkCorrectly(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount) return;

        // Arrange
        int[][] source = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++)
        {
            source.StorageSet(i, (int)i);
        }
        int[][] target = StorageExtensions.StorageCreate<int>(capacity);

        // Act
        source.StorageCopyTo(target, 0, 0, capacity);

        // Assert
        for (long i = 0; i < capacity; i++)
        {
            await Assert.That(target.StorageGet(i)).IsEqualTo((int)i);
        }

        // Test CopyFrom
        int[][] newSource = StorageExtensions.StorageCreate<int>(capacity);
        target.StorageCopyFrom(newSource, 0, 0, capacity); // Should be all 0s
        for (long i = 0; i < capacity; i++)
        {
            await Assert.That(target.StorageGet(i)).IsEqualTo(0);
        }
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageCopyTo_InvalidTarget_Throws(long capacity)
    {
        if (capacity <= 0) return;
        int[][] source = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++) source.StorageSet(i, (int)i);
        int[][] target = StorageExtensions.StorageCreate<int>(capacity);
        long srcOffset = capacity == 0 ? 0L : Math.Max(0L, capacity - 1L);
        long dstOffset = capacity; // start beyond last valid index

        // Internal storage helpers omit guard checks for performance; the underlying array access triggers the index error.
        await Assert.That(() => Task.Run(() => source.StorageCopyTo(target, srcOffset, dstOffset, 2L))).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageCopyToArray_ValidAndInvalidCases(long capacity)
    {
        if (capacity <= 0) return;
        int[][] source = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++) source.StorageSet(i, (int)i);
        int validCount = (int)Math.Min(5L, capacity);
        int[] target = new int[validCount];
        long offset = Math.Max(0L, capacity - validCount);
        source.StorageCopyToArray(target, offset, 0, validCount);
        for (int i = 0; i < validCount; i++)
        {
            await Assert.That(target[i]).IsEqualTo((int)(offset + i));
        }
        // invalid: place copy starting at target.Length to guarantee overflow for any count >= 1
        await Assert.That(() => Task.Run(() => source.StorageCopyToArray(target, 0L, target.Length, 1))).Throws<Exception>();
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageCopyToSpan_ValidAndInvalidCases(long capacity)
    {
        if (capacity <= 0) return;
        int[][] source = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++) source.StorageSet(i, (int)i);
        int validCount = (int)Math.Min(5L, capacity);
        int[] targetBuffer = new int[validCount];
        Span<int> span = targetBuffer.AsSpan();
        long offset = Math.Max(0L, capacity - validCount);
        source.StorageCopyToSpan(span, offset, validCount);
        for (int i = 0; i < validCount; i++)
        {
            await Assert.That(targetBuffer[i]).IsEqualTo((int)(offset + i));
        }
        int[] smallBuffer = new int[Math.Max(0, validCount - 1)];
        Span<int> smallSpan = smallBuffer.AsSpan();
        Exception copyToSpanEx = null;
        try
        {
            source.StorageCopyToSpan(smallSpan, 0L, validCount);
        }
        catch (Exception ex)
        {
            copyToSpanEx = ex;
        }
        await Assert.That(copyToSpanEx is ArgumentException).IsTrue();
    }
#endif

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageCopyFromArray_ValidAndInvalidCases(long capacity)
    {
        if (capacity <= 0) return;
        int[][] target = StorageExtensions.StorageCreate<int>(capacity);
        int validCount = (int)Math.Min(5L, capacity);
        int[] source = new int[validCount];
        for (int i = 0; i < validCount; i++) source[i] = i + 1;
        long dstOffset = Math.Max(0L, capacity - validCount);
        target.StorageCopyFromArray(source, 0, dstOffset, validCount);
        for (int i = 0; i < validCount; i++)
        {
            await Assert.That(target.StorageGet(dstOffset + i)).IsEqualTo(source[i]);
        }
        // invalid: choose a destination offset that cannot fit 'validCount' items
        long invalidDstOffset = Math.Max(0L, capacity - validCount + 1L);
        await Assert.That(() => Task.Run(() => target.StorageCopyFromArray(source, 0, invalidDstOffset, validCount))).Throws<Exception>();
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageCopyFromSpan_ValidAndInvalidCases(long capacity)
    {
        if (capacity <= 0) return;
        int[][] target = StorageExtensions.StorageCreate<int>(capacity);
        int validCount = (int)Math.Min(5L, capacity);
        int[] sourceArr = new int[validCount];
        for (int i = 0; i < validCount; i++) sourceArr[i] = 9 - i;
        ReadOnlySpan<int> span = new ReadOnlySpan<int>(sourceArr);
        long dstOffset = Math.Max(0L, capacity - validCount);
        target.StorageCopyFromSpan(span, dstOffset, validCount);
        for (int i = 0; i < validCount; i++)
        {
            await Assert.That(target.StorageGet(dstOffset + i)).IsEqualTo(sourceArr[i]);
        }
        ReadOnlySpan<int> small = new ReadOnlySpan<int>(new int[Math.Max(0, validCount - 1)]);
        Exception copyFromSpanEx = null;
        try
        {
            target.StorageCopyFromSpan(small, 0L, validCount);
        }
        catch (Exception ex)
        {
            copyFromSpanEx = ex;
        }
        await Assert.That(copyFromSpanEx is ArgumentException).IsTrue();
    }
#endif

    [Test]
    public async Task StorageResize_ToZero_CreatesEmptyArray()
    {
        long initialCapacity = Math.Min(Constants.MaxStorageCapacity, Constants.MaxLargeCollectionCount);
        int[][] storage = StorageExtensions.StorageCreate<int>(initialCapacity);

        StorageExtensions.StorageResize(ref storage, 0L);

        await Assert.That(storage.LongLength).IsEqualTo(0L);
        bool isEmptyReference = ReferenceEquals(storage, Array.Empty<int[]>());
        await Assert.That(isEmptyReference).IsTrue();
    }

    [Test]
    public async Task StorageResize_ExactSegmentMultiple_AllocatesFullSegments()
    {
        long segmentSize = Constants.MaxStorageCapacity;
        long targetCapacity = Math.Min(segmentSize, Constants.MaxLargeCollectionCount);

        if (targetCapacity <= 0L)
        {
            return;
        }

        int[][] storage = StorageExtensions.StorageCreate<int>(1L);

        StorageExtensions.StorageResize(ref storage, targetCapacity);

        int expectedSegmentCount = (int)((targetCapacity + segmentSize - 1L) / segmentSize);
        await Assert.That(storage.Length).IsEqualTo(expectedSegmentCount);

        foreach (int[] segment in storage)
        {
            await Assert.That(segment.Length).IsEqualTo((int)segmentSize);
        }
    }

    [Test]
    public async Task StorageResize_MaximumCapacity_AllocatesFullSegments()
    {
        long maxCapacity = Constants.MaxLargeCollectionCount;

        if (maxCapacity == 0L || maxCapacity > 4096L)
        {
            return;
        }

        int[][] storage = StorageExtensions.StorageCreate<int>(1L);

        StorageExtensions.StorageResize(ref storage, maxCapacity);

        (int expectedStorageIndex, int expectedItemIndex) = StorageExtensions.StorageGetIndex(maxCapacity - 1L);
        int expectedStorageCount = expectedStorageIndex + 1;
        int expectedSegmentLength = expectedItemIndex + 1;

        await Assert.That(storage.Length).IsEqualTo(expectedStorageCount);

        for (int i = 0; i < expectedStorageCount - 1; i++)
        {
            await Assert.That(storage[i].Length).IsEqualTo((int)Constants.MaxStorageCapacity);
        }

        await Assert.That(storage[^1].Length).IsEqualTo(expectedSegmentLength);
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageResize_GrowsAndShrinksCorrectly(long capacity)
    {
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount) return;

        // Arrange
        int[][] storage = StorageExtensions.StorageCreate<int>(capacity);
        for (long i = 0; i < capacity; i++)
        {
            storage.StorageSet(i, (int)i);
        }

        // Act & Assert - Grow
        long grownCapacity = capacity + 10;
        if (grownCapacity <= Constants.MaxLargeCollectionCount)
        {
            StorageExtensions.StorageResize(ref storage, grownCapacity);
            await Assert.That(storage.StorageGetCount()).IsEqualTo(grownCapacity);
            for (long i = 0; i < capacity; i++) // Check preserved data
            {
                await Assert.That(storage.StorageGet(i)).IsEqualTo((int)i);
            }
        }

        // Act & Assert - Shrink
        long shrunkCapacity = Math.Max(0, capacity / 2);
        StorageExtensions.StorageResize(ref storage, shrunkCapacity);
        await Assert.That(storage.StorageGetCount()).IsEqualTo(shrunkCapacity);
        for (long i = 0; i < shrunkCapacity; i++) // Check preserved data
        {
            await Assert.That(storage.StorageGet(i)).IsEqualTo((int)i);
        }
    }

    [Test]
    public async Task StorageResize_Invalid_Throws()
    {
        int[][] storage = StorageExtensions.StorageCreate<int>(0L);
        await Assert.That(() => Task.Run(() => StorageExtensions.StorageResize(ref storage, -1L))).Throws<Exception>();
        await Assert.That(() => Task.Run(() => StorageExtensions.StorageResize(ref storage, Constants.MaxLargeCollectionCount + 1L))).Throws<Exception>();
    }

    #endregion

    #region Stream Tests

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageWriteToStream_And_ReadFromStream_WorkCorrectly(long capacity)
    {
        if (capacity <= 0 || capacity > Constants.MaxLargeCollectionCount) return;

        // Arrange
        byte[][] source = StorageExtensions.StorageCreate<byte>(capacity);
        for (long i = 0; i < capacity; i++)
        {
            source.StorageSet(i, (byte)(i % 256));
        }
        byte[][] target = StorageExtensions.StorageCreate<byte>(capacity);
        using MemoryStream stream = new MemoryStream();

        // Act
        source.StorageWriteToStream(stream, 0, capacity);
        stream.Position = 0; // Reset for reading
        long bytesRead = target.StorageReadFromStream(stream, 0, capacity);

        // Assert
        await Assert.That(bytesRead).IsEqualTo(capacity);
        for (long i = 0; i < capacity; i++)
        {
            await Assert.That(target.StorageGet(i)).IsEqualTo((byte)(i % 256));
        }
    }

    [Test]
    [MethodDataSource(nameof(CapacitiesProvider))]
    public async Task StorageWriteToStream_WithOffsetAndPartialRead(long capacity)
    {
        if (capacity <= 0) return;
        byte[][] source = StorageExtensions.StorageCreate<byte>(capacity);
        for (long i = 0; i < capacity; i++) source.StorageSet(i, (byte)((i + 1) % 256));
        using MemoryStream stream = new MemoryStream();
        long writeOffset = capacity >= 6 ? 5L : 0L;
        long writeCount = Math.Min(10L, Math.Max(0L, capacity - writeOffset));
        // write only a subset
        source.StorageWriteToStream(stream, writeOffset, writeCount);
        stream.Position = 0;
        byte[][] target = StorageExtensions.StorageCreate<byte>(capacity);
        long readOffset = capacity >= 3 ? 2L : 0L;
        long requested = Math.Max(0L, capacity - readOffset);
        long read = target.StorageReadFromStream(stream, readOffset, requested); // request more than available -> should read writeCount
        await Assert.That(read).IsEqualTo(writeCount);
        for (long i = 0; i < writeCount; i++)
        {
            await Assert.That(target.StorageGet(readOffset + i)).IsEqualTo((byte)(((writeOffset + i) + 1) % 256));
        }
    }

    #endregion

    #region Overlapping Copy Tests

    [Test]
    public async Task StorageCopyTo_OverlappingForward_PreservesData()
    {
        // Test copying within the same array where target > source (forward overlap)
        // This should copy backwards to prevent data corruption
        long[][] storage = StorageExtensions.StorageCreate<long>(20);
        for (long i = 0; i < 20; i++) storage.StorageSet(i, i * 10);

        // Copy positions 0-9 to positions 5-14 (overlapping region: 5-9)
        storage.StorageCopyTo(storage, 0L, 5L, 10L);

        // Expected: positions 5-14 should contain original 0-9 values (0, 10, 20, 30, 40, 50, 60, 70, 80, 90)
        await Assert.That(storage.StorageGet(5L)).IsEqualTo(0L);
        await Assert.That(storage.StorageGet(6L)).IsEqualTo(10L);
        await Assert.That(storage.StorageGet(7L)).IsEqualTo(20L);
        await Assert.That(storage.StorageGet(8L)).IsEqualTo(30L);
        await Assert.That(storage.StorageGet(9L)).IsEqualTo(40L);
        await Assert.That(storage.StorageGet(10L)).IsEqualTo(50L);
        await Assert.That(storage.StorageGet(11L)).IsEqualTo(60L);
        await Assert.That(storage.StorageGet(12L)).IsEqualTo(70L);
        await Assert.That(storage.StorageGet(13L)).IsEqualTo(80L);
        await Assert.That(storage.StorageGet(14L)).IsEqualTo(90L);

        // Original positions 0-4 should be unchanged
        await Assert.That(storage.StorageGet(0L)).IsEqualTo(0L);
        await Assert.That(storage.StorageGet(1L)).IsEqualTo(10L);
        await Assert.That(storage.StorageGet(2L)).IsEqualTo(20L);
        await Assert.That(storage.StorageGet(3L)).IsEqualTo(30L);
        await Assert.That(storage.StorageGet(4L)).IsEqualTo(40L);
    }

    [Test]
    public async Task StorageCopyTo_OverlappingBackward_PreservesData()
    {
        // Test copying within the same array where target < source (backward overlap)
        // Standard forward copy should work correctly
        long[][] storage = StorageExtensions.StorageCreate<long>(20);
        for (long i = 0; i < 20; i++) storage.StorageSet(i, i * 10);

        // Copy positions 5-14 to positions 0-9 (overlapping region: 5-9)
        storage.StorageCopyTo(storage, 5L, 0L, 10L);

        // Expected: positions 0-9 should contain original 5-14 values (50, 60, 70, 80, 90, 100, 110, 120, 130, 140)
        await Assert.That(storage.StorageGet(0L)).IsEqualTo(50L);
        await Assert.That(storage.StorageGet(1L)).IsEqualTo(60L);
        await Assert.That(storage.StorageGet(2L)).IsEqualTo(70L);
        await Assert.That(storage.StorageGet(3L)).IsEqualTo(80L);
        await Assert.That(storage.StorageGet(4L)).IsEqualTo(90L);
        await Assert.That(storage.StorageGet(5L)).IsEqualTo(100L);
        await Assert.That(storage.StorageGet(6L)).IsEqualTo(110L);
        await Assert.That(storage.StorageGet(7L)).IsEqualTo(120L);
        await Assert.That(storage.StorageGet(8L)).IsEqualTo(130L);
        await Assert.That(storage.StorageGet(9L)).IsEqualTo(140L);
    }

    [Test]
    public async Task StorageCopyTo_NonOverlapping_WorksCorrectly()
    {
        // Test copying within the same array with no overlap
        long[][] storage = StorageExtensions.StorageCreate<long>(20);
        for (long i = 0; i < 20; i++) storage.StorageSet(i, i * 10);

        // Copy positions 0-4 to positions 15-19 (no overlap)
        storage.StorageCopyTo(storage, 0L, 15L, 5L);

        // Expected: positions 15-19 should contain original 0-4 values
        await Assert.That(storage.StorageGet(15L)).IsEqualTo(0L);
        await Assert.That(storage.StorageGet(16L)).IsEqualTo(10L);
        await Assert.That(storage.StorageGet(17L)).IsEqualTo(20L);
        await Assert.That(storage.StorageGet(18L)).IsEqualTo(30L);
        await Assert.That(storage.StorageGet(19L)).IsEqualTo(40L);

        // Original positions should be unchanged
        await Assert.That(storage.StorageGet(0L)).IsEqualTo(0L);
        await Assert.That(storage.StorageGet(1L)).IsEqualTo(10L);
    }

    #endregion
}
