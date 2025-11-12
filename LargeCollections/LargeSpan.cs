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

public readonly struct LargeSpan<T> : IRefAccessLargeArray<T>
{
    public LargeSpan(IRefAccessLargeArray<T> inner)
    {
        if (inner is null)
        {
            throw new ArgumentNullException(nameof(inner));
        }

        _Inner = inner;
        _Start = 0L;
        _Count = inner.Count;
    }

    public LargeSpan(IRefAccessLargeArray<T> inner, long start)
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

    public LargeSpan(IRefAccessLargeArray<T> inner, long start, long count)
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

    public LargeSpan(LargeSpan<T> span, long start)
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

    public LargeSpan(LargeSpan<T> span, long start, long count)
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

    private readonly IRefAccessLargeArray<T> _Inner;
    internal readonly IRefAccessLargeArray<T> Inner
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
    private IRefAccessLargeArray<T> EnsureInner()
    {
        return _Inner ?? _EmptyLargeArray;
    }

    public T this[long index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            IRefAccessLargeArray<T> inner = EnsureInner();
            StorageExtensions.CheckIndex(index, _Count);
            T result = inner[_Start + index];
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            IRefAccessLargeArray<T> inner = EnsureInner();
            StorageExtensions.CheckIndex(index, _Count);
            inner[_Start + index] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LargeSpan<T> Slice(long start)
        => new(this, start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LargeSpan<T> Slice(long start, long count)
        => new(this, start, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyLargeSpan<T>(LargeSpan<T> span)
        => new(span._Inner, span._Start, span._Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef(long index)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckIndex(index, _Count);
        return ref inner.GetRef(_Start + index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(RefAction<T> action)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        inner.DoForEach(action, _Start, _Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(RefAction<T> action, long offset, long count)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        inner.DoForEach(action, start, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(RefActionWithUserData<T, TUserData> action, ref TUserData userData)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        inner.DoForEach(action, _Start, _Count, ref userData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(RefActionWithUserData<T, TUserData> action, long offset, long count, ref TUserData userData)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        inner.DoForEach(action, start, count, ref userData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(long index, T item)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckIndex(index, _Count);
        inner.Set(_Start + index, item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Sort(Func<T, T, int> comparer)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        inner.Sort(comparer, _Start, _Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Sort(Func<T, T, int> comparer, long offset, long count)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        inner.Sort(comparer, start, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Swap(long leftIndex, long rightIndex)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckIndex(leftIndex, _Count);
        StorageExtensions.CheckIndex(rightIndex, _Count);

        inner.Swap(_Start + leftIndex, _Start + rightIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(IReadOnlyLargeArray<T> source, long sourceOffset, long targetOffset, long count)
    {
        if (count == 0L)
        {
            return;
        }

        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(targetOffset, count, _Count);

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        StorageExtensions.CheckRange(sourceOffset, count, source.Count);
        long targetStart = _Start + targetOffset;
        inner.CopyFrom(source, sourceOffset, targetStart, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFromArray(T[] source, int sourceOffset, long targetOffset, int count)
    {
        if (count == 0L)
        {
            return;
        }

        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(targetOffset, count, _Count);

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        StorageExtensions.CheckRange(sourceOffset, count, source.Length);
        long targetStart = _Start + targetOffset;
        inner.CopyFromArray(source, sourceOffset, targetStart, count);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFromSpan(ReadOnlySpan<T> source, long targetOffset, int count)
    {
        if (count == 0L)
        {
            return;
        }

        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(targetOffset, count, _Count);
        StorageExtensions.CheckRange(0L, count, source.Length);
        long targetStart = _Start + targetOffset;
        inner.CopyFromSpan(source, targetStart, count);
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(ReadOnlyLargeSpan<T> source, long targetOffset, long count)
    {
        if (count == 0L)
        {
            return;
        }

        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(targetOffset, count, _Count);
        StorageExtensions.CheckRange(0L, count, source.Count);
        long targetStart = _Start + targetOffset;
        inner.CopyFrom(source.Inner, source.Start, targetStart, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(long index)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckIndex(index, _Count);
        T result = inner.Get(_Start + index);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch(T item, Func<T, T, int> comparer)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        return inner.BinarySearch(item, comparer, _Start, _Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch(T item, Func<T, T, int> comparer, long offset, long count)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        long result = inner.BinarySearch(item, comparer, start, count);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll(long offset, long count)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        return inner.GetAll(start, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IndexOf(T item)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        long result = inner.IndexOf(item, _Start, _Count);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IndexOf(T item, Func<T, T, bool> equalsFunction)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        long result = inner.IndexOf(item, _Start, _Count, equalsFunction);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IndexOf(T item, long offset, long count)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        long result = inner.IndexOf(item, start, count);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IndexOf(T item, long offset, long count, Func<T, T, bool> equalsFunction)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        long result = inner.IndexOf(item, start, count, equalsFunction);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LastIndexOf(T item)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        long result = inner.LastIndexOf(item, _Start, _Count);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LastIndexOf(T item, Func<T, T, bool> equalsFunction)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        long result = inner.LastIndexOf(item, _Start, _Count, equalsFunction);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LastIndexOf(T item, long offset, long count)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        long result = inner.LastIndexOf(item, start, count);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LastIndexOf(T item, long offset, long count, Func<T, T, bool> equalsFunction)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        long result = inner.LastIndexOf(item, start, count, equalsFunction);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        return inner.Contains(item, _Start, _Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item, long offset, long count)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        bool result = inner.Contains(item, start, count);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item, Func<T, T, bool> equalsFunction)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        bool result = inner.Contains(item, _Start, _Count, equalsFunction);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item, long offset, long count, Func<T, T, bool> equalsFunction)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        bool result = inner.Contains(item, start, count, equalsFunction);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action, long offset, long count)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        inner.DoForEach(action, start, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(ActionWithUserData<T, TUserData> action, long offset, long count, ref TUserData userData)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        StorageExtensions.CheckRange(offset, count, _Count);
        long start = _Start + offset;
        inner.DoForEach(action, start, count, ref userData);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll()
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        return inner.GetAll(_Start, _Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        inner.DoForEach(action, _Start, _Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(ActionWithUserData<T, TUserData> action, ref TUserData userData)
    {
        IRefAccessLargeArray<T> inner = EnsureInner();
        inner.DoForEach(action, _Start, _Count, ref userData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<T> GetEnumerator()
    {
        return GetAll().GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetAll().GetEnumerator();
    }

}