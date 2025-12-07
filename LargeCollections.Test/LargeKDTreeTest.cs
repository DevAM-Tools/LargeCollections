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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace LargeCollections.Test;

#region Test Point Accessors for KDTree

/// <summary>
/// Test-only point accessor for 2D points represented as (double X, double Y) tuples.
/// </summary>
internal readonly struct KDTreePoint2DAccessor : IPointAccessor<(double X, double Y)>
{
    public int Dimensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetCoordinate((double X, double Y) point, int dimension)
    {
        return dimension switch
        {
            0 => point.X,
            1 => point.Y,
            _ => throw new ArgumentOutOfRangeException(nameof(dimension))
        };
    }
}

/// <summary>
/// Test-only point accessor for 3D points represented as (double X, double Y, double Z) tuples.
/// </summary>
internal readonly struct KDTreePoint3DAccessor : IPointAccessor<(double X, double Y, double Z)>
{
    public int Dimensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetCoordinate((double X, double Y, double Z) point, int dimension)
    {
        return dimension switch
        {
            0 => point.X,
            1 => point.Y,
            2 => point.Z,
            _ => throw new ArgumentOutOfRangeException(nameof(dimension))
        };
    }
}

/// <summary>
/// Test-only point accessor for N-dimensional points represented as double arrays.
/// </summary>
internal readonly struct KDTreePointArrayAccessor : IPointAccessor<double[]>
{
    private readonly int _dimensions;

    public KDTreePointArrayAccessor(int dimensions)
    {
        _dimensions = dimensions;
    }

    public int Dimensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _dimensions;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetCoordinate(double[] point, int dimension)
    {
        return point[dimension];
    }
}

#endregion

public class LargeKDTreeTest
{
    #region Helper Methods for Creating Trees

    private static LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> CreateTree2D((double X, double Y)[] points)
    {
        return LargeKDTree.Create<(double X, double Y), KDTreePoint2DAccessor>(new KDTreePoint2DAccessor(), points);
    }

    private static LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> CreateTree2D(IEnumerable<(double X, double Y)> points)
    {
        return LargeKDTree.Create<(double X, double Y), KDTreePoint2DAccessor>(new KDTreePoint2DAccessor(), points);
    }

    private static LargeKDTree<(double X, double Y, double Z), KDTreePoint3DAccessor> CreateTree3D((double X, double Y, double Z)[] points)
    {
        return LargeKDTree.Create<(double X, double Y, double Z), KDTreePoint3DAccessor>(new KDTreePoint3DAccessor(), points);
    }

    private static LargeKDTree<double[], KDTreePointArrayAccessor> CreateTreeND(int dimensions, double[][] points)
    {
        return LargeKDTree.Create<double[], KDTreePointArrayAccessor>(new KDTreePointArrayAccessor(dimensions), points);
    }

    private static LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> CreateEmptyTree2D()
    {
        return LargeKDTree.CreateEmpty<(double X, double Y), KDTreePoint2DAccessor>(new KDTreePoint2DAccessor());
    }

    #endregion

    #region Test Data Sources

#if UNIT_TEST
    public static IEnumerable<int> ItemCounts()
    {
        yield return 0;
        yield return 1;
        yield return 2;
        yield return 10;
        yield return 50;
        yield return 100;
        yield return 200;
    }
#else
    public static IEnumerable<int> ItemCounts()
    {
        yield return 0;
        yield return 1;
        yield return 2;
        yield return 10;
        yield return 100;
        yield return 1000;
        yield return 10000;
    }
#endif

    private static (double X, double Y) CreatePoint2D(int index)
    {
        return (index * 1.5, index * 2.5);
    }

    private static (double X, double Y, double Z) CreatePoint3D(int index)
    {
        return (index * 1.5, index * 2.5, index * 3.5);
    }

