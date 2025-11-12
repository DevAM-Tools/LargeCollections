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

public class LargeObservableCollectionTest
{
    public static IEnumerable<long> Capacities() => Parameters.Capacities;

    #region Constructors

    [Test]
    public async Task Constructor_DefaultInitializesEmpty()
    {
        LargeObservableCollection<long> collection = new();

        await Assert.That(collection.Count).IsEqualTo(0L);
        await Assert.That(collection.GetAll().Any()).IsFalse();
        await Assert.That(collection.AsReadOnly()).IsNotNull();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Constructor_WithInitialCapacity_AllowsAdd(long capacity)
    {
        long initialCapacity = Math.Max(0L, Math.Min(capacity, Constants.MaxLargeCollectionCount));
        LargeObservableCollection<long> collection = new(initialCapacity);
        using EventRecorder<long> recorder = new(collection);

        collection.Add(1L);

        await Assert.That(collection.Count).IsEqualTo(1L);
        await Assert.That(collection[0]).IsEqualTo(1L);
        await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(1);
        await Assert.That(recorder.PropertyEvents.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Constructor_SuppressEventExceptions_ControlsPropagation()
    {
        LargeObservableCollection<long> strict = new(suppressEventExceptions: false);
        strict.CollectionChanged += (_, _) => throw new InvalidOperationException("boom");

        await Assert.That(() => strict.Add(1L)).Throws<InvalidOperationException>();

        LargeObservableCollection<long> tolerant = new(suppressEventExceptions: true);
        int propertyCalls = 0;
        tolerant.CollectionChanged += (_, _) => throw new InvalidOperationException("ignored");
        tolerant.PropertyChanged += (_, _) => propertyCalls++;
        tolerant.PropertyChanged += (_, _) => throw new InvalidOperationException("ignored property");

        tolerant.Add(2L);

        await Assert.That(tolerant.Count).IsEqualTo(1L);
        await Assert.That(propertyCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Constructor_WithCapacityAndSuppression_AllowsOperations()
    {
        LargeObservableCollection<long> collection = new(initialCapacity: 3, suppressEventExceptions: true);
        collection.AddRange(new long[] { 1, 2, 3 });

        await Assert.That(collection.Count).IsEqualTo(3L);
        await Assert.That(collection[1]).IsEqualTo(2L);
    }

    #endregion

    #region Indexer and Set

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Indexer_GetSet_RaisesReplace(long capacity)
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(capacity, 0L);
        if (collection.Count == 0)
        {
            await Assert.That(() => collection[0]).Throws<Exception>();
            await Assert.That(() => collection[0] = 1L).Throws<Exception>();
            return;
        }

        using EventRecorder<long> recorder = new(collection);
        long index = Math.Min(collection.Count - 1, 1L);
        long newValue = collection[index] + 5L;
        collection[index] = newValue;

        await Assert.That(collection[index]).IsEqualTo(newValue);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Replace);
        await Assert.That(recorder.PropertyEvents.Any()).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Set_Method_UpdatesItem(long capacity)
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(capacity, 10L);
        if (collection.Count == 0)
        {
            await Assert.That(() => collection.Set(0L, 1L)).Throws<Exception>();
            return;
        }

        using EventRecorder<long> recorder = new(collection);
        long index = Math.Min(collection.Count - 1, 1L);
        long newValue = 12345L;
        collection.Set(index, newValue);

        await Assert.That(collection[index]).IsEqualTo(newValue);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Replace);
    }

    #endregion

    #region Add and AddRange

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Add_RaisesEventAndProperty(long capacity)
    {
        LargeObservableCollection<long> collection = new(capacity);
        using EventRecorder<long> recorder = new(collection);

        collection.Add(42L);

        await Assert.That(collection.Count).IsEqualTo(1L);
        await Assert.That(recorder.CollectionEvents.Single().Action).IsEqualTo(NotifyCollectionChangedAction.Add);
        await Assert.That(recorder.PropertyEvents.Single().PropertyName).IsEqualTo(nameof(collection.Count));
    }

    [Test]
    public async Task AddRange_IEnumerable_BehavesByCount()
    {
        LargeObservableCollection<long> collection = new();
        using EventRecorder<long> recorder = new(collection);

        collection.AddRange(Array.Empty<long>());
        await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);

        collection.AddRange(new long[] { 1 });
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Add);

        collection.AddRange(new long[] { 2, 3 });
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
        await Assert.That(recorder.PropertyEvents.Count).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task AddRange_IEnumerable_EnumeratesSequence()
    {
        LargeObservableCollection<long> collection = new();
        using EventRecorder<long> recorder = new(collection);

        collection.AddRange(GenerateSequence(3));

        await Assert.That(collection.Count).IsEqualTo(3L);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task AddRange_IEnumerable_ThrowsOnNull()
    {
        LargeObservableCollection<long> collection = new();
        await Assert.That(() => collection.AddRange((IEnumerable<long>)null!)).Throws<Exception>();
    }

    [Test]
    public async Task AddRange_IReadOnlyLargeArray_OverloadsWork()
    {
        LargeObservableCollection<long> collection = new();
        LargeArray<long> source = CreateSequentialArray(5);
        using EventRecorder<long> recorder = new(collection);

        collection.AddRange(source);
        await Assert.That(collection.Count).IsEqualTo(source.Count);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);

        collection.Clear();
        recorder.Reset();
        collection.AddRange(source, 2L);
        await Assert.That(collection.GetAll().SequenceEqual(source.GetAll().Skip(2))).IsTrue();

        collection.Clear();
        recorder.Reset();
        collection.AddRange(source, 1L, 2L);
        await Assert.That(collection.GetAll().SequenceEqual(source.GetAll().Skip(1).Take(2))).IsTrue();

        collection.Clear();
        recorder.Reset();
        collection.AddRange(source, source.Count - 1L, 1L);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Add);

        await Assert.That(() => collection.AddRange((IReadOnlyLargeArray<long>)null!)).Throws<Exception>();
        await Assert.That(() => collection.AddRange(source, -1L)).Throws<Exception>();
        await Assert.That(() => collection.AddRange(source, 0L, source.Count + 1L)).Throws<Exception>();
    }

