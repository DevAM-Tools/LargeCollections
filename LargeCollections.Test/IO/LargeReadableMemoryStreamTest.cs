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

public class LargeReadableMemoryStreamTest
{
    public static IEnumerable<long> Capacities() => Parameters.Capacities;

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Constructor_InitializesProperties(long count)
    {
        ReadOnlyLargeSpan<byte> source = CreateSpan(count, 0x10);
        LargeReadableMemoryStream stream = new(source);

        await Assert.That(stream.Length).IsEqualTo(count);
        await Assert.That(stream.Position).IsEqualTo(0L);
        await Assert.That(stream.CanRead).IsTrue();
        await Assert.That(stream.CanSeek).IsTrue();
        await Assert.That(stream.CanWrite).IsFalse();
        await Assert.That(stream.Source.Count).IsEqualTo(count);
    }

    [Test]
    public async Task Source_Setter_ResetsPositionAndAllowsNewData()
    {
        ReadOnlyLargeSpan<byte> initial = CreateSpan(5, 0x20);
        LargeReadableMemoryStream stream = new(initial);
        stream.ReadByte();

        ReadOnlyLargeSpan<byte> replacement = CreateSpan(3, 0x40);
        stream.Source = replacement;

        await Assert.That(stream.Position).IsEqualTo(0L);
        await Assert.That(stream.Length).IsEqualTo(3L);

        byte[] buffer = new byte[3];
        int read = stream.Read(buffer, 0, buffer.Length);

        await Assert.That(read).IsEqualTo(3);
        await Assert.That(buffer.SequenceEqual(ToByteSequence(0x40, 3))).IsTrue();
    }

