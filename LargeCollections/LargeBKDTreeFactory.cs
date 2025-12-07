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
/// Factory class for creating <see cref="LargeBKDTree{T, TPointAccessor}"/> instances.
/// Provides constants and factory methods for BKD-Tree creation.
/// </summary>
public static class LargeBKDTree
{
    /// <summary>
    /// The default leaf capacity for BKD-Tree nodes.
    /// </summary>
    public const int DefaultLeafCapacity = 64;

    /// <summary>
    /// The minimum leaf capacity.
    /// </summary>
    public const int MinLeafCapacity = 4;

    /// <summary>
    /// Creates a new BKD-Tree with a custom struct point accessor.
    /// </summary>
    /// <typeparam name="T">The point type.</typeparam>
    /// <typeparam name="TPointAccessor">The point accessor type.</typeparam>
    /// <param name="pointAccessor">The point accessor instance.</param>
    /// <param name="leafCapacity">The maximum number of points per leaf node.</param>
    /// <param name="equalityComparer">Optional equality comparer for point comparison.</param>
    /// <returns>A new BKD-Tree instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeBKDTree<T, TPointAccessor> Create<T, TPointAccessor>(
        TPointAccessor pointAccessor,
        int leafCapacity = DefaultLeafCapacity,
        IEqualityComparer<T> equalityComparer = null)
        where TPointAccessor : struct, IPointAccessor<T>
    {
        return new LargeBKDTree<T, TPointAccessor>(pointAccessor, leafCapacity, equalityComparer);
    }

    /// <summary>
    /// Creates a new BKD-Tree with a delegate-based point accessor.
    /// </summary>
    /// <typeparam name="T">The point type.</typeparam>
    /// <param name="dimensions">The number of dimensions.</param>
    /// <param name="getCoordinate">Function to get coordinate at dimension.</param>
    /// <param name="leafCapacity">The maximum number of points per leaf node.</param>
    /// <param name="equalityComparer">Optional equality comparer for point comparison.</param>
    /// <returns>A new BKD-Tree instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeBKDTree<T, DelegatePointAccessor<T>> Create<T>(
        int dimensions,
        Func<T, int, double> getCoordinate,
        int leafCapacity = DefaultLeafCapacity,
        IEqualityComparer<T> equalityComparer = null)
    {
        DelegatePointAccessor<T> accessor = new DelegatePointAccessor<T>(dimensions, getCoordinate);
        return new LargeBKDTree<T, DelegatePointAccessor<T>>(accessor, leafCapacity, equalityComparer);
    }
}
