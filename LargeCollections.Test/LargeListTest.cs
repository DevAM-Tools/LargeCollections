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

public class LargeListTest
{
    public static IEnumerable<long> Capacities() => Parameters.Capacities;

    private const long MarkerBase = 10_000L;

    #region Constructor / Properties
    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Constructor_SetsDefaults(long capacity)
    {
        LargeList<long> list = new(capacity);

        await Assert.That(list.Count).IsEqualTo(0L);
        await Assert.That(list.Capacity).IsEqualTo(capacity);
        await Assert.That(list.CapacityGrowFactor).IsEqualTo(Constants.DefaultCapacityGrowFactor);
        await Assert.That(list.FixedCapacityGrowAmount).IsEqualTo(Constants.DefaultFixedCapacityGrowAmount);
        await Assert.That(list.FixedCapacityGrowLimit).IsEqualTo(Constants.DefaultFixedCapacityGrowLimit);
        await Assert.That(list.MinLoadFactor).IsEqualTo(Constants.DefaultMinLoadFactor);
    }

    [Test]
    public async Task Constructor_ThrowsOnInvalidParameters()
    {
        await Assert.That(() => new LargeList<int>(1, 1.0, Constants.DefaultFixedCapacityGrowAmount, Constants.DefaultFixedCapacityGrowLimit, Constants.DefaultMinLoadFactor)).Throws<Exception>();
        await Assert.That(() => new LargeList<int>(1, Constants.MaxCapacityGrowFactor + 0.1, Constants.DefaultFixedCapacityGrowAmount, Constants.DefaultFixedCapacityGrowLimit, Constants.DefaultMinLoadFactor)).Throws<Exception>();
        await Assert.That(() => new LargeList<int>(1, Constants.DefaultCapacityGrowFactor, 0, Constants.DefaultFixedCapacityGrowLimit, Constants.DefaultMinLoadFactor)).Throws<Exception>();
        await Assert.That(() => new LargeList<int>(1, Constants.DefaultCapacityGrowFactor, Constants.DefaultFixedCapacityGrowAmount, 0, Constants.DefaultMinLoadFactor)).Throws<Exception>();
        await Assert.That(() => new LargeList<int>(1, Constants.DefaultCapacityGrowFactor, Constants.DefaultFixedCapacityGrowAmount, Constants.DefaultFixedCapacityGrowLimit, -0.1)).Throws<Exception>();
        await Assert.That(() => new LargeList<int>(1, Constants.DefaultCapacityGrowFactor, Constants.DefaultFixedCapacityGrowAmount, Constants.DefaultFixedCapacityGrowLimit, 1.0)).Throws<Exception>();
    }

    private sealed class LargeArrayBuffer : ILargeArray<long>
    {
        private readonly long[] _data;

        public LargeArrayBuffer(long count)
        {
            _data = new long[count];
        }

        public LargeArrayBuffer(IReadOnlyLargeArray<long> source)
        {
            _data = source.GetAll().ToArray();
        }

        public long Count => _data.LongLength;

        public long this[long index]
        {
            get
            {
                StorageExtensions.CheckIndex(index, Count);
                return _data[index];
            }
            set
            {
                StorageExtensions.CheckIndex(index, Count);
                _data[index] = value;
            }
        }

        public long Get(long index) => this[index];

        public void Set(long index, long item) => this[index] = item;

        public void Sort(Func<long, long, int> comparer)
        {
            comparer ??= static (a, b) => a.CompareTo(b);
            Array.Sort(_data, 0, (int)Count, Comparer<long>.Create((x, y) => comparer(x, y)));
        }

        public void Sort(Func<long, long, int> comparer, long offset, long count)
        {
            StorageExtensions.CheckRange(offset, count, Count);
            comparer ??= static (a, b) => a.CompareTo(b);
            Array.Sort(_data, (int)offset, (int)count, Comparer<long>.Create((x, y) => comparer(x, y)));
        }

        public void Swap(long leftIndex, long rightIndex)
        {
            StorageExtensions.CheckIndex(leftIndex, Count);
            StorageExtensions.CheckIndex(rightIndex, Count);
            (_data[leftIndex], _data[rightIndex]) = (_data[rightIndex], _data[leftIndex]);
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

            return -1L;
        }

        public IEnumerable<long> GetAll()
        {
            for (long i = 0; i < _data.LongLength; i++)
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

        public long IndexOf(long item) => Array.IndexOf(_data, item);

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

            return -1L;
        }

        public long LastIndexOf(long item) => Array.LastIndexOf(_data, item);

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

            return -1L;
        }

        public void CopyFrom(IReadOnlyLargeArray<long> source, long sourceOffset, long targetOffset, long count)
        {
            StorageExtensions.CheckRange(sourceOffset, count, source.Count);
            StorageExtensions.CheckRange(targetOffset, count, Count);
            for (long i = 0; i < count; i++)
            {
                this[targetOffset + i] = source[sourceOffset + i];
            }
        }

        public void CopyFrom(ReadOnlyLargeSpan<long> source, long targetOffset, long count)
        {
            StorageExtensions.CheckRange(0, count, source.Count);
            StorageExtensions.CheckRange(targetOffset, count, Count);
            for (long i = 0; i < count; i++)
            {
                this[targetOffset + i] = source[i];
            }
        }

        public void CopyFromArray(long[] source, int sourceOffset, long targetOffset, int count)
        {
            StorageExtensions.CheckRange(sourceOffset, count, source.Length);
            StorageExtensions.CheckRange(targetOffset, count, Count);
            Array.Copy(source, sourceOffset, _data, (int)targetOffset, count);
        }