    [Test]
    public async Task AddRange_IReadOnlyLargeArray_ZeroCount_SuppressesEvents()
    {
        LargeObservableCollection<long> collection = new();
        LargeArray<long> source = CreateSequentialArray(0);
        using EventRecorder<long> recorder = new(collection);

        collection.AddRange(source);

        await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
        await Assert.That(recorder.PropertyEvents.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AddRange_ReadOnlyLargeSpan_AddsItems()
    {
        LargeArray<long> source = CreateSequentialArray(3);
        ReadOnlyLargeSpan<long> span = new(source, 0L, source.Count);
        LargeObservableCollection<long> collection = new();
        using EventRecorder<long> recorder = new(collection);

        collection.AddRange(span);

        await Assert.That(collection.GetAll().SequenceEqual(source.GetAll())).IsTrue();
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);

        collection.Clear();
        recorder.Reset();
        ReadOnlyLargeSpan<long> singleSpan = new(source, 0L, 1L);
        collection.AddRange(singleSpan);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Add);
    }

    [Test]
    public async Task AddRange_ReadOnlyLargeSpan_ZeroCount_NoEvents()
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(2L, 0L);
        using EventRecorder<long> recorder = new(collection);

        collection.AddRange(default(ReadOnlyLargeSpan<long>));

        await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
        await Assert.That(recorder.PropertyEvents.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AddRange_ArrayOverloads_AddItems()
    {
        LargeObservableCollection<long> collection = new();
        long[] source = new long[] { 5, 6, 7, 8 };
        using EventRecorder<long> recorder = new(collection);

        collection.AddRange(source);
        await Assert.That(collection.Count).IsEqualTo(source.Length);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);

        collection.Clear();
        recorder.Reset();
        collection.AddRange(source, 1);
        await Assert.That(collection.GetAll().SequenceEqual(source.Skip(1))).IsTrue();

        collection.Clear();
        recorder.Reset();
        collection.AddRange(source, 1, 2);
        await Assert.That(collection.GetAll().SequenceEqual(source.Skip(1).Take(2))).IsTrue();

        collection.Clear();
        recorder.Reset();
        collection.AddRange(source, 0, 1);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Add);

