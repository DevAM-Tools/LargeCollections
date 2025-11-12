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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using LargeCollections;
using LargeCollections.Observable;
using LargeCollections.Test.Helpers;
using TUnit.Core;

namespace LargeCollections.Test.Observable;

public class ReadOnlyLargeObservableCollectionTest
{
    public static IEnumerable<long> Capacities() => Parameters.Capacities;

    #region Constructors and Disposal

    [Test]
    public async Task Constructor_ThrowsOnNullInner()
    {
        await Assert.That(() => new ReadOnlyLargeObservableCollection<int>(null!)).Throws<Exception>();
        await Assert.That(() => new ReadOnlyLargeObservableCollection<int>(null!, suppressEventExceptions: true)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Constructor_WrapsInner(long capacity)
    {
        LargeObservableCollection<long> inner = CreateCollectionWithSequence(capacity, 100L);

        ReadOnlyLargeObservableCollection<long> readOnly = new(inner);
        int collectionEvents = 0;
        readOnly.CollectionChanged += (_, _) => collectionEvents++;
        int propertyEvents = 0;
        readOnly.PropertyChanged += (_, _) => propertyEvents++;

        await Assert.That(readOnly.Count).IsEqualTo(inner.Count);
        if (readOnly.Count > 0)
        {
            await Assert.That(readOnly[0]).IsEqualTo(inner[0]);
        }

        int expectedCollectionEvents = 0;
        int expectedPropertyEvents = 0;
        if (inner.Count >= Constants.MaxLargeCollectionCount && inner.Count > 0)
        {
            inner.RemoveAt(inner.Count - 1L);
            expectedCollectionEvents++;
            expectedPropertyEvents++;
        }

        inner.Add(5000L);
        expectedCollectionEvents++;
        expectedPropertyEvents++;

        await Assert.That(collectionEvents).IsEqualTo(expectedCollectionEvents);
        await Assert.That(propertyEvents).IsEqualTo(expectedPropertyEvents);

        int collectionEventsBeforeDispose = collectionEvents;
        int propertyEventsBeforeDispose = propertyEvents;

        readOnly.Dispose();
        if (inner.Count < Constants.MaxLargeCollectionCount)
        {
            inner.Add(6000L);
        }
        else if (inner.Count > 0)
        {
            inner.RemoveAt(inner.Count - 1L);
        }

        await Assert.That(collectionEvents).IsEqualTo(collectionEventsBeforeDispose);
        await Assert.That(propertyEvents).IsEqualTo(propertyEventsBeforeDispose);
    }

    [Test]
    public async Task Constructor_SuppressEventExceptions_SwallowsHandlerErrors()
    {
        LargeObservableCollection<int> inner = new();
        ReadOnlyLargeObservableCollection<int> tolerant = new(inner, suppressEventExceptions: true);

        tolerant.CollectionChanged += (_, _) => throw new InvalidOperationException("ignore");
        tolerant.PropertyChanged += (_, _) => throw new InvalidOperationException("ignore");

        inner.Add(1);

        await Assert.That(tolerant.Count).IsEqualTo(1L);
    }

    [Test]
    public async Task Dispose_UnsubscribesFromInner()
    {
        LargeObservableCollection<int> inner = new();
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        int events = 0;
        readOnly.CollectionChanged += (_, _) => events++;

        inner.Add(1);
        await Assert.That(events).IsEqualTo(1);

        readOnly.Dispose();
        inner.Add(2);
        await Assert.That(events).IsEqualTo(1);
    }

    #endregion

    #region Indexer and Count

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Indexer_DelegatesToInner(long capacity)
    {
        LargeObservableCollection<long> inner = CreateCollectionWithSequence(capacity, 200L);
        ReadOnlyLargeObservableCollection<long> readOnly = new(inner);

        if (inner.Count == 0)
        {
            await Assert.That(() => readOnly[0]).Throws<Exception>();
        }
        else
        {
            await Assert.That(readOnly[0]).IsEqualTo(inner[0]);
        }
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Count_ReflectsInner(long capacity)
    {
        LargeObservableCollection<long> inner = CreateCollectionWithSequence(capacity, 300L);
        ReadOnlyLargeObservableCollection<long> readOnly = new(inner);

        await Assert.That(readOnly.Count).IsEqualTo(inner.Count);
    }

    #endregion

    #region Events and Suspension

    [Test]
    public async Task Events_ForwardedFromInner()
    {
        LargeObservableCollection<int> inner = new();
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        int collectionEvents = 0;
        int propertyEvents = 0;
        readOnly.CollectionChanged += (_, _) => collectionEvents++;
        readOnly.PropertyChanged += (_, _) => propertyEvents++;

        inner.Add(1);
        await Assert.That(collectionEvents).IsEqualTo(1);
        await Assert.That(propertyEvents).IsEqualTo(1);

        inner[0] = 5;
        await Assert.That(collectionEvents).IsEqualTo(2);
    }

    [Test]
    public async Task SuspendNotifications_BatchesChanges()
    {
        LargeObservableCollection<int> inner = new();
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        int resets = 0;
        int propertyCount = 0;
        readOnly.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                resets++;
            }
        };
        readOnly.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(readOnly.Count))
            {
                propertyCount++;
            }
        };

