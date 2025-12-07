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

#region Test Point Accessors

/// <summary>
/// Test-only point accessor for 2D points represented as (double X, double Y) tuples.
/// </summary>
internal readonly struct TestPoint2DAccessor : IPointAccessor<(double X, double Y)>
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
internal readonly struct TestPoint3DAccessor : IPointAccessor<(double X, double Y, double Z)>
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
internal readonly struct TestPointArrayAccessor : IPointAccessor<double[]>
{
    private readonly int _dimensions;

    public TestPointArrayAccessor(int dimensions)
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

public class LargeBKDTreeTest
{
    #region Helper Methods for Creating Trees

    private static LargeBKDTree<(double X, double Y), TestPoint2DAccessor> CreateTree2D(int leafCapacity = LargeBKDTree.DefaultLeafCapacity)
    {
        return LargeBKDTree.Create<(double X, double Y), TestPoint2DAccessor>(new TestPoint2DAccessor(), leafCapacity);
    }

    private static LargeBKDTree<(double X, double Y, double Z), TestPoint3DAccessor> CreateTree3D(int leafCapacity = LargeBKDTree.DefaultLeafCapacity)
    {
        return LargeBKDTree.Create<(double X, double Y, double Z), TestPoint3DAccessor>(new TestPoint3DAccessor(), leafCapacity);
    }

    private static LargeBKDTree<double[], TestPointArrayAccessor> CreateTreeND(int dimensions, int leafCapacity = LargeBKDTree.DefaultLeafCapacity)
    {
        return LargeBKDTree.Create<double[], TestPointArrayAccessor>(new TestPointArrayAccessor(dimensions), leafCapacity);
    }

    #endregion

    #region Test Data Sources

    public static IEnumerable<int> LeafCapacities()
    {
        yield return 4;
        yield return 8;
        yield return 16;
        yield return 32;
        yield return 64;
    }

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

    public readonly record struct LeafCapacityAndItemCount(int LeafCapacity, int ItemCount);

    public static IEnumerable<LeafCapacityAndItemCount> LeafCapacitiesAndItemCounts()
    {
        foreach (int leafCapacity in LeafCapacities())
        {
            foreach (int itemCount in ItemCounts())
            {
                yield return new LeafCapacityAndItemCount(leafCapacity, itemCount);
            }
        }
    }

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
    public async Task Constructor_DefaultLeafCapacity_ShouldCreate()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        await Assert.That(tree.Count).IsEqualTo(0L);
        await Assert.That(tree.Dimensions).IsEqualTo(2);
        await Assert.That(tree.LeafCapacity).IsEqualTo(LargeBKDTree.DefaultLeafCapacity);
    }

    [Test]
    [MethodDataSource(nameof(LeafCapacities))]
    public async Task Constructor_CustomLeafCapacity_ShouldCreate(int leafCapacity)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(leafCapacity);

        await Assert.That(tree.Count).IsEqualTo(0L);
        await Assert.That(tree.LeafCapacity).IsEqualTo(leafCapacity);
    }

    [Test]
    public async Task Constructor_InvalidLeafCapacity_ShouldThrow()
    {
        await Assert.That(() => CreateTree2D(leafCapacity: 1))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => CreateTree2D(leafCapacity: 0))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => CreateTree2D(leafCapacity: -1))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Constructor_3D_ShouldCreate()
    {
        LargeBKDTree<(double X, double Y, double Z), TestPoint3DAccessor> tree = CreateTree3D();

        await Assert.That(tree.Dimensions).IsEqualTo(3);
    }

    [Test]
    public async Task Constructor_NDimensional_ShouldCreate()
    {
        LargeBKDTree<double[], TestPointArrayAccessor> tree = CreateTreeND(5);

        await Assert.That(tree.Dimensions).IsEqualTo(5);
    }

    [Test]
    public async Task Constructor_WithDelegateAccessor_ShouldCreate()
    {
        LargeBKDTree<(double X, double Y), DelegatePointAccessor<(double X, double Y)>> tree = LargeBKDTree.Create<(double X, double Y)>(
            2,
            (p, d) => d == 0 ? p.X : p.Y);

        await Assert.That(tree.Dimensions).IsEqualTo(2);
    }

    #endregion

    #region Add Tests

    [Test]
    public async Task Add_SinglePoint_ShouldIncreaseCount()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        tree.Add((1.0, 2.0));

        await Assert.That(tree.Count).IsEqualTo(1L);
    }

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task Add_MultiplePoints_ShouldIncreaseCount(LeafCapacityAndItemCount args)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);

        for (int i = 0; i < args.ItemCount; i++)
        {
            tree.Add(CreatePoint2D(i));
        }

        await Assert.That(tree.Count).IsEqualTo((long)args.ItemCount);
    }

    [Test]
    public async Task Add_MoreThanLeafCapacity_ShouldSplitLeaf()
    {
        int leafCapacity = 4;
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(leafCapacity);

        for (int i = 0; i < leafCapacity + 5; i++)
        {
            tree.Add(CreatePoint2D(i));
        }

        await Assert.That(tree.Count).IsEqualTo((long)(leafCapacity + 5));
    }

    [Test]
    public async Task Add_DuplicatePoints_ShouldAllBeAdded()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        tree.Add((1.0, 2.0));
        tree.Add((1.0, 2.0));
        tree.Add((1.0, 2.0));

        await Assert.That(tree.Count).IsEqualTo(3L);
    }

    #endregion

    #region AddRange Tests

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task AddRange_IEnumerable_ShouldAddAllPoints(LeafCapacityAndItemCount args)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        List<(double X, double Y)> points = new List<(double X, double Y)>();
        for (int i = 0; i < args.ItemCount; i++)
        {
            points.Add(CreatePoint2D(i));
        }

        tree.AddRange(points);

        await Assert.That(tree.Count).IsEqualTo((long)args.ItemCount);
    }

    [Test]
    public async Task AddRange_NullEnumerable_ShouldThrow()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        await Assert.That(() => tree.AddRange((IEnumerable<(double X, double Y)>)null))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task AddRange_ReadOnlyLargeSpan_ShouldAddAllPoints(LeafCapacityAndItemCount args)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        LargeArray<(double X, double Y)> array = new LargeArray<(double X, double Y)>(args.ItemCount);
        for (int i = 0; i < args.ItemCount; i++)
        {
            array[i] = CreatePoint2D(i);
        }

        tree.AddRange(array.AsReadOnlyLargeSpan());

        await Assert.That(tree.Count).IsEqualTo((long)args.ItemCount);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    [MethodDataSource(nameof(ItemCounts))]
    public async Task AddRange_ReadOnlySpan_ShouldAddAllPoints(int itemCount)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        (double X, double Y)[] array = new (double X, double Y)[itemCount];
        for (int i = 0; i < itemCount; i++)
        {
            array[i] = CreatePoint2D(i);
        }

        tree.AddRange(array.AsSpan());

        await Assert.That(tree.Count).IsEqualTo((long)itemCount);
    }
