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

namespace LargeCollections;

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
    /// <param name="action">The function that that will be called for each item of the collection.</param>
    void DoForEach(Action<T> action);

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection.
    /// It allows to pass user data to the action to prevent capturing variables.
    /// Depending on the actual collection implementation (i.e. <see cref="LargeArray{T}"/>) this may be significantly faster than iterating over all elements in a foreach-loop.
    /// </summary>
    /// <param name="action">The function that that will be called for each item of the collection.</param>
    /// <param name="userData">The user data that will be passed to the action.</param>
    void DoForEach<TUserData>(ActionWithUserData<T, TUserData> action, ref TUserData userData);
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

#if NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// Adds multiple <paramref name="items"/> to the collection.
    /// Depending on the actual collection implementation exisitng items may be replaced.
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
    /// Performs a binary search to find the index at which the <paramref name="item"/> is located.
    /// The collection must be sorted in ascending order according to <paramref name="comparer"/>. Otherwise you will get undefined results.
    /// </summary>
    /// <param name="item">The <paramref name="item"/> whose location shall be found.</param>
    /// <param name="comparer">The <paramref name="comparer"/> function will be used to compare the items of the collection.</param>
    /// <returns>The 0-based index of the <paramref name="item"/> if it was found.
    /// If the <paramref name="item"/> could not be found a negative number will be returned.
    /// If the specified <paramref name="item"/> is not unique within the collection only one of the potential candidates will be returned.</returns>
    long BinarySearch(T item, Func<T, T, int> comparer);

    /// <summary>
    /// Performs a binary search to find the index at which the <paramref name="item"/> is located within the given range defined by <paramref name="offset"/> and <paramref name="count"/>.
    /// The collection must be sorted in ascending order according to <paramref name="comparer"/>. Otherwise you will get undefined results.
    /// </summary>
    /// <param name="item">The <paramref name="item"/> whose location shall be found.</param>
    /// <param name="comparer">The <paramref name="comparer"/> function will be used to compare the items of the collection.</param>
    /// <param name="offset">The <paramref name="offset"/> where the range starts.</param>
    /// <param name="count">The <paramref name="count"/> of elements that belong to the range.</param>
    /// <returns>The 0-based index of the <paramref name="item"/> if it was found.
    /// If the <paramref name="item"/> could not be found a negative number will be returned.
    /// If the specified <paramref name="item"/> is not unique within the collection only one of the potential candidates will be returned.</returns>
    long BinarySearch(T item, Func<T, T, int> comparer, long offset, long count);

    /// <summary>
    /// Returns all items of the collection as an <see cref="IEnumerable{T}"/> within the given range defined by <paramref name="offset"/> and <paramref name="count"/>.
    /// </summary>
    /// <param name="offset">The <paramref name="offset"/> where the range starts.</param>
    /// <param name="count">The <paramref name="count"/> of elements that belong to the range.</param>
    /// <returns><see cref="IEnumerable{T}"/></returns>
    IEnumerable<T> GetAll(long offset, long count);

    /// <summary>
    /// Determines whether the collection contains a specific <paramref name="item"/> within the given range defined by <paramref name="offset"/> and <paramref name="count"/>.
    /// </summary>
    /// <param name="item">The item that shall be found.</param>
    /// <param name="offset">The <paramref name="offset"/> where the range starts.</param>
    /// <param name="count">The <paramref name="count"/> of elements that belong to the range.</param>
    /// <returns>true if the <paramref name="item"/> is present within the collection. Otherwise false is returned.</returns>
    bool Contains(T item, long offset, long count);

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection within the given range defined by <paramref name="offset"/> and <paramref name="count"/>.
    /// Depending on the actual collection implementation (i.e. <see cref="LargeArray{T}"/>) this may be significantly faster than iterating over all elements in a foreach-loop.
    /// </summary>
    /// <param name="action">The function that that will be called for each item of the collection.</param>
    /// <param name="offset">The <paramref name="offset"/> where the range starts.</param>
    /// <param name="count">The <paramref name="count"/> of elements that belong to the range.</param>
    void DoForEach(Action<T> action, long offset, long count);

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection.
    /// It allows to pass user data to the action to prevent capturing variables.
    /// Depending on the actual collection implementation (i.e. <see cref="LargeArray{T}"/>) this may be significantly faster than iterating over all elements in a foreach-loop.
    /// </summary>
    /// <param name="action">The function that that will be called for each item of the collection.</param>
    /// <param name="offset">The <paramref name="offset"/> where the range starts.</param>
    /// <param name="count">The <paramref name="count"/> of elements that belong to the range.</param>
    /// <param name="userData">The user data that will be passed to the action.</param>
    void DoForEach<TUserData>(ActionWithUserData<T, TUserData> action, long offset, long count, ref TUserData userData);

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
    /// </summary>
    /// <param name="target">The target where the items will be copied to.</param>
    /// <param name="sourceOffset">The offset where the first item will be copied from.</param>
    /// <param name="targetOffset">The offset where the first item will be copied to.</param>
    /// <param name="count">The number of items that will be copied.</param>
    void CopyToArray(T[] target, long sourceOffset, int targetOffset, int count);

