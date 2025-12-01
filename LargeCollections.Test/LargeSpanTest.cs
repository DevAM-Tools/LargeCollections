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

public class LargeSpanTest
{
    public static IEnumerable<long> Capacities() => Parameters.Capacities;

    #region LargeSpan - Constructors

    [Test]
    public async Task Constructor_ThrowsOnNullInner()
    {
        await Assert.That(() => new LargeSpan<long>((IRefAccessLargeArray<long>)null!)).Throws<Exception>();
        await Assert.That(() => new LargeSpan<long>((LargeSpan<long>)default, 0L)).Throws<Exception>();
        await Assert.That(() => new LargeSpan<long>((LargeSpan<long>)default, 0L, 0L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Constructor_FromInner_SetsRange(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        await Assert.That(span.Start).IsEqualTo(0L);
        await Assert.That(span.Count).IsEqualTo(inner.Count);
        await Assert.That(span.GetAll().SequenceEqual(inner.GetAll())).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Constructor_WithStart_SetsRange(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);

        if (capacity == 0)
        {
            await Assert.That(() => new LargeSpan<long>(inner, 0L)).Throws<Exception>();
            return;
        }

        long start = Math.Min(1L, capacity - 1L);
        LargeSpan<long> span = new(inner, start);

        await Assert.That(span.Start).IsEqualTo(start);
        await Assert.That(span.Count).IsEqualTo(inner.Count - start);
        await Assert.That(span[0]).IsEqualTo(inner[start]);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Constructor_WithStartAndCount_SetsRange(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);

        if (capacity == 0)
        {
            await Assert.That(() => new LargeSpan<long>(inner, 0L, 1L)).Throws<Exception>();
            return;
        }

        long count = Math.Min(3L, capacity);
        long start = Math.Max(0L, inner.Count - count);
        LargeSpan<long> span = new(inner, start, count);

        await Assert.That(span.Start).IsEqualTo(start);
        await Assert.That(span.Count).IsEqualTo(count);
        await Assert.That(span[0]).IsEqualTo(inner[start]);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Constructor_FromSpan_SetsRange(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> original = new(inner);

        if (original.Count == 0)
        {
            await Assert.That(() => new LargeSpan<long>(original, 0L)).Throws<Exception>();
            return;
        }

        long start = Math.Min(1L, original.Count - 1L);
        long count = Math.Max(1L, Math.Min(3L, original.Count - start));
        LargeSpan<long> sliced = new(original, start, count);

        await Assert.That(sliced.Start).IsEqualTo(original.Start + start);
        await Assert.That(sliced.Count).IsEqualTo(count);
        await Assert.That(sliced[0]).IsEqualTo(inner[original.Start + start]);
    }

    [Test]
    public async Task DefaultSpan_BehavesAsEmpty()
    {
        LargeSpan<long> span = default;

        await Assert.That(span.Count).IsEqualTo(0L);
        await Assert.That(span.Start).IsEqualTo(0L);
        await Assert.That(span.GetAll().Any()).IsFalse();

        span.CopyFrom((IReadOnlyLargeArray<long>)null!, 0L, 0L, 0L);
        span.CopyFrom((ReadOnlyLargeSpan<long>)default, 0L, 0L);
        span.CopyFromArray(Array.Empty<long>(), 0, 0L, 0);
        span.CopyTo((ILargeArray<long>)null!, 0L, 0L, 0L);
        span.CopyTo(default(LargeSpan<long>), 0L, 0L);
        span.CopyToArray(Array.Empty<long>(), 0L, 0, 0);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        span.CopyFromSpan(ReadOnlySpan<long>.Empty, 0L, 0);
        span.CopyToSpan(Span<long>.Empty, 0L, 0);
#endif
    }

    #endregion

    #region LargeSpan - Slicing

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Slice_ReturnsSubspan(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        if (span.Count == 0)
        {
            await Assert.That(() => span.Slice(0L)).Throws<Exception>();
            return;
        }

        long sliceStart = Math.Min(1L, span.Count - 1L);
        LargeSpan<long> slicedFromStart = span.Slice(sliceStart);
        await Assert.That(slicedFromStart.Start).IsEqualTo(span.Start + sliceStart);
        await Assert.That(slicedFromStart.Count).IsEqualTo(span.Count - sliceStart);

        long sliceCount = Math.Max(1L, Math.Min(3L, span.Count - sliceStart));
        LargeSpan<long> slicedRange = span.Slice(sliceStart, sliceCount);
        await Assert.That(slicedRange.Start).IsEqualTo(span.Start + sliceStart);
        await Assert.That(slicedRange.Count).IsEqualTo(sliceCount);
        await Assert.That(slicedRange[0]).IsEqualTo(inner[span.Start + sliceStart]);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Slice_InvalidParameters_Throw(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        await Assert.That(() => span.Slice(-1L)).Throws<Exception>();
        await Assert.That(() => span.Slice(span.Count)).Throws<Exception>();
        await Assert.That(() => span.Slice(0L, span.Count + 1L)).Throws<Exception>();
        await Assert.That(() => span.Slice(-1L, 1L)).Throws<Exception>();
        await Assert.That(() => span.Slice(0L, -1L)).Throws<Exception>();
    }

    #endregion

    #region LargeSpan - Indexers and Access

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Indexer_GetSet_RespectsOffset(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        await Assert.That(() => span[-1L]).Throws<Exception>();
        await Assert.That(() => span[span.Count]).Throws<Exception>();

        if (span.Count == 0)
        {
            await Assert.That(() => span[0L] = 0L).Throws<Exception>();
            return;
        }

        long index = Math.Min(1L, span.Count - 1L);
        long expected = inner[index];
        await Assert.That(span[index]).IsEqualTo(expected);

        long newValue = expected + 1234L;
        span[index] = newValue;
        await Assert.That(inner[index]).IsEqualTo(newValue);

        long offset = Math.Min(1L, Math.Max(0L, span.Count - 2L));
        long count = span.Count - offset;
        if (count > 0)
        {
            LargeSpan<long> offsetSpan = new(inner, span.Start + offset, count);
            await Assert.That(offsetSpan[0]).IsEqualTo(inner[offset + span.Start]);
        }
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task GetRef_ProvidesLiveReference(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        if (span.Count > 0)
        {
            long original = inner[0];
            ref long reference = ref span.GetRef(0L);
            reference = original + 77L;
            await Assert.That(inner[0]).IsEqualTo(original + 77L);
        }

        await Assert.That(() =>
        {
            span.GetRef(-1L);
            return 0;
        }).Throws<Exception>();

        await Assert.That(() =>
        {
            span.GetRef(span.Count);
            return 0;
        }).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Set_And_Get_ValidateRange(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        if (span.Count > 0)
        {
            long index = Math.Min(1L, span.Count - 1L);
            long value = span[index] + 11L;
            span.Set(index, value);
            await Assert.That(inner[index]).IsEqualTo(value);
            await Assert.That(span.Get(index)).IsEqualTo(value);
        }

        await Assert.That(() => span.Set(-1L, 0L)).Throws<Exception>();
        await Assert.That(() => span.Set(span.Count, 0L)).Throws<Exception>();
        await Assert.That(() => span.Get(-1L)).Throws<Exception>();
        await Assert.That(() => span.Get(span.Count)).Throws<Exception>();
    }

    #endregion

    #region LargeSpan - Iteration

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task DoForEach_RefActions_Modify(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        IncrementRefAction incrementAction = new () { IncrementBy = 1 };
        span.DoForEachRef(ref incrementAction);

        if (span.Count > 0)
        {
            await Assert.That(inner[0]).IsEqualTo(1L);
        }

        long offset = Math.Min(1L, Math.Max(0L, span.Count - 1L));
        long length = span.Count - offset;
        IncrementRefAction addTwoAction = new () { IncrementBy = 2 };
        span.DoForEachRef(ref addTwoAction, offset, length);

        SumRefAction sumRefAction = new ();
        span.DoForEachRef(ref sumRefAction);
        await Assert.That(sumRefAction.Sum).IsEqualTo(inner.GetAll().Sum());

        SumRefAction rangeSumRefAction = new ();
        span.DoForEachRef(ref rangeSumRefAction, offset, length);
        await Assert.That(rangeSumRefAction.Sum).IsEqualTo(inner.GetAll(offset, length).Sum());

        IncrementRefAction noopAction = new ();
        await Assert.That(() => span.DoForEachRef(ref noopAction, -1L, 1L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task DoForEach_ActionOverloads_Work(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        long sum = 0L;
        span.DoForEach(item => sum += item);
        await Assert.That(sum).IsEqualTo(inner.GetAll().Sum());

        long offset = Math.Min(1L, Math.Max(0L, span.Count - 1L));
        long length = span.Count - offset;
        long rangeSum = 0L;
        span.DoForEach(item => rangeSum += item, offset, length);
        await Assert.That(rangeSum).IsEqualTo(inner.GetAll(offset, length).Sum());

        SumAction sumAction = new ();
        span.DoForEach(ref sumAction);
        await Assert.That(sumAction.Sum).IsEqualTo(inner.GetAll().Sum());

        SumAction rangeSumAction = new ();
        span.DoForEach(ref rangeSumAction, offset, length);
        await Assert.That(rangeSumAction.Sum).IsEqualTo(inner.GetAll(offset, length).Sum());

        await Assert.That(() => span.DoForEach(_ => { }, -1L, 1L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Sort_And_Swap_OperateWithinSpan(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        for (long i = 0; i < span.Count; i++)
        {
            inner[i] = span.Count - i;
        }

        span.Sort(new DefaultComparer<long>());
        await Assert.That(span.GetAll().SequenceEqual(span.GetAll().OrderBy(x => x))).IsTrue();

        if (span.Count > 1)
        {
            long offset = 0L;
            long length = Math.Min(5L, span.Count);
            span.Sort(new DescendingComparer<long>(), offset, length);
            List<long> range = span.GetAll(offset, length).ToList();
            await Assert.That(range.SequenceEqual(range.OrderByDescending(x => x))).IsTrue();

            long leftIndex = 0L;
            long rightIndex = span.Count - 1L;
            long leftValue = span[leftIndex];
            long rightValue = span[rightIndex];
            span.Swap(leftIndex, rightIndex);
            await Assert.That(span[leftIndex]).IsEqualTo(rightValue);
            await Assert.That(span[rightIndex]).IsEqualTo(leftValue);
        }

        DefaultComparer<long> comparer = new();
        await Assert.That(() => span.Sort(comparer, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => span.Swap(-1L, 0L)).Throws<Exception>();
        await Assert.That(() => span.Swap(0L, span.Count)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Enumerator_EnumeratesAllItems(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        List<long> enumerated = new();
        foreach (long item in span)
        {
            enumerated.Add(item);
        }

        await Assert.That(enumerated.SequenceEqual(inner.GetAll())).IsTrue();
    }

    #endregion

    #region LargeSpan - Search

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task BinarySearch_ReturnsExpectedIndices(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        long segmentLength = Math.Min(5L, inner.Count);
        long start = Math.Max(0L, inner.Count - segmentLength);
        LargeSpan<long> span = segmentLength > 0L ? new(inner, start, segmentLength) : new(inner);

        DefaultComparer<long> comparer = new();

        if (span.Count > 0)
        {
            long targetValue = inner[start];
            // BinarySearch returns span-relative index, so first element is at index 0
            await Assert.That(span.BinarySearch(targetValue, comparer)).IsEqualTo(0L);
            await Assert.That(span.BinarySearch(targetValue, comparer, 0L, span.Count)).IsEqualTo(0L);

            // Second element (if exists) should be at span-relative index 1
            long expectedNext = span.Count > 1 ? 1L : -1L;
            await Assert.That(span.BinarySearch(targetValue + 1L, comparer)).IsEqualTo(expectedNext);
        }
        else
        {
            await Assert.That(span.BinarySearch(0L, comparer)).IsEqualTo(-1L);
        }

        await Assert.That(span.BinarySearch(-999L, comparer)).IsEqualTo(-1L);
        await Assert.That(() => span.BinarySearch(0L, comparer, -1L, 1L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Contains_And_IndexLookups_Work(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        long searchValue = span.Count > 0 ? span[0] : 0L;

        if (span.Count > 0)
        {
            await Assert.That(span.Contains(searchValue)).IsTrue();
            // IndexOf and LastIndexOf return span-relative indices
            await Assert.That(span.IndexOf(searchValue)).IsEqualTo(0L);
            await Assert.That(span.LastIndexOf(searchValue)).IsEqualTo(0L);
            DefaultEqualityComparer<long> eq1 = new();
            await Assert.That(span.Contains(searchValue, ref eq1)).IsTrue();
            DefaultEqualityComparer<long> eq2 = new();
            await Assert.That(span.IndexOf(searchValue, ref eq2)).IsEqualTo(0L);
            DefaultEqualityComparer<long> eq3 = new();
            await Assert.That(span.LastIndexOf(searchValue, ref eq3)).IsEqualTo(0L);

            long offset = Math.Min(1L, Math.Max(0L, span.Count - 1L));
            long length = span.Count - offset;
            await Assert.That(span.Contains(searchValue, offset, length)).IsEqualTo(offset == 0L);
            DefaultEqualityComparer<long> eq4 = new();
            await Assert.That(span.Contains(searchValue, ref eq4, offset, length)).IsEqualTo(offset == 0L);
            await Assert.That(span.IndexOf(searchValue, offset, length)).IsEqualTo(offset == 0L ? 0L : -1L);
            DefaultEqualityComparer<long> eq5 = new();
            await Assert.That(span.IndexOf(searchValue, ref eq5, offset, length)).IsEqualTo(offset == 0L ? 0L : -1L);
            await Assert.That(span.LastIndexOf(searchValue, offset, length)).IsEqualTo(offset == 0L ? 0L : -1L);
            DefaultEqualityComparer<long> eq6 = new();
            await Assert.That(span.LastIndexOf(searchValue, ref eq6, offset, length)).IsEqualTo(offset == 0L ? 0L : -1L);
        }
        else
        {
            await Assert.That(span.Contains(searchValue)).IsFalse();
            await Assert.That(span.IndexOf(searchValue)).IsEqualTo(-1L);
            await Assert.That(span.LastIndexOf(searchValue)).IsEqualTo(-1L);
        }

        await Assert.That(span.Contains(-12345L)).IsFalse();
        await Assert.That(span.IndexOf(-12345L)).IsEqualTo(-1L);
        await Assert.That(span.LastIndexOf(-12345L)).IsEqualTo(-1L);

        await Assert.That(() => span.Contains(0L, -1L, 1L)).Throws<Exception>();
        DefaultEqualityComparer<long> eq7 = new();
        await Assert.That(() => span.Contains(0L, ref eq7, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => span.IndexOf(0L, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => span.LastIndexOf(0L, -1L, 1L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task GetAll_ReturnsExpectedSequences(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        await Assert.That(span.GetAll().SequenceEqual(inner.GetAll())).IsTrue();

        long offset = Math.Min(1L, Math.Max(0L, span.Count - 1L));
        long length = span.Count - offset;
        await Assert.That(span.GetAll(offset, length).SequenceEqual(inner.GetAll().Skip((int)(span.Start + offset)).Take((int)length))).IsTrue();

        await Assert.That(() => span.GetAll(-1L, 1L)).Throws<Exception>();
        await Assert.That(() => span.GetAll(0L, -1L)).Throws<Exception>();
        await Assert.That(() => span.GetAll(span.Count + 1L, 1L)).Throws<Exception>();
    }

    #endregion

    #region LargeSpan - Copy

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyFrom_IReadOnlyLargeArray_Branches(long capacity)
    {
        LargeArray<long> targetArray = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(targetArray);
        LargeArray<long> sourceArray = CreateSequentialArray(capacity);
        LargeList<long> sourceList = CreateListWithSequence(capacity);

        long copyCount = Math.Min(span.Count, 5L);
        if (copyCount == 0)
        {
            span.CopyFrom(sourceArray, 0L, 0L, 0L);
            return;
        }

        long targetOffset = Math.Max(0L, span.Count - copyCount);
        span.CopyFrom(sourceArray, 0L, targetOffset, copyCount);
        await VerifyRangeEquals(targetArray, sourceArray, span.Start + targetOffset, 0L, copyCount);

        span.CopyFrom(sourceList, 0L, targetOffset, copyCount);
        await VerifyRangeEquals(targetArray, sourceList, span.Start + targetOffset, 0L, copyCount);

        await Assert.That(() => span.CopyFrom((IReadOnlyLargeArray<long>)null!, 0L, 0L, copyCount)).Throws<Exception>();
        await Assert.That(() => span.CopyFrom(sourceArray, -1L, 0L, 1L)).Throws<Exception>();
        await Assert.That(() => span.CopyFrom(sourceArray, 0L, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => span.CopyFrom(sourceArray, 0L, 0L, span.Count + 1L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyFrom_ReadOnlyLargeSpan_And_Arrays(long capacity)
    {
        LargeArray<long> targetArray = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(targetArray);
        LargeArray<long> sourceArray = CreateSequentialArray(capacity);

        long copyCount = Math.Min(span.Count, 5L);
        long targetOffset = Math.Max(0L, span.Count - copyCount);

        if (copyCount == 0)
        {
            span.CopyFrom((ReadOnlyLargeSpan<long>)default, 0L, 0L);
            span.CopyFromArray(Array.Empty<long>(), 0, 0L, 0);
            return;
        }

        ReadOnlyLargeSpan<long> sourceSpan = new(sourceArray, 0L, sourceArray.Count);
        span.CopyFrom(sourceSpan, targetOffset, copyCount);
        await VerifyRangeEquals(targetArray, sourceArray, span.Start + targetOffset, sourceSpan.Start, copyCount);

        long[] raw = sourceArray.GetAll().ToArray();
        span.CopyFromArray(raw, 0, targetOffset, (int)Math.Min(copyCount, raw.Length));
        await VerifyRangeEquals(targetArray, raw, span.Start + targetOffset, 0L, Math.Min(copyCount, raw.Length));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        ReadOnlySpan<long> sourceSlice = raw.AsSpan(0, (int)Math.Min(copyCount, raw.Length));
        span.CopyFromSpan(sourceSlice, targetOffset, sourceSlice.Length);
        await VerifyRangeEquals(targetArray, raw, span.Start + targetOffset, 0L, sourceSlice.Length);
#endif

        await Assert.That(() => span.CopyFrom((ReadOnlyLargeSpan<long>)default, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => span.CopyFromArray((long[])null!, 0, 0L, 1)).Throws<Exception>();
        await Assert.That(() => span.CopyFromArray(raw, -1, 0L, 1)).Throws<Exception>();
        await Assert.That(() => span.CopyFromArray(raw, 0, -1L, 1)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyTo_Targets_Work(long capacity)
    {
        LargeArray<long> sourceArray = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(sourceArray);

        long copyCount = Math.Min(span.Count, 5L);
        long sourceOffset = Math.Max(0L, span.Count - copyCount);

        LargeArray<long> arrayTarget = CreateSequentialArray(capacity);
        span.CopyTo(arrayTarget, sourceOffset, 0L, copyCount);
        await VerifyRangeEquals(arrayTarget, sourceArray, 0L, span.Start + sourceOffset, copyCount);

        LargeArray<long> spanBacking = CreateSequentialArray(capacity);
        LargeSpan<long> spanTarget = new(spanBacking);
        span.CopyTo(spanTarget, sourceOffset, copyCount);
        await VerifyRangeEquals(spanBacking, sourceArray, spanTarget.Start, span.Start + sourceOffset, copyCount);

        if (copyCount == 0)
        {
            span.CopyToArray(Array.Empty<long>(), sourceOffset, 0, 0);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
            span.CopyToSpan(Span<long>.Empty, sourceOffset, 0);
#endif
            return;
        }

        int arrayLength = (int)Math.Min(copyCount, 4L);
        arrayLength = Math.Max(arrayLength, 1);
        long[] array = new long[arrayLength];
        span.CopyToArray(array, sourceOffset, 0, array.Length);
        await VerifyRangeEquals(array, sourceArray, 0L, span.Start + sourceOffset, array.Length);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        Span<long> writableSpan = array.AsSpan();
        span.CopyToSpan(writableSpan, sourceOffset, writableSpan.Length);
        await VerifyRangeEquals(array, sourceArray, 0L, span.Start + sourceOffset, writableSpan.Length);
#endif

        await Assert.That(() => span.CopyTo((ILargeArray<long>)null!, sourceOffset, 0L, 1L)).Throws<Exception>();
        await Assert.That(() => span.CopyTo(arrayTarget, -1L, 0L, 1L)).Throws<Exception>();
        await Assert.That(() => span.CopyTo(arrayTarget, 0L, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => span.CopyTo(arrayTarget, 0L, 0L, span.Count + 1L)).Throws<Exception>();
        await Assert.That(() => span.CopyToArray(array, -1L, 0, 1)).Throws<Exception>();
        await Assert.That(() => span.CopyToArray(array, 0L, -1, 1)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public void CopyOperations_AllowZeroCount(long capacity)
    {
        LargeArray<long> source = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(source);

        span.CopyFrom((IReadOnlyLargeArray<long>)null!, 0L, 0L, 0L);
        span.CopyFrom((ReadOnlyLargeSpan<long>)default, 0L, 0L);
        span.CopyFromArray(Array.Empty<long>(), 0, 0L, 0);

        span.CopyTo((ILargeArray<long>)null!, 0L, 0L, 0L);
        span.CopyTo(default(LargeSpan<long>), 0L, 0L);
        span.CopyToArray(Array.Empty<long>(), 0L, 0, 0);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        span.CopyFromSpan(ReadOnlySpan<long>.Empty, 0L, 0);
        span.CopyToSpan(Span<long>.Empty, 0L, 0);
#endif
    }

    #endregion

    #region LargeSpan - ReadOnly Conversion

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ImplicitConversion_ToReadOnlyLargeSpan(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        ReadOnlyLargeSpan<long> readOnly = span;

        await Assert.That(readOnly.Start).IsEqualTo(span.Start);
        await Assert.That(readOnly.Count).IsEqualTo(span.Count);
        await Assert.That(readOnly.GetAll().SequenceEqual(span.GetAll())).IsTrue();
    }

    #endregion

    #region ReadOnlyLargeSpan - Constructors & Access

    [Test]
    public async Task ReadOnlyConstructor_ThrowsOnNull()
    {
        await Assert.That(() => new ReadOnlyLargeSpan<long>((IReadOnlyLargeArray<long>)null!)).Throws<Exception>();
        await Assert.That(() => new ReadOnlyLargeSpan<long>((ReadOnlyLargeSpan<long>)default, 0L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ReadOnlyConstructor_FromInner(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        ReadOnlyLargeSpan<long> span = new(inner);

        await Assert.That(span.Start).IsEqualTo(0L);
        await Assert.That(span.Count).IsEqualTo(inner.Count);

        if (span.Count > 0)
        {
            await Assert.That(span[0]).IsEqualTo(inner[0]);
        }
        else
        {
            await Assert.That(() => span[0]).Throws<Exception>();
        }
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ReadOnlyConstructor_FromInner_WithStartAndCount(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);

        if (inner.Count == 0)
        {
            await Assert.That(() => new ReadOnlyLargeSpan<long>(inner, 0L)).Throws<Exception>();
            await Assert.That(() => new ReadOnlyLargeSpan<long>(inner, 0L, 1L)).Throws<Exception>();
            return;
        }

        long start = Math.Min(1L, inner.Count - 1L);
        ReadOnlyLargeSpan<long> tail = new(inner, start);
        await Assert.That(tail.Start).IsEqualTo(start);
        await Assert.That(tail.Count).IsEqualTo(inner.Count - start);
        await Assert.That(tail[0]).IsEqualTo(inner[start]);

        long count = Math.Max(1L, Math.Min(2L, tail.Count));
        ReadOnlyLargeSpan<long> subset = new(inner, start, count);
        await Assert.That(subset.Start).IsEqualTo(start);
        await Assert.That(subset.Count).IsEqualTo(count);
        await Assert.That(subset[0]).IsEqualTo(inner[start]);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ReadOnlyConstructor_FromLargeSpan(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);
        ReadOnlyLargeSpan<long> readOnly = new(span, 0L, span.Count);

        await Assert.That(readOnly.Start).IsEqualTo(span.Start);
        await Assert.That(readOnly.Count).IsEqualTo(span.Count);
        await Assert.That(readOnly.GetAll().SequenceEqual(span.GetAll())).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ReadOnlyConstructor_FromLargeSpan_WithStart(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        LargeSpan<long> span = new(inner);

        if (span.Count < 2)
        {
            await Assert.That(() => new ReadOnlyLargeSpan<long>(span, span.Count)).Throws<Exception>();
            return;
        }

        long start = 1L;
        ReadOnlyLargeSpan<long> sliced = new(span, start);
        await Assert.That(sliced.Start).IsEqualTo(span.Start + start);
        await Assert.That(sliced.Count).IsEqualTo(span.Count - start);
        await Assert.That(sliced[0]).IsEqualTo(inner[span.Start + start]);

        long count = Math.Min(2L, sliced.Count);
        ReadOnlyLargeSpan<long> ranged = new(span, start, count);
        await Assert.That(ranged.Start).IsEqualTo(span.Start + start);
        await Assert.That(ranged.Count).IsEqualTo(count);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ReadOnlySlice_BehavesLikeSpan(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        ReadOnlyLargeSpan<long> readOnly = new(inner);

        if (readOnly.Count == 0)
        {
            await Assert.That(() => readOnly.Slice(0L)).Throws<Exception>();
            return;
        }

        long start = Math.Min(1L, readOnly.Count - 1L);
        ReadOnlyLargeSpan<long> sliced = readOnly.Slice(start);
        await Assert.That(sliced.Start).IsEqualTo(readOnly.Start + start);
        await Assert.That(sliced.Count).IsEqualTo(readOnly.Count - start);

        long count = Math.Max(1L, Math.Min(3L, readOnly.Count - start));
        ReadOnlyLargeSpan<long> range = readOnly.Slice(start, count);
        await Assert.That(range.Start).IsEqualTo(readOnly.Start + start);
        await Assert.That(range.Count).IsEqualTo(count);

        await Assert.That(() => readOnly.Slice(-1L)).Throws<Exception>();
        await Assert.That(() => readOnly.Slice(0L, -1L)).Throws<Exception>();
    }

    [Test]
    public async Task ReadOnlyDefault_BehavesAsEmpty()
    {
        ReadOnlyLargeSpan<long> span = default;
        await Assert.That(span.Count).IsEqualTo(0L);
        await Assert.That(span.GetAll().Any()).IsFalse();
    }

    #endregion

    #region ReadOnlyLargeSpan - Iteration & Search

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ReadOnly_DoForEach_And_GetAll(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        ReadOnlyLargeSpan<long> span = new(inner);

        long sum = 0L;
        span.DoForEach(value => sum += value);
        await Assert.That(sum).IsEqualTo(inner.GetAll().Sum());

        long offset = Math.Min(1L, Math.Max(0L, span.Count - 1L));
        long length = span.Count - offset;
        long rangeSum = 0L;
        span.DoForEach(value => rangeSum += value, offset, length);
        await Assert.That(rangeSum).IsEqualTo(inner.GetAll(offset, length).Sum());

        SumAction sumAction = new ();
        span.DoForEach(ref sumAction);
        await Assert.That(sumAction.Sum).IsEqualTo(inner.GetAll().Sum());

        SumAction rangeSumAction = new ();
        span.DoForEach(ref rangeSumAction, offset, length);
        await Assert.That(rangeSumAction.Sum).IsEqualTo(inner.GetAll(offset, length).Sum());

        await Assert.That(() => span.DoForEach(_ => { }, -1L, 1L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ReadOnly_BinarySearch_And_Contains(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        ReadOnlyLargeSpan<long> span = new(inner);
        DefaultComparer<long> comparer = new();

        if (span.Count > 0)
        {
            long value = span[0];
            await Assert.That(span.BinarySearch(value, comparer)).IsEqualTo(span.Start);
            await Assert.That(span.Contains(value)).IsTrue();
            DefaultEqualityComparer<long> eq1 = new();
            await Assert.That(span.Contains(value, ref eq1)).IsTrue();
            await Assert.That(span.IndexOf(value)).IsEqualTo(span.Start);
            await Assert.That(span.LastIndexOf(value)).IsEqualTo(span.Start);
        }

        await Assert.That(span.BinarySearch(-1L, comparer)).IsEqualTo(-1L);
        await Assert.That(span.Contains(-1L)).IsFalse();
        await Assert.That(span.IndexOf(-1L)).IsEqualTo(-1L);
        await Assert.That(span.LastIndexOf(-1L)).IsEqualTo(-1L);

        await Assert.That(() => span.BinarySearch(0L, comparer, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => span.Contains(0L, -1L, 1L)).Throws<Exception>();
        DefaultEqualityComparer<long> eq2 = new();
        await Assert.That(() => span.Contains(0L, ref eq2, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => span.IndexOf(0L, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => span.LastIndexOf(0L, -1L, 1L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ReadOnly_BinarySearch_WithOffset(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        ReadOnlyLargeSpan<long> span = new(inner);
        DefaultComparer<long> comparer = new();

        if (span.Count == 0)
        {
            await Assert.That(span.BinarySearch(0L, comparer, 0L, 0L)).IsEqualTo(-1L);
            return;
        }

        long offset = Math.Min(1L, span.Count - 1L);
        long length = span.Count - offset;
        long target = span[offset];
        await Assert.That(span.BinarySearch(target, comparer, offset, length)).IsEqualTo(span.Start + offset);
        await Assert.That(span.BinarySearch(target + span.Count + 1L, comparer, offset, length)).IsEqualTo(-1L);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ReadOnly_Contains_WithOffsetsAndComparer(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        ReadOnlyLargeSpan<long> span = new(inner);

        if (span.Count == 0)
        {
            await Assert.That(span.Contains(0L, 0L, 0L)).IsFalse();
            DefaultEqualityComparer<long> eq1 = new();
            await Assert.That(span.Contains(0L, ref eq1, 0L, 0L)).IsFalse();
            return;
        }

        long value = span[0];
        await Assert.That(span.Contains(value, 0L, span.Count)).IsTrue();
        DefaultEqualityComparer<long> eq2 = new();
        await Assert.That(span.Contains(value, ref eq2, 0L, span.Count)).IsTrue();

        if (span.Count > 1)
        {
            long offset = 1L;
            long length = span.Count - offset;
            await Assert.That(span.Contains(value, offset, length)).IsFalse();
            DefaultEqualityComparer<long> eq3 = new();
            await Assert.That(span.Contains(value, ref eq3, offset, length)).IsFalse();
        }
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ReadOnly_IndexLookups_WithEqualsFunctionAndRange(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        ReadOnlyLargeSpan<long> span = new(inner);

        long target = span.Count > 0 ? span[0] : 0L;

        if (span.Count > 0)
        {
            DefaultEqualityComparer<long> eq1 = new();
            await Assert.That(span.IndexOf(target, ref eq1)).IsEqualTo(span.Start);
            DefaultEqualityComparer<long> eq2 = new();
            await Assert.That(span.LastIndexOf(target, ref eq2)).IsEqualTo(span.Start);
            DefaultEqualityComparer<long> eq3 = new();
            await Assert.That(span.IndexOf(target, ref eq3, 0L, span.Count)).IsEqualTo(span.Start);
            DefaultEqualityComparer<long> eq4 = new();
            await Assert.That(span.LastIndexOf(target, ref eq4, 0L, span.Count)).IsEqualTo(span.Start);

            if (span.Count > 1)
            {
                long offset = 1L;
                long length = span.Count - offset;
                DefaultEqualityComparer<long> eq5 = new();
                await Assert.That(span.IndexOf(target, ref eq5, offset, length)).IsEqualTo(-1L);
                DefaultEqualityComparer<long> eq6 = new();
                await Assert.That(span.LastIndexOf(target, ref eq6, offset, length)).IsEqualTo(-1L);
            }
        }
        else
        {
            DefaultEqualityComparer<long> eq7 = new();
            await Assert.That(span.IndexOf(target, ref eq7)).IsEqualTo(-1L);
            DefaultEqualityComparer<long> eq8 = new();
            await Assert.That(span.LastIndexOf(target, ref eq8)).IsEqualTo(-1L);
        }
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ReadOnly_GetAll_Enumerates(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        ReadOnlyLargeSpan<long> span = new(inner);

        await Assert.That(span.GetAll().SequenceEqual(inner.GetAll())).IsTrue();

        long offset = Math.Min(1L, Math.Max(0L, span.Count - 1L));
        long length = span.Count - offset;
        await Assert.That(span.GetAll(offset, length).SequenceEqual(inner.GetAll(offset, length))).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task ReadOnly_CopyTo_Targets(long capacity)
    {
        LargeArray<long> inner = CreateSequentialArray(capacity);
        ReadOnlyLargeSpan<long> span = new(inner);
        long copyCount = Math.Min(span.Count, 5L);
        long sourceOffset = Math.Max(0L, span.Count - copyCount);

        LargeArray<long> arrayTarget = CreateSequentialArray(capacity);
        span.CopyTo(arrayTarget, sourceOffset, 0L, copyCount);
        await VerifyRangeEquals(arrayTarget, inner, 0L, span.Start + sourceOffset, copyCount);

        LargeArray<long> spanBacking = CreateSequentialArray(capacity);
        LargeSpan<long> targetSpan = new(spanBacking);
        span.CopyTo(targetSpan, sourceOffset, copyCount);
        await VerifyRangeEquals(spanBacking, inner, targetSpan.Start, span.Start + sourceOffset, copyCount);

        if (copyCount == 0)
        {
            span.CopyToArray(Array.Empty<long>(), sourceOffset, 0, 0);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
            span.CopyToSpan(Span<long>.Empty, sourceOffset, 0);
#endif
            return;
        }

        int rawLength = (int)Math.Min(copyCount, 4L);
        rawLength = Math.Max(rawLength, 1);
        long[] raw = new long[rawLength];
        span.CopyToArray(raw, sourceOffset, 0, raw.Length);
        await VerifyRangeEquals(raw, inner, 0L, span.Start + sourceOffset, raw.Length);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        Span<long> writable = raw.AsSpan();
        span.CopyToSpan(writable, sourceOffset, writable.Length);
        await VerifyRangeEquals(raw, inner, 0L, span.Start + sourceOffset, writable.Length);
#endif

        await Assert.That(() => span.CopyTo((ILargeArray<long>)null!, sourceOffset, 0L, 1L)).Throws<Exception>();
        await Assert.That(() => span.CopyTo(arrayTarget, -1L, 0L, 1L)).Throws<Exception>();
        await Assert.That(() => span.CopyToArray(raw, -1L, 0, 1)).Throws<Exception>();
    }

    #endregion

    #region Helpers

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
        for (long i = 0; i < count && targetOffset + i < target.Count && sourceOffset + i < source.Count; i++)
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

    private static async Task VerifyRangeEquals(long[] target, IReadOnlyLargeArray<long> source, long targetOffset, long sourceOffset, long count)
    {
        for (long i = 0; i < count && targetOffset + i < target.LongLength && sourceOffset + i < source.Count; i++)
        {
            await Assert.That(target[targetOffset + i]).IsEqualTo(source[sourceOffset + i]);
        }
    }

    #endregion

    #region Edge Cases - Self/Overlapping Copy

    [Test]
    public async Task CopyFrom_SameArray_NonOverlapping_Works()
    {
        // Copy from end of array to beginning (no overlap)
        LargeArray<long> array = new(10);
        for (long i = 0; i < 10; i++) array[i] = i * 10;

        LargeSpan<long> span = new(array);
        ReadOnlyLargeSpan<long> sourceSpan = new(array, 5, 5); // [50,60,70,80,90]

        span.CopyFrom(sourceSpan, 0, 5);

        // First 5 elements should now be [50,60,70,80,90]
        await Assert.That(array[0]).IsEqualTo(50L);
        await Assert.That(array[1]).IsEqualTo(60L);
        await Assert.That(array[2]).IsEqualTo(70L);
        await Assert.That(array[3]).IsEqualTo(80L);
        await Assert.That(array[4]).IsEqualTo(90L);
    }

    [Test]
    public async Task Slice_CreateMultipleSlices_IndependentViews()
    {
        LargeArray<long> array = new(10);
        for (long i = 0; i < 10; i++) array[i] = i;

        LargeSpan<long> fullSpan = new(array);
        LargeSpan<long> firstHalf = fullSpan.Slice(0, 5);
        LargeSpan<long> secondHalf = fullSpan.Slice(5, 5);

        // Modify through different slices
        firstHalf[0] = 100;
        secondHalf[0] = 200;

        await Assert.That(array[0]).IsEqualTo(100L);
        await Assert.That(array[5]).IsEqualTo(200L);
        await Assert.That(fullSpan[0]).IsEqualTo(100L);
        await Assert.That(fullSpan[5]).IsEqualTo(200L);
    }

    [Test]
    public async Task Slice_ChainedSlicing_CorrectBounds()
    {
        LargeArray<long> array = new(20);
        for (long i = 0; i < 20; i++) array[i] = i;

        LargeSpan<long> span1 = new(array, 5, 10); // [5..14]
        LargeSpan<long> span2 = span1.Slice(2, 5);  // [7..11] relative to span1

        await Assert.That(span2.Start).IsEqualTo(7L);
        await Assert.That(span2.Count).IsEqualTo(5L);
        await Assert.That(span2[0]).IsEqualTo(7L);
        await Assert.That(span2[4]).IsEqualTo(11L);
    }

    [Test]
    public async Task EmptySpan_Operations_Work()
    {
        LargeArray<long> array = new(10);
        LargeSpan<long> emptySpan = new(array, 5, 0);

        await Assert.That(emptySpan.Count).IsEqualTo(0L);
        await Assert.That(emptySpan.Start).IsEqualTo(5L);
        await Assert.That(emptySpan.GetAll().Any()).IsFalse();

        // Operations on empty span should not throw
        emptySpan.CopyFrom((ReadOnlyLargeSpan<long>)default, 0L, 0L);
        emptySpan.CopyTo(default(LargeSpan<long>), 0L, 0L);
    }

    #endregion

    #region Helper Structs

    private struct IncrementRefAction : ILargeRefAction<long>
    {
        public long IncrementBy;
        public void Invoke(ref long item) => item += IncrementBy;
    }

    private struct SumRefAction : ILargeRefAction<long>
    {
        public long Sum;
        public void Invoke(ref long item) => Sum += item;
    }

    private struct SumAction : ILargeAction<long>
    {
        public long Sum;
        public void Invoke(long item) => Sum += item;
    }

    #endregion
}
