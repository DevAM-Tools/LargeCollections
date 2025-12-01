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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Enums;

namespace LargeCollections.DiskCache;

public struct LongStruct(long value)
{
    public long Value = value;

    public static byte[] Serialize(LongStruct longStruct)
    {
        return BitConverter.GetBytes(longStruct.Value);
    }

    public static LongStruct Deserialize(byte[] serializedLongStruct)
    {
        long value = BitConverter.ToInt64(serializedLongStruct, 0);
        return new LongStruct(value);
    }
}

public class DiskCacheTest : IDisposable
{
    public static IEnumerable<long> CountTestCasesArguments()
    {
        yield return -2L;
        yield return -1L;
        yield return 0L;
        yield return 1L;
        yield return 2L;
        yield return 3L;
        yield return 4L;
        yield return 5L;
        yield return 10L;
        yield return 50L;
        yield return 100L;
    }

    private static readonly string _TestRootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DiskCacheFiles");

    private static readonly Random _Random = new();

    // Global cleanup state management
    private static readonly object _CleanupLock = new();
    private static bool _InitialCleanupDone = false;
    private static bool _FinalCleanupDone = false;
    private static int _ActiveTestInstances = 0;

    public DiskCacheTest()
    {
        // Ensure initial cleanup is done only once and track active instances
        EnsureInitialCleanup();

        lock (_CleanupLock)
        {
            _ActiveTestInstances++;
        }
    }

