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
using System.Threading.Tasks;
using LargeCollections.Test;
using TUnit.Assertions.Enums;

namespace LargeCollections.DiskCache;

#pragma warning disable TUnit0030 // Class doesn't pick up tests from the base class
public class SpatialDiskCacheTest : DiskCacheTest
#pragma warning restore TUnit0030 // Class doesn't pick up tests from the base class
{

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task SetAndGet_Long(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_long_set_get_{count}");
        using SpatialDiskCache<long> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Test setting and getting values with spatial data
        for (long i = 0; i < count; i++)
        {
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, i, boundingBox);

            await Assert.That(spatialCache.Count).IsEqualTo(i + 1L);
            await Assert.That(spatialCache.TryGetValue(i, out long foundValue)).IsTrue();
            await Assert.That(foundValue).IsEqualTo(i);
            await Assert.That(spatialCache[i]).IsEqualTo(i);
            await Assert.That(spatialCache.Get(i)).IsEqualTo(i);
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task SetAndGet_String(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_string_set_get_{count}");
        using SpatialDiskCache<string> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Test null parameter handling for spatial-specific methods
        BoundingBox testBox = new(0, 1, 0, 1);
        await Assert.That(() => spatialCache.AddParallel(null))
            .Throws<Exception>();

        // Test inherited null parameter handling
        await Assert.That(() => spatialCache.AddRange((IEnumerable<KeyValuePair<long, string>>)null))
            .Throws<Exception>();

        long previousCount = spatialCache.Count;
        spatialCache.AddRange((ReadOnlySpan<KeyValuePair<long, string>>)null);
        await Assert.That(spatialCache.Count).IsEqualTo(previousCount);

        // Test Remove with individual key (new API)
        bool removeResult = spatialCache.Remove(999L); // Non-existing key
        await Assert.That(removeResult).IsFalse();
        await Assert.That(() => spatialCache.DoForEach((Action<KeyValuePair<long, string>>)null))
            .Throws<Exception>();

        // Test setting and getting values with spatial data
        for (long i = 0; i < count; i++)
        {
            string stringI = i.ToString();
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, stringI, boundingBox);

            await Assert.That(spatialCache.Count).IsEqualTo(i + 1L);
            await Assert.That(spatialCache.TryGetValue(i, out string foundValue)).IsTrue();
            await Assert.That(foundValue).IsEqualTo(stringI);
            await Assert.That(spatialCache[i]).IsEqualTo(stringI);
            await Assert.That(spatialCache.Get(i)).IsEqualTo(stringI);
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task SetAndGet_ByteArray(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_bytes_set_get_{count}");
        using SpatialDiskCache<byte[]> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Test setting and getting values with spatial data
        for (long i = 0; i < count; i++)
        {
            byte[] bytesI = BitConverter.GetBytes(i);
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, bytesI, boundingBox);

            await Assert.That(spatialCache.Count).IsEqualTo(i + 1L);
            await Assert.That(spatialCache.TryGetValue(i, out byte[] foundValue)).IsTrue();
            await Assert.That(ByteArraysEqual(foundValue, bytesI)).IsTrue();
            await Assert.That(ByteArraysEqual(spatialCache[i], bytesI)).IsTrue();
            await Assert.That(ByteArraysEqual(spatialCache.Get(i), bytesI)).IsTrue();
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task SetAndGet_Struct(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_struct_set_get_{count}");
        using SpatialDiskCache<LongStruct> spatialCache = new(cacheName, degreeOfParallelism: 4,
            serializeValueFunction: LongStruct.Serialize,
            deserializeValueFunction: LongStruct.Deserialize);

        // Test setting and getting values with spatial data
        for (long i = 0; i < count; i++)
        {
            LongStruct structI = new(i);
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, structI, boundingBox);

            await Assert.That(spatialCache.Count).IsEqualTo(i + 1L);
            await Assert.That(spatialCache.TryGetValue(i, out LongStruct foundValue)).IsTrue();
            await Assert.That(foundValue).IsEqualTo(structI);
            await Assert.That(spatialCache[i]).IsEqualTo(structI);
            await Assert.That(spatialCache.Get(i)).IsEqualTo(structI);
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Add_Long(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_long_add_{count}");
        using SpatialDiskCache<long> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Test adding values with spatial data
        for (long i = 0; i < count; i++)
        {
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Add(new KeyValuePair<long, long>(i, i), boundingBox);

            await Assert.That(spatialCache.Count).IsEqualTo(i + 1L);
            await Assert.That(spatialCache[i]).IsEqualTo(i);
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task AddCollection_Long(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_long_add_collection_{count}");
        using SpatialDiskCache<long> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Create collection to add
        List<(KeyValuePair<long, long>, BoundingBox)> itemsToAdd = [];
        for (long i = 0; i < count; i++)
        {
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            itemsToAdd.Add((new KeyValuePair<long, long>(i, i), boundingBox));
        }

        // Add all items at once
        spatialCache.Add(itemsToAdd);

        await Assert.That(spatialCache.Count).IsEqualTo(count);

        // Verify all items were added correctly
        for (long i = 0; i < count; i++)
        {
            await Assert.That(spatialCache[i]).IsEqualTo(i);
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Contains_Long(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_long_contains_{count}");
        using SpatialDiskCache<long> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, i, boundingBox);
        }

        // Test ContainsKey and Contains
        for (long i = 0; i < count; i++)
        {
            await Assert.That(spatialCache.ContainsKey(i)).IsTrue();
            await Assert.That(spatialCache.Contains(new KeyValuePair<long, long>(i, i))).IsTrue();
        }

        // Test non-existing keys
        await Assert.That(spatialCache.ContainsKey(count + 100)).IsFalse();
        await Assert.That(spatialCache.Contains(new KeyValuePair<long, long>(count + 100, count + 100))).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Enumeration_Long(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_long_enum_{count}");
        using SpatialDiskCache<long> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, i, boundingBox);
        }

        // Test enumeration - order doesn't matter, use CollectionOrdering.Any
        IEnumerable<long> expectedKeys = LargeEnumerable.Range(0, count);
        IEnumerable<long> expectedValues = LargeEnumerable.Range(0, count);
        IEnumerable<KeyValuePair<long, long>> expectedPairs = LargeEnumerable.Range(0, count).Select(i => new KeyValuePair<long, long>(i, i));

        await Assert.That(spatialCache.Keys).IsEquivalentTo(expectedKeys, CollectionOrdering.Any);
        await Assert.That(spatialCache.Values).IsEquivalentTo(expectedValues, CollectionOrdering.Any);
        await Assert.That(spatialCache).IsEquivalentTo(expectedPairs, CollectionOrdering.Any);
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Enumeration_ByteArray(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_bytes_enum_{count}");
        using SpatialDiskCache<byte[]> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            byte[] bytesI = BitConverter.GetBytes(i);
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, bytesI, boundingBox);
        }

        // Test enumeration - order is not guaranteed, so we need to sort for comparison
        IEnumerable<long> expectedKeys = LargeEnumerable.Range(0, count);
        IEnumerable<byte[]> expectedValues = LargeEnumerable.Range(0, count).Select(i => BitConverter.GetBytes(i));
        IEnumerable<KeyValuePair<long, byte[]>> expectedPairs = LargeEnumerable.Range(0, count).Select(i => new KeyValuePair<long, byte[]>(i, BitConverter.GetBytes(i)));

        long[] actualKeys = spatialCache.Keys.OrderBy(k => k).ToArray();
        long[] expectedKeysArray = expectedKeys.ToArray();

        byte[][] actualValues = spatialCache.Values.ToArray();
        byte[][] expectedValuesArray = expectedValues.ToArray();
        Array.Sort(actualValues, (a, b) => BitConverter.ToInt64(a, 0).CompareTo(BitConverter.ToInt64(b, 0)));
        Array.Sort(expectedValuesArray, (a, b) => BitConverter.ToInt64(a, 0).CompareTo(BitConverter.ToInt64(b, 0)));

        KeyValuePair<long, byte[]>[] actualPairs = spatialCache.OrderBy(kvp => kvp.Key).ToArray();
        KeyValuePair<long, byte[]>[] expectedPairsArray = expectedPairs.ToArray();

        await Assert.That(actualKeys).IsEquivalentTo(expectedKeysArray, CollectionOrdering.Any);
        await Assert.That(ByteArrayCollectionsEqual(actualValues, expectedValuesArray)).IsTrue();
        await Assert.That(KeyValuePairByteArrayCollectionsEqual(actualPairs, expectedPairsArray)).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task SpatialQuery_Long(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_long_query_{count}");
        using SpatialDiskCache<long> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache with items at various spatial positions
        for (long i = 0; i < count; i++)
        {
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, i, boundingBox);
        }

        // Query for items in a specific spatial range
        double queryMin = count * 0.25;
        double queryMax = count * 0.75;
        BoundingBox queryBox = new(queryMin, queryMax, queryMin, queryMax);

        IEnumerable<KeyValuePair<long, long>> queryResults = spatialCache.Query(queryBox);

        // For spatial queries, we test that:
        // 1. All returned results are within the expected range
        // 2. At least some results are returned for non-empty ranges
        List<KeyValuePair<long, long>> resultsList = queryResults.ToList();

        if (count > 0)
        {
            // Verify all returned results are within bounds
            foreach (KeyValuePair<long, long> result in resultsList)
            {
                await Assert.That(result.Key).IsGreaterThanOrEqualTo((long)Math.Ceiling(queryMin - 0.5));
                await Assert.That(result.Key).IsLessThanOrEqualTo((long)Math.Floor(queryMax));
                await Assert.That(result.Value).IsEqualTo(result.Key);
            }

            // For larger ranges, we expect at least some results
            if (queryMax - queryMin > 2)
            {
                await Assert.That(resultsList.Count).IsGreaterThan(0);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task SpatialQuery_String(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_string_query_{count}");
        using SpatialDiskCache<string> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache with items at various spatial positions
        for (long i = 0; i < count; i++)
        {
            string stringI = i.ToString();
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, stringI, boundingBox);
        }

        // Query for items in a specific spatial range
        double queryMin = count * 0.25;
        double queryMax = count * 0.75;
        BoundingBox queryBox = new(queryMin, queryMax, queryMin, queryMax);

        IEnumerable<KeyValuePair<long, string>> queryResults = spatialCache.Query(queryBox);

        // For spatial queries, we test that:
        // 1. All returned results are within the expected range
        // 2. At least some results are returned for non-empty ranges
        List<KeyValuePair<long, string>> resultsList = queryResults.ToList();

        if (count > 0)
        {
            // Verify all returned results are within bounds
            foreach (KeyValuePair<long, string> result in resultsList)
            {
                await Assert.That(result.Key).IsGreaterThanOrEqualTo((long)Math.Ceiling(queryMin - 0.5));
                await Assert.That(result.Key).IsLessThanOrEqualTo((long)Math.Floor(queryMax));
                await Assert.That(result.Value).IsEqualTo(result.Key.ToString());
            }

            // For larger ranges, we expect at least some results
            if (queryMax - queryMin > 2)
            {
                await Assert.That(resultsList.Count).IsGreaterThan(0);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task SpatialQueryParallel_Long(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_long_query_parallel_{count}");
        using SpatialDiskCache<long> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache with items at various spatial positions
        for (long i = 0; i < count; i++)
        {
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, i, boundingBox);
        }

        // Query for items in a specific spatial range using parallel query
        double queryMin = count * 0.25;
        double queryMax = count * 0.75;
        BoundingBox queryBox = new(queryMin, queryMax, queryMin, queryMax);

        IEnumerable<KeyValuePair<long, long>>[] parallelResults = spatialCache.QueryParallel(queryBox);
        IEnumerable<KeyValuePair<long, long>> combinedResults = parallelResults.SelectMany(x => x);

        // For spatial queries, we test that:
        // 1. All returned results are within the expected range
        // 2. At least some results are returned for non-empty ranges
        List<KeyValuePair<long, long>> resultsList = combinedResults.ToList();

        if (count > 0)
        {
            // Verify all returned results are within bounds
            foreach (KeyValuePair<long, long> result in resultsList)
            {
                await Assert.That(result.Key).IsGreaterThanOrEqualTo((long)Math.Ceiling(queryMin - 0.5));
                await Assert.That(result.Key).IsLessThanOrEqualTo((long)Math.Floor(queryMax));
                await Assert.That(result.Value).IsEqualTo(result.Key);
            }

            // For larger ranges, we expect at least some results
            if (queryMax - queryMin > 2)
            {
                await Assert.That(resultsList.Count).IsGreaterThan(0);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Remove_Long(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"spatial_long_remove_{count}");
        using SpatialDiskCache<long> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, i, boundingBox);
        }

        // Test removal
        for (long i = 0; i < count; i++)
        {
            spatialCache.Remove(i);

            await Assert.That(spatialCache.Count).IsEqualTo(count - 1L - i);
            await Assert.That(spatialCache.TryGetValue(i, out long foundValue)).IsFalse();
        }
    }

    [Test]
    public async Task Clear_Long()
    {
        string cacheName = CreateTestCacheName("spatial_long_clear");
        using SpatialDiskCache<long> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < 10; i++)
        {
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, i, boundingBox);
        }

        await Assert.That(spatialCache.Count).IsEqualTo(10L);

        // Clear cache
        spatialCache.Clear();

        await Assert.That(spatialCache.Count).IsEqualTo(0L);
        await Assert.That(spatialCache.TryGetValue(0, out long foundValue)).IsFalse();
    }

    [Test]
    public async Task BoundingBoxValidation()
    {
        string cacheName = CreateTestCacheName("spatial_validation");
        using SpatialDiskCache<long> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Test invalid bounding boxes
        BoundingBox invalidBoxX = new(10, 5, 0, 5); // MinX > MaxX
        BoundingBox invalidBoxY = new(0, 5, 10, 5); // MinY > MaxY

        await Assert.That(() => spatialCache.Set(1, 1, invalidBoxX)).Throws<Exception>();
        await Assert.That(() => spatialCache.Set(2, 2, invalidBoxY)).Throws<Exception>();
    }

    [Test]
    public async Task SpatialQueryEmpty()
    {
        string cacheName = CreateTestCacheName("spatial_query_empty");
        using SpatialDiskCache<long> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Query empty cache
        BoundingBox queryBox = new(0, 10, 0, 10);
        IEnumerable<KeyValuePair<long, long>> results = spatialCache.Query(queryBox);

        await Assert.That(results.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task SpatialQueryNoOverlap()
    {
        string cacheName = CreateTestCacheName("spatial_query_no_overlap");
        using SpatialDiskCache<long> spatialCache = new(cacheName, degreeOfParallelism: 4);

        // Add items in one area
        for (long i = 0; i < 5; i++)
        {
            BoundingBox boundingBox = new(i, i + 0.5, i, i + 0.5);
            spatialCache.Set(i, i, boundingBox);
        }

        // Query in a completely different area
        BoundingBox queryBox = new(100, 110, 100, 110);
        IEnumerable<KeyValuePair<long, long>> results = spatialCache.Query(queryBox);

        await Assert.That(results.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task ConnectionPool_SpatialConcurrency()
    {
        const int concurrentCaches = 20;
        const int operationsPerCache = 50;

        List<Task> tasks = [];
        List<SpatialDiskCache<long>> caches = [];

        try
        {
            // Create multiple concurrent spatial caches
            for (int i = 0; i < concurrentCaches; i++)
            {
                string cacheName = CreateTestCacheName($"spatial_concurrent_test_{i}");
                SpatialDiskCache<long> cache = new(cacheName, degreeOfParallelism: 4);
                caches.Add(cache);

                // Each cache performs spatial operations concurrently
                Task task = Task.Run(async () =>
                {
                    for (int j = 0; j < operationsPerCache; j++)
                    {
                        // Mix of spatial operations
                        BoundingBox boundingBox = new(j, j + 0.5, j, j + 0.5);
                        cache.Set(j, j * 2, boundingBox);

                        long retrieved = cache[j];
                        await Assert.That(retrieved).IsEqualTo(j * 2);

                        // Spatial query
                        if (j % 10 == 0)
                        {
                            BoundingBox queryBox = new(0, j + 1, 0, j + 1);
                            IEnumerable<KeyValuePair<long, long>> queryResults = cache.Query(queryBox);
                            await Assert.That(queryResults.Count()).IsGreaterThan(0);
                        }
                    }
                });

                tasks.Add(task);
            }

            // Wait for all concurrent operations to complete
            await Task.WhenAll(tasks);

            // Verify final state
            for (int i = 0; i < concurrentCaches; i++)
            {
                await Assert.That(caches[i].Count).IsEqualTo((long)operationsPerCache);
            }
        }
        finally
        {
            // Cleanup all caches
            foreach (SpatialDiskCache<long> cache in caches)
            {
                cache?.Dispose();
            }
        }
    }
}