        public void CopyFromSpan(ReadOnlySpan<long> source, long targetOffset, int count)
        {
            StorageExtensions.CheckRange(targetOffset, count, Count);
            source.Slice(0, count).CopyTo(_data.AsSpan((int)targetOffset, count));
        }

        public void CopyTo(ILargeArray<long> target, long sourceOffset, long targetOffset, long count)
        {
            StorageExtensions.CheckRange(sourceOffset, count, Count);
            StorageExtensions.CheckRange(targetOffset, count, target.Count);
            for (long i = 0; i < count; i++)
            {
                target[targetOffset + i] = _data[sourceOffset + i];
            }
        }

        public void CopyTo(LargeSpan<long> target, long sourceOffset, long count)
        {
            StorageExtensions.CheckRange(sourceOffset, count, Count);
            for (long i = 0; i < count; i++)
            {
                target[i] = _data[sourceOffset + i];
            }
        }

        public void CopyToArray(long[] target, long sourceOffset, int targetOffset, int count)
        {
            StorageExtensions.CheckRange(sourceOffset, count, Count);
            StorageExtensions.CheckRange(targetOffset, count, target.Length);
            Array.Copy(_data, (int)sourceOffset, target, targetOffset, count);
        }

        public void CopyToSpan(Span<long> target, long sourceOffset, int count)
        {
            StorageExtensions.CheckRange(sourceOffset, count, Count);
            StorageExtensions.CheckRange(0, count, target.Length);
            _data.AsSpan((int)sourceOffset, count).CopyTo(target);
        }

        public void DoForEach(Action<long> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (long item in _data)
            {
                action(item);
            }
        }

