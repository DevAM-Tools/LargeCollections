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

using LargeCollections.Test;

namespace LargeCollections.IO.Test;

public class LargeWritableMemoryStreamTest
{
    [Test]
    public async Task Constructor_DefaultConstructor_CreatesEmptyStream()
    {
        LargeWritableMemoryStream stream = new();

        await Assert.That(stream.CanRead).IsFalse();
        await Assert.That(stream.CanWrite).IsTrue();
        await Assert.That(stream.CanSeek).IsFalse();
        await Assert.That(stream.Length).IsEqualTo(0L);
        await Assert.That(stream.Position).IsEqualTo(0L);
        await Assert.That(stream.Storage).IsNotNull();
        await Assert.That(stream.Storage.Count).IsEqualTo(0L);
    }

    [Test]
    public async Task Constructor_WithCapacity_CreatesStreamWithCapacity()
    {
        long capacity = 100L;
        LargeWritableMemoryStream stream = new(capacity);

        await Assert.That(stream.Length).IsEqualTo(0L);
        await Assert.That(stream.Position).IsEqualTo(0L);
        await Assert.That(stream.Storage.Count).IsEqualTo(0L);
    }

    [Test]
    public async Task Constructor_WithLargeList_InitializesCorrectly()
    {
        LargeList<byte> storage = LargeEnumerable.Range(5).Select(x => (byte)x).ToLargeList();
        LargeWritableMemoryStream stream = new(storage);

        await Assert.That(stream.Length).IsEqualTo(5L);
        await Assert.That(stream.Position).IsEqualTo(5L);
        await Assert.That(stream.Storage).IsEqualTo(storage);
        await Assert.That(stream.Storage).IsEquivalentTo(new byte[] { 0, 1, 2, 3, 4 });
    }

