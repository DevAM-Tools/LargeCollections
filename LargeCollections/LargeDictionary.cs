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
/// A mutable dictionary of <typeparamref name="TKey"/> as key and <typeparamref name="TValue"/> as value 
/// that can store up to <see cref="Constants.MaxLargeCollectionCount"/> elements.
/// Dictionaries are hash based. This version uses a struct comparer type parameter for maximum JIT inlining performance.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <typeparam name="TComparer">The type of equality comparer for KeyValuePair. Use a struct implementing <see cref="IEqualityComparer{T}"/> for best performance.</typeparam>
[DebuggerDisplay("LargeDictionary: Count = {Count}")]
public class LargeDictionary<TKey, TValue, TComparer> : ILargeDictionary<TKey, TValue> 
    where TKey : notnull
    where TComparer : IEqualityComparer<KeyValuePair<TKey, TValue>>
{
    private SetElement<KeyValuePair<TKey, TValue>>[][] _storage = null;
    private long _capacity = 0L;
    private TComparer _comparer;

    /// <summary>
    /// Gets the equality comparer used by this dictionary.
    /// </summary>
    public TComparer Comparer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _comparer;
    }

    public long Count { get; private set; }

    public long Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _capacity;
    }

    public double CapacityGrowFactor { get; private set; }
    public long FixedCapacityGrowAmount { get; private set; }
    public long FixedCapacityGrowLimit { get; private set; }

    public readonly double MinLoadFactor;
    public readonly double MaxLoadFactor;
    public readonly double MinLoadFactorTolerance;

    public double LoadFactor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (double)Count / (double)Capacity;
    }

    /// <summary>
    /// Creates a new LargeDictionary with the specified comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer to use for KeyValuePairs. For best performance, use a struct type.</param>
    /// <param name="capacity">Initial bucket capacity.</param>
    /// <param name="capacityGrowFactor">Factor by which capacity grows when needed.</param>
    /// <param name="fixedCapacityGrowAmount">Fixed amount to grow capacity by for small dictionaries.</param>
    /// <param name="fixedCapacityGrowLimit">Capacity limit below which fixed growth is used.</param>
    /// <param name="minLoadFactor">Minimum load factor before shrinking.</param>
    /// <param name="maxLoadFactor">Maximum load factor before growing.</param>
    /// <param name="minLoadFactorTolerance">Tolerance for minimum load factor.</param>
    public LargeDictionary(TComparer comparer,
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
        _storage = StorageExtensions.StorageCreate<SetElement<KeyValuePair<TKey, TValue>>>(capacity);
        _capacity = capacity;

        Count = 0L;

        CapacityGrowFactor = capacityGrowFactor;
        FixedCapacityGrowAmount = fixedCapacityGrowAmount;
        FixedCapacityGrowLimit = fixedCapacityGrowLimit;

        MinLoadFactor = minLoadFactor;
        MaxLoadFactor = maxLoadFactor;
        MinLoadFactorTolerance = minLoadFactorTolerance;
    }

    public IEnumerable<TKey> Keys
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            long capacity = Capacity;
            for (long i = 0L; i < capacity; i++)
            {
                SetElement<KeyValuePair<TKey, TValue>> element = _storage.StorageGet(i);
                while (element is not null)
                {
                    yield return element.Item.Key;
                    element = element.NextElement;
                }
            }
        }
    }

    public IEnumerable<TValue> Values
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            long capacity = Capacity;
            for (long i = 0L; i < capacity; i++)
            {
                SetElement<KeyValuePair<TKey, TValue>> element = _storage.StorageGet(i);
                while (element is not null)
                {
                    yield return element.Item.Value;
                    element = element.NextElement;
                }
            }
        }
    }

    TValue IReadOnlyLargeDictionary<TKey, TValue>.this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Get(key);
    }

    public TValue this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            return Get(key);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            Set(key, value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(TKey key, TValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        Add(new KeyValuePair<TKey, TValue>(key, value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        if (item.Key is null)
        {
            throw new ArgumentNullException(nameof(item), "Key cannot be null");
        }

        long count = Count;
        long previousCount = count;

        LargeSetHelpers.AddToStorage(ref item, _storage, _capacity, ref count, ref _comparer);

        if (count > previousCount && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }

        Count = count;
        Extend();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (items is IReadOnlyList<KeyValuePair<TKey, TValue>> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Add(list[i]);
            }
        }
        else
        {
            foreach (KeyValuePair<TKey, TValue> item in items)
            {
                Add(item);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyLargeArray<KeyValuePair<TKey, TValue>> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }
        AddRange(items, 0L, items.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyLargeArray<KeyValuePair<TKey, TValue>> items, long offset)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }
        AddRange(items, offset, items.Count - offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyLargeArray<KeyValuePair<TKey, TValue>> items, long offset, long count)
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
    public void AddRange(ReadOnlyLargeSpan<KeyValuePair<TKey, TValue>> items)
    {
        for (long i = 0L; i < items.Count; i++)
        {
            Add(items[i]);
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlySpan<KeyValuePair<TKey, TValue>> items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            Add(items[i]);
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
        => Remove(key, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key, out TValue removedValue)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        removedValue = default;
        KeyValuePair<TKey, TValue> keyItem = new(key, default);

        long count = Count;
        bool result = LargeSetHelpers.RemoveFromStorage(ref keyItem, _storage, Capacity, ref count, ref _comparer, out KeyValuePair<TKey, TValue> removedItem);
        Count = count;

        if (result)
        {
            removedValue = removedItem.Value;
            Shrink();
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (item.Key is null)
        {
            throw new ArgumentNullException(nameof(item), "Key cannot be null");
        }
        return Remove(item.Key, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(KeyValuePair<TKey, TValue> item, out KeyValuePair<TKey, TValue> removedItem)
    {
        if (item.Key is null)
        {
            throw new ArgumentNullException(nameof(item), "Key cannot be null");
        }

        if (Remove(item.Key, out TValue removedValue))
        {
            removedItem = new KeyValuePair<TKey, TValue>(item.Key, removedValue);
            return true;
        }

        removedItem = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        long count = Count;
        LargeSetHelpers.ClearStorage(_storage, _capacity, ref count);
        Count = count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue Get(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        KeyValuePair<TKey, TValue> keyItem = new(key, default);
        if (!LargeSetHelpers.TryGetValueFromStorage(ref keyItem, _storage, Capacity, ref _comparer, out KeyValuePair<TKey, TValue> value))
        {
            throw new KeyNotFoundException();
        }

        return value.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        KeyValuePair<TKey, TValue> keyItem = new(key, default);
        return LargeSetHelpers.ContainsInStorage(ref keyItem, _storage, Capacity, ref _comparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        if (item.Key is null)
        {
            throw new ArgumentNullException(nameof(item), "Key cannot be null");
        }
        return TryGetValue(item.Key, out TValue value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        KeyValuePair<TKey, TValue> keyItem = new(key, default);
        if (!LargeSetHelpers.TryGetValueFromStorage(ref keyItem, _storage, Capacity, ref _comparer, out KeyValuePair<TKey, TValue> keyAndValue))
        {
            value = default;
            return false;
        }

        value = keyAndValue.Value;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOrSetDefault(TKey key, out TValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        KeyValuePair<TKey, TValue> keyItem = new(key, default);
        KeyValuePair<TKey, TValue> valueItem = new(key, default);
        long count = Count;
        bool found = LargeSetHelpers.TryGetOrAddToStorage(ref keyItem, ref valueItem, _storage, _capacity, ref count, ref _comparer, out KeyValuePair<TKey, TValue> kvp);

        if (!found && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }

        Count = count;
        value = kvp.Value;

        if (!found)
        {
            Extend();
        }

        return found;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOrSet(TKey key, TValue valueIfNotFound, out TValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        KeyValuePair<TKey, TValue> searchItem = new(key, default);
        KeyValuePair<TKey, TValue> valueItem = new(key, valueIfNotFound);
        long count = Count;
        bool found = LargeSetHelpers.TryGetOrAddToStorage(ref searchItem, ref valueItem, _storage, _capacity, ref count, ref _comparer, out KeyValuePair<TKey, TValue> kvp);

        if (!found && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }

        Count = count;
        value = kvp.Value;

        if (!found)
        {
            Extend();
        }

        return found;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOrSet(TKey key, Func<TValue> valueFactory, out TValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        KeyValuePair<TKey, TValue> searchItem = new(key, default);
        long count = Count;
        bool found = LargeSetHelpers.TryGetOrAddToStorageWithFactory(ref searchItem, () => new KeyValuePair<TKey, TValue>(key, valueFactory.Invoke()), _storage, _capacity, ref count, ref _comparer, out KeyValuePair<TKey, TValue> kvp);

        if (!found && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }

        Count = count;
        value = kvp.Value;

        if (!found)
        {
            Extend();
        }

        return found;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<KeyValuePair<TKey, TValue>> GetAll()
    {
        return LargeSetHelpers.GetAllFromStorage(_storage, _capacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
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
        long capacity = _capacity;
        LargeSetHelpers.ExtendStorage(ref _storage, ref capacity, ref count, CapacityGrowFactor, FixedCapacityGrowAmount, FixedCapacityGrowLimit, MaxLoadFactor, ref _comparer);
        _capacity = capacity;
        Count = count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Shrink()
    {
        long count = Count;
        long capacity = _capacity;
        LargeSetHelpers.ShrinkStorage(ref _storage, ref capacity, ref count, MinLoadFactor, MinLoadFactorTolerance, ref _comparer);
        _capacity = capacity;
        Count = count;
    }

    #region DoForEach Methods

    /// <summary>
    /// Performs the <paramref name="action"/> with key-value pairs of the dictionary.
    /// </summary>
    /// <param name="action">The function that will be called for each key-value pair.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<KeyValuePair<TKey, TValue>> action)
    {
        LargeSetHelpers.DoForEachInStorage(_storage, _capacity, action);
    }

    /// <summary>
    /// Performs the action on key-value pairs using an action for optimal performance.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeAction{KeyValuePair{TKey, TValue}}"/>.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<KeyValuePair<TKey, TValue>>
    {
        LargeSetHelpers.DoForEachInStorage(_storage, _capacity, ref action);
    }

    #endregion
}
