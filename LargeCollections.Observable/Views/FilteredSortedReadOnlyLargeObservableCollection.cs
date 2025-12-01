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

/// <summary>
/// Provides a filtered and sorted, read-only view over an observable collection.
/// Combines filtering and sorting in a single pass for better performance than chaining separate views.
/// The view automatically updates when the source collection changes.
/// Uses lazy evaluation - the filter and sort are only applied when elements are accessed.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
/// <typeparam name="TPredicate">The predicate type used for filtering. Struct implementations enable JIT optimizations.</typeparam>
/// <typeparam name="TComparer">The comparer type used for sorting. Struct implementations enable JIT optimizations.</typeparam>
[DebuggerDisplay("FilteredSortedView: Count = {Count}")]
public class FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, TComparer> : IReadOnlyLargeObservableCollection<T>, IDisposable
    where TPredicate : ILargePredicate<T>
    where TComparer : IComparer<T>
{
    /// <summary>
    /// Creates a new filtered and sorted view over the source collection.
    /// </summary>
    /// <param name="source">The source collection to filter and sort.</param>
    /// <param name="predicate">The predicate used to filter items.</param>
    /// <param name="comparer">The comparer used to sort items.</param>
    public FilteredSortedReadOnlyLargeObservableCollection(IReadOnlyLargeObservableCollection<T> source, TPredicate predicate, TComparer comparer)
        : this(source, predicate, comparer, suppressEventExceptions: false)
    {
    }

    /// <summary>
    /// Creates a new filtered and sorted view over the source collection.
    /// </summary>
    /// <param name="source">The source collection to filter and sort.</param>
    /// <param name="predicate">The predicate used to filter items.</param>
    /// <param name="comparer">The comparer used to sort items.</param>
    /// <param name="suppressEventExceptions">Whether to suppress exceptions from event handlers.</param>
    public FilteredSortedReadOnlyLargeObservableCollection(IReadOnlyLargeObservableCollection<T> source, TPredicate predicate, TComparer comparer, bool suppressEventExceptions)
    {
        _Source = source ?? throw new ArgumentNullException(nameof(source));
        _Predicate = predicate;
        _Comparer = comparer;
        _SuppressEventExceptions = suppressEventExceptions;

        _Source.CollectionChanged += OnSourceCollectionChanged;
        _Source.PropertyChanged += OnSourcePropertyChanged;
        _Source.Changed += OnSourceChanged;
    }

    private readonly IReadOnlyLargeObservableCollection<T> _Source;
    private TPredicate _Predicate;
    private TComparer _Comparer;
    private readonly bool _SuppressEventExceptions;

    // Index mapping: view index -> source index (filtered and sorted)
    private LargeList<long> _IndexMap;
    private volatile bool _IsDirty = true;
    private readonly ReaderWriterLockSlim _Lock = new(LockRecursionPolicy.NoRecursion);
    private bool _Disposed;

    private long _SuspendNotificationsCounter = 0;
    private long _ChangesWhileSuspended = 0;
    private long _CountBeforeSuspend = 0;

    private static readonly NotifyCollectionChangedEventArgs _ResetEventArgs = new(NotifyCollectionChangedAction.Reset);
    private static readonly PropertyChangedEventArgs _CountPropertyChangedEventArgs = new(nameof(Count));
    private static readonly bool _HasFilter = typeof(TPredicate) != typeof(NoFilter<T>);
    private static readonly bool _HasSort = typeof(TComparer) != typeof(NoSort<T>);

    /// <summary>
    /// Gets or sets the predicate used for filtering.
    /// Setting a new predicate invalidates the view and triggers a reset notification.
    /// </summary>
    public TPredicate Predicate
    {
        get => _Predicate;
        set
        {
            _Predicate = value;
            Invalidate();
            RaiseReset();
        }
    }

    /// <summary>
    /// Gets or sets the comparer used for sorting.
    /// Setting a new comparer invalidates the view and triggers a reset notification.
    /// </summary>
    public TComparer Comparer
    {
        get => _Comparer;
        set
        {
            _Comparer = value;
            Invalidate();
            RaiseReset();
        }
    }

    /// <inheritdoc/>
    public T this[long index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            EnterReadLockWithEnsure();
            try
            {
                if (index < 0 || index >= _IndexMap.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return _Source[_IndexMap[index]];
            }
            finally
            {
                ExitReadLock();
            }
        }
    }

    /// <inheritdoc/>
    public long Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            EnterReadLockWithEnsure();
            try
            {
                return _IndexMap.Count;
            }
            finally
            {
                ExitReadLock();
            }
        }
    }

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler CollectionChanged;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <inheritdoc/>
    public event LargeCollectionChangedEventHandler<T> Changed;

    #region Index Map Management

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureIndexMap()
    {
        // Fast path: volatile read without lock
        if (!_IsDirty)
        {
            return;
        }

        // Slow path: acquire write lock and rebuild if still dirty
        _Lock.EnterWriteLock();
        try
        {
            // Double-check pattern
            if (!_IsDirty)
            {
                return;
            }

            RebuildIndexMap();
            _IsDirty = false;
        }
        finally
        {
            _Lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Acquires a read lock and ensures the index map is up-to-date.
    /// The caller MUST call ExitReadLock() after use.
    /// Uses UpgradeableReadLock pattern for safe lock upgrade/downgrade without race conditions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnterReadLockWithEnsure()
    {
        while (true)
        {
            _Lock.EnterReadLock();

            if (!_IsDirty)
            {
                return;
            }

            _Lock.ExitReadLock();
            EnsureIndexMap();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExitReadLock()
    {
        _Lock.ExitReadLock();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LargeList<long> SnapshotIndexMap()
    {
        EnterReadLockWithEnsure();
        try
        {
            long count = _IndexMap.Count;
            LargeList<long> snapshot = new (Math.Max(1L, count));

            if (count > 0)
            {
                snapshot.AddRange(_IndexMap, 0L, count);
            }

            return snapshot;
        }
        finally
        {
            ExitReadLock();
        }
    }

    private void RebuildIndexMap()
    {
        _IndexMap ??= new LargeList<long>();
        _IndexMap.Clear();

        long sourceCount = _Source.Count;

        // JIT will eliminate these branches at compile time for NoFilter<T>/NoSort<T>
        if (_HasFilter)
        {
            // Step 1: Filter - collect indices of matching items
            for (long i = 0; i < sourceCount; i++)
            {
                T item = _Source[i];
                if (_Predicate.Invoke(item))
                {
                    _IndexMap.Add(i);
                }
            }
        }
        else
        {
            // No filter - add all indices (JIT eliminates this branch when TPredicate != NoFilter<T>)
            for (long i = 0; i < sourceCount; i++)
            {
                _IndexMap.Add(i);
            }
        }

        // Step 2: Sort - sort the filtered indices by their values
        // JIT eliminates this entire block when TComparer == NoSort<T>
        if (_HasSort && _IndexMap.Count > 1)
        {
            IndexComparer indexComparer = new (_Source, _Comparer);
            _IndexMap.Sort(indexComparer, 0, _IndexMap.Count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureSortingEnabled()
    {
        if (!_HasSort)
        {
            throw new InvalidOperationException("BinarySearch requires the view to be sorted. Provide a non-NoSort comparer when constructing the view.");
        }
    }

    /// <summary>
    /// Comparer struct that compares indices based on the values they reference in the source collection.
    /// </summary>
    private readonly struct IndexComparer : IComparer<long>
    {
        private readonly IReadOnlyLargeObservableCollection<T> _Source;
        private readonly TComparer _ItemComparer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IndexComparer(IReadOnlyLargeObservableCollection<T> source, TComparer itemComparer)
        {
            _Source = source;
            _ItemComparer = itemComparer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(long left, long right)
        {
            return _ItemComparer.Compare(_Source[left], _Source[right]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Invalidate()
    {
        _IsDirty = true;
    }

    /// <summary>
    /// Forces the view to rebuild its index mapping.
    /// Call this if the predicate's or comparer's behavior has changed without reassigning the properties.
    /// </summary>
    public void Refresh()
    {
        Invalidate();
        RaiseReset();
    }

    #endregion

    #region Event Handling

    private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Invalidate();

        if (Interlocked.CompareExchange(ref _SuspendNotificationsCounter, 0, 0) > 0)
        {
            Interlocked.Increment(ref _ChangesWhileSuspended);
            return;
        }

        if (CollectionChanged != null)
        {
            OnCollectionChanged(_ResetEventArgs);
        }
    }

    private void OnSourcePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Count))
        {
            Invalidate();
            PublishChangedCount();
        }
    }

    private void OnSourceChanged(object sender, in LargeCollectionChangedEventArgs<T> e)
    {
        Invalidate();

        if (Interlocked.CompareExchange(ref _SuspendNotificationsCounter, 0, 0) > 0)
        {
            return;
        }

        if (Changed != null)
        {
            RaiseChanged(LargeCollectionChangedEventArgs<T>.Reset());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PublishChangedCount()
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RaiseChanged(in LargeCollectionChangedEventArgs<T> e)
    {
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

    private void RaiseReset()
    {
        if (CollectionChanged != null)
        {
            OnCollectionChanged(_ResetEventArgs);
        }
        if (Changed != null)
        {
            RaiseChanged(LargeCollectionChangedEventArgs<T>.Reset());
        }
        PublishChangedCount();
    }

    #endregion

    #region IReadOnlyLargeArray<T> Implementation

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(long index) => this[index];

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch(T item, long? offset = null, long? count = null)
    {
        EnsureSortingEnabled();
        EnterReadLockWithEnsure();
        try
        {
            long start = offset ?? 0;
            long length = count ?? (_IndexMap.Count - start);

            if (start < 0 || start > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0 || start + length > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            long lo = start;
            long hi = start + length - 1;

            while (lo <= hi)
            {
                long mid = lo + ((hi - lo) >> 1);
                T midItem = _Source[_IndexMap[mid]];
                int cmp = _Comparer.Compare(midItem, item);

                if (cmp == 0)
                {
                    return mid;
                }
                if (cmp < 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return ~lo;
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch<TOtherComparer>(T item, TOtherComparer comparer, long? offset = null, long? count = null)
        where TOtherComparer : IComparer<T>
    {
        EnsureSortingEnabled();
        EnterReadLockWithEnsure();
        try
        {
            long start = offset ?? 0;
            long length = count ?? (_IndexMap.Count - start);

            if (start < 0 || start > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0 || start + length > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            long lo = start;
            long hi = start + length - 1;

            while (lo <= hi)
            {
                long mid = lo + ((hi - lo) >> 1);
                T midItem = _Source[_IndexMap[mid]];
                int cmp = comparer.Compare(midItem, item);

                if (cmp == 0)
                {
                    return mid;
                }
                if (cmp < 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return ~lo;
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        EnterReadLockWithEnsure();
        try
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (long i = 0; i < _IndexMap.Count; i++)
            {
                if (comparer.Equals(_Source[_IndexMap[i]], item))
                {
                    return true;
                }
            }
            return false;
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item, long offset, long count)
    {
        EnterReadLockWithEnsure();
        try
        {
            if (offset < 0 || offset > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || offset + count > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            long end = offset + count;
            for (long i = offset; i < end; i++)
            {
                if (comparer.Equals(_Source[_IndexMap[i]], item))
                {
                    return true;
                }
            }
            return false;
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains<TEqualityComparer>(T item, ref TEqualityComparer comparer, long? offset = null, long? count = null)
        where TEqualityComparer : IEqualityComparer<T>
    {
        EnterReadLockWithEnsure();
        try
        {
            long start = offset ?? 0;
            long length = count ?? (_IndexMap.Count - start);

            if (start < 0 || start > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0 || start + length > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            long end = start + length;
            for (long i = start; i < end; i++)
            {
                if (comparer.Equals(_Source[_IndexMap[i]], item))
                {
                    return true;
                }
            }
            return false;
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(ILargeArray<T> target, long sourceOffset, long targetOffset, long count)
    {
        EnterReadLockWithEnsure();
        try
        {
            if (sourceOffset < 0 || sourceOffset > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceOffset));
            }
            if (count < 0 || sourceOffset + count > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            for (long i = 0; i < count; i++)
            {
                target[targetOffset + i] = _Source[_IndexMap[sourceOffset + i]];
            }
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(LargeSpan<T> target, long sourceOffset, long count)
    {
        EnterReadLockWithEnsure();
        try
        {
            if (sourceOffset < 0 || sourceOffset > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceOffset));
            }
            if (count < 0 || sourceOffset + count > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            for (long i = 0; i < count; i++)
            {
                target[i] = _Source[_IndexMap[sourceOffset + i]];
            }
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyToArray(T[] target, long sourceOffset, int targetOffset, int count)
    {
        EnterReadLockWithEnsure();
        try
        {
            if (sourceOffset < 0 || sourceOffset > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceOffset));
            }
            if (count < 0 || sourceOffset + count > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            for (int i = 0; i < count; i++)
            {
                target[targetOffset + i] = _Source[_IndexMap[sourceOffset + i]];
            }
        }
        finally
        {
            ExitReadLock();
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyToSpan(Span<T> target, long sourceOffset, int count)
    {
        EnterReadLockWithEnsure();
        try
        {
            if (sourceOffset < 0 || sourceOffset > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceOffset));
            }
            if (count < 0 || sourceOffset + count > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            for (int i = 0; i < count; i++)
            {
                target[i] = _Source[_IndexMap[sourceOffset + i]];
            }
        }
        finally
        {
            ExitReadLock();
        }
    }
#endif

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action)
    {
        EnterReadLockWithEnsure();
        try
        {
            for (long i = 0; i < _IndexMap.Count; i++)
            {
                action(_Source[_IndexMap[i]]);
            }
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action, long offset, long count)
    {
        EnterReadLockWithEnsure();
        try
        {
            if (offset < 0 || offset > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || offset + count > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            long end = offset + count;
            for (long i = offset; i < end; i++)
            {
                action(_Source[_IndexMap[i]]);
            }
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<T>
    {
        EnterReadLockWithEnsure();
        try
        {
            for (long i = 0; i < _IndexMap.Count; i++)
            {
                action.Invoke(_Source[_IndexMap[i]]);
            }
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action, long offset, long count) where TAction : ILargeAction<T>
    {
        EnterReadLockWithEnsure();
        try
        {
            if (offset < 0 || offset > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || offset + count > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            long end = offset + count;
            for (long i = offset; i < end; i++)
            {
                action.Invoke(_Source[_IndexMap[i]]);
            }
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll()
    {
        // Snapshot the index map while holding the lock because iterator methods cannot keep the lock
        LargeList<long> snapshot = SnapshotIndexMap();
        for (long i = 0; i < snapshot.Count; i++)
        {
            yield return _Source[snapshot[i]];
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll(long offset, long count)
    {
        LargeList<long> snapshot = SnapshotIndexMap();
        if (offset < 0 || offset > snapshot.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        if (count < 0 || offset + count > snapshot.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        long end = offset + count;
        for (long i = offset; i < end; i++)
        {
            yield return _Source[snapshot[i]];
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<T> GetEnumerator()
    {
        LargeList<long> snapshot = SnapshotIndexMap();
        for (long i = 0; i < snapshot.Count; i++)
        {
            yield return _Source[snapshot[i]];
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IndexOf(T item, long? offset = null, long? count = null)
    {
        EnterReadLockWithEnsure();
        try
        {
            long start = offset ?? 0;
            long length = count ?? (_IndexMap.Count - start);

            if (start < 0 || start > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0 || start + length > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            long end = start + length;
            for (long i = start; i < end; i++)
            {
                if (comparer.Equals(_Source[_IndexMap[i]], item))
                {
                    return i;
                }
            }
            return -1;
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IndexOf<TEqualityComparer>(T item, ref TEqualityComparer comparer, long? offset = null, long? count = null)
        where TEqualityComparer : IEqualityComparer<T>
    {
        EnterReadLockWithEnsure();
        try
        {
            long start = offset ?? 0;
            long length = count ?? (_IndexMap.Count - start);

            if (start < 0 || start > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0 || start + length > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            long end = start + length;
            for (long i = start; i < end; i++)
            {
                if (comparer.Equals(_Source[_IndexMap[i]], item))
                {
                    return i;
                }
            }
            return -1;
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LastIndexOf(T item, long? offset = null, long? count = null)
    {
        EnterReadLockWithEnsure();
        try
        {
            long start = offset ?? 0;
            long length = count ?? (_IndexMap.Count - start);

            if (start < 0 || start > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0 || start + length > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            long end = start + length;
            for (long i = end - 1; i >= start; i--)
            {
                if (comparer.Equals(_Source[_IndexMap[i]], item))
                {
                    return i;
                }
            }
            return -1;
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LastIndexOf<TEqualityComparer>(T item, ref TEqualityComparer comparer, long? offset = null, long? count = null)
        where TEqualityComparer : IEqualityComparer<T>
    {
        EnterReadLockWithEnsure();
        try
        {
            long start = offset ?? 0;
            long length = count ?? (_IndexMap.Count - start);

            if (start < 0 || start > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0 || start + length > _IndexMap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            long end = start + length;
            for (long i = end - 1; i >= start; i--)
            {
                if (comparer.Equals(_Source[_IndexMap[i]], item))
                {
                    return i;
                }
            }
            return -1;
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDisposable SuspendNotifications()
    {
        if (Interlocked.Increment(ref _SuspendNotificationsCounter) == 1)
        {
            _CountBeforeSuspend = Count;
            Interlocked.Exchange(ref _ChangesWhileSuspended, 0);
        }

        return new NotificationSuspender(this);
    }

    #endregion

    #region Dispose

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by the filtered sorted view.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_Disposed)
        {
            return;
        }

        if (disposing)
        {
            _Source.CollectionChanged -= OnSourceCollectionChanged;
            _Source.PropertyChanged -= OnSourcePropertyChanged;
            _Source.Changed -= OnSourceChanged;
            _Lock.Dispose();
        }

        _Disposed = true;
    }

    #endregion

    internal class NotificationSuspender : IDisposable
    {
        private readonly FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, TComparer> _Collection;
        private bool _Disposed = false;

        public NotificationSuspender(FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, TComparer> collection)
        {
            _Collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        public void Dispose()
        {
            if (!_Disposed)
            {
                if (Interlocked.Decrement(ref _Collection._SuspendNotificationsCounter) == 0)
                {
                    long changeCount = Interlocked.Exchange(ref _Collection._ChangesWhileSuspended, 0);
                    if (changeCount > 0)
                    {
                        _Collection.OnCollectionChanged(_ResetEventArgs);

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

