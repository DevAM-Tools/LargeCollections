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
        await Assert.That(stream.CanSeek).IsFalse();
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

        stream.Write(new byte[] { 1, 2, 3 }, 0, 3);

        await Assert.That(storage.Count).IsEqualTo(7L);
        await Assert.That(storage.GetAll().TakeLast(3).SequenceEqual(new byte[] { 1, 2, 3 })).IsTrue();
    }

    [Test]
    public async Task Position_ReportsLength_SetterThrows()
    {
        LargeWritableMemoryStream stream = new();
        stream.Write(new byte[] { 5, 6, 7 }, 0, 3);

        await Assert.That(stream.Position).IsEqualTo(stream.Length);
        await Assert.That(() => stream.Position = 0L).Throws<NotSupportedException>();
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
        await Assert.That(() => stream.Seek(0, SeekOrigin.Begin)).Throws<NotSupportedException>();
        await Assert.That(() => stream.SetLength(0)).Throws<NotSupportedException>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Write_LargeArray_AppendsData(long count)
    {
        LargeWritableMemoryStream stream = new();
        LargeArray<byte> source = CreateLargeArray(count, 0);

        if (count >= Constants.MaxLargeCollectionCount)
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
        LargeArray<byte> source = CreateLargeArray(2, 1);

        await Assert.That(() => stream.Write(source)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Write_Array_ExceedingCapacity_Throws()
    {
        LargeList<byte> storage = CreateSequentialList(Constants.MaxLargeCollectionCount - 1L, 0);
        LargeWritableMemoryStream stream = new(storage);
        byte[] buffer = new byte[] { 1, 2 };

        await Assert.That(() => stream.Write(buffer, 0, buffer.Length)).Throws<InvalidOperationException>();
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    public async Task Write_Span_ExceedingCapacity_Throws()
    {
        LargeList<byte> storage = CreateSequentialList(Constants.MaxLargeCollectionCount - 1L, 0);
        LargeWritableMemoryStream stream = new(storage);
        await Assert.That(() => stream.Write((ReadOnlySpan<byte>)new byte[] { 3, 4 })).Throws<InvalidOperationException>();
    }
#endif

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
