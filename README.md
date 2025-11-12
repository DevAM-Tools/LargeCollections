# What is LargeCollections?

LargeCollections is a library for .NET framework that offers a number of interfaces and types for collections that can store up to 1_152_921_504_606_846_976 items.
In comparison many .NET standard collections are limited to about 2.1 billion items.

Currently supported collections are:
- LargeArray<T>
- LargeList<T>
- LargeSet<T>
- LargeLinkedList<T>
- LargeSpan<T>
- ReadOnlyLargeSpan<T>
- LargeDictionary<TKey, TValue>
- DiskCache<TKey, TValue>
- SpatialDiskCache<long, TValue>
- LargeObservableCollection<T>
- ReadOnlyLargeObservableCollection<T>

LargeSpan<T> and ReadOnlyLargeSpan<T> are inspired by Span<T> and ReadOnlySpan<T> to simplify slice handling. However this comes at a small performance cost due to additional range and index checks.

DiskCache<TKey, TValue> is a dictionary-like collection that allows to limit the amount of memory (RAM) in MB that will be used.
Any memory requirement that exceeds this amount is automatically swapped out to disk. 
Additionally it offers multi-threaded operations for performance improvements.

SpatialDiskCache<long, TValue> is a DiskCache<long, TValue> that allows to create a spatial index for the contained elements that can be used for spatial queries.

LargeObservableCollection<T> allows to monitor changes of a list-like collection. It is the equivalent of an ObservableCollection<T>. ReadOnlyLargeObservableCollection<T> allows to monitor changes to a collection while not providing the option of modifying it.

Since many .NET API are designed to work with Streams the LargeWritableMemoryStream and LargeReadableMemoryStream implement a reusable wrapper around LargeList<byte> and IReadOnlyLargeArray<byte>.

# License

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

SPDX-License-Identifier: MIT