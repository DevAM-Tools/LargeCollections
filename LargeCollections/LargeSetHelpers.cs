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
using System.Runtime.CompilerServices;

namespace LargeCollections;

/// <summary>
/// Hash entry for separate chaining storage. Uses index-based linking for cache efficiency.
/// Similar to .NET Dictionary's Entry structure.
/// </summary>
internal struct HashEntry<T>
{
    /// <summary>Cached hash code of the item.</summary>
    internal int HashCode;
    
    /// <summary>
    /// 0-based index of next entry in chain: -1 means end of chain.
    /// Also encodes whether this entry is part of the free list by changing sign and subtracting 2,
    /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 on free list, etc.
    /// </summary>
    internal int Next;
    
    /// <summary>The stored item.</summary>
    internal T Item;
}

/// <summary>
/// Internal helper class containing shared algorithms for hash-based collections (LargeSet and LargeDictionary).
/// Uses separate chaining with index-based linking for optimal performance (similar to .NET Dictionary).
/// </summary>
internal static class LargeSetHelpers
{
    /// <summary>Start marker for free list encoding (subtract index and this value to encode).</summary>
    private const int StartOfFreeList = -3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetBucketIndex(int hashCode, long bucketCount)
    {
        return (long)((uint)hashCode % (ulong)bucketCount);
    }

    /// <summary>
    /// Adds an item to storage using separate chaining.
    /// </summary>
    internal static void AddToStorage<T, TComparer>(
        ref T item,
        int[][] buckets,
        HashEntry<T>[][] entries,
        long bucketCount,
        ref long count,
        ref int freeList,
        ref int freeCount,
        ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        int hashCode = comparer.GetHashCode(item) & 0x7FFFFFFF; // Ensure positive
        long bucketIndex = GetBucketIndex(hashCode, bucketCount);
        
        ref int bucket = ref buckets.StorageGetRef(bucketIndex);
        int i = bucket - 1; // Buckets are 1-based
        
        // Search for existing item
        while (i >= 0)
        {
            ref HashEntry<T> entry = ref entries.StorageGetRef(i);
            if (entry.HashCode == hashCode && comparer.Equals(entry.Item, item))
            {
                // Update existing item
                entry.Item = item;
                return;
            }
            i = entry.Next;
        }
        
        // Item not found, add new entry
        int index;
        if (freeCount > 0)
        {
            // Reuse entry from free list
            index = freeList;
            ref HashEntry<T> freeEntry = ref entries.StorageGetRef(index);
            freeList = StartOfFreeList - freeEntry.Next;
            freeCount--;
        }
        else
        {
            // Use next available slot
            index = (int)count;
        }
        
        ref HashEntry<T> newEntry = ref entries.StorageGetRef(index);
        newEntry.HashCode = hashCode;
        newEntry.Next = bucket - 1; // Link to previous head of chain
        newEntry.Item = item;
        bucket = index + 1; // Buckets are 1-based
        
        count++;
    }

    /// <summary>
    /// Tries to get an existing item or adds a new one.
    /// </summary>
    internal static bool TryGetOrAddToStorage<T, TComparer>(
        ref T searchItem,
        ref T valueIfNotFound,
        int[][] buckets,
        HashEntry<T>[][] entries,
        long bucketCount,
        ref long count,
        ref int freeList,
        ref int freeCount,
        ref TComparer comparer,
        out T value)
        where TComparer : IEqualityComparer<T>
    {
        int hashCode = comparer.GetHashCode(searchItem) & 0x7FFFFFFF;
        long bucketIndex = GetBucketIndex(hashCode, bucketCount);
        
        ref int bucket = ref buckets.StorageGetRef(bucketIndex);
        int i = bucket - 1;
        
        // Search for existing item
        while (i >= 0)
        {
            ref HashEntry<T> entry = ref entries.StorageGetRef(i);
            if (entry.HashCode == hashCode && comparer.Equals(entry.Item, searchItem))
            {
                value = entry.Item;
                return true;
            }
            i = entry.Next;
        }
        
        // Not found, add new entry
        int index;
        if (freeCount > 0)
        {
            index = freeList;
            ref HashEntry<T> freeEntry = ref entries.StorageGetRef(index);
            freeList = StartOfFreeList - freeEntry.Next;
            freeCount--;
        }
        else
        {
            index = (int)count;
        }
        
        int valueHashCode = comparer.GetHashCode(valueIfNotFound) & 0x7FFFFFFF;
        ref HashEntry<T> newEntry = ref entries.StorageGetRef(index);
        newEntry.HashCode = valueHashCode;
        newEntry.Next = bucket - 1;
        newEntry.Item = valueIfNotFound;
        bucket = index + 1;
        
        count++;
        value = valueIfNotFound;
        return false;
    }