        await Assert.That(() => collection.AddRange((long[])null!)).Throws<Exception>();
        await Assert.That(() => collection.AddRange(source, -1)).Throws<Exception>();
        await Assert.That(() => collection.AddRange(source, 0, source.Length + 1)).Throws<Exception>();
    }

    [Test]
    public async Task AddRange_ArrayOverloads_ZeroCount_NoEvents()
    {
        LargeObservableCollection<long> collection = new();
        long[] source = new long[] { 1, 2, 3 };
        using EventRecorder<long> recorder = new(collection);

        collection.AddRange(source, 0, 0);

        await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
        await Assert.That(recorder.PropertyEvents.Count).IsEqualTo(0);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    public async Task AddRange_ReadOnlySpan_AddsItems()
    {
        LargeObservableCollection<long> collection = new();
        long[] source = new long[] { 9, 10, 11 };
        using EventRecorder<long> recorder = new(collection);

        collection.AddRange(source.AsSpan());

        await Assert.That(collection.GetAll().SequenceEqual(source)).IsTrue();
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);

        collection.Clear();
        recorder.Reset();
        collection.AddRange(source.AsSpan(0, 1));
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Add);
    }

    [Test]
    public async Task AddRange_ReadOnlySpan_Empty_NoEvents()
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(1L, 0L);
        using EventRecorder<long> recorder = new(collection);

        collection.AddRange(ReadOnlySpan<long>.Empty);

        await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
        await Assert.That(recorder.PropertyEvents.Count).IsEqualTo(0);
    }
