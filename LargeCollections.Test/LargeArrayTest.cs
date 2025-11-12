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
        await Assert.That(array.Contains(existing, static (a, b) => a == b)).IsEqualTo(capacity > 0);
        await Assert.That(array.Contains(missing, static (a, b) => a == b)).IsFalse();

        long offset = Math.Min(1, Math.Max(0, capacity - 1));
        long length = Math.Max(0, capacity - offset);
        bool expectedInRange = capacity > 0 && length > 0 && existing >= offset && existing < offset + length;
        await Assert.That(array.Contains(existing, offset, length)).IsEqualTo(expectedInRange);
        await Assert.That(array.Contains(missing, offset, length)).IsFalse();
        await Assert.That(array.Contains(existing, offset, length, static (a, b) => a == b)).IsEqualTo(expectedInRange);
        await Assert.That(array.Contains(missing, offset, length, static (a, b) => a == b)).IsFalse();

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
            await Assert.That(array.IndexOf(marker, static (a, b) => a == b)).IsEqualTo(baseIndex);
            await Assert.That(array.LastIndexOf(marker)).IsEqualTo(baseIndex + (baseIndex + 1 < capacity ? 1 : 0));
            await Assert.That(array.LastIndexOf(marker, static (a, b) => a == b)).IsEqualTo(baseIndex + (baseIndex + 1 < capacity ? 1 : 0));

            long offset = baseIndex;
            long length = Math.Max(1, Math.Min(2, capacity - offset));
            await Assert.That(array.IndexOf(marker, offset, length)).IsEqualTo(offset);
            await Assert.That(array.IndexOf(marker, offset, length, static (a, b) => a == b)).IsEqualTo(offset);
            await Assert.That(array.LastIndexOf(marker, offset, length)).IsEqualTo(offset + Math.Min(1, length - 1));
            await Assert.That(array.LastIndexOf(marker, offset, length, static (a, b) => a == b)).IsEqualTo(offset + Math.Min(1, length - 1));
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

        long result = array.BinarySearch(existing, static (a, b) => a.CompareTo(b));
        if (capacity > 0)
        {
            await Assert.That(result).IsEqualTo(Math.Max(0, capacity / 2));
        }
        else
        {
            await Assert.That(result).IsEqualTo(~0L);
        }

        long missingResult = array.BinarySearch(missing, static (a, b) => a.CompareTo(b));
        await Assert.That(missingResult).IsEqualTo(-1L);

        long offset = Math.Min(1, Math.Max(0, capacity - 1));
        long length = Math.Max(0, capacity - offset);
        if (length > 0)
        {
            long value = array[offset + length / 2];
            long rangeResult = array.BinarySearch(value, static (a, b) => a.CompareTo(b), offset, length);
            await Assert.That(rangeResult).IsEqualTo(offset + length / 2);
        }

        await Assert.That(() => array.BinarySearch(0, static (a, b) => 0, -1, 1)).Throws<Exception>();
        await Assert.That(() => array.BinarySearch(0, static (a, b) => 0, 0, capacity + 1)).Throws<Exception>();
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

        LargeArray<long> userDataArray = CreateSequentialArray(capacity);
        long userDataSum = 0;
        userDataArray.DoForEach((long item, ref long data) => data += item, ref userDataSum);
        await Assert.That(userDataSum).IsEqualTo(userDataArray.GetAll().Sum());

        LargeArray<long> userDataRangeArray = CreateSequentialArray(capacity);
        long userDataRangeSum = 0;
        userDataRangeArray.DoForEach((long item, ref long data) => data += item, offset, length, ref userDataRangeSum);
        await Assert.That(userDataRangeSum).IsEqualTo(userDataRangeArray.GetAll(offset, length).Sum());

        LargeArray<long> refArray = CreateSequentialArray(capacity);
        if (refArray.Count > 0)
        {
            long original = refArray[0];
            refArray.DoForEach((ref long item) => item++);
            await Assert.That(refArray[0]).IsEqualTo(original + 1);
        }
        else
        {
            refArray.DoForEach((ref long item) => item++);
        }

        LargeArray<long> refRangeArray = CreateSequentialArray(capacity);
        if (length > 0)
        {
            long original = refRangeArray[offset];
            refRangeArray.DoForEach((ref long item) => item++, offset, length);
            await Assert.That(refRangeArray[offset]).IsEqualTo(original + 1);
        }
        else
        {
            refRangeArray.DoForEach((ref long item) => item++, offset, length);
        }

        LargeArray<long> refUserDataArray = CreateSequentialArray(capacity);
        long delta = 5;
        if (refUserDataArray.Count > 0)
        {
            long original = refUserDataArray[0];
            refUserDataArray.DoForEach((ref long item, ref long add) => item += add, ref delta);
            await Assert.That(refUserDataArray[0]).IsEqualTo(original + delta);
        }
        else
        {
            refUserDataArray.DoForEach((ref long item, ref long add) => item += add, ref delta);
        }

        LargeArray<long> refUserDataRangeArray = CreateSequentialArray(capacity);
        long rangeDelta = 7;
        if (length > 0)
        {
            long original = refUserDataRangeArray[offset];
            refUserDataRangeArray.DoForEach((ref long item, ref long add) => item += add, offset, length, ref rangeDelta);
            await Assert.That(refUserDataRangeArray[offset]).IsEqualTo(original + rangeDelta);
        }
        else
        {
            refUserDataRangeArray.DoForEach((ref long item, ref long add) => item += add, offset, length, ref rangeDelta);
        }

        LargeArray<long> validationArray = CreateSequentialArray(Math.Max(1, capacity));
        await Assert.That(() => validationArray.DoForEach((Action<long>)null!)).Throws<Exception>();
        await Assert.That(() => validationArray.DoForEach((RefAction<long>)null!)).Throws<Exception>();
        await Assert.That(() => validationArray.DoForEach(static _ => { }, -1, 1)).Throws<Exception>();
        await Assert.That(() => validationArray.DoForEach((ref long _) => { }, -1, 1)).Throws<Exception>();
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

        array.Sort(null);
        List<long> sorted = array.GetAll().ToList();
        await Assert.That(sorted.SequenceEqual(sorted.OrderBy(x => x))).IsTrue();

        if (array.Count > 1)
        {
            long offset = 0;
            long length = Math.Min(array.Count, 5);
            array.Sort(static (a, b) => b.CompareTo(a), offset, length);
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

        await Assert.That(() => array.Sort(null, -1, 1)).Throws<Exception>();
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

        public bool Contains(long item) => Contains(item, 0, Count);

        public bool Contains(long item, Func<long, long, bool> equalsFunction)
            => Contains(item, 0, Count, equalsFunction);

        public bool Contains(long item, long offset, long count)
            => Contains(item, offset, count, static (a, b) => a == b);

        public bool Contains(long item, long offset, long count, Func<long, long, bool> equalsFunction)
        {
            StorageExtensions.CheckRange(offset, count, Count);
            equalsFunction ??= static (a, b) => a == b;
            for (long i = 0; i < count; i++)
            {
                if (equalsFunction(_data[offset + i], item))
                {
                    return true;
                }
            }
            return false;
        }

        public long BinarySearch(long item, Func<long, long, int> comparer)
            => BinarySearch(item, comparer, 0, Count);

        public long BinarySearch(long item, Func<long, long, int> comparer, long offset, long count)
        {
            StorageExtensions.CheckRange(offset, count, Count);
            comparer ??= static (a, b) => a.CompareTo(b);
            long low = offset;
            long high = offset + count - 1;
            while (low <= high)
            {
                long mid = low + ((high - low) / 2);
                int cmp = comparer(_data[mid], item);
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

        public long IndexOf(long item)
            => IndexOf(item, static (a, b) => a == b);

        public long IndexOf(long item, Func<long, long, bool> equalsFunction)
            => IndexOf(item, 0, Count, equalsFunction);

        public long IndexOf(long item, long offset, long count)
            => IndexOf(item, offset, count, static (a, b) => a == b);

        public long IndexOf(long item, long offset, long count, Func<long, long, bool> equalsFunction)
        {
            StorageExtensions.CheckRange(offset, count, Count);
            equalsFunction ??= static (a, b) => a == b;
            for (long i = 0; i < count; i++)
            {
                if (equalsFunction(_data[offset + i], item))
                {
                    return offset + i;
                }
            }
            return -1;
        }

        public long LastIndexOf(long item)
            => LastIndexOf(item, static (a, b) => a == b);

        public long LastIndexOf(long item, Func<long, long, bool> equalsFunction)
            => LastIndexOf(item, 0, Count, equalsFunction);

        public long LastIndexOf(long item, long offset, long count)
            => LastIndexOf(item, offset, count, static (a, b) => a == b);

        public long LastIndexOf(long item, long offset, long count, Func<long, long, bool> equalsFunction)
        {
            StorageExtensions.CheckRange(offset, count, Count);
            equalsFunction ??= static (a, b) => a == b;
            for (long i = count - 1; i >= 0; i--)
            {
                if (equalsFunction(_data[offset + i], item))
                {
                    return offset + i;
                }
            }
            return -1;
        }

        public void DoForEach(Action<long> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            for (long i = 0; i < Count; i++)
            {
                action(_data[i]);
            }
        }

        public void DoForEach(Action<long> action, long offset, long count)
        {
            StorageExtensions.CheckRange(offset, count, Count);
            ArgumentNullException.ThrowIfNull(action);
            for (long i = 0; i < count; i++)
            {
                action(_data[offset + i]);
            }
        }

        public void DoForEach<TUserData>(ActionWithUserData<long, TUserData> action, ref TUserData userData)
        {
            ArgumentNullException.ThrowIfNull(action);
            for (long i = 0; i < Count; i++)
            {
                action(_data[i], ref userData);
            }
        }

        public void DoForEach<TUserData>(ActionWithUserData<long, TUserData> action, long offset, long count, ref TUserData userData)
        {
            StorageExtensions.CheckRange(offset, count, Count);
            ArgumentNullException.ThrowIfNull(action);
            for (long i = 0; i < count; i++)
            {
                action(_data[offset + i], ref userData);
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

        public bool Contains(long item) => _inner.Contains(item);

        public bool Contains(long item, Func<long, long, bool> equalsFunction) => _inner.Contains(item, equalsFunction);

        public bool Contains(long item, long offset, long count) => _inner.Contains(item, offset, count);

        public bool Contains(long item, long offset, long count, Func<long, long, bool> equalsFunction)
            => _inner.Contains(item, offset, count, equalsFunction);

        public long BinarySearch(long item, Func<long, long, int> comparer) => _inner.BinarySearch(item, comparer);

        public long BinarySearch(long item, Func<long, long, int> comparer, long offset, long count)
            => _inner.BinarySearch(item, comparer, offset, count);

        public IEnumerator<long> GetEnumerator() => _inner.GetAll().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<long> GetAll() => _inner.GetAll();

        public IEnumerable<long> GetAll(long offset, long count) => _inner.GetAll(offset, count);

        public long Get(long index) => _inner.Get(index);

        public long IndexOf(long item) => _inner.IndexOf(item);

        public long IndexOf(long item, Func<long, long, bool> equalsFunction) => _inner.IndexOf(item, equalsFunction);

        public long IndexOf(long item, long offset, long count) => _inner.IndexOf(item, offset, count);

        public long IndexOf(long item, long offset, long count, Func<long, long, bool> equalsFunction)
            => _inner.IndexOf(item, offset, count, equalsFunction);

        public long LastIndexOf(long item) => _inner.LastIndexOf(item);

        public long LastIndexOf(long item, Func<long, long, bool> equalsFunction) => _inner.LastIndexOf(item, equalsFunction);

        public long LastIndexOf(long item, long offset, long count) => _inner.LastIndexOf(item, offset, count);

        public long LastIndexOf(long item, long offset, long count, Func<long, long, bool> equalsFunction)
            => _inner.LastIndexOf(item, offset, count, equalsFunction);

        public void DoForEach(Action<long> action) => _inner.DoForEach(action);

        public void DoForEach(Action<long> action, long offset, long count) => _inner.DoForEach(action, offset, count);

        public void DoForEach<TUserData>(ActionWithUserData<long, TUserData> action, ref TUserData userData)
            => _inner.DoForEach(action, ref userData);

        public void DoForEach<TUserData>(ActionWithUserData<long, TUserData> action, long offset, long count, ref TUserData userData)
            => _inner.DoForEach(action, offset, count, ref userData);

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

        public void Sort(Func<long, long, int> comparer) => _inner.Sort(comparer);

        public void Sort(Func<long, long, int> comparer, long offset, long count) => _inner.Sort(comparer, offset, count);

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
