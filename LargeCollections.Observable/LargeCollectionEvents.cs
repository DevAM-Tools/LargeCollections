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
using System.Runtime.InteropServices;

namespace LargeCollections.Observable;

/// <summary>
/// Specifies the type of change that occurred in the collection.
/// </summary>
public enum LargeCollectionChangeAction : byte
{
    /// <summary>An item was added to the collection.</summary>
    Add = 0,
    /// <summary>An item was removed from the collection.</summary>
    Remove = 1,
    /// <summary>An item was replaced in the collection.</summary>
    Replace = 2,
    /// <summary>The collection was cleared.</summary>
    Clear = 3,
    /// <summary>The collection was reset (multiple changes or reordering).</summary>
    Reset = 4,
    /// <summary>A range of items was added to the collection.</summary>
    RangeAdd = 5
}

/// <summary>
/// High-performance struct-based event arguments that groups all collection change information.
/// Optimized for minimal memory footprint while providing all necessary information.
/// </summary>
/// <typeparam name="T">The type of item in the collection.</typeparam>
[StructLayout(LayoutKind.Auto)]
public readonly struct LargeCollectionChangedEventArgs<T>
{
    /// <summary>The type of change that occurred.</summary>
    public readonly LargeCollectionChangeAction Action;

    /// <summary>The index at which the change occurred.</summary>
    public readonly long Index;

    /// <summary>The number of items affected by the change.</summary>
    public readonly long Count;

    /// <summary>The item involved in the change (new item for Add/Replace, removed item for Remove).</summary>
    public readonly T Item;

    /// <summary>The old item (only for Replace operations).</summary>
    public readonly T OldItem;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LargeCollectionChangedEventArgs(
        LargeCollectionChangeAction action,
        long index,
        long count,
        T item,
        T oldItem)
    {
        Action = action;
        Index = index;
        Count = count;
        Item = item;
        OldItem = oldItem;
    }

    #region Factory Methods

    /// <summary>Creates event args for a single item Add operation.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeCollectionChangedEventArgs<T> ItemAdded(T item, long index)
        => new(LargeCollectionChangeAction.Add, index, 1, item, default);

    /// <summary>Creates event args for a single item Remove operation.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeCollectionChangedEventArgs<T> ItemRemoved(T item, long index)
        => new(LargeCollectionChangeAction.Remove, index, 1, item, default);

    /// <summary>Creates event args for a Replace operation.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeCollectionChangedEventArgs<T> ItemReplaced(T newItem, T oldItem, long index)
        => new(LargeCollectionChangeAction.Replace, index, 1, newItem, oldItem);

    /// <summary>Creates event args for a range Add operation.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeCollectionChangedEventArgs<T> RangeAdded(long startIndex, long addedCount)
        => new(LargeCollectionChangeAction.RangeAdd, startIndex, addedCount, default, default);

    /// <summary>Creates event args for a Clear operation.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeCollectionChangedEventArgs<T> Cleared(long previousCount)
        => new(LargeCollectionChangeAction.Clear, 0, previousCount, default, default);

    /// <summary>Creates event args for a Reset operation (multiple changes, sort, swap, etc.).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LargeCollectionChangedEventArgs<T> Reset()
        => new(LargeCollectionChangeAction.Reset, 0, 0, default, default);

    #endregion
}

/// <summary>
/// High-performance delegate for collection changed events.
/// Uses struct-based event args passed by reference to avoid allocations.
/// </summary>
/// <typeparam name="T">The type of item in the collection.</typeparam>
/// <param name="sender">The source of the event.</param>
/// <param name="e">The event arguments passed by reference for performance.</param>
public delegate void LargeCollectionChangedEventHandler<T>(IReadOnlyLargeObservableCollection<T> sender, in LargeCollectionChangedEventArgs<T> e);
