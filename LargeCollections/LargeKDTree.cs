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
/// An ultra high-performance, allocation-free KD-Tree implementation for spatial indexing.
/// Uses a flat array layout (implicit tree structure) for optimal cache efficiency and zero GC pressure during queries.
/// This is a read-only data structure - points must be provided at construction time.
/// </summary>
/// <typeparam name="T">The point type stored in the tree.</typeparam>
/// <typeparam name="TPointAccessor">The point accessor type for coordinate access. Struct implementations enable JIT optimizations.</typeparam>
/// <remarks>
/// <para>
/// The KD-Tree uses an implicit binary tree layout where nodes are stored in a contiguous array:
/// - Root is at index 0
/// - Left child of node i is at index 2*i + 1
/// - Right child of node i is at index 2*i + 2
/// - Parent of node i is at index (i - 1) / 2
/// </para>
/// <para>
/// This layout eliminates pointer chasing and enables excellent CPU cache utilization.
/// The tree is built using median-of-medians partitioning for optimal balance.
/// </para>
/// </remarks>
public sealed class LargeKDTree<T, TPointAccessor> : IReadOnlyLargeCollection<T>
    where TPointAccessor : IPointAccessor<T>
{
    #region Fields

    private readonly TPointAccessor _pointAccessor;
    private readonly int _dimensions;
    private readonly T[] _points;
    private readonly long _count;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new KD-Tree from the specified points.
    /// The tree is built during construction and cannot be modified afterwards.
    /// </summary>
    /// <param name="pointAccessor">The point accessor for coordinate access.</param>
    /// <param name="points">The points to store in the tree. The array will be modified during construction.</param>
    public LargeKDTree(TPointAccessor pointAccessor, T[] points)
    {
        if (points is null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        _pointAccessor = pointAccessor;
        _dimensions = pointAccessor.Dimensions;
        _count = points.Length;

        if (_count == 0)
        {
            _points = Array.Empty<T>();
            return;
        }

        // Build the tree in-place using the input array
        _points = new T[_count];
        Array.Copy(points, _points, _count);
        BuildTree(0, (int)_count, 0);
    }

    /// <summary>
    /// Creates a new KD-Tree from the specified enumerable of points.
    /// </summary>
    /// <param name="pointAccessor">The point accessor for coordinate access.</param>
    /// <param name="points">The points to store in the tree.</param>
    public LargeKDTree(TPointAccessor pointAccessor, IEnumerable<T> points)
    {
        if (points is null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        _pointAccessor = pointAccessor;
        _dimensions = pointAccessor.Dimensions;

        // Convert to array
        if (points is ICollection<T> collection)
        {
            _points = new T[collection.Count];
            collection.CopyTo(_points, 0);
        }
        else
        {
            List<T> list = new List<T>(points);
            _points = list.ToArray();
        }

        _count = _points.Length;

        if (_count > 0)
        {
            BuildTree(0, (int)_count, 0);
        }
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

    /// <inheritdoc/>
    public long Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    #endregion

    #region IReadOnlyLargeCollection Implementation

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        if (_count == 0)
        {
            return false;
        }

        return FindPoint(item, 0, (int)_count, 0);
    }

    /// <inheritdoc/>
    public IEnumerable<T> GetAll()
    {
        // Return points in tree order (which is a valid traversal)
        for (long i = 0; i < _count; i++)
        {
            yield return _points[i];
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

        T[] points = _points;
        int count = (int)_count;
        for (int i = 0; i < count; i++)
        {
            action(points[i]);
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TAction>(ref TAction action) where TAction : ILargeAction<T>
    {
        T[] points = _points;
        int count = (int)_count;
        for (int i = 0; i < count; i++)
        {
            action.Invoke(points[i]);
        }
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

    /// <summary>
    /// Performs a range query to find all points within the specified bounding box.
    /// </summary>
    /// <param name="minPoint">The point defining the minimum coordinates of the bounding box.</param>
    /// <param name="maxPoint">The point defining the maximum coordinates of the bounding box.</param>
    /// <returns>All points within the bounding box.</returns>
    public IEnumerable<T> RangeQuery(T minPoint, T maxPoint)
    {
        if (_count == 0)
        {
            yield break;
        }

        foreach (T point in RangeQueryInternal(0, (int)_count, 0, minPoint, maxPoint))
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
        if (_count == 0)
        {
            return 0;
        }

        return CountInRangeInternal(0, (int)_count, 0, minPoint, maxPoint);
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

        if (_count == 0)
        {
            return;
        }

        DoForEachInRangeInternal(0, (int)_count, 0, minPoint, maxPoint, action);
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
        if (_count == 0)
        {
            return;
        }

        DoForEachInRangeInternal(ref action, 0, (int)_count, 0, minPoint, maxPoint);
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

        if (_count == 0)
        {
            return false;
        }

        return TryGetFirstInRangeInternal(0, (int)_count, 0, minPoint, maxPoint, out firstPoint);
    }

    #endregion

    #region Nearest Neighbor Query Methods

    /// <summary>
    /// Finds the nearest neighbor to the specified query point.
    /// </summary>
    /// <param name="queryPoint">The query point.</param>
    /// <returns>The nearest point.</returns>
    /// <exception cref="InvalidOperationException">The tree is empty.</exception>
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
        if (_count == 0)
        {
            return false;
        }

        double bestDistanceSquared = double.MaxValue;
        T bestPoint = default;
        bool found = false;

        NearestNeighborSearch(0, (int)_count, 0, queryPoint, ref bestPoint, ref bestDistanceSquared, ref found);

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

        if (_count == 0)
        {
            return false;
        }

        T bestPoint = default;
        double bestDistanceSquared = double.MaxValue;
        bool found = false;

        NearestNeighborSearch(0, (int)_count, 0, queryPoint, ref bestPoint, ref bestDistanceSquared, ref found);

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

        if (_count == 0)
        {
            yield break;
        }

        foreach (T point in FindPointsWithinDistanceInternal(0, (int)_count, 0, queryPoint, maxDistanceSquared))
        {
            yield return point;
        }
    }

    /// <summary>
    /// Finds the k nearest neighbors to the specified query point.
    /// </summary>
    /// <param name="queryPoint">The query point.</param>
    /// <param name="k">The number of neighbors to find.</param>
    /// <returns>The k nearest points, ordered by distance (closest first).</returns>
    public IEnumerable<T> FindKNearestNeighbors(T queryPoint, int k)
    {
        if (k <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(k), "k must be positive.");
        }

        if (_count == 0)
        {
            yield break;
        }

        // Use a max-heap to track the k nearest points
        // The heap stores (distance, point) pairs with the farthest point at the root
        List<(double distanceSquared, T point)> heap = new List<(double, T)>(k + 1);

        KNearestNeighborsSearch(0, (int)_count, 0, queryPoint, k, heap);

        // Sort by distance and return
        heap.Sort((a, b) => a.distanceSquared.CompareTo(b.distanceSquared));
        foreach ((double _, T point) in heap)
        {
            yield return point;
        }
    }

    #endregion

    #region Private Helper Methods - Tree Building

    /// <summary>
    /// Builds the KD-Tree in-place using the QuickSelect algorithm for O(n log n) construction.
    /// Points are partitioned so that the median of each region is at the correct position.
    /// The tree structure is implicit: for a region [start, end), the root is at (start + end) / 2.
    /// </summary>
    private void BuildTree(int start, int end, int depth)
    {
        int count = end - start;
        if (count <= 1)
        {
            return;
        }

        int dim = depth % _dimensions;
        int medianIndex = start + count / 2;

        // Use QuickSelect to partition around the median
        QuickSelect(start, end - 1, medianIndex, dim);

        // Recursively build left and right subtrees
        BuildTree(start, medianIndex, depth + 1);
        BuildTree(medianIndex + 1, end, depth + 1);
    }

    /// <summary>
    /// QuickSelect algorithm to find the k-th smallest element and partition the array.
    /// </summary>
    private void QuickSelect(int left, int right, int k, int dimension)
    {
        while (left < right)
        {
            int pivotIndex = Partition(left, right, dimension);

            if (pivotIndex == k)
            {
                return;
            }
            else if (k < pivotIndex)
            {
                right = pivotIndex - 1;
            }
            else
            {
                left = pivotIndex + 1;
            }
        }
    }

    /// <summary>
    /// Partitions the array around a pivot element.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Partition(int left, int right, int dimension)
    {
        // Use median-of-three for pivot selection to avoid worst-case
        int mid = left + (right - left) / 2;
        double leftVal = _pointAccessor.GetCoordinate(_points[left], dimension);
        double midVal = _pointAccessor.GetCoordinate(_points[mid], dimension);
        double rightVal = _pointAccessor.GetCoordinate(_points[right], dimension);

        // Find median of three
        int pivotIndex;
        if ((leftVal <= midVal && midVal <= rightVal) || (rightVal <= midVal && midVal <= leftVal))
        {
            pivotIndex = mid;
        }
        else if ((midVal <= leftVal && leftVal <= rightVal) || (rightVal <= leftVal && leftVal <= midVal))
        {
            pivotIndex = left;
        }
        else
        {
            pivotIndex = right;
        }

        // Move pivot to right
        if (pivotIndex != right)
        {
            T temp = _points[pivotIndex];
            _points[pivotIndex] = _points[right];
            _points[right] = temp;
        }

        double pivotValue = _pointAccessor.GetCoordinate(_points[right], dimension);
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (_pointAccessor.GetCoordinate(_points[j], dimension) <= pivotValue)
            {
                i++;
                T temp = _points[i];
                _points[i] = _points[j];
                _points[j] = temp;
            }
        }

        T temp2 = _points[i + 1];
        _points[i + 1] = _points[right];
        _points[right] = temp2;

        return i + 1;
    }

    #endregion

    #region Private Helper Methods - Distance Calculation

    /// <summary>
    /// Calculates the squared Euclidean distance between two points.
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

    /// <summary>
    /// Checks if all coordinates of a point are equal.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool PointsEqual(T a, T b)
    {
        for (int d = 0; d < _dimensions; d++)
        {
            if (_pointAccessor.GetCoordinate(a, d) != _pointAccessor.GetCoordinate(b, d))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Checks if a point is within the specified bounding box.
    /// </summary>
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

    #endregion

    #region Private Helper Methods - Search

    /// <summary>
    /// Finds a point in the tree using range-based navigation.
    /// For a range [start, end), the root is at mid = start + (end - start) / 2.
    /// Left subtree: [start, mid), Right subtree: [mid + 1, end)
    /// </summary>
    private bool FindPoint(T target, int start, int end, int depth)
    {
        if (start >= end)
        {
            return false;
        }

        int mid = start + (end - start) / 2;
        T point = _points[mid];

        if (PointsEqual(point, target))
        {
            return true;
        }

        int dim = depth % _dimensions;
        double targetCoord = _pointAccessor.GetCoordinate(target, dim);
        double pointCoord = _pointAccessor.GetCoordinate(point, dim);

        if (targetCoord <= pointCoord)
        {
            // Search left first
            if (FindPoint(target, start, mid, depth + 1))
            {
                return true;
            }
            // Also check right subtree (for points with equal coordinates)
            return FindPoint(target, mid + 1, end, depth + 1);
        }
        else
        {
            // Search right first
            if (FindPoint(target, mid + 1, end, depth + 1))
            {
                return true;
            }
            // Also check left subtree (for points with equal coordinates)
            return FindPoint(target, start, mid, depth + 1);
        }
    }

    #endregion

    #region Private Helper Methods - Range Query

    private long CountInRangeInternal(int start, int end, int depth, T minPoint, T maxPoint)
    {
        if (start >= end)
        {
            return 0;
        }

        int mid = start + (end - start) / 2;
        T point = _points[mid];
        long count = 0;

        if (PointInRange(point, minPoint, maxPoint))
        {
            count = 1;
        }

        int dim = depth % _dimensions;
        double coord = _pointAccessor.GetCoordinate(point, dim);
        double minCoord = _pointAccessor.GetCoordinate(minPoint, dim);
        double maxCoord = _pointAccessor.GetCoordinate(maxPoint, dim);

        // Search left subtree if range overlaps
        if (start < mid && minCoord <= coord)
        {
            count += CountInRangeInternal(start, mid, depth + 1, minPoint, maxPoint);
        }

        // Search right subtree if range overlaps
        if (mid + 1 < end && maxCoord >= coord)
        {
            count += CountInRangeInternal(mid + 1, end, depth + 1, minPoint, maxPoint);
        }

        return count;
    }

    private void DoForEachInRangeInternal(int start, int end, int depth, T minPoint, T maxPoint, Action<T> action)
    {
        if (start >= end)
        {
            return;
        }

        int mid = start + (end - start) / 2;
        T point = _points[mid];

        if (PointInRange(point, minPoint, maxPoint))
        {
            action(point);
        }

        int dim = depth % _dimensions;
        double coord = _pointAccessor.GetCoordinate(point, dim);
        double minCoord = _pointAccessor.GetCoordinate(minPoint, dim);
        double maxCoord = _pointAccessor.GetCoordinate(maxPoint, dim);

        if (start < mid && minCoord <= coord)
        {
            DoForEachInRangeInternal(start, mid, depth + 1, minPoint, maxPoint, action);
        }

        if (mid + 1 < end && maxCoord >= coord)
        {
            DoForEachInRangeInternal(mid + 1, end, depth + 1, minPoint, maxPoint, action);
        }
    }

    private void DoForEachInRangeInternal<TAction>(ref TAction action, int start, int end, int depth, T minPoint, T maxPoint) where TAction : ILargeAction<T>
    {
        if (start >= end)
        {
            return;
        }

        int mid = start + (end - start) / 2;
        T point = _points[mid];

        if (PointInRange(point, minPoint, maxPoint))
        {
            action.Invoke(point);
        }

        int dim = depth % _dimensions;
        double coord = _pointAccessor.GetCoordinate(point, dim);
        double minCoord = _pointAccessor.GetCoordinate(minPoint, dim);
        double maxCoord = _pointAccessor.GetCoordinate(maxPoint, dim);

        if (start < mid && minCoord <= coord)
        {
            DoForEachInRangeInternal(ref action, start, mid, depth + 1, minPoint, maxPoint);
        }

        if (mid + 1 < end && maxCoord >= coord)
        {
            DoForEachInRangeInternal(ref action, mid + 1, end, depth + 1, minPoint, maxPoint);
        }
    }

    private bool TryGetFirstInRangeInternal(int start, int end, int depth, T minPoint, T maxPoint, out T firstPoint)
    {
        firstPoint = default;

        if (start >= end)
        {
            return false;
        }

        int mid = start + (end - start) / 2;
        T point = _points[mid];

        if (PointInRange(point, minPoint, maxPoint))
        {
            firstPoint = point;
            return true;
        }

        int dim = depth % _dimensions;
        double coord = _pointAccessor.GetCoordinate(point, dim);
        double minCoord = _pointAccessor.GetCoordinate(minPoint, dim);
        double maxCoord = _pointAccessor.GetCoordinate(maxPoint, dim);

        if (start < mid && minCoord <= coord)
        {
            if (TryGetFirstInRangeInternal(start, mid, depth + 1, minPoint, maxPoint, out firstPoint))
            {
                return true;
            }
        }

        if (mid + 1 < end && maxCoord >= coord)
        {
            if (TryGetFirstInRangeInternal(mid + 1, end, depth + 1, minPoint, maxPoint, out firstPoint))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerable<T> RangeQueryInternal(int start, int end, int depth, T minPoint, T maxPoint)
    {
        if (start >= end)
        {
            yield break;
        }

        int mid = start + (end - start) / 2;
        T point = _points[mid];

        if (PointInRange(point, minPoint, maxPoint))
        {
            yield return point;
        }

        int dim = depth % _dimensions;
        double coord = _pointAccessor.GetCoordinate(point, dim);
        double minCoord = _pointAccessor.GetCoordinate(minPoint, dim);
        double maxCoord = _pointAccessor.GetCoordinate(maxPoint, dim);

        if (start < mid && minCoord <= coord)
        {
            foreach (T p in RangeQueryInternal(start, mid, depth + 1, minPoint, maxPoint))
            {
                yield return p;
            }
        }

        if (mid + 1 < end && maxCoord >= coord)
        {
            foreach (T p in RangeQueryInternal(mid + 1, end, depth + 1, minPoint, maxPoint))
            {
                yield return p;
            }
        }
    }

    #endregion

    #region Private Helper Methods - Nearest Neighbor

    private void NearestNeighborSearch(int start, int end, int depth, T queryPoint, ref T bestPoint, ref double bestDistanceSquared, ref bool found)
    {
        if (start >= end)
        {
            return;
        }

        int mid = start + (end - start) / 2;
        T point = _points[mid];
        double distSquared = CalculateDistanceSquared(queryPoint, point);

        if (distSquared < bestDistanceSquared)
        {
            bestDistanceSquared = distSquared;
            bestPoint = point;
            found = true;
        }

        int dim = depth % _dimensions;
        double queryCoord = _pointAccessor.GetCoordinate(queryPoint, dim);
        double pointCoord = _pointAccessor.GetCoordinate(point, dim);
        double splitDist = queryCoord - pointCoord;
        double splitDistSquared = splitDist * splitDist;

        // Determine which side is closer to the query point
        int firstStart, firstEnd, secondStart, secondEnd;
        if (queryCoord <= pointCoord)
        {
            // Left side is closer
            firstStart = start;
            firstEnd = mid;
            secondStart = mid + 1;
            secondEnd = end;
        }
        else
        {
            // Right side is closer
            firstStart = mid + 1;
            firstEnd = end;
            secondStart = start;
            secondEnd = mid;
        }

        // Search the closer side first
        if (firstStart < firstEnd)
        {
            NearestNeighborSearch(firstStart, firstEnd, depth + 1, queryPoint, ref bestPoint, ref bestDistanceSquared, ref found);
        }

        // Only search the farther side if it could contain closer points
        if (secondStart < secondEnd && splitDistSquared < bestDistanceSquared)
        {
            NearestNeighborSearch(secondStart, secondEnd, depth + 1, queryPoint, ref bestPoint, ref bestDistanceSquared, ref found);
        }
    }

    private void KNearestNeighborsSearch(int start, int end, int depth, T queryPoint, int k, List<(double distanceSquared, T point)> heap)
    {
        if (start >= end)
        {
            return;
        }

        int mid = start + (end - start) / 2;
        T point = _points[mid];
        double distSquared = CalculateDistanceSquared(queryPoint, point);

        // Add to heap if we have fewer than k points, or if this point is closer than the farthest
        if (heap.Count < k)
        {
            heap.Add((distSquared, point));
            // Bubble up (heapify)
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (heap[i].distanceSquared > heap[parent].distanceSquared)
                {
                    (heap[i], heap[parent]) = (heap[parent], heap[i]);
                    i = parent;
                }
                else
                {
                    break;
                }
            }
        }
        else if (distSquared < heap[0].distanceSquared)
        {
            // Replace the root (farthest point) and heapify down
            heap[0] = (distSquared, point);
            HeapifyDown(heap);
        }

        int dim = depth % _dimensions;
        double queryCoord = _pointAccessor.GetCoordinate(queryPoint, dim);
        double pointCoord = _pointAccessor.GetCoordinate(point, dim);
        double splitDist = queryCoord - pointCoord;
        double splitDistSquared = splitDist * splitDist;

        // Determine which side is closer
        int firstStart, firstEnd, secondStart, secondEnd;
        if (queryCoord <= pointCoord)
        {
            firstStart = start;
            firstEnd = mid;
            secondStart = mid + 1;
            secondEnd = end;
        }
        else
        {
            firstStart = mid + 1;
            firstEnd = end;
            secondStart = start;
            secondEnd = mid;
        }

        // Search closer side first
        if (firstStart < firstEnd)
        {
            KNearestNeighborsSearch(firstStart, firstEnd, depth + 1, queryPoint, k, heap);
        }

        // Search farther side if it could contain closer points
        if (secondStart < secondEnd && (heap.Count < k || splitDistSquared < heap[0].distanceSquared))
        {
            KNearestNeighborsSearch(secondStart, secondEnd, depth + 1, queryPoint, k, heap);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HeapifyDown(List<(double distanceSquared, T point)> heap)
    {
        int i = 0;
        int count = heap.Count;
        while (true)
        {
            int largest = i;
            int left = 2 * i + 1;
            int right = 2 * i + 2;

            if (left < count && heap[left].distanceSquared > heap[largest].distanceSquared)
            {
                largest = left;
            }

            if (right < count && heap[right].distanceSquared > heap[largest].distanceSquared)
            {
                largest = right;
            }

            if (largest == i)
            {
                break;
            }

            (heap[i], heap[largest]) = (heap[largest], heap[i]);
            i = largest;
        }
    }

    private IEnumerable<T> FindPointsWithinDistanceInternal(int start, int end, int depth, T queryPoint, double maxDistanceSquared)
    {
        if (start >= end)
        {
            yield break;
        }

        int mid = start + (end - start) / 2;
        T point = _points[mid];
        double distSquared = CalculateDistanceSquared(queryPoint, point);

        if (distSquared <= maxDistanceSquared)
        {
            yield return point;
        }

        int dim = depth % _dimensions;
        double queryCoord = _pointAccessor.GetCoordinate(queryPoint, dim);
        double pointCoord = _pointAccessor.GetCoordinate(point, dim);
        double splitDist = queryCoord - pointCoord;
        double splitDistSquared = splitDist * splitDist;

        // Determine which side to search first
        int firstStart, firstEnd, secondStart, secondEnd;
        if (queryCoord <= pointCoord)
        {
            firstStart = start;
            firstEnd = mid;
            secondStart = mid + 1;
            secondEnd = end;
        }
        else
        {
            firstStart = mid + 1;
            firstEnd = end;
            secondStart = start;
            secondEnd = mid;
        }

        // Always search the closer side
        if (firstStart < firstEnd)
        {
            foreach (T p in FindPointsWithinDistanceInternal(firstStart, firstEnd, depth + 1, queryPoint, maxDistanceSquared))
            {
                yield return p;
            }
        }

        // Only search the farther side if it could contain closer points
        if (secondStart < secondEnd && splitDistSquared <= maxDistanceSquared)
        {
            foreach (T p in FindPointsWithinDistanceInternal(secondStart, secondEnd, depth + 1, queryPoint, maxDistanceSquared))
            {
                yield return p;
            }
        }
    }

    #endregion
}
