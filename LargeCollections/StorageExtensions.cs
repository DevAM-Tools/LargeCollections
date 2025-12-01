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
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LargeCollections;

public static class StorageExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckRange(long offset, long count, long maxCount)
    {
        if (offset < 0L || count < 0L || offset + count > maxCount || maxCount > Constants.MaxLargeCollectionCount)
        {
            throw new ArgumentException("offset < 0L || count < 0L || offset + count > maxCount || maxCount > Constants.MaxLargeCollectionCount");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageCheckRange<T>(this T[][] array, long offset, long count)
    {
        long maxCount = array.StorageGetCount();
        CheckRange(offset, count, maxCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CheckIndex(long index, long count)
    {
        if (index < 0L || index >= count || index > Constants.MaxLargeCollectionCount)
        {
            throw new IndexOutOfRangeException(nameof(index));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageCheckIndex<T>(this T[][] array, long index)
    {
        long count = array.StorageGetCount();
        CheckIndex(index, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetGrownCapacity(long capacity,
        double capacityGrowFactor = Constants.DefaultCapacityGrowFactor,
        long fixedCapacityGrowAmount = Constants.DefaultFixedCapacityGrowAmount,
        long fixedCapacityGrowLimit = Constants.DefaultFixedCapacityGrowLimit)
    {
        long newCapacity;
        try
        {
            if (capacity >= fixedCapacityGrowLimit)
            {
                newCapacity = capacity + fixedCapacityGrowAmount;
                newCapacity = newCapacity <= Constants.MaxLargeCollectionCount ? newCapacity : Constants.MaxLargeCollectionCount;
            }
            else
            {
                newCapacity = (long)(capacity * capacityGrowFactor) + 1L;
                newCapacity = newCapacity <= Constants.MaxLargeCollectionCount ? newCapacity : Constants.MaxLargeCollectionCount;
            }
        }
        catch
        {
            newCapacity = Constants.MaxLargeCollectionCount;
        }

        return newCapacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (int StorageIndex, int ItemIndex) StorageGetIndex(long index)
    {
        int storageIndex = (int)(index >> Constants.StorageIndexShiftAmount);
        int itemIndex = (int)(index & (Constants.MaxStorageCapacity - 1L));

        return (storageIndex, itemIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long StorageGetCount<T>(this T[][] array, long offset = 0L)
    {
        if (array.Length == 0)
        {
            return 0L;
        }

        long count = (array.LongLength - 1L) * Constants.MaxStorageCapacity;
        count += array[array.LongLength - 1].LongLength;
        count -= offset;
        return count;
    }

    internal static T[][] StorageCreate<T>(long capacity = 0L)
    {
        if (capacity < 0L || capacity > Constants.MaxLargeCollectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (capacity == 0L)
        {
            return Array.Empty<T[]>();
        }

        long lastIndex = capacity - 1L;
        (int storageIndex, int itemIndex) = StorageGetIndex(lastIndex);
        int storageCount = storageIndex + 1;
        int lastSegmentLength = itemIndex + 1;

        T[][] result = new T[storageCount][];

        for (int i = 0; i < storageCount - 1; i++)
        {
            result[i] = new T[Constants.MaxStorageCapacity];
        }

        result[storageCount - 1] = new T[lastSegmentLength];

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageResize<T>(ref T[][] array, long capacity)
    {
        if (capacity < 0L || capacity > Constants.MaxLargeCollectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (capacity == 0L)
        {
            array = Array.Empty<T[]>();
            return;
        }

        long lastIndex = capacity - 1L;
        (int storageIndex, int itemIndex) = StorageGetIndex(lastIndex);
        int newStorageCount = storageIndex + 1;
        int lastSegmentLength = itemIndex + 1;

        Array.Resize(ref array, newStorageCount);

        for (int i = 0; i < newStorageCount - 1; i++)
        {
            if (array[i] == null || array[i].Length != Constants.MaxStorageCapacity)
            {
                Array.Resize(ref array[i], (int)Constants.MaxStorageCapacity);
            }
        }

        if (newStorageCount > 0)
        {
            if (array[newStorageCount - 1] == null)
            {
                array[newStorageCount - 1] = new T[lastSegmentLength];
            }
            else if (array[newStorageCount - 1].Length != lastSegmentLength)
            {
                Array.Resize(ref array[newStorageCount - 1], lastSegmentLength);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T StorageGetRef<T>(this T[][] array, long index)
    {
        (int storageIndex, int itemIndex) = StorageGetIndex(index);

        return ref array[storageIndex][itemIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T StorageGet<T>(this T[][] array, long index)
    {
        (int storageIndex, int itemIndex) = StorageGetIndex(index);

        T result = array[storageIndex][itemIndex];
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T StorageSet<T>(this T[][] array, long index, in T value)
    {
        (int storageIndex, int itemIndex) = StorageGetIndex(index);

        array[storageIndex][itemIndex] = value;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IEnumerable<T> StorageGetAll<T>(this T[][] array, long offset, long count)
    {
        if (count == 0L)
        {
            yield break;
        }

        (int storageIndex, int itemIndex) = StorageGetIndex(offset);

        long currentCount = 0L;

        T[] currentStorage = array[storageIndex];
        for (int j = itemIndex; j < currentStorage.Length; j++)
        {
            if (currentCount >= count)
            {
                yield break;
            }
            T item = currentStorage[j];
            yield return item;
            currentCount++;
        }

        for (int i = storageIndex + 1; i < array.Length; i++)
        {
            if (currentCount >= count)
            {
                yield break;
            }
            currentStorage = array[i];
            for (int j = 0; j < currentStorage.Length; j++)
            {
                if (currentCount >= count)
                {
                    yield break;
                }

                T item = currentStorage[j];
                yield return item;
                currentCount++;
            }
        }
    }

    internal delegate void DoForEachAction<T, TUserData, TInternal>(ref T item, ref TUserData userData, ref TInternal internalData);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageDoForEach<T, TUserData, TInternal>(this T[][] array, DoForEachAction<T, TUserData, TInternal> action, long offset, long count, ref TUserData userData, ref TInternal internalData)
    {
        if (action is null)
        {
            return;
        }
        if (count == 0L)
        {
            return;
        }

        (int storageIndex, int itemIndex) = StorageGetIndex(offset);

        long currentCount = 0L;

        T[] currentStorage = array[storageIndex];
        for (int j = itemIndex; j < currentStorage.Length; j++)
        {
            if (currentCount >= count)
            {
                return;
            }
            ref T item = ref currentStorage[j];
            action.Invoke(ref item, ref userData, ref internalData);
            currentCount++;
        }

        for (int i = storageIndex + 1; i < array.Length; i++)
        {
            if (currentCount >= count)
            {
                return;
            }
            currentStorage = array[i];
            for (int j = 0; j < currentStorage.Length; j++)
            {
                if (currentCount >= count)
                {
                    return;
                }

                ref T item = ref currentStorage[j];
                action.Invoke(ref item, ref userData, ref internalData);
                currentCount++;
            }
        }
    }

    #region High-Performance Struct Action DoForEach

    /// <summary>
    /// Iterates over elements using an action for optimal performance through JIT devirtualization.
    /// This method can be significantly faster than delegate-based iteration.
    /// The action is passed by ref so any state changes are preserved.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageDoForEach<T, TAction>(this T[][] array, ref TAction action, long offset, long count)
        where TAction : ILargeAction<T>
    {
        if (count == 0L)
        {
            return;
        }

        (int storageIndex, int itemIndex) = StorageGetIndex(offset);
        long remaining = count;

        // First (partial) segment - calculate exact end to avoid per-element bounds check
        T[] currentStorage = array[storageIndex];
        int segmentEnd = (int)Math.Min(currentStorage.Length - itemIndex, remaining) + itemIndex;
        
        for (int j = itemIndex; j < segmentEnd; j++)
        {
            action.Invoke(currentStorage[j]);
        }
        remaining -= (segmentEnd - itemIndex);

        // Full segments - no per-element bounds check needed
        for (int i = storageIndex + 1; remaining > 0L && i < array.Length; i++)
        {
            currentStorage = array[i];
            segmentEnd = (int)Math.Min(currentStorage.Length, remaining);
            
            for (int j = 0; j < segmentEnd; j++)
            {
                action.Invoke(currentStorage[j]);
            }
            remaining -= segmentEnd;
        }
    }

    /// <summary>
    /// Iterates over elements by reference using an action for optimal performance.
    /// The action is passed by ref so any state changes are preserved.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageDoForEachRef<T, TAction>(this T[][] array, ref TAction action, long offset, long count)
        where TAction : ILargeRefAction<T>
    {
        if (count == 0L)
        {
            return;
        }

        (int storageIndex, int itemIndex) = StorageGetIndex(offset);
        long remaining = count;

        // First (partial) segment - calculate exact end to avoid per-element bounds check
        T[] currentStorage = array[storageIndex];
        int segmentEnd = (int)Math.Min(currentStorage.Length - itemIndex, remaining) + itemIndex;
        
        for (int j = itemIndex; j < segmentEnd; j++)
        {
            action.Invoke(ref currentStorage[j]);
        }
        remaining -= (segmentEnd - itemIndex);

        // Full segments - no per-element bounds check needed
        for (int i = storageIndex + 1; remaining > 0L && i < array.Length; i++)
        {
            currentStorage = array[i];
            segmentEnd = (int)Math.Min(currentStorage.Length, remaining);
            
            for (int j = 0; j < segmentEnd; j++)
            {
                action.Invoke(ref currentStorage[j]);
            }
            remaining -= segmentEnd;
        }
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageSwap<T>(this T[][] array, long leftIndex, long rightIndex)
    {
        // Optimized: direct array access instead of 4 method calls
        (int leftStorageIndex, int leftItemIndex) = StorageGetIndex(leftIndex);
        (int rightStorageIndex, int rightItemIndex) = StorageGetIndex(rightIndex);

        T[] leftStorage = array[leftStorageIndex];
        T[] rightStorage = array[rightStorageIndex];

        T temp = leftStorage[leftItemIndex];
        leftStorage[leftItemIndex] = rightStorage[rightItemIndex];
        rightStorage[rightItemIndex] = temp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Contains<T>(this T[][] array, T item, long offset, long count, Func<T, T, bool> equalsFunction)
        => array.StorageIndexOf(item, offset, count, equalsFunction) >= 0L;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long StorageIndexOf<T>(this T[][] array, T item, long offset, long count, Func<T, T, bool> equalsFunction)
    {
        if (count == 0L)
        {
            return -1L;
        }

        (int storageIndex, int itemIndex) = StorageGetIndex(offset);
        long currentPosition = offset;
        long endPosition = offset + count;

        // First partial segment
        T[] currentStorage = array[storageIndex];
        int segmentEnd = (int)Math.Min(currentStorage.Length, itemIndex + (endPosition - currentPosition));

#if NET6_0_OR_GREATER
        // Use SIMD-accelerated Array.IndexOf for common primitive types with static/lambda equals
        if (equalsFunction.Target == null && item is IEquatable<T>)
        {
            // Optimize for common numeric types that support vectorized search
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte) ||
                typeof(T) == typeof(short) || typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(int) || typeof(T) == typeof(uint) ||
                typeof(T) == typeof(long) || typeof(T) == typeof(ulong) ||
                typeof(T) == typeof(float) || typeof(T) == typeof(double) ||
                typeof(T) == typeof(char))
            {
                // Use Array.IndexOf which is SIMD-optimized for primitive types
                int idx = Array.IndexOf(currentStorage, item, itemIndex, segmentEnd - itemIndex);
                if (idx >= 0)
                {
                    return currentPosition + (idx - itemIndex);
                }
                currentPosition += segmentEnd - itemIndex;

                // Remaining full segments
                for (int i = storageIndex + 1; currentPosition < endPosition && i < array.Length; i++)
                {
                    currentStorage = array[i];
                    int elementsToSearch = (int)Math.Min(currentStorage.Length, endPosition - currentPosition);

                    idx = Array.IndexOf(currentStorage, item, 0, elementsToSearch);
                    if (idx >= 0)
                    {
                        return currentPosition + idx;
                    }
                    currentPosition += elementsToSearch;
                }

                return -1L;
            }
        }
#endif

        // Fallback: delegate-based comparison
        for (int j = itemIndex; j < segmentEnd; j++)
        {
            if (equalsFunction.Invoke(currentStorage[j], item))
            {
                return currentPosition;
            }
            currentPosition++;
        }

        for (int i = storageIndex + 1; currentPosition < endPosition && i < array.Length; i++)
        {
            currentStorage = array[i];
            segmentEnd = (int)Math.Min(currentStorage.Length, endPosition - currentPosition);

            for (int j = 0; j < segmentEnd; j++)
            {
                if (equalsFunction.Invoke(currentStorage[j], item))
                {
                    return currentPosition;
                }
                currentPosition++;
            }
        }

        return -1L;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long StorageLastIndexOf<T>(this T[][] array, T item, long offset, long count, Func<T, T, bool> equalsFunction)
    {
        if (count == 0L)
        {
            return -1L;
        }

        long endIndex = offset + count - 1L;
        (int endStorageIndex, int endItemIndex) = StorageGetIndex(endIndex);
        (int startStorageIndex, int startItemIndex) = StorageGetIndex(offset);

#if NET6_0_OR_GREATER
        // Use optimized Array.LastIndexOf for common primitive types with static/lambda equals
        if (equalsFunction.Target == null && item is IEquatable<T>)
        {
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte) ||
                typeof(T) == typeof(short) || typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(int) || typeof(T) == typeof(uint) ||
                typeof(T) == typeof(long) || typeof(T) == typeof(ulong) ||
                typeof(T) == typeof(float) || typeof(T) == typeof(double) ||
                typeof(T) == typeof(char))
            {
                long remaining = count;

                // Search in last segment first
                T[] currentStorage = array[endStorageIndex];
                int searchStart = (endStorageIndex == startStorageIndex) ? startItemIndex : 0;
                int searchCount = endItemIndex - searchStart + 1;

                int idx = Array.LastIndexOf(currentStorage, item, endItemIndex, searchCount);
                if (idx >= 0)
                {
                    return offset + (endIndex - offset) - (endItemIndex - idx);
                }
                remaining -= searchCount;

                // Search remaining segments backwards
                for (int i = endStorageIndex - 1; remaining > 0 && i >= startStorageIndex; i--)
                {
                    currentStorage = array[i];
                    int segmentStart = (i == startStorageIndex) ? startItemIndex : 0;
                    searchCount = (int)Math.Min(currentStorage.Length - segmentStart, remaining);

                    idx = Array.LastIndexOf(currentStorage, item, segmentStart + searchCount - 1, searchCount);
                    if (idx >= 0)
                    {
                        // Calculate absolute position
                        long segmentBaseOffset = (long)i * Constants.MaxStorageCapacity;
                        return segmentBaseOffset + idx;
                    }
                    remaining -= searchCount;
                }

                return -1L;
            }
        }
#endif

        // Fallback: delegate-based comparison
        long currentCount = 0L;

        // Start from the end and work backwards
        T[] storage = array[endStorageIndex];
        for (int j = endItemIndex; j >= 0; j--)
        {
            if (currentCount >= count)
            {
                break;
            }
            T currentItem = storage[j];
            if (equalsFunction.Invoke(currentItem, item))
            {
                return endIndex - currentCount;
            }
            currentCount++;
        }

        for (int i = endStorageIndex - 1; i >= 0; i--)
        {
            if (currentCount >= count)
            {
                break;
            }
            storage = array[i];
            for (int j = storage.Length - 1; j >= 0; j--)
            {
                if (currentCount >= count)
                {
                    break;
                }

                T currentItem = storage[j];
                if (equalsFunction.Invoke(currentItem, item))
                {
                    return endIndex - currentCount;
                }
                currentCount++;
            }
        }

        return -1L;
    }

    #region High-Performance Interface-Based Search Methods

    /// <summary>
    /// Checks if the array contains the item using a generic equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Contains<T, TComparer>(this T[][] array, T item, long offset, long count, ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
        => array.StorageIndexOf(item, offset, count, ref comparer) >= 0L;

    /// <summary>
    /// Searches for the item using a generic equality comparer for optimal performance through JIT devirtualization.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long StorageIndexOf<T, TComparer>(this T[][] array, T item, long offset, long count, ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        if (count == 0L)
        {
            return -1L;
        }

        (int storageIndex, int itemIndex) = StorageGetIndex(offset);
        long currentPosition = offset;
        long endPosition = offset + count;

        // First partial segment
        T[] currentStorage = array[storageIndex];
        int segmentEnd = (int)Math.Min(currentStorage.Length, itemIndex + (endPosition - currentPosition));

        for (int j = itemIndex; j < segmentEnd; j++)
        {
            if (comparer.Equals(currentStorage[j], item))
            {
                return currentPosition;
            }
            currentPosition++;
        }

        for (int i = storageIndex + 1; currentPosition < endPosition && i < array.Length; i++)
        {
            currentStorage = array[i];
            segmentEnd = (int)Math.Min(currentStorage.Length, endPosition - currentPosition);

            for (int j = 0; j < segmentEnd; j++)
            {
                if (comparer.Equals(currentStorage[j], item))
                {
                    return currentPosition;
                }
                currentPosition++;
            }
        }

        return -1L;
    }

    /// <summary>
    /// Searches backwards for the item using a generic equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long StorageLastIndexOf<T, TComparer>(this T[][] array, T item, long offset, long count, ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        if (count == 0L)
        {
            return -1L;
        }

        long endIndex = offset + count - 1L;
        (int endStorageIndex, int endItemIndex) = StorageGetIndex(endIndex);

        long currentCount = 0L;

        // Start from the end and work backwards
        T[] storage = array[endStorageIndex];
        for (int j = endItemIndex; j >= 0; j--)
        {
            if (currentCount >= count)
            {
                break;
            }
            if (comparer.Equals(storage[j], item))
            {
                return endIndex - currentCount;
            }
            currentCount++;
        }

        for (int i = endStorageIndex - 1; i >= 0; i--)
        {
            if (currentCount >= count)
            {
                break;
            }
            storage = array[i];
            for (int j = storage.Length - 1; j >= 0; j--)
            {
                if (currentCount >= count)
                {
                    break;
                }

                if (comparer.Equals(storage[j], item))
                {
                    return endIndex - currentCount;
                }
                currentCount++;
            }
        }

        return -1L;
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageHeapify<T>(this T[][] array, long i, long left, long right, Func<T, T, int> comparer)
    {
        if (comparer is null)
        {
            throw new ArgumentNullException(nameof(comparer));
        }

        // Iterative implementation to avoid StackOverflowException with large arrays
        long current = i;
        while (true)
        {
            long maxIndex = current;
            long leftIndex = left + (2L * (current - left)) + 1L;
            long rightIndex = left + (2L * (current - left)) + 2L;

            if (leftIndex <= right && comparer.Invoke(array.StorageGet(maxIndex), array.StorageGet(leftIndex)) < 0)
            {
                maxIndex = leftIndex;
            }

            if (rightIndex <= right && comparer.Invoke(array.StorageGet(maxIndex), array.StorageGet(rightIndex)) < 0)
            {
                maxIndex = rightIndex;
            }

            if (maxIndex == current)
            {
                break;
            }

            array.StorageSwap(current, maxIndex);
            current = maxIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageSort<T>(this T[][] array, Func<T, T, int> comparer, long offset, long count)
    {
        if (count <= 1L)
        {
            return;
        }
        if (comparer is null)
        {
            throw new ArgumentNullException(nameof(comparer));
        }

        // Use IntroSort: QuickSort with HeapSort fallback for worst-case scenarios
        int maxDepth = 2 * (int)Math.Log(count, 2);
        StorageIntroSort(array, offset, offset + count - 1L, comparer, maxDepth);
    }

    private static void StorageIntroSort<T>(T[][] array, long left, long right, Func<T, T, int> comparer, int depthLimit)
    {
        while (right > left)
        {
            long size = right - left + 1L;

            // Use insertion sort for small partitions (better cache locality)
            // Threshold of 32 performs better on modern CPUs due to branch prediction and cache behavior
            if (size <= 32L)
            {
                StorageInsertionSort(array, left, right, comparer);
                return;
            }

            // Switch to HeapSort if recursion depth limit reached (prevents O(n²) worst case)
            if (depthLimit == 0)
            {
                StorageHeapSort(array, left, right, comparer);
                return;
            }

            depthLimit--;

            // QuickSort partition with median-of-three pivot selection
            long pivot = StoragePartition(array, left, right, comparer);

            // Recurse on smaller partition, iterate on larger (tail call optimization)
            if (pivot - left < right - pivot)
            {
                StorageIntroSort(array, left, pivot - 1L, comparer, depthLimit);
                left = pivot + 1L;
            }
            else
            {
                StorageIntroSort(array, pivot + 1L, right, comparer, depthLimit);
                right = pivot - 1L;
            }
        }
    }

    private static void StorageInsertionSort<T>(T[][] array, long left, long right, Func<T, T, int> comparer)
    {
        for (long i = left + 1L; i <= right; i++)
        {
            T key = array.StorageGet(i);
            long j = i - 1L;

            while (j >= left && comparer.Invoke(array.StorageGet(j), key) > 0)
            {
                array.StorageSet(j + 1L, array.StorageGet(j));
                j--;
            }
            array.StorageSet(j + 1L, key);
        }
    }

    private static long StoragePartition<T>(T[][] array, long left, long right, Func<T, T, int> comparer)
    {
        // Median-of-three pivot selection for better average case
        long mid = left + (right - left) / 2L;

        T leftVal = array.StorageGet(left);
        T midVal = array.StorageGet(mid);
        T rightVal = array.StorageGet(right);

        // Sort left, mid, right and use mid as pivot
        if (comparer.Invoke(leftVal, midVal) > 0)
        {
            array.StorageSwap(left, mid);
        }
        if (comparer.Invoke(array.StorageGet(left), rightVal) > 0)
        {
            array.StorageSwap(left, right);
        }
        if (comparer.Invoke(array.StorageGet(mid), array.StorageGet(right)) > 0)
        {
            array.StorageSwap(mid, right);
        }

        // Move pivot to right - 1
        array.StorageSwap(mid, right - 1L);
        T pivot = array.StorageGet(right - 1L);

        long i = left;
        long j = right - 1L;

        while (true)
        {
            while (comparer.Invoke(array.StorageGet(++i), pivot) < 0) { }
            while (j > left && comparer.Invoke(array.StorageGet(--j), pivot) > 0) { }

            if (i >= j)
            {
                break;
            }

            array.StorageSwap(i, j);
        }

        // Restore pivot
        array.StorageSwap(i, right - 1L);
        return i;
    }

    private static void StorageHeapSort<T>(T[][] array, long left, long right, Func<T, T, int> comparer)
    {
        // Build max heap
        for (long i = left + (right - left) / 2L; i >= left; i--)
        {
            array.StorageHeapify(i, left, right, comparer);
        }

        // Extract elements from heap
        for (long i = right; i > left; i--)
        {
            array.StorageSwap(left, i);
            array.StorageHeapify(left, left, i - 1L, comparer);
        }
    }

    #region High-Performance Generic Sort (Struct Comparers)

    /// <summary>
    /// Sorts the array using a struct comparer for optimal performance through JIT devirtualization.
    /// This method can be 20-40% faster than the delegate-based version for IComparable types.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <typeparam name="TComparer">The struct type implementing <see cref="ILargeComparer{T}"/>.</typeparam>
    /// <param name="array">The storage array to sort.</param>
    /// <param name="comparer">The struct comparer instance.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="count">The number of elements to sort.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageSort<T, TComparer>(this T[][] array, TComparer comparer, long offset, long count)
        where TComparer : IComparer<T>
    {
        if (count <= 1L)
        {
            return;
        }

        // Use IntroSort: QuickSort with HeapSort fallback for worst-case scenarios
        int maxDepth = 2 * (int)Math.Log(count, 2);
        StorageIntroSort(array, offset, offset + count - 1L, comparer, maxDepth);
    }

    private static void StorageIntroSort<T, TComparer>(T[][] array, long left, long right, TComparer comparer, int depthLimit)
        where TComparer : IComparer<T>
    {
        while (right > left)
        {
            long size = right - left + 1L;

            // Use insertion sort for small partitions (better cache locality)
            // Threshold of 32 performs better on modern CPUs due to branch prediction and cache behavior
            if (size <= 32L)
            {
                StorageInsertionSort(array, left, right, comparer);
                return;
            }

            // Switch to HeapSort if recursion depth limit reached (prevents O(n²) worst case)
            if (depthLimit == 0)
            {
                StorageHeapSort(array, left, right, comparer);
                return;
            }

            depthLimit--;

            // QuickSort partition with median-of-three pivot selection
            long pivot = StoragePartition(array, left, right, comparer);

            // Recurse on smaller partition, iterate on larger (tail call optimization)
            if (pivot - left < right - pivot)
            {
                StorageIntroSort(array, left, pivot - 1L, comparer, depthLimit);
                left = pivot + 1L;
            }
            else
            {
                StorageIntroSort(array, pivot + 1L, right, comparer, depthLimit);
                right = pivot - 1L;
            }
        }
    }

    private static void StorageInsertionSort<T, TComparer>(T[][] array, long left, long right, TComparer comparer)
        where TComparer : IComparer<T>
    {
        for (long i = left + 1L; i <= right; i++)
        {
            T key = array.StorageGet(i);
            long j = i - 1L;

            while (j >= left && comparer.Compare(array.StorageGet(j), key) > 0)
            {
                array.StorageSet(j + 1L, array.StorageGet(j));
                j--;
            }
            array.StorageSet(j + 1L, key);
        }
    }

    private static long StoragePartition<T, TComparer>(T[][] array, long left, long right, TComparer comparer)
        where TComparer : IComparer<T>
    {
        // Median-of-three pivot selection for better average case
        long mid = left + (right - left) / 2L;

        T leftVal = array.StorageGet(left);
        T midVal = array.StorageGet(mid);
        T rightVal = array.StorageGet(right);

        // Sort left, mid, right and use mid as pivot
        if (comparer.Compare(leftVal, midVal) > 0)
        {
            array.StorageSwap(left, mid);
        }
        if (comparer.Compare(array.StorageGet(left), rightVal) > 0)
        {
            array.StorageSwap(left, right);
        }
        if (comparer.Compare(array.StorageGet(mid), array.StorageGet(right)) > 0)
        {
            array.StorageSwap(mid, right);
        }

        // Move pivot to right - 1
        array.StorageSwap(mid, right - 1L);
        T pivot = array.StorageGet(right - 1L);

        long i = left;
        long j = right - 1L;

        while (true)
        {
            while (comparer.Compare(array.StorageGet(++i), pivot) < 0) { }
            while (j > left && comparer.Compare(array.StorageGet(--j), pivot) > 0) { }

            if (i >= j)
            {
                break;
            }

            array.StorageSwap(i, j);
        }

        // Restore pivot
        array.StorageSwap(i, right - 1L);
        return i;
    }

    private static void StorageHeapify<T, TComparer>(this T[][] array, long i, long left, long right, TComparer comparer)
        where TComparer : IComparer<T>
    {
        // Iterative implementation to avoid StackOverflowException with large arrays
        long current = i;
        while (true)
        {
            long maxIndex = current;
            long leftIndex = left + (2L * (current - left)) + 1L;
            long rightIndex = left + (2L * (current - left)) + 2L;

            if (leftIndex <= right && comparer.Compare(array.StorageGet(maxIndex), array.StorageGet(leftIndex)) < 0)
            {
                maxIndex = leftIndex;
            }

            if (rightIndex <= right && comparer.Compare(array.StorageGet(maxIndex), array.StorageGet(rightIndex)) < 0)
            {
                maxIndex = rightIndex;
            }

            if (maxIndex == current)
            {
                break;
            }

            array.StorageSwap(current, maxIndex);
            current = maxIndex;
        }
    }

    private static void StorageHeapSort<T, TComparer>(T[][] array, long left, long right, TComparer comparer)
        where TComparer : IComparer<T>
    {
        // Build max heap
        for (long i = left + (right - left) / 2L; i >= left; i--)
        {
            array.StorageHeapify(i, left, right, comparer);
        }

        // Extract elements from heap
        for (long i = right; i > left; i--)
        {
            array.StorageSwap(left, i);
            array.StorageHeapify(left, left, i - 1L, comparer);
        }
    }

    /// <summary>
    /// Sorts the array in parallel using a struct comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageParallelSort<T, TComparer>(this T[][] array, TComparer comparer, long offset, long count, int maxDegreeOfParallelism = -1)
        where TComparer : IComparer<T>
    {
        if (count <= 1L)
        {
            return;
        }

        // Determine number of workers
        int workerCount = maxDegreeOfParallelism <= 0 ? Environment.ProcessorCount : maxDegreeOfParallelism;

        // For small arrays or single thread, use regular sort
        const long ParallelThreshold = 100_000L;
        if (count < ParallelThreshold || workerCount == 1)
        {
            array.StorageSort(comparer, offset, count);
            return;
        }

        // Limit workers based on array size (at least 10000 elements per worker)
        int effectiveWorkers = (int)Math.Min(workerCount, Math.Max(1, count / 10_000L));
        effectiveWorkers = Math.Max(1, effectiveWorkers);

        if (effectiveWorkers == 1)
        {
            array.StorageSort(comparer, offset, count);
            return;
        }

        // Phase 1: Sort chunks in parallel
        long chunkSize = count / effectiveWorkers;
        Task[] sortTasks = new Task[effectiveWorkers];

        for (int i = 0; i < effectiveWorkers; i++)
        {
            int workerIndex = i;
            long chunkStart = offset + (workerIndex * chunkSize);
            long chunkCount = (workerIndex == effectiveWorkers - 1)
                ? (count - (workerIndex * chunkSize))
                : chunkSize;

            sortTasks[i] = Task.Run(() =>
            {
                array.StorageSort(comparer, chunkStart, chunkCount);
            });
        }

        System.Threading.Tasks.Task.WaitAll(sortTasks);

        // Phase 2: Merge sorted chunks
        (long Start, long Count)[] chunkBoundaries = new (long Start, long Count)[effectiveWorkers];
        for (int i = 0; i < effectiveWorkers; i++)
        {
            long chunkStart = offset + (i * chunkSize);
            long chunkCount = (i == effectiveWorkers - 1)
                ? (count - (i * chunkSize))
                : chunkSize;
            chunkBoundaries[i] = (chunkStart, chunkCount);
        }

        // Merge pairs until only one chunk remains
        while (chunkBoundaries.Length > 1)
        {
            int pairCount = (chunkBoundaries.Length + 1) / 2;
            (long Start, long Count)[] newBoundaries = new (long Start, long Count)[pairCount];
            Task[] mergeTasks = new Task[pairCount];

            for (int i = 0; i < pairCount; i++)
            {
                int leftIdx = i * 2;
                int rightIdx = leftIdx + 1;

                if (rightIdx >= chunkBoundaries.Length)
                {
                    newBoundaries[i] = chunkBoundaries[leftIdx];
                    mergeTasks[i] = System.Threading.Tasks.Task.CompletedTask;
                }
                else
                {
                    (long Start, long Count) leftChunk = chunkBoundaries[leftIdx];
                    (long Start, long Count) rightChunk = chunkBoundaries[rightIdx];
                    int pairIndex = i;

                    mergeTasks[i] = Task.Run(() =>
                    {
                        StorageMergeInPlace(array, comparer, leftChunk.Start, leftChunk.Count, rightChunk.Count);
                    });

                    newBoundaries[i] = (leftChunk.Start, leftChunk.Count + rightChunk.Count);
                }
            }

            Task.WaitAll(mergeTasks);
            chunkBoundaries = newBoundaries;
        }
    }

    /// <summary>
    /// Merges two adjacent sorted regions in-place using a rotation-based algorithm (struct comparer version).
    /// </summary>
    private static void StorageMergeInPlace<T, TComparer>(T[][] array, TComparer comparer, long start, long leftCount, long rightCount)
        where TComparer : IComparer<T>
    {
        if (leftCount == 0 || rightCount == 0)
        {
            return;
        }

        long leftStart = start;
        long leftEnd = start + leftCount;
        long rightEnd = start + leftCount + rightCount;

        while (leftStart < leftEnd && leftEnd < rightEnd)
        {
            // Find first element in left that is greater than first element in right
            while (leftStart < leftEnd && comparer.Compare(array.StorageGet(leftStart), array.StorageGet(leftEnd)) <= 0)
            {
                leftStart++;
            }

            if (leftStart >= leftEnd)
            {
                break;
            }

            // Find first element in right that is >= element at leftStart
            long rightInsert = leftEnd;
            while (rightInsert < rightEnd && comparer.Compare(array.StorageGet(rightInsert), array.StorageGet(leftStart)) < 0)
            {
                rightInsert++;
            }

            // Rotate the range [leftStart, rightInsert) so that [leftEnd, rightInsert) comes first
            long rotateCount = rightInsert - leftEnd;
            StorageRotate(array, leftStart, leftEnd - leftStart, rotateCount);

            // Update boundaries
            leftStart += rotateCount + 1;
            leftEnd += rotateCount;
        }
    }

    /// <summary>
    /// Performs a binary search using a struct comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long StorageBinarySearch<T, TComparer>(this T[][] array, T item, TComparer comparer, long offset, long count)
        where TComparer : IComparer<T>
    {
        if (count == 0L)
        {
            return -1L;
        }

        if (count < 0L)
        {
            count = array.StorageGetCount(offset);
        }

        long left = offset;
        long right = offset + count - 1L;

        while (left <= right)
        {
            long mid = left + (right - left) / 2L;
            T midItem = array.StorageGet(mid);
            int compareResult = comparer.Compare(item, midItem);

            if (compareResult == 0)
            {
                return mid;
            }

            if (compareResult < 0)
            {
                right = mid - 1L;
            }
            else
            {
                left = mid + 1L;
            }
        }

        return -1L;
    }

    #endregion

    /// <summary>
    /// Sorts the array in parallel using multiple threads.
    /// Recommended for large arrays (>100,000 elements) where the overhead of parallelization is justified.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The storage array to sort.</param>
    /// <param name="comparer">The comparison function.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="count">The number of elements to sort.</param>
    /// <param name="maxDegreeOfParallelism">Maximum number of threads to use. -1 or 0 uses Environment.ProcessorCount.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageParallelSort<T>(this T[][] array, Func<T, T, int> comparer, long offset, long count, int maxDegreeOfParallelism = -1)
    {
        if (count <= 1L)
        {
            return;
        }
        if (comparer is null)
        {
            throw new ArgumentNullException(nameof(comparer));
        }

        // Determine number of workers
        int workerCount = maxDegreeOfParallelism <= 0 ? Environment.ProcessorCount : maxDegreeOfParallelism;

        // For small arrays or single thread, use regular sort
        const long ParallelThreshold = 100_000L;
        if (count < ParallelThreshold || workerCount == 1)
        {
            array.StorageSort(comparer, offset, count);
            return;
        }

        // Limit workers based on array size (at least 10000 elements per worker)
        int effectiveWorkers = (int)Math.Min(workerCount, Math.Max(1, count / 10_000L));
        effectiveWorkers = Math.Max(1, effectiveWorkers);

        if (effectiveWorkers == 1)
        {
            array.StorageSort(comparer, offset, count);
            return;
        }

        // Phase 1: Sort chunks in parallel
        long chunkSize = count / effectiveWorkers;
        Task[] sortTasks = new Task[effectiveWorkers];

        for (int i = 0; i < effectiveWorkers; i++)
        {
            int workerIndex = i;
            long chunkStart = offset + (workerIndex * chunkSize);
            long chunkCount = (workerIndex == effectiveWorkers - 1)
                ? (count - (workerIndex * chunkSize))  // Last chunk gets remainder
                : chunkSize;

            sortTasks[i] = Task.Run(() =>
            {
                array.StorageSort(comparer, chunkStart, chunkCount);
            });
        }

        Task.WaitAll(sortTasks);

        // Phase 2: Merge sorted chunks
        // Use iterative pairwise merging for better parallelism
        (long Start, long Count)[] chunkBoundaries = new (long Start, long Count)[effectiveWorkers];
        for (int i = 0; i < effectiveWorkers; i++)
        {
            long chunkStart = offset + (i * chunkSize);
            long chunkCount = (i == effectiveWorkers - 1)
                ? (count - (i * chunkSize))
                : chunkSize;
            chunkBoundaries[i] = (chunkStart, chunkCount);
        }

        // Merge pairs until only one chunk remains
        while (chunkBoundaries.Length > 1)
        {
            int pairCount = (chunkBoundaries.Length + 1) / 2;
            (long Start, long Count)[] newBoundaries = new (long Start, long Count)[pairCount];
            Task[] mergeTasks = new Task[pairCount];

            for (int i = 0; i < pairCount; i++)
            {
                int leftIdx = i * 2;
                int rightIdx = leftIdx + 1;

                if (rightIdx >= chunkBoundaries.Length)
                {
                    // Odd chunk, no merge needed
                    newBoundaries[i] = chunkBoundaries[leftIdx];
                    mergeTasks[i] = System.Threading.Tasks.Task.CompletedTask;
                }
                else
                {
                    (long Start, long Count) leftChunk = chunkBoundaries[leftIdx];
                    (long Start, long Count) rightChunk = chunkBoundaries[rightIdx];
                    int pairIndex = i;

                    mergeTasks[i] = Task.Run(() =>
                    {
                        StorageMergeInPlace(array, comparer, leftChunk.Start, leftChunk.Count, rightChunk.Count);
                    });

                    newBoundaries[i] = (leftChunk.Start, leftChunk.Count + rightChunk.Count);
                }
            }

            Task.WaitAll(mergeTasks);
            chunkBoundaries = newBoundaries;
        }
    }

    /// <summary>
    /// Merges two adjacent sorted regions in-place using a rotation-based algorithm.
    /// </summary>
    private static void StorageMergeInPlace<T>(T[][] array, Func<T, T, int> comparer, long start, long leftCount, long rightCount)
    {
        if (leftCount == 0 || rightCount == 0)
        {
            return;
        }

        long leftStart = start;
        long leftEnd = start + leftCount;
        long rightEnd = start + leftCount + rightCount;

        while (leftStart < leftEnd && leftEnd < rightEnd)
        {
            // Find first element in left that is greater than first element in right
            while (leftStart < leftEnd && comparer.Invoke(array.StorageGet(leftStart), array.StorageGet(leftEnd)) <= 0)
            {
                leftStart++;
            }

            if (leftStart >= leftEnd)
            {
                break;
            }

            // Find first element in right that is >= element at leftStart
            long rightInsert = leftEnd;
            while (rightInsert < rightEnd && comparer.Invoke(array.StorageGet(rightInsert), array.StorageGet(leftStart)) < 0)
            {
                rightInsert++;
            }

            // Rotate the range [leftStart, rightInsert) so that [leftEnd, rightInsert) comes first
            long rotateCount = rightInsert - leftEnd;
            StorageRotate(array, leftStart, leftEnd - leftStart, rotateCount);

            // Update boundaries
            leftStart += rotateCount + 1;
            leftEnd += rotateCount;
        }
    }

    /// <summary>
    /// Rotates elements: moves the last 'rightCount' elements to the front of the range.
    /// [a,b,c,d,e] with leftCount=3, rightCount=2 becomes [d,e,a,b,c]
    /// </summary>
    private static void StorageRotate<T>(T[][] array, long start, long leftCount, long rightCount)
    {
        if (leftCount == 0 || rightCount == 0)
        {
            return;
        }

        // Use reversal algorithm: reverse left, reverse right, reverse all
        StorageReverse(array, start, leftCount);
        StorageReverse(array, start + leftCount, rightCount);
        StorageReverse(array, start, leftCount + rightCount);
    }

    /// <summary>
    /// Reverses elements in the specified range.
    /// </summary>
    private static void StorageReverse<T>(T[][] array, long start, long count)
    {
        long left = start;
        long right = start + count - 1;

        while (left < right)
        {
            array.StorageSwap(left, right);
            left++;
            right--;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long StorageBinarySearch<T>(this T[][] array, T item, Func<T, T, int> comparer, long offset, long count)
    {
        if (count == 0L)
        {
            return -1L;
        }
        if (comparer is null)
        {
            throw new ArgumentNullException(nameof(comparer));
        }

        if (count < 0L)
        {
            count = array.StorageGetCount(offset);
        }

        long left = offset;
        long right = offset + count - 1L;

        while (left <= right)
        {
            // Avoid overflow: use subtraction instead of addition
            long mid = left + (right - left) / 2L;

            T midItem = array.StorageGet(mid);

            int compareResult = comparer.Invoke(item, midItem);

            // item == midItem
            if (compareResult == 0)
            {
                return mid;
            }

            // item < midItem
            if (compareResult < 0)
            {
                right = mid - 1L;
            }
            else // item > midItem
            {
                left = mid + 1L;
            }
        }

        return -1L;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageCopyTo<T>(this T[][] source, T[][] target, long sourceOffset, long targetOffset, long count)
    {
        if (count == 0L)
        {
            return;
        }

        // Handle overlapping regions - if copying within same array and target > source, copy backwards
        bool sameArray = ReferenceEquals(source, target);
        bool needsReverseCopy = sameArray && targetOffset > sourceOffset && targetOffset < sourceOffset + count;

        if (needsReverseCopy)
        {
            // Copy backwards to avoid overwriting source data before it's read
            long remaining = count;
            while (remaining > 0L)
            {
                long lastIndex = remaining - 1L;
                (int srcStorageIdx, int srcItemIdx) = StorageGetIndex(sourceOffset + lastIndex);
                (int tgtStorageIdx, int tgtItemIdx) = StorageGetIndex(targetOffset + lastIndex);

                T[] srcArray = source[srcStorageIdx];
                T[] tgtArray = target[tgtStorageIdx];

                // Calculate how many elements we can copy from current segment pair
                long elementsFromSrcSegment = srcItemIdx + 1L;
                long elementsFromTgtSegment = tgtItemIdx + 1L;
                long elementsToCopy = Math.Min(Math.Min(elementsFromSrcSegment, elementsFromTgtSegment), remaining);

                int srcStart = srcItemIdx - (int)elementsToCopy + 1;
                int tgtStart = tgtItemIdx - (int)elementsToCopy + 1;

                Array.Copy(srcArray, srcStart, tgtArray, tgtStart, elementsToCopy);
                remaining -= elementsToCopy;
            }
        }
        else
        {
            // Forward copy (original logic)
            long currentCount = 0L;

            while (currentCount < count)
            {
                (int currentSourceStorageIndex, int currentSourceItemIndex) = StorageGetIndex(sourceOffset + currentCount);
                T[] currentSourceArray = source[currentSourceStorageIndex];

                (int currentTargetStorageIndex, int currentTargetItemIndex) = StorageGetIndex(targetOffset + currentCount);
                T[] currentTargetArray = target[currentTargetStorageIndex];

                long elementsToCopyCount = Math.Min(currentSourceArray.Length - currentSourceItemIndex, currentTargetArray.Length - currentTargetItemIndex);
                elementsToCopyCount = Math.Min(elementsToCopyCount, count - currentCount);

                if (elementsToCopyCount <= 0)
                {
                    throw new ArgumentException("No elements to copy. Check source and target arrays and their offsets/counts.");
                }

                Array.Copy(currentSourceArray, currentSourceItemIndex, currentTargetArray, currentTargetItemIndex, elementsToCopyCount);

                currentCount += elementsToCopyCount;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageCopyToArray<T>(this T[][] source, T[] target, long sourceOffset, int targetOffset, int count)
    {
        int currentCount = 0;
        int currentTargetItemIndex = targetOffset;

        while (currentCount < count)
        {
            (int currentSourceStorageIndex, int currentSourceItemIndex) = StorageGetIndex(sourceOffset + currentCount);
            T[] currentSourceArray = source[currentSourceStorageIndex];

            int elementsToCopyCount = Math.Min(currentSourceArray.Length - currentSourceItemIndex, target.Length - currentTargetItemIndex);
            elementsToCopyCount = Math.Min(elementsToCopyCount, count - currentCount);

            if (elementsToCopyCount <= 0)
            {
                throw new ArgumentException("No elements to copy. Check source and target arrays and their offsets/counts.");
            }

            Array.Copy(currentSourceArray, currentSourceItemIndex, target, currentTargetItemIndex, elementsToCopyCount);

            currentCount += elementsToCopyCount;
            currentTargetItemIndex += elementsToCopyCount;
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageCopyToSpan<T>(this T[][] source, Span<T> target, long sourceOffset, int count)
    {
        int currentCount = 0;
        int currentTargetItemIndex = 0;

        while (currentCount < count)
        {
            (int currentSourceStorageIndex, int currentSourceItemIndex) = StorageGetIndex(sourceOffset + currentCount);
            T[] currentSourceArray = source[currentSourceStorageIndex];

            int elementsToCopyCount = Math.Min(currentSourceArray.Length - currentSourceItemIndex, target.Length - currentTargetItemIndex);
            elementsToCopyCount = Math.Min(elementsToCopyCount, count - currentCount);

            if (elementsToCopyCount <= 0)
            {
                throw new ArgumentException("No elements to copy. Check source and target arrays and their offsets/counts.");
            }

            ReadOnlySpan<T> sourceSpan = currentSourceArray.AsSpan(currentSourceItemIndex, elementsToCopyCount);
            Span<T> targetSpan = target.Slice(currentTargetItemIndex, elementsToCopyCount);
            sourceSpan.CopyTo(targetSpan);

            currentCount += elementsToCopyCount;
            currentTargetItemIndex += elementsToCopyCount;
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageCopyFrom<T>(this T[][] target, T[][] source, long sourceOffset, long targetOffset, long count)
    {
        source.StorageCopyTo(target, sourceOffset, targetOffset, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageCopyFromArray<T>(this T[][] target, T[] source, int sourceOffset, long targetOffset, int count)
    {
        long currentCount = 0L;
        int currentSourceItemIndex = sourceOffset;

        while (currentCount < count)
        {
            (int currentTargetStorageIndex, int currentTargetItemIndex) = StorageGetIndex(targetOffset + currentCount);
            T[] currentTargetArray = target[currentTargetStorageIndex];

            int elementsToCopyCount = Math.Min(currentTargetArray.Length - currentTargetItemIndex, source.Length - currentSourceItemIndex);
            elementsToCopyCount = Math.Min(elementsToCopyCount, count - (int)currentCount);

            if (elementsToCopyCount <= 0)
            {
                throw new ArgumentException("No elements to copy. Check source and target arrays and their offsets/counts.");
            }

            Array.Copy(source, currentSourceItemIndex, currentTargetArray, currentTargetItemIndex, elementsToCopyCount);

            currentCount += elementsToCopyCount;
            currentSourceItemIndex += elementsToCopyCount;
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageCopyFromSpan<T>(this T[][] target, ReadOnlySpan<T> source, long targetOffset, int count)
    {
        long currentCount = 0L;
        int currentSourceItemIndex = 0;

        while (currentCount < count)
        {
            (int currentTargetStorageIndex, int currentTargetItemIndex) = StorageGetIndex(targetOffset + currentCount);
            T[] currentTargetArray = target[currentTargetStorageIndex];

            int elementsToCopyCount = Math.Min(currentTargetArray.Length - currentTargetItemIndex, source.Length - currentSourceItemIndex);
            elementsToCopyCount = Math.Min(elementsToCopyCount, count - (int)currentCount);

            if (elementsToCopyCount <= 0)
            {
                throw new ArgumentException("No elements to copy. Check source and target arrays and their offsets/counts.");
            }

            ReadOnlySpan<T> sourceSpan = source.Slice(currentSourceItemIndex, elementsToCopyCount);
            Span<T> targetSpan = currentTargetArray.AsSpan(currentTargetItemIndex, elementsToCopyCount);
            sourceSpan.CopyTo(targetSpan);

            currentCount += elementsToCopyCount;
            currentSourceItemIndex += elementsToCopyCount;
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageWriteToStream(this byte[][] source, Stream stream, long offset, long count)
    {
        long currentCount = 0L;

        while (currentCount < count)
        {
            (int currentSourceStorageIndex, int currentSourceItemIndex) = StorageGetIndex(offset + currentCount);
            byte[] currentSourceArray = source[currentSourceStorageIndex];

            long bytesToWriteCount = Math.Min(currentSourceArray.Length - currentSourceItemIndex, count - currentCount);

            stream.Write(currentSourceArray, (int)currentSourceItemIndex, (int)bytesToWriteCount);

            currentCount += bytesToWriteCount;
        }
    }

    internal static long StorageReadFromStream(this byte[][] target, Stream stream, long offset, long count)
    {
        long currentCount = 0L;

        while (currentCount < count)
        {
            (int currentTargetStorageIndex, int currentTargetItemIndex) = StorageGetIndex(offset + currentCount);
            byte[] currentTargetArray = target[currentTargetStorageIndex];

            long bytesToReadCount = Math.Min(currentTargetArray.Length - currentTargetItemIndex, count - currentCount);

            int bytesReadCount = stream.Read(currentTargetArray, (int)currentTargetItemIndex, (int)bytesToReadCount);

            if (bytesReadCount == 0)
            {
                break;
            }

            currentCount += bytesReadCount;
        }

        return currentCount;
    }
}
