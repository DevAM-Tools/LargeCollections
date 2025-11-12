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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LargeCollections.IO;
using LargeCollections.Test.Helpers;
using TUnit.Core;

namespace LargeCollections.Test.IO;

public class StreamExtensionsTest
{
    public static IEnumerable<long> Capacities() => Parameters.Capacities;

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Write_IReadOnlyLargeArray_WritesExpectedBytes(long count)
    {
        LargeArray<byte> source = CreateSequentialArray(count, 0x10);
        using MemoryStream stream = new();

        stream.Write((IReadOnlyLargeArray<byte>)source);

        await Assert.That(stream.ToArray().SequenceEqual(ToByteSequence(count, 0x10))).IsTrue();
    }

    [Test]
    public async Task Write_IReadOnlyLargeArray_NullSource_Throws()
    {
        using MemoryStream stream = new();

        await Assert.That(() => StreamExtensions.Write(stream, (IReadOnlyLargeArray<byte>)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Write_IReadOnlyLargeArray_NullStream_Throws()
    {
        LargeArray<byte> source = CreateSequentialArray(1, 0x00);

        await Assert.That(() => StreamExtensions.Write((Stream)null!, (IReadOnlyLargeArray<byte>)source)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Write_IReadOnlyLargeArray_WithOffset_WritesFromOffset()
    {
        LargeArray<byte> source = CreateSequentialArray(6, 0x20);
        using MemoryStream stream = new();

        stream.Write((IReadOnlyLargeArray<byte>)source, 2L);

        await Assert.That(stream.ToArray().SequenceEqual(ToByteSequence(4, 0x22))).IsTrue();
    }

    [Test]
    public async Task Write_IReadOnlyLargeArray_WithOffsetEqualCount_NoWrite()
    {
        LargeArray<byte> source = CreateSequentialArray(3, 0x30);
        using MemoryStream stream = new();

        stream.Write((IReadOnlyLargeArray<byte>)source, source.Count);

        await Assert.That(stream.Length).IsEqualTo(0L);
    }

    [Test]
    public async Task Write_IReadOnlyLargeArray_WithOffset_InvalidArguments_Throw()
    {
        LargeArray<byte> source = CreateSequentialArray(4, 0x40);
        using MemoryStream stream = new();

        await Assert.That(() => stream.Write((IReadOnlyLargeArray<byte>)source, -1L)).Throws<ArgumentException>();
        await Assert.That(() => stream.Write((IReadOnlyLargeArray<byte>)source, source.Count + 1L)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Write_IReadOnlyLargeArray_WithOffsetCount_WritesSubset()
    {
        LargeArray<byte> source = CreateSequentialArray(8, 0x50);
        using MemoryStream stream = new();

        stream.Write((IReadOnlyLargeArray<byte>)source, 2L, 3L);

        await Assert.That(stream.ToArray().SequenceEqual(ToByteSequence(3, 0x52))).IsTrue();
    }

    [Test]
    public async Task Write_IReadOnlyLargeArray_WithOffsetCount_ZeroCount_NoWrite()
    {
        LargeArray<byte> source = CreateSequentialArray(5, 0x60);
        using MemoryStream stream = new();

        stream.Write((IReadOnlyLargeArray<byte>)source, 1L, 0L);

        await Assert.That(stream.Length).IsEqualTo(0L);
    }

    [Test]
    public async Task Write_IReadOnlyLargeArray_WithOffsetCount_InvalidArguments_Throw()
    {
        LargeArray<byte> source = CreateSequentialArray(5, 0x70);
        using MemoryStream stream = new();

        await Assert.That(() => stream.Write((IReadOnlyLargeArray<byte>)source, -1L, 1L)).Throws<ArgumentException>();
        await Assert.That(() => stream.Write((IReadOnlyLargeArray<byte>)source, 0L, -1L)).Throws<ArgumentException>();
        await Assert.That(() => stream.Write((IReadOnlyLargeArray<byte>)source, 2L, source.Count)).Throws<ArgumentException>();
        await Assert.That(() => StreamExtensions.Write((Stream)null!, (IReadOnlyLargeArray<byte>)source, 0L, 1L)).Throws<ArgumentNullException>();
        await Assert.That(() => stream.Write((IReadOnlyLargeArray<byte>)null!, 0L, 1L)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Write_LargeArray_ToLargeWritableMemoryStream_AppendsBytes()
    {
        LargeArray<byte> source = CreateSequentialArray(5, 0x80);
        LargeWritableMemoryStream stream = new();

        stream.Write(source);

        await Assert.That(stream.Storage.Count).IsEqualTo(5L);
        await Assert.That(StreamStorageToArray(stream.Storage).SequenceEqual(ToByteSequence(5, 0x80))).IsTrue();
    }

    [Test]
    public async Task Write_LargeArray_WithOffsetCount_ToLargeWritableMemoryStream_WritesSubset()
    {
        LargeArray<byte> source = CreateSequentialArray(6, 0x90);
        LargeWritableMemoryStream stream = new();

        stream.Write(source, 1L, 4L);

        await Assert.That(stream.Storage.Count).IsEqualTo(4L);
        await Assert.That(StreamStorageToArray(stream.Storage).SequenceEqual(ToByteSequence(4, 0x91))).IsTrue();
    }

    [Test]
    public async Task Write_LargeList_ToStream_WritesExpectedBytes()
    {
        LargeList<byte> source = CreateSequentialList(7, 0xA0);
        using MemoryStream stream = new();

        stream.Write(source);

        await Assert.That(stream.ToArray().SequenceEqual(ToByteSequence(7, 0xA0))).IsTrue();
    }

    [Test]
    public async Task Write_LargeList_ToLargeWritableMemoryStream_AppendsBytes()
    {
        LargeList<byte> source = CreateSequentialList(4, 0xB0);
        LargeWritableMemoryStream stream = new();

        stream.Write(source);

        await Assert.That(stream.Storage.Count).IsEqualTo(4L);
        await Assert.That(StreamStorageToArray(stream.Storage).SequenceEqual(ToByteSequence(4, 0xB0))).IsTrue();
    }

    [Test]
    public async Task Write_LargeList_WithOffsetCount_InvalidArguments_Throw()
    {
        LargeList<byte> source = CreateSequentialList(3, 0xC0);
        using MemoryStream stream = new();

        await Assert.That(() => stream.Write(source, -1L)).Throws<ArgumentException>();
        await Assert.That(() => stream.Write(source, source.Count + 1L)).Throws<ArgumentException>();
        await Assert.That(() => stream.Write(source, 0L, -1L)).Throws<ArgumentException>();
        await Assert.That(() => stream.Write(source, 2L, source.Count)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Write_ReadOnlyLargeSpan_ToStream_WritesExpectedBytes()
    {
        LargeArray<byte> array = CreateSequentialArray(6, 0xD0);
        ReadOnlyLargeSpan<byte> span = new(array, 1L, 4L);
        using MemoryStream stream = new();

        stream.Write(span);

        await Assert.That(stream.ToArray().SequenceEqual(ToByteSequence(4, 0xD1))).IsTrue();
    }

    [Test]
    public async Task Write_ReadOnlyLargeSpan_ToLargeWritableMemoryStream_WritesExpectedBytes()
    {
        LargeList<byte> list = CreateSequentialList(8, 0xE0);
        ReadOnlyLargeSpan<byte> span = new(list, 2L, 3L);
        LargeWritableMemoryStream stream = new();

        stream.Write(span);

        await Assert.That(stream.Storage.Count).IsEqualTo(3L);
        await Assert.That(StreamStorageToArray(stream.Storage).SequenceEqual(ToByteSequence(3, 0xE2))).IsTrue();
    }

    [Test]
    public async Task Write_ReadOnlyLargeSpan_WithWrappedArray_FallsBackToGenericPath()
    {
        LargeArray<byte> inner = CreateSequentialArray(5, 0xF0);
        ReadOnlyLargeArrayWrapper wrapper = new(inner);
        ReadOnlyLargeSpan<byte> span = new(wrapper, 1L, 3L);
        using MemoryStream stream = new();

        stream.Write(span);

        await Assert.That(stream.ToArray().SequenceEqual(ToByteSequence(3, 0xF1))).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Read_ILargeArray_ReadsIntoLargeArray(long count)
    {
        using MemoryStream stream = CreateStreamWithSequence(count, 0x10);
        LargeArray<byte> target = new(count);

        long read = stream.Read((ILargeArray<byte>)target);

        await Assert.That(read).IsEqualTo(count);
        await Assert.That(target.GetAll().SequenceEqual(ToByteSequence(count, 0x10))).IsTrue();
        await Assert.That(stream.Position).IsEqualTo(count);
    }

    [Test]
    public async Task Read_ILargeArray_WithOffset_FillsFromOffset()
    {
        using MemoryStream stream = CreateStreamWithSequence(4, 0x20);
        LargeArray<byte> target = new(6);
        Fill(target, 0xFF);

        long read = stream.Read((ILargeArray<byte>)target, 2L, 3L);

        await Assert.That(read).IsEqualTo(3L);
        await Assert.That(target[1]).IsEqualTo((byte)0xFF);
        await Assert.That(target.GetAll(2L, read).SequenceEqual(ToByteSequence(3, 0x20))).IsTrue();
        await Assert.That(target[5]).IsEqualTo((byte)0xFF);
        await Assert.That(stream.Position).IsEqualTo(3L);
    }

    [Test]
    public async Task Read_ILargeArray_WithOffset_UsesRemainingCount()
    {
        using MemoryStream stream = CreateStreamWithSequence(3, 0x30);
        LargeArray<byte> target = new(5);
        Fill(target, 0xEE);

        long read = stream.Read((ILargeArray<byte>)target, 2L);

        await Assert.That(read).IsEqualTo(3L);
        await Assert.That(target.GetAll(2L, read).SequenceEqual(ToByteSequence(3, 0x30))).IsTrue();
        await Assert.That(target[0]).IsEqualTo((byte)0xEE);
        await Assert.That(target[4]).IsEqualTo((byte)(0x30 + 2));
    }

    [Test]
    public async Task Read_ILargeArray_CountZero_ReturnsZero()
    {
        using MemoryStream stream = CreateStreamWithSequence(2, 0x40);
        LargeArray<byte> target = new(3);
        Fill(target, 0x11);

        long read = stream.Read((ILargeArray<byte>)target, 1L, 0L);

        await Assert.That(read).IsEqualTo(0L);
        await Assert.That(stream.Position).IsEqualTo(0L);
        await Assert.That(target[1]).IsEqualTo((byte)0x11);
    }

    [Test]
    public async Task Read_ILargeArray_EndOfStream_ReturnsPartialAndThenZero()
    {
        using MemoryStream stream = CreateStreamWithSequence(2, 0x50);
        LargeArray<byte> target = new(4);

        long first = stream.Read((ILargeArray<byte>)target, 0L, 4L);
        long second = stream.Read((ILargeArray<byte>)target, 0L, 4L);

        await Assert.That(first).IsEqualTo(2L);
        await Assert.That(second).IsEqualTo(0L);
    }

    [Test]
    public async Task Read_ILargeArray_InvalidArguments_Throw()
    {
        using MemoryStream stream = CreateStreamWithSequence(1, 0x60);
        LargeArray<byte> target = new(2);

        await Assert.That(() => stream.Read((ILargeArray<byte>)null!, 0L, 1L)).Throws<ArgumentNullException>();
        await Assert.That(() => stream.Read(target, -1L, 1L)).Throws<ArgumentException>();
        await Assert.That(() => stream.Read(target, 0L, -1L)).Throws<ArgumentException>();
        await Assert.That(() => stream.Read(target, 1L, target.Count)).Throws<ArgumentException>();
        await Assert.That(() => stream.Read(target, target.Count)).Throws<IndexOutOfRangeException>();
        await Assert.That(() => StreamExtensions.Read((Stream)null!, target, 0L, 1L)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Read_ILargeArray_FallbackIntoLargeSpan_WritesData()
    {
        using MemoryStream stream = CreateStreamWithSequence(4, 0x70);
        LargeArray<byte> backing = new(5);
        Fill(backing, 0xAA);
        LargeSpan<byte> span = new(backing);
        ILargeArray<byte> target = span;

        long read = stream.Read(target, 1L, 3L);

        await Assert.That(read).IsEqualTo(3L);
        await Assert.That(backing[0]).IsEqualTo((byte)0xAA);
        await Assert.That(backing.GetAll(1L, 3L).SequenceEqual(ToByteSequence(3, 0x70))).IsTrue();
    }

    [Test]
    public async Task Read_LargeSpan_ReadsExpectedBytes()
    {
        using MemoryStream stream = CreateStreamWithSequence(3, 0x80);
        LargeArray<byte> backing = new(3);
        Fill(backing, 0x00);
        LargeSpan<byte> target = new(backing);

        long read = stream.Read(target);

        await Assert.That(read).IsEqualTo(3L);
        await Assert.That(backing.GetAll().SequenceEqual(ToByteSequence(3, 0x80))).IsTrue();
    }

    [Test]
    public async Task Read_LargeSpan_WithLargeListBacking_ReadsExpectedBytes()
    {
        using MemoryStream stream = CreateStreamWithSequence(4, 0x90);
        LargeList<byte> list = CreateFilledList(5, 0xEE);
        LargeSpan<byte> target = new(list);

        long read = stream.Read(target);

        await Assert.That(read).IsEqualTo(4L);
        await Assert.That(list.GetAll().Take((int)read).SequenceEqual(ToByteSequence(4, 0x90))).IsTrue();
    }

    [Test]
    public async Task Read_LargeSpan_CountZero_ReturnsZero()
    {
        using MemoryStream stream = CreateStreamWithSequence(2, 0xA0);
        LargeArray<byte> backing = new(0);
        LargeSpan<byte> target = new(backing);

        long read = stream.Read(target);

        await Assert.That(read).IsEqualTo(0L);
    }

    [Test]
    public async Task Read_LargeSpan_NullStream_Throws()
    {
        LargeArray<byte> backing = CreateSequentialArray(1, 0xB0);
        LargeSpan<byte> target = new(backing);

        await Assert.That(() => StreamExtensions.Read((Stream)null!, target)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Read_LargeSpan_EndOfStream_ReturnsPartial()
    {
        using MemoryStream stream = CreateStreamWithSequence(1, 0xC0);
        LargeArray<byte> backing = new(3);
        Fill(backing, 0xDD);
        LargeSpan<byte> target = new(backing);

        long first = stream.Read(target);
        long second = stream.Read(target);

        await Assert.That(first).IsEqualTo(1L);
        await Assert.That(second).IsEqualTo(0L);
    }

    private static LargeArray<byte> CreateSequentialArray(long count, byte start)
    {
        LargeArray<byte> array = new(count);
        for (long i = 0; i < count; i++)
        {
            array[i] = ExpectedByte(start, i);
        }
        return array;
    }

    private static LargeList<byte> CreateSequentialList(long count, byte start)
    {
        LargeList<byte> list = new(count);
        for (long i = 0; i < count; i++)
        {
            list.Add(ExpectedByte(start, i));
        }
        return list;
    }

    private static LargeList<byte> CreateFilledList(long count, byte value)
    {
        LargeList<byte> list = new(count);
        for (long i = 0; i < count; i++)
        {
            list.Add(value);
        }
        return list;
    }

    private static void Fill(LargeArray<byte> array, byte value)
    {
        for (long i = 0; i < array.Count; i++)
        {
            array[i] = value;
        }
    }

    private static MemoryStream CreateStreamWithSequence(long count, byte start)
    {
        byte[] data = ToByteSequence(count, start).ToArray();
        return new MemoryStream(data, writable: false);
    }

    private static IEnumerable<byte> ToByteSequence(long count, byte start)
    {
        for (long i = 0; i < count; i++)
        {
            yield return ExpectedByte(start, i);
        }
    }

    private static byte[] StreamStorageToArray(LargeList<byte> storage)
    {
        return storage.GetAll().ToArray();
    }

    private static byte ExpectedByte(byte start, long offset)
    {
        return (byte)((start + offset) & 0xFF);
    }

    private sealed class ReadOnlyLargeArrayWrapper : IReadOnlyLargeArray<byte>
    {
        private readonly IReadOnlyLargeArray<byte> _inner;

        public ReadOnlyLargeArrayWrapper(IReadOnlyLargeArray<byte> inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public long Count => _inner.Count;

        public byte this[long index] => _inner[index];

        public byte Get(long index) => _inner.Get(index);

        public long BinarySearch(byte item, Func<byte, byte, int> comparer) => _inner.BinarySearch(item, comparer);

        public long BinarySearch(byte item, Func<byte, byte, int> comparer, long offset, long count) => _inner.BinarySearch(item, comparer, offset, count);

        public IEnumerable<byte> GetAll() => _inner.GetAll();

        public IEnumerable<byte> GetAll(long offset, long count) => _inner.GetAll(offset, count);

        public bool Contains(byte item) => _inner.Contains(item);

        public bool Contains(byte item, Func<byte, byte, bool> equalsFunction) => _inner.Contains(item, equalsFunction);

        public bool Contains(byte item, long offset, long count) => _inner.Contains(item, offset, count);

        public bool Contains(byte item, long offset, long count, Func<byte, byte, bool> equalsFunction) => _inner.Contains(item, offset, count, equalsFunction);

        public long IndexOf(byte item) => _inner.IndexOf(item);

        public long IndexOf(byte item, Func<byte, byte, bool> equalsFunction) => _inner.IndexOf(item, equalsFunction);

        public long IndexOf(byte item, long offset, long count) => _inner.IndexOf(item, offset, count);

        public long IndexOf(byte item, long offset, long count, Func<byte, byte, bool> equalsFunction) => _inner.IndexOf(item, offset, count, equalsFunction);

        public long LastIndexOf(byte item) => _inner.LastIndexOf(item);

        public long LastIndexOf(byte item, Func<byte, byte, bool> equalsFunction) => _inner.LastIndexOf(item, equalsFunction);

        public long LastIndexOf(byte item, long offset, long count) => _inner.LastIndexOf(item, offset, count);

        public long LastIndexOf(byte item, long offset, long count, Func<byte, byte, bool> equalsFunction) => _inner.LastIndexOf(item, offset, count, equalsFunction);

        public void DoForEach(Action<byte> action) => _inner.DoForEach(action);

        public void DoForEach(Action<byte> action, long offset, long count) => _inner.DoForEach(action, offset, count);

        public void DoForEach<TUserData>(ActionWithUserData<byte, TUserData> action, ref TUserData userData) => _inner.DoForEach(action, ref userData);

        public void DoForEach<TUserData>(ActionWithUserData<byte, TUserData> action, long offset, long count, ref TUserData userData) => _inner.DoForEach(action, offset, count, ref userData);

        public void CopyTo(ILargeArray<byte> target, long sourceOffset, long targetOffset, long count) => _inner.CopyTo(target, sourceOffset, targetOffset, count);

        public void CopyTo(LargeSpan<byte> target, long sourceOffset, long count) => _inner.CopyTo(target, sourceOffset, count);

        public void CopyToArray(byte[] target, long sourceOffset, int targetOffset, int count) => _inner.CopyToArray(target, sourceOffset, targetOffset, count);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        public void CopyToSpan(Span<byte> target, long sourceOffset, int count) => _inner.CopyToSpan(target, sourceOffset, count);
#endif

        public IEnumerator<byte> GetEnumerator() => _inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
