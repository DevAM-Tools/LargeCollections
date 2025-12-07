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

#region BKD-Tree Node Classes

/// <summary>
/// Base class for BKD-Tree nodes.
/// </summary>
/// <typeparam name="T">The point type stored in the tree.</typeparam>
internal abstract class BKDTreeNode<T>
{
    /// <summary>
    /// Gets whether this node is a leaf node.
    /// </summary>
    public abstract bool IsLeaf { get; }

    /// <summary>
    /// Gets the number of points in this subtree.
    /// </summary>
    public abstract long PointCount { get; }
}

/// <summary>
/// Internal node in the BKD-Tree that splits space along a dimension.
/// </summary>
/// <typeparam name="T">The point type stored in the tree.</typeparam>
internal sealed class BKDTreeInternalNode<T> : BKDTreeNode<T>
{
    /// <summary>
    /// The dimension along which this node splits.
    /// </summary>
    public int SplitDimension;

    /// <summary>
    /// The split value for this node.
    /// </summary>
    public double SplitValue;

    /// <summary>
    /// The left child (points with coordinate &lt;= split value).
    /// </summary>
    public BKDTreeNode<T> Left;

    /// <summary>
    /// The right child (points with coordinate &gt; split value).
    /// </summary>
    public BKDTreeNode<T> Right;

    /// <summary>
    /// Cached count of points in this subtree.
    /// </summary>
    private long _pointCount;

    /// <inheritdoc/>
    public override bool IsLeaf
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => false;
    }

    /// <inheritdoc/>
    public override long PointCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _pointCount;
    }

    /// <summary>
    /// Creates a new internal node.
    /// </summary>
    public BKDTreeInternalNode(int splitDimension, double splitValue, BKDTreeNode<T> left, BKDTreeNode<T> right)
    {
        SplitDimension = splitDimension;
        SplitValue = splitValue;
        Left = left;
        Right = right;
        _pointCount = (left?.PointCount ?? 0) + (right?.PointCount ?? 0);
    }

    /// <summary>
    /// Updates the cached point count.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdatePointCount()
    {
        _pointCount = (Left?.PointCount ?? 0) + (Right?.PointCount ?? 0);
    }
}

/// <summary>
/// Leaf node in the BKD-Tree that stores actual points.
/// </summary>
/// <typeparam name="T">The point type stored in the tree.</typeparam>
internal sealed class BKDTreeLeafNode<T> : BKDTreeNode<T>
{
    /// <summary>
    /// The points stored in this leaf.
    /// </summary>
    public T[] Points;

    /// <summary>
    /// The number of points currently stored.
    /// </summary>
    public int Count;

    /// <inheritdoc/>
    public override bool IsLeaf
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => true;
    }

    /// <inheritdoc/>
    public override long PointCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Count;
    }

    /// <summary>
    /// Creates a new leaf node with the specified capacity.
    /// </summary>
    public BKDTreeLeafNode(int capacity)
    {
        Points = new T[capacity];
        Count = 0;
    }

    /// <summary>
    /// Creates a new leaf node with existing points.
    /// </summary>
    public BKDTreeLeafNode(T[] points, int count)
    {
        Points = points;
        Count = count;
    }
}

#endregion

