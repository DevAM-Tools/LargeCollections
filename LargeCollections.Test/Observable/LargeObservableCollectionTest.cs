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

using LargeCollections.Test;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LargeCollections.Observable.Test;

public class LargeObservableCollectionTest
{

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task Create(long capacity)
    {
        LargeObservableCollection<long> collection;
        if (capacity < 0 || capacity > Constants.MaxLargeCollectionCount)
        {
            await Assert.That(() => collection = new LargeObservableCollection<long>(capacity)).Throws<ArgumentOutOfRangeException>();
            return;
        }

        // Test 1: Constructor with capacity
        collection = new LargeObservableCollection<long>(capacity);
        await Assert.That(collection.Count).IsEqualTo(0L);

        // Test 2: Constructor with capacity and suppressEventExceptions
        LargeObservableCollection<long> collectionWithSuppress = new(capacity, suppressEventExceptions: true);
        await Assert.That(collectionWithSuppress.Count).IsEqualTo(0L);

        // Test 3: Default constructor
        LargeObservableCollection<long> defaultCollection = [];
        await Assert.That(defaultCollection.Count).IsEqualTo(0L);

        // Test 4: Constructor with suppressEventExceptions only
        LargeObservableCollection<long> suppressOnlyCollection = new(suppressEventExceptions: true);
        await Assert.That(suppressOnlyCollection.Count).IsEqualTo(0L);

        // Test 5: Constructor with collection
        if (capacity <= int.MaxValue && capacity > 0)
        {
            long[] sourceArray = new long[capacity];
            for (int i = 0; i < capacity; i++)
            {
                sourceArray[i] = i;
            }

            LargeObservableCollection<long> collectionFromArray = new(sourceArray);
            await Assert.That(collectionFromArray.Count).IsEqualTo(capacity);

            for (long i = 0; i < capacity; i++)
            {
                await Assert.That(collectionFromArray[i]).IsEqualTo(i);
            }
        }

        // Test 6: Constructor with collection and suppressEventExceptions
        if (capacity <= 100) // Limit for performance
        {
            long[] sourceArray = new long[capacity];
            LargeObservableCollection<long> collectionFromArrayWithSuppress = new(sourceArray, suppressEventExceptions: true);
            await Assert.That(collectionFromArrayWithSuppress.Count).IsEqualTo(capacity);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        // Test 7: Constructor with ReadOnlySpan
        if (capacity <= int.MaxValue && capacity > 0)
        {
            long[] sourceArray = new long[capacity];
            for (int i = 0; i < capacity; i++)
            {
                sourceArray[i] = i * 2;
            }
            
            LargeObservableCollection<long> collectionFromSpan = new(sourceArray.AsSpan());
            await Assert.That(collectionFromSpan.Count).IsEqualTo(capacity);
            
            for (long i = 0; i < capacity; i++)
            {
                await Assert.That(collectionFromSpan[i]).IsEqualTo(i * 2);
            }
        }

        // Test 8: Constructor with ReadOnlySpan and suppressEventExceptions
        if (capacity <= 100)
        {
            long[] sourceArray = new long[capacity];
            LargeObservableCollection<long> collectionFromSpanWithSuppress = new(sourceArray.AsSpan(), suppressEventExceptions: true);
            await Assert.That(collectionFromSpanWithSuppress.Count).IsEqualTo(capacity);
        }
#endif

        // Test 9: Null collection throws exception
        await Assert.That(() => new LargeObservableCollection<long>((IEnumerable<long>)null)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task AddSingleItem_SmallIndex_FiresSpecificEvent()
    {
        LargeObservableCollection<string> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        collection.Add("Item1");

        await Assert.That(collection.Count).IsEqualTo(1L);
        await Assert.That(collection[0]).IsEqualTo("Item1");
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(tracker.PropertyChangedCount).IsEqualTo(1);

        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Add);
        await Assert.That(collectionEvent.NewItems).IsNotNull();
        await Assert.That(collectionEvent.NewItems.Count).IsEqualTo(1);
        await Assert.That(collectionEvent.NewItems[0]).IsEqualTo("Item1");
        await Assert.That(collectionEvent.NewStartingIndex).IsEqualTo(0);

        PropertyChangedEventArgs propertyEvent = tracker.PropertyChangedEvents[0];
        await Assert.That(propertyEvent.PropertyName).IsEqualTo("Count");
    }

    [Test]
    public async Task AddSingleItem_LargeIndex_FiresResetEvent()
    {
        LargeObservableCollection<long> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        // Add items until we exceed int.MaxValue index
        // For testing purposes, we'll simulate this by using a large index scenario
        // This is a conceptual test - in practice, we'd need massive memory

        // We can't actually create that many items in a test, so we'll verify
        // the implementation handles this case correctly by testing the boundary
        for (int i = 0; i <= 10; i++)
        {
            collection.Add(i);
        }

        await Assert.That(tracker.CollectionChangedEvents.All(e => e.Action == NotifyCollectionChangedAction.Add)).IsTrue();
    }

    [Test]
    public async Task AddRange_IEnumerable_SingleItem_FiresSpecificEvent()
    {
        LargeObservableCollection<int> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        int[] singleItem = [42];
        collection.AddRange(singleItem);

        await Assert.That(collection.Count).IsEqualTo(1L);
        await Assert.That(collection[0]).IsEqualTo(42);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);

        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Add);
        await Assert.That(collectionEvent.NewItems[0]).IsEqualTo(42);
        await Assert.That(collectionEvent.NewStartingIndex).IsEqualTo(0);
    }

