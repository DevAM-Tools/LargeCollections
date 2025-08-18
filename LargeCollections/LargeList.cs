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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LargeCollections;

/// <summary>
/// A mutable list of <typeparamref name="T"/> that can store up to <see cref="Constants.MaxLargeCollectionCount"/> elements.
/// Lists allow index based access to the elements.
/// </summary>
[DebuggerDisplay("LargeList: Count = {Count}")]
public class LargeList<T> : ILargeList<T>
{
    private T[][] _Storage;
    public double CapacityGrowFactor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    public long FixedCapacityGrowAmount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    public long FixedCapacityGrowLimit
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    public long Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    public long Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    public LargeList(long capacity = 1L,
        double capacityGrowFactor = Constants.DefaultCapacityGrowFactor,
        long fixedCapacityGrowAmount = Constants.DefaultFixedCapacityGrowAmount,
        long fixedCapacityGrowLimit = Constants.DefaultFixedCapacityGrowLimit)
    {
        if (capacityGrowFactor <= 1.0 || capacityGrowFactor > Constants.MaxCapacityGrowFactor)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityGrowFactor));
        }
        if (fixedCapacityGrowAmount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedCapacityGrowAmount));
        }
        if (fixedCapacityGrowLimit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedCapacityGrowLimit));
        }

        _Storage = StorageExtensions.StorageCreate<T>(capacity);
        Count = 0L;
        Capacity = capacity;

        CapacityGrowFactor = capacityGrowFactor;

        FixedCapacityGrowAmount = fixedCapacityGrowAmount;

        FixedCapacityGrowLimit = fixedCapacityGrowLimit;
    }

    public LargeList(IEnumerable<T> items,
        long capacity = 1L,
        double capacityGrowFactor = Constants.DefaultCapacityGrowFactor,
        long fixedCapacityGrowAmount = Constants.DefaultFixedCapacityGrowAmount,
        long fixedCapacityGrowLimit = Constants.DefaultFixedCapacityGrowLimit)

        : this(capacity, capacityGrowFactor, fixedCapacityGrowAmount, fixedCapacityGrowLimit)
    {
        AddRange(items);
    }

    public T this[long index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            StorageExtensions.CheckIndex(index, Count);
            T result = _Storage.StorageGet(index);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            StorageExtensions.CheckIndex(index, Count);
            _Storage.StorageSet(index, value);
        }
    }

    T IReadOnlyLargeArray<T>.this[long index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            StorageExtensions.CheckIndex(index, Count);
            T result = _Storage.StorageGet(index);
            return result;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (Count >= Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }

        EnsureRemainingCapacity(1L);
        _Storage.StorageSet(Count, item);
        Count++;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IEnumerable<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }
        if (items is IReadOnlyLargeArray<T> largeArray)
        {
            AddRange(largeArray, 0, largeArray.Count);
        }
        else if (items is T[] array)
        {
            AddRange(array, 0, array.Length);
        }
        else if (items is IReadOnlyList<T> list)
        {
            if (list.Count == 0)
            {
                return;
            }

            if (Count + list.Count >= Constants.MaxLargeCollectionCount)
            {
                throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
            }

            EnsureRemainingCapacity(list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                _Storage.StorageSet(Count + i, list[i]);
            }
            Count += list.Count;
        }
        else
        {
            foreach (T item in items)
            {
                Add(item);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyLargeArray<T> source, long offset, long count)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (Count + count >= Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }

        StorageExtensions.CheckRange(offset, count, source.Count);

        if (count == 0L)
        {
            return;
        }

        EnsureRemainingCapacity(count);

        if (source is LargeArray<T> largeArraySource)
        {
            T[][] sourceStorage = largeArraySource.GetStorage();
            _Storage.StorageCopyFrom(sourceStorage, offset, Count, count);
        }
        else if (source is LargeList<T> largeListSource)
        {
            T[][] sourceStorage = largeListSource.GetStorage();
            _Storage.StorageCopyFrom(sourceStorage, offset, Count, count);
        }
        else
        {
            for (long i = 0L; i < count; i++)
            {
                T item = source[offset + i];
                _Storage.StorageSet(Count + i, item);
            }
        }

        Count += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(T[] source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        AddRange(source, 0, source.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(T[] source, int offset, int count)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        StorageExtensions.CheckRange(offset, count, source.Length);

        if (Count + count >= Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }

        EnsureRemainingCapacity(count);

        _Storage.StorageCopyFromArray(source, offset, Count, count);
        Count += count;
    }

#if NETSTANDARD2_1_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlySpan<T> source)
    {
        if (source.Length == 0)
        {
            return;
        }

        int count = source.Length;
        if (Count + count >= Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }

        EnsureRemainingCapacity(count);

        _Storage.StorageCopyFromSpan(source, Count, count);
        Count += count;
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Resize(1L);
        Count = 0L;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        bool result = _Storage.Contains(item, 0L, Count, LargeSet<T>.DefaultEqualsFunction);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item, long offset, long count)
    {
        StorageExtensions.CheckRange(offset, count, Count);

        if (count == 0L)
        {
            return false;
        }

        bool result = _Storage.Contains(item, offset, count, LargeSet<T>.DefaultEqualsFunction);
        return result;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(IReadOnlyLargeArray<T> source, long sourceOffset, long targetOffset, long count)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        StorageExtensions.CheckRange(sourceOffset, count, source.Count);
        StorageExtensions.CheckRange(targetOffset, count, Count);

        if (count == 0L)
        {
            return;
        }

        if (source is LargeArray<T> largeArraySource)
        {
            T[][] sourceStorage = largeArraySource.GetStorage();
            _Storage.StorageCopyFrom(sourceStorage, sourceOffset, targetOffset, count);
        }
        else if (source is LargeList<T> largeListSource)
        {
            T[][] sourceStorage = largeListSource.GetStorage();
            _Storage.StorageCopyFrom(sourceStorage, sourceOffset, targetOffset, count);
        }
        else
        {
            for (long i = 0L; i < count; i++)
            {
                T item = source[sourceOffset + i];
                _Storage.StorageSet(targetOffset + i, item);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFromArray(T[] source, int sourceOffset, long targetOffset, int count)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        StorageExtensions.CheckRange(sourceOffset, count, source.Length);
        StorageExtensions.CheckRange(targetOffset, count, Count);

        if (count == 0L)
        {
            return;
        }

        _Storage.StorageCopyFromArray(source, sourceOffset, targetOffset, count);
    }
#if NETSTANDARD2_1_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFromSpan(ReadOnlySpan<T> source, long targetOffset, int count)
    {
        StorageExtensions.CheckRange(0, count, source.Length);
        StorageExtensions.CheckRange(targetOffset, count, Count);

        if (count == 0L)
        {
            return;
        }

        _Storage.StorageCopyFromSpan(source, targetOffset, count);
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(ILargeArray<T> target, long sourceOffset, long targetOffset, long count)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        StorageExtensions.CheckRange(targetOffset, count, target.Count);
        StorageExtensions.CheckRange(sourceOffset, count, Count);

        if (count == 0L)
        {
            return;
        }

        if (target is LargeArray<T> largeArrayTarget)
        {
            T[][] targetStorage = largeArrayTarget.GetStorage();
            _Storage.StorageCopyTo(targetStorage, sourceOffset, targetOffset, count);
        }
        else if (target is LargeList<T> largeListTarget)
        {
            T[][] targetStorage = largeListTarget.GetStorage();
            _Storage.StorageCopyTo(targetStorage, sourceOffset, targetOffset, count);
        }
        else
        {
            for (long i = 0L; i < count; i++)
            {
                T item = _Storage.StorageGet(sourceOffset + i);
                target[targetOffset + i] = item;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyToArray(T[] target, long sourceOffset, int targetOffset, int count)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        StorageExtensions.CheckRange(targetOffset, count, target.Length);
        StorageExtensions.CheckRange(sourceOffset, count, Count);

        if (count == 0L)
        {
            return;
        }

        _Storage.StorageCopyToArray(target, sourceOffset, targetOffset, count);
    }


#if NETSTANDARD2_1_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyToSpan(Span<T> target, long sourceOffset, int count)
    {
        StorageExtensions.CheckRange(0, count, target.Length);
        StorageExtensions.CheckRange(sourceOffset, count, Count);

        if (count == 0L)
        {
            return;
        }

        _Storage.StorageCopyToSpan(target, sourceOffset, count);
    }
#endif


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(long index)
    {
        StorageExtensions.CheckIndex(index, Count);
        T result = _Storage.StorageGet(index);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef(long index)
    {
        StorageExtensions.CheckIndex(index, Count);
        ref T result = ref _Storage.StorageGetRef(index);
        return ref result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll()
    {
        foreach (T item in _Storage.StorageGetAll(0L, Count))
        {
            yield return item;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll(long offset, long count)
    {
        StorageExtensions.CheckRange(offset, count, Count);

        foreach (T item in _Storage.StorageGetAll(offset, count))
        {
            yield return item;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(T item)
    {
        for (long i = 0L; i < Count; i++)
        {
            T currentItem = _Storage.StorageGet(i);
            if (LargeSet<T>.DefaultEqualsFunction(item, currentItem))
            {
                RemoveAt(i);
                break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            Remove(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAt(long index)
    {
        StorageExtensions.CheckIndex(index, Count);

        for (long i = index; i < Count - 1L; i++)
        {
            T item = _Storage.StorageGet(i + 1L);
            _Storage.StorageSet(i, item);
        }

        _Storage.StorageSet(Count - 1L, default);
        Count--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(long index, T item)
    {
        StorageExtensions.CheckIndex(index, Count);
        _Storage.StorageSet(index, item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Shrink()
    {
        Resize(Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<T> GetEnumerator()
    {
        return GetAll().GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetAll().GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        bool dummy = false;
        _Storage.StorageDoForEach(static (ref T item, ref Action<T> action, ref bool dummy) => action.Invoke(item),
            0L, Count, ref action, ref dummy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action, long offset, long count)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        StorageExtensions.CheckRange(offset, count, Count);
        bool dummy = false;
        _Storage.StorageDoForEach(static (ref T item, ref Action<T> action, ref bool dummy) => action.Invoke(item),
            offset, count, ref action, ref dummy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(ActionWithUserData<T, TUserData> action, ref TUserData userData)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        _Storage.StorageDoForEach(static (ref T item, ref ActionWithUserData<T, TUserData> action, ref TUserData userData) => action.Invoke(item, ref userData),
            0L, Count, ref action, ref userData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(ActionWithUserData<T, TUserData> action, long offset, long count, ref TUserData userData)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        StorageExtensions.CheckRange(offset, count, Count);
        _Storage.StorageDoForEach(static (ref T item, ref ActionWithUserData<T, TUserData> action, ref TUserData userData) => action.Invoke(item, ref userData),
            offset, count, ref action, ref userData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(RefAction<T> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        bool dummy = false;
        _Storage.StorageDoForEach(static (ref T item, ref RefAction<T> action, ref bool dummy) => action.Invoke(ref item),
            0L, Count, ref action, ref dummy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(RefAction<T> action, long offset, long count)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        StorageExtensions.CheckRange(offset, count, Count);
        bool dummy = false;
        _Storage.StorageDoForEach(static (ref T item, ref RefAction<T> action, ref bool dummy) => action.Invoke(ref item),
            offset, count, ref action, ref dummy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(RefActionWithUserData<T, TUserData> action, ref TUserData userData)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        _Storage.StorageDoForEach(static (ref T item, ref RefActionWithUserData<T, TUserData> action, ref TUserData userData) => action.Invoke(ref item, ref userData),
            0L, Count, ref action, ref userData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(RefActionWithUserData<T, TUserData> action, long offset, long count, ref TUserData userData)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        StorageExtensions.CheckRange(offset, count, Count);
        _Storage.StorageDoForEach(static (ref T item, ref RefActionWithUserData<T, TUserData> action, ref TUserData userData) => action.Invoke(ref item, ref userData),
            offset, count, ref action, ref userData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Sort(Func<T, T, int> comparer)
    {
        comparer ??= LargeArray<T>.DefaultComparer;
        _Storage.StorageSort(comparer, 0L, Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Sort(Func<T, T, int> comparer, long offset, long count)
    {
        StorageExtensions.CheckRange(offset, count, Count);
        comparer ??= LargeArray<T>.DefaultComparer;
        _Storage.StorageSort(comparer, offset, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch(T item, Func<T, T, int> comparer)
    {
        comparer ??= LargeArray<T>.DefaultComparer;
        long result = _Storage.StorageBinarySearch(item, comparer, 0L, Count);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch(T item, Func<T, T, int> comparer, long offset, long count)
    {
        StorageExtensions.CheckRange(offset, count, Count);
        comparer ??= LargeArray<T>.DefaultComparer;
        long result = _Storage.StorageBinarySearch(item, comparer, offset, count);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Swap(long leftIndex, long rightIndex)
    {
        StorageExtensions.CheckIndex(leftIndex, Count);
        StorageExtensions.CheckIndex(rightIndex, Count);
        _Storage.StorageSwap(leftIndex, rightIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T[][] GetStorage()
    {
        T[][] result = _Storage;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureCapacity(long targetCapacity)
    {
        if (targetCapacity < 0L || targetCapacity > Constants.MaxLargeCollectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(targetCapacity));
        }
        if (Capacity >= targetCapacity)
        {
            return;
        }

        long newCapacity = Capacity;

        while (newCapacity < targetCapacity)
        {
            newCapacity = StorageExtensions.GetGrownCapacity(newCapacity, CapacityGrowFactor, FixedCapacityGrowAmount, FixedCapacityGrowLimit);
        }

        if (newCapacity > Constants.MaxLargeCollectionCount)
        {
            newCapacity = Constants.MaxLargeCollectionCount;
        }

        Resize(newCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureRemainingCapacity(long capacity)
    {
        if(capacity < 0L || capacity > Constants.MaxLargeCollectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }
        long newCapacity = Count + capacity;
        EnsureCapacity(newCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Resize(long capacity)
    {
        _Storage = _Storage.StorageResize(capacity);
        Capacity = capacity;
    }
}