/// <summary>
/// A high-performance BKD-Tree implementation for spatial indexing with support for
/// range queries and nearest neighbor queries.
/// </summary>
/// <typeparam name="T">The point type stored in the tree.</typeparam>
/// <typeparam name="TPointAccessor">The point accessor type for coordinate access. Struct implementations enable JIT optimizations.</typeparam>
public sealed class LargeBKDTree<T, TPointAccessor> : ILargeCollection<T>
    where TPointAccessor : IPointAccessor<T>
{
    #region Fields

    private readonly TPointAccessor _pointAccessor;
    private readonly int _leafCapacity;
    private readonly int _dimensions;
    private BKDTreeNode<T> _root;
    private long _count;
    private readonly IEqualityComparer<T> _equalityComparer;

    #endregion

    #region Constants

    /// <summary>
    /// The default leaf capacity for BKD-Tree nodes.
    /// </summary>
    public const int DefaultLeafCapacity = LargeBKDTree.DefaultLeafCapacity;

    /// <summary>
    /// The minimum leaf capacity.
    /// </summary>
    public const int MinLeafCapacity = LargeBKDTree.MinLeafCapacity;

    #endregion

    #region Internal Distance Calculation

    /// <summary>
    /// Calculates the squared Euclidean distance between two points using the point accessor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double CalculateDistanceSquared(T a, T b)
    {
        double sum = 0;
        for (int d = 0; d < _dimensions; d++)
        {
            double diff = _pointAccessor.GetCoordinate(a, d) - _pointAccessor.GetCoordinate(b, d);
            sum += diff * diff;
        }
        return sum;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new empty BKD-Tree.
    /// </summary>
    /// <param name="pointAccessor">The point accessor for coordinate access.</param>
    /// <param name="leafCapacity">The maximum number of points per leaf node.</param>
    /// <param name="equalityComparer">Optional equality comparer for point comparison. If null, default comparer is used.</param>
    public LargeBKDTree(TPointAccessor pointAccessor, int leafCapacity = DefaultLeafCapacity, IEqualityComparer<T> equalityComparer = null)
    {
        if (leafCapacity < MinLeafCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(leafCapacity), $"Leaf capacity must be at least {MinLeafCapacity}.");
        }

        _pointAccessor = pointAccessor;
        _leafCapacity = leafCapacity;
        _dimensions = pointAccessor.Dimensions;
        _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
        _root = null;
        _count = 0;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the number of dimensions of the point space.
    /// </summary>
    public int Dimensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _dimensions;
    }

    /// <summary>
    /// Gets the leaf capacity.
    /// </summary>
    public int LeafCapacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _leafCapacity;
    }

    /// <inheritdoc/>
    public long Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    #endregion

    #region ILargeCollection Implementation

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (_root is null)
        {
            BKDTreeLeafNode<T> leaf = new BKDTreeLeafNode<T>(_leafCapacity);
            leaf.Points[0] = item;
            leaf.Count = 1;
            _root = leaf;
            _count = 1;
            return;
        }

        InsertPoint(item, ref _root, 0);
        _count++;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IEnumerable<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (T item in items)
        {
            Add(item);
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlyLargeSpan<T> items)
    {
        for (long i = 0; i < items.Count; i++)
        {
            Add(items[i]);
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlySpan<T> items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            Add(items[i]);
        }
    }