    /// <summary>
    /// Tries to get an existing item or adds a new one created by factory.
    /// </summary>
    internal static bool TryGetOrAddToStorageWithFactory<T, TComparer>(
        ref T searchItem,
        Func<T> valueFactory,
        int[][] buckets,
        HashEntry<T>[][] entries,
        long bucketCount,
        ref long count,
        ref int freeList,
        ref int freeCount,
        ref TComparer comparer,
        out T value)
        where TComparer : IEqualityComparer<T>
    {
        int hashCode = comparer.GetHashCode(searchItem) & 0x7FFFFFFF;
        long bucketIndex = GetBucketIndex(hashCode, bucketCount);
        
        ref int bucket = ref buckets.StorageGetRef(bucketIndex);
        int i = bucket - 1;
        
        // Search for existing item
        while (i >= 0)
        {
            ref HashEntry<T> entry = ref entries.StorageGetRef(i);
            if (entry.HashCode == hashCode && comparer.Equals(entry.Item, searchItem))
            {
                value = entry.Item;
                return true;
            }
            i = entry.Next;
        }
        
        // Not found, create and add new entry
        T newItem = valueFactory();
        
        int index;
        if (freeCount > 0)
        {
            index = freeList;
            ref HashEntry<T> freeEntry = ref entries.StorageGetRef(index);
            freeList = StartOfFreeList - freeEntry.Next;
            freeCount--;
        }
        else
        {
            index = (int)count;
        }
        
        int valueHashCode = comparer.GetHashCode(newItem) & 0x7FFFFFFF;
        ref HashEntry<T> newEntry = ref entries.StorageGetRef(index);
        newEntry.HashCode = valueHashCode;
        newEntry.Next = bucket - 1;
        newEntry.Item = newItem;
        bucket = index + 1;
        
        count++;
        value = newItem;
        return false;
    }