    private static double[] CreatePointND(int index, int dimensions)
    {
        double[] point = new double[dimensions];
        for (int d = 0; d < dimensions; d++)
        {
            point[d] = index * (d + 1.5);
        }
        return point;
    }

    private static (double X, double Y)[] CreatePoints2D(int count)
    {
        (double X, double Y)[] points = new (double X, double Y)[count];
        for (int i = 0; i < count; i++)
        {
            points[i] = CreatePoint2D(i);
        }
        return points;
    }

    private static (double X, double Y, double Z)[] CreatePoints3D(int count)
    {
        (double X, double Y, double Z)[] points = new (double X, double Y, double Z)[count];
        for (int i = 0; i < count; i++)
        {
            points[i] = CreatePoint3D(i);
        }
        return points;
    }

    #endregion

    #region Struct Action for DoForEach Tests

    private struct CountAction : ILargeAction<(double X, double Y)>
    {
        public long Count;
        public double SumX;
        public double SumY;

        public void Invoke((double X, double Y) item)
        {
            Count++;
            SumX += item.X;
            SumY += item.Y;
        }
    }

    #endregion

    #region Constructor Tests

    [Test]
    public async Task Constructor_EmptyArray_ShouldCreate()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(Array.Empty<(double X, double Y)>());

        await Assert.That(tree.Count).IsEqualTo(0L);
        await Assert.That(tree.Dimensions).IsEqualTo(2);
    }

    [Test]
    public async Task Constructor_SinglePoint_ShouldCreate()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(new[] { (1.0, 2.0) });

        await Assert.That(tree.Count).IsEqualTo(1L);
        await Assert.That(tree.Dimensions).IsEqualTo(2);
    }

    [Test]
    [MethodDataSource(nameof(ItemCounts))]
    public async Task Constructor_MultiplePoints_ShouldCreate(int itemCount)
    {
        (double X, double Y)[] points = CreatePoints2D(itemCount);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        await Assert.That(tree.Count).IsEqualTo((long)itemCount);
    }

    [Test]
    public async Task Constructor_NullArray_ShouldThrow()
    {
        await Assert.That(() => CreateTree2D(((double X, double Y)[])null))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_NullEnumerable_ShouldThrow()
    {
        await Assert.That(() => CreateTree2D((IEnumerable<(double X, double Y)>)null))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_3D_ShouldCreate()
    {
        LargeKDTree<(double X, double Y, double Z), KDTreePoint3DAccessor> tree = CreateTree3D(CreatePoints3D(10));

        await Assert.That(tree.Dimensions).IsEqualTo(3);
        await Assert.That(tree.Count).IsEqualTo(10L);
    }

    [Test]
    public async Task Constructor_NDimensional_ShouldCreate()
    {
        double[][] points = new double[5][];
        for (int i = 0; i < 5; i++)
        {
            points[i] = CreatePointND(i, 5);
        }

        LargeKDTree<double[], KDTreePointArrayAccessor> tree = CreateTreeND(5, points);

        await Assert.That(tree.Dimensions).IsEqualTo(5);
        await Assert.That(tree.Count).IsEqualTo(5L);
    }

    [Test]
    public async Task Constructor_WithDelegateAccessor_ShouldCreate()
    {
        (double X, double Y)[] points = new[] { (1.0, 2.0), (3.0, 4.0) };

        LargeKDTree<(double X, double Y), DelegatePointAccessor<(double X, double Y)>> tree = LargeKDTree.Create(
            2,
            (p, d) => d == 0 ? p.X : p.Y,
            points);

        await Assert.That(tree.Dimensions).IsEqualTo(2);
        await Assert.That(tree.Count).IsEqualTo(2L);
    }

    [Test]
    public async Task CreateEmpty_ShouldCreateEmptyTree()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateEmptyTree2D();

        await Assert.That(tree.Count).IsEqualTo(0L);
        await Assert.That(tree.Dimensions).IsEqualTo(2);
    }

    #endregion

    #region Contains Tests

    [Test]
    [MethodDataSource(nameof(ItemCounts))]
    public async Task Contains_ExistingPoint_ShouldReturnTrue(int itemCount)
    {
        if (itemCount == 0)
        {
            return;
        }

        (double X, double Y)[] points = CreatePoints2D(itemCount);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        bool result = tree.Contains(CreatePoint2D(itemCount / 2));

        await Assert.That(result).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(ItemCounts))]
    public async Task Contains_NonExistingPoint_ShouldReturnFalse(int itemCount)
    {
        (double X, double Y)[] points = CreatePoints2D(itemCount);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        bool result = tree.Contains((-999.0, -999.0));

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Contains_EmptyTree_ShouldReturnFalse()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateEmptyTree2D();

        bool result = tree.Contains((1.0, 2.0));

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Contains_AllAddedPoints_ShouldReturnTrue()
    {
        (double X, double Y)[] points = CreatePoints2D(100);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        foreach ((double X, double Y) point in points)
        {
            await Assert.That(tree.Contains(point)).IsTrue();
        }
    }

    #endregion

    #region GetAll and Enumeration Tests

    [Test]
    [MethodDataSource(nameof(ItemCounts))]
    public async Task GetAll_ShouldReturnAllPoints(int itemCount)
    {
        (double X, double Y)[] points = CreatePoints2D(itemCount);
        HashSet<(double X, double Y)> addedPoints = new HashSet<(double X, double Y)>(points);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        List<(double X, double Y)> retrievedPoints = tree.GetAll().ToList();

        await Assert.That(retrievedPoints.Count).IsEqualTo(itemCount);
        foreach ((double X, double Y) point in retrievedPoints)
        {
            await Assert.That(addedPoints.Contains(point)).IsTrue();
        }
    }

    [Test]
    public async Task GetAll_EmptyTree_ShouldReturnEmpty()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateEmptyTree2D();

        List<(double X, double Y)> points = tree.GetAll().ToList();

        await Assert.That(points.Count).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(nameof(ItemCounts))]
    public async Task GetEnumerator_ShouldEnumerateAllPoints(int itemCount)
    {
        (double X, double Y)[] points = CreatePoints2D(itemCount);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        int count = 0;
        foreach ((double X, double Y) _ in tree)
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(itemCount);
    }

    #endregion

    #region DoForEach Tests

    [Test]
    [MethodDataSource(nameof(ItemCounts))]
    public async Task DoForEach_Action_ShouldVisitAllPoints(int itemCount)
    {
        (double X, double Y)[] points = CreatePoints2D(itemCount);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        int count = 0;
        tree.DoForEach(_ => count++);

        await Assert.That(count).IsEqualTo(itemCount);
    }

    [Test]
    public async Task DoForEach_NullAction_ShouldThrow()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(CreatePoints2D(10));

        await Assert.That(() => tree.DoForEach((Action<(double X, double Y)>)null))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [MethodDataSource(nameof(ItemCounts))]
    public async Task DoForEach_StructAction_ShouldVisitAllPoints(int itemCount)
    {
        (double X, double Y)[] points = CreatePoints2D(itemCount);
        double expectedSumX = points.Sum(p => p.X);
        double expectedSumY = points.Sum(p => p.Y);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        CountAction action = new CountAction();
        tree.DoForEach(ref action);

        await Assert.That(action.Count).IsEqualTo((long)itemCount);
        await Assert.That(action.SumX).IsEqualTo(expectedSumX);
        await Assert.That(action.SumY).IsEqualTo(expectedSumY);
    }

    #endregion

    #region Range Query Tests

    [Test]
    public async Task RangeQuery_ShouldReturnPointsInRange()
    {
        (double X, double Y)[] points = new[]
        {
            (1.0, 1.0),
            (2.0, 2.0),
            (3.0, 3.0),
            (10.0, 10.0),
            (20.0, 20.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        List<(double X, double Y)> results = tree.RangeQuery((0.0, 0.0), (5.0, 5.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(3);
        await Assert.That(results.Contains((1.0, 1.0))).IsTrue();
        await Assert.That(results.Contains((2.0, 2.0))).IsTrue();
        await Assert.That(results.Contains((3.0, 3.0))).IsTrue();
    }

    [Test]
    public async Task RangeQuery_EmptyRange_ShouldReturnEmpty()
    {
        (double X, double Y)[] points = new[]
        {
            (10.0, 10.0),
            (20.0, 20.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        List<(double X, double Y)> results = tree.RangeQuery((0.0, 0.0), (5.0, 5.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RangeQuery_EmptyTree_ShouldReturnEmpty()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateEmptyTree2D();

        List<(double X, double Y)> results = tree.RangeQuery((0.0, 0.0), (100.0, 100.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(nameof(ItemCounts))]
    public async Task RangeQuery_LargeDataSet_ShouldWork(int itemCount)
    {
        (double X, double Y)[] points = CreatePoints2D(itemCount);
        int expectedInRange = points.Count(p => p.X >= 0 && p.X <= 50 && p.Y >= 0 && p.Y <= 100);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        List<(double X, double Y)> results = tree.RangeQuery((0.0, 0.0), (50.0, 100.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(expectedInRange);
    }

    [Test]
    public async Task RangeQuery_3D_ShouldReturnPointsInRange()
    {
        (double X, double Y, double Z)[] points = new[]
        {
            (1.0, 1.0, 1.0),
            (2.0, 2.0, 2.0),
            (3.0, 3.0, 3.0),
            (10.0, 10.0, 10.0)
        };
        LargeKDTree<(double X, double Y, double Z), KDTreePoint3DAccessor> tree = CreateTree3D(points);

        List<(double X, double Y, double Z)> results = tree.RangeQuery((0.0, 0.0, 0.0), (5.0, 5.0, 5.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(3);
    }

    #endregion

    #region DoForEachInRange Tests

    [Test]
    public async Task DoForEachInRange_ShouldCallActionForPointsInRange()
    {
        (double X, double Y)[] points = new[]
        {
            (1.0, 1.0),
            (2.0, 2.0),
            (3.0, 3.0),
            (10.0, 10.0),
            (20.0, 20.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        List<(double X, double Y)> collected = new List<(double X, double Y)>();
        tree.DoForEachInRange((0.0, 0.0), (5.0, 5.0), point => collected.Add(point));

        await Assert.That(collected.Count).IsEqualTo(3);
        await Assert.That(collected.Contains((1.0, 1.0))).IsTrue();
        await Assert.That(collected.Contains((2.0, 2.0))).IsTrue();
        await Assert.That(collected.Contains((3.0, 3.0))).IsTrue();
    }

    [Test]
    public async Task DoForEachInRange_EmptyRange_ShouldNotCallAction()
    {
        (double X, double Y)[] points = new[] { (10.0, 10.0), (20.0, 20.0) };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        int callCount = 0;
        tree.DoForEachInRange((0.0, 0.0), (5.0, 5.0), _ => callCount++);

        await Assert.That(callCount).IsEqualTo(0);
    }

    [Test]
    public async Task DoForEachInRange_EmptyTree_ShouldNotCallAction()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateEmptyTree2D();

        int callCount = 0;
        tree.DoForEachInRange((0.0, 0.0), (100.0, 100.0), _ => callCount++);

        await Assert.That(callCount).IsEqualTo(0);
    }

    [Test]
    public async Task DoForEachInRange_ILargeAction_ShouldCallActionForPointsInRange()
    {
        (double X, double Y)[] points = new[]
        {
            (1.0, 1.0),
            (2.0, 2.0),
            (3.0, 3.0),
            (10.0, 10.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        CountAction action = new CountAction();
        tree.DoForEachInRange((0.0, 0.0), (5.0, 5.0), ref action);

        await Assert.That(action.Count).IsEqualTo(3);
    }

    #endregion

    #region CountInRange Tests

    [Test]
    public async Task CountInRange_ShouldCountPointsInRange()
    {
        (double X, double Y)[] points = new[]
        {
            (1.0, 1.0),
            (2.0, 2.0),
            (3.0, 3.0),
            (10.0, 10.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        long count = tree.CountInRange((0.0, 0.0), (5.0, 5.0));

        await Assert.That(count).IsEqualTo(3L);
    }

    [Test]
    public async Task CountInRange_EmptyTree_ShouldReturnZero()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateEmptyTree2D();

        long count = tree.CountInRange((0.0, 0.0), (100.0, 100.0));

        await Assert.That(count).IsEqualTo(0L);
    }

    [Test]
    [MethodDataSource(nameof(ItemCounts))]
    public async Task CountInRange_LargeDataSet_ShouldMatchRangeQueryCount(int itemCount)
    {
        (double X, double Y)[] points = CreatePoints2D(itemCount);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        long countResult = tree.CountInRange((0.0, 0.0), (50.0, 100.0));
        int rangeQueryCount = tree.RangeQuery((0.0, 0.0), (50.0, 100.0)).Count();

        await Assert.That(countResult).IsEqualTo((long)rangeQueryCount);
    }

    #endregion

    #region TryGetFirstInRange Tests

    [Test]
    public async Task TryGetFirstInRange_MatchingPoint_ShouldReturnTrue()
    {
        (double X, double Y)[] points = new[]
        {
            (1.0, 2.0),
            (5.0, 5.0),
            (10.0, 10.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        bool result = tree.TryGetFirstInRange((0.0, 0.0), (3.0, 3.0), out (double X, double Y) firstPoint);

        await Assert.That(result).IsTrue();
        await Assert.That(firstPoint).IsEqualTo((1.0, 2.0));
    }

    [Test]
    public async Task TryGetFirstInRange_NoMatchingPoint_ShouldReturnFalse()
    {
        (double X, double Y)[] points = new[] { (10.0, 10.0), (20.0, 20.0) };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        bool result = tree.TryGetFirstInRange((0.0, 0.0), (5.0, 5.0), out _);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryGetFirstInRange_EmptyTree_ShouldReturnFalse()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateEmptyTree2D();

        bool result = tree.TryGetFirstInRange((0.0, 0.0), (100.0, 100.0), out _);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryGetFirstInRange_MultipleMatches_ShouldReturnOneInRange()
    {
        (double X, double Y)[] points = new[]
        {
            (1.0, 1.0),
            (2.0, 2.0),
            (3.0, 3.0),
            (50.0, 50.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        bool result = tree.TryGetFirstInRange((0.0, 0.0), (10.0, 10.0), out (double X, double Y) firstPoint);

        await Assert.That(result).IsTrue();
        bool isInRange = firstPoint.X >= 0.0 && firstPoint.X <= 10.0 &&
                         firstPoint.Y >= 0.0 && firstPoint.Y <= 10.0;
        await Assert.That(isInRange).IsTrue();
    }

    #endregion

    #region Nearest Neighbor Tests

    [Test]
    public async Task NearestNeighbor_ShouldReturnClosestPoint()
    {
        (double X, double Y)[] points = new[]
        {
            (0.0, 0.0),
            (10.0, 10.0),
            (5.0, 5.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        (double X, double Y) nearest = tree.NearestNeighbor((4.0, 4.0));

        await Assert.That(nearest).IsEqualTo((5.0, 5.0));
    }

    [Test]
    public async Task NearestNeighbor_EmptyTree_ShouldThrow()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateEmptyTree2D();

        await Assert.That(() => tree.NearestNeighbor((1.0, 1.0)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TryGetNearestNeighbor_ExistingPoints_ShouldReturnTrue()
    {
        (double X, double Y)[] points = new[] { (0.0, 0.0), (10.0, 10.0) };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        bool result = tree.TryGetNearestNeighbor((1.0, 1.0), out (double X, double Y) nearest);

        await Assert.That(result).IsTrue();
        await Assert.That(nearest).IsEqualTo((0.0, 0.0));
    }

    [Test]
    public async Task TryGetNearestNeighbor_EmptyTree_ShouldReturnFalse()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateEmptyTree2D();

        bool result = tree.TryGetNearestNeighbor((1.0, 1.0), out _);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryGetNearestNeighborWithDistance_ShouldReturnDistanceSquared()
    {
        (double X, double Y)[] points = new[] { (0.0, 0.0), (3.0, 4.0) };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        bool result = tree.TryGetNearestNeighborWithDistance((0.0, 0.0), out (double X, double Y) nearest, out double distSq);

        await Assert.That(result).IsTrue();
        await Assert.That(nearest).IsEqualTo((0.0, 0.0));
        await Assert.That(distSq).IsEqualTo(0.0);
    }

    [Test]
    [MethodDataSource(nameof(ItemCounts))]
    public async Task NearestNeighbor_LargeDataSet_ShouldFindCorrectPoint(int itemCount)
    {
        if (itemCount == 0)
        {
            return;
        }

        (double X, double Y)[] points = CreatePoints2D(itemCount);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        (double X, double Y) queryPoint = (50.0, 80.0);
        (double X, double Y) nearest = tree.NearestNeighbor(queryPoint);

        // Verify by brute force
        (double X, double Y) expectedNearest = points[0];
        double minDistSq = double.MaxValue;
        foreach ((double X, double Y) point in points)
        {
            double dx = point.X - queryPoint.X;
            double dy = point.Y - queryPoint.Y;
            double distSq = dx * dx + dy * dy;
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                expectedNearest = point;
            }
        }

        await Assert.That(nearest).IsEqualTo(expectedNearest);
    }

    [Test]
    public async Task NearestNeighbor3D_ShouldWork()
    {
        (double X, double Y, double Z)[] points = new[]
        {
            (0.0, 0.0, 0.0),
            (10.0, 10.0, 10.0)
        };
        LargeKDTree<(double X, double Y, double Z), KDTreePoint3DAccessor> tree = CreateTree3D(points);

        (double X, double Y, double Z) nearest = tree.NearestNeighbor((1.0, 1.0, 1.0));

        await Assert.That(nearest).IsEqualTo((0.0, 0.0, 0.0));
    }

    #endregion

    #region FindPointsWithinDistance Tests

    [Test]
    public async Task FindPointsWithinDistance_ShouldReturnPointsInRadius()
    {
        (double X, double Y)[] points = new[]
        {
            (0.0, 0.0),
            (1.0, 0.0),
            (0.0, 1.0),
            (10.0, 10.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        // Distance of sqrt(2) ≈ 1.41, squared = 2
        List<(double X, double Y)> results = tree.FindPointsWithinDistance((0.0, 0.0), 2.0).ToList();

        await Assert.That(results.Count).IsEqualTo(3);
        await Assert.That(results.Contains((0.0, 0.0))).IsTrue();
        await Assert.That(results.Contains((1.0, 0.0))).IsTrue();
        await Assert.That(results.Contains((0.0, 1.0))).IsTrue();
    }

    [Test]
    public async Task FindPointsWithinDistance_EmptyTree_ShouldReturnEmpty()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateEmptyTree2D();

        List<(double X, double Y)> results = tree.FindPointsWithinDistance((0.0, 0.0), 100.0).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FindPointsWithinDistance_NegativeDistance_ShouldThrow()
    {
        (double X, double Y)[] points = new[] { (0.0, 0.0) };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        await Assert.That(() => tree.FindPointsWithinDistance((0.0, 0.0), -1.0).ToList())
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task FindPointsWithinDistance_ZeroDistance_ShouldReturnExactMatches()
    {
        (double X, double Y)[] points = new[] { (0.0, 0.0), (1.0, 1.0) };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        List<(double X, double Y)> results = tree.FindPointsWithinDistance((0.0, 0.0), 0.0).ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0]).IsEqualTo((0.0, 0.0));
    }

    #endregion

    #region FindKNearestNeighbors Tests

    [Test]
    public async Task FindKNearestNeighbors_ShouldReturnKClosestPoints()
    {
        (double X, double Y)[] points = new[]
        {
            (0.0, 0.0),
            (1.0, 1.0),
            (2.0, 2.0),
            (10.0, 10.0),
            (20.0, 20.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        List<(double X, double Y)> results = tree.FindKNearestNeighbors((0.0, 0.0), 3).ToList();

        await Assert.That(results.Count).IsEqualTo(3);
        await Assert.That(results.Contains((0.0, 0.0))).IsTrue();
        await Assert.That(results.Contains((1.0, 1.0))).IsTrue();
        await Assert.That(results.Contains((2.0, 2.0))).IsTrue();
    }

    [Test]
    public async Task FindKNearestNeighbors_KLargerThanCount_ShouldReturnAllPoints()
    {
        (double X, double Y)[] points = new[] { (0.0, 0.0), (1.0, 1.0) };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        List<(double X, double Y)> results = tree.FindKNearestNeighbors((0.0, 0.0), 10).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task FindKNearestNeighbors_EmptyTree_ShouldReturnEmpty()
    {
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateEmptyTree2D();

        List<(double X, double Y)> results = tree.FindKNearestNeighbors((0.0, 0.0), 5).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FindKNearestNeighbors_InvalidK_ShouldThrow()
    {
        (double X, double Y)[] points = new[] { (0.0, 0.0) };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        await Assert.That(() => tree.FindKNearestNeighbors((0.0, 0.0), 0).ToList())
            .Throws<ArgumentOutOfRangeException>();

        await Assert.That(() => tree.FindKNearestNeighbors((0.0, 0.0), -1).ToList())
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task FindKNearestNeighbors_OrderedByDistance()
    {
        (double X, double Y)[] points = new[]
        {
            (10.0, 0.0),
            (5.0, 0.0),
            (0.0, 0.0),
            (20.0, 0.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        List<(double X, double Y)> results = tree.FindKNearestNeighbors((0.0, 0.0), 3).ToList();

        await Assert.That(results.Count).IsEqualTo(3);
        // Should be ordered by distance (closest first)
        await Assert.That(results[0]).IsEqualTo((0.0, 0.0));
        await Assert.That(results[1]).IsEqualTo((5.0, 0.0));
        await Assert.That(results[2]).IsEqualTo((10.0, 0.0));
    }

    #endregion

    #region Edge Case Tests

    [Test]
    public async Task SinglePoint_AllOperations_ShouldWork()
    {
        (double X, double Y)[] points = new[] { (5.0, 5.0) };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        await Assert.That(tree.Contains((5.0, 5.0))).IsTrue();
        await Assert.That(tree.RangeQuery((0.0, 0.0), (10.0, 10.0)).Count()).IsEqualTo(1);
        await Assert.That(tree.NearestNeighbor((0.0, 0.0))).IsEqualTo((5.0, 5.0));
        await Assert.That(tree.CountInRange((0.0, 0.0), (10.0, 10.0))).IsEqualTo(1L);
    }

    [Test]
    public async Task PointsOnBoundary_RangeQuery_ShouldInclude()
    {
        (double X, double Y)[] points = new[] { (0.0, 0.0), (10.0, 10.0) };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        List<(double X, double Y)> results = tree.RangeQuery((0.0, 0.0), (10.0, 10.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task NegativeCoordinates_ShouldWork()
    {
        (double X, double Y)[] points = new[]
        {
            (-5.0, -5.0),
            (5.0, 5.0),
            (-5.0, 5.0),
            (5.0, -5.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        List<(double X, double Y)> results = tree.RangeQuery((-10.0, -10.0), (0.0, 0.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0]).IsEqualTo((-5.0, -5.0));
    }

    [Test]
    public async Task VeryLargeCoordinates_ShouldWork()
    {
        (double X, double Y)[] points = new[] { (1e15, 1e15), (-1e15, -1e15) };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        await Assert.That(tree.Count).IsEqualTo(2L);
        await Assert.That(tree.Contains((1e15, 1e15))).IsTrue();
    }

    [Test]
    public async Task VerySmallCoordinates_ShouldWork()
    {
        (double X, double Y)[] points = new[] { (1e-15, 1e-15), (2e-15, 2e-15) };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        await Assert.That(tree.Count).IsEqualTo(2L);
    }

    [Test]
    public async Task DuplicatePoints_ShouldBeStored()
    {
        (double X, double Y)[] points = new[]
        {
            (1.0, 1.0),
            (1.0, 1.0),
            (1.0, 1.0)
        };
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        await Assert.That(tree.Count).IsEqualTo(3L);
        await Assert.That(tree.Contains((1.0, 1.0))).IsTrue();
    }

    #endregion

    #region N-Dimensional Tests

    [Test]
    public async Task NDimensional_5D_ShouldWork()
    {
        double[][] points = new double[5][];
        for (int i = 0; i < 5; i++)
        {
            points[i] = new double[] { i, i * 2, i * 3, i * 4, i * 5 };
        }

        LargeKDTree<double[], KDTreePointArrayAccessor> tree = CreateTreeND(5, points);

        await Assert.That(tree.Count).IsEqualTo(5L);
        await Assert.That(tree.Dimensions).IsEqualTo(5);
    }

    [Test]
    public async Task NDimensional_NearestNeighbor_ShouldWork()
    {
        double[][] points = new double[][]
        {
            new double[] { 0, 0, 0 },
            new double[] { 10, 10, 10 }
        };
        LargeKDTree<double[], KDTreePointArrayAccessor> tree = CreateTreeND(3, points);

        bool result = tree.TryGetNearestNeighbor(new double[] { 1, 1, 1 }, out double[] nearest);

        await Assert.That(result).IsTrue();
        await Assert.That(nearest[0]).IsEqualTo(0.0);
        await Assert.That(nearest[1]).IsEqualTo(0.0);
        await Assert.That(nearest[2]).IsEqualTo(0.0);
    }

    #endregion

    #region Performance / Stress Tests

    [Test]
    public async Task LargeDataSet_Construction_ShouldComplete()
    {
        int count = 10000;
        (double X, double Y)[] points = CreatePoints2D(count);

        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        await Assert.That(tree.Count).IsEqualTo((long)count);
    }

    [Test]
    public async Task LargeDataSet_NearestNeighborQueries_ShouldComplete()
    {
        int count = 1000;
        (double X, double Y)[] points = CreatePoints2D(count);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        // Run many nearest neighbor queries
        for (int i = 0; i < 100; i++)
        {
            (double X, double Y) query = (i * 10.0, i * 15.0);
            bool found = tree.TryGetNearestNeighbor(query, out _);
            await Assert.That(found).IsTrue();
        }
    }

    [Test]
    public async Task LargeDataSet_RangeQueries_ShouldComplete()
    {
        int count = 1000;
        (double X, double Y)[] points = CreatePoints2D(count);
        LargeKDTree<(double X, double Y), KDTreePoint2DAccessor> tree = CreateTree2D(points);

        // Run many range queries
        for (int i = 0; i < 100; i++)
        {
            List<(double X, double Y)> results = tree.RangeQuery(
                (i * 10.0, i * 10.0),
                (i * 10.0 + 100.0, i * 10.0 + 100.0)).ToList();

            // Just verify it doesn't crash
            await Assert.That(results).IsNotNull();
        }
    }

    #endregion
}