#endif

    #endregion

    #region Remove and Clear

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Clear_RemovesItems_WhenNotEmpty(long capacity)
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(capacity, 0L);
        using EventRecorder<long> recorder = new(collection);

        collection.Clear();

        await Assert.That(collection.Count).IsEqualTo(0L);
        if (capacity > 0)
        {
            await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
            await Assert.That(recorder.PropertyEvents.Last().PropertyName).IsEqualTo(nameof(collection.Count));
        }
        else
        {
            await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
        }
    }

    [Test]
    public async Task Remove_ReturnsFalse_WhenMissing()
    {
        LargeObservableCollection<long> collection = new();
        using EventRecorder<long> recorder = new(collection);

        bool removed = collection.Remove(99L);

        await Assert.That(removed).IsFalse();
        await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Remove_RemovesItem_AndRaisesReset(long capacity)
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(Math.Max(1L, capacity), 5L);
        using EventRecorder<long> recorder = new(collection);

        bool removed = collection.Remove(5L, preserveOrder: true, out long removedValue, static (l, r) => l == r);

        await Assert.That(removed).IsTrue();
        await Assert.That(removedValue).IsEqualTo(5L);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
        await Assert.That(recorder.PropertyEvents.Last().PropertyName).IsEqualTo(nameof(collection.Count));
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Remove_OutParameter_ReturnsRemovedItem(long capacity)
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(capacity, 20L);
        using EventRecorder<long> recorder = new(collection);

        bool expectedRemoved = collection.Count > 0;
        long target = expectedRemoved ? collection[0] : 20L;

        bool removed = collection.Remove(target, out long removedItem);

        await Assert.That(removed).IsEqualTo(expectedRemoved);
        if (expectedRemoved)
        {
            await Assert.That(removedItem).IsEqualTo(target);
            await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
        }
        else
        {
            await Assert.That(removedItem).IsEqualTo(0L);
            await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
            await Assert.That(recorder.PropertyEvents.Count).IsEqualTo(0);
        }
    }

    [Test]
    public async Task Remove_PreserveOrderFalse_UsesSwapRemoval()
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(3L, 30L);
        using EventRecorder<long> recorder = new(collection);

        bool removed = collection.Remove(31L, preserveOrder: false, out long removedItem);

        await Assert.That(removed).IsTrue();
        await Assert.That(removedItem).IsEqualTo(31L);
        await Assert.That(collection.Count).IsEqualTo(2L);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task Remove_WithComparerOnly_UsesEqualityFunction()
    {
        LargeObservableCollection<string> collection = new();
        using EventRecorder<string> recorder = new(collection);
        collection.Add("FOO");

        bool removed = collection.Remove("foo", static (l, r) => string.Equals(l, r, StringComparison.OrdinalIgnoreCase));

        await Assert.That(removed).IsTrue();
        await Assert.That(collection.Count).IsEqualTo(0L);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task Remove_PreserveOrderFlagOnly_Overload_Works()
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(3L, 400L);
        using EventRecorder<long> recorder = new(collection);

        bool removed = collection.Remove(401L, preserveOrder: false);

        await Assert.That(removed).IsTrue();
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task Remove_WithComparerAndOutParameter_ReturnsItem()
    {
        LargeObservableCollection<string> collection = new();
        using EventRecorder<string> recorder = new(collection);
        collection.Add("alpha");

        bool removed = collection.Remove("ALPHA", out string removedItem, static (l, r) => string.Equals(l, r, StringComparison.OrdinalIgnoreCase));

        await Assert.That(removed).IsTrue();
        await Assert.That(removedItem).IsEqualTo("alpha");
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task Remove_WithComparerAndPreserveFlag_Removes()
    {
        LargeObservableCollection<string> collection = new();
        using EventRecorder<string> recorder = new(collection);
        collection.AddRange(new[] { "x", "y" });

        bool removed = collection.Remove("Y", preserveOrder: false, static (l, r) => string.Equals(l, r, StringComparison.OrdinalIgnoreCase));

        await Assert.That(removed).IsTrue();
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task RemoveAt_RaisesEvent(long capacity)
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(Math.Max(1L, capacity), 100L);
        using EventRecorder<long> recorder = new(collection);

        long index = Math.Min(collection.Count - 1, 1L);
        long removed = collection.RemoveAt(index);

        await Assert.That(removed).IsEqualTo(100L + index);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Remove);
        await Assert.That(recorder.PropertyEvents.Last().PropertyName).IsEqualTo(nameof(collection.Count));
    }

    [Test]
    public async Task RemoveAt_PreserveOrderFalse_RaisesEvent()
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(3L, 200L);
        using EventRecorder<long> recorder = new(collection);

        long removed = collection.RemoveAt(1L, preserveOrder: false);

        await Assert.That(removed).IsEqualTo(201L);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Remove);
    }

    [Test]
    public async Task RemoveAt_InvalidIndex_Throws()
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(1L, 0L);
        await Assert.That(() => collection.RemoveAt(-1L)).Throws<Exception>();
    }

    #endregion

    #region CopyFrom and CopyTo

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyFrom_IReadOnlyLargeArray_TriggersReplaceOrReset(long capacity)
    {
        LargeArray<long> source = CreateSequentialArray(Math.Max(2L, capacity + 1L), 500L);
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(Math.Max(2L, capacity + 1L), 1000L);
        using EventRecorder<long> recorder = new(collection);

        collection.CopyFrom(source, 0L, 0L, 1L);
        await Assert.That(collection[0]).IsEqualTo(source[0]);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Replace);

        recorder.Reset();
        collection.CopyFrom(source, 0L, 0L, Math.Min(2L, collection.Count));
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);

        collection.CopyFrom(source, 0L, 0L, 0L);

        await Assert.That(() => collection.CopyFrom((IReadOnlyLargeArray<long>)null!, 0L, 0L, 1L)).Throws<Exception>();
    }

    [Test]
    public async Task CopyFrom_IReadOnlyLargeArray_CountZero_NoEvents()
    {
        LargeArray<long> source = CreateSequentialArray(3L, 600L);
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(3L, 700L);
        using EventRecorder<long> recorder = new(collection);

        collection.CopyFrom(source, 0L, 0L, 0L);

        await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
        await Assert.That(recorder.PropertyEvents.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CopyFrom_IReadOnlyLargeArray_InvalidRangesThrow()
    {
        LargeArray<long> source = CreateSequentialArray(3L, 800L);
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(3L, 900L);

        await Assert.That(() => collection.CopyFrom(source, -1L, 0L, 1L)).Throws<Exception>();
        await Assert.That(() => collection.CopyFrom(source, 0L, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => collection.CopyFrom(source, 0L, 0L, source.Count + 1L)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyFrom_ReadOnlyLargeSpan_TriggersExpectedEvents(long capacity)
    {
        LargeArray<long> backing = CreateSequentialArray(Math.Max(2L, capacity + 1L), 2000L);
        ReadOnlyLargeSpan<long> source = new(backing, 0L, backing.Count);
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(Math.Max(2L, capacity + 1L), 3000L);
        using EventRecorder<long> recorder = new(collection);

        collection.CopyFrom(source, 0L, 1L);
        await Assert.That(collection[0]).IsEqualTo(backing[0]);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Replace);

        recorder.Reset();
        collection.CopyFrom(source, 0L, Math.Min(2L, collection.Count));
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task CopyFrom_ReadOnlyLargeSpan_CountZero_NoEvents()
    {
        LargeArray<long> backing = CreateSequentialArray(3L, 3100L);
        ReadOnlyLargeSpan<long> source = new(backing, 0L, 0L);
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(3L, 3200L);
        using EventRecorder<long> recorder = new(collection);

        collection.CopyFrom(source, 0L, 0L);

        await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyFromArray_TriggersExpectedEvents(long capacity)
    {
        long length = Math.Max(2L, Math.Min(capacity + 1L, Constants.MaxLargeCollectionCount));
        long[] source = Enumerable.Range(0, (int)length).Select(i => (long)i + 9000L).ToArray();
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(length, 10000L);
        using EventRecorder<long> recorder = new(collection);

        collection.CopyFromArray(source, 0, 0L, 1);
        await Assert.That(collection[0]).IsEqualTo(source[0]);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Replace);

        recorder.Reset();
        collection.CopyFromArray(source, 0, 0L, Math.Min(2, source.Length));
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);

        await Assert.That(() => collection.CopyFromArray((long[])null!, 0, 0L, 1)).Throws<Exception>();
        await Assert.That(() => collection.CopyFromArray(source, -1, 0L, 1)).Throws<Exception>();
        await Assert.That(() => collection.CopyFromArray(source, 0, -1L, 1)).Throws<Exception>();
        await Assert.That(() => collection.CopyFromArray(source, 0, 0L, source.Length + 1)).Throws<Exception>();
    }

    [Test]
    public async Task CopyFromArray_CountZero_NoEvents()
    {
        long[] source = new long[] { 1, 2, 3 };
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(3L, 15000L);
        using EventRecorder<long> recorder = new(collection);

        collection.CopyFromArray(source, 0, 0L, 0);

        await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyFromSpan_TriggersExpectedEvents(long capacity)
    {
        long length = Math.Max(2L, Math.Min(capacity + 1L, Constants.MaxLargeCollectionCount));
        long[] source = Enumerable.Range(0, (int)length).Select(i => (long)i + 12000L).ToArray();
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(length, 13000L);
        using EventRecorder<long> recorder = new(collection);

        collection.CopyFromSpan(source.AsSpan(), 0L, 1);
        await Assert.That(collection[0]).IsEqualTo(source[0]);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Replace);

        recorder.Reset();
        collection.CopyFromSpan(source.AsSpan(), 0L, Math.Min(2, source.Length));
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task CopyFromSpan_CountZero_NoEvents()
    {
        long[] source = new long[] { 1, 2, 3 };
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(3L, 14000L);
        using EventRecorder<long> recorder = new(collection);

        collection.CopyFromSpan(source.AsSpan(), 0L, 0);

        await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CopyFromSpan_InvalidRangesThrow()
    {
        long[] source = new long[] { 1, 2, 3 };
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(3L, 14500L);

        await Assert.That(() => collection.CopyFromSpan(source.AsSpan(), -1L, 1)).Throws<Exception>();
        await Assert.That(() => collection.CopyFromSpan(source.AsSpan(), 0L, source.Length + 1)).Throws<Exception>();
        await Assert.That(() => collection.CopyFromSpan(source.AsSpan(), 0L, -1)).Throws<Exception>();
    }
#endif

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task CopyTo_Variants_CopyData(long capacity)
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(Math.Max(3L, capacity + 1L), 400L);
        LargeArray<long> targetArray = CreateSequentialArray(collection.Count + 2L, 0L);
        collection.CopyTo(targetArray, 0L, 1L, Math.Min(2L, collection.Count));
        await Assert.That(targetArray[1]).IsEqualTo(collection[0]);

        LargeArray<long> spanBacking = CreateSequentialArray(collection.Count + 1L, 0L);
        LargeSpan<long> spanTarget = new(spanBacking);
        collection.CopyTo(spanTarget, 0L, Math.Min(2L, collection.Count));
        await Assert.That(spanBacking[0]).IsEqualTo(collection[0]);

        long[] raw = new long[Math.Max(1, (int)Math.Min(2L, collection.Count))];
        collection.CopyToArray(raw, 0L, 0, raw.Length);
        await Assert.That(raw[0]).IsEqualTo(collection[0]);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        Span<long> writable = raw.AsSpan();
        collection.CopyToSpan(writable, 0L, writable.Length);
        await Assert.That(writable[0]).IsEqualTo(collection[0]);
#endif
    }

    [Test]
    public async Task CopyTo_InvalidParameters_Throw()
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(2L, 500L);
        LargeArray<long> arrayTarget = CreateSequentialArray(5L, 0L);

        await Assert.That(() => collection.CopyTo(arrayTarget, -1L, 0L, 1L)).Throws<Exception>();
        await Assert.That(() => collection.CopyTo(arrayTarget, 0L, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => collection.CopyTo(arrayTarget, 0L, 0L, arrayTarget.Count + 1L)).Throws<Exception>();
        await Assert.That(() => collection.CopyTo(arrayTarget, 0L, 0L, -1L)).Throws<Exception>();

        long[] raw = new long[5];
        await Assert.That(() => collection.CopyToArray(raw, -1L, 0, 1)).Throws<Exception>();
        await Assert.That(() => collection.CopyToArray(raw, 0L, -1, 1)).Throws<Exception>();
        await Assert.That(() => collection.CopyToArray(raw, 0L, 0, raw.Length + 1)).Throws<Exception>();

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        await Assert.That(() => collection.CopyToSpan(raw.AsSpan(), -1L, 1)).Throws<Exception>();
        await Assert.That(() => collection.CopyToSpan(raw.AsSpan(), 0L, raw.Length + 1)).Throws<Exception>();
        await Assert.That(() => collection.CopyToSpan(raw.AsSpan(), 0L, -1)).Throws<Exception>();
#endif

        LargeSpan<long> spanTarget = new(CreateSequentialArray(5L, 0L));
        await Assert.That(() => collection.CopyTo(spanTarget, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => collection.CopyTo(spanTarget, 0L, spanTarget.Count + 1L)).Throws<Exception>();
    }

    #endregion

    #region Lookup and Enumeration

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task BinarySearch_And_IndexLookups_Work(long capacity)
    {
        // Use a sequence for BinarySearch tests (needs sorted unique values)
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(capacity, 100L);
        Func<long, long, int> comparer = static (l, r) => l.CompareTo(r);

        long value = collection.Count > 0 ? collection[0] : 0L;

        await Assert.That(collection.BinarySearch(value, comparer)).IsEqualTo(collection.Count > 0 ? 0L : -1L);
        await Assert.That(collection.BinarySearch(value, comparer, 0L, collection.Count)).IsEqualTo(collection.Count > 0 ? 0L : -1L);

        await Assert.That(collection.Contains(value)).IsEqualTo(collection.Count > 0);
        await Assert.That(collection.Contains(value, static (l, r) => l == r)).IsEqualTo(collection.Count > 0);
        await Assert.That(collection.Contains(value, 0L, collection.Count)).IsEqualTo(collection.Count > 0);
        await Assert.That(collection.Contains(value, 0L, collection.Count, static (l, r) => l == r)).IsEqualTo(collection.Count > 0);

        await Assert.That(collection.IndexOf(value)).IsEqualTo(collection.Count > 0 ? 0L : -1L);
        await Assert.That(collection.IndexOf(value, 0L, collection.Count)).IsEqualTo(collection.Count > 0 ? 0L : -1L);
        await Assert.That(collection.IndexOf(value, static (l, r) => l == r)).IsEqualTo(collection.Count > 0 ? 0L : -1L);
        await Assert.That(collection.IndexOf(value, 0L, collection.Count, static (l, r) => l == r)).IsEqualTo(collection.Count > 0 ? 0L : -1L);

        // LastIndexOf on a sequence should return 0 (value is only at index 0)
        await Assert.That(collection.LastIndexOf(value)).IsEqualTo(collection.Count > 0 ? 0L : -1L);
        await Assert.That(collection.LastIndexOf(value, 0L, collection.Count)).IsEqualTo(collection.Count > 0 ? 0L : -1L);
        await Assert.That(collection.LastIndexOf(value, static (l, r) => l == r)).IsEqualTo(collection.Count > 0 ? 0L : -1L);
        await Assert.That(collection.LastIndexOf(value, 0L, collection.Count, static (l, r) => l == r)).IsEqualTo(collection.Count > 0 ? 0L : -1L);
        
        // Test LastIndexOf with duplicate values
        if (collection.Count > 0)
        {
            LargeObservableCollection<long> collectionWithDuplicates = CreateCollectionWithSameValue(capacity, 100L);
            long duplicateValue = 100L;
            await Assert.That(collectionWithDuplicates.LastIndexOf(duplicateValue)).IsEqualTo(collectionWithDuplicates.Count - 1);
            await Assert.That(collectionWithDuplicates.LastIndexOf(duplicateValue, 0L, collectionWithDuplicates.Count)).IsEqualTo(collectionWithDuplicates.Count - 1);
            await Assert.That(collectionWithDuplicates.LastIndexOf(duplicateValue, static (l, r) => l == r)).IsEqualTo(collectionWithDuplicates.Count - 1);
            await Assert.That(collectionWithDuplicates.LastIndexOf(duplicateValue, 0L, collectionWithDuplicates.Count, static (l, r) => l == r)).IsEqualTo(collectionWithDuplicates.Count - 1);
        }
    }

    [Test]
    public async Task Lookup_InvalidRanges_Throw()
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(2L, 0L);
        Func<long, long, int> comparer = static (l, r) => l.CompareTo(r);

        await Assert.That(() => collection.BinarySearch(0L, comparer, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => collection.Contains(0L, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => collection.Contains(0L, -1L, 1L, static (l, r) => l == r)).Throws<Exception>();
        await Assert.That(() => collection.IndexOf(0L, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => collection.IndexOf(0L, -1L, 1L, static (l, r) => l == r)).Throws<Exception>();
        await Assert.That(() => collection.LastIndexOf(0L, -1L, 1L)).Throws<Exception>();
        await Assert.That(() => collection.LastIndexOf(0L, -1L, 1L, static (l, r) => l == r)).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Get_GetAll_And_Enumerator_ReturnElements(long capacity)
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(capacity, 5000L);

        if (collection.Count > 0)
        {
            await Assert.That(collection.Get(0L)).IsEqualTo(collection[0]);
        }
        else
        {
            await Assert.That(() => collection.Get(0L)).Throws<Exception>();
        }

        await Assert.That(collection.GetAll().SequenceEqual(collection.ToList())).IsTrue();
        long offset = Math.Min(1L, Math.Max(0L, collection.Count - 1L));
        long count = collection.Count - offset;
        await Assert.That(collection.GetAll(offset, count).SequenceEqual(collection.Skip((int)offset))).IsTrue();

        List<long> enumerated = new();
        foreach (long item in collection)
        {
            enumerated.Add(item);
        }
        await Assert.That(enumerated.SequenceEqual(collection.ToList())).IsTrue();

        IEnumerator enumerator = ((IEnumerable)collection).GetEnumerator();
        List<long> enumeratedNonGeneric = new();
        while (enumerator.MoveNext())
        {
            enumeratedNonGeneric.Add((long)enumerator.Current!);
        }
        await Assert.That(enumeratedNonGeneric.SequenceEqual(collection.ToList())).IsTrue();
    }

    [Test]
    public async Task GetAll_InvalidRange_Throws()
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(3L, 8000L);
        await Assert.That(() => collection.GetAll(-1L, 1L).ToList()).Throws<Exception>();
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task DoForEach_Variants_Execute(long capacity)
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(capacity, 7000L);
        long sum = 0L;
        collection.DoForEach(item => sum += item);
        await Assert.That(sum).IsEqualTo(collection.ToList().Sum());

        long offset = Math.Min(1L, Math.Max(0L, collection.Count - 1L));
        long rangeCount = collection.Count - offset;
        long rangeSum = 0L;
        collection.DoForEach(item => rangeSum += item, offset, rangeCount);
        await Assert.That(rangeSum).IsEqualTo(collection.Skip((int)offset).Sum());

        long accumulator = 0L;
        collection.DoForEach(static (long value, ref long acc) => acc += value, ref accumulator);
        await Assert.That(accumulator).IsEqualTo(collection.ToList().Sum());

        long rangeAccumulator = 0L;
        collection.DoForEach(static (long value, ref long acc) => acc += value, offset, rangeCount, ref rangeAccumulator);
        await Assert.That(rangeAccumulator).IsEqualTo(collection.Skip((int)offset).Sum());
    }

    [Test]
    public async Task DoForEach_InvalidRangesThrow()
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(3L, 0L);
        await Assert.That(() => collection.DoForEach(_ => { }, -1L, 1L)).Throws<Exception>();
        long data = 0L;
        await Assert.That(() => collection.DoForEach(static (long _, ref long __) => { }, -1L, 1L, ref data)).Throws<Exception>();
    }

    #endregion

    #region Ordering

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Sort_RaisesReset(long capacity)
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(capacity, 0L);
        using EventRecorder<long> recorder = new(collection);

        collection.Sort(static (l, r) => r.CompareTo(l));

        if (collection.Count > 1)
        {
            await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
            List<long> sorted = collection.ToList();
            await Assert.That(sorted.SequenceEqual(sorted.OrderByDescending(x => x))).IsTrue();
        }
        else
        {
            await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
        }

        recorder.Reset();
        long offset = Math.Min(1L, Math.Max(0L, collection.Count - 2L));
        long count = Math.Min(2L, collection.Count - offset);
        collection.Sort(static (l, r) => l.CompareTo(r), offset, count);
        if (count > 1)
        {
            await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
        }
    }

    [Test]
    [MethodDataSource(nameof(Capacities))]
    public async Task Swap_RaisesReset(long capacity)
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(capacity, 100L);
        if (collection.Count < 2)
        {
            await Assert.That(() => collection.Swap(0L, 1L)).Throws<Exception>();
            return;
        }

        using EventRecorder<long> recorder = new(collection);
        long left = collection[0];
        long right = collection[1];
        collection.Swap(0L, 1L);

        await Assert.That(collection[0]).IsEqualTo(right);
        await Assert.That(collection[1]).IsEqualTo(left);
        await Assert.That(recorder.CollectionEvents.Last().Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    #endregion

    #region Suspension and ReadOnly

    [Test]
    public async Task SuspendNotifications_BatchesChanges()
    {
        LargeObservableCollection<long> collection = new();
        using EventRecorder<long> recorder = new(collection);

        using (collection.SuspendNotifications())
        {
            collection.Add(1L);
            collection.Add(2L);
        }

        await Assert.That(recorder.CollectionEvents.Count(e => e.Action == NotifyCollectionChangedAction.Reset)).IsEqualTo(1);
        await Assert.That(recorder.PropertyEvents.Count(e => e.PropertyName == nameof(collection.Count))).IsEqualTo(1);
    }

    [Test]
    public async Task SuspendNotifications_NoChanges_NoEvents()
    {
        LargeObservableCollection<long> collection = new();
        using EventRecorder<long> recorder = new(collection);

        using (collection.SuspendNotifications())
        {
        }

        await Assert.That(recorder.CollectionEvents.Count).IsEqualTo(0);
        await Assert.That(recorder.PropertyEvents.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SuspendNotifications_NoCountChange_SuppressesProperty()
    {
        LargeObservableCollection<long> collection = CreateCollectionWithSequence(3L, 10L);
        using EventRecorder<long> recorder = new(collection);

        using (collection.SuspendNotifications())
        {
            collection[0] = 99L;
        }

        await Assert.That(recorder.CollectionEvents.Count(e => e.Action == NotifyCollectionChangedAction.Reset)).IsEqualTo(1);
        await Assert.That(recorder.PropertyEvents.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AsReadOnly_ReflectsChanges()
    {
        LargeObservableCollection<long> collection = new();
        ReadOnlyLargeObservableCollection<long> readOnly = collection.AsReadOnly();

        collection.Add(5L);

        await Assert.That(readOnly.Count).IsEqualTo(1L);
        await Assert.That(readOnly[0]).IsEqualTo(5L);
    }

    #endregion

    #region Helpers

    private static LargeObservableCollection<long> CreateCollectionWithSequence(long count, long start)
    {
        long actual = Math.Max(0L, Math.Min(count, Constants.MaxLargeCollectionCount));
        LargeObservableCollection<long> collection = new(actual);
        for (long i = 0L; i < actual; i++)
        {
            collection.Add(start + i);
        }
        return collection;
    }

    private static LargeObservableCollection<long> CreateCollectionWithSameValue(long count, long value)
    {
        long actual = Math.Max(0L, Math.Min(count, Constants.MaxLargeCollectionCount));
        LargeObservableCollection<long> collection = new(actual);
        for (long i = 0L; i < actual; i++)
        {
            collection.Add(value);
        }
        return collection;
    }

    private static LargeArray<long> CreateSequentialArray(long count, long start = 0L)
    {
        long actual = Math.Max(0L, Math.Min(count, Constants.MaxLargeCollectionCount));
        LargeArray<long> array = new(actual);
        for (long i = 0L; i < actual; i++)
        {
            array[i] = start + i;
        }
        return array;
    }

    private sealed class EventRecorder<T> : IDisposable
    {
        private readonly LargeObservableCollection<T> _collection;

        public List<NotifyCollectionChangedEventArgs> CollectionEvents { get; } = new();
        public List<PropertyChangedEventArgs> PropertyEvents { get; } = new();

        public EventRecorder(LargeObservableCollection<T> collection)
        {
            _collection = collection;
            _collection.CollectionChanged += OnCollectionChanged;
            _collection.PropertyChanged += OnPropertyChanged;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionEvents.Add(e);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyEvents.Add(e);
        }

        public void Reset()
        {
            CollectionEvents.Clear();
            PropertyEvents.Clear();
        }

        public void Dispose()
        {
            _collection.CollectionChanged -= OnCollectionChanged;
            _collection.PropertyChanged -= OnPropertyChanged;
        }
    }

    private static IEnumerable<long> GenerateSequence(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return i;
        }
    }

    #endregion
}

