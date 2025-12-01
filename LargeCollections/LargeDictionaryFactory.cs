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
using System.Runtime.CompilerServices;

namespace LargeCollections;

/// <summary>
/// Equality comparer for KeyValuePair that only compares keys using the default comparer.
/// Used by LargeDictionary for JIT devirtualization.
/// </summary>
/// <typeparam name="TKey">The type of keys.</typeparam>
/// <typeparam name="TValue">The type of values.</typeparam>
public struct KeyValuePairDefaultComparer<TKey, TValue> : IEqualityComparer<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(KeyValuePair<TKey, TValue> left, KeyValuePair<TKey, TValue> right)
        => EqualityComparer<TKey>.Default.Equals(left.Key, right.Key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(KeyValuePair<TKey, TValue> item)
        => item.Key is not null ? EqualityComparer<TKey>.Default.GetHashCode(item.Key) : 0;
}

/// <summary>
/// Equality comparer for KeyValuePair that only compares keys using delegate functions.
/// Used by LargeDictionary for JIT devirtualization.
/// </summary>
/// <typeparam name="TKey">The type of keys.</typeparam>
/// <typeparam name="TValue">The type of values.</typeparam>
public readonly struct KeyValuePairDelegateComparer<TKey, TValue> : IEqualityComparer<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    private readonly Func<TKey, TKey, bool> _keyEquals;
    private readonly Func<TKey, int> _keyHashCode;

    public KeyValuePairDelegateComparer(Func<TKey, TKey, bool> keyEquals, Func<TKey, int> keyHashCode)
    {
        _keyEquals = keyEquals ?? throw new ArgumentNullException(nameof(keyEquals));
        _keyHashCode = keyHashCode ?? throw new ArgumentNullException(nameof(keyHashCode));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(KeyValuePair<TKey, TValue> left, KeyValuePair<TKey, TValue> right)
        => _keyEquals(left.Key, right.Key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(KeyValuePair<TKey, TValue> item)
        => _keyHashCode(item.Key);
}

/// <summary>
/// Factory class for creating <see cref="LargeDictionary{TKey, TValue, TComparer}"/> instances with convenient methods.
/// </summary>
public static class LargeDictionary
{
    /// <summary>
    /// Creates a new LargeDictionary with the default key equality comparer (uses <see cref="EqualityComparer{TKey}.Default"/>).
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="capacity">Initial bucket capacity.</param>
    /// <param name="capacityGrowFactor">Factor by which capacity grows when needed.</param>
    /// <param name="fixedCapacityGrowAmount">Fixed amount to grow capacity by for small dictionaries.</param>
    /// <param name="fixedCapacityGrowLimit">Capacity limit below which fixed growth is used.</param>
    /// <param name="minLoadFactor">Minimum load factor before shrinking.</param>
    /// <param name="maxLoadFactor">Maximum load factor before growing.</param>
    /// <param name="minLoadFactorTolerance">Tolerance for minimum load factor.</param>
    /// <returns>A new <see cref="LargeDictionary{TKey, TValue, TComparer}"/> with <see cref="KeyValuePairDefaultComparer{TKey, TValue}"/>.</returns>
    public static LargeDictionary<TKey, TValue, KeyValuePairDefaultComparer<TKey, TValue>> Create<TKey, TValue>(
        long capacity = 1L,
        double capacityGrowFactor = Constants.DefaultCapacityGrowFactor,
        long fixedCapacityGrowAmount = Constants.DefaultFixedCapacityGrowAmount,
        long fixedCapacityGrowLimit = Constants.DefaultFixedCapacityGrowLimit,
        double minLoadFactor = Constants.DefaultMinLoadFactor,
        double maxLoadFactor = Constants.DefaultMaxLoadFactor,
        double minLoadFactorTolerance = Constants.DefaultMinLoadFactorTolerance) where TKey : notnull
    {
        return new LargeDictionary<TKey, TValue, KeyValuePairDefaultComparer<TKey, TValue>>(
            new KeyValuePairDefaultComparer<TKey, TValue>(),
            capacity,
            capacityGrowFactor,
            fixedCapacityGrowAmount,
            fixedCapacityGrowLimit,
            minLoadFactor,
            maxLoadFactor,
            minLoadFactorTolerance);
    }

    /// <summary>
    /// Creates a new LargeDictionary with custom key equality functions using a <see cref="KeyValuePairDelegateComparer{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="keyEqualsFunction">The function to compare two keys for equality.</param>
    /// <param name="keyHashCodeFunction">The function to compute the hash code of a key.</param>
    /// <param name="capacity">Initial bucket capacity.</param>
    /// <param name="capacityGrowFactor">Factor by which capacity grows when needed.</param>
    /// <param name="fixedCapacityGrowAmount">Fixed amount to grow capacity by for small dictionaries.</param>
    /// <param name="fixedCapacityGrowLimit">Capacity limit below which fixed growth is used.</param>
    /// <param name="minLoadFactor">Minimum load factor before shrinking.</param>
    /// <param name="maxLoadFactor">Maximum load factor before growing.</param>
    /// <param name="minLoadFactorTolerance">Tolerance for minimum load factor.</param>
    /// <returns>A new <see cref="LargeDictionary{TKey, TValue, TComparer}"/> with <see cref="KeyValuePairDelegateComparer{TKey, TValue}"/>.</returns>
    public static LargeDictionary<TKey, TValue, KeyValuePairDelegateComparer<TKey, TValue>> Create<TKey, TValue>(
        Func<TKey, TKey, bool> keyEqualsFunction,
        Func<TKey, int> keyHashCodeFunction,
        long capacity = 1L,
        double capacityGrowFactor = Constants.DefaultCapacityGrowFactor,
        long fixedCapacityGrowAmount = Constants.DefaultFixedCapacityGrowAmount,
        long fixedCapacityGrowLimit = Constants.DefaultFixedCapacityGrowLimit,
        double minLoadFactor = Constants.DefaultMinLoadFactor,
        double maxLoadFactor = Constants.DefaultMaxLoadFactor,
        double minLoadFactorTolerance = Constants.DefaultMinLoadFactorTolerance) where TKey : notnull
    {
        if (keyEqualsFunction is null)
        {
            throw new ArgumentNullException(nameof(keyEqualsFunction));
        }
        if (keyHashCodeFunction is null)
        {
            throw new ArgumentNullException(nameof(keyHashCodeFunction));
        }

        return new LargeDictionary<TKey, TValue, KeyValuePairDelegateComparer<TKey, TValue>>(
            new KeyValuePairDelegateComparer<TKey, TValue>(keyEqualsFunction, keyHashCodeFunction),
            capacity,
            capacityGrowFactor,
            fixedCapacityGrowAmount,
            fixedCapacityGrowLimit,
            minLoadFactor,
            maxLoadFactor,
            minLoadFactorTolerance);
    }
}