        using (readOnly.SuspendNotifications())
        {
            inner.Add(1);
            inner.Add(2);
        }

        await Assert.That(resets).IsEqualTo(1);
        await Assert.That(propertyCount).IsEqualTo(1);
    }

    [Test]
    public async Task SuspendNotifications_NoChanges_NoEvents()
    {
        LargeObservableCollection<int> inner = new();
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        int events = 0;
        readOnly.CollectionChanged += (_, _) => events++;

        using (readOnly.SuspendNotifications())
        {
        }

        await Assert.That(events).IsEqualTo(0);
    }

    [Test]
    public async Task SuspendNotifications_NoCountChange_SuppressesProperty()
    {
        LargeObservableCollection<int> inner = new();
        inner.Add(1);
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        int countChanges = 0;
        readOnly.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(readOnly.Count))
            {
                countChanges++;
            }
        };

        using (readOnly.SuspendNotifications())
        {
            inner[0] = 5;
        }

        await Assert.That(countChanges).IsEqualTo(0);
    }

    #endregion

    #region Read Operations

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task BinarySearch_DelegatesToInner(long capacity)
    {
        LargeObservableCollection<long> inner = CreateCollectionWithSequence(capacity, 400L);
        ReadOnlyLargeObservableCollection<long> readOnly = new(inner);
        Func<long, long, int> comparer = static (l, r) => l.CompareTo(r);

        long value = inner.Count > 0 ? inner[0] : 0L;
        await Assert.That(readOnly.BinarySearch(value, comparer)).IsEqualTo(inner.Count > 0 ? 0L : -1L);
        await Assert.That(readOnly.BinarySearch(value, comparer, 0L, inner.Count)).IsEqualTo(inner.Count > 0 ? 0L : -1L);

        await Assert.That(() => readOnly.BinarySearch(value, comparer, -1L, 1L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Contains_DelegatesToInner(long capacity)
    {
        LargeObservableCollection<long> inner = CreateCollectionWithSequence(capacity, 500L);
        ReadOnlyLargeObservableCollection<long> readOnly = new(inner);
        long value = inner.Count > 0 ? inner[0] : 0L;

        await Assert.That(readOnly.Contains(value)).IsEqualTo(inner.Count > 0);
        await Assert.That(readOnly.Contains(value, static (l, r) => l == r)).IsEqualTo(inner.Count > 0);
        await Assert.That(readOnly.Contains(value, 0L, inner.Count)).IsEqualTo(inner.Count > 0);
        await Assert.That(() => readOnly.Contains(value, -1L, 1L)).Throws<Exception>();

        if (inner.Count == 0)
        {
            await Assert.That(readOnly.Contains(123L, 0L, 0L)).IsFalse();
            return;
        }

        await Assert.That(readOnly.Contains(value + 1, static (candidate, needle) => candidate + 1 == needle)).IsTrue();
        await Assert.That(readOnly.Contains(value, static (candidate, needle) => candidate + 1 == needle)).IsFalse();

        if (inner.Count > 1)
        {
            long offset = 1L;
            long rangeCount = inner.Count - 1L;
            await Assert.That(readOnly.Contains(inner[1], offset, rangeCount)).IsTrue();
            await Assert.That(readOnly.Contains(inner[0], offset, rangeCount)).IsFalse();
            await Assert.That(readOnly.Contains(inner[1] + 1, offset, rangeCount, static (candidate, needle) => candidate + 1 == needle)).IsTrue();
        }
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyTo_Variants_Work(long capacity)
    {
        LargeObservableCollection<long> inner = CreateCollectionWithSequence(Math.Max(3L, capacity + 1L), 600L);
        ReadOnlyLargeObservableCollection<long> readOnly = new(inner);

        LargeArray<long> target = CreateSequentialArray(inner.Count + 2L, 0L);
        readOnly.CopyTo(target, 0L, 1L, Math.Min(2L, inner.Count));
        await Assert.That(target[1]).IsEqualTo(inner.Count > 0 ? inner[0] : target[1]);

        LargeSpan<long> spanTarget = new(CreateSequentialArray(inner.Count + 2L, 0L));
        readOnly.CopyTo(spanTarget, 0L, Math.Min(2L, inner.Count));

        long[] raw = new long[Math.Max(1, (int)Math.Min(2L, inner.Count))];
        readOnly.CopyToArray(raw, 0L, 0, raw.Length);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        readOnly.CopyToSpan(raw.AsSpan(), 0L, raw.Length);
#endif

        await Assert.That(() => readOnly.CopyTo(target, -1L, 0L, 1L)).Throws<Exception>();
        await Assert.That(() => readOnly.CopyToArray(raw, -1L, 0, 1)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Get_GetAll_Enumerator_Work(long capacity)
    {
        LargeObservableCollection<long> inner = CreateCollectionWithSequence(capacity, 700L);
        ReadOnlyLargeObservableCollection<long> readOnly = new(inner);

        if (inner.Count > 0)
        {
            await Assert.That(readOnly.Get(0L)).IsEqualTo(inner[0]);
        }
        else
        {
            await Assert.That(() => readOnly.Get(0L)).Throws<Exception>();
        }

        await Assert.That(readOnly.GetAll().SequenceEqual(inner.GetAll())).IsTrue();
        long offset = Math.Min(1L, Math.Max(0L, inner.Count - 1L));
        long count = inner.Count - offset;
        await Assert.That(readOnly.GetAll(offset, count).SequenceEqual(inner.GetAll(offset, count))).IsTrue();

        List<long> enumerated = new();
        foreach (long item in readOnly)
        {
            enumerated.Add(item);
        }
        await Assert.That(enumerated.SequenceEqual(inner.ToList())).IsTrue();

        IEnumerator nonGeneric = ((IEnumerable)readOnly).GetEnumerator();
        List<long> nonGenericItems = new();
        while (nonGeneric.MoveNext())
        {
            nonGenericItems.Add((long)nonGeneric.Current!);
        }
        await Assert.That(nonGenericItems.SequenceEqual(inner.ToList())).IsTrue();

        await Assert.That(() => readOnly.GetAll(-1L, 1L).ToList()).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task IndexLookups_DelegateToInner(long capacity)
    {
        LargeObservableCollection<long> inner = CreateCollectionWithSequence(capacity, 800L);
        ReadOnlyLargeObservableCollection<long> readOnly = new(inner);
        long firstValue = inner.Count > 0 ? inner[0] : 0L;
        long lastValue = inner.Count > 0 ? inner[inner.Count - 1L] : 0L;

        if (inner.Count > 0)
        {
            await Assert.That(readOnly.IndexOf(firstValue)).IsEqualTo(0L);
            await Assert.That(readOnly.IndexOf(firstValue, 0L, inner.Count)).IsEqualTo(0L);
            await Assert.That(readOnly.LastIndexOf(lastValue)).IsEqualTo(inner.Count - 1L);
            await Assert.That(readOnly.LastIndexOf(lastValue, 0L, inner.Count)).IsEqualTo(inner.Count - 1L);
        }
        else
        {
            await Assert.That(readOnly.IndexOf(firstValue)).IsEqualTo(-1L);
            await Assert.That(readOnly.LastIndexOf(lastValue)).IsEqualTo(-1L);
        }

        await Assert.That(() => readOnly.IndexOf(firstValue, -1L, 1L)).Throws<Exception>();

        if (inner.Count > 0)
        {
            Func<long, long, bool> offsetEquals = static (candidate, needle) => candidate + 1 == needle;
            await Assert.That(readOnly.IndexOf(firstValue + 1, offsetEquals)).IsEqualTo(0L);
            await Assert.That(readOnly.IndexOf(firstValue + 1, 0L, inner.Count, offsetEquals)).IsEqualTo(0L);

            await Assert.That(readOnly.LastIndexOf(lastValue + 1, offsetEquals)).IsEqualTo(inner.Count - 1L);
            await Assert.That(readOnly.LastIndexOf(lastValue + 1, 0L, inner.Count, offsetEquals)).IsEqualTo(inner.Count - 1L);
            long unmatchedNeedle = lastValue + 2L;
            await Assert.That(readOnly.IndexOf(unmatchedNeedle, offsetEquals)).IsEqualTo(-1L);
            await Assert.That(readOnly.LastIndexOf(unmatchedNeedle, offsetEquals)).IsEqualTo(-1L);
        }
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task DoForEach_Delegates(long capacity)
    {
        LargeObservableCollection<long> inner = CreateCollectionWithSequence(capacity, 900L);
        ReadOnlyLargeObservableCollection<long> readOnly = new(inner);

        long sum = 0L;
        readOnly.DoForEach(item => sum += item);
        await Assert.That(sum).IsEqualTo(inner.GetAll().Sum());

        long offset = Math.Min(1L, Math.Max(0L, inner.Count - 1L));
        long rangeCount = inner.Count - offset;
        long rangeSum = 0L;
        readOnly.DoForEach(item => rangeSum += item, offset, rangeCount);
        await Assert.That(rangeSum).IsEqualTo(inner.GetAll(offset, rangeCount).Sum());

        long accumulator = 0L;
        readOnly.DoForEach(static (long value, ref long acc) => acc += value, ref accumulator);
        await Assert.That(accumulator).IsEqualTo(inner.GetAll().Sum());

        long rangeAccumulator = 0L;
        readOnly.DoForEach(static (long value, ref long acc) => acc += value, offset, rangeCount, ref rangeAccumulator);
        await Assert.That(rangeAccumulator).IsEqualTo(inner.GetAll(offset, rangeCount).Sum());

        await Assert.That(() => readOnly.DoForEach(_ => { }, -1L, 1L)).Throws<Exception>();
    }

    #endregion

    #region AsReadOnly

    [Test]
    public async Task AsReadOnly_ReturnsNewWrapper()
    {
        LargeObservableCollection<int> inner = new();
        ReadOnlyLargeObservableCollection<int> first = new(inner);
        ReadOnlyLargeObservableCollection<int> second = first.AsReadOnly();

        await Assert.That(second).IsNotNull();
        await Assert.That(ReferenceEquals(first, second)).IsFalse();
        second.Dispose();
    }

    #endregion

    #region Helpers

    private static LargeObservableCollection<long> CreateCollectionWithSequence(long count, long start)
    {
        long actual = Math.Max(0L, Math.Min(count, Constants.MaxLargeCollectionCount));
        LargeObservableCollection<long> collection = new(actual);
        for (long i = 0; i < actual; i++)
        {
            collection.Add(start + i);
        }
        return collection;
    }

    private static LargeArray<long> CreateSequentialArray(long count, long start = 0L)
    {
        long actual = Math.Max(0L, Math.Min(count, Constants.MaxLargeCollectionCount));
        LargeArray<long> array = new(actual);
        for (long i = 0; i < actual; i++)
        {
            array[i] = start + i;
        }
        return array;
    }

    #endregion
}