    protected string CreateTestCacheName(string baseName)
    {
        string cacheName = Path.Combine(_TestRootDirectory, $"{baseName}_{GenerateRandomString(12)}");
        return cacheName;
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_Random.Next(s.Length)]).ToArray());
    }

    protected static bool ByteArraysEqual(byte[] a, byte[] b)
    {
        if (a == null && b == null)
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }
        if (a.Length != b.Length)
        {
            return false;
        }

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }
        return true;
    }

    protected static bool ByteArrayCollectionsEqual(IEnumerable<byte[]> actual, IEnumerable<byte[]> expected)
    {
        byte[][] actualArray = actual.ToArray();
        byte[][] expectedArray = expected.ToArray();

        if (actualArray.Length != expectedArray.Length)
        {
            return false;
        }

        for (int i = 0; i < actualArray.Length; i++)
        {
            byte[] actualBytes = actualArray[i];
            byte[] expectedBytes = expectedArray[i];
            if (!ByteArraysEqual(actualBytes, expectedBytes))
            {
                return false;
            }
        }
        return true;
    }

    protected static bool KeyValuePairByteArrayCollectionsEqual(IEnumerable<KeyValuePair<byte[], byte[]>> actual, IEnumerable<KeyValuePair<byte[], byte[]>> expected)
    {
        KeyValuePair<byte[], byte[]>[] actualArray = actual.ToArray();
        KeyValuePair<byte[], byte[]>[] expectedArray = expected.ToArray();

        if (actualArray.Length != expectedArray.Length) return false;

        for (int i = 0; i < actualArray.Length; i++)
        {
            if (!ByteArraysEqual(actualArray[i].Key, expectedArray[i].Key)
                || !ByteArraysEqual(actualArray[i].Value, expectedArray[i].Value))
            {
                return false;
            }
        }
        return true;
    }

    protected static bool KeyValuePairByteArrayCollectionsEqual(IEnumerable<KeyValuePair<long, byte[]>> actual, IEnumerable<KeyValuePair<long, byte[]>> expected)
    {
        KeyValuePair<long, byte[]>[] actualArray = actual.ToArray();
        KeyValuePair<long, byte[]>[] expectedArray = expected.ToArray();

        if (actualArray.Length != expectedArray.Length) return false;

        for (int i = 0; i < actualArray.Length; i++)
        {
            if (actualArray[i].Key != expectedArray[i].Key
                || !ByteArraysEqual(actualArray[i].Value, expectedArray[i].Value))
            {
                return false;
            }
        }
        return true;
    }

    private static void EnsureInitialCleanup()
    {
        lock (_CleanupLock)
        {
            if (!_InitialCleanupDone)
            {
                try
                {
                    // Perform initial cleanup - remove entire DiskCacheFiles directory if it exists
                    if (Directory.Exists(_TestRootDirectory))
                    {
                        // Force garbage collection to ensure any unreferenced connections are disposed
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        // Wait a bit to allow file handles to be released
                        Thread.Sleep(200);

                        Directory.Delete(_TestRootDirectory, recursive: true);
                    }

                    _InitialCleanupDone = true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to perform initial cleanup of DiskCacheFiles directory: {ex.Message}", ex);
                }
            }

            // Always ensure the directory exists after cleanup
            if (!Directory.Exists(_TestRootDirectory))
            {
                Directory.CreateDirectory(_TestRootDirectory);
            }
        }
    }

    private static void EnsureFinalCleanup()
    {
        lock (_CleanupLock)
        {
            _ActiveTestInstances--;

            // Only perform final cleanup when no more test instances are active
            if (_ActiveTestInstances <= 0 && !_FinalCleanupDone)
            {
                try
                {
                    // Perform final cleanup - remove entire DiskCacheFiles directory
                    if (Directory.Exists(_TestRootDirectory))
                    {
                        // Force garbage collection to ensure any unreferenced connections are disposed
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        // Wait a bit to allow file handles to be released
                        Thread.Sleep(200);

                        Directory.Delete(_TestRootDirectory, recursive: true);
                    }

                    _FinalCleanupDone = true;
                }
                catch
                {
                    // Final cleanup should not throw exceptions to avoid masking test failures
                    // If we can't cleanup, we'll let the OS handle it eventually
                }
            }
        }
    }

    /// <summary>
    /// Static method to cleanup the entire test root directory.
    /// Should be called after all tests are completed.
    /// </summary>
    public static void CleanupAllTestFiles()
    {
        lock (_CleanupLock)
        {
            // Force final cleanup regardless of active instance count
            if (!_FinalCleanupDone)
            {
                try
                {
                    // Perform final cleanup - remove entire DiskCacheFiles directory
                    if (Directory.Exists(_TestRootDirectory))
                    {
                        // Force garbage collection to ensure any unreferenced connections are disposed
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        // Wait a bit to allow file handles to be released
                        Thread.Sleep(200);

                        Directory.Delete(_TestRootDirectory, recursive: true);
                    }

                    _FinalCleanupDone = true;
                }
                catch
                {
                    // Final cleanup should not throw exceptions to avoid masking test failures
                    // If we can't cleanup, we'll let the OS handle it eventually
                }
            }
        }
    }

    public void Dispose()
    {
        EnsureFinalCleanup();
        GC.SuppressFinalize(this);
    }

    ~DiskCacheTest()
    {
        EnsureFinalCleanup();
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task AddAndGet_LongLong(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"long_long_add_get_{count}");
        using DiskCache<long, long> diskCache = new(cacheName, degreeOfParallelism: 4);

        // Test adding and getting values
        for (long i = 0; i < count; i++)
        {
            diskCache[i] = i;

            await Assert.That(diskCache.Count).IsEqualTo(i + 1L);
            await Assert.That(diskCache.TryGetValue(i, out long foundValue)).IsTrue();
            await Assert.That(foundValue).IsEqualTo(i);
            await Assert.That(diskCache[i]).IsEqualTo(i);
            await Assert.That(diskCache.Get(i)).IsEqualTo(i);
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task AddAndGet_StringString(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"string_string_add_get_{count}");
        using DiskCache<string, string> diskCache = new(cacheName, degreeOfParallelism: 4);

        // Test null parameter handling for string keys
        await Assert.That(() => diskCache.Set(null, "value"))
            .Throws<Exception>();
        await Assert.That(() => diskCache.Get(null))
            .Throws<Exception>();
        await Assert.That(() => diskCache.TryGetValue(null, out string foundValue))
            .Throws<Exception>();
        await Assert.That(() => diskCache.ContainsKey(null))
            .Throws<Exception>();
        await Assert.That(() => diskCache.Remove((string)null))
            .Throws<Exception>();
        await Assert.That(() => diskCache.Remove((string)null, out _))
            .Throws<Exception>();

        // Test adding and getting values
        for (long i = 0; i < count; i++)
        {
            string stringI = i.ToString();
            diskCache[stringI] = stringI;

            await Assert.That(diskCache.Count).IsEqualTo(i + 1L);
            await Assert.That(diskCache.TryGetValue(stringI, out string foundValue)).IsTrue();
            await Assert.That(foundValue).IsEqualTo(stringI);
            await Assert.That(diskCache[stringI]).IsEqualTo(stringI);
            await Assert.That(diskCache.Get(stringI)).IsEqualTo(stringI);
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task AddAndGet_BytesBytes(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"bytes_bytes_add_get_{count}");
        using DiskCache<byte[], byte[]> diskCache = new(cacheName, degreeOfParallelism: 4);

        // Test null parameter handling for byte array keys
        await Assert.That(() => diskCache.Set(null, [1, 2, 3]))
            .Throws<Exception>();
        await Assert.That(() => diskCache.Get(null))
            .Throws<Exception>();
        await Assert.That(() => diskCache.TryGetValue(null, out byte[] foundValue))
            .Throws<Exception>();
        await Assert.That(() => diskCache.ContainsKey(null))
            .Throws<Exception>();
        await Assert.That(() => diskCache.Remove((byte[])null))
            .Throws<Exception>();
        await Assert.That(() => diskCache.Remove((byte[])null, out _))
            .Throws<Exception>();

        // Test adding and getting values
        for (long i = 0; i < count; i++)
        {
            byte[] bytesI = BitConverter.GetBytes(i);
            diskCache[bytesI] = bytesI;

            await Assert.That(diskCache.Count).IsEqualTo(i + 1L);
            await Assert.That(diskCache.TryGetValue(bytesI, out byte[] foundValue)).IsTrue();
            await Assert.That(ByteArraysEqual(foundValue, bytesI)).IsTrue();
            await Assert.That(ByteArraysEqual(diskCache[bytesI], bytesI)).IsTrue();
            await Assert.That(ByteArraysEqual(diskCache.Get(bytesI), bytesI)).IsTrue();
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task AddAndGet_StructStruct(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"struct_struct_add_get_{count}");
        using DiskCache<LongStruct, LongStruct> diskCache = new(cacheName, degreeOfParallelism: 4,
            serializeKeyFunction: LongStruct.Serialize,
            deserializeKeyFunction: LongStruct.Deserialize,
            serializeValueFunction: LongStruct.Serialize,
            deserializeValueFunction: LongStruct.Deserialize);

        // Test adding and getting values
        for (long i = 0; i < count; i++)
        {
            LongStruct structI = new(i);
            diskCache[structI] = structI;

            await Assert.That(diskCache.Count).IsEqualTo(i + 1L);
            await Assert.That(diskCache.TryGetValue(structI, out LongStruct foundValue)).IsTrue();
            await Assert.That(foundValue).IsEqualTo(structI);
            await Assert.That(diskCache[structI]).IsEqualTo(structI);
            await Assert.That(diskCache.Get(structI)).IsEqualTo(structI);
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Contains_LongLong(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"long_long_contains_{count}");
        using DiskCache<long, long> diskCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            diskCache[i] = i;
        }

        // Test ContainsKey and Contains
        for (long i = 0; i < count; i++)
        {
            await Assert.That(diskCache.ContainsKey(i)).IsTrue();
            await Assert.That(diskCache.Contains(new KeyValuePair<long, long>(i, i))).IsTrue();
        }

        // Test non-existing keys
        await Assert.That(diskCache.ContainsKey(count + 100)).IsFalse();
        await Assert.That(diskCache.Contains(new KeyValuePair<long, long>(count + 100, count + 100))).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Contains_StringString(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"string_string_contains_{count}");
        using DiskCache<string, string> diskCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            string stringI = i.ToString();
            diskCache[stringI] = stringI;
        }

        // Test ContainsKey and Contains
        for (long i = 0; i < count; i++)
        {
            string stringI = i.ToString();
            await Assert.That(diskCache.ContainsKey(stringI)).IsTrue();
            await Assert.That(diskCache.Contains(new KeyValuePair<string, string>(stringI, stringI))).IsTrue();
        }

        // Test non-existing keys
        string nonExistingKey = (count + 100).ToString();
        await Assert.That(diskCache.ContainsKey(nonExistingKey)).IsFalse();
        await Assert.That(diskCache.Contains(new KeyValuePair<string, string>(nonExistingKey, nonExistingKey))).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Enumeration_LongLong(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"long_long_enum_{count}");
        using DiskCache<long, long> diskCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            diskCache[i] = i;
        }

        // Test enumeration - order doesn't matter, use CollectionOrdering.Any
        IEnumerable<long> expectedKeys = LargeEnumerable.Range(0, count);
        IEnumerable<long> expectedValues = LargeEnumerable.Range(0, count);
        IEnumerable<KeyValuePair<long, long>> expectedPairs = LargeEnumerable.Range(0, count).Select(i => new KeyValuePair<long, long>(i, i));

        await Assert.That(diskCache.Keys).IsEquivalentTo(expectedKeys, CollectionOrdering.Any);
        await Assert.That(diskCache.Values).IsEquivalentTo(expectedValues, CollectionOrdering.Any);
        await Assert.That(diskCache).IsEquivalentTo(expectedPairs, CollectionOrdering.Any);
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Enumeration_StringString(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"string_string_enum_{count}");
        using DiskCache<string, string> diskCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            string stringI = i.ToString();
            diskCache[stringI] = stringI;
        }

        // Test enumeration - order doesn't matter, use CollectionOrdering.Any
        IEnumerable<string> expectedKeys = LargeEnumerable.Range(0, count).Select(i => i.ToString());
        IEnumerable<string> expectedValues = LargeEnumerable.Range(0, count).Select(i => i.ToString());
        IEnumerable<KeyValuePair<string, string>> expectedPairs = LargeEnumerable.Range(0, count).Select(i => new KeyValuePair<string, string>(i.ToString(), i.ToString()));

        await Assert.That(diskCache.Keys).IsEquivalentTo(expectedKeys, CollectionOrdering.Any);
        await Assert.That(diskCache.Values).IsEquivalentTo(expectedValues, CollectionOrdering.Any);
        await Assert.That(diskCache).IsEquivalentTo(expectedPairs, CollectionOrdering.Any);
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Enumeration_BytesBytes(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"bytes_bytes_enum_{count}");
        using DiskCache<byte[], byte[]> diskCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            byte[] bytesI = BitConverter.GetBytes(i);
            diskCache[bytesI] = bytesI;
        }

        // Test enumeration - order is not guaranteed, so we need to sort for comparison
        IEnumerable<byte[]> expectedKeys = LargeEnumerable.Range(0, count).Select(i => BitConverter.GetBytes(i));
        IEnumerable<byte[]> expectedValues = LargeEnumerable.Range(0, count).Select(i => BitConverter.GetBytes(i));
        IEnumerable<KeyValuePair<byte[], byte[]>> expectedPairs = LargeEnumerable.Range(0, count).Select(i => new KeyValuePair<byte[], byte[]>(BitConverter.GetBytes(i), BitConverter.GetBytes(i)));

        byte[][] actualKeys = diskCache.Keys.ToArray();
        byte[][] expectedKeysArray = expectedKeys.ToArray();
        Array.Sort(actualKeys, (a, b) => BitConverter.ToInt64(a, 0).CompareTo(BitConverter.ToInt64(b, 0)));
        Array.Sort(expectedKeysArray, (a, b) => BitConverter.ToInt64(a, 0).CompareTo(BitConverter.ToInt64(b, 0)));

        byte[][] actualValues = diskCache.Values.ToArray();
        byte[][] expectedValuesArray = expectedValues.ToArray();
        Array.Sort(actualValues, (a, b) => BitConverter.ToInt64(a, 0).CompareTo(BitConverter.ToInt64(b, 0)));
        Array.Sort(expectedValuesArray, (a, b) => BitConverter.ToInt64(a, 0).CompareTo(BitConverter.ToInt64(b, 0)));

        KeyValuePair<byte[], byte[]>[] actualPairs = diskCache.ToArray();
        KeyValuePair<byte[], byte[]>[] expectedPairsArray = expectedPairs.ToArray();
        Array.Sort(actualPairs, (a, b) => BitConverter.ToInt64(a.Key, 0).CompareTo(BitConverter.ToInt64(b.Key, 0)));
        Array.Sort(expectedPairsArray, (a, b) => BitConverter.ToInt64(a.Key, 0).CompareTo(BitConverter.ToInt64(b.Key, 0)));

        await Assert.That(ByteArrayCollectionsEqual(actualKeys, expectedKeysArray)).IsTrue();
        await Assert.That(ByteArrayCollectionsEqual(actualValues, expectedValuesArray)).IsTrue();
        await Assert.That(KeyValuePairByteArrayCollectionsEqual(actualPairs, expectedPairsArray)).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Enumeration_StructStruct(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"struct_struct_enum_{count}");
        using DiskCache<LongStruct, LongStruct> diskCache = new(cacheName, degreeOfParallelism: 4,
            serializeKeyFunction: LongStruct.Serialize,
            deserializeKeyFunction: LongStruct.Deserialize,
            serializeValueFunction: LongStruct.Serialize,
            deserializeValueFunction: LongStruct.Deserialize);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            LongStruct structI = new(i);
            diskCache[structI] = structI;
        }

        // Test enumeration - order doesn't matter, use CollectionOrdering.Any
        IEnumerable<LongStruct> expectedKeys = LargeEnumerable.Range(0, count).Select(i => new LongStruct(i));
        IEnumerable<LongStruct> expectedValues = LargeEnumerable.Range(0, count).Select(i => new LongStruct(i));
        IEnumerable<KeyValuePair<LongStruct, LongStruct>> expectedPairs = LargeEnumerable.Range(0, count).Select(i => new KeyValuePair<LongStruct, LongStruct>(new LongStruct(i), new LongStruct(i)));

        await Assert.That(diskCache.Keys).IsEquivalentTo(expectedKeys, CollectionOrdering.Any);
        await Assert.That(diskCache.Values).IsEquivalentTo(expectedValues, CollectionOrdering.Any);
        await Assert.That(diskCache).IsEquivalentTo(expectedPairs, CollectionOrdering.Any);
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Remove_LongLong(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"long_long_remove_{count}");
        using DiskCache<long, long> diskCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            diskCache[i] = i;
        }

        // Test removal
        for (long i = 0; i < count; i++)
        {
            diskCache.Remove(i);

            await Assert.That(diskCache.Count).IsEqualTo(count - 1L - i);
            await Assert.That(diskCache.TryGetValue(i, out long foundValue)).IsFalse();
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Remove_StringString(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"string_string_remove_{count}");
        using DiskCache<string, string> diskCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            string stringI = i.ToString();
            diskCache[stringI] = stringI;
        }

        // Test removal
        for (long i = 0; i < count; i++)
        {
            string stringI = i.ToString();
            diskCache.Remove(stringI);

            await Assert.That(diskCache.Count).IsEqualTo(count - 1L - i);
            await Assert.That(diskCache.TryGetValue(stringI, out string foundValue)).IsFalse();
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Remove_BytesBytes(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"bytes_bytes_remove_{count}");
        using DiskCache<byte[], byte[]> diskCache = new(cacheName, degreeOfParallelism: 4);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            byte[] bytesI = BitConverter.GetBytes(i);
            diskCache[bytesI] = bytesI;
        }

        // Test removal
        for (long i = 0; i < count; i++)
        {
            byte[] bytesI = BitConverter.GetBytes(i);
            diskCache.Remove(bytesI);

            await Assert.That(diskCache.Count).IsEqualTo(count - 1L - i);
            await Assert.That(diskCache.TryGetValue(bytesI, out byte[] foundValue)).IsFalse();
        }
    }

    [Test]
    [MethodDataSource(nameof(CountTestCasesArguments))]
    public async Task Remove_StructStruct(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        string cacheName = CreateTestCacheName($"struct_struct_remove_{count}");
        using DiskCache<LongStruct, LongStruct> diskCache = new(cacheName, degreeOfParallelism: 4,
            serializeKeyFunction: LongStruct.Serialize,
            deserializeKeyFunction: LongStruct.Deserialize,
            serializeValueFunction: LongStruct.Serialize,
            deserializeValueFunction: LongStruct.Deserialize);

        // Fill cache
        for (long i = 0; i < count; i++)
        {
            LongStruct structI = new(i);
            diskCache[structI] = structI;
        }

        // Test removal
        for (long i = 0; i < count; i++)
        {
            LongStruct structI = new(i);
            diskCache.Remove(structI);

            await Assert.That(diskCache.Count).IsEqualTo(count - 1L - i);
            await Assert.That(diskCache.TryGetValue(structI, out LongStruct foundValue)).IsFalse();
        }
    }

    [Test]
    public async Task ConnectionPool_HighConcurrency()
    {
        const int concurrentCaches = 50;
        const int operationsPerCache = 100;

        List<Task> tasks = [];
        List<DiskCache<long, long>> caches = [];

        try
        {
            // Create multiple concurrent caches
            for (int i = 0; i < concurrentCaches; i++)
            {
                string cacheName = CreateTestCacheName($"concurrent_test_{i}");
                DiskCache<long, long> cache = new(cacheName, degreeOfParallelism: 8);
                caches.Add(cache);

                // Each cache performs operations concurrently
                Task task = Task.Run(async () =>
                {
                    for (int j = 0; j < operationsPerCache; j++)
                    {
                        // Mix of operations to stress the connection pool
                        cache[j] = j * 2;

                        long retrieved = cache[j];
                        await Assert.That(retrieved).IsEqualTo(j * 2);

                        bool exists = cache.ContainsKey(j);
                        await Assert.That(exists).IsTrue();

                        // Occasionally remove and re-add to create more connection activity
                        if (j % 10 == 0)
                        {
                            cache.Remove(j);
                            cache[j] = j * 2;
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
                await Assert.That(caches[i].Count).IsGreaterThanOrEqualTo((long)(operationsPerCache * 0.9)); // Allow some removed items
            }
        }
        finally
        {
            // Cleanup all caches
            foreach (DiskCache<long, long> cache in caches)
            {
                cache?.Dispose();
            }
        }
    }

    [Test]
    public async Task ConnectionPool_RapidCreateDestroy()
    {
        const int iterations = 100;
        const int operationsPerIteration = 50;

        // Rapidly create and destroy caches to stress connection pool cleanup
        for (int i = 0; i < iterations; i++)
        {
            string cacheName = CreateTestCacheName($"rapid_test_{i}");
            using DiskCache<long, string> cache = new(cacheName, degreeOfParallelism: 4);

            // Perform some operations
            for (int j = 0; j < operationsPerIteration; j++)
            {
                cache[j] = $"value_{j}";
                string value = cache[j];
                await Assert.That(value).IsEqualTo($"value_{j}");
            }

            await Assert.That(cache.Count).IsEqualTo(operationsPerIteration);

            // Cache will be disposed at end of using block
        }
    }

    [Test]
    public async Task ConnectionPool_LongRunningOperations()
    {
        const int parallelOperations = 20;
        string cacheName = CreateTestCacheName("longrunning_test");
        using DiskCache<long, byte[]> cache = new(cacheName, degreeOfParallelism: 8);

        List<Task> tasks = [];

        // Create long-running operations that might hold connections for extended periods
        for (int i = 0; i < parallelOperations; i++)
        {
            int taskId = i;
            Task task = Task.Run(async () =>
            {
                for (int j = 0; j < 1000; j++)
                {
                    // Create larger data to simulate more realistic usage
                    byte[] largeData = new byte[1024]; // 1KB per entry
                    for (int k = 0; k < largeData.Length; k++)
                    {
                        largeData[k] = (byte)((taskId + j + k) % 256);
                    }

                    long key = taskId * 1000 + j;
                    cache[key] = largeData;

                    // Simulate some processing time
                    await Task.Delay(1);

                    byte[] retrieved = cache[key];
                    await Assert.That(ByteArraysEqual(retrieved, largeData)).IsTrue();

                    // Occasionally enumerate to stress different connection usage patterns
                    if (j % 100 == 0)
                    {
                        int count = cache.Keys.Take(10).Count();
                        await Assert.That(count).IsGreaterThan(0);
                    }
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Verify final state
        long finalCount = cache.Count;
        await Assert.That(finalCount).IsEqualTo(parallelOperations * 1000);
    }

    [Test]
    public async Task ConnectionPool_TransactionStress()
    {
        const int batchSize = 100;
        const int numberOfBatches = 50;

        string cacheName = CreateTestCacheName("transaction_test");
        using DiskCache<string, string> cache = new(cacheName, degreeOfParallelism: 8);

        List<Task> tasks = [];

        // Create multiple concurrent batch operations
        for (int batch = 0; batch < numberOfBatches; batch++)
        {
            int batchId = batch;
            Task task = Task.Run(async () =>
            {
                // Perform batch operations that might use transactions internally
                List<KeyValuePair<string, string>> batchData = new List<KeyValuePair<string, string>>();

                for (int i = 0; i < batchSize; i++)
                {
                    string key = $"batch_{batchId}_item_{i}";
                    string value = $"value_{batchId}_{i}_{Guid.NewGuid()}";
                    batchData.Add(new KeyValuePair<string, string>(key, value));
                }

                // Add all items in the batch
                foreach (KeyValuePair<string, string> item in batchData)
                {
                    cache[item.Key] = item.Value;
                }

                // Verify all items in the batch
                foreach (KeyValuePair<string, string> item in batchData)
                {
                    string retrieved = cache[item.Key];
                    await Assert.That(retrieved).IsEqualTo(item.Value);
                }

                // Occasionally remove half the batch and re-add
                if (batchId % 5 == 0)
                {
                    for (int i = 0; i < batchSize / 2; i++)
                    {
                        string key = $"batch_{batchId}_item_{i}";
                        cache.Remove(key);
                    }

                    for (int i = 0; i < batchSize / 2; i++)
                    {
                        string key = $"batch_{batchId}_item_{i}";
                        string value = $"revalue_{batchId}_{i}_{Guid.NewGuid()}";
                        cache[key] = value;
                    }
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Verify final state - we should have data from all batches
        long finalCount = cache.Count;
        await Assert.That(finalCount).IsGreaterThan((long)(numberOfBatches * batchSize * 0.8)); // Allow for some removed items
    }

    [Test]
    public async Task ConnectionPool_ExceptionHandling()
    {
        const int iterations = 100;
        List<Exception> exceptions = [];

        string cacheName = CreateTestCacheName("exception_test");
        using DiskCache<long, string> cache = new(cacheName, degreeOfParallelism: 4);

        List<Task> tasks = [];

        // Create operations that might cause exceptions to test connection cleanup
        for (int i = 0; i < iterations; i++)
        {
            int taskId = i;
            Task task = Task.Run(async () =>
            {
                try
                {
                    // Normal operations
                    cache[taskId] = $"value_{taskId}";
                    string value = cache[taskId];

                    // Simulate potential error conditions
                    if (taskId % 20 == 0)
                    {
                        // Try to access a key that doesn't exist in a way that might cause issues
                        try
                        {
                            cache.Remove(taskId + 1000000); // Non-existent key
                        }
                        catch
                        {
                            // Ignore expected exceptions
                        }
                    }

                    // Verify the cache still works after potential exceptions
                    string retrieved = cache[taskId];
                    await Assert.That(retrieved).IsEqualTo($"value_{taskId}");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                    throw;
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Ensure we don't have unexpected exceptions
        await Assert.That(exceptions.Count).IsEqualTo(0);

        // Verify cache is still functional
        await Assert.That(cache.Count).IsEqualTo(iterations);
    }
}