    /// <summary>
    /// Gets an item from storage using separate chaining lookup.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetValueFromStorage<T, TComparer>(
        ref T searchItem,
        int[][] buckets,
        HashEntry<T>[][] entries,
        long bucketCount,
        ref TComparer comparer,
        out T value)
        where TComparer : IEqualityComparer<T>
    {
        int hashCode = comparer.GetHashCode(searchItem) & 0x7FFFFFFF;
        long bucketIndex = GetBucketIndex(hashCode, bucketCount);
        
        int i = buckets.StorageGet(bucketIndex) - 1; // Buckets are 1-based
        
        while (i >= 0)
        {
            ref HashEntry<T> entry = ref entries.StorageGetRef(i);
            if (entry.HashCode == hashCode && comparer.Equals(entry.Item, searchItem))
            {
                value = entry.Item;
                return true;
            }
            i = entry.Next;
        }
        
        value = default;
        return false;
    }

    /// <summary>
    /// Gets a reference to an item from storage.
    /// </summary>
    internal static ref T GetRefFromStorage<T, TComparer>(
        ref T searchItem,
        int[][] buckets,
        HashEntry<T>[][] entries,
        long bucketCount,
        ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        int hashCode = comparer.GetHashCode(searchItem) & 0x7FFFFFFF;
        long bucketIndex = GetBucketIndex(hashCode, bucketCount);
        
        int i = buckets.StorageGet(bucketIndex) - 1;
        
        while (i >= 0)
        {
            ref HashEntry<T> entry = ref entries.StorageGetRef(i);
            if (entry.HashCode == hashCode && comparer.Equals(entry.Item, searchItem))
            {
                return ref entry.Item;
            }
            i = entry.Next;
        }
        
        throw new KeyNotFoundException("The given item was not found in the storage.");
    }

    /// <summary>
    /// Checks if storage contains an item.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ContainsInStorage<T, TComparer>(
        ref T searchItem,
        int[][] buckets,
        HashEntry<T>[][] entries,
        long bucketCount,
        ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        return TryGetValueFromStorage(ref searchItem, buckets, entries, bucketCount, ref comparer, out _);
    }

    /// <summary>
    /// Removes an item from storage using separate chaining.
    /// </summary>
    internal static bool RemoveFromStorage<T, TComparer>(
        ref T searchItem,
        int[][] buckets,
        HashEntry<T>[][] entries,
        long bucketCount,
        ref long count,
        ref int freeList,
        ref int freeCount,
        ref TComparer comparer,
        out T removedItem)
        where TComparer : IEqualityComparer<T>
    {
        int hashCode = comparer.GetHashCode(searchItem) & 0x7FFFFFFF;
        long bucketIndex = GetBucketIndex(hashCode, bucketCount);
        
        ref int bucket = ref buckets.StorageGetRef(bucketIndex);
        int i = bucket - 1;
        int last = -1;
        
        while (i >= 0)
        {
            ref HashEntry<T> entry = ref entries.StorageGetRef(i);
            
            if (entry.HashCode == hashCode && comparer.Equals(entry.Item, searchItem))
            {
                removedItem = entry.Item;
                
                // Remove from chain
                if (last < 0)
                {
                    bucket = entry.Next + 1; // Update bucket head
                }
                else
                {
                    entries.StorageGetRef(last).Next = entry.Next;
                }
                
                // Add to free list
                entry.Next = StartOfFreeList - freeList;
                entry.Item = default; // Clear reference for GC
                
                freeList = i;
                freeCount++;
                count--;
                
                return true;
            }
            
            last = i;
            i = entry.Next;
        }
        
        removedItem = default;
        return false;
    }

    /// <summary>
    /// Clears storage.
    /// </summary>
    internal static void ClearStorage<T>(
        int[][] buckets,
        HashEntry<T>[][] entries,
        long bucketCount,
        ref long count,
        ref int freeList,
        ref int freeCount)
    {
        if (count > 0 || freeCount > 0)
        {
            // Clear buckets
            for (int i = 0; i < buckets.Length; i++)
            {
                if (buckets[i] != null)
                    Array.Clear(buckets[i], 0, buckets[i].Length);
            }
            
            // Clear entries up to count + freeCount
            long entriesToClear = count + freeCount;
            for (long i = 0; i < entriesToClear; i++)
            {
                ref HashEntry<T> entry = ref entries.StorageGetRef(i);
                entry = default;
            }
            
            count = 0L;
            freeList = -1;
            freeCount = 0;
        }
    }

    internal static HashStorageEnumerable<T> GetAllFromStorage<T>(HashEntry<T>[][] entries, long count, int freeCount)
    {
        return new HashStorageEnumerable<T>(entries, count, freeCount);
    }

    internal static void DoForEachInStorage<T>(HashEntry<T>[][] entries, long count, int freeCount, Action<T> action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        long totalEntries = count + freeCount;
        for (long i = 0L; i < totalEntries; i++)
        {
            ref HashEntry<T> entry = ref entries.StorageGetRef(i);
            if (entry.Next >= -1) // Not in free list
                action(entry.Item);
        }
    }

    internal static void DoForEachInStorage<T, TAction>(HashEntry<T>[][] entries, long count, int freeCount, ref TAction action) 
        where TAction : ILargeAction<T>
    {
        long totalEntries = count + freeCount;
        for (long i = 0L; i < totalEntries; i++)
        {
            ref HashEntry<T> entry = ref entries.StorageGetRef(i);
            if (entry.Next >= -1) // Not in free list
                action.Invoke(entry.Item);
        }
    }

    internal static void ExtendStorage<T, TComparer>(
        ref int[][] buckets,
        ref HashEntry<T>[][] entries,
        ref long bucketCount,
        ref long count,
        ref int freeList,
        ref int freeCount,
        double capacityGrowFactor,
        long fixedCapacityGrowAmount,
        long fixedCapacityGrowLimit,
        double maxLoadFactor,
        ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        // Check if we need to extend based on load factor or capacity
        long usedSlots = count + freeCount;
        double loadFactor = (double)(count + 1) / bucketCount;
        
        if (loadFactor <= maxLoadFactor && usedSlots < bucketCount)
            return;

        long maxCapacity = Math.Min(Constants.MaxLargeCollectionCount, uint.MaxValue);
        
        if (bucketCount >= maxCapacity)
        {
            if (usedSlots >= bucketCount)
                throw new InvalidOperationException($"Cannot extend collection beyond maximum capacity of {maxCapacity} elements.");
            return;
        }

        long newBucketCount = StorageExtensions.GetGrownCapacity(bucketCount, capacityGrowFactor, fixedCapacityGrowAmount, fixedCapacityGrowLimit);
        if (newBucketCount > maxCapacity)
            newBucketCount = maxCapacity;

        // Create new storage
        int[][] newBuckets = StorageExtensions.StorageCreate<int>(newBucketCount);
        HashEntry<T>[][] newEntries = StorageExtensions.StorageCreate<HashEntry<T>>(newBucketCount);

        // Copy and rehash entries (excluding free list entries)
        long newCount = 0;
        long totalEntries = count + freeCount;
        for (long i = 0; i < totalEntries; i++)
        {
            ref HashEntry<T> oldEntry = ref entries.StorageGetRef(i);
            if (oldEntry.Next >= -1) // Not in free list
            {
                long newBucketIndex = GetBucketIndex(oldEntry.HashCode, newBucketCount);
                ref int newBucket = ref newBuckets.StorageGetRef(newBucketIndex);
                
                ref HashEntry<T> newEntry = ref newEntries.StorageGetRef(newCount);
                newEntry.HashCode = oldEntry.HashCode;
                newEntry.Next = newBucket - 1;
                newEntry.Item = oldEntry.Item;
                newBucket = (int)newCount + 1;
                
                newCount++;
            }
        }

        buckets = newBuckets;
        entries = newEntries;
        bucketCount = newBucketCount;
        count = newCount;
        freeList = -1;
        freeCount = 0;
    }

    internal static void ShrinkStorage<T, TComparer>(
        ref int[][] buckets,
        ref HashEntry<T>[][] entries,
        ref long bucketCount,
        ref long count,
        ref int freeList,
        ref int freeCount,
        double minLoadFactor,
        double minLoadFactorTolerance,
        ref TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        double loadFactor = (double)count / bucketCount;
        if (loadFactor >= minLoadFactor * minLoadFactorTolerance)
            return;

        long newBucketCount = Math.Max(1L, (long)(count / 0.5)); // Target ~50% load after shrink

        int[][] newBuckets = StorageExtensions.StorageCreate<int>(newBucketCount);
        HashEntry<T>[][] newEntries = StorageExtensions.StorageCreate<HashEntry<T>>(newBucketCount);

        long newCount = 0;
        long totalEntries = count + freeCount;
        for (long i = 0; i < totalEntries; i++)
        {
            ref HashEntry<T> oldEntry = ref entries.StorageGetRef(i);
            if (oldEntry.Next >= -1)
            {
                long newBucketIndex = GetBucketIndex(oldEntry.HashCode, newBucketCount);
                ref int newBucket = ref newBuckets.StorageGetRef(newBucketIndex);
                
                ref HashEntry<T> newEntry = ref newEntries.StorageGetRef(newCount);
                newEntry.HashCode = oldEntry.HashCode;
                newEntry.Next = newBucket - 1;
                newEntry.Item = oldEntry.Item;
                newBucket = (int)newCount + 1;
                
                newCount++;
            }
        }

        buckets = newBuckets;
        entries = newEntries;
        bucketCount = newBucketCount;
        count = newCount;
        freeList = -1;
        freeCount = 0;
    }
}