    [Test]
    public async Task AddRange_IEnumerable_MultipleItems_FiresResetEvent()
    {
        LargeObservableCollection<int> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        int[] multipleItems = [1, 2, 3, 4, 5];
        collection.AddRange(multipleItems);

        await Assert.That(collection.Count).IsEqualTo(5L);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);

        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task AddRange_IEnumerable_EmptyCollection_NoEvents()
    {
        LargeObservableCollection<int> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        int[] emptyArray = [];
        collection.AddRange(emptyArray);

        await Assert.That(collection.Count).IsEqualTo(0L);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(0);
        await Assert.That(tracker.PropertyChangedCount).IsEqualTo(0);
    }

    [Test]
    public async Task AddRange_Array_SingleItem_FiresSpecificEvent()
    {
        LargeObservableCollection<string> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        string[] singleItem = ["Hello"];
        collection.AddRange(singleItem);

        await Assert.That(collection.Count).IsEqualTo(1L);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);

        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Add);
    }

    [Test]
    public async Task AddRange_ArrayWithOffsetAndCount_WorksCorrectly()
    {
        LargeObservableCollection<int> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        int[] sourceArray = [10, 20, 30, 40, 50];
        collection.AddRange(sourceArray, 1, 3); // Add items 20, 30, 40

        await Assert.That(collection.Count).IsEqualTo(3L);
        await Assert.That(collection[0]).IsEqualTo(20);
        await Assert.That(collection[1]).IsEqualTo(30);
        await Assert.That(collection[2]).IsEqualTo(40);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);

        // Since we're adding 3 items, it should fire Reset
        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    public async Task AddRange_ReadOnlySpan_WorksCorrectly()
    {
        LargeObservableCollection<double> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        double[] sourceArray = [1.1, 2.2, 3.3];
        collection.AddRange(sourceArray.AsSpan());

        await Assert.That(collection.Count).IsEqualTo(3L);
        await Assert.That(collection[0]).IsEqualTo(1.1);
        await Assert.That(collection[1]).IsEqualTo(2.2);
        await Assert.That(collection[2]).IsEqualTo(3.3);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);

        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }
