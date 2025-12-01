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
using System.Runtime.CompilerServices;

namespace LargeCollections;

#region High-Performance Comparer Structs

/// <summary>
/// Default comparer for types implementing <see cref="IComparable{T}"/>.
/// This struct implementation enables JIT devirtualization and inlining for optimal performance.
/// </summary>
/// <typeparam name="T">The type to compare. Must implement <see cref="IComparable{T}"/>.</typeparam>
public readonly struct DefaultComparer<T> : IComparer<T> where T : IComparable<T>
{
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(T left, T right)
    {
        if (left is null)
        {
            return right is null ? 0 : -1;
        }
        return left.CompareTo(right);
    }
}

/// <summary>
/// Descending comparer for types implementing <see cref="IComparable{T}"/>.
/// This struct implementation enables JIT devirtualization and inlining for optimal performance.
/// </summary>
/// <typeparam name="T">The type to compare. Must implement <see cref="IComparable{T}"/>.</typeparam>
public readonly struct DescendingComparer<T> : IComparer<T> where T : IComparable<T>
{
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(T left, T right)
    {
        if (right is null)
        {
            return left is null ? 0 : -1;
        }
        return right.CompareTo(left);
    }
}

/// <summary>
/// Default equality comparer for types implementing <see cref="IEquatable{T}"/>.
/// This struct implementation enables JIT devirtualization and inlining for optimal performance.
/// </summary>
/// <typeparam name="T">The type to compare. Must implement <see cref="IEquatable{T}"/>.</typeparam>
public readonly struct DefaultEqualityComparer<T> : IEqualityComparer<T> where T : IEquatable<T>
{
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(T left, T right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left.Equals(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(T item)
    {
        return item?.GetHashCode() ?? 0;
    }
}

/// <summary>
/// Default equality comparer for any type using <see cref="EqualityComparer{T}.Default"/>.
/// This struct implementation wraps the default equality comparer for types that may not implement <see cref="IEquatable{T}"/>.
/// For types implementing <see cref="IEquatable{T}"/>, prefer <see cref="DefaultEqualityComparer{T}"/> for better performance.
/// </summary>
/// <typeparam name="T">The type to compare.</typeparam>
public readonly struct ObjectEqualityComparer<T> : IEqualityComparer<T>
{
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(T left, T right)
        => EqualityComparer<T>.Default.Equals(left, right);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(T item)
        => item is not null ? EqualityComparer<T>.Default.GetHashCode(item) : 0;
}

/// <summary>
/// Wrapper that adapts a <see cref="Func{T, T, TResult}"/> delegate to the <see cref="IComparer{T}"/> interface.
/// Use this when you have an existing delegate but want to use the generic comparer overloads.
/// Note: This wrapper has the same performance as direct delegate usage.
/// </summary>
/// <typeparam name="T">The type to compare.</typeparam>
public readonly struct DelegateComparer<T> : IComparer<T>
{
    private readonly Func<T, T, int> _comparer;

    /// <summary>
    /// Creates a new delegate wrapper comparer.
    /// </summary>
    /// <param name="comparer">The comparison delegate to wrap.</param>
    public DelegateComparer(Func<T, T, int> comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(T left, T right) => _comparer(left, right);
}

/// <summary>
/// Wrapper that adapts <see cref="Func{T, T, TResult}"/> delegates to the <see cref="IEqualityComparer{T}"/> interface.
/// Use this when you have existing delegates but want to use the generic comparer overloads.
/// Note: This wrapper has the same performance as direct delegate usage.
/// </summary>
/// <typeparam name="T">The type to compare.</typeparam>
public readonly struct DelegateEqualityComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T, T, bool> _equals;
    private readonly Func<T, int> _hashCode;

    /// <summary>
    /// Creates a new delegate wrapper equality comparer.
    /// </summary>
    /// <param name="equals">The equality delegate to wrap.</param>
    /// <param name="hashCode">The hash code delegate to wrap.</param>
    public DelegateEqualityComparer(Func<T, T, bool> equals, Func<T, int> hashCode)
    {
        _equals = equals ?? throw new ArgumentNullException(nameof(equals));
        _hashCode = hashCode ?? throw new ArgumentNullException(nameof(hashCode));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(T left, T right) => _equals(left, right);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(T item) => _hashCode(item);
}

#endregion

#region High-Performance Action Interfaces

/// <summary>
/// High-performance action interface for iteration callbacks.
/// Using struct implementations enables JIT devirtualization and inlining for optimal performance.
/// Store any user data directly as fields in your struct implementation - the action is passed by ref
/// so state changes are preserved after iteration.
/// </summary>
/// <typeparam name="T">The type of items to process.</typeparam>
/// <example>
/// <code>
/// struct SumAction : ILargeAction&lt;long&gt;
/// {
///     public long Sum;  // User data stored directly in the struct
///     public void Invoke(long item) =&gt; Sum += item;
/// }
/// 
/// SumAction action = new SumAction();
/// array.DoForEach(ref action);  // Passed by ref - state is preserved
/// Console.WriteLine(action.Sum);
/// </code>
/// </example>
public interface ILargeAction<T>
{
    /// <summary>
    /// Executes the action on the specified item.
    /// </summary>
    /// <param name="item">The item to process.</param>
    void Invoke(T item);
}

/// <summary>
/// High-performance action interface for iteration callbacks with ref access to items.
/// Using struct implementations enables JIT devirtualization and inlining for optimal performance.
/// Store any user data directly as fields in your struct implementation - the action is passed by ref
/// so state changes are preserved after iteration.
/// </summary>
/// <typeparam name="T">The type of items to process.</typeparam>
/// <example>
/// <code>
/// struct IncrementAction : ILargeRefAction&lt;int&gt;
/// {
///     public int IncrementBy;
///     public int ModifiedCount;  // User data
///     public void Invoke(ref int item) { item += IncrementBy; ModifiedCount++; }
/// }
/// 
/// IncrementAction action = new IncrementAction { IncrementBy = 10 };
/// array.DoForEachRef(ref action);
/// Console.WriteLine($"Modified {action.ModifiedCount} items");
/// </code>
/// </example>
public interface ILargeRefAction<T>
{
    /// <summary>
    /// Executes the action on the specified item by reference.
    /// </summary>
    /// <param name="item">A reference to the item to process.</param>
    void Invoke(ref T item);
}

/// <summary>
/// Wrapper that adapts a delegate to the <see cref="ILargeAction{T}"/> interface.
/// </summary>
public readonly struct DelegateLargeAction<T> : ILargeAction<T>
{
    private readonly Action<T> _action;

    public DelegateLargeAction(Action<T> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invoke(T item) => _action(item);
}

#endregion

public interface IReadOnlyLargeCollection<T> : IEnumerable<T>
{
    /// <summary>
    /// Gets the number of items that are contained in the collection.
    /// The number of elements is limited to <see cref="Constants.MaxLargeCollectionCount"/>.
    /// </summary>
    long Count { get; }

    /// <summary>
    /// Determines whether the collection contains a specific <paramref name="item"/>.
    /// </summary>
    /// <param name="item">The item that shall be found.</param>
    /// <returns>true if the <paramref name="item"/> is present within the collection. Otherwise false is returned.</returns>
    bool Contains(T item);

    /// <summary>
    /// Returns all items of the collection as an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <returns><see cref="IEnumerable{T}"/></returns>
    IEnumerable<T> GetAll();

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection.
    /// Depending on the actual collection implementation (i.e. <see cref="LargeArray{T}"/>) this may be significantly faster than iterating over all elements in a foreach-loop.
    /// </summary>
    /// <param name="action">The function that will be called for each item of the collection.</param>
    void DoForEach(Action<T> action);

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection using an action type for optimal performance.
    /// Using struct implementations enables JIT devirtualization and inlining.
    /// Store any user data directly as fields in your type - the action is passed by ref so state changes are preserved.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeAction{T}"/>. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<T>;
}

public interface ILargeCollection<T> : IReadOnlyLargeCollection<T>
{
    /// <summary>
    /// Adds an <paramref name="item"/> to the collection.
    /// Depending on the actual collection implementation exisitng items may be replaced.
    /// </summary>
    /// <param name="item">The item <paramref name="item"/> shall be added to the collection.</param>
    void Add(T item);

    /// <summary>
    /// Adds multiple <paramref name="items"/> to the collection.
    /// Depending on the actual collection implementation exisitng items may be replaced.
    /// </summary>
    /// <param name="items">An enumeration of items that shall be added to the collection.</param>
    void AddRange(IEnumerable<T> items);

    /// <summary>
    /// Adds multiple <paramref name="items"/> to the collection.
    /// Depending on the actual collection implementation exisitng items may be replaced.
    /// This overload is inteded to prevent boxing for ReadOnlyLargeSpans.
    /// </summary>
    /// <param name="items">A span of items that shall be added to the collection.</param>
    void AddRange(ReadOnlyLargeSpan<T> items);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    /// <summary>
    /// Adds multiple <paramref name="items"/> to the collection.
    /// Depending on the actual collection implementation exisitng items may be replaced.
    ///  This overload is inteded to prevent boxing for ReadOnlySpan.
    /// </summary>
    /// <param name="items">A span of items that shall be added to the collection.</param>
    void AddRange(ReadOnlySpan<T> items);
#endif

    /// <summary>
    /// Removes the first occurance of an <paramref name="item"/> from the collection.
    /// </summary>
    /// <param name="item">The <paramref name="item"/> that shall be removed from the collection.</param>
    /// <returns>true if the <paramref name="item"/> was found and removed. Otherwise false is returned.</returns>
    bool Remove(T item);

    /// <summary>
    /// Removes the first occurance of an <paramref name="item"/> from the collection.
    /// </summary>
    /// <param name="item">The <paramref name="item"/> that shall be removed from the collection.</param>
    /// <param name="removedItem">The removed item will be assigned if the <paramref name="item"/> was found. Otherwise the <see cref="default(T)"/> will be assigned.</param>
    /// <returns>true if the <paramref name="item"/> was found and removed. Otherwise false is returned.</returns>
    bool Remove(T item, out T removedItem);

    /// <summary>
    /// Removes all items from the collection. Resets <see cref="IReadOnlyCollection{T}.Count"/> to 0;
    /// </summary>
    void Clear();
}

public interface IReadOnlyLargeArray<T> : IReadOnlyLargeCollection<T>
{
    /// <summary>
    /// Gets the item at the specified 0-based <paramref name="index"/> if <paramref name="index"/> is within the valid range.
    /// </summary>
    /// <param name="index">The 0-based <paramref name="index"/> of item that shall be accessed.</param>
    /// <returns>The item which is located at the specified 0-based <paramref name="index"/> if <paramref name="index"/> was within the valid range.</returns>
    T this[long index] { get; }

    /// <summary>
    /// Gets the item at the specified 0-based <paramref name="index"/> if <paramref name="index"/> is within the valid range.
    /// </summary>
    /// <param name="index">The 0-based <paramref name="index"/> of item that shall be accessed.</param>
    /// <returns>The item which is located at the specified 0-based <paramref name="index"/> if <paramref name="index"/> was within the valid range.</returns>
    T Get(long index);

    /// <summary>
    /// Performs a binary search using the default comparer.
    /// The collection must be sorted in ascending order.
    /// </summary>
    /// <param name="item">The item whose location shall be found.</param>
    /// <param name="offset">Optional starting offset. If null, starts from 0.</param>
    /// <param name="count">Optional number of elements to search. If null, searches all remaining elements.</param>
    /// <returns>The 0-based index of the item if found; otherwise, a negative number.</returns>
    long BinarySearch(T item, long? offset = null, long? count = null);

    /// <summary>
    /// Performs a binary search using a generic comparer for optimal performance through JIT devirtualization.
    /// The collection must be sorted in ascending order according to <paramref name="comparer"/>.
    /// </summary>
    /// <typeparam name="TComparer">A type implementing <see cref="IComparer{T}"/>.</typeparam>
    /// <param name="item">The item whose location shall be found.</param>
    /// <param name="comparer">The comparer instance.</param>
    /// <param name="offset">Optional starting offset. If null, starts from 0.</param>
    /// <param name="count">Optional number of elements to search. If null, searches all remaining elements.</param>
    /// <returns>The 0-based index of the item if found; otherwise, a negative number.</returns>
    long BinarySearch<TComparer>(T item, TComparer comparer, long? offset = null, long? count = null) where TComparer : IComparer<T>;

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection within the specified range.
    /// Depending on the actual collection implementation (i.e. <see cref="LargeArray{T}"/>) this may be significantly faster than iterating over all elements in a foreach-loop.
    /// </summary>
    /// <param name="action">The function that will be called for each item of the collection.</param>
    /// <param name="offset">Starting offset.</param>
    /// <param name="count">Number of elements to process.</param>
    void DoForEach(Action<T> action, long offset, long count);

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection using an action type for optimal performance.
    /// Using struct implementations enables JIT devirtualization and inlining.
    /// Store any user data directly as fields in your type - the action is passed by ref so state changes are preserved.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeAction{T}"/>. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    /// <param name="offset">Starting offset.</param>
    /// <param name="count">Number of elements to process.</param>
    void DoForEach<TAction>(ref TAction action, long offset, long count) where TAction : ILargeAction<T>;

    /// <summary>
    /// Returns all items of the collection as an <see cref="IEnumerable{T}"/> within the given range defined by <paramref name="offset"/> and <paramref name="count"/>.
    /// </summary>
    /// <param name="offset">The <paramref name="offset"/> where the range starts.</param>
    /// <param name="count">The <paramref name="count"/> of elements that belong to the range.</param>
    /// <returns><see cref="IEnumerable{T}"/></returns>
    IEnumerable<T> GetAll(long offset, long count);

    /// <summary>
    /// Finds the index of the first occurance of an <paramref name="item"/> within the collection.
    /// </summary>
    /// <param name="item">The item to find.</param>
    /// <param name="offset">Optional starting offset. If null, starts from 0.</param>
    /// <param name="count">Optional number of elements to search. If null, searches all remaining elements.</param>
    /// <returns>The 0-based index of the item if it was found; otherwise, -1.</returns>
    long IndexOf(T item, long? offset = null, long? count = null);

    /// <summary>
    /// Finds the index of the first occurrence of an item using a generic equality comparer for optimal performance.
    /// </summary>
    /// <typeparam name="TComparer">A type implementing <see cref="IEqualityComparer{T}"/>.</typeparam>
    /// <param name="item">The item to find.</param>
    /// <param name="comparer">The comparer instance passed by reference.</param>
    /// <param name="offset">Optional starting offset. If null, starts from 0.</param>
    /// <param name="count">Optional number of elements to search. If null, searches all remaining elements.</param>
    /// <returns>The 0-based index of the item if found; otherwise, -1.</returns>
    long IndexOf<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>;

    /// <summary>
    /// Finds the index of the last occurance of an <paramref name="item"/> within the collection.
    /// </summary>
    /// <param name="item">The item to find.</param>
    /// <param name="offset">Optional starting offset. If null, starts from 0.</param>
    /// <param name="count">Optional number of elements to search. If null, searches all remaining elements.</param>
    /// <returns>The 0-based index of the item if it was found; otherwise, -1.</returns>
    long LastIndexOf(T item, long? offset = null, long? count = null);

    /// <summary>
    /// Finds the index of the last occurrence of an item using a generic equality comparer for optimal performance.
    /// </summary>
    /// <typeparam name="TComparer">A type implementing <see cref="IEqualityComparer{T}"/>.</typeparam>
    /// <param name="item">The item to find.</param>
    /// <param name="comparer">The comparer instance passed by reference.</param>
    /// <param name="offset">Optional starting offset. If null, starts from 0.</param>
    /// <param name="count">Optional number of elements to search. If null, searches all remaining elements.</param>
    /// <returns>The 0-based index of the item if found; otherwise, -1.</returns>
    long LastIndexOf<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>;

    /// <summary>
    /// Determines whether the collection contains a specific <paramref name="item"/> within the specified range.
    /// </summary>
    /// <param name="item">The item that shall be found.</param>
    /// <param name="offset">Starting offset.</param>
    /// <param name="count">Number of elements to search.</param>
    /// <returns>true if the <paramref name="item"/> is present within the collection. Otherwise false is returned.</returns>
    bool Contains(T item, long offset, long count);

    /// <summary>
    /// Determines whether the collection contains a specific item using a generic equality comparer for optimal performance.
    /// </summary>
    /// <typeparam name="TComparer">A type implementing <see cref="IEqualityComparer{T}"/>.</typeparam>
    /// <param name="item">The item to find.</param>
    /// <param name="comparer">The comparer instance passed by reference.</param>
    /// <param name="offset">Optional starting offset. If null, starts from 0.</param>
    /// <param name="count">Optional number of elements to search. If null, searches all remaining elements.</param>
    /// <returns>true if the item is present; otherwise, false.</returns>
    bool Contains<TComparer>(T item, ref TComparer comparer, long? offset = null, long? count = null) where TComparer : IEqualityComparer<T>;

    /// <summary>
    /// Copies <paramref name="count"/> items to the <paramref name="target"/> at <paramref name="targetOffset"/> from this collection at <paramref name="sourceOffset"/>.
    /// </summary>
    /// <param name="target">The target where the items will be copied to.</param>
    /// <param name="sourceOffset">The offset where the first item will be copied from.</param>
    /// <param name="targetOffset">The offset where the first item will be copied to.</param>
    /// <param name="count">The number of items that will be copied.</param>
    void CopyTo(ILargeArray<T> target, long sourceOffset, long targetOffset, long count);

    /// <summary>
    /// Copies <paramref name="count"/> items to the <paramref name="target"/> from this collection at <paramref name="sourceOffset"/>.
    /// This overload is intended to prevent boxing for LargeSpans.
    /// </summary>
    /// <param name="target">The target where the items will be copied to.</param>
    /// <param name="sourceOffset">The offset where the first item will be copied from.</param>
    /// <param name="count">The number of items that will be copied.</param>
    void CopyTo(LargeSpan<T> target, long sourceOffset, long count);

    /// <summary>
    /// Copies <paramref name="count"/> items to the <paramref name="target"/> from this collection at <paramref name="sourceOffset"/>.
    /// </summary>
    /// <param name="target">The target where the items will be copied to.</param>
    /// <param name="sourceOffset">The offset where the first item will be copied from.</param>
    /// <param name="targetOffset">The offset where the first item will be copied to.</param>
    /// <param name="count">The number of items that will be copied.</param>
    void CopyToArray(T[] target, long sourceOffset, int targetOffset, int count);

# if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    /// <summary>
    /// Copies <paramref name="count"/> items to the <paramref name="target"/> from this collection at <paramref name="sourceOffset"/>.
    /// This overload is intended to prevent boxing for Spans.
    /// </summary>
    /// <param name="target">The target where the items will be copied to.</param>
    /// <param name="sourceOffset">The offset where the first item will be copied from.</param>
    /// <param name="count">The number of items that will be copied.</param>
    void CopyToSpan(Span<T> target, long sourceOffset, int count);
#endif
}

public interface ILargeArray<T> : IReadOnlyLargeArray<T>
{
    /// <summary>
    /// Gets or stores the item at the specified 0-based <paramref name="index"/> if <paramref name="index"/> is within the valid range.
    /// </summary>
    /// <param name="index">The 0-based <paramref name="index"/> of the location where the item shall be stored or got from.</param>
    /// <returns>The item which is located at the specified 0-based <paramref name="index"/> if <paramref name="index"/> was within the valid range.</returns>
    new T this[long index] { get; set; }

    /// <summary>
    /// Stores the item at the specified 0-based <paramref name="index"/> if <paramref name="index"/> is within the valid range.
    /// </summary>
    /// <param name="index">The 0-based <paramref name="index"/> of the location where the item shall be stored.</param>
    /// <param name="item">The <paramref name="item"/> that shall be stored at the location of the specified 0-based <paramref name="index"/>.</param>
    void Set(long index, T item);

    /// <summary>
    /// Reorders the items of the collection in ascending order using a generic comparer for optimal performance.
    /// </summary>
    /// <typeparam name="TComparer">A type implementing <see cref="IComparer{T}"/>.</typeparam>
    /// <param name="comparer">The comparer instance.</param>
    /// <param name="offset">Optional starting offset. If null, starts from 0.</param>
    /// <param name="count">Optional number of elements to sort. If null, sorts all remaining elements.</param>
    void Sort<TComparer>(TComparer comparer, long? offset = null, long? count = null) where TComparer : IComparer<T>;

    /// <summary>
    /// Swaps the the item at index <paramref name="leftIndex"/> with the item at <paramref name="rightIndex"/>.
    /// </summary>
    /// <param name="leftIndex">The index of the first item.</param>
    /// <param name="rightIndex">The index of the second item.</param>
    void Swap(long leftIndex, long rightIndex);

    /// <summary>
    /// Copies <paramref name="count"/> items from the <paramref name="source"/> to this collection at <paramref name="targetOffset"/>.
    /// </summary>
    /// <param name="source">The source where the items will be copied from.</param>
    /// <param name="sourceOffset">The offset where the first item will be copied from.</param>
    /// <param name="targetOffset">The offset where the first item will be copied to.</param>
    /// <param name="count">The number of items that will be copied.</param>
    void CopyFrom(IReadOnlyLargeArray<T> source, long sourceOffset, long targetOffset, long count);

    /// <summary>
    /// Copies <paramref name="count"/> items from the <paramref name="source"/> to this collection at <paramref name="targetOffset"/>.
    /// This overload is intended to prevent boxing for ReadOnlyLargeSpan.
    /// </summary>
    /// <param name="source">The source where the items will be copied from.</param>
    /// <param name="targetOffset">The offset where the first item will be copied to.</param>
    /// <param name="count">The number of items that will be copied.</param>
    void CopyFrom(ReadOnlyLargeSpan<T> source, long targetOffset, long count);

    /// <summary>
    /// Copies <paramref name="count"/> items from the <paramref name="source"/> to this collection at <paramref name="targetOffset"/>.
    /// </summary>
    /// <param name="source">The source where the items will be copied from.</param>
    /// <param name="sourceOffset">The offset where the first item will be copied from.</param>
    /// <param name="targetOffset">The offset where the first item will be copied to.</param>
    /// <param name="count">The number of items that will be copied.</param>
    void CopyFromArray(T[] source, int sourceOffset, long targetOffset, int count);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    /// <summary>
    /// Copies <paramref name="count"/> items from the <paramref name="source"/> to this collection at <paramref name="targetOffset"/>.
    /// This overload is intended to prevent boxing for ReadOnlySpan.
    /// </summary>
    /// <param name="source">The source where the items will be copied from.</param>
    /// <param name="targetOffset">The offset where the first item will be copied to.</param>
    /// <param name="count">The number of items that will be copied.</param>
    void CopyFromSpan(ReadOnlySpan<T> source, long targetOffset, int count);
#endif
}

public interface IRefAccessLargeArray<T> : ILargeArray<T>
{
    /// <summary>
    /// Gets a reference to the item at the specified 0-based <paramref name="index"/> if <paramref name="index"/> is within the valid range.
    /// The reference can be used to read or modify the item directly.
    /// </summary>
    /// <param name="index">The 0-based <paramref name="index"/> of the location where the item shall be accessed.</param>
    /// <returns>A reference to the item which is located at the specified 0-based <paramref name="index"/> if <paramref name="index"/> was within the valid range.</returns>
    new ref T this[long index] { get; }

    /// <summary>
    /// Gets a reference to the item at the specified 0-based <paramref name="index"/> if <paramref name="index"/> is within the valid range.
    /// </summary>
    /// <param name="index">The 0-based <paramref name="index"/> of the location where the item shall be got from.</param>
    /// <returns>A reference to the item which is located at the specified 0-based <paramref name="index"/> if <paramref name="index"/> was within the valid range.</returns>
    ref T GetRef(long index);

    /// <summary>
    /// Performs the <paramref name="action"/> with items by reference of the collection using a action passed by reference for optimal performance.
    /// Store any user data directly as fields in your action - the action is passed by ref so state changes are preserved.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeRefAction{T}"/>.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    /// <param name="offset">Optional starting offset. If null, starts from 0.</param>
    /// <param name="count">Optional number of elements to process. If null, processes all remaining elements.</param>
    void DoForEachRef<TAction>(ref TAction action, long? offset = null, long? count = null) where TAction : ILargeRefAction<T>;
}

public interface ILargeList<T> : ILargeArray<T>, ILargeCollection<T>
{
    /// <summary>
    /// Removes the first occurance of an <paramref name="item"/> from the collection.
    /// </summary>
    /// <param name="item">The <paramref name="item"/> that shall be removed from the collection.</param>
    /// <param name="removedItem">The removed item will be assigned if the <paramref name="item"/> was found. Otherwise the <see cref="default(T)"/> will be assigned.</param>
    /// <param name="preserveOrder">If set to false the order of items may change but the operation will be faster. Defaults to true.</param>
    /// <returns>true if the <paramref name="item"/> was found and removed. Otherwise false is returned.</returns>
    bool Remove(T item, out T removedItem, bool preserveOrder = true);

    /// <summary>
    /// Removes the first occurance of an <paramref name="item"/> from the collection using a custom equality comparer.
    /// </summary>
    /// <typeparam name="TComparer">The type of the equality comparer.</typeparam>
    /// <param name="item">The <paramref name="item"/> that shall be removed from the collection.</param>
    /// <param name="removedItem">The removed item will be assigned if the <paramref name="item"/> was found. Otherwise the <see cref="default(T)"/> will be assigned.</param>
    /// <param name="comparer">The equality comparer used to find the item.</param>
    /// <param name="preserveOrder">If set to false the order of items may change but the operation will be faster. Defaults to true.</param>
    /// <returns>true if the <paramref name="item"/> was found and removed. Otherwise false is returned.</returns>
    bool Remove<TComparer>(T item, out T removedItem, TComparer comparer, bool preserveOrder = true) where TComparer : IEqualityComparer<T>;

    /// <summary>
    /// Removes the item at the specified 0-based <paramref name="index"/> if <paramref name="index"/> is within the valid range.
    /// </summary>
    /// <param name="index">The 0-based <paramref name="index"/> of the location where the item shall be removed.</param>
    /// <param name="preserveOrder">If set to false the order of items may change but the operation will be faster. Defaults to true.</param>
    /// <returns>The item which was located at the specified 0-based <paramref name="index"/> if <paramref name="index"/> was within the valid range.</returns>
    T RemoveAt(long index, bool preserveOrder = true);
}

public interface IRefAccessLargeList<T> : ILargeList<T>, IRefAccessLargeArray<T>;

public interface IReadOnlyLargeDictionary<TKey, TValue> : IReadOnlyLargeCollection<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    /// <summary>
    /// Gets  the value that is associated with the specified <paramref name="key"/> that uniquely identifies the item.
    /// </summary>
    /// <param name="key">The <paramref name="key"/> that uniquely identifies the item.</param>
    /// <returns>The value that is associated with the specified <paramref name="key"/>.</returns>
    TValue this[TKey key] { get; }

    /// <summary>
    /// Gets the value that is associated with the specified <paramref name="key"/> that uniquely identifies the item.
    /// If the specified <paramref name="key"/> could not be found an <see cref="KeyNotFoundException"/> will be thrown.
    /// </summary>
    /// <param name="key">The <paramref name="key"/> that uniquely identifies the item.</param>
    /// <returns>The value that is associated with the specified <paramref name="key"/>.</returns>
    TValue Get(TKey key);

    /// <summary>
    /// An enumeration of all keys that are used to uniquely identify the items of the collection.
    /// </summary>
    IEnumerable<TKey> Keys { get; }

    /// <summary>
    /// An enumeration of all item values that are stored in the collection.
    /// </summary>
    IEnumerable<TValue> Values { get; }

    /// <summary>
    /// Checks if the specified <paramref name="key"/> is used in the collection to uniquely identify a stored item.
    /// </summary>
    /// <param name="key">The <paramref name="key"/> that uniquely identifies the item.</param>
    /// <returns>true if the specified <paramref name="key"/> is present within the collection. Otherwise false is returned.</returns>
    bool ContainsKey(TKey key);

    /// <summary>
    /// Gets the value that is associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The <paramref name="key"/> that uniquely identifies the item.</param>
    /// <param name="value">The item value will be assigned if the specified <paramref name="key"/> was found.
    /// Otherwise the <see cref="default(T)"/> will be assigned.</param>
    /// <returns>true if the specified <paramref name="key"/> is present within the collection. Otherwise false is returned.</returns>
    bool TryGetValue(TKey key, out TValue value);

}

public interface ILargeDictionary<TKey, TValue> : IReadOnlyLargeDictionary<TKey, TValue>, ILargeCollection<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    /// <summary>
    /// Gets or stores the value that is or will be associated with the specified <paramref name="key"/> that uniquely identifies the item.
    /// In case of get: If the specified <paramref name="key"/> could not be found an <see cref="KeyNotFoundException"/> will be thrown.
    /// In case of set: An existing item with the same <paramref name="key"/> will be replaced.
    /// </summary>
    /// <param name="key">The <paramref name="key"/> that uniquely identifies the item.</param>
    /// <returns>The value that is associated with the specified <paramref name="key"/>.</returns>
    new TValue this[TKey key] { get; set; }

    /// <summary>
    /// Stores the <paramref name="value"/> that will be associated with the specified <paramref name="key"/> that uniquely identifies the item.
    /// An existing item with the same <paramref name="key"/> will be replaced.
    /// </summary>
    /// <param name="key">The <paramref name="key"/> that uniquely identifies the item.</param>
    void Set(TKey key, TValue value);

    /// <summary>
    /// Removes the value that is associated with the specified <paramref name="key"/> that uniquely identifies the item.
    /// </summary>
    /// <param name="key">The <paramref name="key"/> that uniquely identifies the item.</param>
    /// <returns>true if the item was found and removed. Otherwise false is returned.</returns>
    bool Remove(TKey key);

    /// <summary>
    /// Removes the value that is associated with the specified <paramref name="key"/> that uniquely identifies the item.
    /// </summary>
    /// <param name="key">The <paramref name="key"/> that uniquely identifies the item.</param>
    /// <param name="removedValue">The removed value will be assigned if the item was found. Otherwise the <see cref="default(T)"/> will be assigned.</param>
    /// <returns>true if the item was found and removed. Otherwise false is returned.</returns>
    bool Remove(TKey key, out TValue removedValue);
}