/// <summary>
/// High-performance struct enumerator for separate chaining hash storage.
/// Only iterates over actual entries, skipping free list entries.
/// </summary>
internal struct HashStorageEnumerator<T> : IEnumerator<T>
{
    private readonly HashEntry<T>[][] _entries;
    private readonly long _totalEntries;
    private long _currentIndex;
    private T _current;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal HashStorageEnumerator(HashEntry<T>[][] entries, long count, int freeCount)
    {
        _entries = entries;
        _totalEntries = count + freeCount;
        _currentIndex = -1;
        _current = default!;
    }

    public readonly T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _current;
    }

    readonly object IEnumerator.Current => _current!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (++_currentIndex < _totalEntries)
        {
            ref HashEntry<T> entry = ref _entries.StorageGetRef(_currentIndex);
            if (entry.Next >= -1) // Not in free list
            {
                _current = entry.Item;
                return true;
            }
        }
        _current = default!;
        return false;
    }

    public void Reset() => throw new NotSupportedException();
    public readonly void Dispose() { }
}

/// <summary>
/// High-performance struct enumerable for separate chaining hash storage.
/// </summary>
internal readonly struct HashStorageEnumerable<T> : IEnumerable<T>
{
    private readonly HashEntry<T>[][] _entries;
    private readonly long _count;
    private readonly int _freeCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal HashStorageEnumerable(HashEntry<T>[][] entries, long count, int freeCount)
    {
        _entries = entries;
        _count = count;
        _freeCount = freeCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HashStorageEnumerator<T> GetEnumerator() => new(_entries, _count, _freeCount);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
