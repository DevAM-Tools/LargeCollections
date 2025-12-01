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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace LargeCollections.IO;

/// <summary>
/// A slim writeonly seekable wrapper for <see cref="Stream"/> APIs for <see cref="LargeList{byte}"/>.
/// Supports seeking within the current length. Writing at a position overwrites existing data,
/// and any excess data is appended to the end.
/// </summary>
[DebuggerDisplay("LargeWritableMemoryStream: Position = {Position}, Length = {Length}")]
public class LargeWritableMemoryStream : Stream
{
    private long _position;

    public LargeWritableMemoryStream()
    {
        Storage = [];
    }

    public LargeWritableMemoryStream(long capacity)
    {
        Storage = new(capacity);
    }

    public LargeWritableMemoryStream(LargeList<byte> storage)
    {
        if (storage is null)
        {
            throw new ArgumentNullException(nameof(storage));
        }

        Storage = storage;
    }

    public LargeList<byte> Storage { get; private set; }

    public override bool CanRead
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => false;
    }

    public override bool CanSeek
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => true;
    }

    public override bool CanWrite
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => true;
    }

    public override long Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Storage is not null ? Storage.Count : 0L;
    }

    public override long Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (value < 0 || value > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Position must be within the bounds of the stream (0 to Length).");
            }
            _position = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Flush()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => Length + offset,
            _ => _position,
        };
        return _position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Writes bytes from the given buffer to the stream at the current position.
    /// Overwrites existing data and appends any excess.
    /// </summary>
    /// <param name="buffer">The buffer to write from.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the stream.</param>
    /// <param name="count">The number of bytes to write to the stream. If null, writes all remaining bytes from <paramref name="offset"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(IReadOnlyLargeArray<byte> buffer, long offset = 0L, long? count = null)
    {
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        long actualCount = count ?? (buffer.Count - offset);
        StorageExtensions.CheckRange(offset, actualCount, buffer.Count);

        if (actualCount == 0L)
        {
            return;
        }

        long overwriteCount = Math.Min(actualCount, Length - _position);
        if (overwriteCount > 0L)
        {
            // Overwrite existing data
            Storage.CopyFrom(buffer, offset, _position, overwriteCount);
        }

        long appendCount = actualCount - overwriteCount;
        if (appendCount > 0L)
        {
            // Append remaining data
            Storage.AddRange(buffer, offset + overwriteCount, appendCount);
        }

        _position += actualCount;
    }

    /// <summary>
    /// Writes the entire contents of the given buffer to the stream at the current position.
    /// Overwrites existing data and appends any excess.
    /// </summary>
    /// <param name="buffer">The buffer to write from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlyLargeSpan<byte> buffer)
    {
        if (buffer.Count == 0L)
        {
            return;
        }

        long overwriteCount = Math.Min(buffer.Count, Length - _position);
        if (overwriteCount > 0L)
        {
            // Overwrite existing data
            Storage.CopyFrom(buffer.Slice(0L, overwriteCount), _position, overwriteCount);
        }

        long appendCount = buffer.Count - overwriteCount;
        if (appendCount > 0L)
        {
            // Append remaining data
            Storage.AddRange(buffer.Slice(overwriteCount, appendCount));
        }

        _position += buffer.Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        StorageExtensions.CheckRange(offset, count, buffer.Length);

        if (count == 0)
        {
            return;
        }

        long overwriteCount = Math.Min(count, Length - _position);
        if (overwriteCount > 0L)
        {
            // Overwrite existing data
            Storage.CopyFromArray(buffer, offset, _position, (int)overwriteCount);
        }

        long appendCount = count - overwriteCount;
        if (appendCount > 0L)
        {
            // Append remaining data
            Storage.AddRange(buffer, offset + (int)overwriteCount, (int)appendCount);
        }

        _position += count;
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0)
        {
            return;
        }

        long overwriteCount = Math.Min(source.Length, Length - _position);
        if (overwriteCount > 0L)
        {
            // Overwrite existing data
            Storage.CopyFromSpan(source.Slice(0, (int)overwriteCount), _position, (int)overwriteCount);
        }

        int appendCount = source.Length - (int)overwriteCount;
        if (appendCount > 0)
        {
            // Append remaining data
            Storage.AddRange(source.Slice((int)overwriteCount, appendCount));
        }

        _position += source.Length;
    }
#endif
}