# if NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// Copies <paramref name="count"/> items to the <paramref name="target"/> from this collection at <paramref name="sourceOffset"/>.
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
    /// Reorders the items of the collection in ascending order according to <paramref name="comparer"/>.
    /// </summary>
    /// <param name="comparer">The <paramref name="comparer"/> function will be used to compare the items of the collection.</param>
    void Sort(Func<T, T, int> comparer);

    /// <summary>
    /// Reorders the items of the collection within the given range defined by <paramref name="offset"/> and <paramref name="count"/> in ascending order according to <paramref name="comparer"/>.
    /// </summary>
    /// <param name="comparer">The <paramref name="comparer"/> function will be used to compare the items of the collection.</param>
    /// <param name="offset">The <paramref name="offset"/> where the range starts.</param>
    /// <param name="count">The <paramref name="count"/> of elements that belong to the range.</param>
    void Sort(Func<T, T, int> comparer, long offset, long count);

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
    /// </summary>
    /// <param name="source">The source where the items will be copied from.</param>
    /// <param name="sourceOffset">The offset where the first item will be copied from.</param>
    /// <param name="targetOffset">The offset where the first item will be copied to.</param>
    /// <param name="count">The number of items that will be copied.</param>
    void CopyFromArray(T[] source, int sourceOffset, long targetOffset, int count);

#if NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// Copies <paramref name="count"/> items from the <paramref name="source"/> to this collection at <paramref name="targetOffset"/>.
    /// </summary>
    /// <param name="source">The source where the items will be copied from.</param>
    /// <param name="sourceOffset">The offset where the first item will be copied from.</param>
    /// <param name="targetOffset">The offset where the first item will be copied to.</param>
    /// <param name="count">The number of items that will be copied.</param>
    void CopyFromSpan(ReadOnlySpan<T> source, long targetOffset, int count);
#endif
}

