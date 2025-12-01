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

[DebuggerDisplay("ReadOnlyLargeObservableCollection: Count = {Count}")]
public class ReadOnlyLargeObservableCollection<T> : IReadOnlyLargeObservableCollection<T>, IDisposable
{
    public ReadOnlyLargeObservableCollection(IReadOnlyLargeObservableCollection<T> innerObservableCollection) : this(innerObservableCollection, suppressEventExceptions: false)
    {
    }

    public ReadOnlyLargeObservableCollection(IReadOnlyLargeObservableCollection<T> innerObservableCollection, bool suppressEventExceptions)
    {
        _Inner = innerObservableCollection ?? throw new ArgumentNullException(nameof(innerObservableCollection));
        _SuppressEventExceptions = suppressEventExceptions;

        _Inner.CollectionChanged += OnInnerCollectionChanged;
        _Inner.PropertyChanged += OnInnerPropertyChanged;
        _Inner.Changed += OnInnerChanged;
    }

    private readonly IReadOnlyLargeObservableCollection<T> _Inner;
    private readonly bool _SuppressEventExceptions;

    private long _SuspendNotificationsCounter = 0;
    private long _ChangesWhileSuspended = 0;
    private long _CountBeforeSuspend = 0;

    private static readonly NotifyCollectionChangedEventArgs _ResetEventArgs = new(NotifyCollectionChangedAction.Reset);
    private static readonly PropertyChangedEventArgs _CountPropertyChangedEventArgs = new(nameof(Count));

    public T this[long index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Inner[index];
    }
    public long Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Inner.Count;
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    /// <inheritdoc/>
    public event LargeCollectionChangedEventHandler<T> Changed;

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

    private void OnInnerCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        OnCollectionChanged(e);
    }

    private void OnInnerPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Count))
        {
            PublishChangedCount();
        }
        else
        {
            if (Interlocked.CompareExchange(ref _SuspendNotificationsCounter, 0, 0) > 0)
            {
                return;
            }

            if (PropertyChanged != null)
            {
                if (_SuppressEventExceptions)
                {
                    try
                    {
                        PropertyChanged.Invoke(this, e);
                    }
                    catch
                    {
                        // Suppress exceptions if configured
                    }
                }
                else
                {
                    PropertyChanged.Invoke(this, e);
                }
            }
        }
    }

    private void OnInnerChanged(object sender, in LargeCollectionChangedEventArgs<T> e)
    {
        if (Interlocked.CompareExchange(ref _SuspendNotificationsCounter, 0, 0) > 0)
        {
            return;
        }

        if (Changed != null)
        {
            if (_SuppressEventExceptions)
            {
                try
                {
                    Changed.Invoke(this, in e);
                }
                catch
                {
                    // Suppress exceptions if configured
                }
            }
            else
            {
                Changed.Invoke(this, in e);
            }
        }
    }

    /// <summary>
    /// Performs a binary search using a generic comparer for optimal performance through JIT devirtualization.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch<TComparer>(T item, TComparer comparer, long? offset = null, long? count = null) where TComparer : IComparer<T>
    {
        if (_Inner is LargeObservableCollection<T> observable)
        {
            return observable.BinarySearch(item, comparer, offset, count);
        }
        throw new NotSupportedException($"Generic BinarySearch is not supported for inner type {_Inner.GetType().Name}. Use the delegate-based overload instead.");
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch(T item, long? offset = null, long? count = null)
    {
        if (_Inner is LargeObservableCollection<T> observable)
        {
            return observable.BinarySearch(item, offset, count);
        }
        throw new NotSupportedException($"BinarySearch is not supported for inner type {_Inner.GetType().Name}.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
        => _Inner.Contains(item);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item, long offset, long count)
        => _Inner.Contains(item, offset, count);

    /// <summary>
    /// Determines whether the collection contains a specific item using a generic equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>
    {
        if (_Inner is LargeObservableCollection<T> observable)
        {
            return observable.Contains(item, ref comparer, offset, count);
        }
        throw new NotSupportedException($"Generic Contains is not supported for inner type {_Inner.GetType().Name}. Use the delegate-based overload instead.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(ILargeArray<T> target, long sourceOffset, long targetOffset, long count)
        => _Inner.CopyTo(target, sourceOffset, targetOffset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(LargeSpan<T> target, long sourceOffset, long count)
       => _Inner.CopyTo(target, sourceOffset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyToArray(T[] target, long sourceOffset, int targetOffset, int count)
        => _Inner.CopyToArray(target, sourceOffset, targetOffset, count);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyToSpan(Span<T> target, long sourceOffset, int count)
        => _Inner.CopyToSpan(target, sourceOffset, count);
#endif

    #region DoForEach Methods

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection.
    /// </summary>
    /// <param name="action">The function that will be called for each item.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action)
        => _Inner.DoForEach(action);

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection within the specified range.
    /// </summary>
    /// <param name="action">The function that will be called for each item.</param>
    /// <param name="offset">Starting offset.</param>
    /// <param name="count">Number of elements to process.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action, long offset, long count)
        => _Inner.DoForEach(action, offset, count);

    /// <summary>
    /// Performs the action on items using an action for optimal performance.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeAction{T}"/>.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<T>
        => _Inner.DoForEach(ref action);

    /// <summary>
    /// Performs the action on items using an action for optimal performance.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeAction{T}"/>.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    /// <param name="offset">Starting offset.</param>
    /// <param name="count">Number of elements to process.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action, long offset, long count) where TAction : ILargeAction<T>
        => _Inner.DoForEach(ref action, offset, count);

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(long index)
        => _Inner.Get(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll(long offset, long count)
        => _Inner.GetAll(offset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll()
        => _Inner.GetAll();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<T> GetEnumerator()
        => _Inner.GetEnumerator();

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
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyLargeObservableCollection<T> AsReadOnly()
        => new(this);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe from inner collection's events
            _Inner.CollectionChanged -= OnInnerCollectionChanged;
            _Inner.PropertyChanged -= OnInnerPropertyChanged;
            _Inner.Changed -= OnInnerChanged;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IndexOf(T item, long? offset = null, long? count = null)
        => _Inner.IndexOf(item, offset, count);

    /// <summary>
    /// Finds the index of the first occurrence of an item using a generic equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IndexOf<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>
    {
        if (_Inner is LargeObservableCollection<T> observable)
        {
            return observable.IndexOf(item, ref comparer, offset, count);
        }
        throw new NotSupportedException($"Generic IndexOf is not supported for inner type {_Inner.GetType().Name}. Use the delegate-based overload instead.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LastIndexOf(T item, long? offset = null, long? count = null)
        => _Inner.LastIndexOf(item, offset, count);

    /// <summary>
    /// Finds the index of the last occurrence of an item using a generic equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LastIndexOf<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>
    {
        if (_Inner is LargeObservableCollection<T> observable)
        {
            return observable.LastIndexOf(item, ref comparer, offset, count);
        }
        throw new NotSupportedException($"Generic LastIndexOf is not supported for inner type {_Inner.GetType().Name}. Use the delegate-based overload instead.");
    }

    internal class NotificationSuspender : IDisposable
    {
        private readonly ReadOnlyLargeObservableCollection<T> _Collection;
        private bool _Disposed = false;

        public NotificationSuspender(ReadOnlyLargeObservableCollection<T> collection)
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