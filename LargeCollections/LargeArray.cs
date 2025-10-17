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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LargeCollections
{
    /// <summary>
    /// A mutable array of <typeparamref name="T"/> that can store up to <see cref="Constants.MaxLargeCollectionCount"/> elements.
    /// Arrays allow index based access to the elements.
    /// </summary>
    [DebuggerDisplay("LargeArray: Count = {Count}")]
    public class LargeArray<T>(long capacity = 0L) : IRefAccessLargeArray<T>
    {
        private static readonly Comparer<T> _DefaultComparer = Comparer<T>.Default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DefaultComparer(T left, T right)
        {
            int result = _DefaultComparer.Compare(left, right);
            return result;
        }

        private T[][] _Storage = StorageExtensions.StorageCreate<T>(capacity);

        public long Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set;
        } = capacity;

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
        public bool Contains(T item)
        {
            bool result = _Storage.Contains(item, 0L, Count, LargeSet<T>.DefaultEqualsFunction);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item, long offset, long count)
        {
            StorageExtensions.CheckRange(offset, count, Count);

            if (count == 0L)
            {
                return false;
            }

            bool result = _Storage.Contains(item, offset, count, LargeSet<T>.DefaultEqualsFunction);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(IReadOnlyLargeArray<T> source, long sourceOffset, long targetOffset, long count)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            StorageExtensions.CheckRange(sourceOffset, count, source.Count);
            StorageExtensions.CheckRange(targetOffset, count, Count);

            if (count == 0L)
            {
                return;
            }

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
        public void CopyFromArray(T[] source, int sourceOffset, long targetOffset, int count)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            StorageExtensions.CheckRange(sourceOffset, count, source.Length);
            StorageExtensions.CheckRange(targetOffset, count, Count);

            if (count == 0L)
            {
                return;
            }

            _Storage.StorageCopyFromArray(source, sourceOffset, targetOffset, count);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFromSpan(ReadOnlySpan<T> source, long targetOffset, int count)
        {
            StorageExtensions.CheckRange(0, count, source.Length);
            StorageExtensions.CheckRange(targetOffset, count, Count);

            if (count == 0L)
            {
                return;
            }

            _Storage.StorageCopyFromSpan(source, targetOffset, count);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(ILargeArray<T> target, long sourceOffset, long targetOffset, long count)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            StorageExtensions.CheckRange(targetOffset, count, target.Count);
            StorageExtensions.CheckRange(sourceOffset, count, Count);

            if (count == 0L)
            {
                return;
            }

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
        public void CopyToArray(T[] target, long sourceOffset, int targetOffset, int count)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            StorageExtensions.CheckRange(targetOffset, count, target.Length);
            StorageExtensions.CheckRange(sourceOffset, count, Count);

            if (count == 0L)
            {
                return;
            }

            _Storage.StorageCopyToArray(target, sourceOffset, targetOffset, count);
        }


#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToSpan(Span<T> target, long sourceOffset, int count)
        {
            StorageExtensions.CheckRange(0, count, target.Length);
            StorageExtensions.CheckRange(sourceOffset, count, Count);

            if (count == 0L)
            {
                return;
            }

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
            foreach (T item in _Storage.StorageGetAll(0L, Count))
            {
                yield return item;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> GetAll(long offset, long count)
        {
            StorageExtensions.CheckRange(offset, count, Count);

            foreach (T item in _Storage.StorageGetAll(offset, count))
            {
                yield return item;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<T> GetEnumerator()
        {
            return GetAll().GetEnumerator();
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
            return GetAll().GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEach(Action<T> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            bool dummy = false;
            _Storage.StorageDoForEach(static (ref T item, ref Action<T> action, ref bool dummy) => action.Invoke(item),
                0L, Count, ref action, ref dummy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEach(Action<T> action, long offset, long count)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            StorageExtensions.CheckRange(offset, count, Count);
            bool dummy = false;
            _Storage.StorageDoForEach(static (ref T item, ref Action<T> action, ref bool dummy) => action.Invoke(item),
                offset, count, ref action, ref dummy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEach<TUserData>(ActionWithUserData<T, TUserData> action, ref TUserData userData)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _Storage.StorageDoForEach(static (ref T item, ref ActionWithUserData<T, TUserData> action, ref TUserData userData) => action.Invoke(item, ref userData),
                0L, Count, ref action, ref userData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEach<TUserData>(ActionWithUserData<T, TUserData> action, long offset, long count, ref TUserData userData)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            StorageExtensions.CheckRange(offset, count, Count);
            _Storage.StorageDoForEach(static (ref T item, ref ActionWithUserData<T, TUserData> action, ref TUserData userData) => action.Invoke(item, ref userData),
                offset, count, ref action, ref userData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEach(RefAction<T> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            bool dummy = false;
            _Storage.StorageDoForEach(static (ref T item, ref RefAction<T> action, ref bool dummy) => action.Invoke(ref item),
                0L, Count, ref action, ref dummy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEach(RefAction<T> action, long offset, long count)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            StorageExtensions.CheckRange(offset, count, Count);
            bool dummy = false;
            _Storage.StorageDoForEach(static (ref T item, ref RefAction<T> action, ref bool dummy) => action.Invoke(ref item),
                offset, count, ref action, ref dummy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEach<TUserData>(RefActionWithUserData<T, TUserData> action, ref TUserData userData)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _Storage.StorageDoForEach(static (ref T item, ref RefActionWithUserData<T, TUserData> action, ref TUserData userData) => action.Invoke(ref item, ref userData),
                0L, Count, ref action, ref userData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoForEach<TUserData>(RefActionWithUserData<T, TUserData> action, long offset, long count, ref TUserData userData)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            StorageExtensions.CheckRange(offset, count, Count);
            _Storage.StorageDoForEach(static (ref T item, ref RefActionWithUserData<T, TUserData> action, ref TUserData userData) => action.Invoke(ref item, ref userData),
                offset, count, ref action, ref userData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sort(Func<T, T, int> comparer)
        {
            comparer ??= DefaultComparer;
            _Storage.StorageSort(comparer, 0L, Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sort(Func<T, T, int> comparer, long offset, long count)
        {
            StorageExtensions.CheckRange(offset, count, Count);
            comparer ??= DefaultComparer;
            _Storage.StorageSort(comparer, offset, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long BinarySearch(T item, Func<T, T, int> comparer)
        {
            comparer ??= DefaultComparer;
            long result = _Storage.StorageBinarySearch(item, comparer, 0L, Count);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long BinarySearch(T item, Func<T, T, int> comparer, long offset, long count)
        {
            StorageExtensions.CheckRange(offset, count, Count);
            comparer ??= DefaultComparer;
            long result = _Storage.StorageBinarySearch(item, comparer, offset, count);
            return result;
        }

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
    }
}