    [Test]
    public async Task Position_Setter_ValidatesBounds()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(6, 0x30));

        stream.Position = 4;
        await Assert.That(stream.Position).IsEqualTo(4L);

        stream.Position = stream.Length;
        await Assert.That(stream.Position).IsEqualTo(stream.Length);

        await Assert.That(() => stream.Position = -1L).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => stream.Position = stream.Length + 1L).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Flush_DoesNotChangeState()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(4, 0x10));
        stream.Position = 2;

        stream.Flush();

        await Assert.That(stream.Position).IsEqualTo(2L);
        await Assert.That(stream.Length).IsEqualTo(4L);
    }

    [Test]
    public async Task ReadByte_ReturnsSequenceUntilEnd()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(3, 0x50));

        await Assert.That(stream.ReadByte()).IsEqualTo(0x50);
        await Assert.That(stream.ReadByte()).IsEqualTo(0x51);
        await Assert.That(stream.ReadByte()).IsEqualTo(0x52);
        await Assert.That(stream.ReadByte()).IsEqualTo(-1);
        await Assert.That(stream.Position).IsEqualTo(3L);
    }

    [Test]
    public async Task ReadByte_OnEmpty_ReturnsMinusOne()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(0, 0x00));

        await Assert.That(stream.ReadByte()).IsEqualTo(-1);
        await Assert.That(stream.Position).IsEqualTo(0L);
    }

    [Test]
    public async Task Read_LargeArray_ReadsAvailableData()
    {
        const byte start = 0x60;
        LargeReadableMemoryStream stream = new(CreateSpan(5, start));
        LargeArray<byte> target = CreateFilledLargeArray(8, 0xAA);

        long read = stream.Read(target, 2L, 6L);

        await Assert.That(read).IsEqualTo(5L);
        await Assert.That(stream.Position).IsEqualTo(5L);
        await Assert.That(target[0]).IsEqualTo((byte)0xAA);
        await Assert.That(target[1]).IsEqualTo((byte)0xAA);
        await Assert.That(target[7]).IsEqualTo((byte)0xAA);
        await Assert.That(target.GetAll(2L, read).SequenceEqual(ToByteSequence(start, read))).IsTrue();
    }

    [Test]
    public async Task Read_LargeArray_ZeroCount_ReturnsZero()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(3, 0x11));
        LargeArray<byte> target = CreateFilledLargeArray(4, 0x77);

        long read = stream.Read(target, 1L, 0L);

        await Assert.That(read).IsEqualTo(0L);
        await Assert.That(stream.Position).IsEqualTo(0L);
        await Assert.That(target[1]).IsEqualTo((byte)0x77);
    }

    [Test]
    public async Task Read_LargeArray_EndOfStream_ReturnsZero()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(2, 0x21));
        LargeArray<byte> target = CreateFilledLargeArray(2, 0x00);

        long first = stream.Read(target, 0L, 2L);
        long second = stream.Read(target, 0L, 2L);

        await Assert.That(first).IsEqualTo(2L);
        await Assert.That(second).IsEqualTo(0L);
        await Assert.That(stream.Position).IsEqualTo(2L);
    }

    [Test]
    public async Task Read_LargeArray_InvalidArguments_Throw()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(5, 0x33));
        LargeArray<byte> target = CreateFilledLargeArray(4, 0x00);

        await Assert.That(() => stream.Read((ILargeArray<byte>)null!, 0L, 1L)).Throws<ArgumentNullException>();
        await Assert.That(() => stream.Read(target, -1L, 1L)).Throws<ArgumentException>();
        await Assert.That(() => stream.Read(target, 0L, -1L)).Throws<ArgumentException>();
        await Assert.That(() => stream.Read(target, 1L, target.Count)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Read_LargeSpan_ReadsExpectedBytes()
    {
        const byte start = 0x70;
        LargeReadableMemoryStream stream = new(CreateSpan(4, start));
        LargeArray<byte> targetArray = CreateFilledLargeArray(4, 0xEE);
        LargeSpan<byte> targetSpan = new(targetArray);

        long read = stream.Read(targetSpan);

        await Assert.That(read).IsEqualTo(4L);
        await Assert.That(stream.Position).IsEqualTo(4L);
        await Assert.That(targetArray.GetAll().SequenceEqual(ToByteSequence(start, 4))).IsTrue();
    }

    [Test]
    public async Task Read_LargeSpan_TargetSmallerThanRemaining_ReadsPartial()
    {
        const byte start = 0x42;
        LargeReadableMemoryStream stream = new(CreateSpan(5, start));
        stream.Position = 3;

        LargeArray<byte> targetArray = CreateFilledLargeArray(4, 0xFE);
        LargeSpan<byte> targetSpan = new(targetArray);

        long read = stream.Read(targetSpan);

        await Assert.That(read).IsEqualTo(2L);
        await Assert.That(stream.Position).IsEqualTo(5L);
        await Assert.That(targetArray[0]).IsEqualTo(ExpectedByte(start, 3));
        await Assert.That(targetArray[1]).IsEqualTo(ExpectedByte(start, 4));
        await Assert.That(targetArray[2]).IsEqualTo((byte)0xFE);
        await Assert.That(targetArray[3]).IsEqualTo((byte)0xFE);
    }

    [Test]
    public async Task Read_LargeSpan_ZeroCount_ReturnsZero()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(3, 0x12));
        LargeArray<byte> targetArray = CreateFilledLargeArray(0, 0x00);
        LargeSpan<byte> targetSpan = new(targetArray);

        long read = stream.Read(targetSpan);

        await Assert.That(read).IsEqualTo(0L);
        await Assert.That(stream.Position).IsEqualTo(0L);
    }

    [Test]
    public async Task Read_LargeSpan_EndOfStream_ReturnsZero()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(1, 0x01));
        LargeArray<byte> targetArray = CreateFilledLargeArray(1, 0x00);
        LargeSpan<byte> targetSpan = new(targetArray);

        long first = stream.Read(targetSpan);
        long read = stream.Read(targetSpan);

        await Assert.That(first).IsEqualTo(1L);
        await Assert.That(read).IsEqualTo(0L);
    }

    [Test]
    public async Task Read_Array_ReadsExpectedBytes()
    {
        const byte start = 0x10;
        LargeReadableMemoryStream stream = new(CreateSpan(4, start));
        byte[] buffer = Enumerable.Repeat((byte)0xCC, 6).ToArray();

        int read = stream.Read(buffer, 1, 4);

        await Assert.That(read).IsEqualTo(4);
        await Assert.That(stream.Position).IsEqualTo(4L);
        await Assert.That(buffer[0]).IsEqualTo((byte)0xCC);
        await Assert.That(buffer[^1]).IsEqualTo((byte)0xCC);
        await Assert.That(buffer.Skip(1).Take(4).SequenceEqual(ToByteSequence(start, 4))).IsTrue();
    }

    [Test]
    public async Task Read_Array_ZeroCount_ReturnsZero()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(3, 0x05));
        byte[] buffer = new byte[3];

        int read = stream.Read(buffer, 0, 0);

        await Assert.That(read).IsEqualTo(0);
        await Assert.That(stream.Position).IsEqualTo(0L);
    }

    [Test]
    public async Task Read_Array_InvalidArguments_Throw()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(2, 0x07));
        byte[] buffer = new byte[2];

        await Assert.That(() => stream.Read((byte[])null!, 0, 1)).Throws<ArgumentNullException>();
        await Assert.That(() => stream.Read(buffer, -1, 1)).Throws<ArgumentException>();
        await Assert.That(() => stream.Read(buffer, 0, -1)).Throws<ArgumentException>();
        await Assert.That(() => stream.Read(buffer, 1, 2)).Throws<ArgumentException>();
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    public async Task Read_Span_ReadsExpectedBytes()
    {
        const byte start = 0x90;
        LargeReadableMemoryStream stream = new(CreateSpan(3, start));
        byte[] span = new byte[2];

        int read = stream.Read(span.AsSpan());

        await Assert.That(read).IsEqualTo(2);
        await Assert.That(stream.Position).IsEqualTo(2L);
        await Assert.That(span.SequenceEqual(ToByteSequence(start, 2))).IsTrue();
    }

    [Test]
    public async Task Read_Span_Empty_ReturnsZero()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(2, 0x22));
        Span<byte> span = Span<byte>.Empty;

        int read = stream.Read(span);

        await Assert.That(read).IsEqualTo(0);
        await Assert.That(stream.Position).IsEqualTo(0L);
    }

    [Test]
    public async Task Read_Span_EndOfStream_ReturnsZero()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(1, 0x44));
        Span<byte> span = stackalloc byte[1];

        int consumed = stream.Read(span);
        int read = stream.Read(span);

        await Assert.That(consumed).IsEqualTo(1);
        await Assert.That(read).IsEqualTo(0);
    }
