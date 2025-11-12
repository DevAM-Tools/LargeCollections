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
/// A mutable dictionary of <typeparamref name="TKey"/> as key and <typeparamref name="TValue"/> as value that can store up to <see cref="Constants.MaxLargeCollectionCount"/> elements.
/// Dictionaries are hash based.
/// </summary>
[DebuggerDisplay("LargeDictionary: Count = {Count}")]
public class LargeDictionary<TKey, TValue> : ILargeDictionary<TKey, TValue> where TKey : notnull
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DefaultKeyEqualsFunction(TKey left, TKey right)
    {
        bool result = (left is null && right is null) || (left is not null && left.Equals(right));
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DefaultKeyHashCodeFunction(TKey key)
    {
        int result = key is null ? 0 : key.GetHashCode();
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool KeyValuePairEqualsFunction(KeyValuePair<TKey, TValue> left, KeyValuePair<TKey, TValue> right, Func<TKey, TKey, bool> keyEqualsFunction)
    {
        return keyEqualsFunction.Invoke(left.Key, right.Key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int KeyValuePairHashCodeFunction(KeyValuePair<TKey, TValue> item, Func<TKey, int> keyHashCodeFunction)
    {
        return keyHashCodeFunction.Invoke(item.Key);
    }

    private SetElement<KeyValuePair<TKey, TValue>>[][] _storage = null;
    private long _capacity = 0L;
    private readonly Func<TKey, TKey, bool> _keyEqualsFunction;
    private readonly Func<TKey, int> _keyHashCodeFunction;

    // Cached delegates to avoid allocations on every call
    private readonly Func<KeyValuePair<TKey, TValue>, KeyValuePair<TKey, TValue>, bool> _kvpEqualsFunction;
    private readonly Func<KeyValuePair<TKey, TValue>, int> _kvpHashCodeFunction;

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

    public LargeDictionary(Func<TKey, TKey, bool> keyEqualsFunction = null,
        Func<TKey, int> hashCodeFunction = null,
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

        _keyEqualsFunction = keyEqualsFunction ?? DefaultKeyEqualsFunction;
        _keyHashCodeFunction = hashCodeFunction ?? DefaultKeyHashCodeFunction;

        // Initialize cached delegates once to avoid allocations on every storage operation
        _kvpEqualsFunction = (left, right) => KeyValuePairEqualsFunction(left, right, _keyEqualsFunction);
        _kvpHashCodeFunction = (kvp) => KeyValuePairHashCodeFunction(kvp, _keyHashCodeFunction);

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
        KeyValuePair<TKey, TValue> itemCopy = item;
        LargeSetHelpers.AddToStorage(
            ref itemCopy,
            _storage,
            _capacity,
            ref count,
            _kvpEqualsFunction,
            _kvpHashCodeFunction);
        
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

    /// <summary>
    /// Adds a range of items from a large array to the dictionary.
    /// </summary>
    /// <param name="items">The large array of items to add.</param>
    /// <param name="offset">The zero-based index in the large array at which to begin adding items.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyLargeArray<KeyValuePair<TKey, TValue>> items, long offset)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        AddRange(items, offset, items.Count - offset);
    }

    /// <summary>
    /// Adds a range of items from a large array to the dictionary.
    /// </summary>
    /// <param name="items">The large array of items to add.</param>
    /// <param name="offset">The zero-based index in the large array at which to begin adding items.</param>
    /// <param name="count">The number of items to add from the large array.</param>
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
        bool result = LargeSetHelpers.RemoveFromStorage(
            keyItem,
            _storage,
            ref count,
            Capacity,
            _kvpEqualsFunction,
            _kvpHashCodeFunction,
            out KeyValuePair<TKey, TValue> removedItem);
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
        if (!LargeSetHelpers.TryGetValueFromStorage(
            keyItem,
            _storage,
            Capacity,
            _kvpEqualsFunction,
            _kvpHashCodeFunction,
            out KeyValuePair<TKey, TValue> value))
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
        return LargeSetHelpers.ContainsInStorage(
            keyItem,
            _storage,
            Capacity,
            _kvpEqualsFunction,
            _kvpHashCodeFunction);
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
        if (!LargeSetHelpers.TryGetValueFromStorage(
            keyItem,
            _storage,
            Capacity,
            _kvpEqualsFunction,
            _kvpHashCodeFunction,
            out KeyValuePair<TKey, TValue> keyAndValue))
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
        long previousCount = count;
        bool found = LargeSetHelpers.TryGetOrAddToStorage(
            ref keyItem,
            ref valueItem,
            _storage,
            _capacity,
            ref count,
            _kvpEqualsFunction,
            _kvpHashCodeFunction,
            out KeyValuePair<TKey, TValue> kvp);
        
        if (!found && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }
        
        Count = count;

        value = kvp.Value;

        if (!found)
        {
            // Item was added, check if we need to extend
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
        long previousCount = count;
        bool found = LargeSetHelpers.TryGetOrAddToStorage(
            ref searchItem,
            ref valueItem,
            _storage,
            _capacity,
            ref count,
            _kvpEqualsFunction,
            _kvpHashCodeFunction,
            out KeyValuePair<TKey, TValue> kvp);
        
        if (!found && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }
        
        Count = count;

        value = kvp.Value;

        if (!found)
        {
            // Item was added, check if we need to extend
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
        long previousCount = count;
        bool found = LargeSetHelpers.TryGetOrAddToStorageWithFactory(
            searchItem,
            () => new KeyValuePair<TKey, TValue>(key, valueFactory.Invoke()),
            _storage,
            _capacity,
            ref count,
            _kvpEqualsFunction,
            _kvpHashCodeFunction,
            out KeyValuePair<TKey, TValue> kvp);
        
        if (!found && count > Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Can not store more than {Constants.MaxLargeCollectionCount} items.");
        }
        
        Count = count;

        value = kvp.Value;

        if (!found)
        {
            // Item was added, check if we need to extend
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
        LargeSetHelpers.ExtendStorage(
            ref _storage,
            ref capacity,
            ref count,
            CapacityGrowFactor,
            FixedCapacityGrowAmount,
            FixedCapacityGrowLimit,
            MaxLoadFactor,
            _kvpEqualsFunction,
            _kvpHashCodeFunction);
        _capacity = capacity;
        Count = count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Shrink()
    {
        long count = Count;
        long capacity = _capacity;
        LargeSetHelpers.ShrinkStorage(
            ref _storage,
            ref capacity,
            ref count,
            MinLoadFactor,
            MinLoadFactorTolerance,
            _kvpEqualsFunction,
            _kvpHashCodeFunction);
        _capacity = capacity;
        Count = count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<KeyValuePair<TKey, TValue>> action)
    {
        LargeSetHelpers.DoForEachInStorage(_storage, _capacity, action);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(ActionWithUserData<KeyValuePair<TKey, TValue>, TUserData> action, ref TUserData userData)
    {
        LargeSetHelpers.DoForEachInStorage(_storage, _capacity, action, ref userData);
    }
}
