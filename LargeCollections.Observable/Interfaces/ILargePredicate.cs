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

namespace LargeCollections.Observable;

/// <summary>
/// High-performance predicate interface for filtering operations.
/// Using struct implementations enables JIT devirtualization and inlining for optimal performance.
/// </summary>
/// <typeparam name="T">The type of items to filter.</typeparam>
/// <example>
/// <code>
/// struct PositiveFilter : ILargePredicate&lt;int&gt;
/// {
///     public bool Invoke(int item) =&gt; item &gt; 0;
/// }
/// 
/// var filtered = collection.CreateFilteredView(new PositiveFilter());
/// </code>
/// </example>
public interface ILargePredicate<T>
{
    /// <summary>
    /// Evaluates the predicate for the specified item.
    /// </summary>
    /// <param name="item">The item to evaluate.</param>
    /// <returns>true if the item satisfies the predicate; otherwise, false.</returns>
    bool Invoke(T item);
}

/// <summary>
/// Wrapper that adapts a <see cref="Func{T, TResult}"/> delegate to the <see cref="ILargePredicate{T}"/> interface.
/// Use this when you have an existing delegate but want to use the generic predicate overloads.
/// Note: This wrapper has the same performance as direct delegate usage.
/// </summary>
/// <typeparam name="T">The type to filter.</typeparam>
public readonly struct DelegatePredicate<T> : ILargePredicate<T>
{
    private readonly Func<T, bool> _predicate;

    /// <summary>
    /// Creates a new delegate wrapper predicate.
    /// </summary>
    /// <param name="predicate">The predicate delegate to wrap.</param>
    public DelegatePredicate(Func<T, bool> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Invoke(T item) => _predicate(item);
}

/// <summary>
/// A pass-through predicate that always returns true (no filtering).
/// The JIT compiler can completely eliminate the predicate check when this struct is used,
/// resulting in zero overhead compared to not having a predicate at all.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
public readonly struct NoFilter<T> : ILargePredicate<T>
{
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Invoke(T item) => true;
}

/// <summary>
/// A pass-through comparer that preserves the original order (no sorting).
/// The JIT compiler can completely eliminate the sort operation when this struct is used,
/// resulting in zero overhead compared to not having a comparer at all.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
public readonly struct NoSort<T> : IComparer<T>
{
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(T x, T y) => 0;
}

