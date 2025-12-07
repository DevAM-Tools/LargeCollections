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
/// Factory class for creating <see cref="LargeKDTree{T, TPointAccessor}"/> instances.
/// Provides factory methods for KD-Tree creation with optimal type inference.
/// </summary>
public static class LargeKDTree
{
    /// <summary>
    /// Creates a new KD-Tree with a custom struct point accessor.
    /// </summary>
    /// <typeparam name="T">The point type.</typeparam>
    /// <typeparam name="TPointAccessor">The point accessor type. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="pointAccessor">The point accessor instance.</param>
    /// <param name="points">The points to store in the tree.</param>
    /// <returns>A new KD-Tree instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeKDTree<T, TPointAccessor> Create<T, TPointAccessor>(
        TPointAccessor pointAccessor,
        T[] points)
        where TPointAccessor : struct, IPointAccessor<T>
    {
        return new LargeKDTree<T, TPointAccessor>(pointAccessor, points);
    }

    /// <summary>
    /// Creates a new KD-Tree with a custom struct point accessor from an enumerable.
    /// </summary>
    /// <typeparam name="T">The point type.</typeparam>
    /// <typeparam name="TPointAccessor">The point accessor type. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="pointAccessor">The point accessor instance.</param>
    /// <param name="points">The points to store in the tree.</param>
    /// <returns>A new KD-Tree instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeKDTree<T, TPointAccessor> Create<T, TPointAccessor>(
        TPointAccessor pointAccessor,
        IEnumerable<T> points)
        where TPointAccessor : struct, IPointAccessor<T>
    {
        return new LargeKDTree<T, TPointAccessor>(pointAccessor, points);
    }

    /// <summary>
    /// Creates a new KD-Tree with a delegate-based point accessor.
    /// </summary>
    /// <typeparam name="T">The point type.</typeparam>
    /// <param name="dimensions">The number of dimensions.</param>
    /// <param name="getCoordinate">Function to get coordinate at dimension.</param>
    /// <param name="points">The points to store in the tree.</param>
    /// <returns>A new KD-Tree instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeKDTree<T, DelegatePointAccessor<T>> Create<T>(
        int dimensions,
        Func<T, int, double> getCoordinate,
        T[] points)
    {
        DelegatePointAccessor<T> accessor = new DelegatePointAccessor<T>(dimensions, getCoordinate);
        return new LargeKDTree<T, DelegatePointAccessor<T>>(accessor, points);
    }

    /// <summary>
    /// Creates a new KD-Tree with a delegate-based point accessor from an enumerable.
    /// </summary>
    /// <typeparam name="T">The point type.</typeparam>
    /// <param name="dimensions">The number of dimensions.</param>
    /// <param name="getCoordinate">Function to get coordinate at dimension.</param>
    /// <param name="points">The points to store in the tree.</param>
    /// <returns>A new KD-Tree instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeKDTree<T, DelegatePointAccessor<T>> Create<T>(
        int dimensions,
        Func<T, int, double> getCoordinate,
        IEnumerable<T> points)
    {
        DelegatePointAccessor<T> accessor = new DelegatePointAccessor<T>(dimensions, getCoordinate);
        return new LargeKDTree<T, DelegatePointAccessor<T>>(accessor, points);
    }

    /// <summary>
    /// Creates an empty KD-Tree with a custom struct point accessor.
    /// </summary>
    /// <typeparam name="T">The point type.</typeparam>
    /// <typeparam name="TPointAccessor">The point accessor type. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="pointAccessor">The point accessor instance.</param>
    /// <returns>A new empty KD-Tree instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeKDTree<T, TPointAccessor> CreateEmpty<T, TPointAccessor>(
        TPointAccessor pointAccessor)
        where TPointAccessor : struct, IPointAccessor<T>
    {
        return new LargeKDTree<T, TPointAccessor>(pointAccessor, Array.Empty<T>());
    }

    /// <summary>
    /// Creates an empty KD-Tree with a delegate-based point accessor.
    /// </summary>
    /// <typeparam name="T">The point type.</typeparam>
    /// <param name="dimensions">The number of dimensions.</param>
    /// <param name="getCoordinate">Function to get coordinate at dimension.</param>
    /// <returns>A new empty KD-Tree instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeKDTree<T, DelegatePointAccessor<T>> CreateEmpty<T>(
        int dimensions,
        Func<T, int, double> getCoordinate)
    {
        DelegatePointAccessor<T> accessor = new DelegatePointAccessor<T>(dimensions, getCoordinate);
        return new LargeKDTree<T, DelegatePointAccessor<T>>(accessor, Array.Empty<T>());
    }
}