#endif

    /// <summary>
    /// Performs a bulk insert of points, which is more efficient than adding points one by one.
    /// This rebuilds the tree structure for optimal balance.
    /// </summary>
    /// <param name="items">The points to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BulkAdd(IEnumerable<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        // Collect all existing points
        List<T> allPoints = new List<T>();
        if (_root is not null)
        {
            CollectAllPoints(_root, allPoints);
        }

        // Add new points
        foreach (T item in items)
        {
            allPoints.Add(item);
        }

        // Rebuild tree
        if (allPoints.Count == 0)
        {
            _root = null;
            _count = 0;
            return;
        }

        T[] pointArray = allPoints.ToArray();
        _root = BuildTree(pointArray, 0, pointArray.Length, 0);
        _count = pointArray.Length;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T item)
    {
        return Remove(item, out _);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T item, out T removedItem)
    {
        removedItem = default;
        if (_root is null)
        {
            return false;
        }

        bool found = RemovePoint(item, ref _root, out removedItem);
        if (found)
        {
            _count--;
        }
        return found;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _root = null;
        _count = 0;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        if (_root is null)
        {
            return false;
        }

        return FindPoint(item, _root);
    }

    /// <inheritdoc/>
    public IEnumerable<T> GetAll()
    {
        if (_root is null)
        {
            yield break;
        }

        foreach (T point in EnumerateNode(_root))
        {
            yield return point;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<T> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (_root is null)
        {
            return;
        }

        DoForEachNode(_root, action);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<T>
    {
        if (_root is null)
        {
            return;
        }

        DoForEachNode(ref action, _root);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<T> GetEnumerator()
    {
        return GetAll().GetEnumerator();
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    #region Range Query Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateRangeInputs(T minPoint, T maxPoint)
    {
        ValidatePointDimensions(minPoint, nameof(minPoint));
        ValidatePointDimensions(maxPoint, nameof(maxPoint));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidatePointDimensions(T point, string parameterName)
    {
        if (point is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        try
        {
            _ = _pointAccessor.GetCoordinate(point, _dimensions - 1);
        }
        catch (ArgumentNullException ex)
        {
            throw new ArgumentNullException(parameterName, ex);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ArgumentException($"Point '{parameterName}' must expose {_dimensions} dimensions to be used with this tree.", parameterName, ex);
        }
        catch (IndexOutOfRangeException ex)
        {
            throw new ArgumentException($"Point '{parameterName}' must expose {_dimensions} dimensions to be used with this tree.", parameterName, ex);
        }
    }

    /// <summary>
    /// Performs a range query to find all points within the specified bounding box.
    /// </summary>
    /// <param name="minPoint">The point defining the minimum coordinates of the bounding box.</param>
    /// <param name="maxPoint">The point defining the maximum coordinates of the bounding box.</param>
    /// <returns>All points within the bounding box.</returns>
    public IEnumerable<T> RangeQuery(T minPoint, T maxPoint)
    {
        ValidateRangeInputs(minPoint, maxPoint);

        if (_root is null)
        {
            yield break;
        }

        foreach (T point in RangeQueryNode(_root, minPoint, maxPoint))
        {
            yield return point;
        }
    }

    /// <summary>
    /// Counts the number of points within the specified bounding box.
    /// </summary>
    /// <param name="minPoint">The point defining the minimum coordinates of the bounding box.</param>
    /// <param name="maxPoint">The point defining the maximum coordinates of the bounding box.</param>
    /// <returns>The number of points in the range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long CountInRange(T minPoint, T maxPoint)
    {
        ValidateRangeInputs(minPoint, maxPoint);

        if (_root is null)
        {
            return 0;
        }

        return CountInRangeNode(_root, minPoint, maxPoint);
    }

    /// <summary>
    /// Executes an action for each point within the specified bounding box.
    /// </summary>
    /// <param name="minPoint">The point defining the minimum coordinates of the bounding box.</param>
    /// <param name="maxPoint">The point defining the maximum coordinates of the bounding box.</param>
    /// <param name="action">The action to execute for each point in range.</param>
    public void DoForEachInRange(T minPoint, T maxPoint, Action<T> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        ValidateRangeInputs(minPoint, maxPoint);

        if (_root is null)
        {
            return;
        }

        DoForEachInRangeNode(_root, minPoint, maxPoint, action);
    }

    /// <summary>
    /// Executes an action for each point within the specified bounding box using a struct action for optimal performance.
    /// </summary>
    /// <typeparam name="TAction">The action type implementing <see cref="ILargeAction{T}"/>.</typeparam>
    /// <param name="minPoint">The point defining the minimum coordinates of the bounding box.</param>
    /// <param name="maxPoint">The point defining the maximum coordinates of the bounding box.</param>
    /// <param name="action">The action instance passed by reference.</param>
    public void DoForEachInRange<TAction>(T minPoint, T maxPoint, ref TAction action) where TAction : ILargeAction<T>
    {
        ValidateRangeInputs(minPoint, maxPoint);

        if (_root is null)
        {
            return;
        }

        DoForEachInRangeNode(ref action, _root, minPoint, maxPoint);
    }

    /// <summary>
    /// Tries to get the first point within the specified bounding box.
    /// This is an optimized method that stops immediately when a match is found.
    /// </summary>
    /// <param name="minPoint">The point defining the minimum coordinates of the bounding box.</param>
    /// <param name="maxPoint">The point defining the maximum coordinates of the bounding box.</param>
    /// <param name="firstPoint">The first point found in range, or default if none found.</param>
    /// <returns>true if a point was found; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetFirstInRange(T minPoint, T maxPoint, out T firstPoint)
    {
        firstPoint = default;

        ValidateRangeInputs(minPoint, maxPoint);

        if (_root is null)
        {
            return false;
        }

        return TryGetFirstInRangeNode(_root, minPoint, maxPoint, out firstPoint);
    }

    private bool TryGetFirstInRangeNode(BKDTreeNode<T> node, T minPoint, T maxPoint, out T firstPoint)
    {
        firstPoint = default;

        if (node is BKDTreeLeafNode<T> leaf)
        {
            T[] points = leaf.Points;
            int count = leaf.Count;
            for (int i = 0; i < count; i++)
            {
                T point = points[i];
                if (PointInRange(point, minPoint, maxPoint))
                {
                    firstPoint = point;
                    return true;
                }
            }
            return false;
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            int dim = internalNode.SplitDimension;
            double splitVal = internalNode.SplitValue;
            double minCoord = _pointAccessor.GetCoordinate(minPoint, dim);
            double maxCoord = _pointAccessor.GetCoordinate(maxPoint, dim);

            // Search left subtree first
            if (minCoord <= splitVal && internalNode.Left is not null)
            {
                if (TryGetFirstInRangeNode(internalNode.Left, minPoint, maxPoint, out firstPoint))
                {
                    return true;
                }
            }
            // Then search right subtree
            if (maxCoord > splitVal && internalNode.Right is not null)
            {
                if (TryGetFirstInRangeNode(internalNode.Right, minPoint, maxPoint, out firstPoint))
                {
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Nearest Neighbor Query Methods

    /// <summary>
    /// Finds the nearest neighbor to the specified query point.
    /// </summary>
    /// <param name="queryPoint">The query point.</param>
    /// <returns>The nearest point, or default if the tree is empty.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T NearestNeighbor(T queryPoint)
    {
        if (!TryGetNearestNeighbor(queryPoint, out T result))
        {
            throw new InvalidOperationException("The tree is empty.");
        }
        return result;
    }

    /// <summary>
    /// Tries to find the nearest neighbor to the specified query point.
    /// </summary>
    /// <param name="queryPoint">The query point.</param>
    /// <param name="nearestPoint">The nearest point if found.</param>
    /// <returns>true if a nearest neighbor was found; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetNearestNeighbor(T queryPoint, out T nearestPoint)
    {
        nearestPoint = default;
        if (_root is null)
        {
            return false;
        }

        double bestDistanceSquared = double.MaxValue;
        T bestPoint = default;
        bool found = false;

        NearestNeighborSearch(_root, queryPoint, ref bestPoint, ref bestDistanceSquared, ref found);

        if (found)
        {
            nearestPoint = bestPoint;
        }
        return found;
    }

    /// <summary>
    /// Finds the nearest neighbor to the specified query point and returns the distance.
    /// </summary>
    /// <param name="queryPoint">The query point.</param>
    /// <param name="nearestPoint">The nearest point.</param>
    /// <param name="distanceSquared">The squared distance to the nearest point.</param>
    /// <returns>true if a nearest neighbor was found; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetNearestNeighborWithDistance(T queryPoint, out T nearestPoint, out double distanceSquared)
    {
        nearestPoint = default;
        distanceSquared = double.MaxValue;

        if (_root is null)
        {
            return false;
        }

        T bestPoint = default;
        double bestDistanceSquared = double.MaxValue;
        bool found = false;

        NearestNeighborSearch(_root, queryPoint, ref bestPoint, ref bestDistanceSquared, ref found);

        if (found)
        {
            nearestPoint = bestPoint;
            distanceSquared = bestDistanceSquared;
        }
        return found;
    }

    /// <summary>
    /// Finds all points within the specified distance from the query point.
    /// </summary>
    /// <param name="queryPoint">The query point.</param>
    /// <param name="maxDistanceSquared">The maximum squared distance.</param>
    /// <returns>All points within the specified distance.</returns>
    public IEnumerable<T> FindPointsWithinDistance(T queryPoint, double maxDistanceSquared)
    {
        if (maxDistanceSquared < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDistanceSquared), "Distance squared must be non-negative.");
        }

        if (_root is null)
        {
            yield break;
        }

        foreach (T point in FindPointsWithinDistanceNode(_root, queryPoint, maxDistanceSquared))
        {
            yield return point;
        }
    }

    #endregion

    #region Private Helper Methods - Tree Building

    private BKDTreeNode<T> BuildTree(T[] points, int start, int end, int depth)
    {
        int count = end - start;
        if (count == 0)
        {
            return null;
        }

        if (count <= _leafCapacity)
        {
            T[] leafPoints = new T[_leafCapacity];
            Array.Copy(points, start, leafPoints, 0, count);
            return new BKDTreeLeafNode<T>(leafPoints, count);
        }

        int splitDimension = depth % _dimensions;

        // Sort by split dimension and find median
        SortByDimension(points, start, end, splitDimension);
        int medianIndex = start + count / 2;
        double splitValue = _pointAccessor.GetCoordinate(points[medianIndex], splitDimension);

        BKDTreeNode<T> left = BuildTree(points, start, medianIndex, depth + 1);
        BKDTreeNode<T> right = BuildTree(points, medianIndex, end, depth + 1);

        return new BKDTreeInternalNode<T>(splitDimension, splitValue, left, right);
    }

    private void SortByDimension(T[] points, int start, int end, int dimension)
    {
        // Simple quicksort by dimension
        if (end - start <= 1)
        {
            return;
        }

        QuickSortByDimension(points, start, end - 1, dimension);
    }

    private void QuickSortByDimension(T[] points, int left, int right, int dimension)
    {
        if (left >= right)
        {
            return;
        }

        int pivotIndex = Partition(points, left, right, dimension);
        QuickSortByDimension(points, left, pivotIndex - 1, dimension);
        QuickSortByDimension(points, pivotIndex + 1, right, dimension);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Partition(T[] points, int left, int right, int dimension)
    {
        double pivotValue = _pointAccessor.GetCoordinate(points[right], dimension);
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (_pointAccessor.GetCoordinate(points[j], dimension) <= pivotValue)
            {
                i++;
                T temp = points[i];
                points[i] = points[j];
                points[j] = temp;
            }
        }

        T temp2 = points[i + 1];
        points[i + 1] = points[right];
        points[right] = temp2;

        return i + 1;
    }

    #endregion

    #region Private Helper Methods - Insertion

    private void InsertPoint(T point, ref BKDTreeNode<T> node, int depth)
    {
        if (node is BKDTreeLeafNode<T> leaf)
        {
            if (leaf.Count < _leafCapacity)
            {
                leaf.Points[leaf.Count] = point;
                leaf.Count++;
            }
            else
            {
                // Split the leaf
                node = SplitLeafAndInsert(leaf, point, depth);
            }
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            double pointCoord = _pointAccessor.GetCoordinate(point, internalNode.SplitDimension);
            if (pointCoord <= internalNode.SplitValue)
            {
                InsertPoint(point, ref internalNode.Left, depth + 1);
            }
            else
            {
                InsertPoint(point, ref internalNode.Right, depth + 1);
            }
            internalNode.UpdatePointCount();
        }
    }

    private BKDTreeNode<T> SplitLeafAndInsert(BKDTreeLeafNode<T> leaf, T newPoint, int depth)
    {
        // Create array with all points including new one
        T[] allPoints = new T[leaf.Count + 1];
        Array.Copy(leaf.Points, 0, allPoints, 0, leaf.Count);
        allPoints[leaf.Count] = newPoint;

        // Build subtree from all points
        return BuildTree(allPoints, 0, allPoints.Length, depth);
    }

    #endregion

    #region Private Helper Methods - Removal

    private bool RemovePoint(T point, ref BKDTreeNode<T> node, out T removedPoint)
    {
        removedPoint = default;

        if (node is BKDTreeLeafNode<T> leaf)
        {
            for (int i = 0; i < leaf.Count; i++)
            {
                if (_equalityComparer.Equals(leaf.Points[i], point))
                {
                    removedPoint = leaf.Points[i];
                    // Shift remaining points
                    for (int j = i; j < leaf.Count - 1; j++)
                    {
                        leaf.Points[j] = leaf.Points[j + 1];
                    }
                    leaf.Points[leaf.Count - 1] = default;
                    leaf.Count--;

                    // If leaf becomes empty, set node to null
                    if (leaf.Count == 0)
                    {
                        node = null;
                    }
                    return true;
                }
            }
            return false;
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            bool found = false;
            double pointCoord = _pointAccessor.GetCoordinate(point, internalNode.SplitDimension);

            // Try left subtree first if point could be there
            if (pointCoord <= internalNode.SplitValue && internalNode.Left is not null)
            {
                found = RemovePoint(point, ref internalNode.Left, out removedPoint);
            }

            // If not found and point could be in right subtree
            if (!found && internalNode.Right is not null)
            {
                found = RemovePoint(point, ref internalNode.Right, out removedPoint);
            }

            if (found)
            {
                internalNode.UpdatePointCount();

                // Collapse if only one child remains
                if (internalNode.Left is null && internalNode.Right is not null)
                {
                    node = internalNode.Right;
                }
                else if (internalNode.Right is null && internalNode.Left is not null)
                {
                    node = internalNode.Left;
                }
                else if (internalNode.Left is null && internalNode.Right is null)
                {
                    node = null;
                }
            }

            return found;
        }

        return false;
    }

    #endregion

    #region Private Helper Methods - Search

    private bool FindPoint(T point, BKDTreeNode<T> node)
    {
        if (node is BKDTreeLeafNode<T> leaf)
        {
            for (int i = 0; i < leaf.Count; i++)
            {
                if (_equalityComparer.Equals(leaf.Points[i], point))
                {
                    return true;
                }
            }
            return false;
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            double pointCoord = _pointAccessor.GetCoordinate(point, internalNode.SplitDimension);

            // Point could be in either subtree due to duplicates
            if (pointCoord <= internalNode.SplitValue && internalNode.Left is not null)
            {
                if (FindPoint(point, internalNode.Left))
                {
                    return true;
                }
            }
            if (internalNode.Right is not null)
            {
                if (FindPoint(point, internalNode.Right))
                {
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Private Helper Methods - Enumeration

    private IEnumerable<T> EnumerateNode(BKDTreeNode<T> node)
    {
        if (node is BKDTreeLeafNode<T> leaf)
        {
            for (int i = 0; i < leaf.Count; i++)
            {
                yield return leaf.Points[i];
            }
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            if (internalNode.Left is not null)
            {
                foreach (T point in EnumerateNode(internalNode.Left))
                {
                    yield return point;
                }
            }
            if (internalNode.Right is not null)
            {
                foreach (T point in EnumerateNode(internalNode.Right))
                {
                    yield return point;
                }
            }
        }
    }

    private void DoForEachNode(BKDTreeNode<T> node, Action<T> action)
    {
        if (node is BKDTreeLeafNode<T> leaf)
        {
            for (int i = 0; i < leaf.Count; i++)
            {
                action(leaf.Points[i]);
            }
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            if (internalNode.Left is not null)
            {
                DoForEachNode(internalNode.Left, action);
            }
            if (internalNode.Right is not null)
            {
                DoForEachNode(internalNode.Right, action);
            }
        }
    }

    private void DoForEachNode<TAction>(ref TAction action, BKDTreeNode<T> node) where TAction : ILargeAction<T>
    {
        if (node is BKDTreeLeafNode<T> leaf)
        {
            for (int i = 0; i < leaf.Count; i++)
            {
                action.Invoke(leaf.Points[i]);
            }
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            if (internalNode.Left is not null)
            {
                DoForEachNode(ref action, internalNode.Left);
            }
            if (internalNode.Right is not null)
            {
                DoForEachNode(ref action, internalNode.Right);
            }
        }
    }

    private void CollectAllPoints(BKDTreeNode<T> node, List<T> points)
    {
        if (node is BKDTreeLeafNode<T> leaf)
        {
            for (int i = 0; i < leaf.Count; i++)
            {
                points.Add(leaf.Points[i]);
            }
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            if (internalNode.Left is not null)
            {
                CollectAllPoints(internalNode.Left, points);
            }
            if (internalNode.Right is not null)
            {
                CollectAllPoints(internalNode.Right, points);
            }
        }
    }

    #endregion

    #region Private Helper Methods - Range Query

    private IEnumerable<T> RangeQueryNode(BKDTreeNode<T> node, T minPoint, T maxPoint)
    {
        if (node is BKDTreeLeafNode<T> leaf)
        {
            for (int i = 0; i < leaf.Count; i++)
            {
                if (PointInRange(leaf.Points[i], minPoint, maxPoint))
                {
                    yield return leaf.Points[i];
                }
            }
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            int dim = internalNode.SplitDimension;
            double splitVal = internalNode.SplitValue;
            double minCoord = _pointAccessor.GetCoordinate(minPoint, dim);
            double maxCoord = _pointAccessor.GetCoordinate(maxPoint, dim);

            // Check left subtree if range overlaps
            if (minCoord <= splitVal && internalNode.Left is not null)
            {
                foreach (T point in RangeQueryNode(internalNode.Left, minPoint, maxPoint))
                {
                    yield return point;
                }
            }

            // Check right subtree if range overlaps
            if (maxCoord > splitVal && internalNode.Right is not null)
            {
                foreach (T point in RangeQueryNode(internalNode.Right, minPoint, maxPoint))
                {
                    yield return point;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool PointInRange(T point, T minPoint, T maxPoint)
    {
        for (int d = 0; d < _dimensions; d++)
        {
            double coord = _pointAccessor.GetCoordinate(point, d);
            double minCoord = _pointAccessor.GetCoordinate(minPoint, d);
            double maxCoord = _pointAccessor.GetCoordinate(maxPoint, d);
            if (coord < minCoord || coord > maxCoord)
            {
                return false;
            }
        }
        return true;
    }

    private long CountInRangeNode(BKDTreeNode<T> node, T minPoint, T maxPoint)
    {
        if (node is BKDTreeLeafNode<T> leaf)
        {
            long count = 0;
            for (int i = 0; i < leaf.Count; i++)
            {
                if (PointInRange(leaf.Points[i], minPoint, maxPoint))
                {
                    count++;
                }
            }
            return count;
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            long count = 0;
            int dim = internalNode.SplitDimension;
            double splitVal = internalNode.SplitValue;
            double minCoord = _pointAccessor.GetCoordinate(minPoint, dim);
            double maxCoord = _pointAccessor.GetCoordinate(maxPoint, dim);

            if (minCoord <= splitVal && internalNode.Left is not null)
            {
                count += CountInRangeNode(internalNode.Left, minPoint, maxPoint);
            }

            if (maxCoord > splitVal && internalNode.Right is not null)
            {
                count += CountInRangeNode(internalNode.Right, minPoint, maxPoint);
            }

            return count;
        }

        return 0;
    }

    private void DoForEachInRangeNode(BKDTreeNode<T> node, T minPoint, T maxPoint, Action<T> action)
    {
        if (node is BKDTreeLeafNode<T> leaf)
        {
            for (int i = 0; i < leaf.Count; i++)
            {
                if (PointInRange(leaf.Points[i], minPoint, maxPoint))
                {
                    action(leaf.Points[i]);
                }
            }
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            int dim = internalNode.SplitDimension;
            double splitVal = internalNode.SplitValue;
            double minCoord = _pointAccessor.GetCoordinate(minPoint, dim);
            double maxCoord = _pointAccessor.GetCoordinate(maxPoint, dim);

            if (minCoord <= splitVal && internalNode.Left is not null)
            {
                DoForEachInRangeNode(internalNode.Left, minPoint, maxPoint, action);
            }

            if (maxCoord > splitVal && internalNode.Right is not null)
            {
                DoForEachInRangeNode(internalNode.Right, minPoint, maxPoint, action);
            }
        }
    }

    private void DoForEachInRangeNode<TAction>(ref TAction action, BKDTreeNode<T> node, T minPoint, T maxPoint) where TAction : ILargeAction<T>
    {
        if (node is BKDTreeLeafNode<T> leaf)
        {
            for (int i = 0; i < leaf.Count; i++)
            {
                if (PointInRange(leaf.Points[i], minPoint, maxPoint))
                {
                    action.Invoke(leaf.Points[i]);
                }
            }
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            int dim = internalNode.SplitDimension;
            double splitVal = internalNode.SplitValue;
            double minCoord = _pointAccessor.GetCoordinate(minPoint, dim);
            double maxCoord = _pointAccessor.GetCoordinate(maxPoint, dim);

            if (minCoord <= splitVal && internalNode.Left is not null)
            {
                DoForEachInRangeNode(ref action, internalNode.Left, minPoint, maxPoint);
            }

            if (maxCoord > splitVal && internalNode.Right is not null)
            {
                DoForEachInRangeNode(ref action, internalNode.Right, minPoint, maxPoint);
            }
        }
    }

    #endregion

    #region Private Helper Methods - Nearest Neighbor

    private void NearestNeighborSearch(BKDTreeNode<T> node, T queryPoint, ref T bestPoint, ref double bestDistanceSquared, ref bool found)
    {
        if (node is BKDTreeLeafNode<T> leaf)
        {
            for (int i = 0; i < leaf.Count; i++)
            {
                double distSq = CalculateDistanceSquared(queryPoint, leaf.Points[i]);
                if (distSq < bestDistanceSquared)
                {
                    bestDistanceSquared = distSq;
                    bestPoint = leaf.Points[i];
                    found = true;
                }
            }
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            int dim = internalNode.SplitDimension;
            double queryCoord = _pointAccessor.GetCoordinate(queryPoint, dim);
            double splitVal = internalNode.SplitValue;

            // Determine which side to search first
            BKDTreeNode<T> firstSide = queryCoord <= splitVal ? internalNode.Left : internalNode.Right;
            BKDTreeNode<T> secondSide = queryCoord <= splitVal ? internalNode.Right : internalNode.Left;

            // Search the closer side first
            if (firstSide is not null)
            {
                NearestNeighborSearch(firstSide, queryPoint, ref bestPoint, ref bestDistanceSquared, ref found);
            }

            // Check if we need to search the other side
            double splitDist = queryCoord - splitVal;
            double splitDistSquared = splitDist * splitDist;

            if (secondSide is not null && splitDistSquared < bestDistanceSquared)
            {
                NearestNeighborSearch(secondSide, queryPoint, ref bestPoint, ref bestDistanceSquared, ref found);
            }
        }
    }

    private IEnumerable<T> FindPointsWithinDistanceNode(BKDTreeNode<T> node, T queryPoint, double maxDistanceSquared)
    {
        if (node is BKDTreeLeafNode<T> leaf)
        {
            for (int i = 0; i < leaf.Count; i++)
            {
                double distSq = CalculateDistanceSquared(queryPoint, leaf.Points[i]);
                if (distSq <= maxDistanceSquared)
                {
                    yield return leaf.Points[i];
                }
            }
        }
        else if (node is BKDTreeInternalNode<T> internalNode)
        {
            int dim = internalNode.SplitDimension;
            double queryCoord = _pointAccessor.GetCoordinate(queryPoint, dim);
            double splitVal = internalNode.SplitValue;
            double splitDist = queryCoord - splitVal;
            double splitDistSquared = splitDist * splitDist;

            // Check left subtree if it could contain points within distance
            if (internalNode.Left is not null && (queryCoord <= splitVal || splitDistSquared <= maxDistanceSquared))
            {
                foreach (T point in FindPointsWithinDistanceNode(internalNode.Left, queryPoint, maxDistanceSquared))
                {
                    yield return point;
                }
            }

            // Check right subtree if it could contain points within distance
            if (internalNode.Right is not null && (queryCoord > splitVal || splitDistSquared <= maxDistanceSquared))
            {
                foreach (T point in FindPointsWithinDistanceNode(internalNode.Right, queryPoint, maxDistanceSquared))
                {
                    yield return point;
                }
            }
        }
    }

    #endregion
}
