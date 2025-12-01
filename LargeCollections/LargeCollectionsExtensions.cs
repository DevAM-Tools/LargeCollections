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
/// Provides default equality and hash code functions for use in collections.
/// </summary>
public static class DefaultFunctions<T>
{
    /// <summary>
    /// Default equals function using <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DefaultEqualsFunction(T left, T right)
        => EqualityComparer<T>.Default.Equals(left, right);

    /// <summary>
    /// Default hash code function using <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DefaultHashCodeFunction(T item)
        => item is not null ? EqualityComparer<T>.Default.GetHashCode(item) : 0;
}

public static class LargeCollectionsExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo<T>(this T[] source, ILargeArray<T> target, int sourceOffset, long targetOffset, int count)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        StorageExtensions.CheckRange(sourceOffset, count, source.LongLength);
        StorageExtensions.CheckRange(targetOffset, count, target.Count);

        if (target is LargeArray<T> largeArrayTarget)
        {
            largeArrayTarget.CopyFromArray(source, sourceOffset, targetOffset, count);
        }
        else if (target is LargeList<T> largeListTarget)
        {
            largeListTarget.CopyFromArray(source, sourceOffset, targetOffset, count);
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                T item = source[sourceOffset + i];
                target[targetOffset + i] = item;
            }
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo<T>(this ReadOnlySpan<T> source, ILargeArray<T> target, long targetOffset, int count)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        StorageExtensions.CheckRange(targetOffset, count, target.Count);
        if (target is LargeArray<T> largeArrayTarget)
        {
            largeArrayTarget.CopyFromSpan(source, targetOffset, count);
        }
        else if (target is LargeList<T> largeListTarget)
        {
            largeListTarget.CopyFromSpan(source, targetOffset, count);
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                T item = source[i];
                target[targetOffset + i] = item;
            }
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeSpan<T> AsLargeSpan<T>(this IRefAccessLargeArray<T> array)
        => new(array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeSpan<T> AsLargeSpan<T>(this IRefAccessLargeArray<T> array, long start)
        => new(array, start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeSpan<T> AsLargeSpan<T>(this IRefAccessLargeArray<T> array, long start, long count)
        => new(array, start, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyLargeSpan<T> AsReadOnlyLargeSpan<T>(this IReadOnlyLargeArray<T> array)
        => new(array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyLargeSpan<T> AsReadOnlyLargeSpan<T>(this IReadOnlyLargeArray<T> array, long start)
        => new(array, start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyLargeSpan<T> AsReadOnlyLargeSpan<T>(this IReadOnlyLargeArray<T> array, long start, long count)
        => new(array, start, count);
}
