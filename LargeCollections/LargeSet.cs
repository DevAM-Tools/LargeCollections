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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LargeCollections;

/// <summary>
/// A mutable set of <typeparamref name="T"/> that can store up to <see cref="Constants.MaxLargeCollectionCount"/> elements.
/// Sets are hash based. This version uses a struct comparer type parameter for maximum JIT inlining performance.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
/// <typeparam name="TComparer">The type of equality comparer. Use a struct implementing <see cref="IEqualityComparer{T}"/> for best performance.</typeparam>
[DebuggerDisplay("LargeSet: Count = {Count}")]
public class LargeSet<T, TComparer> : ILargeCollection<T> where TComparer : IEqualityComparer<T>
{
    private SetElement<T>[][] _Storage = null;
    private long _Capacity = 0L;
    private TComparer _comparer;

    /// <summary>
    /// Gets the equality comparer used by this set.
    /// </summary>
    public TComparer Comparer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _comparer;
    }

    public double CapacityGrowFactor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected set;
    }

    public long FixedCapacityGrowAmount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected set;
    }

    public long FixedCapacityGrowLimit
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected set;
    }

    public long Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected set;
    }

    public long Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Capacity;
    }

    public readonly double MinLoadFactor;
    public readonly double MaxLoadFactor;
    public readonly double MinLoadFactorTolerance;

    public double LoadFactor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (double)Count / (double)Capacity;
    }

    /// <summary>
    /// Creates a new LargeSet with the specified comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer to use. For best performance, use a struct type.</param>
    /// <param name="capacity">Initial bucket capacity.</param>
    /// <param name="capacityGrowFactor">Factor by which capacity grows when needed.</param>
    /// <param name="fixedCapacityGrowAmount">Fixed amount to grow capacity by for small sets.</param>
    /// <param name="fixedCapacityGrowLimit">Capacity limit below which fixed growth is used.</param>
    /// <param name="minLoadFactor">Minimum load factor before shrinking.</param>
    /// <param name="maxLoadFactor">Maximum load factor before growing.</param>
    /// <param name="minLoadFactorTolerance">Tolerance for minimum load factor.</param>
    public LargeSet(TComparer comparer,
        long capacity = 1L,
        double capacityGrowFactor = Constants.DefaultCapacityGrowFactor,
        long fixedCapacityGrowAmount = Constants.DefaultFixedCapacityGrowAmount,
        long fixedCapacityGrowLimit = Constants.DefaultFixedCapacityGrowLimit,
        double minLoadFactor = Constants.DefaultMinLoadFactor,
        double maxLoadFactor = Constants.DefaultMaxLoadFactor,
        double minLoadFactorTolerance = Constants.DefaultMinLoadFactorTolerance)
    {
        if (comparer is null)
        {
            throw new ArgumentNullException(nameof(comparer));
        }
        if (capacity <= 0L || capacity > Constants.MaxLargeCollectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }
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
        if (minLoadFactor < 0.0 || minLoadFactor >= maxLoadFactor)
        {
            throw new ArgumentOutOfRangeException(nameof(minLoadFactor));
        }
        if (maxLoadFactor < 0.0 || maxLoadFactor <= minLoadFactor)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLoadFactor));
        }
        if (minLoadFactorTolerance < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(minLoadFactorTolerance));
        }

        _comparer = comparer;

        _Storage = StorageExtensions.StorageCreate<SetElement<T>>(capacity);
        _Capacity = capacity;

        Count = 0L;

        CapacityGrowFactor = capacityGrowFactor;
        FixedCapacityGrowAmount = fixedCapacityGrowAmount;
        FixedCapacityGrowLimit = fixedCapacityGrowLimit;

        MinLoadFactor = minLoadFactor;
        MaxLoadFactor = maxLoadFactor;
        MinLoadFactorTolerance = minLoadFactorTolerance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        long count = Count;
        long previousCount = count;

        LargeSetHelpers.AddToStorage(ref item, _Storage, _Capacity, ref count, ref _comparer);

        if (count > previousCount && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }

        Count = count;
        Extend();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IEnumerable<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (items is IReadOnlyList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Add(list[i]);
            }
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
    public void AddRange(IReadOnlyLargeArray<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        AddRange(items, 0L, items.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyLargeArray<T> items, long offset)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        AddRange(items, offset, items.Count - offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyLargeArray<T> items, long offset, long count)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        StorageExtensions.CheckRange(offset, count, items.Count);
        for (long i = 0L; i < count; i++)
        {
            Add(items[offset + i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlyLargeSpan<T> items)
    {
        for (long i = 0L; i < items.Count; i++)
        {
            Add(items[i]);
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlySpan<T> items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            Add(items[i]);
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T item)
        => Remove(item, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T item, out T removedItem)
    {
        long count = Count;
        bool result = LargeSetHelpers.RemoveFromStorage(ref item, _Storage, Capacity, ref count, ref _comparer, out removedItem);
        Count = count;

        if (result)
        {
            Shrink();
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        long count = Count;
        LargeSetHelpers.ClearStorage(_Storage, _Capacity, ref count);
        Count = count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(T item, out T value)
    {
        return LargeSetHelpers.TryGetValueFromStorage(ref item, _Storage, Capacity, ref _comparer, out value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOrSetDefault(T item, out T value)
    {
        long count = Count;
        bool found = LargeSetHelpers.TryGetOrAddToStorage(ref item, ref item, _Storage, _Capacity, ref count, ref _comparer, out value);

        if (!found && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }

        Count = count;

        if (!found)
        {
            Extend();
        }

        return found;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOrSet(T searchItem, T valueIfNotFound, out T value)
    {
        long count = Count;
        bool found = LargeSetHelpers.TryGetOrAddToStorage(ref searchItem, ref valueIfNotFound, _Storage, _Capacity, ref count, ref _comparer, out value);

        if (!found && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }

        Count = count;

        if (!found)
        {
            Extend();
        }

        return found;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOrSet(T searchItem, Func<T> valueFactory, out T value)
    {
        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        long count = Count;
        bool found = LargeSetHelpers.TryGetOrAddToStorageWithFactory(ref searchItem, valueFactory, _Storage, _Capacity, ref count, ref _comparer, out value);

        if (!found && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }

        Count = count;

        if (!found)
        {
            Extend();
        }

        return found;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        return LargeSetHelpers.ContainsInStorage(ref item, _Storage, Capacity, ref _comparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll()
    {
        return LargeSetHelpers.GetAllFromStorage(_Storage, _Capacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<T> GetEnumerator()
    {
        return GetAll().GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Extend()
    {
        long count = Count;
        long capacity = _Capacity;
        LargeSetHelpers.ExtendStorage(ref _Storage, ref capacity, ref count, CapacityGrowFactor, FixedCapacityGrowAmount, FixedCapacityGrowLimit, MaxLoadFactor, ref _comparer);
        _Capacity = capacity;
        Count = count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Shrink()
    {
        long count = Count;
        long capacity = _Capacity;
        LargeSetHelpers.ShrinkStorage(ref _Storage, ref capacity, ref count, MinLoadFactor, MinLoadFactorTolerance, ref _comparer);
        _Capacity = capacity;
        Count = count;
    }

    #region DoForEach Methods

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the set.
    /// </summary>
    /// <param name="action">The function that will be called for each item.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action)
    {
        LargeSetHelpers.DoForEachInStorage(_Storage, _Capacity, action);
    }

    /// <summary>
    /// Performs the action on items using an action for optimal performance.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeAction{T}"/>.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<T>
    {
        LargeSetHelpers.DoForEachInStorage(_Storage, _Capacity, ref action);
    }

    #endregion
}
