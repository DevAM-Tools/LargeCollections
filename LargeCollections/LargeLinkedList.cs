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

/// <summary>
/// A linked list of <typeparamref name="T"/> that can store up to <see cref="Constants.MaxLargeCollectionCount"/> elements.
/// </summary>
/// <typeparam name="T">The type of elements in the list</typeparam>
[DebuggerDisplay("LargeLinkedList: Count = {Count}")]
public class LargeLinkedList<T> : ILargeCollection<T>
{
    private Node _Head;
    private Node _Tail;
    private long _Count;

    public long Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Count;
    }

    public Node First
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Head;
    }

    public Node Last
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Tail;
    }

    public LargeLinkedList()
    {
        _Head = null;
        _Tail = null;
        _Count = 0L;
    }

    public LargeLinkedList(IEnumerable<T> items)
    {
        _Head = null;
        _Tail = null;
        _Count = 0L;

        if (items is not null)
        {
            foreach (T item in items)
            {
                AddLast(item);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
        => AddLast(item);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IEnumerable<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (items is IReadOnlyList<T> readOnlyList)
        {
            long count = readOnlyList.Count;
            for (int i = 0; i < count; i++)
            {
                AddLast(readOnlyList[i]);
            }
        }
        else if (items is IReadOnlyLargeArray<T> readOnlyLargeArray)
        {
            long count = readOnlyLargeArray.Count;
            for (long i = 0; i < count; i++)
            {
                AddLast(readOnlyLargeArray[i]);
            }
        }
        else
        {
            foreach (T item in items)
            {
                AddLast(item);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlyLargeSpan<T> items)
    {
        for (long i = 0; i < items.Count; i++)
        {
            AddLast(items[i]);
        }
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlySpan<T> items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            AddLast(items[i]);
        }
    }
#endif

    /// <summary>
    /// Adds a value to the end of the list
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Node AddLast(T value)
    {
        if (_Count >= Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Cannot add more elements. Maximum capacity of {Constants.MaxLargeCollectionCount} reached.");
        }

        Node newNode = new(this, value);

        if (_Tail is null)
        {
            // First node
            _Head = newNode;
            _Tail = newNode;
        }
        else
        {
            // Link to existing tail
            _Tail.Next = newNode;
            newNode.Previous = _Tail;
            _Tail = newNode;
        }

        _Count++;
        return newNode;
    }

    /// <summary>
    /// Adds a value to the beginning of the list
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Node AddFirst(T value)
    {
        if (_Count >= Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Cannot add more elements. Maximum capacity of {Constants.MaxLargeCollectionCount} reached.");
        }

        Node newNode = new(this, value);

        if (_Head is null)
        {
            // First node
            _Head = newNode;
            _Tail = newNode;
        }
        else
        {
            // Link to existing head
            _Head.Previous = newNode;
            newNode.Next = _Head;
            _Head = newNode;
        }

        _Count++;
        return newNode;
    }

    /// <summary>
    /// Adds a node to the end of the list
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLast(Node node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (_Count >= Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Cannot add more elements. Maximum capacity of {Constants.MaxLargeCollectionCount} reached.");
        }

        ValidateNode(node);

        if (_Tail is null)
        {
            // First node
            _Head = node;
            _Tail = node;
        }
        else
        {
            // Link to existing tail
            _Tail.Next = node;
            node.Previous = _Tail;
            _Tail = node;
        }

        _Count++;
    }

    /// <summary>
    /// Adds a node to the beginning of the list
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddFirst(Node node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (_Count >= Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Cannot add more elements. Maximum capacity of {Constants.MaxLargeCollectionCount} reached.");
        }

        ValidateNode(node);

        if (_Head is null)
        {
            // First node
            _Head = node;
            _Tail = node;
        }
        else
        {
            // Link to existing head
            _Head.Previous = node;
            node.Next = _Head;
            _Head = node;
        }

        _Count++;
    }

    /// <summary>
    /// Inserts a value after the specified node
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Node AddAfter(Node node, T value)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (_Count >= Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Cannot add more elements. Maximum capacity of {Constants.MaxLargeCollectionCount} reached.");
        }

        ValidateNode(node);

        Node newNode = new(this, value);

        if (node == _Tail)
        {
            // Adding after tail
            _Tail.Next = newNode;
            newNode.Previous = _Tail;
            _Tail = newNode;
        }
        else
        {
            // Insert between node and node.Next
            newNode.Next = node.Next;
            newNode.Previous = node;
            node.Next.Previous = newNode;
            node.Next = newNode;
        }

        _Count++;
        return newNode;
    }

    /// <summary>
    /// Inserts a value before the specified node
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Node AddBefore(Node node, T value)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (_Count >= Constants.MaxLargeCollectionCount)
        {
            throw new InvalidOperationException($"Cannot add more elements. Maximum capacity of {Constants.MaxLargeCollectionCount} reached.");
        }

        ValidateNode(node);

        Node newNode = new(this, value);

        if (node == _Head)
        {
            // Adding before head
            _Head.Previous = newNode;
            newNode.Next = _Head;
            _Head = newNode;
        }
        else
        {
            // Insert between node.Previous and node
            newNode.Previous = node.Previous;
            newNode.Next = node;
            node.Previous.Next = newNode;
            node.Previous = newNode;
        }

        _Count++;
        return newNode;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T item)
        => Remove(item, out _, null);

    /// <summary>
    /// Removes the first node from the list
    /// </summary>
    /// <returns>True if the node was found and removed; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveFirst()
    {
        if (_Head is null)
        {
            throw new InvalidOperationException("The list is empty.");
        }

        bool result = Remove(_Head);
        return result;
    }

    /// <summary>
    /// Removes the last node from the list
    /// </summary>
    /// <returns>True if the node was found and removed; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveLast()
    {
        if (_Tail is null)
        {
            throw new InvalidOperationException("The list is empty.");
        }

        bool result = Remove(_Tail);
        return result;
    }

    /// <summary>
    /// Removes the first occurrence of the specified value
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <param name="removedItem">The removed item.</param>
    /// <returns>True if the item was found and removed; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T value, out T removedItem)
        => Remove(value, out removedItem, null);

    /// <summary>
    /// Removes the first occurrence of the specified value
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <param name="removedItem">The removed item.</param>
    /// <param name="equalsFunction">The function used to compare values.</param>
    /// <returns>True if the item was found and removed; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T value, out T removedItem, Func<T, T, bool> equalsFunction)
    {
        equalsFunction ??= DefaultFunctions<T>.DefaultEqualsFunction;

        removedItem = default;
        Node node = Find(value, equalsFunction);
        if (node is not null)
        {
            removedItem = node.Value;
            Remove(node);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes the specified node from the list
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(Node node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        ValidateNode(node);

        if (node.Previous is null && node.Next is null)
        {
            // Only node
            _Head = null;
            _Tail = null;
        }
        else if (node.Previous is null)
        {
            // First node
            _Head = node.Next;
            _Head.Previous = null;
        }
        else if (node.Next is null)
        {
            // Last node
            _Tail = node.Previous;
            _Tail.Next = null;
        }
        else
        {
            // Middle node
            node.Previous.Next = node.Next;
            node.Next.Previous = node.Previous;
        }

        node.Invalidate();
        _Count--;

        return true;
    }

    /// <summary>
    /// Finds the first node containing the specified value
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Node Find(T value, Func<T, T, bool> equalsFunction = null)
    {
        equalsFunction ??= DefaultFunctions<T>.DefaultEqualsFunction;
        Node current = _Head;

        while (current is not null)
        {
            if (equalsFunction.Invoke(current.Value, value))
            {
                return current;
            }
            current = current.Next;
        }

        return null;
    }

    /// <summary>
    /// Finds the last node containing the specified value
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Node FindLast(T value, Func<T, T, bool> equalsFunction = null)
    {
        equalsFunction ??= DefaultFunctions<T>.DefaultEqualsFunction;
        Node current = _Tail;

        while (current is not null)
        {
            if (equalsFunction.Invoke(current.Value, value))
            {
                return current;
            }
            current = current.Previous;
        }

        return null;
    }

    /// <summary>
    /// Clears all nodes from the list
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        // Simply reset head/tail - GC will clean up the nodes
        // This is O(1) instead of O(n)
        // Note: Nodes will have stale references but they're no longer accessible
        _Head = null;
        _Tail = null;
        _Count = 0L;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        Func<T, T, bool> equalsFunction = DefaultFunctions<T>.DefaultEqualsFunction;
        return Find(item, equalsFunction) is not null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item, Func<T, T, bool> equalsFunction)
    {
        equalsFunction ??= DefaultFunctions<T>.DefaultEqualsFunction;
        return Find(item, equalsFunction) is not null;
    }

    #region DoForEach Methods

    /// <summary>
    /// Performs the <paramref name="action"/> with items of the linked list.
    /// </summary>
    /// <param name="action">The function that will be called for each item.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        Node current = _Head;
        while (current is not null)
        {
            action.Invoke(current.Value);
            current = current.Next;
        }
    }

    /// <summary>
    /// Performs the action on items using an action for optimal performance.
    /// </summary>
    /// <typeparam name="TAction">A type implementing <see cref="ILargeAction{T}"/>.</typeparam>
    /// <param name="action">The action instance passed by reference.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<T>
    {
        Node current = _Head;
        while (current is not null)
        {
            action.Invoke(current.Value);
            current = current.Next;
        }
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAll()
    {
        Node current = _Head;
        while (current is not null)
        {
            yield return current.Value;
            current = current.Next;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<T> GetEnumerator()
        => GetAll().GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator()
        => GetAll().GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateNode(Node node)
    {
        if (node._list != this)
        {
            throw new InvalidOperationException("The node does not belong to this list.");
        }
    }

    public sealed class Node
    {
        internal Node(LargeLinkedList<T> list, T value)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
            Value = value;
            Next = null;
            Previous = null;
        }

        internal LargeLinkedList<T> _list;
        public T Value { get; set; }
        public Node Next { get; internal set; }
        public Node Previous { get; internal set; }

        internal void Invalidate()
        {
            _list = null;
            Next = null;
            Previous = null;
        }
    }
}