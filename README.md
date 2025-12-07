# What is LargeCollections?

LargeCollections is a library for .NET framework that offers a number of interfaces and types for collections that can store up to 1_152_921_504_606_846_976 items.
In comparison many .NET standard collections are limited to about 2.1 billion items.

## Collections

### Core Collections
- **LargeArray&lt;T&gt;** - Store billions of elements in a single array – no more 2GB limits
- **LargeList&lt;T&gt;** - A growable list that scales beyond standard .NET limits
- **LargeSet&lt;T&gt;** - Lightning-fast uniqueness checks, even with massive datasets
- **LargeDictionary&lt;TKey, TValue&gt;** - Key-value lookups that stay fast at any scale
- **LargeLinkedList&lt;T&gt;** - Efficient insertions and deletions anywhere in the list
- **LargeSpan&lt;T&gt;** - Work with slices of data without copying
- **ReadOnlyLargeSpan&lt;T&gt;** - Safe, read-only views into your data

### Tree-Based Collections
- **LargeBPlusTree&lt;TKey, TValue&gt;** - Keep data sorted automatically; perfect for range queries like "find all items between A and B"
- **LargeKDTree&lt;T&gt;** - Find nearest neighbors in milliseconds among millions of points (e.g., "find the closest store to me")
- **LargeBKDTree&lt;T&gt;** - Blazing-fast spatial searches; ideal for geographic data and bounding box queries

### Observable Collections
- **LargeObservableCollection&lt;T&gt;** - Get notified when your data changes – great for UI bindings
- **ReadOnlyLargeObservableCollection&lt;T&gt;** - Monitor changes without allowing modifications

### Disk-Backed Collections
- **DiskCache&lt;TKey, TValue&gt;** - Handle datasets larger than your RAM; data automatically spills to disk when needed
- **SpatialDiskCache&lt;long, TValue&gt;** - Combine disk caching with spatial queries for massive geographic datasets

### Stream Wrappers
- **LargeWritableMemoryStream** - Write large amounts of data as a stream without hitting memory limits
- **LargeReadableMemoryStream** - Read large byte collections through the familiar Stream API

## Why Use Each Collection?

| If you need to... | Use this |
|-------------------|----------|
| Store billions of items in order | LargeArray, LargeList |
| Check if items exist (fast) | LargeSet |
| Look up values by key | LargeDictionary |
| Keep data sorted & query ranges | LargeBPlusTree |
| Find nearest points in space | LargeKDTree, LargeBKDTree |
| React to data changes in UI | LargeObservableCollection |
| Work with more data than fits in RAM | DiskCache |
| Query geographic/spatial data on disk | SpatialDiskCache |

## Features

- **No Size Limits** – Work with up to 1.15 quintillion (1,152,921,504,606,846,976) elements
- **Near-Native Speed** – Hash collections perform within 1.4× of .NET's built-in Dictionary
- **Smooth Performance** – Spatial queries run without causing garbage collection pauses
- **Always Sorted** – B+Trees keep your data in order automatically as you add and remove items
- **Find Nearby Fast** – Spatial trees locate nearest neighbors among millions of points in microseconds
- **Memory Smart** – Disk-backed collections let you work with more data than fits in RAM
- **Cross-Platform** – Supports .NET Standard 2.0/2.1, .NET 6, 8, and 10

## Installation

Install via NuGet:

```bash
dotnet add package DevAM.LargeCollections
dotnet add package DevAM.LargeCollections.DiskCache
dotnet add package DevAM.LargeCollections.Observable
```

## Target Frameworks

- .NET Standard 2.0 / 2.1
- .NET 6.0 / 8.0 / 10.0

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