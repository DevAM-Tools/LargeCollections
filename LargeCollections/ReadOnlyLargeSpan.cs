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
using System.Runtime.CompilerServices;

namespace LargeCollections;

public readonly struct ReadOnlyLargeSpan<T> : IReadOnlyLargeArray<T>
{
    public ReadOnlyLargeSpan(IReadOnlyLargeArray<T> inner)
    {
        if (inner is null)
        {
            throw new ArgumentNullException(nameof(inner));
        }

        _Inner = inner;
        _Start = 0L;
        _Count = inner.Count;
    }

    public ReadOnlyLargeSpan(IReadOnlyLargeArray<T> inner, long start)
    {
        if (inner is null)
        {
            throw new ArgumentNullException(nameof(inner));
        }

        long count = inner.Count;
        StorageExtensions.CheckIndex(start, count);

        _Inner = inner;
        _Start = start;
        _Count = count - start;
    }

    public ReadOnlyLargeSpan(IReadOnlyLargeArray<T> inner, long start, long count)
    {
        if (inner is null)
        {
            throw new ArgumentNullException(nameof(inner));
        }

        StorageExtensions.CheckRange(start, count, inner.Count);

        _Inner = inner;
        _Start = start;
        _Count = count;
    }

    public ReadOnlyLargeSpan(ReadOnlyLargeSpan<T> span, long start)
    {
        if (span._Inner is null)
        {
            throw new ArgumentException("Invalid span");
        }

        StorageExtensions.CheckIndex(start, span._Count);

        _Inner = span._Inner;
        _Start = span._Start + start;
        _Count = span._Count - start;
    }

    public ReadOnlyLargeSpan(ReadOnlyLargeSpan<T> span, long start, long count)
    {
        if (span._Inner is null)
        {
            throw new ArgumentException("Invalid span");
        }

        StorageExtensions.CheckRange(start, count, span._Count);

        _Inner = span._Inner;
        _Start = span._Start + start;
        _Count = count;
    }

    public ReadOnlyLargeSpan(LargeSpan<T> span, long start)
    {
        if (span.Inner is null)
        {
            throw new ArgumentException("Invalid span");
        }

        StorageExtensions.CheckIndex(start, span.Count);

        _Inner = span.Inner;
        _Start = span.Start + start;
        _Count = span.Count - start;
    }

    public ReadOnlyLargeSpan(LargeSpan<T> span, long start, long count)
    {
        if (span.Inner is null)
        {
            throw new ArgumentException("Invalid span");
        }

        StorageExtensions.CheckRange(start, count, span.Count);

        _Inner = span.Inner;
        _Start = span.Start + start;
        _Count = count;
    }

    private readonly IReadOnlyLargeArray<T> _Inner;

    internal readonly IReadOnlyLargeArray<T> Inner
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => EnsureInner();
    }

    private readonly long _Start;

    public readonly long Start
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Start;
    }

    private readonly long _Count;


    public readonly long Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Count;
    }

    private static readonly LargeArray<T> _EmptyLargeArray = new(0L);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly IReadOnlyLargeArray<T> EnsureInner()
    {
        return _Inner ?? _EmptyLargeArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyLargeSpan<T> Slice(long start)
    {
        return new(this, start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyLargeSpan<T> Slice(long start, long count)
    {
        return new(this, start, count);
    }

    public T this[long index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            IReadOnlyLargeArray<T> inner = EnsureInner();
            StorageExtensions.CheckIndex(index, _Count);
            T result = inner[_Start + index];
            return result;
        }
    }

    /// <summary>
    /// Performs a binary search using a generic comparer for optimal performance through JIT devirtualization.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch<TComparer>(T item, TComparer comparer, long? offset = null, long? count = null) where TComparer : IComparer<T>
    {
        IReadOnlyLargeArray<T> inner = EnsureInner();
        long actualOffset = offset ?? 0L;
        long actualCount = count ?? (_Count - actualOffset);
        StorageExtensions.CheckRange(actualOffset, actualCount, _Count);
        long start = _Start + actualOffset;
        long result;
        if (inner is LargeArray<T> largeArray)
        {
            result = largeArray.BinarySearch(item, comparer, start, actualCount);
        }
        else if (inner is LargeList<T> largeList)
        {
            result = largeList.BinarySearch(item, comparer, start, actualCount);
        }
        else
        {
            throw new NotSupportedException($"Generic BinarySearch is not supported for inner type {inner.GetType().Name}. Use the delegate-based overload instead.");
        }
        return result >= 0L ? result - _Start : result;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch(T item, long? offset = null, long? count = null)
    {
        return BinarySearch(item, Comparer<T>.Default, offset, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item) => Contains(item, 0L, _Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item, long offset, long count)
    {
        IReadOnlyLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        bool result = inner.Contains(item, start, count);
        return result;
    }

    /// <summary>
    /// Determines whether the span contains a specific item using a generic equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>
    {
        IReadOnlyLargeArray<T> inner = EnsureInner();
        long actualOffset = offset ?? 0L;
        long actualCount = count ?? (_Count - actualOffset);
        StorageExtensions.CheckRange(actualOffset, actualCount, _Count);
        if (actualCount == 0L) return false;
        long start = _Start + actualOffset;
        if (inner is LargeArray<T> largeArray)
        {
            return largeArray.Contains(item, ref comparer, start, actualCount);
        }
        if (inner is LargeList<T> largeList)
        {
            return largeList.Contains(item, ref comparer, start, actualCount);
        }
        throw new NotSupportedException($"Generic Contains is not supported for inner type {inner.GetType().Name}. Use the delegate-based overload instead.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(ILargeArray<T> target, long sourceOffset, long targetOffset, long count)
    {
        if (count == 0L)
        {
            return;
        }

        IReadOnlyLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(sourceOffset, count, _Count);

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        StorageExtensions.CheckRange(targetOffset, count, target.Count);

        long sourceStart = _Start + sourceOffset;
        inner.CopyTo(target, sourceStart, targetOffset, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(LargeSpan<T> target, long sourceOffset, long count)
    {
        if (count == 0L)
        {
            return;
        }

        IReadOnlyLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(sourceOffset, count, _Count);
        StorageExtensions.CheckRange(0L, count, target.Count);
        long sourceStart = _Start + sourceOffset;
        inner.CopyTo(target.Inner, sourceStart, target.Start, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyToArray(T[] target, long sourceOffset, int targetOffset, int count)
    {
        if (count == 0L)
        {
            return;
        }

        IReadOnlyLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(sourceOffset, count, _Count);

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        StorageExtensions.CheckRange(targetOffset, count, target.Length);
        long sourceStart = _Start + sourceOffset;
        inner.CopyToArray(target, sourceStart, targetOffset, count);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyToSpan(Span<T> target, long sourceOffset, int count)
    {
        if (count == 0L)
        {
            return;
        }

        IReadOnlyLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(sourceOffset, count, _Count);
        StorageExtensions.CheckRange(0L, count, target.Length);
        long sourceStart = _Start + sourceOffset;
        inner.CopyToSpan(target.Slice(0, count), sourceStart, count);
    }
#endif

    #region DoForEach Methods

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the span.
    /// </summary>
    /// <param name="action">The function that will be called for each item.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action) => DoForEach(action, 0L, _Count);

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the span within the specified range.
    /// </summary>
    /// <param name="action">The function that will be called for each item.</param>
    /// <param name="offset">Starting offset within the span.</param>
    /// <param name="count">Number of elements to process.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action, long offset, long count)
    {
        IReadOnlyLargeArray<T> inner = EnsureInner();
        
        if (count == 0L)
        {
            return;
        }

        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        inner.DoForEach(action, start, count);
    }

    /// <summary>
    /// Performs the action on items using an action for optimal performance.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeAction{T}"/>.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<T> => DoForEach(ref action, 0L, _Count);

    /// <summary>
    /// Performs the action on items using an action for optimal performance.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeAction{T}"/>.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    /// <param name="offset">Starting offset within the span.</param>
    /// <param name="count">Number of elements to process.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action, long offset, long count) where TAction : ILargeAction<T>
    {
        IReadOnlyLargeArray<T> inner = EnsureInner();
        
        if (count == 0L)
        {
            return;
        }

        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        inner.DoForEach(ref action, start, count);
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(long index)
    {
        IReadOnlyLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckIndex(index, _Count);
        T result = inner.Get(_Start + index);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll(long offset, long count)
    {
        IReadOnlyLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        return inner.GetAll(start, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll()
    {
        IReadOnlyLargeArray<T> inner = EnsureInner();
        return inner.GetAll(_Start, _Count);
    }

    /// <summary>
    /// Returns a high-performance struct enumerator for efficient iteration.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyLargeSpanEnumerator<T> GetEnumerator()
    {
        return new ReadOnlyLargeSpanEnumerator<T>(EnsureInner(), _Start, _Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return new ReadOnlyLargeSpanEnumerator<T>(EnsureInner(), _Start, _Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IndexOf(T item, long? offset = null, long? count = null)
    {
        IReadOnlyLargeArray<T> inner = EnsureInner();
        long actualOffset = offset ?? 0L;
        long actualCount = count ?? (_Count - actualOffset);
        StorageExtensions.CheckRange(actualOffset, actualCount, _Count);
        long start = _Start + actualOffset;
        long result = inner.IndexOf(item, start, actualCount);
        return result >= 0L ? result - _Start : result;
    }

    /// <summary>
    /// Finds the index of the first occurrence of an item using a generic equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IndexOf<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>
    {
        IReadOnlyLargeArray<T> inner = EnsureInner();
        long actualOffset = offset ?? 0L;
        long actualCount = count ?? (_Count - actualOffset);
        StorageExtensions.CheckRange(actualOffset, actualCount, _Count);
        long start = _Start + actualOffset;
        long result;
        if (inner is LargeArray<T> largeArray)
        {
            result = largeArray.IndexOf(item, ref comparer, start, actualCount);
        }
        else if (inner is LargeList<T> largeList)
        {
            result = largeList.IndexOf(item, ref comparer, start, actualCount);
        }
        else
        {
            throw new NotSupportedException($"Generic IndexOf is not supported for inner type {inner.GetType().Name}. Use the delegate-based overload instead.");
        }
        return result >= 0L ? result - _Start : result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LastIndexOf(T item, long? offset = null, long? count = null)
    {
        IReadOnlyLargeArray<T> inner = EnsureInner();
        long actualOffset = offset ?? 0L;
        long actualCount = count ?? (_Count - actualOffset);
        StorageExtensions.CheckRange(actualOffset, actualCount, _Count);
        long start = _Start + actualOffset;
        long result = inner.LastIndexOf(item, start, actualCount);
        return result >= 0L ? result - _Start : result;
    }

    /// <summary>
    /// Finds the index of the last occurrence of an item using a generic equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LastIndexOf<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>
    {
        IReadOnlyLargeArray<T> inner = EnsureInner();
        long actualOffset = offset ?? 0L;
        long actualCount = count ?? (_Count - actualOffset);
        StorageExtensions.CheckRange(actualOffset, actualCount, _Count);
        long start = _Start + actualOffset;
        long result;
        if (inner is LargeArray<T> largeArray)
        {
            result = largeArray.LastIndexOf(item, ref comparer, start, actualCount);
        }
        else if (inner is LargeList<T> largeList)
        {
            result = largeList.LastIndexOf(item, ref comparer, start, actualCount);
        }
        else
        {
            throw new NotSupportedException($"Generic LastIndexOf is not supported for inner type {inner.GetType().Name}. Use the delegate-based overload instead.");
        }
        return result >= 0L ? result - _Start : result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return new ReadOnlyLargeSpanEnumerator<T>(EnsureInner(), _Start, _Count);
    }
}

/// <summary>
/// High-performance struct enumerator for ReadOnlyLargeSpan.
/// Uses direct storage access when available, falls back to indexed access otherwise.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public struct ReadOnlyLargeSpanEnumerator<T> : IEnumerator<T>
{
    private readonly IReadOnlyLargeArray<T> _inner;
    private readonly long _start;
    private readonly long _count;
    private long _index;
    private T _current;

    // Optimized path: direct storage access
    private readonly T[][] _storage;
    private int _storageIndex;
    private int _itemIndex;
    private int _currentChunkLength;
    private T[] _currentChunk;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlyLargeSpanEnumerator(IReadOnlyLargeArray<T> inner, long start, long count)
    {
        _inner = inner;
        _start = start;
        _count = count;
        _index = -1;
        _current = default!;

        // Try to get direct storage access for optimal performance
        if (inner is LargeArray<T> largeArray)
        {
            _storage = largeArray.GetStorage();
        }
        else if (inner is LargeList<T> largeList)
        {
            _storage = largeList.GetStorage();
        }
        else
        {
            _storage = null!;
        }

        if (_storage != null && count > 0)
        {
            (int storageIndex, int itemIndex) = StorageExtensions.StorageGetIndex(start);
            _storageIndex = storageIndex;
            _itemIndex = itemIndex - 1; // Will be incremented in MoveNext
            _currentChunk = _storage[storageIndex];
            _currentChunkLength = _currentChunk.Length;
        }
        else
        {
            _storageIndex = 0;
            _itemIndex = -1;
            _currentChunk = null!;
            _currentChunkLength = 0;
        }
    }

    /// <inheritdoc/>
    public readonly T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _current;
    }

    /// <inheritdoc/>
    readonly object System.Collections.IEnumerator.Current => _current!;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        _index++;
        if (_index >= _count)
        {
            return false;
        }

        // Optimized path: direct storage access
        if (_storage != null)
        {
            _itemIndex++;

            // Check if we need to move to the next chunk
            if (_itemIndex >= _currentChunkLength)
            {
                _storageIndex++;
                if (_storageIndex >= _storage.Length)
                {
                    return false;
                }
                _currentChunk = _storage[_storageIndex];
                _currentChunkLength = _currentChunk.Length;
                _itemIndex = 0;
            }

            _current = _currentChunk[_itemIndex];
        }
        else
        {
            // Fallback: use interface indexed access
            _current = _inner[_start + _index];
        }

        return true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public readonly void Dispose()
    {
        // Nothing to dispose
    }
}