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
using System.Threading.Tasks;
using LargeCollections;
using LargeCollections.IO;
using LargeCollections.Test.Helpers;
using TUnit.Core;

namespace LargeCollections.Test.IO;

public class LargeWritableMemoryStreamTest
{
    public static IEnumerable<long> Capacities() => Parameters.Capacities;

    [Test]
    public async Task Constructor_Default_InitializesEmptyStorage()
    {
        LargeWritableMemoryStream stream = new();

        await Assert.That(stream.Storage).IsNotNull();
        await Assert.That(stream.Length).IsEqualTo(0L);
        await Assert.That(stream.CanWrite).IsTrue();
        await Assert.That(stream.CanRead).IsFalse();
        await Assert.That(stream.CanSeek).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Constructor_WithCapacity_PreparesStorage(long capacity)
    {
        LargeWritableMemoryStream stream = new(capacity);

        await Assert.That(stream.Storage).IsNotNull();
        await Assert.That(stream.Storage.Count).IsEqualTo(0L);
        await Assert.That(stream.Storage.Capacity).IsEqualTo(capacity);
        await Assert.That(stream.Length).IsEqualTo(0L);
    }

    [Test]
    public async Task Constructor_WithStorage_ThrowsOnNull()
    {
        await Assert.That(() => new LargeWritableMemoryStream((LargeList<byte>)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_WithStorage_UsesProvidedInstance()
    {
        LargeList<byte> storage = CreateSequentialList(4, 10);
        LargeWritableMemoryStream stream = new(storage);

        await Assert.That(stream.Length).IsEqualTo(storage.Count);
        await Assert.That(ReferenceEquals(stream.Storage, storage)).IsTrue();

        // Position starts at 0, so Write overwrites the first 3 bytes
        stream.Write(new byte[] { 1, 2, 3 }, 0, 3);

        await Assert.That(storage.Count).IsEqualTo(4L);  // Length unchanged (overwrite)
        await Assert.That(storage[0]).IsEqualTo((byte)1);
        await Assert.That(storage[1]).IsEqualTo((byte)2);
        await Assert.That(storage[2]).IsEqualTo((byte)3);
        await Assert.That(storage[3]).IsEqualTo((byte)13);  // Original byte untouched
    }

    [Test]
    public async Task Position_CanBeSetWithinBounds()
    {
        LargeWritableMemoryStream stream = new();
        stream.Write(new byte[] { 5, 6, 7 }, 0, 3);

        await Assert.That(stream.Position).IsEqualTo(3L);

        stream.Position = 1L;
        await Assert.That(stream.Position).IsEqualTo(1L);

        stream.Position = 0L;
        await Assert.That(stream.Position).IsEqualTo(0L);

        stream.Position = stream.Length;
        await Assert.That(stream.Position).IsEqualTo(stream.Length);

        // Out of bounds throws
        await Assert.That(() => stream.Position = -1L).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => stream.Position = stream.Length + 1L).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Flush_DoesNotAlterLength()
    {
        LargeWritableMemoryStream stream = new();
        stream.Write(new byte[] { 1, 2 }, 0, 2);
        long before = stream.Length;

        stream.Flush();

        await Assert.That(stream.Length).IsEqualTo(before);
    }

    [Test]
    public async Task UnsupportedOperations_Throw()
    {
        LargeWritableMemoryStream stream = new();
        await Assert.That(() => stream.Read(Array.Empty<byte>(), 0, 0)).Throws<NotSupportedException>();
        await Assert.That(() => stream.SetLength(0)).Throws<NotSupportedException>();
    }

    [Test]
    public async Task Seek_WorksCorrectly()
    {
        LargeWritableMemoryStream stream = new();
        stream.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

        // Seek from Begin
        long pos = stream.Seek(2, SeekOrigin.Begin);
        await Assert.That(pos).IsEqualTo(2L);
        await Assert.That(stream.Position).IsEqualTo(2L);

        // Seek from Current
        pos = stream.Seek(1, SeekOrigin.Current);
        await Assert.That(pos).IsEqualTo(3L);
        await Assert.That(stream.Position).IsEqualTo(3L);

        // Seek from End
        pos = stream.Seek(-2, SeekOrigin.End);
        await Assert.That(pos).IsEqualTo(3L);
        await Assert.That(stream.Position).IsEqualTo(3L);

        // Seek to beginning
        pos = stream.Seek(0, SeekOrigin.Begin);
        await Assert.That(pos).IsEqualTo(0L);

        // Seek to end
        pos = stream.Seek(0, SeekOrigin.End);
        await Assert.That(pos).IsEqualTo(5L);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Write_LargeArray_AppendsData(long count)
    {
        LargeWritableMemoryStream stream = new();
        LargeArray<byte> source = CreateLargeArray(count, 0);

        // With corrected capacity check (>), we can store exactly MaxLargeCollectionCount elements
        if (count > Constants.MaxLargeCollectionCount)
        {
            await Assert.That(() => stream.Write(source)).Throws<InvalidOperationException>();
            return;
        }

        stream.Write(source);

        await Assert.That(stream.Length).IsEqualTo(count);
        await Assert.That(StorageToArray(stream.Storage).SequenceEqual(ToByteSequence(count, 0))).IsTrue();
    }

    [Test]
    public async Task Write_LargeArray_Null_Throws()
    {
        LargeWritableMemoryStream stream = new();
        await Assert.That(() => stream.Write((IReadOnlyLargeArray<byte>)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Write_LargeArray_WithOffset_RespectsOffset()
    {
        LargeWritableMemoryStream stream = new();
        LargeArray<byte> source = CreateLargeArray(6, 5);

        stream.Write(source, 2L);

        await Assert.That(StorageToArray(stream.Storage).SequenceEqual(ToByteSequence(4, 7))).IsTrue();
    }

    [Test]
    public async Task Write_LargeArray_WithOffset_InvalidArguments_Throw()
    {
        LargeWritableMemoryStream stream = new();
        LargeArray<byte> source = CreateLargeArray(3, 0);

        await Assert.That(() => stream.Write((IReadOnlyLargeArray<byte>)null!, 0L)).Throws<ArgumentNullException>();
        await Assert.That(() => stream.Write(source, -1L)).Throws<ArgumentException>();
        // Note: offset = source.Count + 1 causes negative actualCount which Storage.AddRange validates
        await Assert.That(() => stream.Write(source, source.Count + 1L)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Write_LargeArray_WithOffsetCount_RespectsRange()
    {
        LargeWritableMemoryStream stream = new();
        LargeArray<byte> source = CreateLargeArray(8, 20);

        stream.Write(source, 2L, 3L);

        await Assert.That(StorageToArray(stream.Storage).SequenceEqual(ToByteSequence(3, 22))).IsTrue();

        long before = stream.Length;
        stream.Write(source, 0L, 0L);
        await Assert.That(stream.Length).IsEqualTo(before);
    }

    [Test]
    public async Task Write_LargeArray_WithOffsetCount_InvalidArguments_Throw()
    {
        LargeWritableMemoryStream stream = new();
        LargeArray<byte> source = CreateLargeArray(5, 0);

        await Assert.That(() => stream.Write((IReadOnlyLargeArray<byte>)null!, 0L, 1L)).Throws<ArgumentNullException>();
        await Assert.That(() => stream.Write(source, -1L, 1L)).Throws<ArgumentException>();
        await Assert.That(() => stream.Write(source, 0L, -1L)).Throws<ArgumentException>();
        await Assert.That(() => stream.Write(source, 2L, 10L)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Write_ReadOnlyLargeSpan_AppendsData()
    {
        LargeWritableMemoryStream stream = new();
        LargeArray<byte> source = CreateLargeArray(5, 30);
        ReadOnlyLargeSpan<byte> span = new(source);

        stream.Write(span);

        await Assert.That(StorageToArray(stream.Storage).SequenceEqual(ToByteSequence(5, 30))).IsTrue();
    }

    [Test]
    public async Task Write_ReadOnlyLargeSpan_Empty_NoChange()
    {
        LargeWritableMemoryStream stream = new();
        long before = stream.Length;

        stream.Write(default(ReadOnlyLargeSpan<byte>));

        await Assert.That(stream.Length).IsEqualTo(before);
    }

    [Test]
    public async Task Write_Array_AppendsData()
    {
        LargeWritableMemoryStream stream = new();
        byte[] buffer = new byte[] { 100, 101, 102, 103 };

        stream.Write(buffer, 1, 2);

        await Assert.That(StorageToArray(stream.Storage).SequenceEqual(new byte[] { 101, 102 })).IsTrue();
    }

    [Test]
    public async Task Write_Array_InvalidArguments_Throw()
    {
        LargeWritableMemoryStream stream = new();
        byte[] data = new byte[] { 1, 2, 3 };

        await Assert.That(() => stream.Write((byte[])null!, 0, 1)).Throws<ArgumentNullException>();
        await Assert.That(() => stream.Write(data, -1, 1)).Throws<ArgumentException>();
        await Assert.That(() => stream.Write(data, 0, -1)).Throws<ArgumentException>();
        await Assert.That(() => stream.Write(data, 2, 5)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Write_Array_ZeroCount_NoChange()
    {
        LargeWritableMemoryStream stream = new();
        long before = stream.Length;

        stream.Write(new byte[] { 9, 8, 7 }, 0, 0);

        await Assert.That(stream.Length).IsEqualTo(before);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    public async Task Write_Span_AppendsData()
    {
        LargeWritableMemoryStream stream = new();
        ReadOnlySpan<byte> span = new byte[] { 11, 12, 13 };

        stream.Write(span);

        await Assert.That(StorageToArray(stream.Storage).SequenceEqual(new byte[] { 11, 12, 13 })).IsTrue();
    }

    [Test]
    public async Task Write_Span_Empty_NoChange()
    {
        LargeWritableMemoryStream stream = new();
        long before = stream.Length;

        stream.Write(ReadOnlySpan<byte>.Empty);

        await Assert.That(stream.Length).IsEqualTo(before);
    }
#endif

    [Test]
    public async Task Write_ExceedingCapacity_Throws()
    {
        LargeList<byte> storage = CreateSequentialList(Constants.MaxLargeCollectionCount - 1L, 0);
        LargeWritableMemoryStream stream = new(storage);
        stream.Seek(0, SeekOrigin.End);  // Seek to end to append
        LargeArray<byte> source = CreateLargeArray(2, 1);

        await Assert.That(() => stream.Write(source)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Write_Array_ExceedingCapacity_Throws()
    {
        LargeList<byte> storage = CreateSequentialList(Constants.MaxLargeCollectionCount - 1L, 0);
        LargeWritableMemoryStream stream = new(storage);
        stream.Seek(0, SeekOrigin.End);  // Seek to end to append
        byte[] buffer = new byte[] { 1, 2 };

        await Assert.That(() => stream.Write(buffer, 0, buffer.Length)).Throws<InvalidOperationException>();
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    public async Task Write_Span_ExceedingCapacity_Throws()
    {
        LargeList<byte> storage = CreateSequentialList(Constants.MaxLargeCollectionCount - 1L, 0);
        LargeWritableMemoryStream stream = new(storage);
        stream.Seek(0, SeekOrigin.End);  // Seek to end to append
        await Assert.That(() => stream.Write((ReadOnlySpan<byte>)new byte[] { 3, 4 })).Throws<InvalidOperationException>();
    }
#endif

    [Test]
    public async Task Write_OverwriteAndAppend_WorksCorrectly()
    {
        LargeWritableMemoryStream stream = new();
        stream.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

        // Seek to middle
        stream.Seek(2, SeekOrigin.Begin);

        // Write 4 bytes: overwrites 3, 4, 5 and appends one
        stream.Write(new byte[] { 10, 11, 12, 13 }, 0, 4);

        await Assert.That(stream.Length).IsEqualTo(6L);
        await Assert.That(stream.Position).IsEqualTo(6L);
        await Assert.That(StorageToArray(stream.Storage).SequenceEqual(new byte[] { 1, 2, 10, 11, 12, 13 })).IsTrue();
    }

    [Test]
    public async Task Write_OverwriteOnly_LengthUnchanged()
    {
        LargeWritableMemoryStream stream = new();
        stream.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

        // Seek to start
        stream.Seek(0, SeekOrigin.Begin);

        // Write 3 bytes: only overwrites, no append
        stream.Write(new byte[] { 10, 11, 12 }, 0, 3);

        await Assert.That(stream.Length).IsEqualTo(5L);
        await Assert.That(stream.Position).IsEqualTo(3L);
        await Assert.That(StorageToArray(stream.Storage).SequenceEqual(new byte[] { 10, 11, 12, 4, 5 })).IsTrue();
    }

    [Test]
    public async Task Write_AtEnd_AppendsOnly()
    {
        LargeWritableMemoryStream stream = new();
        stream.Write(new byte[] { 1, 2, 3 }, 0, 3);

        // Position is already at end
        await Assert.That(stream.Position).IsEqualTo(stream.Length);

        // Write more: appends only
        stream.Write(new byte[] { 4, 5 }, 0, 2);

        await Assert.That(stream.Length).IsEqualTo(5L);
        await Assert.That(StorageToArray(stream.Storage).SequenceEqual(new byte[] { 1, 2, 3, 4, 5 })).IsTrue();
    }

    #region Helpers

    private static LargeArray<byte> CreateLargeArray(long count, byte start)
    {
        LargeArray<byte> array = new(count);
        for (long i = 0; i < count; i++)
        {
            array[i] = ToByte(i, start);
        }
        return array;
    }

    private static LargeList<byte> CreateSequentialList(long count, byte start)
    {
        LargeList<byte> list = new(count);
        for (long i = 0; i < count; i++)
        {
            list.Add(ToByte(i, start));
        }
        return list;
    }

    private static IEnumerable<byte> ToByteSequence(long count, byte start)
    {
        for (long i = 0; i < count; i++)
        {
            yield return ToByte(i, start);
        }
    }

    private static byte[] StorageToArray(LargeList<byte> storage)
    {
        return storage.GetAll().ToArray();
    }

    private static byte ToByte(long index, byte start)
    {
        return (byte)((start + index) % 256);
    }

    #endregion
}