public interface IRefAccessLargeArray<T> : ILargeArray<T>
{
    /// <summary>
    /// Gets a reference to the item at the specified 0-based <paramref name="index"/> if <paramref name="index"/> is within the valid range.
    /// </summary>
    /// <param name="index">The 0-based <paramref name="index"/> of the location where the item shall be got from.</param>
    /// <returns>A reference to the item which is located at the specified 0-based <paramref name="index"/> if <paramref name="index"/> was within the valid range.</returns>
    ref T GetRef(long index);

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection.
    /// Depending on the actual collection implementation (i.e. <see cref="LargeArray{T}"/>) this may be significantly faster than iterating over all elements in a foreach-loop.
    /// </summary>
    /// <param name="action">The function that that will be called for each item of the collection.</param>
    void DoForEach(RefAction<T> action);

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection within the given range defined by <paramref name="offset"/> and <paramref name="count"/>.
    /// Depending on the actual collection implementation (i.e. <see cref="LargeArray{T}"/>) this may be significantly faster than iterating over all elements in a foreach-loop.
    /// </summary>
    /// <param name="action">The function that that will be called for each item of the collection.</param>
    /// <param name="offset">The <paramref name="offset"/> where the range starts.</param>
    /// <param name="count">The <paramref name="count"/> of elements that belong to the range.</param>
    void DoForEach(RefAction<T> action, long offset, long count);

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection.
    /// It allows to pass user data to the action to prevent capturing variables.
    /// Depending on the actual collection implementation (i.e. <see cref="LargeArray{T}"/>) this may be significantly faster than iterating over all elements in a foreach-loop.
    /// </summary>
    /// <param name="action">The function that that will be called for each item of the collection.</param>
    /// <param name="userData">The user data that will be passed to the action.</param>
    void DoForEach<TUserData>(RefActionWithUserData<T, TUserData> action, ref TUserData userData);

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the collection within the given range defined by <paramref name="offset"/> and <paramref name="count"/>.
    /// It allows to pass user data to the action to prevent capturing variables.
    /// Depending on the actual collection implementation (i.e. <see cref="LargeArray{T}"/>) this may be significantly faster than iterating over all elements in a foreach-loop.
    /// </summary>
    /// <param name="action">The function that that will be called for each item of the collection.</param>
    /// <param name="userData">The user data that will be passed to the action.</param>
    /// <param name="offset">The <paramref name="offset"/> where the range starts.</param>
    /// <param name="count">The <paramref name="count"/> of elements that belong to the range.</param>
    void DoForEach<TUserData>(RefActionWithUserData<T, TUserData> action, long offset, long count, ref TUserData userData);
}

public interface ILargeList<T> : ILargeArray<T>, ILargeCollection<T>
{
    /// <summary>
    /// Removes the first occurance of an <paramref name="item"/> from the collection.
    /// </summary>
    /// <param name="item">The <paramref name="item"/> that shall be removed from the collection.</param>
    /// <param name="preserveOrder">If set to false the order of items may change but the operation will be faster.</param>
    /// <returns>true if the <paramref name="item"/> was found and removed. Otherwise false is returned.</returns>
    bool Remove(T item, bool preserveOrder);

    /// <summary>
    /// Removes the first occurance of an <paramref name="item"/> from the collection.
    /// </summary>
    /// <param name="item">The <paramref name="item"/> that shall be removed from the collection.</param>
    /// <param name="preserveOrder">If set to false the order of items may change but the operation will be faster.</param>
    /// <param name="removedItem">The removed item will be assigned if the <paramref name="item"/> was found. Otherwise the <see cref="default(T)"/> will be assigned.</param>
    /// <returns>true if the <paramref name="item"/> was found and removed. Otherwise false is returned.</returns>
    bool Remove(T item, bool preserveOrder, out T removedItem);

    /// <summary>
    /// Removes the item at the specified 0-based <paramref name="index"/> if <paramref name="index"/> is within the valid range.
    /// </summary>
    /// <param name="index">The 0-based <paramref name="index"/> of the location where the item shall be removed.</param>
    /// <returns>The item which was located at the specified 0-based <paramref name="index"/> if <paramref name="index"/> was within the valid range.</returns>
    T RemoveAt(long index);

    /// <summary>
    /// Removes the item at the specified 0-based <paramref name="index"/> if <paramref name="index"/> is within the valid range.
    /// </summary>
    /// <param name="index">The 0-based <paramref name="index"/> of the location where the item shall be removed.</param>
    /// <param name="preserveOrder">If set to false the order of items may change but the operation will be faster.</param>
    /// <returns>The item which was located at the specified 0-based <paramref name="index"/> if <paramref name="index"/> was within the valid range.</returns>
    T RemoveAt(long index, bool preserveOrder);
}

public interface IRefAccessLargeList<T> : ILargeList<T>, IRefAccessLargeArray<T>
{

}

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