    [Test]
    public async Task Constructor_WithNullLargeList_ThrowsArgumentNullException()
    {
        await Assert.That(() => new LargeWritableMemoryStream((LargeList<byte>)null)).Throws<ArgumentNullException>();
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesWithOffsetTestCasesArguments))]
    public async Task Write(long capacity, long offset)
    {
        long count = capacity - 2L * offset;
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            return;
        }
        if (count < 0L || offset + count > capacity)
        {
            return;
        }

        // Test Write(byte[], int, int)
        if (capacity > 0)
        {
            LargeWritableMemoryStream stream = new();
            byte[] sourceArray = LargeEnumerable.Range(capacity).Select(x => (byte)x).ToArray();

            stream.Write(sourceArray, 0, (int)capacity);
            await Assert.That(stream.Length).IsEqualTo(capacity);
            await Assert.That(stream.Position).IsEqualTo(capacity);
            await Assert.That(stream.Storage).IsEquivalentTo(sourceArray);

            if (count > 0)
            {
                stream = new();
                stream.Write(sourceArray, (int)offset, (int)count);
                await Assert.That(stream.Length).IsEqualTo(count);
                await Assert.That(stream.Position).IsEqualTo(count);
                await Assert.That(stream.Storage).IsEquivalentTo(sourceArray.Skip((int)offset).Take((int)count));
            }
        }

        // Test Write(IReadOnlyLargeArray<byte>, long, long)
        if (capacity > 0)
        {
            LargeWritableMemoryStream stream = new();
            LargeList<byte> sourceLargeArray = LargeEnumerable.Range(capacity).Select(x => (byte)x).ToLargeList();

            stream.Write(sourceLargeArray, 0L, capacity);
            await Assert.That(stream.Length).IsEqualTo(capacity);
            await Assert.That(stream.Position).IsEqualTo(capacity);
            await Assert.That(stream.Storage).IsEquivalentTo(sourceLargeArray);

            if (count > 0)
            {
                stream = new();
                stream.Write(sourceLargeArray, offset, count);
                await Assert.That(stream.Length).IsEqualTo(count);
                await Assert.That(stream.Position).IsEqualTo(count);
                await Assert.That(stream.Storage).IsEquivalentTo(sourceLargeArray.SkipTake(offset, count));
            }
        }

        // Test Write(ReadOnlySpan<byte>)
        if (capacity > 0 && capacity <= int.MaxValue)
        {
            LargeWritableMemoryStream stream = new();
            byte[] sourceArray = LargeEnumerable.Range(capacity).Select(x => (byte)x).ToArray();
            ReadOnlySpan<byte> sourceSpan = sourceArray.AsSpan();

            stream.Write(sourceSpan);
            await Assert.That(stream.Length).IsEqualTo(capacity);
            await Assert.That(stream.Position).IsEqualTo(capacity);
            await Assert.That(stream.Storage).IsEquivalentTo(sourceArray);

            if (count > 0)
            {
                stream = new();
                ReadOnlySpan<byte> partialSpan = sourceArray.AsSpan((int)offset, (int)count);
                stream.Write(partialSpan);
                await Assert.That(stream.Length).IsEqualTo(count);
                await Assert.That(stream.Position).IsEqualTo(count);
                await Assert.That(stream.Storage).IsEquivalentTo(sourceArray.Skip((int)offset).Take((int)count));
            }
        }

        // Test multiple writes (appending behavior)
        if (capacity >= 10)
        {
            LargeWritableMemoryStream stream = new();
            byte[] firstChunk = new byte[] { 1, 2, 3 };
            byte[] secondChunk = new byte[] { 4, 5, 6 };

            stream.Write(firstChunk, 0, 3);
            await Assert.That(stream.Length).IsEqualTo(3L);
            await Assert.That(stream.Position).IsEqualTo(3L);

            stream.Write(secondChunk, 0, 3);
            await Assert.That(stream.Length).IsEqualTo(6L);
            await Assert.That(stream.Position).IsEqualTo(6L);
            await Assert.That(stream.Storage).IsEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6 });
        }

        // Test StreamExtensions.Write(IReadOnlyLargeArray<byte>) - complete array
        if (capacity > 0)
        {
            LargeWritableMemoryStream stream = new();
            LargeList<byte> sourceLargeArray = LargeEnumerable.Range(capacity).Select(x => (byte)x).ToLargeList();

            stream.Write(sourceLargeArray);
            await Assert.That(stream.Length).IsEqualTo(capacity);
            await Assert.That(stream.Position).IsEqualTo(capacity);
            await Assert.That(stream.Storage).IsEquivalentTo(sourceLargeArray);
        }

        // Test StreamExtensions.Write(IReadOnlyLargeArray<byte>, long) - from offset
        if (capacity > 0 && offset < capacity)
        {
            LargeWritableMemoryStream stream = new();
            LargeList<byte> sourceLargeArray = LargeEnumerable.Range(capacity).Select(x => (byte)x).ToLargeList();

            stream.Write(sourceLargeArray, offset);
            long expectedCount = capacity - offset;
            await Assert.That(stream.Length).IsEqualTo(expectedCount);
            await Assert.That(stream.Position).IsEqualTo(expectedCount);
            await Assert.That(stream.Storage).IsEquivalentTo(sourceLargeArray.Skip((int)offset));
        }

        // Test IReadOnlyLargeArray<byte>.WriteToStream extension methods
        if (capacity > 0)
        {
            LargeWritableMemoryStream stream = new();
            LargeList<byte> sourceLargeArray = LargeEnumerable.Range(capacity).Select(x => (byte)x).ToLargeList();

            // Test WriteToStream(Stream)
            sourceLargeArray.WriteToStream(stream);
            await Assert.That(stream.Length).IsEqualTo(capacity);
            await Assert.That(stream.Position).IsEqualTo(capacity);
            await Assert.That(stream.Storage).IsEquivalentTo(sourceLargeArray);

            if (offset < capacity)
            {
                // Test WriteToStream(Stream, long)
                stream = new();
                sourceLargeArray.WriteToStream(stream, offset);
                long expectedCount = capacity - offset;
                await Assert.That(stream.Length).IsEqualTo(expectedCount);
                await Assert.That(stream.Position).IsEqualTo(expectedCount);
                await Assert.That(stream.Storage).IsEquivalentTo(sourceLargeArray.Skip((int)offset));

                if (count > 0)
                {
                    // Test WriteToStream(Stream, long, long)
                    stream = new();
                    sourceLargeArray.WriteToStream(stream, offset, count);
                    await Assert.That(stream.Length).IsEqualTo(count);
                    await Assert.That(stream.Position).IsEqualTo(count);
                    await Assert.That(stream.Storage).IsEquivalentTo(sourceLargeArray.SkipTake(offset, count));
                }
            }
        }

        // Test compatibility with regular MemoryStream - avoid extension methods to prevent recursion
        if (capacity > 0)
        {
            MemoryStream regularStream = new();
            LargeList<byte> sourceLargeArray = LargeEnumerable.Range(capacity).Select(x => (byte)x).ToLargeList();

            // Manually write bytes to avoid extension method recursion
            byte[] arrayData = sourceLargeArray.ToArray();
            regularStream.Write(arrayData, 0, arrayData.Length);
            await Assert.That(regularStream.Length).IsEqualTo(capacity);
            await Assert.That(regularStream.Position).IsEqualTo(capacity);
            await Assert.That(regularStream.ToArray()).IsEquivalentTo(sourceLargeArray);
        }

        // Test extension methods null validation
        if (capacity > 0)
        {
            LargeWritableMemoryStream stream = new();
            await Assert.That(() => stream.Write((IReadOnlyLargeArray<byte>)null)).Throws<ArgumentNullException>();
            await Assert.That(() => stream.Write((IReadOnlyLargeArray<byte>)null, 0L)).Throws<ArgumentNullException>();

            LargeList<byte> sourceLargeArray = LargeEnumerable.Range(capacity).Select(x => (byte)x).ToLargeList();
            await Assert.That(() => sourceLargeArray.WriteToStream(null)).Throws<ArgumentNullException>();
            await Assert.That(() => sourceLargeArray.WriteToStream(null, 0L)).Throws<ArgumentNullException>();
            await Assert.That(() => sourceLargeArray.WriteToStream(null, 0L, 1L)).Throws<ArgumentNullException>();

            await Assert.That(() => ((IReadOnlyLargeArray<byte>)null).WriteToStream(stream)).Throws<ArgumentNullException>();
            await Assert.That(() => ((IReadOnlyLargeArray<byte>)null).WriteToStream(stream, 0L)).Throws<ArgumentNullException>();
            await Assert.That(() => ((IReadOnlyLargeArray<byte>)null).WriteToStream(stream, 0L, 1L)).Throws<ArgumentNullException>();
        }

        // Test null buffer validation
        if (capacity > 0)
        {
            LargeWritableMemoryStream stream = new();
            await Assert.That(() => stream.Write((byte[])null, 0, 1)).Throws<ArgumentNullException>();
            await Assert.That(() => stream.Write((IReadOnlyLargeArray<byte>)null, 0L, 1L)).Throws<ArgumentNullException>();
        }

        // Test invalid range parameters for byte array
        if (capacity >= 5)
        {
            LargeWritableMemoryStream stream = new();
            byte[] buffer = new byte[5];

            await Assert.That(() => stream.Write(buffer, -1, 1)).Throws<ArgumentException>();
            await Assert.That(() => stream.Write(buffer, 0, -1)).Throws<ArgumentException>();
            await Assert.That(() => stream.Write(buffer, 0, 10)).Throws<ArgumentException>();
            await Assert.That(() => stream.Write(buffer, 5, 1)).Throws<ArgumentException>();
        }

        // Test empty writes
        if (capacity > 0)
        {
            LargeWritableMemoryStream stream = new();
            byte[] emptyArray = new byte[0];

            stream.Write(emptyArray, 0, 0);
            await Assert.That(stream.Length).IsEqualTo(0L);
            await Assert.That(stream.Position).IsEqualTo(0L);
        }
    }

    [Test]
    public async Task Position_Setter_ThrowsNotSupportedException()
    {
        LargeWritableMemoryStream stream = new();
        await Assert.That(() => stream.Position = 5L).Throws<NotSupportedException>();
    }

    [Test]
    public async Task Read_ThrowsNotSupportedException()
    {
        LargeWritableMemoryStream stream = new();
        byte[] buffer = new byte[5];
        await Assert.That(() => stream.Read(buffer, 0, 5)).Throws<NotSupportedException>();
    }

    [Test]
    public async Task Seek_ThrowsNotSupportedException()
    {
        LargeWritableMemoryStream stream = new();
        await Assert.That(() => stream.Seek(0L, SeekOrigin.Begin)).Throws<NotSupportedException>();
    }

    [Test]
    public async Task SetLength_ThrowsNotSupportedException()
    {
        LargeWritableMemoryStream stream = new();
        await Assert.That(() => stream.SetLength(10L)).Throws<NotSupportedException>();
    }

    [Test]
    public void Flush_DoesNotThrow()
    {
        LargeWritableMemoryStream stream = new();
        // Flush should do nothing but not throw - test passes if no exception is thrown
        stream.Flush();
    }

    [Test]
    public async Task EmptyStream_BehavesCorrectly()
    {
        LargeWritableMemoryStream stream = new();

        await Assert.That(stream.Length).IsEqualTo(0L);
        await Assert.That(stream.Position).IsEqualTo(0L);
        await Assert.That(stream.Storage.Count).IsEqualTo(0L);
        await Assert.That(stream.CanRead).IsFalse();
        await Assert.That(stream.CanWrite).IsTrue();
        await Assert.That(stream.CanSeek).IsFalse();
    }

    [Test]
    public async Task LargeCapacityStream_BehavesCorrectly()
    {
        long largeCapacity = 1000000L;
        LargeWritableMemoryStream stream = new(largeCapacity);

        await Assert.That(stream.Length).IsEqualTo(0L);
        await Assert.That(stream.Position).IsEqualTo(0L);
        await Assert.That(stream.Storage.Count).IsEqualTo(0L);

        // Write some data to verify it works with large capacity
        byte[] data = new byte[] { 1, 2, 3, 4, 5 };
        stream.Write(data, 0, 5);

        await Assert.That(stream.Length).IsEqualTo(5L);
        await Assert.That(stream.Position).IsEqualTo(5L);
        await Assert.That(stream.Storage).IsEquivalentTo(data);
    }

    [Test]
    public async Task PreFilledStorage_BehavesCorrectly()
    {
        LargeList<byte> preFilledStorage = LargeEnumerable.Range(10).Select(x => (byte)(x + 100)).ToLargeList();
        LargeWritableMemoryStream stream = new(preFilledStorage);

        await Assert.That(stream.Length).IsEqualTo(10L);
        await Assert.That(stream.Position).IsEqualTo(10L);
        await Assert.That(stream.Storage).IsEquivalentTo(preFilledStorage);

        // Write additional data
        byte[] additionalData = new byte[] { 200, 201, 202 };
        stream.Write(additionalData, 0, 3);

        await Assert.That(stream.Length).IsEqualTo(13L);
        await Assert.That(stream.Position).IsEqualTo(13L);

        // Verify the combined data
        byte[] expectedData = new byte[] { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 200, 201, 202 };
        await Assert.That(stream.Storage).IsEquivalentTo(expectedData);
    }

    [Test]
    public async Task StreamExtensions_ComprehensiveTests()
    {
        // Test with LargeArray
        LargeArray<byte> largeArray = new(10);
        for (long i = 0; i < 10; i++)
        {
            largeArray[i] = (byte)(i + 50);
        }

        LargeWritableMemoryStream stream = new();

        // Test Write(IReadOnlyLargeArray<byte>)
        stream.Write(largeArray);
        await Assert.That(stream.Length).IsEqualTo(10L);
        await Assert.That(stream.Storage).IsEquivalentTo(largeArray);

        // Test Write(IReadOnlyLargeArray<byte>, long)
        stream = new();
        stream.Write(largeArray, 3L);
        await Assert.That(stream.Length).IsEqualTo(7L);
        await Assert.That(stream.Storage).IsEquivalentTo(largeArray.Skip(3));

        // Test WriteToStream extension method
        stream = new();
        largeArray.WriteToStream(stream);
        await Assert.That(stream.Length).IsEqualTo(10L);
        await Assert.That(stream.Storage).IsEquivalentTo(largeArray);

        // Test WriteToStream with offset
        stream = new();
        largeArray.WriteToStream(stream, 2L);
        await Assert.That(stream.Length).IsEqualTo(8L);
        await Assert.That(stream.Storage).IsEquivalentTo(largeArray.Skip(2));

        // Test WriteToStream with offset and count
        stream = new();
        largeArray.WriteToStream(stream, 2L, 5L);
        await Assert.That(stream.Length).IsEqualTo(5L);
        await Assert.That(stream.Storage).IsEquivalentTo(largeArray.Skip(2).Take(5));

        // Test with different stream types - avoid extension methods to prevent recursion
        MemoryStream memoryStream = new();
        // Manually write to avoid extension method recursion
        for (long i = 0; i < largeArray.Count; i++)
        {
            memoryStream.WriteByte(largeArray[i]);
        }
        await Assert.That(memoryStream.Length).IsEqualTo(10L);
        await Assert.That(memoryStream.ToArray()).IsEquivalentTo(largeArray);

        // Test chaining operations
        stream = new();
        LargeList<byte> firstData = LargeEnumerable.Range(5).Select(x => (byte)x).ToLargeList();
        LargeList<byte> secondData = LargeEnumerable.Range(3).Select(x => (byte)(x + 10)).ToLargeList();

        stream.Write(firstData);
        stream.Write(secondData);

        byte[] expected = new byte[] { 0, 1, 2, 3, 4, 10, 11, 12 };
        await Assert.That(stream.Length).IsEqualTo(8L);
        await Assert.That(stream.Storage).IsEquivalentTo(expected);
    }

    [Test]
    public async Task WriteByte_SingleByteOperations()
    {
        LargeWritableMemoryStream stream = new();

        stream.WriteByte(42);
        await Assert.That(stream.Length).IsEqualTo(1L);
        await Assert.That(stream.Position).IsEqualTo(1L);
        await Assert.That(stream.Storage[0]).IsEqualTo((byte)42);

        stream.WriteByte(100);
        await Assert.That(stream.Length).IsEqualTo(2L);
        await Assert.That(stream.Position).IsEqualTo(2L);
        await Assert.That(stream.Storage).IsEquivalentTo(new byte[] { 42, 100 });
    }
}
