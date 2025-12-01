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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LargeCollections.Observable;

[DebuggerDisplay("LargeObservableCollection: Count = {Count}")]
public class LargeObservableCollection<T> : ILargeObservableCollection<T>
{
    private readonly bool _SuppressEventExceptions;
    private readonly LargeList<T> _List;

    private long _SuspendNotificationsCounter = 0;
    private long _ChangesWhileSuspended = 0;
    private long _CountBeforeSuspend = 0;

    public LargeObservableCollection() : this(suppressEventExceptions: false)
    {
    }

    public LargeObservableCollection(long initialCapacity) : this(initialCapacity, suppressEventExceptions: false)
    {
    }

    public LargeObservableCollection(bool suppressEventExceptions) : this(0, suppressEventExceptions)
    {
    }

    public LargeObservableCollection(long initialCapacity, bool suppressEventExceptions)
    {
        _List = new(initialCapacity);
        _SuppressEventExceptions = suppressEventExceptions;
    }

    #region Standard Events

    public event NotifyCollectionChangedEventHandler CollectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region High-Performance Events

    /// <inheritdoc/>
    public event LargeCollectionChangedEventHandler<T> Changed;

    #endregion

    private static readonly NotifyCollectionChangedEventArgs _ResetEventArgs = new(NotifyCollectionChangedAction.Reset);
    private static readonly PropertyChangedEventArgs _CountPropertyChangedEventArgs = new(nameof(Count));

    #region Event Firing Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PublishChangedCount()
    {
        if (Interlocked.CompareExchange(ref _SuspendNotificationsCounter, 0, 0) > 0)
        {
            return;
        }

        // Fire PropertyChanged event with optional exception handling
        if (PropertyChanged != null)
        {
            if (_SuppressEventExceptions)
            {
                try
                {
                    PropertyChanged.Invoke(this, _CountPropertyChangedEventArgs);
                }
                catch
                {
                    // Suppress exceptions if configured
                }
            }
            else
            {
                PropertyChanged.Invoke(this, _CountPropertyChangedEventArgs);
            }
        }
    }

    /// <summary>
    /// Raises collection changed event with optional exception handling.
    /// For multiple item changes or large indices, fires Reset event.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (Interlocked.CompareExchange(ref _SuspendNotificationsCounter, 0, 0) > 0)
        {
            Interlocked.Increment(ref _ChangesWhileSuspended);
            return;
        }