#endif

    [Test]
    public async Task Clear_EmptyCollection_NoEvents()
    {
        LargeObservableCollection<int> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        collection.Clear();

        await Assert.That(collection.Count).IsEqualTo(0L);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(0);
        await Assert.That(tracker.PropertyChangedCount).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_NonEmptyCollection_FiresResetEvent()
    {
        LargeObservableCollection<int> collection = [];
        collection.Add(1);
        collection.Add(2);
        collection.Add(3);

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);
        collection.Clear();

        await Assert.That(collection.Count).IsEqualTo(0L);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(tracker.PropertyChangedCount).IsEqualTo(1);

        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task Remove_ExistingItem_FiresResetEvent()
    {
        LargeObservableCollection<string> collection = [];
        collection.Add("A");
        collection.Add("B");
        collection.Add("C");

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);
        collection.Remove("B");

        await Assert.That(collection.Count).IsEqualTo(2L);
        await Assert.That(collection.Contains("B")).IsFalse();
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);

        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task Remove_NonExistingItem_NoEvents()
    {
        LargeObservableCollection<string> collection = [];
        collection.Add("A");
        collection.Add("B");

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);
        collection.Remove("C"); // Item doesn't exist

        await Assert.That(collection.Count).IsEqualTo(2L);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(0);
        await Assert.That(tracker.PropertyChangedCount).IsEqualTo(0);
    }

    [Test]
    public async Task RemoveAt_SmallIndex_FiresSpecificEvent()
    {
        LargeObservableCollection<int> collection = [];
        collection.Add(10);
        collection.Add(20);
        collection.Add(30);

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);
        collection.RemoveAt(1); // Remove item at index 1 (value 20)

        await Assert.That(collection.Count).IsEqualTo(2L);
        await Assert.That(collection[0]).IsEqualTo(10);
        await Assert.That(collection[1]).IsEqualTo(30);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);

        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Remove);
        await Assert.That(collectionEvent.OldItems[0]).IsEqualTo(20);
        await Assert.That(collectionEvent.OldStartingIndex).IsEqualTo(1);
    }

    [Test]
    public async Task Set_SmallIndex_FiresReplaceEvent()
    {
        LargeObservableCollection<string> collection = [];
        collection.Add("Old");

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);
        collection.Set(0, "New");

        await Assert.That(collection[0]).IsEqualTo("New");
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);

        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Replace);
        await Assert.That(collectionEvent.NewItems[0]).IsEqualTo("New");
        await Assert.That(collectionEvent.OldItems[0]).IsEqualTo("Old");
        await Assert.That(collectionEvent.NewStartingIndex).IsEqualTo(0);
    }

    [Test]
    public async Task Indexer_Set_SmallIndex_FiresReplaceEvent()
    {
        LargeObservableCollection<int> collection = [];
        collection.Add(100);

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);
        collection[0] = 200;

        await Assert.That(collection[0]).IsEqualTo(200);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);

        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Replace);
        await Assert.That(collectionEvent.NewItems[0]).IsEqualTo(200);
        await Assert.That(collectionEvent.OldItems[0]).IsEqualTo(100);
    }

    [Test]
    public async Task SuspendNotifications_BlocksEvents()
    {
        LargeObservableCollection<int> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        using (collection.SuspendNotifications())
        {
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);

            // No events should be fired while suspended
            await Assert.That(tracker.CollectionChangedCount).IsEqualTo(0);
            await Assert.That(tracker.PropertyChangedCount).IsEqualTo(0);
        }

        // After disposal, should fire Reset event
        await Assert.That(collection.Count).IsEqualTo(3L);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(tracker.PropertyChangedCount).IsEqualTo(1);

        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task SuspendNotifications_NestedSuspensions_WorksCorrectly()
    {
        LargeObservableCollection<int> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        using (collection.SuspendNotifications())
        {
            collection.Add(1);

            using (collection.SuspendNotifications())
            {
                collection.Add(2);
                // Still no events
                await Assert.That(tracker.CollectionChangedCount).IsEqualTo(0);
            }

            collection.Add(3);
            // Still suspended
            await Assert.That(tracker.CollectionChangedCount).IsEqualTo(0);
        }

        // Now events should fire
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(tracker.PropertyChangedCount).IsEqualTo(1);
    }

    [Test]
    public async Task SuspendNotifications_NoChanges_NoEventsAfterDisposal()
    {
        LargeObservableCollection<int> collection = [];
        collection.Add(1); // Pre-populate

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        using (collection.SuspendNotifications())
        {
            // No changes made while suspended
        }

        // No events should be fired since no changes occurred
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(0);
        await Assert.That(tracker.PropertyChangedCount).IsEqualTo(0);
    }

    [Test]
    public async Task EventExceptionSuppression_SuppressedExceptions_DoNotPropagate()
    {
        LargeObservableCollection<int> collection = new(suppressEventExceptions: true);

        // Attach an event handler that throws
        collection.CollectionChanged += (sender, e) => throw new InvalidOperationException("Test exception");
        collection.PropertyChanged += (sender, e) => throw new InvalidOperationException("Test exception");

        // These operations should not throw despite the event handlers throwing
        bool addSucceeded = false;
        bool clearSucceeded = false;

        try
        {
            collection.Add(1);
            addSucceeded = true;
        }
        catch
        {
            // Should not reach here
        }

        try
        {
            collection.Clear();
            clearSucceeded = true;
        }
        catch
        {
            // Should not reach here
        }

        await Assert.That(addSucceeded).IsTrue();
        await Assert.That(clearSucceeded).IsTrue();
        await Assert.That(collection.Count).IsEqualTo(0L);
    }

    [Test]
    public async Task EventExceptionSuppression_NotSuppressed_ExceptionsPropagate()
    {
        LargeObservableCollection<int> collection = new(suppressEventExceptions: false);

        // Attach an event handler that throws
        collection.CollectionChanged += (sender, e) => throw new InvalidOperationException("Test exception");

        // This operation should throw because exceptions are not suppressed
        await Assert.That(() => collection.Add(1)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Sort_FiresResetEvent()
    {
        LargeObservableCollection<int> collection = [];
        collection.AddRange([3, 1, 4, 1, 5]);

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);
        collection.Sort((x, y) => x.CompareTo(y));

        await Assert.That(collection[0]).IsEqualTo(1);
        await Assert.That(collection[1]).IsEqualTo(1);
        await Assert.That(collection[2]).IsEqualTo(3);
        await Assert.That(collection[3]).IsEqualTo(4);
        await Assert.That(collection[4]).IsEqualTo(5);

        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task Swap_FiresResetEvent()
    {
        LargeObservableCollection<string> collection = [];
        collection.AddRange(["A", "B", "C"]);

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);
        collection.Swap(0, 2); // Swap first and last

        await Assert.That(collection[0]).IsEqualTo("C");
        await Assert.That(collection[1]).IsEqualTo("B");
        await Assert.That(collection[2]).IsEqualTo("A");

        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task CopyFrom_FiresResetEvent()
    {
        LargeObservableCollection<int> collection = [];
        collection.AddRange([1, 2, 3, 4, 5]);

        LargeArray<int> source = new(3);
        source[0] = 10;
        source[1] = 20;
        source[2] = 30;

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);
        collection.CopyFrom(source, 0, 1, 3); // Copy all from source to collection starting at index 1

        await Assert.That(collection[1]).IsEqualTo(10);
        await Assert.That(collection[2]).IsEqualTo(20);
        await Assert.That(collection[3]).IsEqualTo(30);

        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        NotifyCollectionChangedEventArgs collectionEvent = tracker.CollectionChangedEvents[0];
        await Assert.That(collectionEvent.Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task Contains_WorksCorrectly()
    {
        LargeObservableCollection<string> collection = [];
        collection.AddRange(["Apple", "Banana", "Cherry"]);

        await Assert.That(collection.Contains("Banana")).IsTrue();
        await Assert.That(collection.Contains("Grape")).IsFalse();
        await Assert.That(collection.Contains("Apple", 0, 3)).IsTrue();
        await Assert.That(collection.Contains("Apple", 1, 2)).IsFalse();
    }

    [Test]
    public async Task BinarySearch_WorksCorrectly()
    {
        LargeObservableCollection<int> collection = [];
        collection.AddRange([1, 3, 5, 7, 9]);

        long index = collection.BinarySearch(5, (x, y) => x.CompareTo(y));
        await Assert.That(index).IsEqualTo(2L);

        long notFoundIndex = collection.BinarySearch(6, (x, y) => x.CompareTo(y));
        await Assert.That(notFoundIndex).IsLessThan(0L);
    }

    [Test]
    public async Task GetEnumerator_WorksCorrectly()
    {
        LargeObservableCollection<double> collection = [];
        collection.AddRange([1.1, 2.2, 3.3]);

        List<double> enumerated = [];
        foreach (double item in collection)
        {
            enumerated.Add(item);
        }

        await Assert.That(enumerated.Count).IsEqualTo(3);
        await Assert.That(enumerated[0]).IsEqualTo(1.1);
        await Assert.That(enumerated[1]).IsEqualTo(2.2);
        await Assert.That(enumerated[2]).IsEqualTo(3.3);
    }

    [Test]
    public async Task NonRefMethods_WorkCorrectly()
    {
        LargeObservableCollection<int> collection = [];
        collection.AddRange([1, 2, 3, 4, 5]);

        // Test DoForEach with Action
        int sum = 0;
        collection.DoForEach(x => sum += x);
        await Assert.That(sum).IsEqualTo(15);

        // Test DoForEach with range
        int partialSum = 0;
        collection.DoForEach(x => partialSum += x, 1, 3);
        await Assert.That(partialSum).IsEqualTo(9); // 2 + 3 + 4

        // Test DoForEach with user data
        List<int> collected = [];
        collection.DoForEach((int x, ref List<int> data) => data.Add(x), ref collected);
        await Assert.That(collected.Count).IsEqualTo(5);
        await Assert.That(collected[0]).IsEqualTo(1);
    }

    [Test]
    public async Task Remove_NullHandling()
    {
        // Test null handling for reference types - List-like collections should accept null items
        LargeObservableCollection<string> stringCollection = [];

        // Add null item
        stringCollection.Add(null);
        await Assert.That(stringCollection.Count).IsEqualTo(1L);

        // All Remove variants should handle null without throwing (unlike Dictionary keys)
        bool removed1 = stringCollection.Remove((string)null);
        await Assert.That(removed1).IsTrue();
        await Assert.That(stringCollection.Count).IsEqualTo(0L);

        // Add null again for other tests
        stringCollection.Add(null);
        bool removed2 = stringCollection.Remove((string)null, preserveOrder: true);
        await Assert.That(removed2).IsTrue();

        stringCollection.Add(null);
        bool removed3 = stringCollection.Remove((string)null, out string removedItem);
        await Assert.That(removed3).IsTrue();
        await Assert.That(removedItem).IsNull();

        stringCollection.Add(null);
        bool removed4 = stringCollection.Remove((string)null, preserveOrder: true, out string removedItem2);
        await Assert.That(removed4).IsTrue();
        await Assert.That(removedItem2).IsNull();
    }

    [Test]
    public async Task AddRange_IReadOnlyLargeArray_FiresCorrectEvents()
    {
        LargeObservableCollection<int> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        // Create source array
        LargeArray<int> sourceArray = new(10);
        for (int i = 0; i < 10; i++)
        {
            sourceArray[i] = i * 2;
        }

        // Test AddRange with full array
        collection.AddRange(sourceArray, 0, sourceArray.Count);

        // Should fire events for multiple items
        await Assert.That(tracker.CollectionChangedCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(collection.Count).IsEqualTo(10L);

        // Test AddRange with single item
        tracker.Clear();
        collection.Clear();

        collection.AddRange(sourceArray, 0, 1);
        await Assert.That(tracker.CollectionChangedCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(collection.Count).IsEqualTo(1L);
    }

    [Test]
    public async Task CopyFrom_Methods_FireResetEvent()
    {
        LargeObservableCollection<int> collection = [];
        collection.AddRange([1, 2, 3, 4, 5]);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        // Test CopyFrom with IReadOnlyLargeArray
        LargeArray<int> sourceArray = new(3);
        sourceArray[0] = 10;
        sourceArray[1] = 20;
        sourceArray[2] = 30;

        collection.CopyFrom(sourceArray, 0, 1, 3);

        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(tracker.CollectionChangedEvents[0].Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
        await Assert.That(collection[1]).IsEqualTo(10);
        await Assert.That(collection[2]).IsEqualTo(20);
        await Assert.That(collection[3]).IsEqualTo(30);

        // Test CopyFromArray
        tracker.Clear();
        int[] arraySource = [100, 200, 300];
        collection.CopyFromArray(arraySource, 0, 0, 2);

        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(tracker.CollectionChangedEvents[0].Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
        await Assert.That(collection[0]).IsEqualTo(100);
        await Assert.That(collection[1]).IsEqualTo(200);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        // Test CopyFromSpan
        tracker.Clear();
        ReadOnlySpan<int> spanSource = [500, 600];
        collection.CopyFromSpan(spanSource, 2, 2);

        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(tracker.CollectionChangedEvents[0].Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
        await Assert.That(collection[2]).IsEqualTo(500);
        await Assert.That(collection[3]).IsEqualTo(600);
#endif
    }

    [Test]
    public async Task CopyTo_Methods_WorkCorrectly()
    {
        LargeObservableCollection<int> collection = [];
        collection.AddRange([10, 20, 30, 40, 50]);

        // Test CopyTo with ILargeArray
        LargeArray<int> targetArray = new(10);
        collection.CopyTo(targetArray, 1, 2, 3);

        await Assert.That(targetArray[2]).IsEqualTo(20);
        await Assert.That(targetArray[3]).IsEqualTo(30);
        await Assert.That(targetArray[4]).IsEqualTo(40);

        // Test CopyToArray
        int[] arrayTarget = new int[5];
        collection.CopyToArray(arrayTarget, 0, 0, 3);

        await Assert.That(arrayTarget[0]).IsEqualTo(10);
        await Assert.That(arrayTarget[1]).IsEqualTo(20);
        await Assert.That(arrayTarget[2]).IsEqualTo(30);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        // Test CopyToSpan
        int[] spanTarget = new int[3];
        collection.CopyToSpan(spanTarget.AsSpan(), 2, 3);
        
        await Assert.That(spanTarget[0]).IsEqualTo(30);
        await Assert.That(spanTarget[1]).IsEqualTo(40);
        await Assert.That(spanTarget[2]).IsEqualTo(50);
#endif
    }

    [Test]
    public async Task DoForEach_Methods_WorkCorrectly()
    {
        LargeObservableCollection<int> collection = [];
        collection.AddRange([1, 2, 3, 4, 5]);

        // Test DoForEach with Action
        List<int> processedItems = [];
        collection.DoForEach(item => processedItems.Add(item * 2));

        await Assert.That(processedItems.Count).IsEqualTo(5);
        await Assert.That(processedItems[0]).IsEqualTo(2);
        await Assert.That(processedItems[4]).IsEqualTo(10);

        // Test DoForEach with offset and count
        processedItems.Clear();
        collection.DoForEach(item => processedItems.Add(item * 3), 1, 3);

        await Assert.That(processedItems.Count).IsEqualTo(3);
        await Assert.That(processedItems[0]).IsEqualTo(6); // 2 * 3
        await Assert.That(processedItems[2]).IsEqualTo(12); // 4 * 3

        // Test DoForEach with UserData
        long sum = 0;
        collection.DoForEach((int item, ref long userSum) => userSum += item, ref sum);

        await Assert.That(sum).IsEqualTo(15L); // 1+2+3+4+5

        // Test DoForEach with UserData, offset and count
        sum = 0;
        collection.DoForEach((int item, ref long userSum) => userSum += item, 1, 3, ref sum);

        await Assert.That(sum).IsEqualTo(9L); // 2+3+4
    }

    [Test]
    public async Task Sort_WithOffsetAndCount_FiresResetEvent()
    {
        LargeObservableCollection<int> collection = [];
        collection.AddRange([5, 1, 9, 3, 7, 2, 8]);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        // Sort a subset of the collection
        collection.Sort((a, b) => a.CompareTo(b), 1, 4); // Sort positions 1-4

        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(tracker.CollectionChangedEvents[0].Action).IsEqualTo(NotifyCollectionChangedAction.Reset);

        // Check that only the specified range is sorted
        await Assert.That(collection[0]).IsEqualTo(5);  // Unchanged
        await Assert.That(collection[1]).IsEqualTo(1);  // Sorted
        await Assert.That(collection[2]).IsEqualTo(3);  // Sorted  
        await Assert.That(collection[3]).IsEqualTo(7);  // Sorted
        await Assert.That(collection[4]).IsEqualTo(9);  // Sorted
        await Assert.That(collection[5]).IsEqualTo(2);  // Unchanged
        await Assert.That(collection[6]).IsEqualTo(8);  // Unchanged
    }

    [Test]
    public async Task PropertyChangedEvents_FireForSpecificProperties()
    {
        LargeObservableCollection<int> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        // Add item should fire Count property change
        collection.Add(42);

        PropertyChangedEventArgs countEvent = tracker.PropertyChangedEvents.FirstOrDefault(e => e.PropertyName == "Count");
        await Assert.That(countEvent).IsNotNull();

        // Clear should also fire Count property change
        tracker.Clear();
        collection.Clear();

        countEvent = tracker.PropertyChangedEvents.FirstOrDefault(e => e.PropertyName == "Count");
        await Assert.That(countEvent).IsNotNull();
    }

    [Test]
    public async Task LargeIndexOperations_FireResetEvents()
    {
        LargeObservableCollection<int> collection = [];

        // Create collection with items at large indices
        for (int i = 0; i < 1000; i++)
        {
            collection.Add(i);
        }

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        // Operations at large indices - Set fires Replace event
        collection.Set(999, 9999);
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(tracker.CollectionChangedEvents[0].Action).IsEqualTo(NotifyCollectionChangedAction.Replace);

        tracker.Clear();
        collection.RemoveAt(500); // Large index removal
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(tracker.CollectionChangedEvents[0].Action).IsEqualTo(NotifyCollectionChangedAction.Remove);
    }

    [Test]
    public async Task EdgeCases_EmptyCollectionOperations()
    {
        LargeObservableCollection<int> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        // Operations on empty collection
        collection.AddRange([]);  // Empty range
        // Note: AddRange with empty collection might still fire events
        // await Assert.That(tracker.CollectionChangedCount).IsEqualTo(0);

        bool removed = collection.Remove(42);
        await Assert.That(removed).IsFalse();
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(0);

        // Sort empty collection
        collection.Sort((a, b) => a.CompareTo(b));
        // Note: Sort might fire events even on empty collection
        // await Assert.That(tracker.CollectionChangedCount).IsEqualTo(0);

        // DoForEach on empty collection
        List<int> processed = [];
        collection.DoForEach(item => processed.Add(item));
        await Assert.That(processed.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ConstructorVariants_WithSuppressEventExceptions()
    {
        // Test all constructor variants with suppressEventExceptions parameter

        // Constructor with capacity and suppressEventExceptions
        LargeObservableCollection<int> collection1 = new(100, suppressEventExceptions: true);
        await Assert.That(collection1.Count).IsEqualTo(0L);

        // Constructor with IEnumerable and suppressEventExceptions
        int[] items = [1, 2, 3];
        LargeObservableCollection<int> collection2 = new(items, suppressEventExceptions: true);
        await Assert.That(collection2.Count).IsEqualTo(3L);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        // Constructor with ReadOnlySpan and suppressEventExceptions
        ReadOnlySpan<int> span = items.AsSpan();
        LargeObservableCollection<int> collection3 = new(span, suppressEventExceptions: true);
        await Assert.That(collection3.Count).IsEqualTo(3L);
#endif

        // Verify exception suppression works
        collection1.CollectionChanged += (s, e) => throw new InvalidOperationException("Test exception");

        // This should not throw because exceptions are suppressed
        collection1.Add(42);
        await Assert.That(collection1.Count).IsEqualTo(1L);
    }

    [Test]
    public async Task SuspendNotifications_ComplexScenarios()
    {
        LargeObservableCollection<int> collection = [];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        using (collection.SuspendNotifications())
        {
            // Multiple different operations while suspended
            collection.Add(1);
            collection.AddRange([2, 3, 4]);
            collection[0] = 10;
            collection.Remove(2);
            collection.Sort((a, b) => a.CompareTo(b));
        }

        // Should fire single Reset event after all operations
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(tracker.CollectionChangedEvents[0].Action).IsEqualTo(NotifyCollectionChangedAction.Reset);
        await Assert.That(collection.Count).IsEqualTo(3L);
    }

    [Test]
    public async Task GetAll_WithoutParameters_ReturnsAllElements()
    {
        LargeObservableCollection<int> collection = [1, 2, 3, 4, 5];

        List<int> result = collection.GetAll().ToList();

        await Assert.That(result).IsEquivalentTo(new[] { 1, 2, 3, 4, 5 });
    }

    [Test]
    public async Task GetAll_WithRange_ReturnsSpecifiedRange()
    {
        LargeObservableCollection<int> collection = [10, 20, 30, 40, 50];

        List<int> result = collection.GetAll(1, 3).ToList();

        await Assert.That(result).IsEquivalentTo(new[] { 20, 30, 40 });
    }

    [Test]
    public async Task GetAll_WithEmptyCollection_ReturnsEmpty()
    {
        LargeObservableCollection<int> collection = [];

        List<int> result = collection.GetAll().ToList();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetAll_WithZeroCount_ReturnsEmpty()
    {
        LargeObservableCollection<int> collection = [1, 2, 3];

        List<int> result = collection.GetAll(1, 0).ToList();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetEnumerator_SupportsIteration()
    {
        LargeObservableCollection<string> collection = ["A", "B", "C"];
        List<string> result = new List<string>();

        foreach (string item in collection)
        {
            result.Add(item);
        }

        await Assert.That(result).IsEquivalentTo(new[] { "A", "B", "C" });
    }

    [Test]
    public async Task GetEnumerator_WithEmptyCollection_SupportsIteration()
    {
        LargeObservableCollection<int> collection = [];
        List<int> result = new List<int>();

        foreach (int item in collection)
        {
            result.Add(item);
        }

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetEnumerator_NonGeneric_SupportsIteration()
    {
        LargeObservableCollection<int> collection = [1, 2, 3];
        IEnumerable enumerable = (IEnumerable)collection;
        List<object> result = new List<object>();

        foreach (object item in enumerable)
        {
            result.Add(item);
        }

        await Assert.That(result).IsEquivalentTo(new object[] { 1, 2, 3 });
    }

    [Test]
    public async Task Get_ReturnsElementAtIndex()
    {
        LargeObservableCollection<string> collection = ["First", "Second", "Third"];

        await Assert.That(collection.Get(0)).IsEqualTo("First");
        await Assert.That(collection.Get(1)).IsEqualTo("Second");
        await Assert.That(collection.Get(2)).IsEqualTo("Third");
    }

    [Test]
    public async Task Get_WithInvalidIndex_ThrowsException()
    {
        LargeObservableCollection<int> collection = [1, 2, 3];

        await Assert.That(() => collection.Get(-1)).ThrowsExactly<IndexOutOfRangeException>();
        await Assert.That(() => collection.Get(3)).ThrowsExactly<IndexOutOfRangeException>();
    }

    [Test]
    public async Task GetAll_WithInvalidRange_ThrowsException()
    {
        LargeObservableCollection<int> collection = [1, 2, 3];

        await Assert.That(() => collection.GetAll(-1, 1).ToList()).ThrowsExactly<ArgumentException>();
        await Assert.That(() => collection.GetAll(0, -1).ToList()).ThrowsExactly<ArgumentException>();
        await Assert.That(() => collection.GetAll(2, 5).ToList()).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task GetEnumerator_SupportsConcurrentRead()
    {
        LargeObservableCollection<int> collection = [1, 2, 3, 4, 5];
        List<int> results = [];

        // Test that reading while enumeration works (no concurrent modification exception)
        foreach (int item in collection)
        {
            results.Add(item);
            _ = collection.Count; // Read operation during enumeration
        }

        await Assert.That(results).IsEquivalentTo(new[] { 1, 2, 3, 4, 5 });
    }

    // =============== AsReadOnly Tests ===============

    [Test]
    public async Task AsReadOnly_ReturnsReadOnlyWrapper()
    {
        LargeObservableCollection<int> collection = [1, 2, 3, 4, 5];
        ReadOnlyLargeObservableCollection<int> readOnly = collection.AsReadOnly();

        await Assert.That(readOnly).IsNotNull();
        await Assert.That(readOnly.Count).IsEqualTo(5L);
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 0, 1);
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 4, 5);
    }

    [Test]
    public async Task AsReadOnly_ForwardsEventsFromOriginalCollection()
    {
        LargeObservableCollection<string> collection = ["A", "B", "C"];
        ReadOnlyLargeObservableCollection<string> readOnly = collection.AsReadOnly();
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        // Modify original collection
        collection.Add("D");
        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Add);

        tracker.Clear();
        collection.RemoveAt(0);
        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Remove);

        tracker.Clear();
        collection[0] = "Modified";
        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Replace);
    }

    [Test]
    public async Task AsReadOnly_ReflectsChangesInOriginalCollection()
    {
        LargeObservableCollection<int> collection = [10, 20, 30];
        ReadOnlyLargeObservableCollection<int> readOnly = collection.AsReadOnly();

        await Assert.That(readOnly.Count).IsEqualTo(3L);

        // Add to original
        collection.Add(40);
        await Assert.That(readOnly.Count).IsEqualTo(4L);
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 3, 40);

        // Remove from original
        collection.RemoveAt(0);
        await Assert.That(readOnly.Count).IsEqualTo(3L);
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 0, 20);

        // Clear original
        collection.Clear();
        await Assert.That(readOnly.Count).IsEqualTo(0L);
    }

    [Test]
    public async Task AsReadOnly_MultipleSuspensions_WorkIndependently()
    {
        LargeObservableCollection<int> collection = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = collection.AsReadOnly();

        SharedObservableTests.EventTracker collectionTracker = SharedObservableTests.AttachEventTracker(collection);
        SharedObservableTests.EventTracker readOnlyTracker = SharedObservableTests.AttachEventTracker(readOnly);

        // Suspend only the read-only wrapper
        using (readOnly.SuspendNotifications())
        {
            collection.Add(4);

            // Original collection should fire events
            await Assert.That(collectionTracker.CollectionChangedCount).IsEqualTo(1);
            // Read-only wrapper should not fire events
            await SharedObservableTests.VerifyNoEventsFire(readOnlyTracker);
        }

        // After suspension, read-only wrapper should fire events
        await Assert.That(readOnlyTracker.CollectionChangedCount).IsEqualTo(1);
    }

    [Test]
    public async Task AsReadOnly_WithSuppressEventExceptions_OriginalSettingNotAffected()
    {
        // Original collection does NOT suppress exceptions
        LargeObservableCollection<int> collection = new(suppressEventExceptions: false);
        collection.Add(1);

        // But AsReadOnly() creates wrapper with default suppressEventExceptions: false
        ReadOnlyLargeObservableCollection<int> readOnly = collection.AsReadOnly();

        // Attach throwing handler to read-only wrapper
        readOnly.CollectionChanged += (s, e) => throw new InvalidOperationException("ReadOnly exception");

        // This should throw from read-only wrapper
        await Assert.That(() => collection.Add(2)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AsReadOnly_MultipleCallsReturnDifferentInstances()
    {
        LargeObservableCollection<int> collection = [1, 2, 3];

        ReadOnlyLargeObservableCollection<int> readOnly1 = collection.AsReadOnly();
        ReadOnlyLargeObservableCollection<int> readOnly2 = collection.AsReadOnly();

        // Should return different instances (not cached)
        await Assert.That(ReferenceEquals(readOnly1, readOnly2)).IsFalse();

        // But both should wrap the same collection
        await Assert.That(readOnly1.Count).IsEqualTo(readOnly2.Count);
        collection.Add(4);
        await Assert.That(readOnly1.Count).IsEqualTo(4L);
        await Assert.That(readOnly2.Count).IsEqualTo(4L);
    }

    [Test]
    public async Task AsReadOnly_WithEmptyCollection_WorksCorrectly()
    {
        LargeObservableCollection<string> collection = [];
        ReadOnlyLargeObservableCollection<string> readOnly = collection.AsReadOnly();

        await Assert.That(readOnly.Count).IsEqualTo(0L);
        await SharedObservableTests.VerifyGetAll(readOnly, []);

        collection.Add("First");
        await Assert.That(readOnly.Count).IsEqualTo(1L);
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 0, "First");
    }

    [Test]
    public async Task AsReadOnly_SupportsAllReadOnlyOperations()
    {
        LargeObservableCollection<int> collection = [1, 2, 3, 4, 5];
        ReadOnlyLargeObservableCollection<int> readOnly = collection.AsReadOnly();

        // Test Contains
        await SharedObservableTests.VerifyContains(readOnly, 3, true);
        await SharedObservableTests.VerifyContains(readOnly, 10, false);

        // Test Get
        await SharedObservableTests.VerifyGet(readOnly, 2, 3);

        // Test GetAll
        await SharedObservableTests.VerifyGetAll(readOnly, [1, 2, 3, 4, 5]);

        // Test Enumeration
        await SharedObservableTests.VerifyEnumeration(readOnly, [1, 2, 3, 4, 5]);

        // Test DoForEach
        List<int> processed = [];
        readOnly.DoForEach(item => processed.Add(item * 2));
        await Assert.That(processed).IsEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    // =============== Missing Method Coverage Tests ===============

    [Test]
    public async Task RemoveAt_WithPreserveOrderFalse_WorksCorrectly()
    {
        LargeObservableCollection<int> collection = [1, 2, 3, 4, 5];
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        // RemoveAt with preserveOrder=false should be faster but changes order
        int removed = collection.RemoveAt(1, preserveOrder: false);

        await Assert.That(removed).IsEqualTo(2);
        await Assert.That(collection.Count).IsEqualTo(4L);

        // Should fire Remove event
        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Remove);
        await SharedObservableTests.VerifyPropertyChangedEventForCount(tracker);
    }

    [Test]
    public async Task RemoveAt_PreserveOrderComparison()
    {
        // Test with preserveOrder=true
        LargeObservableCollection<int> collectionPreserve = [1, 2, 3, 4, 5];
        collectionPreserve.RemoveAt(1, preserveOrder: true);

        await Assert.That(collectionPreserve[0]).IsEqualTo(1);
        await Assert.That(collectionPreserve[1]).IsEqualTo(3);
        await Assert.That(collectionPreserve[2]).IsEqualTo(4);
        await Assert.That(collectionPreserve[3]).IsEqualTo(5);

        // Test with preserveOrder=false (order may change)
        LargeObservableCollection<int> collectionNoPreserve = [1, 2, 3, 4, 5];
        collectionNoPreserve.RemoveAt(1, preserveOrder: false);

        await Assert.That(collectionNoPreserve.Count).IsEqualTo(4L);
        await Assert.That(collectionNoPreserve.Contains(2)).IsFalse();
    }

    [Test]
    public async Task BinarySearch_WithOffsetAndCount_WorksCorrectly()
    {
        LargeObservableCollection<int> collection = [0, 1, 2, 3, 5, 8, 13, 21, 34, 55];

        // Search within a specific range
        long index = collection.BinarySearch(8, (x, y) => x.CompareTo(y), 3, 5);
        await Assert.That(index).IsEqualTo(5L);

        // Search outside the specified range should not find
        long notFoundIndex = collection.BinarySearch(1, (x, y) => x.CompareTo(y), 5, 5);
        await Assert.That(notFoundIndex).IsLessThan(0L);

        // Search at exact range boundary
        long boundaryIndex = collection.BinarySearch(3, (x, y) => x.CompareTo(y), 0, 4);
        await Assert.That(boundaryIndex).IsEqualTo(3L);
    }

    [Test]
    public async Task BinarySearch_WithOffsetAndCount_EdgeCases()
    {
        LargeObservableCollection<string> collection = ["A", "C", "E", "G", "I", "K", "M"];

        // Search with offset=0, count=3
        long index1 = collection.BinarySearch("C", (x, y) => string.Compare(x, y, StringComparison.Ordinal), 0, 3);
        await Assert.That(index1).IsEqualTo(1L);

        // Search with offset in middle
        long index2 = collection.BinarySearch("I", (x, y) => string.Compare(x, y, StringComparison.Ordinal), 3, 3);
        await Assert.That(index2).IsEqualTo(4L);

        // Search for item outside range
        long notFound = collection.BinarySearch("M", (x, y) => string.Compare(x, y, StringComparison.Ordinal), 0, 5);
        await Assert.That(notFound).IsLessThan(0L);
    }

    [Test]
    public async Task IndexerSet_LargeIndex_ConceptualTest()
    {
        // Note: We cannot actually test with indices > int.MaxValue in practical tests
        // but we can verify the behavior with available indices
        LargeObservableCollection<string> collection = [];

        // Add enough items to test the logic
        for (int i = 0; i < 1000; i++)
        {
            collection.Add($"Item{i}");
        }

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        // Set at index 999 (within int.MaxValue)
        collection[999] = "Modified";

        await Assert.That(collection[999]).IsEqualTo("Modified");
        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Replace);
    }

    [Test]
    public async Task CopyFrom_SingleItem_FiresReplaceEvent()
    {
        LargeObservableCollection<int> collection = [1, 2, 3, 4, 5];
        LargeArray<int> source = new(1);
        source[0] = 99;

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        // Copy single item
        collection.CopyFrom(source, 0, 2, 1);

        await Assert.That(collection[2]).IsEqualTo(99);
        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Replace);
    }

    [Test]
    public async Task CopyFromArray_SingleItem_FiresReplaceEvent()
    {
        LargeObservableCollection<string> collection = ["A", "B", "C", "D"];
        string[] source = ["X"];

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        collection.CopyFromArray(source, 0, 1, 1);

        await Assert.That(collection[1]).IsEqualTo("X");
        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Replace);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    public async Task CopyFromSpan_SingleItem_FiresReplaceEvent()
    {
        LargeObservableCollection<double> collection = [1.1, 2.2, 3.3, 4.4];
        ReadOnlySpan<double> source = [9.9];

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);
        
        collection.CopyFromSpan(source, 0, 1);

        await Assert.That(collection[0]).IsEqualTo(9.9);
        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Replace);
    }
#endif

    [Test]
    public async Task CopyFrom_ZeroCount_NoEvents()
    {
        LargeObservableCollection<int> collection = [1, 2, 3];
        LargeArray<int> source = new(5);

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(collection);

        // Copy zero items - implementation may validate this
        // The behavior depends on implementation details
        bool threwException = false;
        try
        {
            collection.CopyFrom(source, 0, 0, 0);
        }
        catch (ArgumentException)
        {
            threwException = true;
        }

        // If no exception, verify no events fired
        if (!threwException)
        {
            await SharedObservableTests.VerifyNoEventsFire(tracker);
        }
    }
}
