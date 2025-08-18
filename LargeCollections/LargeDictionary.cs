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

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LargeCollections;

/// <summary>
/// A mutable dictionary of <typeparamref name="TKey"/> as key and <typeparamref name="TValue"/> as value that can store up to <see cref="Constants.MaxLargeCollectionCount"/> elements.
/// Dictionaries are hash based.
/// </summary>
[DebuggerDisplay("LargeDictionary: Count = {Count}")]
public class LargeDictionary<TKey, TValue> : LargeSet<KeyValuePair<TKey, TValue>>, ILargeDictionary<TKey, TValue> where TKey : notnull
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DefaultKeyEqualsFunction(KeyValuePair<TKey, TValue> left, KeyValuePair<TKey, TValue> right)
    {
        bool result = LargeSet<TKey>.DefaultEqualsFunction(left.Key, right.Key);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DefaultKeyHashCodeFunction(KeyValuePair<TKey, TValue> item)
    {
        int result = LargeSet<TKey>.DefaultHashCodeFunction(item.Key);
        return result;
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

        : base(DefaultKeyEqualsFunction,
            DefaultKeyHashCodeFunction,
            capacity,
            capacityGrowFactor,
            fixedCapacityGrowAmount,
            fixedCapacityGrowLimit,
            minLoadFactor,
            maxLoadFactor,
            minLoadFactorTolerance)
    {
        if (keyEqualsFunction is not null)
        {
            EqualsFunction = (left, right) => keyEqualsFunction.Invoke(left.Key, right.Key);
        }
        if (hashCodeFunction is not null)
        {
            HashCodeFunction = item => hashCodeFunction.Invoke(item.Key);
        }
    }

    public LargeDictionary(IEnumerable<KeyValuePair<TKey, TValue>> items,
        Func<TKey, TKey, bool> keyEqualsFunction = null,
        Func<TKey, int> hashCodeFunction = null,
        long capacity = 1L,
        double capacityGrowFactor = Constants.DefaultCapacityGrowFactor,
        long fixedCapacityGrowAmount = Constants.DefaultFixedCapacityGrowAmount,
        long fixedCapacityGrowLimit = Constants.DefaultFixedCapacityGrowLimit,
        double minLoadFactor = Constants.DefaultMinLoadFactor,
        double maxLoadFactor = Constants.DefaultMaxLoadFactor,
        double minLoadFactorTolerance = Constants.DefaultMinLoadFactorTolerance)

        : this(keyEqualsFunction,
              hashCodeFunction,
              capacity,
              capacityGrowFactor,
              fixedCapacityGrowAmount,
              fixedCapacityGrowLimit,
              minLoadFactor,
              maxLoadFactor,
              minLoadFactorTolerance)
    {
        AddRange(items);
    }

    public IEnumerable<TKey> Keys
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            long capacity = Capacity;

            for (long i = 0L; i < capacity; i++)
            {
                SetElement element = _Storage[i];

                while (element is not null)
                {
                    TKey key = element.Item.Key;
                    yield return key;
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
                SetElement element = _Storage[i];

                while (element is not null)
                {
                    TValue value = element.Item.Value;
                    yield return value;
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
    public override void Add(KeyValuePair<TKey, TValue> item)
    {
        if (item.Key is null)
        {
            throw new ArgumentNullException(nameof(item), "Key cannot be null");
        }

        base.Add(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (KeyValuePair<TKey, TValue> item in items)
        {
            if (item.Key is null)
            {
                throw new ArgumentNullException(nameof(item), "Key cannot be null");
            }
            base.Add(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        KeyValuePair<TKey, TValue> keyItem = new(key, default);
        if (TryGetValue(keyItem, out KeyValuePair<TKey, TValue> actualItem))
        {
            base.Remove(actualItem);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Remove(KeyValuePair<TKey, TValue> item)
    {
        if (item.Key is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        // Only remove if both key and value match exactly
        if (Contains(item))
        {
            base.Remove(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(IEnumerable<TKey> keys)
    {
        if (keys is null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        foreach (TKey key in keys)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Remove(key);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Remove(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (KeyValuePair<TKey, TValue> item in items)
        {
            base.Remove(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue Get(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        KeyValuePair<TKey, TValue> keyItem = new(key, default);
        if (!TryGetValue(keyItem, out KeyValuePair<TKey, TValue> value))
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
        bool result = TryGetValue(keyItem, out _);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Contains(KeyValuePair<TKey, TValue> item)
    {
        if (item.Key is null)
        {
            throw new ArgumentNullException(nameof(item), "Key cannot be null");
        }

        if (!TryGetValue(item.Key, out TValue value))
        {
            return false;
        }

        bool result = LargeSet<TValue>.DefaultEqualsFunction(item.Value, value);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        KeyValuePair<TKey, TValue> keyItem = new(key, default);
        if (!TryGetValue(keyItem, out KeyValuePair<TKey, TValue> keyAndValue))
        {
            value = default;
            return false;
        }

        value = keyAndValue.Value;
        return true;
    }
}
