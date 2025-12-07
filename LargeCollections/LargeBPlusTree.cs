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

namespace LargeCollections;

#region B+Tree Node Classes

/// <summary>
/// Base class for B+Tree nodes.
/// </summary>
/// <typeparam name="TKey">The type of keys in the tree.</typeparam>
/// <typeparam name="TValue">The type of values in the tree.</typeparam>
internal abstract class BPlusTreeNode<TKey, TValue>
{
    /// <summary>
    /// The keys stored in this node.
    /// </summary>
    public TKey[] Keys;

    /// <summary>
    /// The number of keys currently stored in this node.
    /// </summary>
    public int KeyCount;

    /// <summary>
    /// Parent node pointer for O(1) parent lookup.
    /// </summary>
    public BPlusTreeInternalNode<TKey, TValue> Parent;

    /// <summary>
    /// Gets whether this node is a leaf node.
    /// </summary>
    public abstract bool IsLeaf { get; }

    /// <summary>
    /// Creates a new node with the specified order.
    /// </summary>
    /// <param name="order">The maximum number of children for internal nodes.</param>
    protected BPlusTreeNode(int order)
    {
        // Internal nodes can have up to order children and order-1 keys
        // Leaf nodes can have up to order-1 key-value pairs
        // We allocate order keys to handle temporary overflow during splits
        Keys = new TKey[order];
        KeyCount = 0;
        Parent = null;
    }
}

/// <summary>
/// Internal (non-leaf) node in a B+Tree.
/// </summary>
/// <typeparam name="TKey">The type of keys in the tree.</typeparam>
/// <typeparam name="TValue">The type of values in the tree.</typeparam>
internal sealed class BPlusTreeInternalNode<TKey, TValue> : BPlusTreeNode<TKey, TValue>
{
    /// <summary>
    /// The child pointers. Children[i] points to subtree with keys less than Keys[i].
    /// Children[KeyCount] points to subtree with keys greater than or equal to Keys[KeyCount-1].
    /// </summary>
    public BPlusTreeNode<TKey, TValue>[] Children;

    /// <inheritdoc/>
    public override bool IsLeaf => false;

    /// <summary>
    /// Creates a new internal node with the specified order.
    /// </summary>
    /// <param name="order">The maximum number of children.</param>
    public BPlusTreeInternalNode(int order) : base(order)
    {
        // Internal nodes can have up to order children
        // We allocate order+1 to handle temporary overflow during splits
        Children = new BPlusTreeNode<TKey, TValue>[order + 1];
    }
}

/// <summary>
/// Leaf node in a B+Tree. Stores key-value pairs and links to adjacent leaves.
/// </summary>
/// <typeparam name="TKey">The type of keys in the tree.</typeparam>
/// <typeparam name="TValue">The type of values in the tree.</typeparam>
internal sealed class BPlusTreeLeafNode<TKey, TValue> : BPlusTreeNode<TKey, TValue>
{
    /// <summary>
    /// The values stored in this leaf node.
    /// </summary>
    public TValue[] Values;

    /// <summary>
    /// Pointer to the next leaf node (for range queries).
    /// </summary>
    public BPlusTreeLeafNode<TKey, TValue> NextLeaf;

    /// <summary>
    /// Pointer to the previous leaf node (for reverse range queries).
    /// </summary>
    public BPlusTreeLeafNode<TKey, TValue> PreviousLeaf;

    /// <inheritdoc/>
    public override bool IsLeaf => true;

    /// <summary>
    /// Creates a new leaf node with the specified order.
    /// </summary>
    /// <param name="order">The maximum number of key-value pairs is order-1.</param>
    public BPlusTreeLeafNode(int order) : base(order)
    {
        // Leaf nodes can have up to order-1 key-value pairs
        // We allocate order to handle temporary overflow during splits
        Values = new TValue[order];
        NextLeaf = null;
        PreviousLeaf = null;
    }
}

#endregion

