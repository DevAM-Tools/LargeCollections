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
/// Internal helper class containing shared algorithms for hash-based collections (LargeSet and LargeDictionary).
/// All methods use generic TComparer parameters to enable JIT devirtualization and inlining for optimal performance.
/// </summary>
internal static class LargeSetHelpers
{
    /// <summary>
    /// Gets the bucket index using a struct equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetBucketIndex<T, TComparer>(ref T item, long capacity, ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        ulong hash = unchecked((uint)comparer.GetHashCode(item));
        long bucketIndex = (long)(hash % (ulong)capacity);
        return bucketIndex;
    }

    /// <summary>
    /// Adds an item to storage using a struct equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void AddToStorage<T, TComparer>(ref T item, SetElement<T>[][] storage, long capacity, ref long count, ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        long bucketIndex = GetBucketIndex(ref item, capacity, ref comparer);

        SetElement<T> element = storage.StorageGet(bucketIndex);

        if (element is null)
        {
            storage.StorageSet(bucketIndex, new SetElement<T>(item));
            count++;
            return;
        }

        while (element is not null)
        {
            if (comparer.Equals(item, element.Item))
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

    /// <summary>
    /// Tries to get an existing item or adds a new one using a struct equality comparer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetOrAddToStorage<T, TComparer>(ref T searchItem, ref T valueIfNotFound, SetElement<T>[][] storage, long capacity, ref long count, ref TComparer comparer, out T value)
        where TComparer : IEqualityComparer<T>
    {
        long bucketIndex = GetBucketIndex(ref searchItem, capacity, ref comparer);

        SetElement<T> element = storage.StorageGet(bucketIndex);

        if (element is null)
        {
            storage.StorageSet(bucketIndex, new SetElement<T>(valueIfNotFound));
            count++;
            value = valueIfNotFound;
            return false;
        }

        while (element is not null)
        {
            if (comparer.Equals(searchItem, element.Item))
            {
                value = element.Item;
                return true;
            }

            if (element.NextElement is null)
            {
                element.NextElement = new SetElement<T>(valueIfNotFound);
                count++;
                value = valueIfNotFound;
                return false;
            }

            element = element.NextElement;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Tries to get an existing item or adds a new one created by factory using a struct equality comparer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetOrAddToStorageWithFactory<T, TComparer>(ref T searchItem, Func<T> valueFactory, SetElement<T>[][] storage, long capacity, ref long count, ref TComparer comparer, out T value)
        where TComparer : IEqualityComparer<T>
    {
        long bucketIndex = GetBucketIndex(ref searchItem, capacity, ref comparer);

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
            if (comparer.Equals(searchItem, element.Item))
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

    /// <summary>
    /// Gets an item from storage using a struct equality comparer for optimal performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetValueFromStorage<T, TComparer>(ref T searchItem, SetElement<T>[][] storage, long capacity, ref TComparer comparer, out T value)
        where TComparer : IEqualityComparer<T>
    {
        long bucketIndex = GetBucketIndex(ref searchItem, capacity, ref comparer);

        SetElement<T> element = storage.StorageGet(bucketIndex);

        while (element is not null)
        {
            if (comparer.Equals(searchItem, element.Item))
            {
                value = element.Item;
                return true;
            }

            element = element.NextElement;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets a reference to an item from storage using a struct equality comparer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T GetRefFromStorage<T, TComparer>(ref T searchItem, SetElement<T>[][] storage, long capacity, ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        long bucketIndex = GetBucketIndex(ref searchItem, capacity, ref comparer);

        SetElement<T> element = storage.StorageGet(bucketIndex);

        while (element is not null)
        {
            if (comparer.Equals(searchItem, element.Item))
            {
                return ref element.Item;
            }
            element = element.NextElement;
        }

        throw new KeyNotFoundException($"The given item was not found in the storage.");
    }

    /// <summary>
    /// Checks if storage contains an item using a struct equality comparer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ContainsInStorage<T, TComparer>(ref T searchItem, SetElement<T>[][] storage, long capacity, ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        return TryGetValueFromStorage(ref searchItem, storage, capacity, ref comparer, out _);
    }

    /// <summary>
    /// Removes an item from storage using a struct equality comparer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool RemoveFromStorage<T, TComparer>(ref T searchItem, SetElement<T>[][] storage, long capacity, ref long count, ref TComparer comparer, out T removedItem)
        where TComparer : IEqualityComparer<T>
    {
        long bucketIndex = GetBucketIndex(ref searchItem, capacity, ref comparer);

        SetElement<T> element = storage.StorageGet(bucketIndex);
        SetElement<T> previousElement = null;

        while (element is not null)
        {
            if (comparer.Equals(searchItem, element.Item))
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

        removedItem = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ClearStorage<T>(SetElement<T>[][] storage, long capacity, ref long count)
    {
        // Use bulk Array.Clear for each segment instead of per-element clearing
        // This is significantly faster for large capacities
        for (int i = 0; i < storage.Length; i++)
        {
            SetElement<T>[] segment = storage[i];
            if (segment != null)
            {
                Array.Clear(segment, 0, segment.Length);
            }
        }

        count = 0L;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CopyStorage<T, TComparer>(SetElement<T>[][] sourceStorage, long sourceCapacity, SetElement<T>[][] targetStorage, long targetCapacity, ref long targetCount, ref TComparer comparer, bool clearSourceStorage)
        where TComparer : IEqualityComparer<T>
    {
        for (long i = 0L; i < sourceCapacity; i++)
        {
            SetElement<T> element = sourceStorage.StorageGet(i);

            while (element is not null)
            {
                AddToStorage(ref element.Item, targetStorage, targetCapacity, ref targetCount, ref comparer);

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
    internal static void DoForEachInStorage<T, TAction>(SetElement<T>[][] storage, long capacity, ref TAction action) where TAction : ILargeAction<T>
    {
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
    internal static void ExtendStorage<T, TComparer>(ref SetElement<T>[][] storage, ref long capacity, ref long count, double capacityGrowFactor, long fixedCapacityGrowAmount, long fixedCapacityGrowLimit, double maxLoadFactor, ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
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
        CopyStorage(storage, capacity, newStorage, newCapacity, ref newStorageCount, ref comparer, true);

        storage = newStorage;
        capacity = newCapacity;
        count = newStorageCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ShrinkStorage<T, TComparer>(ref SetElement<T>[][] storage, ref long capacity, ref long count, double minLoadFactor, double minLoadFactorTolerance, ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
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
        CopyStorage(storage, capacity, newStorage, newCapacity, ref newStorageCount, ref comparer, true);

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
