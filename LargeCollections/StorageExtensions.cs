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

using System.Runtime.CompilerServices;

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
        int storageIndex = (int)((index >> Constants.StorageIndexShiftAmount) & (Constants.MaxStorageCapacity - 1L));
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

        (int storageCount, int remainder) = StorageGetIndex(capacity);
        storageCount++;

        T[][] result = new T[storageCount][];

        for (int i = 0; i < storageCount - 1; i++)
        {
            result[i] = new T[Constants.MaxStorageCapacity];
        }
        result[storageCount - 1] = new T[remainder];

        return result;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageSwap<T>(this T[][] array, long leftIndex, long rightIndex)
    {
        T leftItem = array.StorageGet(leftIndex);
        T rightItem = array.StorageGet(rightIndex);
        array.StorageSet(leftIndex, rightItem);
        array.StorageSet(rightIndex, leftItem);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Contains<T>(this T[][] array, T item, long offset, long count, Func<T, T, bool> equalsFunction)
    {
        if (count == 0L)
        {
            return false;
        }

        (int storageIndex, int itemIndex) = StorageGetIndex(offset);

        long currentCount = 0L;

        T[] currentStorage = array[storageIndex];
        for (int j = itemIndex; j < currentStorage.Length; j++)
        {
            if (currentCount >= count)
            {
                return false;
            }
            T currentItem = currentStorage[j];
            if (equalsFunction.Invoke(item, currentItem))
            {
                return true;
            }
            currentCount++;
        }

        for (int i = storageIndex + 1; i < array.Length; i++)
        {
            if (currentCount >= count)
            {
                return false;
            }
            currentStorage = array[i];
            for (int j = 0; j < currentStorage.Length; j++)
            {
                if (currentCount >= count)
                {
                    return false;
                }

                T currentItem = currentStorage[j];
                if (equalsFunction.Invoke(item, currentItem))
                {
                    return true;
                }
                currentCount++;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageHeapify<T>(this T[][] array, long i, long left, long right, Func<T, T, int> comparer)
    {
        if (comparer is null)
        {
            return;
        }
        long maxIndex = i;
        long leftIndex = left + (2L * (i - left)) + 1L;
        long rightIndex = left + (2L * (i - left)) + 2L;

        if (leftIndex <= right && comparer.Invoke(array.StorageGet(maxIndex), array.StorageGet(leftIndex)) < 0)
        {
            maxIndex = leftIndex;
        }

        if (rightIndex <= right && comparer.Invoke(array.StorageGet(maxIndex), array.StorageGet(rightIndex)) < 0)
        {
            maxIndex = rightIndex;
        }

        if (maxIndex != i)
        {
            array.StorageSwap(i, maxIndex);

            array.StorageHeapify(maxIndex, left, right, comparer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageSort<T>(this T[][] array, Func<T, T, int> comparer, long offset, long count)
    {
        if (count == 0L)
        {
            return;
        }
        if (comparer is null)
        {
            return;
        }

        long left = offset;
        long mid = (offset + count) / 2L;
        long right = offset + count - 1L;

        for (long i = mid; i >= left; i--)
        {
            array.StorageHeapify(i, left, right, comparer);
        }

        for (long i = right; i >= left; i--)
        {
            array.StorageSwap(i, left);

            array.StorageHeapify(left, left, i - 1L, comparer);
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
            return -1L;
        }

        if (count < 0L)
        {
            count = array.StorageGetCount(offset);
        }

        long left = offset;
        long right = offset + count - 1L;

        while (right >= left)
        {
            long mid = (right + left) / 2;

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
                right = mid - 1;
            }
            else // item > midItem
            {
                left = mid + 1;
            }
        }

        return -1L;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageCopyTo<T>(this T[][] source, T[][] target, long sourceOffset, long targetOffset, long count)
    {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void StorageResize<T>(ref T[][] array, long capacity)
    {
        if (capacity < 0L || capacity > Constants.MaxLargeCollectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        (int newStorageCount, int newRemainder) = StorageGetIndex(capacity);
        newStorageCount++;

        Array.Resize(ref array, newStorageCount);

        for (int i = 0; i < newStorageCount - 1; i++)
        {
            if (array[i] == null)
            {
                array[i] = new T[Constants.MaxStorageCapacity];
            }
            else if (array[i].Length < Constants.MaxStorageCapacity)
            {
                Array.Resize(ref array[i], (int)Constants.MaxStorageCapacity);
            }
        }

        if (newStorageCount > 0)
        {
            if (array[newStorageCount - 1] == null)
            {
                array[newStorageCount - 1] = new T[newRemainder];
            }
            else
            {
                Array.Resize(ref array[newStorageCount - 1], newRemainder);
            }
        }
    }
}