/// <summary>
/// A B+Tree implementation that provides ordered storage of key-value pairs with efficient
/// range queries and O(log n) operations. Implements <see cref="ILargeDictionary{TKey, TValue}"/>.
/// This version uses a struct comparer type parameter for maximum JIT inlining performance.
/// </summary>
/// <typeparam name="TKey">The type of keys in the tree.</typeparam>
/// <typeparam name="TValue">The type of values in the tree.</typeparam>
/// <typeparam name="TKeyComparer">The type of key comparer. Use a struct implementing <see cref="IComparer{T}"/> for best performance.</typeparam>
[DebuggerDisplay("LargeBPlusTree: Count = {Count}, Order = {Order}")]
public sealed class LargeBPlusTree<TKey, TValue, TKeyComparer> : ILargeDictionary<TKey, TValue>
    where TKey : notnull
    where TKeyComparer : IComparer<TKey>
{
    private BPlusTreeNode<TKey, TValue> _root;
    private BPlusTreeLeafNode<TKey, TValue> _firstLeaf;
    private BPlusTreeLeafNode<TKey, TValue> _lastLeaf;
    private TKeyComparer _comparer;
    private readonly int _order;
    private readonly int _minKeys;
    private long _count;

    /// <summary>
    /// Gets the order (maximum number of children per internal node) of this B+Tree.
    /// </summary>
    public int Order
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _order;
    }

    /// <summary>
    /// Gets the key comparer used by this tree.
    /// </summary>
    public TKeyComparer Comparer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _comparer;
    }

    /// <summary>
    /// Gets the number of key-value pairs stored in the tree.
    /// </summary>
    public long Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    /// <summary>
    /// Creates a new B+Tree with the specified key comparer and order.
    /// </summary>
    /// <param name="comparer">The key comparer to use. For best performance, use a struct type.</param>
    /// <param name="order">The order of the tree (maximum children per internal node). Must be at least 3. Default is 128.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="order"/> is less than 3.</exception>
    public LargeBPlusTree(TKeyComparer comparer, int order = 128)
    {
        if (comparer is null)
        {
            throw new ArgumentNullException(nameof(comparer));
        }
        if (order < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(order), "Order must be at least 3.");
        }

        _comparer = comparer;
        _order = order;
        _minKeys = (order - 1) / 2; // Minimum keys in a node (ceiling of (order-1)/2)
        _count = 0;

        // Initialize with an empty leaf node
        BPlusTreeLeafNode<TKey, TValue> initialLeaf = new BPlusTreeLeafNode<TKey, TValue>(order);
        _root = initialLeaf;
        _firstLeaf = initialLeaf;
        _lastLeaf = initialLeaf;
    }

    #region ILargeDictionary Implementation

    /// <inheritdoc/>
    TValue IReadOnlyLargeDictionary<TKey, TValue>.this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Get(key);
    }

    /// <inheritdoc/>
    public TValue this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            return Get(key);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            Set(key, value);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<TKey> Keys
    {
        get
        {
            BPlusTreeLeafNode<TKey, TValue> leaf = _firstLeaf;
            while (leaf is not null)
            {
                for (int i = 0; i < leaf.KeyCount; i++)
                {
                    yield return leaf.Keys[i];
                }
                leaf = leaf.NextLeaf;
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerable<TValue> Values
    {
        get
        {
            BPlusTreeLeafNode<TKey, TValue> leaf = _firstLeaf;
            while (leaf is not null)
            {
                for (int i = 0; i < leaf.Values.Length && i < leaf.KeyCount; i++)
                {
                    yield return leaf.Values[i];
                }
                leaf = leaf.NextLeaf;
            }
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue Get(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (TryGetValue(key, out TValue value))
        {
            return value;
        }

        throw new KeyNotFoundException($"The key '{key}' was not found in the tree.");
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(TKey key, TValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        InsertOrUpdate(key, value);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return TryGetValue(key, out _);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        BPlusTreeLeafNode<TKey, TValue> leaf = FindLeaf(key);
        int index = BinarySearchInNode(leaf, key);

        if (index >= 0)
        {
            value = leaf.Values[index];
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        if (item.Key is null)
        {
            throw new ArgumentNullException(nameof(item), "Key cannot be null");
        }

        InsertOrUpdate(item.Key, item.Value);
    }

    /// <inheritdoc/>
    public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (KeyValuePair<TKey, TValue> item in items)
        {
            Add(item);
        }
    }

    /// <inheritdoc/>
    public void AddRange(ReadOnlyLargeSpan<KeyValuePair<TKey, TValue>> items)
    {
        long count = items.Count;
        for (long i = 0; i < count; i++)
        {
            Add(items[i]);
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    /// <inheritdoc/>
    public void AddRange(ReadOnlySpan<KeyValuePair<TKey, TValue>> items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            Add(items[i]);
        }
    }
#endif

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (item.Key is null)
        {
            return false;
        }

        return Remove(item.Key);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(KeyValuePair<TKey, TValue> item, out KeyValuePair<TKey, TValue> removedItem)
    {
        if (item.Key is null)
        {
            removedItem = default;
            return false;
        }

        if (Remove(item.Key, out TValue removedValue))
        {
            removedItem = new KeyValuePair<TKey, TValue>(item.Key, removedValue);
            return true;
        }

        removedItem = default;
        return false;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        return Remove(key, out _);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key, out TValue removedValue)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        bool result = DeleteKey(key, out removedValue);
        return result;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        if (item.Key is null)
        {
            return false;
        }

        if (TryGetValue(item.Key, out TValue value))
        {
            return EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        return false;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        BPlusTreeLeafNode<TKey, TValue> newLeaf = new BPlusTreeLeafNode<TKey, TValue>(_order);
        _root = newLeaf;
        _firstLeaf = newLeaf;
        _lastLeaf = newLeaf;
        _count = 0;
    }

    /// <inheritdoc/>
    public BPlusTreeEnumerable GetAll()
    {
        return new BPlusTreeEnumerable(_firstLeaf);
    }

    /// <summary>
    /// Explicit interface implementation for IReadOnlyLargeCollection.GetAll().
    /// </summary>
    IEnumerable<KeyValuePair<TKey, TValue>> IReadOnlyLargeCollection<KeyValuePair<TKey, TValue>>.GetAll()
    {
        return GetAll();
    }

    /// <summary>
    /// High-performance struct enumerator for B+Tree iteration.
    /// </summary>
    public struct BPlusTreeEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private BPlusTreeLeafNode<TKey, TValue> _currentLeaf;
        private int _currentIndex;
        private KeyValuePair<TKey, TValue> _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal BPlusTreeEnumerator(BPlusTreeLeafNode<TKey, TValue> firstLeaf)
        {
            _currentLeaf = firstLeaf;
            _currentIndex = -1;
            _current = default;
        }

        public readonly KeyValuePair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        readonly object IEnumerator.Current => _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (_currentLeaf is not null)
            {
                _currentIndex++;
                if (_currentIndex < _currentLeaf.KeyCount)
                {
                    _current = new KeyValuePair<TKey, TValue>(_currentLeaf.Keys[_currentIndex], _currentLeaf.Values[_currentIndex]);
                    return true;
                }
                _currentLeaf = _currentLeaf.NextLeaf;
                _currentIndex = -1;
            }
            return false;
        }

        public void Reset() => throw new NotSupportedException();
        public readonly void Dispose() { }
    }

    /// <summary>
    /// High-performance struct enumerable for B+Tree iteration.
    /// </summary>
    public readonly struct BPlusTreeEnumerable : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly BPlusTreeLeafNode<TKey, TValue> _firstLeaf;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal BPlusTreeEnumerable(BPlusTreeLeafNode<TKey, TValue> firstLeaf) => _firstLeaf = firstLeaf;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BPlusTreeEnumerator GetEnumerator() => new(_firstLeaf);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <inheritdoc/>
    public void DoForEach(Action<KeyValuePair<TKey, TValue>> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        BPlusTreeLeafNode<TKey, TValue> leaf = _firstLeaf;
        while (leaf is not null)
        {
            for (int i = 0; i < leaf.KeyCount; i++)
            {
                action(new KeyValuePair<TKey, TValue>(leaf.Keys[i], leaf.Values[i]));
            }
            leaf = leaf.NextLeaf;
        }
    }

    /// <inheritdoc/>
    public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<KeyValuePair<TKey, TValue>>
    {
        BPlusTreeLeafNode<TKey, TValue> leaf = _firstLeaf;
        while (leaf is not null)
        {
            for (int i = 0; i < leaf.KeyCount; i++)
            {
                action.Invoke(new KeyValuePair<TKey, TValue>(leaf.Keys[i], leaf.Values[i]));
            }
            leaf = leaf.NextLeaf;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BPlusTreeEnumerator GetEnumerator()
    {
        return new BPlusTreeEnumerator(_firstLeaf);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    #region Range Query Methods

    /// <summary>
    /// Returns all key-value pairs in the specified range [minKey, maxKey] in ascending key order.
    /// </summary>
    /// <param name="minKey">The minimum key (inclusive).</param>
    /// <param name="maxKey">The maximum key (inclusive).</param>
    /// <returns>An enumerable of key-value pairs in the range.</returns>
    public IEnumerable<KeyValuePair<TKey, TValue>> GetRange(TKey minKey, TKey maxKey)
    {
        if (minKey is null)
        {
            throw new ArgumentNullException(nameof(minKey));
        }
        if (maxKey is null)
        {
            throw new ArgumentNullException(nameof(maxKey));
        }
        if (_comparer.Compare(minKey, maxKey) > 0)
        {
            yield break;
        }

        // Find the leaf containing minKey
        BPlusTreeLeafNode<TKey, TValue> leaf = FindLeaf(minKey);

        // Find starting position in the leaf
        int startIndex = FindFirstGreaterOrEqual(leaf, minKey);

        // Iterate through leaves until we exceed maxKey
        while (leaf is not null)
        {
            for (int i = startIndex; i < leaf.KeyCount; i++)
            {
                if (_comparer.Compare(leaf.Keys[i], maxKey) > 0)
                {
                    yield break;
                }
                yield return new KeyValuePair<TKey, TValue>(leaf.Keys[i], leaf.Values[i]);
            }
            leaf = leaf.NextLeaf;
            startIndex = 0;
        }
    }

    /// <summary>
    /// Returns all keys in the specified range [minKey, maxKey] in ascending order.
    /// </summary>
    /// <param name="minKey">The minimum key (inclusive).</param>
    /// <param name="maxKey">The maximum key (inclusive).</param>
    /// <returns>An enumerable of keys in the range.</returns>
    public IEnumerable<TKey> GetKeysInRange(TKey minKey, TKey maxKey)
    {
        if (minKey is null)
        {
            throw new ArgumentNullException(nameof(minKey));
        }
        if (maxKey is null)
        {
            throw new ArgumentNullException(nameof(maxKey));
        }
        if (_comparer.Compare(minKey, maxKey) > 0)
        {
            yield break;
        }

        BPlusTreeLeafNode<TKey, TValue> leaf = FindLeaf(minKey);
        int startIndex = FindFirstGreaterOrEqual(leaf, minKey);

        while (leaf is not null)
        {
            for (int i = startIndex; i < leaf.KeyCount; i++)
            {
                if (_comparer.Compare(leaf.Keys[i], maxKey) > 0)
                {
                    yield break;
                }
                yield return leaf.Keys[i];
            }
            leaf = leaf.NextLeaf;
            startIndex = 0;
        }
    }

    /// <summary>
    /// Returns all values for keys in the specified range [minKey, maxKey] in ascending key order.
    /// </summary>
    /// <param name="minKey">The minimum key (inclusive).</param>
    /// <param name="maxKey">The maximum key (inclusive).</param>
    /// <returns>An enumerable of values in the range.</returns>
    public IEnumerable<TValue> GetValuesInRange(TKey minKey, TKey maxKey)
    {
        if (minKey is null)
        {
            throw new ArgumentNullException(nameof(minKey));
        }
        if (maxKey is null)
        {
            throw new ArgumentNullException(nameof(maxKey));
        }
        if (_comparer.Compare(minKey, maxKey) > 0)
        {
            yield break;
        }

        BPlusTreeLeafNode<TKey, TValue> leaf = FindLeaf(minKey);
        int startIndex = FindFirstGreaterOrEqual(leaf, minKey);

        while (leaf is not null)
        {
            for (int i = startIndex; i < leaf.KeyCount; i++)
            {
                if (_comparer.Compare(leaf.Keys[i], maxKey) > 0)
                {
                    yield break;
                }
                yield return leaf.Values[i];
            }
            leaf = leaf.NextLeaf;
            startIndex = 0;
        }
    }

    /// <summary>
    /// Counts the number of key-value pairs in the specified range [minKey, maxKey].
    /// </summary>
    /// <param name="minKey">The minimum key (inclusive).</param>
    /// <param name="maxKey">The maximum key (inclusive).</param>
    /// <returns>The count of items in the range.</returns>
    public long CountInRange(TKey minKey, TKey maxKey)
    {
        if (minKey is null)
        {
            throw new ArgumentNullException(nameof(minKey));
        }
        if (maxKey is null)
        {
            throw new ArgumentNullException(nameof(maxKey));
        }
        if (_comparer.Compare(minKey, maxKey) > 0)
        {
            return 0L;
        }

        long count = 0L;
        BPlusTreeLeafNode<TKey, TValue> leaf = FindLeaf(minKey);
        int startIndex = FindFirstGreaterOrEqual(leaf, minKey);

        while (leaf is not null)
        {
            for (int i = startIndex; i < leaf.KeyCount; i++)
            {
                if (_comparer.Compare(leaf.Keys[i], maxKey) > 0)
                {
                    return count;
                }
                count++;
            }
            leaf = leaf.NextLeaf;
            startIndex = 0;
        }

        return count;
    }

    /// <summary>
    /// Executes an action for each key-value pair in the specified range [minKey, maxKey].
    /// </summary>
    /// <param name="minKey">The minimum key (inclusive).</param>
    /// <param name="maxKey">The maximum key (inclusive).</param>
    /// <param name="action">The action to execute for each key-value pair.</param>
    public void DoForEachInRange(TKey minKey, TKey maxKey, Action<KeyValuePair<TKey, TValue>> action)
    {
        if (minKey is null)
        {
            throw new ArgumentNullException(nameof(minKey));
        }
        if (maxKey is null)
        {
            throw new ArgumentNullException(nameof(maxKey));
        }
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }
        if (_comparer.Compare(minKey, maxKey) > 0)
        {
            return;
        }

        BPlusTreeLeafNode<TKey, TValue> leaf = FindLeaf(minKey);
        int startIndex = FindFirstGreaterOrEqual(leaf, minKey);

        while (leaf is not null)
        {
            for (int i = startIndex; i < leaf.KeyCount; i++)
            {
                if (_comparer.Compare(leaf.Keys[i], maxKey) > 0)
                {
                    return;
                }
                action(new KeyValuePair<TKey, TValue>(leaf.Keys[i], leaf.Values[i]));
            }
            leaf = leaf.NextLeaf;
            startIndex = 0;
        }
    }

    /// <summary>
    /// Executes an action for each key-value pair in the specified range [minKey, maxKey] using a struct action for optimal performance.
    /// </summary>
    /// <typeparam name="TAction">The action type implementing <see cref="ILargeAction{T}"/>.</typeparam>
    /// <param name="minKey">The minimum key (inclusive).</param>
    /// <param name="maxKey">The maximum key (inclusive).</param>
    /// <param name="action">The action instance passed by reference.</param>
    public void DoForEachInRange<TAction>(TKey minKey, TKey maxKey, ref TAction action) where TAction : ILargeAction<KeyValuePair<TKey, TValue>>
    {
        if (minKey is null)
        {
            throw new ArgumentNullException(nameof(minKey));
        }
        if (maxKey is null)
        {
            throw new ArgumentNullException(nameof(maxKey));
        }
        if (_comparer.Compare(minKey, maxKey) > 0)
        {
            return;
        }

        BPlusTreeLeafNode<TKey, TValue> leaf = FindLeaf(minKey);
        int startIndex = FindFirstGreaterOrEqual(leaf, minKey);

        while (leaf is not null)
        {
            for (int i = startIndex; i < leaf.KeyCount; i++)
            {
                if (_comparer.Compare(leaf.Keys[i], maxKey) > 0)
                {
                    return;
                }
                action.Invoke(new KeyValuePair<TKey, TValue>(leaf.Keys[i], leaf.Values[i]));
            }
            leaf = leaf.NextLeaf;
            startIndex = 0;
        }
    }

    /// <summary>
    /// Returns the minimum key in the tree.
    /// </summary>
    /// <returns>The minimum key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tree is empty.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TKey GetMinKey()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("The tree is empty.");
        }
        return _firstLeaf.Keys[0];
    }

    /// <summary>
    /// Returns the maximum key in the tree.
    /// </summary>
    /// <returns>The maximum key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tree is empty.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TKey GetMaxKey()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("The tree is empty.");
        }
        return _lastLeaf.Keys[_lastLeaf.KeyCount - 1];
    }

    /// <summary>
    /// Tries to get the minimum key in the tree.
    /// </summary>
    /// <param name="minKey">The minimum key if the tree is not empty.</param>
    /// <returns>True if the tree is not empty; otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMinKey(out TKey minKey)
    {
        if (_count == 0)
        {
            minKey = default;
            return false;
        }
        minKey = _firstLeaf.Keys[0];
        return true;
    }

    /// <summary>
    /// Tries to get the maximum key in the tree.
    /// </summary>
    /// <param name="maxKey">The maximum key if the tree is not empty.</param>
    /// <returns>True if the tree is not empty; otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMaxKey(out TKey maxKey)
    {
        if (_count == 0)
        {
            maxKey = default;
            return false;
        }
        maxKey = _lastLeaf.Keys[_lastLeaf.KeyCount - 1];
        return true;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Finds the leaf node that should contain the given key.
    /// </summary>
    private BPlusTreeLeafNode<TKey, TValue> FindLeaf(TKey key)
    {
        BPlusTreeNode<TKey, TValue> node = _root;

        while (!node.IsLeaf)
        {
            BPlusTreeInternalNode<TKey, TValue> internalNode = (BPlusTreeInternalNode<TKey, TValue>)node;
            int childIndex = FindChildIndex(internalNode, key);
            node = internalNode.Children[childIndex];
        }

        return (BPlusTreeLeafNode<TKey, TValue>)node;
    }

    /// <summary>
    /// Finds the child index for a key in an internal node.
    /// </summary>
    private int FindChildIndex(BPlusTreeInternalNode<TKey, TValue> node, TKey key)
    {
        int low = 0;
        int high = node.KeyCount - 1;

        while (low <= high)
        {
            int mid = low + ((high - low) >> 1);
            int cmp = _comparer.Compare(key, node.Keys[mid]);

            if (cmp < 0)
            {
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }

        return low;
    }

    /// <summary>
    /// Binary search for a key in a node. Returns non-negative index if found, negative if not found.
    /// </summary>
    private int BinarySearchInNode(BPlusTreeNode<TKey, TValue> node, TKey key)
    {
        int low = 0;
        int high = node.KeyCount - 1;

        while (low <= high)
        {
            int mid = low + ((high - low) >> 1);
            int cmp = _comparer.Compare(key, node.Keys[mid]);

            if (cmp == 0)
            {
                return mid;
            }
            else if (cmp < 0)
            {
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }

        return ~low; // Return bitwise complement of insertion point
    }

    /// <summary>
    /// Finds the index of the first key greater than or equal to the given key.
    /// </summary>
    private int FindFirstGreaterOrEqual(BPlusTreeNode<TKey, TValue> node, TKey key)
    {
        int low = 0;
        int high = node.KeyCount - 1;

        while (low <= high)
        {
            int mid = low + ((high - low) >> 1);
            int cmp = _comparer.Compare(node.Keys[mid], key);

            if (cmp < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }

    /// <summary>
    /// Inserts or updates a key-value pair in the tree.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InsertOrUpdate(TKey key, TValue value)
    {
        // Find the leaf where the key should be inserted
        BPlusTreeLeafNode<TKey, TValue> leaf = FindLeaf(key);

        // Check if key already exists
        int index = BinarySearchInNode(leaf, key);
        if (index >= 0)
        {
            // Update existing value
            leaf.Values[index] = value;
            return;
        }

        // Key doesn't exist, insert new key-value pair
        if (_count >= Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Cannot store more than {Constants.MaxLargeCollectionCount} items.");
        }

        // Insert into leaf (may cause split)
        int insertionPoint = ~index;
        InsertIntoLeaf(leaf, key, value, insertionPoint);
        _count++;
    }

    /// <summary>
    /// Inserts a key-value pair into a leaf node at the specified position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InsertIntoLeaf(BPlusTreeLeafNode<TKey, TValue> leaf, TKey key, TValue value, int insertionPoint)
    {
        // Shift existing elements to make room using Array.Copy
        int shiftCount = leaf.KeyCount - insertionPoint;
        if (shiftCount > 0)
        {
            Array.Copy(leaf.Keys, insertionPoint, leaf.Keys, insertionPoint + 1, shiftCount);
            Array.Copy(leaf.Values, insertionPoint, leaf.Values, insertionPoint + 1, shiftCount);
        }

        leaf.Keys[insertionPoint] = key;
        leaf.Values[insertionPoint] = value;
        leaf.KeyCount++;

        // Check if split is needed (leaf can hold order-1 keys)
        if (leaf.KeyCount == _order)
        {
            SplitLeaf(leaf);
        }
    }

    /// <summary>
    /// Splits a full leaf node.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SplitLeaf(BPlusTreeLeafNode<TKey, TValue> leaf)
    {
        int splitIndex = leaf.KeyCount / 2;

        // Create new leaf
        BPlusTreeLeafNode<TKey, TValue> newLeaf = new BPlusTreeLeafNode<TKey, TValue>(_order);
        newLeaf.Parent = leaf.Parent;

        // Move half the keys to new leaf using Array.Copy
        int newLeafKeyCount = leaf.KeyCount - splitIndex;
        Array.Copy(leaf.Keys, splitIndex, newLeaf.Keys, 0, newLeafKeyCount);
        Array.Copy(leaf.Values, splitIndex, newLeaf.Values, 0, newLeafKeyCount);
        newLeaf.KeyCount = newLeafKeyCount;

        // Clear moved entries in original leaf
        Array.Clear(leaf.Keys, splitIndex, newLeafKeyCount);
        Array.Clear(leaf.Values, splitIndex, newLeafKeyCount);
        leaf.KeyCount = splitIndex;

        // Update linked list pointers
        newLeaf.NextLeaf = leaf.NextLeaf;
        newLeaf.PreviousLeaf = leaf;
        if (leaf.NextLeaf is not null)
        {
            leaf.NextLeaf.PreviousLeaf = newLeaf;
        }
        leaf.NextLeaf = newLeaf;

        // Update last leaf pointer if needed
        if (_lastLeaf == leaf)
        {
            _lastLeaf = newLeaf;
        }

        // Promote smallest key of new leaf to parent
        TKey promotedKey = newLeaf.Keys[0];
        InsertIntoParent(leaf, promotedKey, newLeaf);
    }

    /// <summary>
    /// Inserts a key and new child into the parent of a split node.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InsertIntoParent(BPlusTreeNode<TKey, TValue> leftChild, TKey key, BPlusTreeNode<TKey, TValue> rightChild)
    {
        // If leftChild is root, create new root
        if (leftChild == _root)
        {
            BPlusTreeInternalNode<TKey, TValue> newRoot = new BPlusTreeInternalNode<TKey, TValue>(_order);
            newRoot.Keys[0] = key;
            newRoot.KeyCount = 1;
            newRoot.Children[0] = leftChild;
            newRoot.Children[1] = rightChild;
            leftChild.Parent = newRoot;
            rightChild.Parent = newRoot;
            _root = newRoot;
            return;
        }

        // Use parent pointer (O(1) instead of O(n) FindParent)
        BPlusTreeInternalNode<TKey, TValue> parent = leftChild.Parent;

        // Find insertion position in parent using binary search
        int insertionPoint = FindFirstGreaterOrEqual(parent, key);

        // Shift keys and children to make room
        int shiftCount = parent.KeyCount - insertionPoint;
        if (shiftCount > 0)
        {
            Array.Copy(parent.Keys, insertionPoint, parent.Keys, insertionPoint + 1, shiftCount);
            Array.Copy(parent.Children, insertionPoint + 1, parent.Children, insertionPoint + 2, shiftCount);
        }

        parent.Keys[insertionPoint] = key;
        parent.Children[insertionPoint + 1] = rightChild;
        rightChild.Parent = parent;
        parent.KeyCount++;

        // Check if split is needed (internal node can hold order-1 keys)
        if (parent.KeyCount == _order)
        {
            SplitInternal(parent);
        }
    }

    /// <summary>
    /// Splits a full internal node.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SplitInternal(BPlusTreeInternalNode<TKey, TValue> node)
    {
        int splitIndex = node.KeyCount / 2;
        TKey promotedKey = node.Keys[splitIndex];

        // Create new internal node
        BPlusTreeInternalNode<TKey, TValue> newNode = new BPlusTreeInternalNode<TKey, TValue>(_order);

        // Move keys after split point to new node
        int newNodeKeyCount = node.KeyCount - splitIndex - 1;
        Array.Copy(node.Keys, splitIndex + 1, newNode.Keys, 0, newNodeKeyCount);
        newNode.KeyCount = newNodeKeyCount;

        // Move children after split point to new node and update parent pointers
        for (int i = 0; i <= newNodeKeyCount; i++)
        {
            BPlusTreeNode<TKey, TValue> child = node.Children[splitIndex + 1 + i];
            newNode.Children[i] = child;
            child.Parent = newNode;
        }

        // Clear moved entries in original node
        Array.Clear(node.Keys, splitIndex, node.KeyCount - splitIndex);
        Array.Clear(node.Children, splitIndex + 1, node.KeyCount - splitIndex);
        node.KeyCount = splitIndex;

        // Insert promoted key into parent
        InsertIntoParent(node, promotedKey, newNode);
    }

    /// <summary>
    /// Finds the parent of a given child node.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private BPlusTreeInternalNode<TKey, TValue> FindParent(BPlusTreeNode<TKey, TValue> current, BPlusTreeNode<TKey, TValue> child)
    {
        if (current.IsLeaf || current == child)
        {
            return null;
        }

        BPlusTreeInternalNode<TKey, TValue> internalNode = (BPlusTreeInternalNode<TKey, TValue>)current;

        for (int i = 0; i <= internalNode.KeyCount; i++)
        {
            if (internalNode.Children[i] == child)
            {
                return internalNode;
            }

            if (!internalNode.Children[i].IsLeaf)
            {
                BPlusTreeInternalNode<TKey, TValue> result = FindParent(internalNode.Children[i], child);
                if (result is not null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Deletes a key from the tree.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool DeleteKey(TKey key, out TValue removedValue)
    {
        BPlusTreeLeafNode<TKey, TValue> leaf = FindLeaf(key);
        int index = BinarySearchInNode(leaf, key);

        if (index < 0)
        {
            removedValue = default;
            return false;
        }

        removedValue = leaf.Values[index];

        // Remove from leaf
        for (int i = index; i < leaf.KeyCount - 1; i++)
        {
            leaf.Keys[i] = leaf.Keys[i + 1];
            leaf.Values[i] = leaf.Values[i + 1];
        }
        leaf.Keys[leaf.KeyCount - 1] = default;
        leaf.Values[leaf.KeyCount - 1] = default;
        leaf.KeyCount--;
        _count--;

        // Handle underflow if not root
        if (leaf != _root && leaf.KeyCount < _minKeys)
        {
            HandleLeafUnderflow(leaf);
        }

        // If root is internal and has only one child, make that child the new root
        if (!_root.IsLeaf && _root.KeyCount == 0)
        {
            BPlusTreeInternalNode<TKey, TValue> rootInternal = (BPlusTreeInternalNode<TKey, TValue>)_root;
            _root = rootInternal.Children[0];
        }

        return true;
    }

    /// <summary>
    /// Handles underflow in a leaf node by redistributing or merging.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleLeafUnderflow(BPlusTreeLeafNode<TKey, TValue> leaf)
    {
        BPlusTreeInternalNode<TKey, TValue> parent = leaf.Parent;
        if (parent is null)
        {
            return; // leaf is root
        }

        // Find leaf's position in parent
        int leafIndex = 0;
        while (leafIndex <= parent.KeyCount && parent.Children[leafIndex] != leaf)
        {
            leafIndex++;
        }

        // Try to borrow from left sibling
        if (leafIndex > 0)
        {
            BPlusTreeLeafNode<TKey, TValue> leftSibling = (BPlusTreeLeafNode<TKey, TValue>)parent.Children[leafIndex - 1];
            if (leftSibling.KeyCount > _minKeys)
            {
                BorrowFromLeftLeaf(leaf, leftSibling, parent, leafIndex - 1);
                return;
            }
        }

        // Try to borrow from right sibling
        if (leafIndex < parent.KeyCount)
        {
            BPlusTreeLeafNode<TKey, TValue> rightSibling = (BPlusTreeLeafNode<TKey, TValue>)parent.Children[leafIndex + 1];
            if (rightSibling.KeyCount > _minKeys)
            {
                BorrowFromRightLeaf(leaf, rightSibling, parent, leafIndex);
                return;
            }
        }

        // Merge with a sibling
        if (leafIndex > 0)
        {
            // Merge with left sibling
            BPlusTreeLeafNode<TKey, TValue> leftSibling = (BPlusTreeLeafNode<TKey, TValue>)parent.Children[leafIndex - 1];
            MergeLeaves(leftSibling, leaf, parent, leafIndex - 1);
        }
        else
        {
            // Merge with right sibling
            BPlusTreeLeafNode<TKey, TValue> rightSibling = (BPlusTreeLeafNode<TKey, TValue>)parent.Children[leafIndex + 1];
            MergeLeaves(leaf, rightSibling, parent, leafIndex);
        }
    }

    /// <summary>
    /// Borrows a key from the left leaf sibling.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BorrowFromLeftLeaf(BPlusTreeLeafNode<TKey, TValue> leaf, BPlusTreeLeafNode<TKey, TValue> leftSibling,
        BPlusTreeInternalNode<TKey, TValue> parent, int parentKeyIndex)
    {
        // Shift leaf's entries to make room
        for (int i = leaf.KeyCount; i > 0; i--)
        {
            leaf.Keys[i] = leaf.Keys[i - 1];
            leaf.Values[i] = leaf.Values[i - 1];
        }

        // Move last entry from left sibling
        leaf.Keys[0] = leftSibling.Keys[leftSibling.KeyCount - 1];
        leaf.Values[0] = leftSibling.Values[leftSibling.KeyCount - 1];
        leaf.KeyCount++;

        leftSibling.Keys[leftSibling.KeyCount - 1] = default;
        leftSibling.Values[leftSibling.KeyCount - 1] = default;
        leftSibling.KeyCount--;

        // Update parent key
        parent.Keys[parentKeyIndex] = leaf.Keys[0];
    }

    /// <summary>
    /// Borrows a key from the right leaf sibling.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BorrowFromRightLeaf(BPlusTreeLeafNode<TKey, TValue> leaf, BPlusTreeLeafNode<TKey, TValue> rightSibling,
        BPlusTreeInternalNode<TKey, TValue> parent, int parentKeyIndex)
    {
        // Move first entry from right sibling
        leaf.Keys[leaf.KeyCount] = rightSibling.Keys[0];
        leaf.Values[leaf.KeyCount] = rightSibling.Values[0];
        leaf.KeyCount++;

        // Shift right sibling's entries
        for (int i = 0; i < rightSibling.KeyCount - 1; i++)
        {
            rightSibling.Keys[i] = rightSibling.Keys[i + 1];
            rightSibling.Values[i] = rightSibling.Values[i + 1];
        }
        rightSibling.Keys[rightSibling.KeyCount - 1] = default;
        rightSibling.Values[rightSibling.KeyCount - 1] = default;
        rightSibling.KeyCount--;

        // Update parent key
        parent.Keys[parentKeyIndex] = rightSibling.Keys[0];
    }

    /// <summary>
    /// Merges two leaf nodes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MergeLeaves(BPlusTreeLeafNode<TKey, TValue> left, BPlusTreeLeafNode<TKey, TValue> right,
        BPlusTreeInternalNode<TKey, TValue> parent, int parentKeyIndex)
    {
        // Move all entries from right to left
        for (int i = 0; i < right.KeyCount; i++)
        {
            left.Keys[left.KeyCount + i] = right.Keys[i];
            left.Values[left.KeyCount + i] = right.Values[i];
        }
        left.KeyCount += right.KeyCount;

        // Update linked list
        left.NextLeaf = right.NextLeaf;
        if (right.NextLeaf is not null)
        {
            right.NextLeaf.PreviousLeaf = left;
        }

        // Update last leaf pointer if needed
        if (_lastLeaf == right)
        {
            _lastLeaf = left;
        }

        // Remove key and child from parent
        RemoveFromInternal(parent, parentKeyIndex);
    }

    /// <summary>
    /// Removes a key and its right child from an internal node.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveFromInternal(BPlusTreeInternalNode<TKey, TValue> node, int keyIndex)
    {
        // Shift keys left
        for (int i = keyIndex; i < node.KeyCount - 1; i++)
        {
            node.Keys[i] = node.Keys[i + 1];
        }
        node.Keys[node.KeyCount - 1] = default;

        // Shift children left (remove right child of the removed key)
        for (int i = keyIndex + 1; i < node.KeyCount; i++)
        {
            node.Children[i] = node.Children[i + 1];
        }
        node.Children[node.KeyCount] = null;

        node.KeyCount--;

        // Handle underflow if not root
        if (node != _root && node.KeyCount < _minKeys)
        {
            HandleInternalUnderflow(node);
        }
    }

    /// <summary>
    /// Handles underflow in an internal node.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleInternalUnderflow(BPlusTreeInternalNode<TKey, TValue> node)
    {
        BPlusTreeInternalNode<TKey, TValue> parent = node.Parent;
        if (parent is null)
        {
            return;
        }

        // Find node's position in parent
        int nodeIndex = 0;
        while (nodeIndex <= parent.KeyCount && parent.Children[nodeIndex] != node)
        {
            nodeIndex++;
        }

        // Try to borrow from left sibling
        if (nodeIndex > 0)
        {
            BPlusTreeInternalNode<TKey, TValue> leftSibling = (BPlusTreeInternalNode<TKey, TValue>)parent.Children[nodeIndex - 1];
            if (leftSibling.KeyCount > _minKeys)
            {
                BorrowFromLeftInternal(node, leftSibling, parent, nodeIndex - 1);
                return;
            }
        }

        // Try to borrow from right sibling
        if (nodeIndex < parent.KeyCount)
        {
            BPlusTreeInternalNode<TKey, TValue> rightSibling = (BPlusTreeInternalNode<TKey, TValue>)parent.Children[nodeIndex + 1];
            if (rightSibling.KeyCount > _minKeys)
            {
                BorrowFromRightInternal(node, rightSibling, parent, nodeIndex);
                return;
            }
        }

        // Merge with a sibling
        if (nodeIndex > 0)
        {
            BPlusTreeInternalNode<TKey, TValue> leftSibling = (BPlusTreeInternalNode<TKey, TValue>)parent.Children[nodeIndex - 1];
            MergeInternals(leftSibling, node, parent, nodeIndex - 1);
        }
        else
        {
            BPlusTreeInternalNode<TKey, TValue> rightSibling = (BPlusTreeInternalNode<TKey, TValue>)parent.Children[nodeIndex + 1];
            MergeInternals(node, rightSibling, parent, nodeIndex);
        }
    }

    /// <summary>
    /// Borrows a key from the left internal sibling.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BorrowFromLeftInternal(BPlusTreeInternalNode<TKey, TValue> node, BPlusTreeInternalNode<TKey, TValue> leftSibling,
        BPlusTreeInternalNode<TKey, TValue> parent, int parentKeyIndex)
    {
        // Shift node's keys and children to make room
        for (int i = node.KeyCount; i > 0; i--)
        {
            node.Keys[i] = node.Keys[i - 1];
        }
        for (int i = node.KeyCount + 1; i > 0; i--)
        {
            node.Children[i] = node.Children[i - 1];
        }

        // Move parent key down to node
        node.Keys[0] = parent.Keys[parentKeyIndex];
        node.KeyCount++;

        // Move last child from left sibling and update its parent
        BPlusTreeNode<TKey, TValue> movedChild = leftSibling.Children[leftSibling.KeyCount];
        node.Children[0] = movedChild;
        movedChild.Parent = node;
        leftSibling.Children[leftSibling.KeyCount] = null;

        // Move last key from left sibling up to parent
        parent.Keys[parentKeyIndex] = leftSibling.Keys[leftSibling.KeyCount - 1];
        leftSibling.Keys[leftSibling.KeyCount - 1] = default;
        leftSibling.KeyCount--;
    }

    /// <summary>
    /// Borrows a key from the right internal sibling.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BorrowFromRightInternal(BPlusTreeInternalNode<TKey, TValue> node, BPlusTreeInternalNode<TKey, TValue> rightSibling,
        BPlusTreeInternalNode<TKey, TValue> parent, int parentKeyIndex)
    {
        // Move parent key down to node
        node.Keys[node.KeyCount] = parent.Keys[parentKeyIndex];
        node.KeyCount++;

        // Move first child from right sibling and update its parent
        BPlusTreeNode<TKey, TValue> movedChild = rightSibling.Children[0];
        node.Children[node.KeyCount] = movedChild;
        movedChild.Parent = node;

        // Move first key from right sibling up to parent
        parent.Keys[parentKeyIndex] = rightSibling.Keys[0];

        // Shift right sibling's keys and children
        for (int i = 0; i < rightSibling.KeyCount - 1; i++)
        {
            rightSibling.Keys[i] = rightSibling.Keys[i + 1];
        }
        rightSibling.Keys[rightSibling.KeyCount - 1] = default;

        for (int i = 0; i < rightSibling.KeyCount; i++)
        {
            rightSibling.Children[i] = rightSibling.Children[i + 1];
        }
        rightSibling.Children[rightSibling.KeyCount] = null;

        rightSibling.KeyCount--;
    }

    /// <summary>
    /// Merges two internal nodes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MergeInternals(BPlusTreeInternalNode<TKey, TValue> left, BPlusTreeInternalNode<TKey, TValue> right,
        BPlusTreeInternalNode<TKey, TValue> parent, int parentKeyIndex)
    {
        // Move parent key down to left
        left.Keys[left.KeyCount] = parent.Keys[parentKeyIndex];
        left.KeyCount++;

        // Move all keys and children from right to left, updating parent pointers
        for (int i = 0; i < right.KeyCount; i++)
        {
            left.Keys[left.KeyCount + i] = right.Keys[i];
        }
        for (int i = 0; i <= right.KeyCount; i++)
        {
            BPlusTreeNode<TKey, TValue> child = right.Children[i];
            left.Children[left.KeyCount + i] = child;
            child.Parent = left;
        }
        left.KeyCount += right.KeyCount;

        // Remove key and child from parent
        RemoveFromInternal(parent, parentKeyIndex);
    }

    #endregion
}
