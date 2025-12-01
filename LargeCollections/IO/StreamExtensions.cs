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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER || NET5_0_OR_GREATER
using System.Buffers;
#endif
using System.IO;
using System.Runtime.CompilerServices;

namespace LargeCollections.IO;

public static class StreamExtensions
{
    /// <summary>
    /// Writes bytes from the <paramref name="source"/> to the <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The stream where the bytes will be written to.</param>
    /// <param name="source">The source where the bytes will be read from.</param>
    /// <param name="offset">The offset where the first byte will be read from.</param>
    /// <param name="count">The number of bytes that will be written. If null, writes all remaining bytes from <paramref name="offset"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, IReadOnlyLargeArray<byte> source, long offset = 0L, long? count = null)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        long actualCount = count ?? source.Count - offset;
        if (actualCount == 0L)
        {
            return;
        }

        StorageExtensions.CheckRange(offset, actualCount, source.Count);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER

        Span<byte> buffer = stackalloc byte[2048];
        long currentCount = 0L;

        while (currentCount < actualCount)
        {
            int chunkSize = (int)Math.Min(buffer.Length, actualCount - currentCount);
            Span<byte> currentBuffer = buffer.Slice(0, chunkSize);
            source.CopyToSpan(currentBuffer, offset + currentCount, chunkSize);
            stream.Write(currentBuffer);

            currentCount += chunkSize;
        }

#else
        byte[] buffer = new byte[8192];
        long currentCount = 0L;