#endif

    #endregion

    #region BulkAdd Tests

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task BulkAdd_ShouldAddAllPoints(LeafCapacityAndItemCount args)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        List<(double X, double Y)> points = new List<(double X, double Y)>();
        for (int i = 0; i < args.ItemCount; i++)
        {
            points.Add(CreatePoint2D(i));
        }

        tree.BulkAdd(points);

        await Assert.That(tree.Count).IsEqualTo((long)args.ItemCount);
    }

    [Test]
    public async Task BulkAdd_ToExistingTree_ShouldMergePoints()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        // Add initial points
        tree.Add((1.0, 1.0));
        tree.Add((2.0, 2.0));

        // Bulk add more points
        List<(double X, double Y)> morePoints = new List<(double X, double Y)>
        {
            (3.0, 3.0),
            (4.0, 4.0),
            (5.0, 5.0)
        };
        tree.BulkAdd(morePoints);

        await Assert.That(tree.Count).IsEqualTo(5L);
    }

    [Test]
    public async Task BulkAdd_NullEnumerable_ShouldThrow()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        await Assert.That(() => tree.BulkAdd(null))
            .Throws<ArgumentNullException>();
    }

    #endregion

    #region Contains Tests

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task Contains_ExistingPoint_ShouldReturnTrue(LeafCapacityAndItemCount args)
    {
        if (args.ItemCount == 0)
        {
            return;
        }

        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        for (int i = 0; i < args.ItemCount; i++)
        {
            tree.Add(CreatePoint2D(i));
        }

        bool result = tree.Contains(CreatePoint2D(args.ItemCount / 2));

        await Assert.That(result).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task Contains_NonExistingPoint_ShouldReturnFalse(LeafCapacityAndItemCount args)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        for (int i = 0; i < args.ItemCount; i++)
        {
            tree.Add(CreatePoint2D(i));
        }

        bool result = tree.Contains((-999.0, -999.0));

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Contains_EmptyTree_ShouldReturnFalse()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        bool result = tree.Contains((1.0, 2.0));

        await Assert.That(result).IsFalse();
    }

    #endregion

    #region Remove Tests

    [Test]
    public async Task Remove_ExistingPoint_ShouldRemoveAndDecreaseCount()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1.0, 2.0));
        tree.Add((3.0, 4.0));

        bool result = tree.Remove((1.0, 2.0));

        await Assert.That(result).IsTrue();
        await Assert.That(tree.Count).IsEqualTo(1L);
        await Assert.That(tree.Contains((1.0, 2.0))).IsFalse();
        await Assert.That(tree.Contains((3.0, 4.0))).IsTrue();
    }

    [Test]
    public async Task Remove_NonExistingPoint_ShouldReturnFalse()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1.0, 2.0));

        bool result = tree.Remove((999.0, 999.0));

        await Assert.That(result).IsFalse();
        await Assert.That(tree.Count).IsEqualTo(1L);
    }

    [Test]
    public async Task Remove_EmptyTree_ShouldReturnFalse()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        bool result = tree.Remove((1.0, 2.0));

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Remove_WithOutParameter_ShouldReturnRemovedPoint()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1.0, 2.0));

        bool result = tree.Remove((1.0, 2.0), out (double X, double Y) removedPoint);

        await Assert.That(result).IsTrue();
        await Assert.That(removedPoint).IsEqualTo((1.0, 2.0));
    }

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task Remove_AllPoints_ShouldResultInEmptyTree(LeafCapacityAndItemCount args)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        List<(double X, double Y)> points = new List<(double X, double Y)>();
        for (int i = 0; i < args.ItemCount; i++)
        {
            (double X, double Y) point = CreatePoint2D(i);
            points.Add(point);
            tree.Add(point);
        }

        foreach ((double X, double Y) point in points)
        {
            tree.Remove(point);
        }

        await Assert.That(tree.Count).IsEqualTo(0L);
    }

    #endregion

    #region Clear Tests

    [Test]
    [MethodDataSource(nameof(ItemCounts))]
    public async Task Clear_ShouldRemoveAllPoints(int itemCount)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        for (int i = 0; i < itemCount; i++)
        {
            tree.Add(CreatePoint2D(i));
        }

        tree.Clear();

        await Assert.That(tree.Count).IsEqualTo(0L);
    }

    [Test]
    public async Task Clear_EmptyTree_ShouldNotThrow()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        tree.Clear();

        await Assert.That(tree.Count).IsEqualTo(0L);
    }

    #endregion

    #region GetAll and Enumeration Tests

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task GetAll_ShouldReturnAllPoints(LeafCapacityAndItemCount args)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        HashSet<(double X, double Y)> addedPoints = new HashSet<(double X, double Y)>();
        for (int i = 0; i < args.ItemCount; i++)
        {
            (double X, double Y) point = CreatePoint2D(i);
            tree.Add(point);
            addedPoints.Add(point);
        }

        List<(double X, double Y)> retrievedPoints = tree.GetAll().ToList();

        await Assert.That(retrievedPoints.Count).IsEqualTo(args.ItemCount);
        foreach ((double X, double Y) point in retrievedPoints)
        {
            await Assert.That(addedPoints.Contains(point)).IsTrue();
        }
    }

    [Test]
    public async Task GetAll_EmptyTree_ShouldReturnEmpty()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        List<(double X, double Y)> points = tree.GetAll().ToList();

        await Assert.That(points.Count).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task GetEnumerator_ShouldEnumerateAllPoints(LeafCapacityAndItemCount args)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        for (int i = 0; i < args.ItemCount; i++)
        {
            tree.Add(CreatePoint2D(i));
        }

        int count = 0;
        foreach ((double X, double Y) _ in tree)
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(args.ItemCount);
    }

    #endregion

    #region DoForEach Tests

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task DoForEach_Action_ShouldVisitAllPoints(LeafCapacityAndItemCount args)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        for (int i = 0; i < args.ItemCount; i++)
        {
            tree.Add(CreatePoint2D(i));
        }

        int count = 0;
        tree.DoForEach(_ => count++);

        await Assert.That(count).IsEqualTo(args.ItemCount);
    }

    [Test]
    public async Task DoForEach_NullAction_ShouldThrow()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        await Assert.That(() => tree.DoForEach((Action<(double X, double Y)>)null))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task DoForEach_StructAction_ShouldVisitAllPoints(LeafCapacityAndItemCount args)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        double expectedSumX = 0;
        double expectedSumY = 0;
        for (int i = 0; i < args.ItemCount; i++)
        {
            (double X, double Y) point = CreatePoint2D(i);
            tree.Add(point);
            expectedSumX += point.X;
            expectedSumY += point.Y;
        }

        CountAction action = new CountAction();
        tree.DoForEach(ref action);

        await Assert.That(action.Count).IsEqualTo((long)args.ItemCount);
        await Assert.That(action.SumX).IsEqualTo(expectedSumX);
        await Assert.That(action.SumY).IsEqualTo(expectedSumY);
    }

    #endregion

    #region Range Query Tests

    [Test]
    public async Task RangeQuery_ShouldReturnPointsInRange()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1.0, 1.0));
        tree.Add((2.0, 2.0));
        tree.Add((3.0, 3.0));
        tree.Add((10.0, 10.0));
        tree.Add((20.0, 20.0));

        List<(double X, double Y)> results = tree.RangeQuery(
            (0.0, 0.0),
            (5.0, 5.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(3);
        await Assert.That(results.Contains((1.0, 1.0))).IsTrue();
        await Assert.That(results.Contains((2.0, 2.0))).IsTrue();
        await Assert.That(results.Contains((3.0, 3.0))).IsTrue();
    }

    [Test]
    public async Task RangeQuery_2D_ShouldReturnPointsInRange()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1.0, 1.0));
        tree.Add((2.0, 2.0));
        tree.Add((3.0, 3.0));
        tree.Add((10.0, 10.0));

        List<(double X, double Y)> results = tree.RangeQuery((0.0, 0.0), (5.0, 5.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(3);
    }

    [Test]
    public async Task RangeQuery_3D_ShouldReturnPointsInRange()
    {
        LargeBKDTree<(double X, double Y, double Z), TestPoint3DAccessor> tree = CreateTree3D();
        tree.Add((1.0, 1.0, 1.0));
        tree.Add((2.0, 2.0, 2.0));
        tree.Add((3.0, 3.0, 3.0));
        tree.Add((10.0, 10.0, 10.0));

        List<(double X, double Y, double Z)> results = tree.RangeQuery((0.0, 0.0, 0.0), (5.0, 5.0, 5.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(3);
    }

    [Test]
    public async Task RangeQuery_EmptyRange_ShouldReturnEmpty()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((10.0, 10.0));
        tree.Add((20.0, 20.0));

        List<(double X, double Y)> results = tree.RangeQuery(
            (0.0, 0.0),
            (5.0, 5.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RangeQuery_EmptyTree_ShouldReturnEmpty()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        List<(double X, double Y)> results = tree.RangeQuery(
            (0.0, 0.0),
            (100.0, 100.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RangeQuery_NullMinPoint_ShouldThrow()
    {
        LargeBKDTree<double[], TestPointArrayAccessor> tree = CreateTreeND(3);

        await Assert.That(() => tree.RangeQuery(null, new double[] { 1.0, 1.0, 1.0 }).ToList())
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task RangeQuery_NullMaxPoint_ShouldThrow()
    {
        LargeBKDTree<double[], TestPointArrayAccessor> tree = CreateTreeND(3);

        await Assert.That(() => tree.RangeQuery(new double[] { 0.0, 0.0, 0.0 }, null).ToList())
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task RangeQuery_DimensionMismatch_ShouldThrow()
    {
        LargeBKDTree<double[], TestPointArrayAccessor> tree = CreateTreeND(3);

        await Assert.That(() => tree.RangeQuery(new double[] { 0.0, 0.0 }, new double[] { 1.0, 1.0 }).ToList())
            .Throws<ArgumentException>();
    }

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task RangeQuery_LargeDataSet_ShouldWork(LeafCapacityAndItemCount args)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        int expectedInRange = 0;
        for (int i = 0; i < args.ItemCount; i++)
        {
            (double X, double Y) point = CreatePoint2D(i);
            tree.Add(point);
            if (point.X >= 0 && point.X <= 50 && point.Y >= 0 && point.Y <= 100)
            {
                expectedInRange++;
            }
        }

        List<(double X, double Y)> results = tree.RangeQuery((0.0, 0.0), (50.0, 100.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(expectedInRange);
    }

    #endregion

    #region DoForEachInRange Tests

    [Test]
    public async Task DoForEachInRange_ShouldCallActionForPointsInRange()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1.0, 1.0));
        tree.Add((2.0, 2.0));
        tree.Add((3.0, 3.0));
        tree.Add((10.0, 10.0));
        tree.Add((20.0, 20.0));

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
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((10.0, 10.0));
        tree.Add((20.0, 20.0));

        int callCount = 0;
        tree.DoForEachInRange((0.0, 0.0), (5.0, 5.0), _ => callCount++);

        await Assert.That(callCount).IsEqualTo(0);
    }

    [Test]
    public async Task DoForEachInRange_EmptyTree_ShouldNotCallAction()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        int callCount = 0;
        tree.DoForEachInRange((0.0, 0.0), (100.0, 100.0), _ => callCount++);

        await Assert.That(callCount).IsEqualTo(0);
    }

    [Test]
    public async Task DoForEachInRange_ILargeAction_ShouldCallActionForPointsInRange()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1.0, 1.0));
        tree.Add((2.0, 2.0));
        tree.Add((3.0, 3.0));
        tree.Add((10.0, 10.0));

        CountAction action = new CountAction();
        tree.DoForEachInRange((0.0, 0.0), (5.0, 5.0), ref action);

        await Assert.That(action.Count).IsEqualTo(3);
    }

    [Test]
    public async Task DoForEachInRange_3D_ShouldWork()
    {
        LargeBKDTree<(double X, double Y, double Z), TestPoint3DAccessor> tree = CreateTree3D();
        tree.Add((1.0, 1.0, 1.0));
        tree.Add((2.0, 2.0, 2.0));
        tree.Add((10.0, 10.0, 10.0));

        List<(double X, double Y, double Z)> collected = new List<(double X, double Y, double Z)>();
        tree.DoForEachInRange((0.0, 0.0, 0.0), (5.0, 5.0, 5.0), point => collected.Add(point));

        await Assert.That(collected.Count).IsEqualTo(2);
    }

    #endregion

    #region CountInRange Tests

    [Test]
    public async Task CountInRange_ShouldCountPointsInRange()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1.0, 1.0));
        tree.Add((2.0, 2.0));
        tree.Add((3.0, 3.0));
        tree.Add((10.0, 10.0));

        long count = tree.CountInRange(
            (0.0, 0.0),
            (5.0, 5.0));

        await Assert.That(count).IsEqualTo(3L);
    }

    [Test]
    public async Task CountInRange_EmptyTree_ShouldReturnZero()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        long count = tree.CountInRange(
            (0.0, 0.0),
            (100.0, 100.0));

        await Assert.That(count).IsEqualTo(0L);
    }

    [Test]
    public async Task CountInRange_DimensionMismatch_ShouldThrow()
    {
        LargeBKDTree<double[], TestPointArrayAccessor> tree = CreateTreeND(3);

        await Assert.That(() => tree.CountInRange(new double[] { 0.0, 0.0 }, new double[] { 1.0, 1.0 }))
            .Throws<ArgumentException>();
    }

    #endregion

    #region TryGetFirstInRange Tests

    [Test]
    public async Task TryGetFirstInRange_MatchingPoint_ShouldReturnTrue()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1.0, 2.0));
        tree.Add((5.0, 5.0));
        tree.Add((10.0, 10.0));

        bool result = tree.TryGetFirstInRange(
            (0.0, 0.0),
            (3.0, 3.0),
            out (double X, double Y) firstPoint);

        await Assert.That(result).IsTrue();
        await Assert.That(firstPoint).IsEqualTo((1.0, 2.0));
    }

    [Test]
    public async Task TryGetFirstInRange_NoMatchingPoint_ShouldReturnFalse()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((10.0, 10.0));
        tree.Add((20.0, 20.0));

        bool result = tree.TryGetFirstInRange(
            (0.0, 0.0),
            (5.0, 5.0),
            out _);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryGetFirstInRange_EmptyTree_ShouldReturnFalse()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        bool result = tree.TryGetFirstInRange(
            (0.0, 0.0),
            (100.0, 100.0),
            out _);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryGetFirstInRange_MultipleMatches_ShouldReturnOne()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1.0, 1.0));
        tree.Add((2.0, 2.0));
        tree.Add((3.0, 3.0));
        tree.Add((50.0, 50.0));

        bool result = tree.TryGetFirstInRange(
            (0.0, 0.0),
            (10.0, 10.0),
            out (double X, double Y) firstPoint);

        await Assert.That(result).IsTrue();
        // Should return one of the three points in range
        bool isInRange = firstPoint.X >= 0.0 && firstPoint.X <= 10.0 &&
                         firstPoint.Y >= 0.0 && firstPoint.Y <= 10.0;
        await Assert.That(isInRange).IsTrue();
    }

    [Test]
    public async Task TryGetFirstInRange_DimensionMismatch_ShouldThrow()
    {
        LargeBKDTree<double[], TestPointArrayAccessor> tree = CreateTreeND(3);

        await Assert.That(() => tree.TryGetFirstInRange(new double[] { 0.0, 0.0 }, new double[] { 1.0, 1.0 }, out _))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task TryGetFirstInRange_2D_MatchingPoint_ShouldReturnTrue()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1.0, 2.0));
        tree.Add((10.0, 10.0));

        bool result = tree.TryGetFirstInRange((0.0, 0.0), (5.0, 5.0), out (double X, double Y) firstPoint);

        await Assert.That(result).IsTrue();
        await Assert.That(firstPoint).IsEqualTo((1.0, 2.0));
    }

    [Test]
    public async Task TryGetFirstInRange_3D_MatchingPoint_ShouldReturnTrue()
    {
        LargeBKDTree<(double X, double Y, double Z), TestPoint3DAccessor> tree = CreateTree3D();
        tree.Add((1.0, 2.0, 3.0));
        tree.Add((10.0, 10.0, 10.0));

        bool result = tree.TryGetFirstInRange((0.0, 0.0, 0.0), (5.0, 5.0, 5.0), out (double X, double Y, double Z) firstPoint);

        await Assert.That(result).IsTrue();
        await Assert.That(firstPoint).IsEqualTo((1.0, 2.0, 3.0));
    }

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task TryGetFirstInRange_LargeDataSet_ShouldFindMatchIfExists(LeafCapacityAndItemCount args)
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        for (int i = 0; i < args.ItemCount; i++)
        {
            tree.Add(CreatePoint2D(i));
        }

        // Search for points in the first quadrant of our test data range
        bool result = tree.TryGetFirstInRange(
            (0.0, 0.0),
            (50.0, 50.0),
            out (double X, double Y) firstPoint);

        if (args.ItemCount > 0)
        {
            await Assert.That(result).IsTrue();
            // Verify the point is actually in range
            await Assert.That(firstPoint.X >= 0.0 && firstPoint.X <= 50.0).IsTrue();
            await Assert.That(firstPoint.Y >= 0.0 && firstPoint.Y <= 50.0).IsTrue();
        }
        else
        {
            await Assert.That(result).IsFalse();
        }
    }

    #endregion

    #region Nearest Neighbor Tests

    [Test]
    public async Task NearestNeighbor_ShouldReturnClosestPoint()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((0.0, 0.0));
        tree.Add((10.0, 10.0));
        tree.Add((5.0, 5.0));

        (double X, double Y) nearest = tree.NearestNeighbor((4.0, 4.0));

        await Assert.That(nearest).IsEqualTo((5.0, 5.0));
    }

    [Test]
    public async Task NearestNeighbor_EmptyTree_ShouldThrow()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        await Assert.That(() => tree.NearestNeighbor((1.0, 1.0)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TryGetNearestNeighbor_ExistingPoints_ShouldReturnTrue()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((0.0, 0.0));
        tree.Add((10.0, 10.0));

        bool result = tree.TryGetNearestNeighbor((1.0, 1.0), out (double X, double Y) nearest);

        await Assert.That(result).IsTrue();
        await Assert.That(nearest).IsEqualTo((0.0, 0.0));
    }

    [Test]
    public async Task TryGetNearestNeighbor_EmptyTree_ShouldReturnFalse()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        bool result = tree.TryGetNearestNeighbor((1.0, 1.0), out _);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryGetNearestNeighborWithDistance_ShouldReturnDistanceSquared()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((0.0, 0.0));
        tree.Add((3.0, 4.0)); // Distance from origin: 5, squared: 25

        bool result = tree.TryGetNearestNeighborWithDistance((0.0, 0.0), out (double X, double Y) nearest, out double distSq);

        await Assert.That(result).IsTrue();
        await Assert.That(nearest).IsEqualTo((0.0, 0.0));
        await Assert.That(distSq).IsEqualTo(0.0);
    }

    [Test]
    [MethodDataSource(nameof(LeafCapacitiesAndItemCounts))]
    public async Task NearestNeighbor_LargeDataSet_ShouldFindCorrectPoint(LeafCapacityAndItemCount args)
    {
        if (args.ItemCount == 0)
        {
            return;
        }

        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D(args.LeafCapacity);
        List<(double X, double Y)> points = new List<(double X, double Y)>();
        for (int i = 0; i < args.ItemCount; i++)
        {
            (double X, double Y) point = CreatePoint2D(i);
            points.Add(point);
            tree.Add(point);
        }

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

    #endregion

    #region FindPointsWithinDistance Tests

    [Test]
    public async Task FindPointsWithinDistance_ShouldReturnPointsInRadius()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((0.0, 0.0));
        tree.Add((1.0, 0.0));
        tree.Add((0.0, 1.0));
        tree.Add((10.0, 10.0));

        // Distance of sqrt(2) ≈ 1.41, squared = 2
        List<(double X, double Y)> results = tree.FindPointsWithinDistance((0.0, 0.0), 2.0).ToList();

        await Assert.That(results.Count).IsEqualTo(3); // (0,0), (1,0), (0,1)
        await Assert.That(results.Contains((0.0, 0.0))).IsTrue();
        await Assert.That(results.Contains((1.0, 0.0))).IsTrue();
        await Assert.That(results.Contains((0.0, 1.0))).IsTrue();
    }

    [Test]
    public async Task FindPointsWithinDistance_EmptyTree_ShouldReturnEmpty()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        List<(double X, double Y)> results = tree.FindPointsWithinDistance((0.0, 0.0), 100.0).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FindPointsWithinDistance_NegativeDistance_ShouldThrow()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();

        await Assert.That(() => tree.FindPointsWithinDistance((0.0, 0.0), -1.0).ToList())
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task FindPointsWithinDistance_ZeroDistance_ShouldReturnExactMatches()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((0.0, 0.0));
        tree.Add((1.0, 1.0));

        List<(double X, double Y)> results = tree.FindPointsWithinDistance((0.0, 0.0), 0.0).ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0]).IsEqualTo((0.0, 0.0));
    }

    #endregion

    #region 3D Tests

    [Test]
    public async Task Add3D_ShouldWork()
    {
        LargeBKDTree<(double X, double Y, double Z), TestPoint3DAccessor> tree = CreateTree3D();

        tree.Add((1.0, 2.0, 3.0));
        tree.Add((4.0, 5.0, 6.0));

        await Assert.That(tree.Count).IsEqualTo(2L);
    }

    [Test]
    public async Task RangeQuery_3D_Large_ShouldWork()
    {
        LargeBKDTree<(double X, double Y, double Z), TestPoint3DAccessor> tree = CreateTree3D();
        tree.Add((1.0, 1.0, 1.0));
        tree.Add((2.0, 2.0, 2.0));
        tree.Add((10.0, 10.0, 10.0));

        List<(double X, double Y, double Z)> results = tree.RangeQuery((0.0, 0.0, 0.0), (5.0, 5.0, 5.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task NearestNeighbor3D_ShouldWork()
    {
        LargeBKDTree<(double X, double Y, double Z), TestPoint3DAccessor> tree = CreateTree3D();
        tree.Add((0.0, 0.0, 0.0));
        tree.Add((10.0, 10.0, 10.0));

        (double X, double Y, double Z) nearest = tree.NearestNeighbor((1.0, 1.0, 1.0));

        await Assert.That(nearest).IsEqualTo((0.0, 0.0, 0.0));
    }

    #endregion

    #region N-Dimensional Tests

    [Test]
    public async Task NDimensional_5D_ShouldWork()
    {
        LargeBKDTree<double[], TestPointArrayAccessor> tree = CreateTreeND(5);

        tree.Add(new double[] { 1, 2, 3, 4, 5 });
        tree.Add(new double[] { 6, 7, 8, 9, 10 });

        await Assert.That(tree.Count).IsEqualTo(2L);
        await Assert.That(tree.Dimensions).IsEqualTo(5);
    }

    [Test]
    public async Task NDimensional_RangeQuery_ShouldWork()
    {
        LargeBKDTree<double[], TestPointArrayAccessor> tree = CreateTreeND(3);
        tree.Add(new double[] { 1, 1, 1 });
        tree.Add(new double[] { 2, 2, 2 });
        tree.Add(new double[] { 10, 10, 10 });

        List<double[]> results = tree.RangeQuery(
            new double[] { 0, 0, 0 },
            new double[] { 5, 5, 5 }).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    #endregion

    #region Edge Case Tests

    [Test]
    public async Task SinglePoint_AllOperations_ShouldWork()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((5.0, 5.0));

        await Assert.That(tree.Contains((5.0, 5.0))).IsTrue();
        await Assert.That(tree.RangeQuery((0.0, 0.0), (10.0, 10.0)).Count()).IsEqualTo(1);
        await Assert.That(tree.NearestNeighbor((0.0, 0.0))).IsEqualTo((5.0, 5.0));
        await Assert.That(tree.CountInRange((0.0, 0.0), (10.0, 10.0))).IsEqualTo(1L);
    }

    [Test]
    public async Task PointsOnBoundary_RangeQuery_ShouldInclude()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((0.0, 0.0));
        tree.Add((10.0, 10.0));

        List<(double X, double Y)> results = tree.RangeQuery((0.0, 0.0), (10.0, 10.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task NegativeCoordinates_ShouldWork()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((-5.0, -5.0));
        tree.Add((5.0, 5.0));
        tree.Add((-5.0, 5.0));
        tree.Add((5.0, -5.0));

        List<(double X, double Y)> results = tree.RangeQuery((-10.0, -10.0), (0.0, 0.0)).ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0]).IsEqualTo((-5.0, -5.0));
    }

    [Test]
    public async Task VeryLargeCoordinates_ShouldWork()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1e15, 1e15));
        tree.Add((-1e15, -1e15));

        await Assert.That(tree.Count).IsEqualTo(2L);
        await Assert.That(tree.Contains((1e15, 1e15))).IsTrue();
    }

    [Test]
    public async Task VerySmallCoordinates_ShouldWork()
    {
        LargeBKDTree<(double X, double Y), TestPoint2DAccessor> tree = CreateTree2D();
        tree.Add((1e-15, 1e-15));
        tree.Add((2e-15, 2e-15));

        await Assert.That(tree.Count).IsEqualTo(2L);
    }

    #endregion
}
