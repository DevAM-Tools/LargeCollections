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

using System.Runtime.CompilerServices;

namespace LargeCollections;

public static class StreamExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, IReadOnlyLargeArray<byte> source)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        stream.Write(source, 0L, source.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, IReadOnlyLargeArray<byte> source, long offset)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        stream.Write(source, offset, source.Count - offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, IReadOnlyLargeArray<byte> source, long offset, long count)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        StorageExtensions.CheckRange(offset, count, source.Count);

        if (count == 0L)
        {
            return;
        }

        if (stream is LargeWritableMemoryStream largeWritableMemoryStream)
        {
            largeWritableMemoryStream.Storage.AddRange(source, offset, count);
        }
        else if (source is LargeArray<byte> largeArray)
        {
            byte[][] storage = largeArray.GetStorage();
            storage.StorageWriteToStream(stream, offset, count);
        }
        else if (source is LargeList<byte> largeList)
        {
            byte[][] storage = largeList.GetStorage();
            storage.StorageWriteToStream(stream, offset, count);
        }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        else
        {
            Span<byte> buffer = stackalloc byte[4096];
            long currentCount = 0L;

            while (currentCount < count)
            {
                int chunkSize = (int)Math.Min(buffer.Length, count - currentCount);
                Span<byte> currentBuffer = buffer.Slice(0, chunkSize);
                source.CopyToSpan(currentBuffer, offset + currentCount, chunkSize);
                stream.Write(currentBuffer);

                currentCount += chunkSize;
            }
        }
#else
        else
        {
            byte[] buffer = new byte[4096];
            long currentCount = 0L;

            while (currentCount < count)
            {
                int chunkSize = (int)Math.Min(buffer.Length, count - currentCount);
                source.CopyToArray(buffer, offset + currentCount, 0, chunkSize);
                stream.Write(buffer, 0, chunkSize);

                currentCount += chunkSize;
            }
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteToStream(this IReadOnlyLargeArray<byte> source, Stream stream)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        source.WriteToStream(stream, 0L, source.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteToStream(this IReadOnlyLargeArray<byte> source, Stream stream, long offset)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        source.WriteToStream(stream, offset, source.Count - offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteToStream(this IReadOnlyLargeArray<byte> source, Stream stream, long offset, long count)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        StorageExtensions.CheckRange(offset, count, source.Count);

        if (count == 0L)
        {
            return;
        }

        if (source is LargeArray<byte> largeArraySource)
        {
            byte[][] storage = largeArraySource.GetStorage();
            storage.StorageWriteToStream(stream, offset, count);
        }
        else if (source is LargeList<byte> largeListSource)
        {
            byte[][] storage = largeListSource.GetStorage();
            storage.StorageWriteToStream(stream, offset, count);
        }
        else
        {
            stream.Write(source, offset, count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Read(this Stream stream, ILargeArray<byte> target)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        return stream.Read(target, 0L, target.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Read(this Stream stream, ILargeArray<byte> target, long offset)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        return stream.Read(target, offset, target.Count - offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Read(this Stream stream, ILargeArray<byte> target, long offset, long count)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        StorageExtensions.CheckRange(offset, count, target.Count);

        if (count == 0L)
        {
            return 0L;
        }

        if (target is LargeArray<byte> largeArray)
        {
            byte[][] storage = largeArray.GetStorage();
            return storage.StorageReadFromStream(stream, offset, count);
        }
        else if (target is LargeList<byte> largeList)
        {
            byte[][] storage = largeList.GetStorage();
            return storage.StorageReadFromStream(stream, offset, count);
        }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        else
        {
            Span<byte> buffer = stackalloc byte[4096];
            long totalReadCount = 0L;
            long remaining = count;

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
            byte[] buffer = new byte[4096];
            long totalReadCount = 0L;
            long remaining = count;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadFromStream(ILargeArray<byte> target, Stream stream, long offset, long count)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (count == 0L)
        {
            return 0L;
        }

        StorageExtensions.CheckRange(offset, count, target.Count);

        if (target is LargeArray<byte> largeArray)
        {
            byte[][] storage = largeArray.GetStorage();
            return storage.StorageReadFromStream(stream, offset, count);
        }
        else if (target is LargeList<byte> largeList)
        {
            byte[][] storage = largeList.GetStorage();
            return storage.StorageReadFromStream(stream, offset, count);
        }
        else if (stream is LargeReadableMemoryStream largeReadableMemoryStream)
        {
            return largeReadableMemoryStream.Read(target, offset, count);
        }
        else
        {
            return stream.Read(target, offset, count);
        }
    }
}