#endif

    [Test]
    public async Task Seek_UpdatesPositionBasedOnOrigin()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(10, 0x10));

        long begin = stream.Seek(4, SeekOrigin.Begin);
        long current = stream.Seek(-2, SeekOrigin.Current);
        long end = stream.Seek(-1, SeekOrigin.End);

        await Assert.That(begin).IsEqualTo(4L);
        await Assert.That(current).IsEqualTo(2L);
        await Assert.That(end).IsEqualTo(stream.Length - 1L);
        await Assert.That(stream.Position).IsEqualTo(stream.Length - 1L);
    }

    [Test]
    public async Task Seek_InvalidArguments_Throw()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(5, 0x10));

        await Assert.That(() => stream.Seek(-1, SeekOrigin.Begin)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => stream.Seek(1, SeekOrigin.End)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => stream.Seek(long.MaxValue, SeekOrigin.Begin)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task SetLength_Throws()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(1, 0x00));

        await Assert.That(() => stream.SetLength(0)).Throws<NotSupportedException>();
    }

    [Test]
    public async Task Write_Throws()
    {
        LargeReadableMemoryStream stream = new(CreateSpan(1, 0x00));

        await Assert.That(() => stream.Write(Array.Empty<byte>(), 0, 0)).Throws<NotSupportedException>();
    }

    #region Helpers

    private static ReadOnlyLargeSpan<byte> CreateSpan(long count, byte start)
    {
        LargeArray<byte> array = CreateSequentialArray(count, start);
        return new ReadOnlyLargeSpan<byte>(array);
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

    private static LargeArray<byte> CreateFilledLargeArray(long count, byte value)
    {
        LargeArray<byte> array = new(count);
        for (long i = 0; i < count; i++)
        {
            array[i] = value;
        }
        return array;
    }

    private static IEnumerable<byte> ToByteSequence(byte start, long count)
    {
        for (long i = 0; i < count; i++)
        {
            yield return ExpectedByte(start, i);
        }
    }

    private static byte ExpectedByte(byte start, long offset)
    {
        return (byte)((start + offset) % 256);
    }

    #endregion
}
