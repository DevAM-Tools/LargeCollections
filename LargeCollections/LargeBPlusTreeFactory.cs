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

namespace LargeCollections;

/// <summary>
/// Factory class for creating <see cref="LargeBPlusTree{TKey, TValue, TKeyComparer}"/> instances with convenient methods.
/// </summary>
public static class LargeBPlusTree
{
    /// <summary>
    /// Default order (maximum children per internal node) for B+Trees.
    /// </summary>
    public const int DefaultOrder = 128;

    /// <summary>
    /// Minimum order for B+Trees.
    /// </summary>
    public const int MinOrder = 3;

    /// <summary>
    /// Creates a new B+Tree with the default key comparer for types implementing <see cref="IComparable{T}"/>.
    /// Uses <see cref="DefaultComparer{T}"/> for optimal JIT devirtualization.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the tree. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <typeparam name="TValue">The type of values in the tree.</typeparam>
    /// <param name="order">The order of the tree (maximum children per internal node). Must be at least 3. Default is 128.</param>
    /// <returns>A new <see cref="LargeBPlusTree{TKey, TValue, TKeyComparer}"/> with <see cref="DefaultComparer{TKey}"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="order"/> is less than 3.</exception>
    public static LargeBPlusTree<TKey, TValue, DefaultComparer<TKey>> Create<TKey, TValue>(int order = DefaultOrder)
        where TKey : notnull, IComparable<TKey>
    {
        return new LargeBPlusTree<TKey, TValue, DefaultComparer<TKey>>(new DefaultComparer<TKey>(), order);
    }

    /// <summary>
    /// Creates a new B+Tree with a custom comparer function using a <see cref="DelegateComparer{T}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the tree.</typeparam>
    /// <typeparam name="TValue">The type of values in the tree.</typeparam>
    /// <param name="keyCompareFunction">The function to compare two keys. Returns negative if left &lt; right, zero if equal, positive if left &gt; right.</param>
    /// <param name="order">The order of the tree (maximum children per internal node). Must be at least 3. Default is 128.</param>
    /// <returns>A new <see cref="LargeBPlusTree{TKey, TValue, TKeyComparer}"/> with <see cref="DelegateComparer{TKey}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keyCompareFunction"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="order"/> is less than 3.</exception>
    public static LargeBPlusTree<TKey, TValue, DelegateComparer<TKey>> Create<TKey, TValue>(
        Func<TKey, TKey, int> keyCompareFunction,
        int order = DefaultOrder)
        where TKey : notnull
    {
        if (keyCompareFunction is null)
        {
            throw new ArgumentNullException(nameof(keyCompareFunction));
        }

        return new LargeBPlusTree<TKey, TValue, DelegateComparer<TKey>>(
            new DelegateComparer<TKey>(keyCompareFunction),
            order);
    }

    /// <summary>
    /// Creates a new B+Tree with a custom struct comparer for maximum performance.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the tree.</typeparam>
    /// <typeparam name="TValue">The type of values in the tree.</typeparam>
    /// <typeparam name="TKeyComparer">The type of the key comparer. Use a struct implementing <see cref="IComparer{T}"/> for best performance.</typeparam>
    /// <param name="comparer">The key comparer instance.</param>
    /// <param name="order">The order of the tree (maximum children per internal node). Must be at least 3. Default is 128.</param>
    /// <returns>A new <see cref="LargeBPlusTree{TKey, TValue, TKeyComparer}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="order"/> is less than 3.</exception>
    public static LargeBPlusTree<TKey, TValue, TKeyComparer> Create<TKey, TValue, TKeyComparer>(
        TKeyComparer comparer,
        int order = DefaultOrder)
        where TKey : notnull
        where TKeyComparer : IComparer<TKey>
    {
        if (comparer is null)
        {
            throw new ArgumentNullException(nameof(comparer));
        }

        return new LargeBPlusTree<TKey, TValue, TKeyComparer>(comparer, order);
    }

    /// <summary>
    /// Creates a new B+Tree with descending key order for types implementing <see cref="IComparable{T}"/>.
    /// Uses <see cref="DescendingComparer{T}"/> for optimal JIT devirtualization.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the tree. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <typeparam name="TValue">The type of values in the tree.</typeparam>
    /// <param name="order">The order of the tree (maximum children per internal node). Must be at least 3. Default is 128.</param>
    /// <returns>A new <see cref="LargeBPlusTree{TKey, TValue, TKeyComparer}"/> with <see cref="DescendingComparer{TKey}"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="order"/> is less than 3.</exception>
    public static LargeBPlusTree<TKey, TValue, DescendingComparer<TKey>> CreateDescending<TKey, TValue>(int order = DefaultOrder)
        where TKey : notnull, IComparable<TKey>
    {
        return new LargeBPlusTree<TKey, TValue, DescendingComparer<TKey>>(new DescendingComparer<TKey>(), order);
    }
}