        while (currentCount < actualCount)
        {
            int chunkSize = (int)Math.Min(buffer.Length, actualCount - currentCount);
            source.CopyToArray(buffer, offset + currentCount, 0, chunkSize);
            stream.Write(buffer, 0, chunkSize);

            currentCount += chunkSize;
        }

#endif
    }

    /// <summary>
    /// Writes bytes from the <paramref name="source"/> to the <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The stream where the bytes will be written to.</param>
    /// <param name="source">The source where the bytes will be read from.</param>
    /// <param name="offset">The offset where the first byte will be read from.</param>
    /// <param name="count">The number of bytes that will be written. If null, writes all remaining bytes from <paramref name="offset"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, LargeArray<byte> source, long offset = 0L, long? count = null)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        long actualCount = count ?? source.Count - offset;
        if (actualCount == 0L)
        {
            return;
        }

        StorageExtensions.CheckRange(offset, actualCount, source.Count);

        if (stream is LargeWritableMemoryStream largeWritableMemoryStream)
        {
            largeWritableMemoryStream.Storage.AddRange(source, offset, actualCount);
        }
        else
        {
            byte[][] storage = source.GetStorage();
            storage.StorageWriteToStream(stream, offset, actualCount);
        }
    }

    /// <summary>
    /// Writes bytes from the <paramref name="source"/> to the <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The stream where the bytes will be written to.</param>
    /// <param name="source">The source where the bytes will be read from.</param>
    /// <param name="offset">The offset where the first byte will be read from.</param>
    /// <param name="count">The number of bytes that will be written. If null, writes all remaining bytes from <paramref name="offset"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, LargeList<byte> source, long offset = 0L, long? count = null)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        long actualCount = count ?? source.Count - offset;
        if (actualCount == 0L)
        {
            return;
        }

        StorageExtensions.CheckRange(offset, actualCount, source.Count);

        if (stream is LargeWritableMemoryStream largeWritableMemoryStream)
        {
            largeWritableMemoryStream.Storage.AddRange(source, offset, actualCount);
        }
        else
        {
            byte[][] storage = source.GetStorage();
            storage.StorageWriteToStream(stream, offset, actualCount);
        }
    }

    /// <summary>
    /// Writes all bytes from the <paramref name="source"/> to the <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The stream where the bytes will be written to.</param>
    /// <param name="source">The source where the bytes will be read from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, ReadOnlyLargeSpan<byte> source)
    {
        if (source.Count == 0L)
        {
            return;
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (stream is LargeWritableMemoryStream largeWritableMemoryStream)
        {
            if (source.Inner is LargeArray<byte> largeArray)
            {
                largeWritableMemoryStream.Storage.AddRange(largeArray, source.Start, source.Count);
            }
            else if (source.Inner is LargeList<byte> largeList)
            {
                largeWritableMemoryStream.Storage.AddRange(largeList, source.Start, source.Count);
            }
            else
            {
                largeWritableMemoryStream.Storage.AddRange(source);
            }

        }
        else if (source.Inner is LargeArray<byte> largeArray)
        {
            byte[][] storage = largeArray.GetStorage();
            storage.StorageWriteToStream(stream, source.Start, source.Count);
        }
        else if (source.Inner is LargeList<byte> largeList)
        {
            byte[][] storage = largeList.GetStorage();
            storage.StorageWriteToStream(stream, source.Start, source.Count);
        }
        else
        {
            stream.Write(source.Inner, source.Start, source.Count);
        }
    }

    /// <summary>
    /// Reads bytes from the <paramref name="stream"/> to the <paramref name="target"/>.
    /// </summary>
    /// <param name="stream">The stream where the bytes will be read from.</param>
    /// <param name="target">The target where the bytes will be written to.</param>
    /// <param name="offset">The offset where the first byte will be written to.</param>
    /// <param name="count">The number of bytes that will be read. If null, reads to fill the remaining capacity from <paramref name="offset"/>.</param>
    /// <returns>The total number of bytes read.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Read(this Stream stream, ILargeArray<byte> target, long offset = 0L, long? count = null)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        long actualCount = count ?? target.Count - offset;
        if (actualCount == 0L)
        {
            return 0L;
        }

        StorageExtensions.CheckRange(offset, actualCount, target.Count);

        if (target is LargeArray<byte> largeArray)
        {
            byte[][] storage = largeArray.GetStorage();
            return storage.StorageReadFromStream(stream, offset, actualCount);
        }
        else if (target is LargeList<byte> largeList)
        {
            byte[][] storage = largeList.GetStorage();
            return storage.StorageReadFromStream(stream, offset, actualCount);
        }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        else
        {
            Span<byte> buffer = stackalloc byte[2048];
            long totalReadCount = 0L;
            long remaining = actualCount;

            while (remaining > 0L)
            {
                int chunkSize = (int)Math.Min(buffer.Length, remaining);
                Span<byte> currentBuffer = buffer.Slice(0, chunkSize);
                int bytesReadCount = stream.Read(currentBuffer);

                if (bytesReadCount == 0)
                {
                    break;
                }

                target.CopyFromSpan(currentBuffer, offset + totalReadCount, bytesReadCount);

                totalReadCount += bytesReadCount;
                remaining -= bytesReadCount;
            }

            return totalReadCount;
        }
#else
        else
        {
            byte[] buffer = new byte[8192];
            long totalReadCount = 0L;
            long remaining = actualCount;

            while (remaining > 0L)
            {
                int chunkSize = (int)Math.Min(buffer.Length, remaining);
                int bytesReadCount = stream.Read(buffer, 0, chunkSize);

                if (bytesReadCount == 0)
                {
                    break;
                }

                target.CopyFromArray(buffer, 0, offset + totalReadCount, bytesReadCount);

                totalReadCount += bytesReadCount;
                remaining -= bytesReadCount;
            }

            return totalReadCount;
        }
#endif

    }

    /// <summary>
    /// Reads all bytes from the <paramref name="stream"/> to the <paramref name="target"/>.
    /// </summary>
    /// <param name="stream">The stream where the bytes will be read from.</param>
    /// <param name="target">The target where the bytes will be written to.</param>
    /// <returns>The total number of bytes read.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Read(this Stream stream, LargeSpan<byte> target)
    {
        if (target.Count == 0)
        {
            return 0L;
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (target.Inner is LargeArray<byte> largeArray)
        {
            byte[][] storage = largeArray.GetStorage();
            return storage.StorageReadFromStream(stream, target.Start, target.Count);
        }
        else if (target.Inner is LargeList<byte> largeList)
        {
            byte[][] storage = largeList.GetStorage();
            return storage.StorageReadFromStream(stream, target.Start, target.Count);
        }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        else
        {
            Span<byte> buffer = stackalloc byte[2048];
            long totalReadCount = 0L;
            long remaining = target.Count;

            while (remaining > 0L)
            {
                int chunkSize = (int)Math.Min(buffer.Length, remaining);
                Span<byte> currentBuffer = buffer.Slice(0, chunkSize);
                int bytesReadCount = stream.Read(currentBuffer);

                if (bytesReadCount == 0)
                {
                    break;
                }

                target.CopyFromSpan(currentBuffer, totalReadCount, bytesReadCount);

                totalReadCount += bytesReadCount;
                remaining -= bytesReadCount;
            }

            return totalReadCount;
        }
#else
        else
        {
            byte[] buffer = new byte[8192];
            long totalReadCount = 0L;
            long remaining = target.Count;

            while (remaining > 0L)
            {
                int chunkSize = (int)Math.Min(buffer.Length, remaining);
                int bytesReadCount = stream.Read(buffer, 0, chunkSize);

                if (bytesReadCount == 0)
                {
                    break;
                }

                target.CopyFromArray(buffer, 0, totalReadCount, bytesReadCount);

                totalReadCount += bytesReadCount;
                remaining -= bytesReadCount;
            }

            return totalReadCount;
        }
#endif

    }
}