        if (CollectionChanged != null)
        {
            if (_SuppressEventExceptions)
            {
                try
                {
                    CollectionChanged.Invoke(this, e);
                }
                catch
                {
                    // Suppress exceptions if configured
                }
            }
            else
            {
                CollectionChanged.Invoke(this, e);
            }
        }
    }

    /// <summary>
    /// Raises the high-performance Changed event with the specified event args.
    /// Only fires if someone is listening.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RaiseChanged(in LargeCollectionChangedEventArgs<T> args)
    {
        if (Interlocked.CompareExchange(ref _SuspendNotificationsCounter, 0, 0) > 0)
        {
            return;
        }

        // Fire Changed event only if someone is listening
        if (Changed != null)
        {
            if (_SuppressEventExceptions)
            {
                try { Changed.Invoke(this, in args); } catch { }
            }
            else
            {
                Changed.Invoke(this, in args);
            }
        }
    }

    #endregion

    public T this[long index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _List[index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            StorageExtensions.CheckRange(index, 1, Count);
            T oldItem = _List[index];
            _List[index] = value;

            // Fire standard event only if someone is listening
            if (CollectionChanged != null)
            {
                if (index <= int.MaxValue)
                {
                    OnCollectionChanged(new(NotifyCollectionChangedAction.Replace, value, oldItem, (int)index));
                }
                else
                {
                    OnCollectionChanged(_ResetEventArgs);
                }
            }

            // Fire high-performance event only if someone is listening
            if (Changed != null)
            {
                RaiseChanged(LargeCollectionChangedEventArgs<T>.ItemReplaced(value, oldItem, index));
            }
        }
    }

    public long Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return _List.Count;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        long index = Count;
        _List.Add(item);

        // Fire standard event only if someone is listening
        if (CollectionChanged != null)
        {
            if (index <= int.MaxValue)
            {
                OnCollectionChanged(new(NotifyCollectionChangedAction.Add, item, (int)index));
            }
            else
            {
                OnCollectionChanged(_ResetEventArgs);
            }
        }

        // Fire high-performance event only if someone is listening
        if (Changed != null)
        {
            RaiseChanged(LargeCollectionChangedEventArgs<T>.ItemAdded(item, index));
        }

        // Fire PropertyChanged only if someone is listening
        if (PropertyChanged != null)
        {
            PublishChangedCount();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IEnumerable<T> items)
    {
        long initialCount = _List.Count;
        _List.AddRange(items);

        long addedCount = _List.Count - initialCount;
        if (addedCount == 0)
        {
            return;
        }
        else if (addedCount == 1)
        {
            // Single item added
            T addedItem = _List[initialCount];
            if (CollectionChanged != null)
            {
                if (initialCount <= int.MaxValue)
                {
                    OnCollectionChanged(new(NotifyCollectionChangedAction.Add, addedItem, (int)initialCount));
                }
                else
                {
                    OnCollectionChanged(_ResetEventArgs);
                }
            }
            if (Changed != null)
            {
                RaiseChanged(LargeCollectionChangedEventArgs<T>.ItemAdded(addedItem, initialCount));
            }
            if (PropertyChanged != null)
            {
                PublishChangedCount();
            }
        }
        else
        {
            // Multiple items added
            if (CollectionChanged != null)
            {
                OnCollectionChanged(_ResetEventArgs);
            }
            if (Changed != null)
            {
                RaiseChanged(LargeCollectionChangedEventArgs<T>.RangeAdded(initialCount, addedCount));
            }
            if (PropertyChanged != null)
            {
                PublishChangedCount();
            }
        }
    }

    /// <summary>
    /// Adds a range of items from a large array to the collection.
    /// </summary>
    /// <param name="source">The source large array.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyLargeArray<T> source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        AddRange(source, 0, source.Count);
    }

    /// <summary>
    /// Adds a range of items from a large array to the collection.
    /// </summary>
    /// <param name="source">The source large array.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyLargeArray<T> source, long offset)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        AddRange(source, offset, source.Count - offset);
    }

    /// <summary>
    /// Adds a range of items from a large array to the collection.
    /// </summary>
    /// <param name="source">The source large array.</param>
    /// <param name="offset">The offset in the source array to start adding from.</param>
    /// <param name="count">The number of items to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyLargeArray<T> source, long offset, long count)
    {
        if (count == 0L)
        {
            return;
        }

        long initialCount = _List.Count;
        _List.AddRange(source, offset, count);

        long addedCount = _List.Count - initialCount;
        if (addedCount == 0)
        {
            return;
        }
        else if (addedCount == 1)
        {
            // Single item added
            T addedItem = _List[initialCount];
            if (CollectionChanged != null)
            {
                if (initialCount <= int.MaxValue)
                {
                    OnCollectionChanged(new(NotifyCollectionChangedAction.Add, addedItem, (int)initialCount));
                }
                else
                {
                    OnCollectionChanged(_ResetEventArgs);
                }
            }
            if (Changed != null)
            {
                RaiseChanged(LargeCollectionChangedEventArgs<T>.ItemAdded(addedItem, initialCount));
            }
            if (PropertyChanged != null)
            {
                PublishChangedCount();
            }
        }
        else
        {
            // Multiple items added
            if (CollectionChanged != null)
            {
                OnCollectionChanged(_ResetEventArgs);
            }
            if (Changed != null)
            {
                RaiseChanged(LargeCollectionChangedEventArgs<T>.RangeAdded(initialCount, addedCount));
            }
            if (PropertyChanged != null)
            {
                PublishChangedCount();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlyLargeSpan<T> source)
    {
        if (source.Count == 0)
        {
            return;
        }

        long initialCount = _List.Count;
        _List.AddRange(source);

        long addedCount = _List.Count - initialCount;
        if (addedCount == 0)
        {
            return;
        }
        else if (addedCount == 1)
        {
            // Single item added
            T addedItem = _List[initialCount];
            if (CollectionChanged != null)
            {
                if (initialCount <= int.MaxValue)
                {
                    OnCollectionChanged(new(NotifyCollectionChangedAction.Add, addedItem, (int)initialCount));
                }
                else
                {
                    OnCollectionChanged(_ResetEventArgs);
                }
            }
            if (Changed != null)
            {
                RaiseChanged(LargeCollectionChangedEventArgs<T>.ItemAdded(addedItem, initialCount));
            }
            if (PropertyChanged != null)
            {
                PublishChangedCount();
            }
        }
        else
        {
            // Multiple items added
            if (CollectionChanged != null)
            {
                OnCollectionChanged(_ResetEventArgs);
            }
            if (Changed != null)
            {
                RaiseChanged(LargeCollectionChangedEventArgs<T>.RangeAdded(initialCount, addedCount));
            }
            if (PropertyChanged != null)
            {
                PublishChangedCount();
            }
        }
    }

    /// <summary>
    /// Adds a range of items from an array to the collection.
    /// </summary>
    /// <param name="source">The source array.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(T[] source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        AddRange(source, 0, source.Length);
    }

    /// <summary>
    /// Adds a range of items from an array to the collection.
    /// </summary>
    /// <param name="source">The source array.</param>
    /// <param name="offset">The offset in the source array to start adding from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(T[] source, int offset)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        AddRange(source, offset, source.Length - offset);
    }

    /// <summary>
    /// Adds a range of items from an array to the collection.
    /// </summary>
    /// <param name="source">The source array.</param>
    /// <param name="offset">The offset in the source array to start adding from.</param>
    /// <param name="count">The number of items to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(T[] source, int offset, int count)
    {
        if (count == 0)
        {
            return;
        }

        long initialCount = _List.Count;
        _List.AddRange(source, offset, count);

        long addedCount = _List.Count - initialCount;
        if (addedCount == 0)
        {
            return;
        }
        else if (addedCount == 1)
        {
            // Single item added
            T addedItem = _List[initialCount];
            if (CollectionChanged != null)
            {
                if (initialCount <= int.MaxValue)
                {
                    OnCollectionChanged(new(NotifyCollectionChangedAction.Add, addedItem, (int)initialCount));
                }
                else
                {
                    OnCollectionChanged(_ResetEventArgs);
                }
            }
            if (Changed != null)
            {
                RaiseChanged(LargeCollectionChangedEventArgs<T>.ItemAdded(addedItem, initialCount));
            }
            if (PropertyChanged != null)
            {
                PublishChangedCount();
            }
        }
        else
        {
            // Multiple items added
            if (CollectionChanged != null)
            {
                OnCollectionChanged(_ResetEventArgs);
            }
            if (Changed != null)
            {
                RaiseChanged(LargeCollectionChangedEventArgs<T>.RangeAdded(initialCount, addedCount));
            }
            if (PropertyChanged != null)
            {
                PublishChangedCount();
            }
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlySpan<T> source)
    {
        if (source.Length == 0)
        {
            return;
        }

        long initialCount = _List.Count;
        _List.AddRange(source);

        long addedCount = _List.Count - initialCount;
        if (addedCount == 0)
        {
            return;
        }
        else if (addedCount == 1)
        {
            // Single item added
            T addedItem = _List[initialCount];
            if (CollectionChanged != null)
            {
                if (initialCount <= int.MaxValue)
                {
                    OnCollectionChanged(new(NotifyCollectionChangedAction.Add, addedItem, (int)initialCount));
                }
                else
                {
                    OnCollectionChanged(_ResetEventArgs);
                }
            }
            if (Changed != null)
            {
                RaiseChanged(LargeCollectionChangedEventArgs<T>.ItemAdded(addedItem, initialCount));
            }
            if (PropertyChanged != null)
            {
                PublishChangedCount();
            }
        }
        else
        {
            // Multiple items added
            if (CollectionChanged != null)
            {
                OnCollectionChanged(_ResetEventArgs);
            }
            if (Changed != null)
            {
                RaiseChanged(LargeCollectionChangedEventArgs<T>.RangeAdded(initialCount, addedCount));
            }
            if (PropertyChanged != null)
            {
                PublishChangedCount();
            }
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        long previousCount = Count;
        if (previousCount == 0)
        {
            return;
        }

        _List.Clear();

        if (CollectionChanged != null)
        {
            OnCollectionChanged(_ResetEventArgs);
        }
        if (Changed != null)
        {
            RaiseChanged(LargeCollectionChangedEventArgs<T>.Cleared(previousCount));
        }
        if (PropertyChanged != null)
        {
            PublishChangedCount();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T item)
        => Remove(item, out _, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T item, out T removedItem)
        => Remove(item, out removedItem, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T item, out T removedItem, bool preserveOrder = true)
    {
        ObjectEqualityComparer<T> comparer = new ();
        return Remove(item, out removedItem, comparer, preserveOrder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove<TComparer>(T item, out T removedItem, TComparer comparer, bool preserveOrder = true)
        where TComparer : IEqualityComparer<T>
    {
        removedItem = default;

        if (_List.Remove(item, out removedItem, comparer, preserveOrder))
        {
            if (CollectionChanged != null)
            {
                OnCollectionChanged(_ResetEventArgs);
            }
            if (Changed != null)
            {
                RaiseChanged(LargeCollectionChangedEventArgs<T>.Reset());
            }
            if (PropertyChanged != null)
            {
                PublishChangedCount();
            }

            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T RemoveAt(long index, bool preserveOrder = true)
    {
        T removedItem = _List.RemoveAt(index, preserveOrder);

        if (CollectionChanged != null)
        {
            if (index <= int.MaxValue)
            {
                OnCollectionChanged(new(NotifyCollectionChangedAction.Remove, removedItem, (int)index));
            }
            else
            {
                OnCollectionChanged(_ResetEventArgs);
            }
        }
        if (Changed != null)
        {
            RaiseChanged(LargeCollectionChangedEventArgs<T>.ItemRemoved(removedItem, index));
        }
        if (PropertyChanged != null)
        {
            PublishChangedCount();
        }

        return removedItem;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(long index, T item)
    {
        StorageExtensions.CheckRange(index, 1, Count);

        T oldItem = _List[index];
        _List.Set(index, item);

        if (CollectionChanged != null)
        {
            if (index <= int.MaxValue)
            {
                OnCollectionChanged(new(NotifyCollectionChangedAction.Replace, item, oldItem, (int)index));
            }
            else
            {
                OnCollectionChanged(_ResetEventArgs);
            }
        }
        if (Changed != null)
        {
            RaiseChanged(LargeCollectionChangedEventArgs<T>.ItemReplaced(item, oldItem, index));
        }
    }

    /// <summary>
    /// Performs a binary search using a generic comparer for optimal performance through JIT devirtualization.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch<TComparer>(T item, TComparer comparer, long? offset = null, long? count = null) where TComparer : IComparer<T>
        => _List.BinarySearch(item, comparer, offset, count);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch(T item, long? offset = null, long? count = null)
        => _List.BinarySearch(item, offset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
        => _List.Contains(item);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item, long offset, long count)
        => _List.Contains(item, offset, count);

    /// <summary>
    /// Determines whether the collection contains a specific item using a generic equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>
        => _List.Contains(item, ref comparer, offset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(IReadOnlyLargeArray<T> source, long sourceOffset, long targetOffset, long count)
    {
        if (count == 0L)
        {
            return;
        }

        if (count == 1L)
        {
            StorageExtensions.CheckRange(targetOffset, count, Count);
            StorageExtensions.CheckRange(sourceOffset, count, source.Count);
            T oldItem = _List[targetOffset];
            T newItem = source[sourceOffset];
            _List[targetOffset] = newItem;

            if (CollectionChanged != null)
            {
                if (targetOffset <= int.MaxValue)
                {
                    OnCollectionChanged(new(NotifyCollectionChangedAction.Replace, newItem, oldItem, (int)targetOffset));
                }
                else
                {
                    OnCollectionChanged(_ResetEventArgs);
                }
            }
        }
        else
        {
            _List.CopyFrom(source, sourceOffset, targetOffset, count);
            if (CollectionChanged != null)
            {
                OnCollectionChanged(_ResetEventArgs);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(ReadOnlyLargeSpan<T> source, long targetOffset, long count)
    {
        if (count == 0L)
        {
            return;
        }

        if (count == 1L)
        {
            StorageExtensions.CheckRange(targetOffset, count, Count);
            StorageExtensions.CheckRange(0L, count, source.Count);
            T oldItem = _List[targetOffset];
            T newItem = source[0L];
            _List[targetOffset] = newItem;

            if (CollectionChanged != null)
            {
                if (targetOffset <= int.MaxValue)
                {
                    OnCollectionChanged(new(NotifyCollectionChangedAction.Replace, newItem, oldItem, (int)targetOffset));
                }
                else
                {
                    OnCollectionChanged(_ResetEventArgs);
                }
            }
        }
        else
        {
            _List.CopyFrom(source, targetOffset, count);
            if (CollectionChanged != null)
            {
                OnCollectionChanged(_ResetEventArgs);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFromArray(T[] source, int sourceOffset, long targetOffset, int count)
    {
        if (count == 0L)
        {
            return;
        }

        if (count == 1L)
        {
            StorageExtensions.CheckRange(targetOffset, count, Count);
            StorageExtensions.CheckRange(sourceOffset, count, source.Length);
            T oldItem = _List[targetOffset];
            T newItem = source[sourceOffset];
            _List[targetOffset] = newItem;

            if (CollectionChanged != null)
            {
                if (targetOffset <= int.MaxValue)
                {
                    OnCollectionChanged(new(NotifyCollectionChangedAction.Replace, newItem, oldItem, (int)targetOffset));
                }
                else
                {
                    OnCollectionChanged(_ResetEventArgs);
                }
            }
        }
        else
        {
            _List.CopyFromArray(source, sourceOffset, targetOffset, count);
            if (CollectionChanged != null)
            {
                OnCollectionChanged(_ResetEventArgs);
            }
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFromSpan(ReadOnlySpan<T> source, long targetOffset, int count)
    {
        if (count == 0L)
        {
            return;
        }

        if (count == 1L)
        {
            StorageExtensions.CheckRange(targetOffset, count, Count);
            StorageExtensions.CheckRange(0, count, source.Length);
            T oldItem = _List[targetOffset];
            T newItem = source[0];
            _List[targetOffset] = newItem;

            if (CollectionChanged != null)
            {
                if (targetOffset <= int.MaxValue)
                {
                    OnCollectionChanged(new(NotifyCollectionChangedAction.Replace, newItem, oldItem, (int)targetOffset));
                }
                else
                {
                    OnCollectionChanged(_ResetEventArgs);
                }
            }
        }
        else
        {
            _List.CopyFromSpan(source, targetOffset, count);
            if (CollectionChanged != null)
            {
                OnCollectionChanged(_ResetEventArgs);
            }
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(ILargeArray<T> target, long sourceOffset, long targetOffset, long count)
        => _List.CopyTo(target, sourceOffset, targetOffset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(LargeSpan<T> target, long sourceOffset, long count)
        => _List.CopyTo(target, sourceOffset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyToArray(T[] target, long sourceOffset, int targetOffset, int count)
        => _List.CopyToArray(target, sourceOffset, targetOffset, count);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyToSpan(Span<T> target, long sourceOffset, int count)
        => _List.CopyToSpan(target, sourceOffset, count);
#endif

    #region DoForEach Methods

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection.
    /// </summary>
    /// <param name="action">The function that will be called for each item.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action)
        => _List.DoForEach(action);

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection within the specified range.
    /// </summary>
    /// <param name="action">The function that will be called for each item.</param>
    /// <param name="offset">Starting offset.</param>
    /// <param name="count">Number of elements to process.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action, long offset, long count)
        => _List.DoForEach(action, offset, count);

    /// <summary>
    /// Performs the action on items using an action for optimal performance.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeAction{T}"/>.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<T>
        => _List.DoForEach(ref action);

    /// <summary>
    /// Performs the action on items using an action for optimal performance.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeAction{T}"/>.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    /// <param name="offset">Starting offset.</param>
    /// <param name="count">Number of elements to process.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action, long offset, long count) where TAction : ILargeAction<T>
        => _List.DoForEach(ref action, offset, count);

    /// <summary>
    /// Performs the action on each item by reference using an action.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeRefAction{T}"/>.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    /// <param name="offset">Optional starting offset. If null, starts from 0.</param>
    /// <param name="count">Optional number of elements to process. If null, processes all remaining elements.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEachRef<TAction>(ref TAction action, long? offset = null, long? count = null) where TAction : ILargeRefAction<T>
        => _List.DoForEachRef(ref action, offset, count);

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(long index) => _List.Get(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll(long offset, long count)
        => _List.GetAll(offset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll()
        => _List.GetAll();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<T> GetEnumerator()
        => _List.GetEnumerator();

    /// <summary>
    /// Reorders the items of the collection in ascending order using a generic comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Sort<TComparer>(TComparer comparer, long? offset = null, long? count = null) where TComparer : IComparer<T>
    {
        _List.Sort(comparer, offset, count);

        long actualCount = count ?? Count;
        if (actualCount <= 1L)
        {
            return;
        }

        if (CollectionChanged != null)
        {
            OnCollectionChanged(_ResetEventArgs);
        }
        if (Changed != null)
        {
            RaiseChanged(LargeCollectionChangedEventArgs<T>.Reset());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Swap(long leftIndex, long rightIndex)
    {
        _List.Swap(leftIndex, rightIndex);

        if (CollectionChanged != null)
        {
            OnCollectionChanged(_ResetEventArgs);
        }
        if (Changed != null)
        {
            RaiseChanged(LargeCollectionChangedEventArgs<T>.Reset());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyLargeObservableCollection<T> AsReadOnly()
        => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDisposable SuspendNotifications()
    {
        // Capture count before suspension for comparison later
        if (Interlocked.Increment(ref _SuspendNotificationsCounter) == 1)
        {
            _CountBeforeSuspend = Count;
            Interlocked.Exchange(ref _ChangesWhileSuspended, 0);
        }

        return new NotificationSuspender(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IndexOf(T item, long? offset = null, long? count = null)
        => _List.IndexOf(item, offset, count);

    /// <summary>
    /// Finds the index of the first occurrence of an item using a generic equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IndexOf<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>
        => _List.IndexOf(item, ref comparer, offset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LastIndexOf(T item, long? offset = null, long? count = null)
        => _List.LastIndexOf(item, offset, count);

    /// <summary>
    /// Finds the index of the last occurrence of an item using a generic equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LastIndexOf<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>
        => _List.LastIndexOf(item, ref comparer, offset, count);

    internal class NotificationSuspender : IDisposable
    {
        private readonly LargeObservableCollection<T> _Collection;
        private bool _Disposed = false;

        public NotificationSuspender(LargeObservableCollection<T> collection)
        {
            _Collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        public void Dispose()
        {
            if (!_Disposed)
            {
                // Atomically decrement counter
                if (Interlocked.Decrement(ref _Collection._SuspendNotificationsCounter) == 0)
                {
                    // Last suspension removed - check if we need to fire events
                    long changeCount = Interlocked.Exchange(ref _Collection._ChangesWhileSuspended, 0);
                    if (changeCount > 0)
                    {
                        // Fire reset event to indicate collection has changed
                        _Collection.OnCollectionChanged(_ResetEventArgs);

                        // Also fire count change if count has changed
                        if (_Collection.Count != _Collection._CountBeforeSuspend)
                        {
                            _Collection.PublishChangedCount();
                        }
                    }
                }
                _Disposed = true;
            }
        }
    }
}