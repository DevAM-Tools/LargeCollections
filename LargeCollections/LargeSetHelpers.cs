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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LargeCollections;

/// <summary>
/// Internal helper class containing shared algorithms for hash-based collections (LargeSet and LargeDictionary)
/// </summary>
internal static class LargeSetHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetBucketIndex<T>(T item, long capacity, Func<T, int> hashCodeFunction)
    {
        ulong hash = unchecked((uint)hashCodeFunction.Invoke(item));
        long bucketIndex = (long)(hash % (ulong)capacity);
        return bucketIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void AddToStorage<T>(ref T item, SetElement<T>[][] storage, long capacity, ref long count, Func<T, T, bool> equalsFunction, Func<T, int> hashCodeFunction)
    {
        long bucketIndex = GetBucketIndex(item, capacity, hashCodeFunction);

        SetElement<T> element = storage.StorageGet(bucketIndex);

        if (element is null)
        {
            storage.StorageSet(bucketIndex, new SetElement<T>(item));
            count++;
            return;
        }

        while (element is not null)
        {
            if (equalsFunction.Invoke(item, element.Item))
            {
                element.Item = item;
                return;
            }

            if (element.NextElement is null)
            {
                element.NextElement = new SetElement<T>(item);
                count++;
                return;
            }

            element = element.NextElement;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetOrAddToStorage<T>(ref T searchItem, ref T valueIfNotFound, SetElement<T>[][] storage, long capacity, ref long count, Func<T, T, bool> equalsFunction, Func<T, int> hashCodeFunction, out T value)
    {
        long bucketIndex = GetBucketIndex(searchItem, capacity, hashCodeFunction);

        SetElement<T> element = storage.StorageGet(bucketIndex);

        if (element is null)
        {
            // Item not found, add valueIfNotFound
            storage.StorageSet(bucketIndex, new SetElement<T>(valueIfNotFound));
            count++;
            value = valueIfNotFound;
            return false;
        }

        while (element is not null)
        {
            if (equalsFunction.Invoke(searchItem, element.Item))
            {
                // Item found, return existing value
                value = element.Item;
                return true;
            }

            if (element.NextElement is null)
            {
                // Item not found, add valueIfNotFound to the chain
                element.NextElement = new SetElement<T>(valueIfNotFound);
                count++;
                value = valueIfNotFound;
                return false;
            }

            element = element.NextElement;
        }

        // Should never reach here
        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetOrAddToStorageWithFactory<T>(T searchItem, Func<T> valueFactory, SetElement<T>[][] storage, long capacity, ref long count, Func<T, T, bool> equalsFunction, Func<T, int> hashCodeFunction, out T value)
    {
        long bucketIndex = GetBucketIndex(searchItem, capacity, hashCodeFunction);

        SetElement<T> element = storage.StorageGet(bucketIndex);

        if (element is null)
        {
            // Item not found, create and add it
            T newItem = valueFactory.Invoke();
            storage.StorageSet(bucketIndex, new SetElement<T>(newItem));
            count++;
            value = newItem;
            return false;
        }

        while (element is not null)
        {
            if (equalsFunction.Invoke(searchItem, element.Item))
            {
                // Item found, return existing value
                value = element.Item;
                return true;
            }

            if (element.NextElement is null)
            {
                // Item not found, create and add it to the chain
                T newItem = valueFactory.Invoke();
                element.NextElement = new SetElement<T>(newItem);
                count++;
                value = newItem;
                return false;
            }

            element = element.NextElement;
        }

        // Should never reach here
        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool RemoveFromStorage<T>(T item, SetElement<T>[][] storage, ref long count, long capacity, Func<T, T, bool> equalsFunction, Func<T, int> hashCodeFunction, out T removedItem)
    {
        removedItem = default;

        long bucketIndex = GetBucketIndex(item, capacity, hashCodeFunction);

        SetElement<T> element = storage.StorageGet(bucketIndex);
        SetElement<T> previousElement = null;

        while (element is not null)
        {
            if (equalsFunction.Invoke(item, element.Item))
            {
                removedItem = element.Item;
                element.Item = default;

                // Is it the first and only element?
                if (previousElement is null && element.NextElement is null)
                {
                    storage.StorageSet(bucketIndex, null);
                }
                // Is it the first but one of many elements?
                else if (previousElement is null && element.NextElement is not null)
                {
                    storage.StorageSet(bucketIndex, element.NextElement);
                }
                // Is it one of many elements but not the first one?
                else
                {
                    previousElement.NextElement = element.NextElement;
                }

                count--;
                return true;
            }

            previousElement = element;
            element = element.NextElement;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ClearStorage<T>(SetElement<T>[][] storage, long capacity, ref long count)
    {

        for (long i = 0L; i < capacity; i++)
        {
            SetElement<T> element = storage.StorageGet(i);

            while (element is not null)
            {
                element.Item = default;

                SetElement<T> nextElement = element.NextElement;
                element.NextElement = null;
                element = nextElement;
            }

            storage.StorageSet(i, null);
        }

        count = 0L;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetValueFromStorage<T>(T item, SetElement<T>[][] storage, long capacity, Func<T, T, bool> equalsFunction, Func<T, int> hashCodeFunction, out T value)
    {
        long bucketIndex = GetBucketIndex(item, capacity, hashCodeFunction);

        SetElement<T> element = storage.StorageGet(bucketIndex);

        while (element is not null)
        {
            if (equalsFunction.Invoke(item, element.Item))
            {
                value = element.Item;
                return true;
            }
            element = element.NextElement;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T GetRefFromStorage<T>(T item, SetElement<T>[][] storage, long capacity, Func<T, T, bool> equalsFunction, Func<T, int> hashCodeFunction)
    {
        long bucketIndex = GetBucketIndex(item, capacity, hashCodeFunction);

        SetElement<T> element = storage.StorageGet(bucketIndex);

        while (element is not null)
        {
            if (equalsFunction.Invoke(item, element.Item))
            {
                return ref element.Item;
            }
            element = element.NextElement;
        }

        throw new KeyNotFoundException($"The given item was not found in the storage.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ContainsInStorage<T>(T item, SetElement<T>[][] storage, long capacity, Func<T, T, bool> equalsFunction, Func<T, int> hashCodeFunction)
    {
        return TryGetValueFromStorage(item, storage, capacity, equalsFunction, hashCodeFunction, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CopyStorage<T>(SetElement<T>[][] sourceStorage, long sourceCapacity, SetElement<T>[][] targetStorage, long targetCapacity, ref long targetCount, Func<T, T, bool> equalsFunction, Func<T, int> hashCodeFunction, bool clearSourceStorage)
    {
        for (long i = 0L; i < sourceCapacity; i++)
        {
            SetElement<T> element = sourceStorage.StorageGet(i);

            while (element is not null)
            {
                AddToStorage(ref element.Item, targetStorage, targetCapacity, ref targetCount, equalsFunction, hashCodeFunction);

                SetElement<T> nextElement = element.NextElement;
                if (clearSourceStorage)
                {
                    element.NextElement = null;
                }
                element = nextElement;
            }

            if (clearSourceStorage)
            {
                sourceStorage.StorageSet(i, null);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IEnumerable<T> GetAllFromStorage<T>(SetElement<T>[][] storage, long capacity)
    {

        for (long i = 0L; i < capacity; i++)
        {
            SetElement<T> element = storage.StorageGet(i);

            while (element is not null)
            {
                yield return element.Item;
                element = element.NextElement;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DoForEachInStorage<T>(SetElement<T>[][] storage, long capacity, Action<T> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        for (long i = 0L; i < capacity; i++)
        {
            SetElement<T> element = storage.StorageGet(i);

            while (element is not null)
            {
                ref T item = ref element.Item;
                action.Invoke(item);
                element = element.NextElement;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DoForEachInStorage<T, TUserData>(SetElement<T>[][] storage, long capacity, ActionWithUserData<T, TUserData> action, ref TUserData userData)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        for (long i = 0L; i < capacity; i++)
        {
            SetElement<T> element = storage.StorageGet(i);

            while (element is not null)
            {
                ref T item = ref element.Item;
                action.Invoke(item, ref userData);
                element = element.NextElement;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ExtendStorage<T>(ref SetElement<T>[][] storage, ref long capacity, ref long count, double capacityGrowFactor, long fixedCapacityGrowAmount, long fixedCapacityGrowLimit, double maxLoadFactor, Func<T, T, bool> equalsFunction, Func<T, int> hashCodeFunction)
    {
        double loadFactor = (double)count / capacity;

        if (loadFactor <= maxLoadFactor)
        {
            return;
        }

        // Check if we're already at maximum capacity
        if (capacity >= Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Cannot extend collection beyond maximum capacity of {Constants.MaxLargeCollectionCount} elements.");
        }

        // As long as the used hash value only uses 32 bit it does not make sense to use more than 2^32-1 buckets
        long maxCapacity = Math.Min(Constants.MaxLargeCollectionCount, uint.MaxValue);
        if (capacity >= maxCapacity)
        {
            throw new InvalidOperationException($"Cannot extend collection beyond maximum capacity of {maxCapacity} elements.");
        }

        long newCapacity = StorageExtensions.GetGrownCapacity(capacity, capacityGrowFactor, fixedCapacityGrowAmount, fixedCapacityGrowLimit);

        // Cap to maximum allowed capacity (both MaxLargeCollectionCount and uint.MaxValue for hash bucket limit)
        if (newCapacity > maxCapacity)
        {
            newCapacity = maxCapacity;
        }

        SetElement<T>[][] newStorage = StorageExtensions.StorageCreate<SetElement<T>>(newCapacity);
        long newStorageCount = 0L;
        CopyStorage(storage, capacity, newStorage, newCapacity, ref newStorageCount, equalsFunction, hashCodeFunction, true);

        storage = newStorage;
        capacity = newCapacity;
        count = newStorageCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ShrinkStorage<T>(ref SetElement<T>[][] storage, ref long capacity, ref long count, double minLoadFactor, double minLoadFactorTolerance, Func<T, T, bool> equalsFunction, Func<T, int> hashCodeFunction)
    {
        double loadFactor = (double)count / capacity;

        if (loadFactor >= minLoadFactor * minLoadFactorTolerance)
        {
            return;
        }

        long newCapacity = (long)(capacity * minLoadFactor);
        newCapacity = newCapacity > 0L ? newCapacity : 1L;

        SetElement<T>[][] newStorage = StorageExtensions.StorageCreate<SetElement<T>>(newCapacity);
        long newStorageCount = 0L;
        CopyStorage(storage, capacity, newStorage, newCapacity, ref newStorageCount, equalsFunction, hashCodeFunction, true);

        storage = newStorage;
        capacity = newCapacity;
        count = newStorageCount;
    }
}

/// <summary>
/// Internal element class for hash-based storage
/// </summary>
[DebuggerDisplay("Item = {Item}")]
internal class SetElement<T>
{
    internal T Item;
    internal SetElement<T> NextElement;

    internal SetElement(T item, SetElement<T> nextElement)
    {
        Item = item;
        NextElement = nextElement;
    }

    internal SetElement() : this(default, null) { }

    internal SetElement(T item) : this(item, null) { }
}
