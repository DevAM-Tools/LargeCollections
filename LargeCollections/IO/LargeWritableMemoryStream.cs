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
/// A slim writeonly non seekable wrapper for <see cref="Stream"/> APIs for <see cref="LargeList{byte}"/>.
/// </summary>
[DebuggerDisplay("LargeWritableMemoryStream: Length = {Length}")]
public class LargeWritableMemoryStream : Stream
{
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
        get => false;
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
        get => Length;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => throw new NotSupportedException();
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
        throw new NotSupportedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Writes the entire contents of the given buffer to the stream.
    /// </summary>
    /// <param name="buffer">The buffer to write from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(IReadOnlyLargeArray<byte> buffer)
    {
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }
        Storage.AddRange(buffer);
    }

    /// <summary>
    /// Writes a portion of the given buffer to the stream.
    /// </summary>
    /// <param name="buffer">The buffer to write from.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the stream.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(IReadOnlyLargeArray<byte> buffer, long offset)
    {
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }
        Storage.AddRange(buffer, offset);
    }

    /// <summary>
    /// Writes a portion of the given buffer to the stream.
    /// </summary>
    /// <param name="buffer">The buffer to write from.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the stream.</param>
    /// <param name="count">The number of bytes to write to the stream.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(IReadOnlyLargeArray<byte> buffer, long offset, long count)
    {
        Storage.AddRange(buffer, offset, count);
    }

    /// <summary>
    /// Writes the entire contents of the given buffer to the stream.
    /// </summary>
    /// <param name="buffer">The buffer to write from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlyLargeSpan<byte> buffer)
    {
        Storage.AddRange(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write(byte[] buffer, int offset, int count)
    {
        Storage.AddRange(buffer, offset, count);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write(ReadOnlySpan<byte> source)
    {
        Storage.AddRange(source);
    }
#endif
}
