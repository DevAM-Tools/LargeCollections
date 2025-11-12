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
/// Sets are hash based.
/// </summary>
[DebuggerDisplay("LargeSet: Count = {Count}")]
public class LargeSet<T> : ILargeCollection<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DefaultEqualsFunction(T left, T right)
    {
        bool result = (left is null && right is null) || (left is not null && left.Equals(right));
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DefaultHashCodeFunction(T item)
    {
        int result = item is null ? 0 : item.GetHashCode();
        return result;
    }

    private SetElement<T>[][] _Storage = null;
    private long _Capacity = 0L;

    private readonly Func<T, T, bool> _equalsFunction;
    private readonly Func<T, int> _hashCodeFunction;

    public Func<T, T, bool> EqualsFunction
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _equalsFunction;
    }


    public Func<T, int> HashCodeFunction
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _hashCodeFunction;
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

    public LargeSet(Func<T, T, bool> equalsFunction = null,
        Func<T, int> hashCodeFunction = null,
        long capacity = 1L,
        double capacityGrowFactor = Constants.DefaultCapacityGrowFactor,
        long fixedCapacityGrowAmount = Constants.DefaultFixedCapacityGrowAmount,
        long fixedCapacityGrowLimit = Constants.DefaultFixedCapacityGrowLimit,
        double minLoadFactor = Constants.DefaultMinLoadFactor,
        double maxLoadFactor = Constants.DefaultMaxLoadFactor,
        double minLoadFactorTolerance = Constants.DefaultMinLoadFactorTolerance)
    {
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

        _equalsFunction = equalsFunction ?? DefaultEqualsFunction;
        _hashCodeFunction = hashCodeFunction ?? DefaultHashCodeFunction;

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
        LargeSetHelpers.AddToStorage(ref item, _Storage, _Capacity, ref count, EqualsFunction, HashCodeFunction);
        
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

    /// <summary>
    /// Adds a range of items from a large array to the set.
    /// </summary>
    /// <param name="items">The large array of items to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyLargeArray<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        AddRange(items, 0L, items.Count);
    }

    /// <summary>
    /// Adds a range of items from a large array to the set.
    /// </summary>
    /// <param name="items">The large array of items to add.</param>
    /// <param name="offset">The offset in the large array to start adding from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyLargeArray<T> items, long offset)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        AddRange(items, offset, items.Count - offset);
    }


    /// <summary>
    /// Adds a range of items from a large array to the set.
    /// </summary>
    /// <param name="items">The large array of items to add.</param>
    /// <param name="offset">The offset in the large array to start adding from.</param>
    /// <param name="count">The number of items to add.</param>
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
        bool result = LargeSetHelpers.RemoveFromStorage(item, _Storage, ref count, Capacity, EqualsFunction, HashCodeFunction, out removedItem);
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
        return LargeSetHelpers.TryGetValueFromStorage(item, _Storage, Capacity, EqualsFunction, HashCodeFunction, out value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOrSetDefault(T item, out T value)
    {
        long count = Count;
        long previousCount = count;
        bool found = LargeSetHelpers.TryGetOrAddToStorage(ref item, ref item, _Storage, _Capacity, ref count, EqualsFunction, HashCodeFunction, out value);
        
        if (!found && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }
        
        Count = count;

        if (!found)
        {
            // Item was added, check if we need to extend
            Extend();
        }

        return found;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOrSet(T searchItem, T valueIfNotFound, out T value)
    {
        long count = Count;
        long previousCount = count;
        bool found = LargeSetHelpers.TryGetOrAddToStorage(ref searchItem, ref valueIfNotFound, _Storage, _Capacity, ref count, EqualsFunction, HashCodeFunction, out value);
        
        if (!found && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }
        
        Count = count;

        if (!found)
        {
            // Item was added, check if we need to extend
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
        long previousCount = count;
        bool found = LargeSetHelpers.TryGetOrAddToStorageWithFactory(searchItem, valueFactory, _Storage, _Capacity, ref count, EqualsFunction, HashCodeFunction, out value);
        
        if (!found && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }
        
        Count = count;

        if (!found)
        {
            // Item was added, check if we need to extend
            Extend();
        }

        return found;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        return LargeSetHelpers.ContainsInStorage(item, _Storage, Capacity, EqualsFunction, HashCodeFunction);
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
        LargeSetHelpers.ExtendStorage(ref _Storage, ref capacity, ref count, CapacityGrowFactor, FixedCapacityGrowAmount, FixedCapacityGrowLimit, MaxLoadFactor, EqualsFunction, HashCodeFunction);
        _Capacity = capacity;
        Count = count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Shrink()
    {
        long count = Count;
        long capacity = _Capacity;
        LargeSetHelpers.ShrinkStorage(ref _Storage, ref capacity, ref count, MinLoadFactor, MinLoadFactorTolerance, EqualsFunction, HashCodeFunction);
        _Capacity = capacity;
        Count = count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action)
    {
        LargeSetHelpers.DoForEachInStorage(_Storage, _Capacity, action);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(ActionWithUserData<T, TUserData> action, ref TUserData userData)
    {
        LargeSetHelpers.DoForEachInStorage(_Storage, _Capacity, action, ref userData);
    }
}