        public void DoForEach(Action<long> action, long offset, long count)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            StorageExtensions.CheckRange(offset, count, Count);
            for (long i = 0; i < count; i++)
            {
                action(_data[offset + i]);
            }
        }

        public void DoForEach<TUserData>(ActionWithUserData<long, TUserData> action, ref TUserData userData)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (long item in _data)
            {
                action(item, ref userData);
            }
        }

        public void DoForEach<TUserData>(ActionWithUserData<long, TUserData> action, long offset, long count, ref TUserData userData)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            StorageExtensions.CheckRange(offset, count, Count);
            for (long i = 0; i < count; i++)
            {
                action(_data[offset + i], ref userData);
            }
        }

        public IEnumerator<long> GetEnumerator() => ((IEnumerable<long>)_data).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    #endregion

    #region Add / Indexer / Accessors

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Add_And_Indexer_Work(long capacity)
    {
        long itemCount = Math.Max(1, capacity);
        LargeList<long> list = new(capacity);

        for (long i = 0; i < itemCount; i++)
        {
            list.Add(i);
        }

        await Assert.That(list.Count).IsEqualTo(itemCount);

        for (long i = 0; i < itemCount; i++)
        {
            await Assert.That(list[i]).IsEqualTo(i);
            list[i] = MarkerBase + i;
            await Assert.That(list.Get(i)).IsEqualTo(MarkerBase + i);
        }

        if (itemCount > 0)
        {
            ref long lastRef = ref list.GetRef(itemCount - 1);
            long original = lastRef;
            lastRef = original + 5;
            await Assert.That(list[itemCount - 1]).IsEqualTo(original + 5);
        }

        await Assert.That(() => list[-1]).Throws<Exception>();
        await Assert.That(() => list[-1] = 0).Throws<Exception>();
        await Assert.That(() => list[itemCount]).Throws<Exception>();
        await Assert.That(() => list[itemCount] = 0).Throws<Exception>();
        await Assert.That(() => list.Get(-1)).Throws<Exception>();
        await Assert.That(() => list.Get(itemCount)).Throws<Exception>();
        await Assert.That(() => list.GetRef(-1)).Throws<Exception>();
        await Assert.That(() => list.GetRef(itemCount)).Throws<Exception>();
    }

    #endregion

    #region Contains / IndexOf / LastIndexOf

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Contains_IndexOf_LastIndexOf(long capacity)
    {
        LargeList<long> list = CreateSequentialList(capacity);

        if (capacity > 0)
        {
            long baseIndex = Math.Max(0, capacity / 2);
            long marker = MarkerBase + capacity;
            list[baseIndex] = marker;
            if (baseIndex + 1 < capacity)
            {
                list[baseIndex + 1] = marker;
            }

            await Assert.That(list.Contains(marker)).IsTrue();
            await Assert.That(list.Contains(marker, static (a, b) => a == b)).IsTrue();
            await Assert.That(list.IndexOf(marker)).IsEqualTo(baseIndex);
            await Assert.That(list.IndexOf(marker, static (a, b) => a == b)).IsEqualTo(baseIndex);

            long expectedLast = baseIndex + (baseIndex + 1 < capacity ? 1 : 0);
            await Assert.That(list.LastIndexOf(marker)).IsEqualTo(expectedLast);
            await Assert.That(list.LastIndexOf(marker, static (a, b) => a == b)).IsEqualTo(expectedLast);

            long offset = baseIndex;
            long length = Math.Max(1, Math.Min(2, capacity - offset));
            await Assert.That(list.Contains(marker, offset, length)).IsTrue();
            await Assert.That(list.Contains(marker, offset, length, static (a, b) => a == b)).IsTrue();
            await Assert.That(list.IndexOf(marker, offset, length)).IsEqualTo(offset);
            await Assert.That(list.IndexOf(marker, offset, length, static (a, b) => a == b)).IsEqualTo(offset);
            long expectedLastInRange = offset + Math.Min(1, length - 1);
            await Assert.That(list.LastIndexOf(marker, offset, length)).IsEqualTo(expectedLastInRange);
            await Assert.That(list.LastIndexOf(marker, offset, length, static (a, b) => a == b)).IsEqualTo(expectedLastInRange);
        }

        long missingValue = MarkerBase + capacity + 10;
        await Assert.That(list.Contains(missingValue)).IsFalse();
        await Assert.That(list.Contains(missingValue, static (a, b) => a == b)).IsFalse();

        long offsetCheck = Math.Min(1, Math.Max(0, list.Count - 1));
        long lengthCheck = Math.Max(0, list.Count - offsetCheck);
        await Assert.That(list.Contains(missingValue, offsetCheck, lengthCheck)).IsFalse();
        await Assert.That(list.Contains(missingValue, offsetCheck, lengthCheck, static (a, b) => a == b)).IsFalse();

        await Assert.That(list.IndexOf(missingValue)).IsEqualTo(-1L);
        await Assert.That(list.LastIndexOf(missingValue)).IsEqualTo(-1L);

        await Assert.That(() => list.Contains(missingValue, -1, 1)).Throws<Exception>();
        await Assert.That(() => list.Contains(missingValue, 0, list.Count + 1)).Throws<Exception>();
        await Assert.That(() => list.IndexOf(missingValue, -1, 1)).Throws<Exception>();
        await Assert.That(() => list.IndexOf(missingValue, 0, list.Count + 1)).Throws<Exception>();
        await Assert.That(() => list.LastIndexOf(missingValue, -1, 1)).Throws<Exception>();
        await Assert.That(() => list.LastIndexOf(missingValue, 0, list.Count + 1)).Throws<Exception>();
    }

    #endregion

    #region Sort / BinarySearch / Swap

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Sort_And_Swap_Work(long capacity)
    {
        LargeList<long> list = CreateSequentialList(capacity);

        for (long i = 0; i < list.Count; i++)
        {
            list[i] = list.Count > 0 ? list.Count - i : 0;
        }

        list.Sort(null);
        List<long> sorted = list.GetAll().ToList();
        await Assert.That(sorted.SequenceEqual(sorted.OrderBy(x => x))).IsTrue();

        if (list.Count > 1)
        {
            long offset = 0;
            long length = Math.Min(list.Count, 5);
            list.Sort(static (a, b) => b.CompareTo(a), offset, length);
            List<long> segment = list.GetAll(offset, length).ToList();
            await Assert.That(segment.SequenceEqual(segment.OrderByDescending(x => x))).IsTrue();

            long left = 0;
            long right = list.Count - 1;
            long leftValue = list[left];
            long rightValue = list[right];
            list.Swap(left, right);
            await Assert.That(list[left]).IsEqualTo(rightValue);
            await Assert.That(list[right]).IsEqualTo(leftValue);
        }

        await Assert.That(() => list.Sort(null, -1, 1)).Throws<Exception>();
        await Assert.That(() => list.Swap(-1, 0)).Throws<Exception>();
        await Assert.That(() => list.Swap(0, list.Count)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task BinarySearch_Overloads(long capacity)
    {
        LargeList<long> list = CreateSequentialList(capacity);
        for (long i = 0; i < capacity; i++)
        {
            list[i] = i * 2;
        }

        long existing = capacity > 0 ? list[Math.Max(0, capacity / 2)] : 0;
        long missing = existing + 1;

        long result = list.BinarySearch(existing, static (a, b) => a.CompareTo(b));
        if (capacity > 0)
        {
            await Assert.That(result).IsEqualTo(Math.Max(0, capacity / 2));
        }
        else
        {
            await Assert.That(result).IsEqualTo(-1L);
        }

        long missingResult = list.BinarySearch(missing, static (a, b) => a.CompareTo(b));
        await Assert.That(missingResult).IsEqualTo(-1L);

        long offset = Math.Min(1, Math.Max(0, capacity - 1));
        long length = Math.Max(0, capacity - offset);
        if (length > 0)
        {
            long value = list[offset + length / 2];
            long rangeResult = list.BinarySearch(value, static (a, b) => a.CompareTo(b), offset, length);
            await Assert.That(rangeResult).IsEqualTo(offset + length / 2);
        }

        await Assert.That(() => list.BinarySearch(0, static (a, b) => 0, -1, 1)).Throws<Exception>();
        await Assert.That(() => list.BinarySearch(0, static (a, b) => 0, 0, list.Count + 1)).Throws<Exception>();
    }

    #endregion

    #region AddRange

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task AddRange_IEnumerable_Overloads(long capacity)
    {
        long itemsToAdd = Math.Max(0, Math.Min(capacity, 5));
        List<long> sequence = Enumerable.Range(0, (int)itemsToAdd).Select(i => (long)i).ToList();

        LargeList<long> listFromList = new(0);
        listFromList.AddRange(sequence);
        await Assert.That(listFromList.GetAll().SequenceEqual(sequence)).IsTrue();

        LargeList<long> listFromReadOnly = new(0);
        IReadOnlyList<long> readOnly = sequence.AsReadOnly();
        listFromReadOnly.AddRange(readOnly);
        await Assert.That(listFromReadOnly.GetAll().SequenceEqual(readOnly)).IsTrue();

        List<long> enumerableSource = sequence.Select(i => i + MarkerBase).ToList();
        LargeList<long> listFromEnumerable = new(0);
        listFromEnumerable.AddRange(enumerableSource.Select(i => i));
        await Assert.That(listFromEnumerable.GetAll().SequenceEqual(enumerableSource)).IsTrue();

        LargeList<long> validation = new(0);
        await Assert.That(() => validation.AddRange((IEnumerable<long>)null!)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task AddRange_LargeArray_And_Spans(long capacity)
    {
        long safeCount = Math.Min(capacity, Constants.MaxLargeCollectionCount - 1);
        LargeArray<long> arraySource = CreateSequentialArray(safeCount);
        LargeList<long> listSource = CreateSequentialList(safeCount);
        LargeArrayBuffer fallbackSource = new(arraySource);

        LargeList<long> targetFromArray = new(0);
        targetFromArray.AddRange(arraySource);
        await Assert.That(targetFromArray.GetAll().SequenceEqual(arraySource.GetAll())).IsTrue();

        LargeList<long> targetFromList = new(0);
        targetFromList.AddRange(listSource);
        await Assert.That(targetFromList.GetAll().SequenceEqual(listSource.GetAll())).IsTrue();

        LargeList<long> targetFromStub = new(0);
        targetFromStub.AddRange(fallbackSource);
        await Assert.That(targetFromStub.GetAll().SequenceEqual(fallbackSource.GetAll())).IsTrue();

        long offset = Math.Min(1, Math.Max(0, arraySource.Count - 1));
        long count = Math.Max(0, arraySource.Count - offset);
        if (count > 0)
        {
            LargeList<long> offsetTarget = new(0);
            offsetTarget.AddRange(arraySource, offset);
            await Assert.That(offsetTarget.GetAll().SequenceEqual(arraySource.GetAll(offset, arraySource.Count - offset))).IsTrue();

            LargeList<long> rangeTarget = new(0);
            rangeTarget.AddRange(arraySource, offset, count);
            await Assert.That(rangeTarget.GetAll().SequenceEqual(arraySource.GetAll(offset, count))).IsTrue();
        }

        ReadOnlyLargeSpan<long> arraySpan = new(arraySource, 0, arraySource.Count);
        LargeList<long> spanFromArrayTarget = new(0);
        spanFromArrayTarget.AddRange(arraySpan);
        await Assert.That(spanFromArrayTarget.GetAll().SequenceEqual(arraySource.GetAll())).IsTrue();

        ReadOnlyLargeSpan<long> listSpan = new(listSource, 0, listSource.Count);
        LargeList<long> spanFromListTarget = new(0);
        spanFromListTarget.AddRange(listSpan);
        await Assert.That(spanFromListTarget.GetAll().SequenceEqual(listSource.GetAll())).IsTrue();

        long[] raw = Enumerable.Range(0, (int)Math.Min(arraySource.Count, 8)).Select(i => (long)(MarkerBase + i)).ToArray();
        LargeList<long> rawTarget = new(0);
        rawTarget.AddRange(raw);
        await Assert.That(rawTarget.GetAll().SequenceEqual(raw)).IsTrue();

        if (raw.Length > 0)
        {
            int rawOffset = Math.Min(1, raw.Length - 1);
            int rawCount = raw.Length - rawOffset;

            LargeList<long> rawRangeTarget = new(0);
            rawRangeTarget.AddRange(raw, rawOffset, rawCount);
            await Assert.That(rawRangeTarget.GetAll().SequenceEqual(raw.Skip(rawOffset).Take(rawCount))).IsTrue();

            LargeList<long> rawSpanTarget = new(0);
            rawSpanTarget.AddRange(raw.AsSpan(rawOffset, rawCount));
            await Assert.That(rawSpanTarget.GetAll().SequenceEqual(raw.Skip(rawOffset).Take(rawCount))).IsTrue();
        }

        if (capacity >= Constants.MaxLargeCollectionCount)
        {
            LargeList<long> capacityGuard = CreateSequentialList(Constants.MaxLargeCollectionCount - 1);
            await Assert.That(() => capacityGuard.AddRange(new long[] { 1L })).Throws<Exception>();
        }

        LargeList<long> validation = new(1);
        await Assert.That(() => validation.AddRange((IReadOnlyLargeArray<long>)null!)).Throws<Exception>();
        await Assert.That(() => validation.AddRange(arraySource, -1)).Throws<Exception>();
        await Assert.That(() => validation.AddRange(arraySource, 0, arraySource.Count + 1)).Throws<Exception>();
        await Assert.That(() => validation.AddRange((long[])null!)).Throws<Exception>();
        await Assert.That(() => validation.AddRange(raw, -1, 1)).Throws<Exception>();
        await Assert.That(() => validation.AddRange(raw, raw.Length + 1, 1)).Throws<Exception>();
    }

    #endregion

    #region CopyFrom

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyFrom_Variants(long capacity)
    {
        LargeList<long> target = CreateSequentialList(capacity);
        LargeArray<long> arraySource = CreateSequentialArray(capacity);
        LargeList<long> listSource = CreateSequentialList(capacity);
        LargeArrayBuffer fallbackSource = new(arraySource);

        long copyCount = Math.Min(capacity, 5);
        long sourceOffset = 0;
        long targetOffset = capacity > copyCount ? capacity - copyCount : 0;

        if (copyCount > 0)
        {
            target.CopyFrom(arraySource, sourceOffset, targetOffset, copyCount);
            await VerifyRangeEquals(target, arraySource, targetOffset, sourceOffset, copyCount);

            target.CopyFrom(listSource, sourceOffset, targetOffset, copyCount);
            await VerifyRangeEquals(target, listSource, targetOffset, sourceOffset, copyCount);

            target.CopyFrom(fallbackSource, sourceOffset, targetOffset, copyCount);
            await VerifyRangeEquals(target, fallbackSource, targetOffset, sourceOffset, copyCount);
        }
        else
        {
            target.CopyFrom(arraySource, 0, 0, 0);
        }

        ReadOnlyLargeSpan<long> arraySpan = new(arraySource, 0, arraySource.Count);
        ReadOnlyLargeSpan<long> listSpan = new(listSource, 0, listSource.Count);

        if (capacity > 0)
        {
            long spanCount = Math.Min(capacity, 3);
            target.CopyFrom(arraySpan, 0, spanCount);
            await VerifyRangeEquals(target, arraySource, 0, 0, spanCount);

            target.CopyFrom(listSpan, 0, spanCount);
            await VerifyRangeEquals(target, listSource, 0, 0, spanCount);
        }

        long[] raw = Enumerable.Range(0, (int)Math.Min(capacity, 6)).Select(i => (long)(MarkerBase + i)).ToArray();
        if (target.Count > 0 && raw.Length > 0)
        {
            int rawCount = Math.Min(raw.Length, (int)Math.Min(target.Count, 6));
            target.CopyFromArray(raw, 0, 0, rawCount);
            await VerifyRangeEquals(target, raw, 0, 0, rawCount);

            target.CopyFromSpan(raw.AsSpan(0, rawCount), 0, rawCount);
            await VerifyRangeEquals(target, raw, 0, 0, rawCount);
        }

        LargeList<long> validation = CreateSequentialList(Math.Max(1, capacity));
        await Assert.That(() => validation.CopyFrom((IReadOnlyLargeArray<long>)null!, 0, 0, 1)).Throws<Exception>();
        await Assert.That(() => validation.CopyFrom(arraySource, -1, 0, 1)).Throws<Exception>();
        await Assert.That(() => validation.CopyFrom(arraySource, 0, -1, 1)).Throws<Exception>();
        await Assert.That(() => validation.CopyFrom(arraySource, 0, validation.Count, 1)).Throws<Exception>();
        await Assert.That(() => validation.CopyFrom(arraySource, arraySource.Count, 0, 1)).Throws<Exception>();
        await Assert.That(() => validation.CopyFrom(arraySource, 0, 0, validation.Count + 1)).Throws<Exception>();
        await Assert.That(() => validation.CopyFrom((ReadOnlyLargeSpan<long>)default, 0, 1)).Throws<Exception>();
        await Assert.That(() => validation.CopyFromArray((long[])null!, 0, 0, 1)).Throws<Exception>();
        await Assert.That(() => validation.CopyFromArray(raw, raw.Length + 1, 0, 1)).Throws<Exception>();
        await Assert.That(() => validation.CopyFromArray(raw, 0, -1, 1)).Throws<Exception>();
        await Assert.That(() => validation.CopyFromArray(raw, 0, validation.Count, 1)).Throws<Exception>();
        await Assert.That(() => validation.CopyFromSpan(raw.AsSpan(), -1, 1)).Throws<Exception>();
        await Assert.That(() => validation.CopyFromSpan(raw.AsSpan(), 0, raw.Length + 1)).Throws<Exception>();
    }

    #endregion

    #region CopyTo

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyTo_Variants(long capacity)
    {
        LargeList<long> source = CreateSequentialList(capacity);
        long safeTargetCount = Math.Min(Constants.MaxLargeCollectionCount, Math.Max(1, capacity + 3));
        LargeArray<long> arrayTarget = new(safeTargetCount);
        LargeList<long> listTarget = CreateSequentialList(Math.Min(Constants.MaxLargeCollectionCount - 1, safeTargetCount));
        LargeArrayBuffer bufferTarget = new(safeTargetCount);

        source.CopyTo(arrayTarget, 0, 0, 0);
        source.CopyTo(listTarget, 0, 0, 0);
        source.CopyTo(bufferTarget, 0, 0, 0);

        long copyCount = Math.Min(capacity, 4);
        long sourceOffset = capacity > copyCount ? 1 : 0;
        long arrayTargetOffset = 0;
        long listTargetOffset = 0;
        long bufferTargetOffset = 0;
        long secondaryOffset = copyCount > 1 ? 1 : 0;

        if (copyCount > 0)
        {
            source.CopyTo(arrayTarget, sourceOffset, arrayTargetOffset, copyCount);
            await VerifyRangeEquals(arrayTarget, source, arrayTargetOffset, sourceOffset, copyCount);

            source.CopyTo(listTarget, sourceOffset, listTargetOffset, copyCount);
            await VerifyRangeEquals(listTarget, source, listTargetOffset, sourceOffset, copyCount);

            source.CopyTo(bufferTarget, sourceOffset, bufferTargetOffset, copyCount);
            await VerifyRangeEquals(bufferTarget, source, bufferTargetOffset, sourceOffset, copyCount);

            long offsetValidationCount = Math.Min(copyCount, 3);
            if (offsetValidationCount > 0)
            {
                LargeList<long> offsetListTarget = CreateSequentialList(Math.Max(4, offsetValidationCount + 2));
                source.CopyTo(offsetListTarget, sourceOffset, secondaryOffset, offsetValidationCount);
                await VerifyRangeEquals(offsetListTarget, source, secondaryOffset, sourceOffset, offsetValidationCount);

                LargeArrayBuffer offsetBufferTarget = new(Math.Max(4, offsetValidationCount + 2));
                source.CopyTo(offsetBufferTarget, sourceOffset, secondaryOffset, offsetValidationCount);
                await VerifyRangeEquals(offsetBufferTarget, source, secondaryOffset, sourceOffset, offsetValidationCount);
            }
        }

        long spanLength = Math.Max(copyCount, 1);
        long spanOffset = Math.Min(secondaryOffset, spanLength == 0 ? 0 : spanLength - 1);
        LargeList<long> spanBacker = CreateSequentialList(Math.Max(spanLength + spanOffset, 1));
        LargeSpan<long> spanTarget = new(spanBacker, spanOffset, spanLength);
        source.CopyTo(spanTarget, sourceOffset, copyCount);
        if (copyCount > 0)
        {
            await VerifyRangeEquals(spanBacker, source, spanOffset, sourceOffset, copyCount);
        }

        int rawLength = Math.Max((int)copyCount + 3, 3);
        int rawOffset = copyCount > 0 ? 1 : 0;
        int rawCount = (int)copyCount;

        long[] arrayCopyTarget = Enumerable.Range(0, rawLength).Select(i => (long)(MarkerBase + i)).ToArray();
        source.CopyToArray(arrayCopyTarget, sourceOffset, rawOffset, rawCount);
        await VerifyRangeEquals(arrayCopyTarget, source, rawOffset, sourceOffset, rawCount);

        long[] spanCopyTarget = Enumerable.Range(0, rawLength).Select(i => (long)(MarkerBase * 2 + i)).ToArray();
        Span<long> rawSpan = spanCopyTarget.AsSpan(rawOffset);
        source.CopyToSpan(rawSpan, sourceOffset, rawCount);
        await VerifyRangeEquals(spanCopyTarget, source, rawOffset, sourceOffset, rawCount);

        LargeList<long> guardSource = CreateSequentialList(3);
        LargeArray<long> guardArray = new(4);
        LargeList<long> guardList = CreateSequentialList(4);
        LargeArrayBuffer guardBuffer = new(4);
        long[] guardRawArray = new long[4];

        await Assert.That(() => guardSource.CopyTo((ILargeArray<long>)null!, 0, 0, 1)).Throws<Exception>();
        await Assert.That(() => guardSource.CopyTo(guardArray, -1, 0, 1)).Throws<Exception>();
        await Assert.That(() => guardSource.CopyTo(guardArray, 0, -1, 1)).Throws<Exception>();
        await Assert.That(() => guardSource.CopyTo(guardArray, guardSource.Count, 0, 1)).Throws<Exception>();
        await Assert.That(() => guardSource.CopyTo(guardArray, 0, guardArray.Count, 1)).Throws<Exception>();
        await Assert.That(() => guardSource.CopyTo(guardList, 0, guardList.Count, 1)).Throws<Exception>();
        await Assert.That(() => guardSource.CopyTo(guardBuffer, 0, guardBuffer.Count, 1)).Throws<Exception>();
        await Assert.That(() => guardSource.CopyTo(default, 0, 1)).Throws<Exception>();
        await Assert.That(() => guardSource.CopyToArray((long[])null!, 0, 0, 1)).Throws<Exception>();
        await Assert.That(() => guardSource.CopyToArray(guardRawArray, guardSource.Count, 0, 1)).Throws<Exception>();
        await Assert.That(() => guardSource.CopyToArray(guardRawArray, 0, guardRawArray.Length, 1)).Throws<Exception>();
        await Assert.That(() => guardSource.CopyToSpan(guardRawArray.AsSpan(), -1, 1)).Throws<Exception>();
        await Assert.That(() => guardSource.CopyToSpan(guardRawArray.AsSpan(), 0, guardRawArray.Length + 1)).Throws<Exception>();
    }

    #endregion

    #region Removal / Capacity Management

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Remove_Overloads(long capacity)
    {
        LargeList<long> missingList = CreateSequentialList(capacity);
        bool missingRemoved = missingList.Remove(MarkerBase);
        await Assert.That(missingRemoved).IsFalse();

        LargeList<long> missingOutList = CreateSequentialList(capacity);
        bool missingWithOut = missingOutList.Remove(MarkerBase, out long missingOutResult);
        await Assert.That(missingWithOut).IsFalse();
        await Assert.That(missingOutResult).IsEqualTo(default(long));

        if (capacity == 0)
        {
            return;
        }

        long index = Math.Min(1, capacity - 1);
        long targetValue = index;

        LargeList<long> defaultRemove = CreateSequentialList(capacity);
        bool removedDefault = defaultRemove.Remove(targetValue);
        await Assert.That(removedDefault).IsTrue();
        await Assert.That(defaultRemove.Count).IsEqualTo(capacity - 1);
        await Assert.That(defaultRemove.IndexOf(targetValue)).IsEqualTo(-1L);

        LargeList<long> preserveFalseList = CreateSequentialList(capacity);
        bool removedNoOrder = preserveFalseList.Remove(targetValue, preserveOrder: false);
        await Assert.That(removedNoOrder).IsTrue();
        await Assert.That(preserveFalseList.Count).IsEqualTo(capacity - 1);
        if (capacity > 1 && index < preserveFalseList.Count)
        {
            await Assert.That(preserveFalseList[index]).IsEqualTo(capacity - 1);
        }

        LargeList<long> withOutList = CreateSequentialList(capacity);
        bool removedWithOut = withOutList.Remove(targetValue, out long removedItem);
        await Assert.That(removedWithOut).IsTrue();
        await Assert.That(removedItem).IsEqualTo(targetValue);

        LargeList<long> preserveOutList = CreateSequentialList(capacity);
        bool removedPreserveOut = preserveOutList.Remove(targetValue, preserveOrder: true, out long preserveOutItem);
        await Assert.That(removedPreserveOut).IsTrue();
        await Assert.That(preserveOutItem).IsEqualTo(targetValue);
        if (index < preserveOutList.Count)
        {
            await Assert.That(preserveOutList[index]).IsEqualTo(targetValue + 1);
        }

        LargeList<long> customEqualsList = CreateSequentialList(capacity);
        long searchValue = targetValue + MarkerBase;
        bool removedCustom = customEqualsList.Remove(searchValue, preserveOrder: true, out long customRemoved, static (long stored, long search) => stored == search - MarkerBase);
        await Assert.That(removedCustom).IsTrue();
        await Assert.That(customRemoved).IsEqualTo(targetValue);

        LargeList<long> equalsOnlyList = CreateSequentialList(capacity);
        bool removedWithEqualsOnly = equalsOnlyList.Remove(searchValue, static (long stored, long search) => stored == search - MarkerBase);
        await Assert.That(removedWithEqualsOnly).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task RemoveAt_Variants(long capacity)
    {
        LargeList<long> empty = new(0);
        await Assert.That(() => empty.RemoveAt(0)).Throws<Exception>();
        await Assert.That(() => empty.RemoveAt(0, false)).Throws<Exception>();

        LargeList<long> single = CreateSequentialList(1);
        await Assert.That(() => single.RemoveAt(-1)).Throws<Exception>();

        if (capacity == 0)
        {
            return;
        }

        long index = Math.Min(1, capacity - 1);
        LargeList<long> preserveList = CreateSequentialList(capacity);
        long removedPreserve = preserveList.RemoveAt(index);
        await Assert.That(removedPreserve).IsEqualTo(index);
        await Assert.That(preserveList.Count).IsEqualTo(capacity - 1);
        if (index < preserveList.Count)
        {
            await Assert.That(preserveList[index]).IsEqualTo(index + 1);
        }

        LargeList<long> noOrderList = CreateSequentialList(capacity);
        long removedNoOrder = noOrderList.RemoveAt(index, preserveOrder: false);
        await Assert.That(removedNoOrder).IsEqualTo(index);
        await Assert.That(noOrderList.Count).IsEqualTo(capacity - 1);
        if (capacity > 1 && index < noOrderList.Count)
        {
            await Assert.That(noOrderList[index]).IsEqualTo(capacity - 1);
        }
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Clear_EnsureCapacity_And_Shrink_Work(long capacity)
    {
        LargeList<long> list = CreateSequentialList(capacity);
        list.Clear();
        await Assert.That(list.Count).IsEqualTo(0L);
        await Assert.That(list.Capacity).IsEqualTo(1L);
        await Assert.That(list.GetAll().Any()).IsFalse();

        list.Add(42);
        long initialCapacity = list.Capacity;
        long targetCapacity = Math.Max(initialCapacity + 3, 8);
        list.EnsureCapacity(targetCapacity);
        await Assert.That(list.Capacity).IsGreaterThanOrEqualTo(targetCapacity);

        long remaining = 5;
        long expectedCapacity = list.Count + remaining;
        list.EnsureRemainingCapacity(remaining);
        await Assert.That(list.Capacity).IsGreaterThanOrEqualTo(expectedCapacity);

        await Assert.That(() => list.EnsureCapacity(-1)).Throws<Exception>();
        await Assert.That(() => list.EnsureCapacity(Constants.MaxLargeCollectionCount + 1)).Throws<Exception>();
        await Assert.That(() => list.EnsureRemainingCapacity(-1)).Throws<Exception>();
        await Assert.That(() => list.EnsureRemainingCapacity(Constants.MaxLargeCollectionCount + 1)).Throws<Exception>();

        list.EnsureCapacity(list.Capacity + 5);
        list.Shrink();
        await Assert.That(list.Capacity).IsEqualTo(list.Count);

        long autoSize = Math.Min(Math.Max(capacity, 4), 32);
        LargeList<long> autoShrink = new(autoSize, 2.0, Constants.DefaultFixedCapacityGrowAmount, Constants.DefaultFixedCapacityGrowLimit, 0.8);
        for (long i = 0; i < autoSize; i++)
        {
            autoShrink.Add(i);
        }
        autoShrink.EnsureCapacity(autoSize * 2);
        long expanded = autoShrink.Capacity;
        autoShrink.RemoveAt(autoShrink.Count - 1);
        await Assert.That(autoShrink.Capacity).IsEqualTo(autoShrink.Count);
        await Assert.That(autoShrink.Capacity).IsLessThanOrEqualTo(expanded);
    }

    #endregion

    #region Set / Enumeration

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Set_GetAll_And_Enumeration(long capacity)
    {
        LargeList<long> list = CreateSequentialList(capacity);
        if (list.Count > 0)
        {
            long index = Math.Min(1, list.Count - 1);
            list.Set(index, MarkerBase);
            await Assert.That(list[index]).IsEqualTo(MarkerBase);
        }

        List<long> enumerated = list.ToList();
        await Assert.That(list.GetAll().SequenceEqual(enumerated)).IsTrue();

        long offset = Math.Min(1, Math.Max(0, list.Count - 1));
        long count = Math.Max(0, list.Count - offset);
        IEnumerable<long> range = list.GetAll(offset, count);
        await Assert.That(range.SequenceEqual(enumerated.Skip((int)offset).Take((int)count))).IsTrue();

        using IEnumerator<long> enumerator = list.GetEnumerator();
        List<long> manual = new();
        while (enumerator.MoveNext())
        {
            manual.Add(enumerator.Current);
        }
        await Assert.That(manual.SequenceEqual(enumerated)).IsTrue();

        System.Collections.IEnumerator nonGeneric = ((System.Collections.IEnumerable)list).GetEnumerator();
        List<long> nonGenericItems = new();
        while (nonGeneric.MoveNext())
        {
            nonGenericItems.Add((long)nonGeneric.Current);
        }
        await Assert.That(nonGenericItems.SequenceEqual(enumerated)).IsTrue();

        await Assert.That(() => list.Set(-1, 0)).Throws<Exception>();
        await Assert.That(() => list.Set(list.Count, 0)).Throws<Exception>();
        await Assert.That(() => list.GetAll(-1, 1).ToList()).Throws<Exception>();
        await Assert.That(() => list.GetAll(0, list.Count + 1).ToList()).Throws<Exception>();
    }

    #endregion

    #region DoForEach

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task DoForEach_Overloads(long capacity)
    {
        LargeList<long> list = CreateSequentialList(capacity);
        long sum = 0;
        list.DoForEach(item => sum += item);
        long expectedSum = list.GetAll().Sum();
        await Assert.That(sum).IsEqualTo(expectedSum);

        long offset = Math.Min(1, Math.Max(0, list.Count - 1));
        long count = Math.Max(0, list.Count - offset);
        long rangedSum = 0;
        list.DoForEach(item => rangedSum += item, offset, count);
        long expectedRangeSum = list.GetAll(offset, count).Sum();
        await Assert.That(rangedSum).IsEqualTo(expectedRangeSum);

        long userSum = 0;
        list.DoForEach(static (long value, ref long acc) => acc += value, ref userSum);
        await Assert.That(userSum).IsEqualTo(expectedSum);

        long userRangeSum = 0;
        list.DoForEach(static (long value, ref long acc) => acc += value, offset, count, ref userRangeSum);
        await Assert.That(userRangeSum).IsEqualTo(expectedRangeSum);

        LargeList<long> refList = CreateSequentialList(capacity);
        refList.DoForEach(static (ref long value) => value += MarkerBase);
        long[] refExpected = Enumerable.Range(0, (int)refList.Count).Select(i => (long)i + MarkerBase).ToArray();
        await Assert.That(refList.GetAll().SequenceEqual(refExpected)).IsTrue();

        LargeList<long> refRangeList = CreateSequentialList(capacity);
        long[] before = refRangeList.GetAll().ToArray();
        refRangeList.DoForEach(static (ref long value) => value += MarkerBase, offset, count);
        long[] after = refRangeList.GetAll().ToArray();
        for (long i = 0; i < after.LongLength; i++)
        {
            long expected = before[i];
            if (i >= offset && i < offset + count)
            {
                expected += MarkerBase;
            }
            await Assert.That(after[i]).IsEqualTo(expected);
        }

        LargeList<long> refUserList = CreateSequentialList(capacity);
        long increment = 2;
        refUserList.DoForEach(static (ref long value, ref long add) => value += add, ref increment);
        long[] refUserExpected = Enumerable.Range(0, (int)refUserList.Count).Select(i => (long)i + increment).ToArray();
        await Assert.That(refUserList.GetAll().SequenceEqual(refUserExpected)).IsTrue();

        LargeList<long> refUserRangeList = CreateSequentialList(capacity);
        long delta = 3;
        long[] rangeBefore = refUserRangeList.GetAll().ToArray();
        refUserRangeList.DoForEach(static (ref long value, ref long add) => value += add, offset, count, ref delta);
        long[] rangeAfter = refUserRangeList.GetAll().ToArray();
        for (long i = 0; i < rangeAfter.LongLength; i++)
        {
            long expected = rangeBefore[i];
            if (i >= offset && i < offset + count)
            {
                expected += delta;
            }
            await Assert.That(rangeAfter[i]).IsEqualTo(expected);
        }
    }

    [Test]
    public async Task DoForEach_ThrowsOnNullActions()
    {
        LargeList<long> list = CreateSequentialList(2);
        await Assert.That(() => list.DoForEach((Action<long>)null!)).Throws<Exception>();
        await Assert.That(() => list.DoForEach((Action<long>)null!, 0, 1)).Throws<Exception>();

        long user = 0;
        await Assert.That(() => list.DoForEach((ActionWithUserData<long, long>)null!, ref user)).Throws<Exception>();
        await Assert.That(() => list.DoForEach((ActionWithUserData<long, long>)null!, 0, 1, ref user)).Throws<Exception>();

        await Assert.That(() => list.DoForEach((RefAction<long>)null!)).Throws<Exception>();
        await Assert.That(() => list.DoForEach((RefAction<long>)null!, 0, 1)).Throws<Exception>();

        await Assert.That(() => list.DoForEach((RefActionWithUserData<long, long>)null!, ref user)).Throws<Exception>();
        await Assert.That(() => list.DoForEach((RefActionWithUserData<long, long>)null!, 0, 1, ref user)).Throws<Exception>();
    }

    #endregion

    #region Helpers

    private static LargeList<long> CreateSequentialList(long count)
    {
        if (count < 0 || count > Constants.MaxLargeCollectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        LargeList<long> list = new(count);
        for (long i = 0; i < count; i++)
        {
            list.Add(i);
        }

        return list;
    }

    private static LargeArray<long> CreateSequentialArray(long count)
    {
        if (count < 0 || count > Constants.MaxLargeCollectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        LargeArray<long> array = new(count);
        for (long i = 0; i < count; i++)
        {
            array[i] = i;
        }

        return array;
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

    #endregion
}

