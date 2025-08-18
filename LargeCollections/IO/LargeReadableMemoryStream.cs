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


using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LargeCollections;

/// <summary>
/// A slim readonly seekable wrapper for <see cref="Stream"/> APIs for <see cref="IReadOnlyLargeArray{byte}"/>.
/// </summary>
[DebuggerDisplay("LargeReadableMemoryStream: Position = {Position}, Length = {Length}")]
public class LargeReadableMemoryStream : Stream
{
    public LargeReadableMemoryStream(IReadOnlyLargeArray<byte> source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        Source = source;
        _Position = 0;
    }

    private IReadOnlyLargeArray<byte> _Source;

    public IReadOnlyLargeArray<byte> Source
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Source;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            _Source = value;
            _Position = 0L;
        }
    }

    private long _Position;

    public override bool CanRead
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => true;
    }

    public override bool CanSeek
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => true;
    }

    public override bool CanWrite
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => false;
    }

    public override long Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Source.Count;
    }

    public override long Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return _Position;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (value < 0 || value > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Position must be within the bounds of the stream.");
            }
            _Position = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Flush()
    {
    }

    public override int ReadByte()
    {
        if (Position >= Length)
        {
            return -1; // End of stream
        }

        byte value = _Source[(int)Position];
        Position++;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Read(ILargeArray<byte> target, long offset, long count)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        StorageExtensions.CheckRange(offset, count, target.Count);

        long maxReadableCount = Length - Position;
        if (maxReadableCount == 0L)
        {
            return 0;
        }
        if (count < maxReadableCount)
        {
            maxReadableCount = count;
        }

        _Source.CopyTo(target, Position, offset, maxReadableCount);
        Position += maxReadableCount;

        return maxReadableCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(byte[] target, int offset, int count)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        StorageExtensions.CheckRange(offset, count, target.Length);

        long maxReadableCount = Length - Position;
        if (maxReadableCount == 0L)
        {
            return 0;
        }
        if (count < maxReadableCount)
        {
            maxReadableCount = count;
        }

        _Source.CopyToArray(target, Position, offset, (int)maxReadableCount);
        Position += maxReadableCount;

        return (int)maxReadableCount;
    }

#if NETSTANDARD2_1_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(Span<byte> target)
    {
        long maxReadableCount = Length - Position;
        if (maxReadableCount == 0L)
        {
            return 0;
        }
        if (target.Length < maxReadableCount)
        {
            maxReadableCount = target.Length;
        }

        _Source.CopyToSpan(target, Position, (int)maxReadableCount);
        Position += maxReadableCount;

        return (int)maxReadableCount;
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => Position,
        };
        return Position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
