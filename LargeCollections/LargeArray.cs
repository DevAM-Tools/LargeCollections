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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LargeCollections
{
    /// <summary>
    /// A mutable array of <typeparamref name="T"/> that can store up to <see cref="Constants.MaxLargeCollectionCount"/> elements.
    /// Arrays allow index based access to the elements.
    /// </summary>
    [DebuggerDisplay("LargeArray: Count = {Count}")]
    public class LargeArray<T> : IRefAccessLargeArray<T>
    {
        public LargeArray(long capacity)
        {
            _Storage = StorageExtensions.StorageCreate<T>(capacity);
            Count = capacity;
        }

        private static readonly Comparer<T> _DefaultComparer = Comparer<T>.Default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DefaultComparer(T left, T right)
        {
            int result = _DefaultComparer.Compare(left, right);
            return result;
        }

        private T[][] _Storage;

        public long Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(long capacity)
        {
            if (capacity < 0L || capacity > Constants.MaxLargeCollectionCount)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            if (capacity == Count)
            {
                return;
            }
            StorageExtensions.StorageResize(ref _Storage, capacity);
            Count = capacity;
        }

        public T this[long index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                StorageExtensions.CheckIndex(index, Count);
                T result = _Storage.StorageGet(index);
                return result;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                StorageExtensions.CheckIndex(index, Count);
                _Storage.StorageSet(index, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item) => Contains(item, 0L, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item, long offset, long count)
        {
            StorageExtensions.CheckRange(offset, count, Count);

            if (count == 0L)
            {
                return false;
            }

            bool result = _Storage.Contains(item, offset, count, DefaultFunctions<T>.DefaultEqualsFunction);
            return result;
        }

        /// <summary>
        /// Determines whether the collection contains a specific item using a generic equality comparer for optimal performance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? Count - actualOffset;
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);

            if (actualCount == 0L)
            {
                return false;
            }

            bool result = _Storage.Contains(item, actualOffset, actualCount, ref comparer);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(IReadOnlyLargeArray<T> source, long sourceOffset, long targetOffset, long count)
        {
            if (count == 0L)
            {
                return;
            }

            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            StorageExtensions.CheckRange(sourceOffset, count, source.Count);
            StorageExtensions.CheckRange(targetOffset, count, Count);

            if (source is LargeArray<T> largeArraySource)
            {
                T[][] sourceStorage = largeArraySource.GetStorage();
                _Storage.StorageCopyFrom(sourceStorage, sourceOffset, targetOffset, count);
            }
            else if (source is LargeList<T> largeListSource)
            {
                T[][] sourceStorage = largeListSource.GetStorage();
                _Storage.StorageCopyFrom(sourceStorage, sourceOffset, targetOffset, count);
            }
            else
            {
                for (long i = 0L; i < count; i++)
                {
                    T item = source[sourceOffset + i];
                    _Storage.StorageSet(targetOffset + i, item);
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

            StorageExtensions.CheckRange(0L, count, source.Count);
            StorageExtensions.CheckRange(targetOffset, count, Count);

            if (source.Inner is LargeArray<T> largeArraySource)
            {
                T[][] sourceStorage = largeArraySource.GetStorage();
                _Storage.StorageCopyFrom(sourceStorage, source.Start, targetOffset, count);
            }
            else if (source.Inner is LargeList<T> largeListSource)
            {
                T[][] sourceStorage = largeListSource.GetStorage();
                _Storage.StorageCopyFrom(sourceStorage, source.Start, targetOffset, count);
            }
            else
            {
                for (long i = 0L; i < count; i++)
                {
                    T item = source[i];
                    _Storage.StorageSet(targetOffset + i, item);
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

            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            StorageExtensions.CheckRange(sourceOffset, count, source.Length);
            StorageExtensions.CheckRange(targetOffset, count, Count);

            _Storage.StorageCopyFromArray(source, sourceOffset, targetOffset, count);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFromSpan(ReadOnlySpan<T> source, long targetOffset, int count)
        {
            if (count == 0L)
            {
                return;
            }

            StorageExtensions.CheckRange(0, count, source.Length);
            StorageExtensions.CheckRange(targetOffset, count, Count);

            _Storage.StorageCopyFromSpan(source, targetOffset, count);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(ILargeArray<T> target, long sourceOffset, long targetOffset, long count)
        {
            if (count == 0L)
            {
                return;
            }

            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            StorageExtensions.CheckRange(targetOffset, count, target.Count);
            StorageExtensions.CheckRange(sourceOffset, count, Count);

            if (target is LargeArray<T> largeArrayTarget)
            {
                T[][] targetStorage = largeArrayTarget.GetStorage();
                _Storage.StorageCopyTo(targetStorage, sourceOffset, targetOffset, count);
            }
            else if (target is LargeList<T> largeListTarget)
            {
                T[][] targetStorage = largeListTarget.GetStorage();
                _Storage.StorageCopyTo(targetStorage, sourceOffset, targetOffset, count);
            }
            else
            {
                for (long i = 0L; i < count; i++)
                {
                    T item = _Storage.StorageGet(sourceOffset + i);
                    target[targetOffset + i] = item;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(LargeSpan<T> target, long sourceOffset, long count)
        {
            if (count == 0L)
            {
                return;
            }

            StorageExtensions.CheckRange(0L, count, target.Count);
            StorageExtensions.CheckRange(sourceOffset, count, Count);

            if (target.Inner is LargeArray<T> largeArrayTarget)
            {
                T[][] targetStorage = largeArrayTarget.GetStorage();
                _Storage.StorageCopyTo(targetStorage, sourceOffset, target.Start, count);
            }
            else if (target.Inner is LargeList<T> largeListTarget)
            {
                T[][] targetStorage = largeListTarget.GetStorage();
                _Storage.StorageCopyTo(targetStorage, sourceOffset, target.Start, count);
            }
            else
            {
                for (long i = 0L; i < count; i++)
                {
                    T item = _Storage.StorageGet(sourceOffset + i);
                    target[i] = item;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToArray(T[] target, long sourceOffset, int targetOffset, int count)
        {
            if (count == 0L)
            {
                return;
            }

            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            StorageExtensions.CheckRange(targetOffset, count, target.Length);
            StorageExtensions.CheckRange(sourceOffset, count, Count);

            _Storage.StorageCopyToArray(target, sourceOffset, targetOffset, count);
        }


#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToSpan(Span<T> target, long sourceOffset, int count)
        {
            if (count == 0L)
            {
                return;
            }

            StorageExtensions.CheckRange(0, count, target.Length);
            StorageExtensions.CheckRange(sourceOffset, count, Count);

            _Storage.StorageCopyToSpan(target, sourceOffset, count);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(long index)
        {
            StorageExtensions.CheckIndex(index, Count);
            T result = _Storage.StorageGet(index);
            return result;
        }

        /// <inheritdoc/>
        ref T IRefAccessLargeArray<T>.this[long index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref GetRef(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetRef(long index)
        {
            StorageExtensions.CheckIndex(index, Count);
            ref T result = ref _Storage.StorageGetRef(index);
            return ref result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> GetAll()
        {
            return _Storage.StorageGetAll(0L, Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> GetAll(long offset, long count)
        {
            StorageExtensions.CheckRange(offset, count, Count);

            return _Storage.StorageGetAll(offset, count);
        }

        /// <summary>
        /// Returns a high-performance struct enumerator for efficient iteration.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LargeStorageEnumerator<T> GetEnumerator()
        {
            return _Storage.GetStructEnumerator(0L, Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _Storage.GetStructEnumerator(0L, Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(long index, T item)
        {
            StorageExtensions.CheckIndex(index, Count);
            _Storage.StorageSet(index, item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Storage.GetStructEnumerator(0L, Count);
        }

        #region DoForEach Methods

        /// <summary>
        /// Performs the <paramref name="action"/> with items of the collection.
        /// </summary>
        /// <param name="action">The function that will be called for each item of the collection.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEach(Action<T> action) => DoForEach(action, 0L, Count);

        /// <summary>
        /// Performs the <paramref name="action"/> with items of the collection within the specified range.
        /// </summary>
        /// <param name="action">The function that will be called for each item of the collection.</param>
        /// <param name="offset">Starting offset.</param>
        /// <param name="count">Number of elements to process.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEach(Action<T> action, long offset, long count)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (count == 0L)
            {
                return;
            }

            StorageExtensions.CheckRange(offset, count, Count);

            DelegateLargeAction<T> wrapper = new (action);
            _Storage.StorageDoForEach(ref wrapper, offset, count);
        }

        /// <summary>
        /// Performs the action on items using a struct action for optimal performance through JIT devirtualization.
        /// This method can be significantly faster than the delegate-based version.
        /// Store any user data directly as fields in your struct - the action is passed by ref so state changes are preserved.
        /// </summary>
        /// <typeparam name="TAction">A struct type implementing <see cref="ILargeAction{T}"/>.</typeparam>
        /// <param name="action">The struct action instance passed by reference.</param>
        /// <example>
        /// <code>
        /// struct SumAction : ILargeAction&lt;long&gt;
        /// {
        ///     public long Sum;
        ///     public void Invoke(long item) =&gt; Sum += item;
        /// }
        /// var action = new SumAction();
        /// array.DoForEach(ref action);
        /// Console.WriteLine(action.Sum);
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<T> => DoForEach(ref action, 0L, Count);

        /// <summary>
        /// Performs the action on items using a struct action for optimal performance through JIT devirtualization.
        /// This method can be significantly faster than the delegate-based version.
        /// Store any user data directly as fields in your struct - the action is passed by ref so state changes are preserved.
        /// </summary>
        /// <typeparam name="TAction">A struct type implementing <see cref="ILargeAction{T}"/>.</typeparam>
        /// <param name="action">The struct action instance passed by reference.</param>
        /// <param name="offset">Starting offset.</param>
        /// <param name="count">Number of elements to process.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEach<TAction>(ref TAction action, long offset, long count) where TAction : ILargeAction<T>
        {
            if (count == 0L)
            {
                return;
            }

            StorageExtensions.CheckRange(offset, count, Count);
            _Storage.StorageDoForEach(ref action, offset, count);
        }

        /// <summary>
        /// Performs the action on each item by reference using an action for optimal performance.
        /// Store any user data directly as fields in your action - the action is passed by ref so state changes are preserved.
        /// </summary>
        /// <typeparam name="TAction">A type implementing <see cref="ILargeRefAction{T}"/>.</typeparam>
        /// <param name="action">The action instance passed by reference.</param>
        /// <param name="offset">Optional starting offset. If null, starts from 0.</param>
        /// <param name="count">Optional number of elements to process. If null, processes all remaining elements.</param>
        /// <example>
        /// <code>
        /// struct IncrementAction : ILargeRefAction&lt;int&gt;
        /// {
        ///     public int IncrementBy;
        ///     public int ModifiedCount;
        ///     public void Invoke(ref int item) { item += IncrementBy; ModifiedCount++; }
        /// }
        /// var action = new IncrementAction { IncrementBy = 10 };
        /// array.DoForEachRef(ref action);
        /// Console.WriteLine($"Modified {action.ModifiedCount} items");
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEachRef<TAction>(ref TAction action, long? offset = null, long? count = null) where TAction : ILargeRefAction<T>
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? (Count - actualOffset);
            
            if (actualCount == 0L)
            {
                return;
            }

            StorageExtensions.CheckRange(actualOffset, actualCount, Count);
            _Storage.StorageDoForEachRef(ref action, actualOffset, actualCount);
        }

        #endregion

        #region High-Performance Struct Comparer Sort

        /// <summary>
        /// Sorts the array using a struct comparer for optimal performance through JIT devirtualization.
        /// This method can be 20-40% faster than the delegate-based version for <see cref="IComparable{T}"/> types.
        /// </summary>
        /// <typeparam name="TComparer">A struct type implementing <see cref="ILargeComparer{T}"/>.</typeparam>
        /// <param name="comparer">The struct comparer instance.</param>
        /// <example>
        /// <code>
        /// // Using the default comparer for IComparable types:
        /// array.Sort(new DefaultLargeComparer&lt;int&gt;());
        /// 
        /// // Using a descending comparer:
        /// array.Sort(new DescendingLargeComparer&lt;int&gt;());
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sort<TComparer>(TComparer comparer, long? offset = null, long? count = null) where TComparer : IComparer<T>
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? Count - actualOffset;
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);
            _Storage.StorageSort(comparer, actualOffset, actualCount);
        }

        /// <summary>
        /// Sorts the array in parallel using a struct comparer for optimal performance.
        /// Recommended for large arrays (>100,000 elements).
        /// </summary>
        /// <typeparam name="TComparer">A struct type implementing <see cref="ILargeComparer{T}"/>.</typeparam>
        /// <param name="comparer">The struct comparer instance.</param>
        /// <param name="maxDegreeOfParallelism">Maximum number of threads to use. -1 or 0 uses all available processors.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ParallelSort<TComparer>(TComparer comparer, int maxDegreeOfParallelism = -1) where TComparer : IComparer<T>
        {
            _Storage.StorageParallelSort(comparer, 0L, Count, maxDegreeOfParallelism);
        }

        /// <summary>
        /// Sorts a range of the array in parallel using a struct comparer for optimal performance.
        /// </summary>
        /// <typeparam name="TComparer">A struct type implementing <see cref="ILargeComparer{T}"/>.</typeparam>
        /// <param name="comparer">The struct comparer instance.</param>
        /// <param name="offset">The starting index of the range to sort.</param>
        /// <param name="count">The number of elements to sort.</param>
        /// <param name="maxDegreeOfParallelism">Maximum number of threads to use. -1 or 0 uses all available processors.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ParallelSort<TComparer>(TComparer comparer, long offset, long count, int maxDegreeOfParallelism = -1) where TComparer : IComparer<T>
        {
            StorageExtensions.CheckRange(offset, count, Count);
            _Storage.StorageParallelSort(comparer, offset, count, maxDegreeOfParallelism);
        }

        /// <summary>
        /// Performs a binary search using a struct comparer for optimal performance.
        /// The array must be sorted in ascending order according to the comparer.
        /// </summary>
        /// <typeparam name="TComparer">A struct type implementing <see cref="ILargeComparer{T}"/>.</typeparam>
        /// <param name="item">The item to search for.</param>
        /// <param name="comparer">The struct comparer instance.</param>
        /// <param name="offset">Optional starting offset. If null, starts from 0.</param>
        /// <param name="count">Optional number of elements to search. If null, searches all remaining elements.</param>
        /// <returns>The index of the item if found; otherwise, -1.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long BinarySearch<TComparer>(T item, TComparer comparer, long? offset = null, long? count = null) where TComparer : IComparer<T>
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? Count - actualOffset;
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);
            return _Storage.StorageBinarySearch(item, comparer, actualOffset, actualCount);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long BinarySearch(T item, long? offset = null, long? count = null)
        {
            return BinarySearch(item, Comparer<T>.Default, offset, count);
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Swap(long leftIndex, long rightIndex)
        {
            StorageExtensions.CheckIndex(leftIndex, Count);
            StorageExtensions.CheckIndex(rightIndex, Count);
            _Storage.StorageSwap(leftIndex, rightIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T[][] GetStorage()
        {
            T[][] result = _Storage;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long IndexOf(T item, long? offset = null, long? count = null)
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? Count - actualOffset;
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);

            long result = _Storage.StorageIndexOf(item, actualOffset, actualCount, DefaultFunctions<T>.DefaultEqualsFunction);
            return result;
        }

        /// <summary>
        /// Finds the index of the first occurrence of an item using a generic equality comparer for optimal performance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long IndexOf<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? Count - actualOffset;
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);
            long result = _Storage.StorageIndexOf(item, actualOffset, actualCount, ref comparer);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long LastIndexOf(T item, long? offset = null, long? count = null)
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? Count - actualOffset;
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);

            long result = _Storage.StorageLastIndexOf(item, actualOffset, actualCount, DefaultFunctions<T>.DefaultEqualsFunction);
            return result;
        }

        /// <summary>
        /// Finds the index of the last occurrence of an item using a generic equality comparer for optimal performance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long LastIndexOf<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>
        {
            long actualOffset = offset ?? 0L;
            long actualCount = count ?? Count - actualOffset;
            StorageExtensions.CheckRange(actualOffset, actualCount, Count);
            long result = _Storage.StorageLastIndexOf(item, actualOffset, actualCount, ref comparer);
            return result;
        }
    }
}
