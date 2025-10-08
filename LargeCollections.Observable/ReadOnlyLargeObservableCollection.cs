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

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch(T item, Func<T, T, int> comparer)
        => _Inner.BinarySearch(item, comparer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long BinarySearch(T item, Func<T, T, int> comparer, long offset, long count)
            => _Inner.BinarySearch(item, comparer, offset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item, long offset, long count)
        => _Inner.Contains(item, offset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
            => _Inner.Contains(item);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(ILargeArray<T> target, long sourceOffset, long targetOffset, long count)
        => _Inner.CopyTo(target, sourceOffset, targetOffset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyToArray(T[] target, long sourceOffset, int targetOffset, int count)
        => _Inner.CopyToArray(target, sourceOffset, targetOffset, count);

#if NETSTANDARD2_1_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyToSpan(Span<T> target, long sourceOffset, int count)
        => _Inner.CopyToSpan(target, sourceOffset, count);
#endif

    public void DoForEach(Action<T> action, long offset, long count)
        => _Inner.DoForEach(action, offset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(ActionWithUserData<T, TUserData> action, long offset, long count, ref TUserData userData)
        => _Inner.DoForEach(action, offset, count, ref userData);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action)
        => _Inner.DoForEach(action);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(ActionWithUserData<T, TUserData> action, ref TUserData userData)
        => _Inner.DoForEach(action, ref userData);

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
        }
    }

    internal class NotificationSuspender(ReadOnlyLargeObservableCollection<T> collection) : IDisposable
    {
        private readonly ReadOnlyLargeObservableCollection<T> _Collection = collection ?? throw new ArgumentNullException(nameof(collection));
        private bool _Disposed = false;

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