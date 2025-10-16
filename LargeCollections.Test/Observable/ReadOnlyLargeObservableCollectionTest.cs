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

public class ReadOnlyLargeObservableCollectionTest
{
    // =============== Constructor Tests ===============

    [Test]
    public async Task Constructor_WithNullCollection_ThrowsArgumentNullException()
    {
        await Assert.That(() => new ReadOnlyLargeObservableCollection<int>(null)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_WithValidCollection_CreatesReadOnlyWrapper()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await Assert.That(readOnly.Count).IsEqualTo(3L);
        await Assert.That(readOnly[0]).IsEqualTo(1);
        await Assert.That(readOnly[1]).IsEqualTo(2);
        await Assert.That(readOnly[2]).IsEqualTo(3);
    }

    [Test]
    public async Task Constructor_WithSuppressEventExceptions_CreatesReadOnlyWrapper()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner, suppressEventExceptions: true);

        await Assert.That(readOnly.Count).IsEqualTo(3L);
    }

    // =============== Event Forwarding Tests ===============

    [Test]
    public async Task EventForwarding_Add_ForwardsCollectionChangedEvent()
    {
        LargeObservableCollection<string> inner = [];
        ReadOnlyLargeObservableCollection<string> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        inner.Add("Item1");

        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Add);
        await SharedObservableTests.VerifyPropertyChangedEventForCount(tracker);
    }

    [Test]
    public async Task EventForwarding_AddMultiple_ForwardsCollectionChangedEvent()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        inner.AddRange([1, 2, 3, 4, 5]);

        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task EventForwarding_Remove_ForwardsCollectionChangedEvent()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        inner.Remove(2);

        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task EventForwarding_RemoveAt_ForwardsCollectionChangedEvent()
    {
        LargeObservableCollection<string> inner = ["A", "B", "C"];
        ReadOnlyLargeObservableCollection<string> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        inner.RemoveAt(1);

        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Remove);
    }

    [Test]
    public async Task EventForwarding_Clear_ForwardsCollectionChangedEvent()
    {
        LargeObservableCollection<int> inner = [1, 2, 3, 4, 5];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        inner.Clear();

        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Reset);
        await SharedObservableTests.VerifyPropertyChangedEventForCount(tracker);
    }

    [Test]
    public async Task EventForwarding_Set_ForwardsCollectionChangedEvent()
    {
        LargeObservableCollection<int> inner = [10, 20, 30];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        inner[1] = 200;

        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Replace);
    }

    [Test]
    public async Task EventForwarding_Sort_ForwardsCollectionChangedEvent()
    {
        LargeObservableCollection<int> inner = [5, 2, 8, 1, 9];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        inner.Sort((a, b) => a.CompareTo(b));

        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task EventForwarding_Swap_ForwardsCollectionChangedEvent()
    {
        LargeObservableCollection<string> inner = ["First", "Second", "Third"];
        ReadOnlyLargeObservableCollection<string> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        inner.Swap(0, 2);

        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task EventForwarding_MultipleOperations_ForwardsAllEvents()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        inner.Add(1);
        inner.Add(2);
        inner.Add(3);
        inner.RemoveAt(1);

        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(4);
    }

    // =============== SuspendNotifications Tests ===============

    [Test]
    public async Task SuspendNotifications_BlocksForwardedEvents()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        using (readOnly.SuspendNotifications())
        {
            inner.Add(1);
            inner.Add(2);
            inner.Add(3);

            // No events should be forwarded while suspended
            await SharedObservableTests.VerifyNoEventsFire(tracker);
        }

        // After disposal, should fire Reset event
        await Assert.That(readOnly.Count).IsEqualTo(3L);
        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Reset);
        await SharedObservableTests.VerifyPropertyChangedEventForCount(tracker);
    }

    [Test]
    public async Task SuspendNotifications_NestedSuspensions_WorksCorrectly()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        using (readOnly.SuspendNotifications())
        {
            inner.Add(1);

            using (readOnly.SuspendNotifications())
            {
                inner.Add(2);
                await SharedObservableTests.VerifyNoEventsFire(tracker);
            }

            inner.Add(3);
            // Still suspended at outer level
            await SharedObservableTests.VerifyNoEventsFire(tracker);
        }

        // Now events should fire
        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public async Task SuspendNotifications_NoChanges_NoEventsAfterDisposal()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        using (readOnly.SuspendNotifications())
        {
            // No changes made while suspended
        }

        // No events should be fired since no changes occurred
        await SharedObservableTests.VerifyNoEventsFire(tracker);
    }

    [Test]
    public async Task SuspendNotifications_CountChangeDetection_WorksCorrectly()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        using (readOnly.SuspendNotifications())
        {
            inner.Add(4);
            inner.Add(5);
        }

        // Should fire both CollectionChanged and PropertyChanged for Count
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(1);
        await SharedObservableTests.VerifyPropertyChangedEventForCount(tracker);
        await Assert.That(readOnly.Count).IsEqualTo(5L);
    }

    [Test]
    public async Task SuspendNotifications_InnerCollectionSuspended_BothSuspended()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker innerTracker = SharedObservableTests.AttachEventTracker(inner);
        SharedObservableTests.EventTracker readOnlyTracker = SharedObservableTests.AttachEventTracker(readOnly);

        // Suspend on inner collection
        using (inner.SuspendNotifications())
        {
            inner.Add(1);
            inner.Add(2);

            // Both should have no events
            await SharedObservableTests.VerifyNoEventsFire(innerTracker);
            await SharedObservableTests.VerifyNoEventsFire(readOnlyTracker);
        }

        // Both should now have events
        await Assert.That(innerTracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(readOnlyTracker.CollectionChangedCount).IsEqualTo(1);
    }

    // =============== SuppressEventExceptions Tests ===============

    [Test]
    public async Task SuppressEventExceptions_Enabled_SuppressesExceptions()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner, suppressEventExceptions: true);

        // Attach event handlers that throw
        readOnly.CollectionChanged += (sender, e) => throw new InvalidOperationException("Test exception");
        readOnly.PropertyChanged += (sender, e) => throw new InvalidOperationException("Test exception");

        bool succeeded = false;
        try
        {
            // This should not throw despite the event handlers throwing
            inner.Add(1);
            succeeded = true;
        }
        catch
        {
            // Should not reach here
        }

        await Assert.That(succeeded).IsTrue();
        await Assert.That(readOnly.Count).IsEqualTo(1L);
    }

    [Test]
    public async Task SuppressEventExceptions_Disabled_PropagatesExceptions()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner, suppressEventExceptions: false);

        // Attach event handler that throws
        readOnly.CollectionChanged += (sender, e) => throw new InvalidOperationException("Test exception");

        // This should throw because exceptions are not suppressed
        await Assert.That(() => inner.Add(1)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task SuppressEventExceptions_PropertyChanged_SuppressesExceptions()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner, suppressEventExceptions: true);

        // Attach PropertyChanged handler that throws
        readOnly.PropertyChanged += (sender, e) => throw new InvalidOperationException("PropertyChanged exception");

        bool succeeded = false;
        try
        {
            inner.Add(42);
            succeeded = true;
        }
        catch
        {
            // Should not reach here
        }

        await Assert.That(succeeded).IsTrue();
    }

    // =============== Disposal Tests ===============

    [Test]
    public async Task Dispose_UnsubscribesFromInnerCollection()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        // Dispose the read-only collection
        readOnly.Dispose();

        // Modify inner collection
        inner.Add(1);
        inner.Add(2);

        // ReadOnly collection should not receive events after disposal
        await SharedObservableTests.VerifyNoEventsFire(tracker);
    }

    [Test]
    public async Task Dispose_MultipleDisposals_DoesNotThrow()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        // Multiple disposals should not throw
        readOnly.Dispose();
        readOnly.Dispose();
        readOnly.Dispose();

        // Verify collection is still accessible
        await Assert.That(readOnly.Count).IsEqualTo(3L);
    }

    [Test]
    public async Task Dispose_AfterDisposal_CanStillReadData()
    {
        LargeObservableCollection<int> inner = [10, 20, 30];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        readOnly.Dispose();

        // Should still be able to read data from inner collection
        await Assert.That(readOnly.Count).IsEqualTo(3L);
        await Assert.That(readOnly[0]).IsEqualTo(10);
        await Assert.That(readOnly[1]).IsEqualTo(20);
        await Assert.That(readOnly[2]).IsEqualTo(30);
    }

    // =============== Read-Only Operations Tests (Using Shared Methods) ===============

    [Test]
    public async Task IndexerAccess_ReturnsCorrectValues()
    {
        LargeObservableCollection<string> inner = ["Alpha", "Beta", "Gamma"];
        ReadOnlyLargeObservableCollection<string> readOnly = new(inner);

        await SharedObservableTests.VerifyIndexerAccess(readOnly, 0, "Alpha");
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 1, "Beta");
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 2, "Gamma");
    }

    [Test]
    public async Task Count_ReflectsInnerCollectionCount()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyCount(readOnly, 0L);

        inner.Add(1);
        await SharedObservableTests.VerifyCount(readOnly, 1L);

        inner.AddRange([2, 3, 4]);
        await SharedObservableTests.VerifyCount(readOnly, 4L);

        inner.Clear();
        await SharedObservableTests.VerifyCount(readOnly, 0L);
    }

    [Test]
    public async Task Contains_WorksCorrectly()
    {
        LargeObservableCollection<string> inner = ["Apple", "Banana", "Cherry"];
        ReadOnlyLargeObservableCollection<string> readOnly = new(inner);

        await SharedObservableTests.VerifyContains(readOnly, "Banana", true);
        await SharedObservableTests.VerifyContains(readOnly, "Grape", false);
    }

    [Test]
    public async Task Contains_WithRange_WorksCorrectly()
    {
        LargeObservableCollection<int> inner = [1, 2, 3, 4, 5];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyContainsWithRange(readOnly, 3, 0, 5, true);
        await SharedObservableTests.VerifyContainsWithRange(readOnly, 1, 1, 3, false);
        await SharedObservableTests.VerifyContainsWithRange(readOnly, 5, 0, 4, false);
    }

    [Test]
    public async Task Get_ReturnsCorrectValue()
    {
        LargeObservableCollection<double> inner = [1.1, 2.2, 3.3];
        ReadOnlyLargeObservableCollection<double> readOnly = new(inner);

        await SharedObservableTests.VerifyGet(readOnly, 0, 1.1);
        await SharedObservableTests.VerifyGet(readOnly, 1, 2.2);
        await SharedObservableTests.VerifyGet(readOnly, 2, 3.3);
    }

    [Test]
    public async Task GetAll_WithoutParameters_ReturnsAllElements()
    {
        LargeObservableCollection<int> inner = [10, 20, 30, 40, 50];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyGetAll(readOnly, [10, 20, 30, 40, 50]);
    }

    [Test]
    public async Task GetAll_WithRange_ReturnsCorrectRange()
    {
        LargeObservableCollection<string> inner = ["A", "B", "C", "D", "E"];
        ReadOnlyLargeObservableCollection<string> readOnly = new(inner);

        await SharedObservableTests.VerifyGetAllWithRange(readOnly, 1, 3, ["B", "C", "D"]);
    }

    [Test]
    public async Task GetAll_EmptyCollection_ReturnsEmpty()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyGetAll(readOnly, []);
    }

    [Test]
    public async Task Enumeration_WorksCorrectly()
    {
        LargeObservableCollection<int> inner = [1, 2, 3, 4, 5];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyEnumeration(readOnly, [1, 2, 3, 4, 5]);
    }

    [Test]
    public async Task NonGenericEnumeration_WorksCorrectly()
    {
        LargeObservableCollection<string> inner = ["X", "Y", "Z"];
        ReadOnlyLargeObservableCollection<string> readOnly = new(inner);

        await SharedObservableTests.VerifyNonGenericEnumeration(readOnly, ["X", "Y", "Z"]);
    }

    [Test]
    public async Task CopyTo_Methods_WorkCorrectly()
    {
        LargeObservableCollection<int> inner = [10, 20, 30, 40, 50];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyCopyTo(readOnly, 1, 3);
    }

    [Test]
    public async Task DoForEach_WorksCorrectly()
    {
        LargeObservableCollection<int> inner = [1, 2, 3, 4, 5];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyDoForEach(readOnly, (item, list) => list.Add(item));
    }

    [Test]
    public async Task DoForEach_WithRange_WorksCorrectly()
    {
        LargeObservableCollection<int> inner = [10, 20, 30, 40, 50];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyDoForEachWithRange(readOnly, 1, 3, (item, list) => list.Add(item));
    }

    [Test]
    public async Task DoForEach_WithUserData_WorksCorrectly()
    {
        LargeObservableCollection<int> inner = [1, 2, 3, 4, 5];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyDoForEachWithUserData(readOnly);
    }

    [Test]
    public async Task BinarySearch_WorksCorrectly()
    {
        LargeObservableCollection<int> inner = [1, 3, 5, 7, 9];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyBinarySearch(readOnly, 5, (x, y) => x.CompareTo(y), 2L);

        long notFoundIndex = readOnly.BinarySearch(6, (x, y) => x.CompareTo(y));
        await Assert.That(notFoundIndex).IsLessThan(0L);
    }

    [Test]
    public async Task BinarySearch_WithRange_WorksCorrectly()
    {
        LargeObservableCollection<int> inner = [1, 3, 5, 7, 9, 11, 13];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyBinarySearchWithRange(readOnly, 7, (x, y) => x.CompareTo(y), 0, 5, 3L);
    }

    // =============== Exception Handling Tests ===============

    [Test]
    public async Task InvalidIndex_ThrowsIndexOutOfRangeException()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyInvalidIndexThrows(readOnly, -1);
        await SharedObservableTests.VerifyInvalidIndexThrows(readOnly, 3);
        await SharedObservableTests.VerifyInvalidIndexThrows(readOnly, 100);
    }

    [Test]
    public async Task Get_InvalidIndex_ThrowsIndexOutOfRangeException()
    {
        LargeObservableCollection<string> inner = ["A", "B"];
        ReadOnlyLargeObservableCollection<string> readOnly = new(inner);

        await SharedObservableTests.VerifyGetInvalidIndexThrows(readOnly, -1);
        await SharedObservableTests.VerifyGetInvalidIndexThrows(readOnly, 2);
    }

    [Test]
    public async Task GetAll_InvalidRange_ThrowsArgumentException()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        await SharedObservableTests.VerifyGetAllInvalidRangeThrows(readOnly, -1, 1);
        await SharedObservableTests.VerifyGetAllInvalidRangeThrows(readOnly, 0, -1);
        await SharedObservableTests.VerifyGetAllInvalidRangeThrows(readOnly, 2, 5);
    }

    // =============== Integration Tests ===============

    [Test]
    public async Task IntegrationTest_ComplexScenario()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        // Add initial items
        inner.AddRange([5, 3, 8, 1, 9, 2]);
        await Assert.That(tracker.CollectionChangedCount).IsGreaterThanOrEqualTo(1);

        tracker.Clear();

        // Sort the collection
        inner.Sort((a, b) => a.CompareTo(b));
        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Reset);

        // Verify sorted order through read-only wrapper
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 0, 1);
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 5, 9);

        tracker.Clear();

        // Suspend and modify
        using (readOnly.SuspendNotifications())
        {
            inner.Add(10);
            inner.RemoveAt(0);
            await SharedObservableTests.VerifyNoEventsFire(tracker);
        }

        // Should fire events after suspension
        await Assert.That(tracker.CollectionChangedCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task IntegrationTest_AsReadOnlyFromLargeObservableCollection()
    {
        LargeObservableCollection<string> inner = ["One", "Two", "Three"];
        ReadOnlyLargeObservableCollection<string> readOnly = inner.AsReadOnly();

        await Assert.That(readOnly.Count).IsEqualTo(3L);
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 0, "One");

        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);
        inner.Add("Four");

        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1);
        await Assert.That(readOnly.Count).IsEqualTo(4L);
    }

    [Test]
    public async Task IntegrationTest_ConcurrentSuspensions()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker readOnlyTracker = SharedObservableTests.AttachEventTracker(readOnly);
        SharedObservableTests.EventTracker innerTracker = SharedObservableTests.AttachEventTracker(inner);

        // Suspend both inner and read-only
        using (inner.SuspendNotifications())
        using (readOnly.SuspendNotifications())
        {
            inner.Add(4);
            inner.Add(5);

            await SharedObservableTests.VerifyNoEventsFire(innerTracker);
            await SharedObservableTests.VerifyNoEventsFire(readOnlyTracker);
        }

        // Both should fire events
        await Assert.That(innerTracker.CollectionChangedCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(readOnlyTracker.CollectionChangedCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task NullHandling_ReferencetypesWithNull()
    {
        LargeObservableCollection<string> inner = [];
        ReadOnlyLargeObservableCollection<string> readOnly = new(inner);

        inner.Add(null);
        inner.Add("NotNull");
        inner.Add(null);

        await Assert.That(readOnly.Count).IsEqualTo(3L);
        await Assert.That(readOnly[0]).IsNull();
        await Assert.That(readOnly[1]).IsEqualTo("NotNull");
        await Assert.That(readOnly[2]).IsNull();
        await SharedObservableTests.VerifyContains(readOnly, null, true);
    }

    [Test]
    public async Task LargeDataSet_PerformanceTest()
    {
        LargeObservableCollection<int> inner = new(10000);
        for (int i = 0; i < 10000; i++)
        {
            inner.Add(i);
        }

        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        await Assert.That(readOnly.Count).IsEqualTo(10000L);
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 5000, 5000);

        // Test enumeration performance
        long sum = 0;
        foreach (int item in readOnly)
        {
            sum += item;
        }

        await Assert.That(sum).IsEqualTo(49995000L); // Sum of 0 to 9999
    }

    // =============== AsReadOnly Extended Tests ===============

    [Test]
    public async Task AsReadOnly_CreatesCorrectWrapperType()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = inner.AsReadOnly();

        // Verify it's actually a ReadOnlyLargeObservableCollection
        await Assert.That(readOnly).IsTypeOf<ReadOnlyLargeObservableCollection<int>>();
        await Assert.That(readOnly.Count).IsEqualTo(3L);
    }

    [Test]
    public async Task AsReadOnly_EventForwardingWorksImmediately()
    {
        LargeObservableCollection<string> inner = ["Initial"];
        ReadOnlyLargeObservableCollection<string> readOnly = inner.AsReadOnly();
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        // Immediately test event forwarding
        inner.Add("Added");

        await SharedObservableTests.VerifyCollectionChangedEventFires(tracker, 1, NotifyCollectionChangedAction.Add);
        await SharedObservableTests.VerifyPropertyChangedEventForCount(tracker);
    }

    [Test]
    public async Task AsReadOnly_DisposalStopsEventForwarding()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = inner.AsReadOnly();
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        // Dispose the read-only wrapper
        readOnly.Dispose();

        // Modify inner collection
        inner.Add(4);
        inner.Add(5);

        // Should not receive events after disposal
        await SharedObservableTests.VerifyNoEventsFire(tracker);
    }

    [Test]
    public async Task AsReadOnly_SuspensionOnWrapperDoesNotAffectOriginal()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = inner.AsReadOnly();

        SharedObservableTests.EventTracker innerTracker = SharedObservableTests.AttachEventTracker(inner);
        SharedObservableTests.EventTracker readOnlyTracker = SharedObservableTests.AttachEventTracker(readOnly);

        // Suspend only the wrapper
        using (readOnly.SuspendNotifications())
        {
            inner.Add(4);
            inner.Add(5);

            // Inner collection should fire events normally
            await Assert.That(innerTracker.CollectionChangedCount).IsEqualTo(2);

            // Wrapper should not fire events during suspension
            await SharedObservableTests.VerifyNoEventsFire(readOnlyTracker);
        }

        // After suspension, wrapper should fire batched event
        await Assert.That(readOnlyTracker.CollectionChangedCount).IsEqualTo(1);
    }

    [Test]
    public async Task AsReadOnly_AllReadOnlyOperationsWork()
    {
        LargeObservableCollection<double> inner = [1.1, 2.2, 3.3, 4.4, 5.5];
        ReadOnlyLargeObservableCollection<double> readOnly = inner.AsReadOnly();

        // Indexer
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 0, 1.1);
        await SharedObservableTests.VerifyIndexerAccess(readOnly, 4, 5.5);

        // Count
        await SharedObservableTests.VerifyCount(readOnly, 5L);

        // Contains
        await SharedObservableTests.VerifyContains(readOnly, 3.3, true);
        await SharedObservableTests.VerifyContains(readOnly, 9.9, false);

        // Get
        await SharedObservableTests.VerifyGet(readOnly, 2, 3.3);

        // GetAll
        await SharedObservableTests.VerifyGetAll(readOnly, [1.1, 2.2, 3.3, 4.4, 5.5]);

        // Enumeration
        await SharedObservableTests.VerifyEnumeration(readOnly, [1.1, 2.2, 3.3, 4.4, 5.5]);

        // DoForEach
        double sum = 0;
        readOnly.DoForEach(item => sum += item);
        await Assert.That(Math.Abs(sum - 16.5)).IsLessThan(0.01);
    }

    [Test]
    public async Task AsReadOnly_WithNullElements_WorksCorrectly()
    {
        LargeObservableCollection<string> inner = ["A", null, "C", null];
        ReadOnlyLargeObservableCollection<string> readOnly = inner.AsReadOnly();

        await Assert.That(readOnly.Count).IsEqualTo(4L);
        await Assert.That(readOnly[0]).IsEqualTo("A");
        await Assert.That(readOnly[1]).IsNull();
        await Assert.That(readOnly[2]).IsEqualTo("C");
        await Assert.That(readOnly[3]).IsNull();

        await SharedObservableTests.VerifyContains(readOnly, null, true);
    }

    [Test]
    public async Task AsReadOnly_ChainedWithInnerSuspension_WorksCorrectly()
    {
        LargeObservableCollection<int> inner = [1, 2, 3];
        ReadOnlyLargeObservableCollection<int> readOnly = inner.AsReadOnly();

        SharedObservableTests.EventTracker innerTracker = SharedObservableTests.AttachEventTracker(inner);
        SharedObservableTests.EventTracker readOnlyTracker = SharedObservableTests.AttachEventTracker(readOnly);

        // Suspend inner collection
        using (inner.SuspendNotifications())
        {
            inner.Add(4);
            inner.Add(5);

            // Both should have no events during inner suspension
            await SharedObservableTests.VerifyNoEventsFire(innerTracker);
            await SharedObservableTests.VerifyNoEventsFire(readOnlyTracker);
        }

        // Both should fire events after inner suspension ends
        await Assert.That(innerTracker.CollectionChangedCount).IsEqualTo(1);
        await Assert.That(readOnlyTracker.CollectionChangedCount).IsEqualTo(1);
    }

    // =============== Missing Method Coverage Tests ===============

    [Test]
    public async Task DoForEach_WithUserDataAndRange_WorksCorrectly()
    {
        LargeObservableCollection<int> inner = [10, 20, 30, 40, 50, 60];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        // Test DoForEach with UserData, offset and count
        long sum = 0;
        readOnly.DoForEach((int item, ref long userSum) => userSum += item, 1, 4, ref sum);

        // Should sum items at indices 1-4: 20 + 30 + 40 + 50 = 140
        await Assert.That(sum).IsEqualTo(140L);
    }

    [Test]
    public async Task DoForEach_WithUserDataAndRange_EmptyRange()
    {
        LargeObservableCollection<string> inner = ["A", "B", "C"];
        ReadOnlyLargeObservableCollection<string> readOnly = new(inner);

        List<string> collected = [];
        readOnly.DoForEach((string item, ref List<string> list) => list.Add(item), 1, 0, ref collected);

        // Zero count should result in no items processed
        await Assert.That(collected.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DoForEach_WithUserDataAndRange_ComplexUserData()
    {
        LargeObservableCollection<int> inner = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        // Use complex user data structure
        (int count, int sum, int max) stats = (0, 0, int.MinValue);

        readOnly.DoForEach(
            (int item, ref (int count, int sum, int max) userStats) =>
            {
                userStats.count++;
                userStats.sum += item;
                if (item > userStats.max) userStats.max = item;
            },
            2, 5, ref stats);

        // Should process items at indices 2-6: 3, 4, 5, 6, 7
        await Assert.That(stats.count).IsEqualTo(5);
        await Assert.That(stats.sum).IsEqualTo(25); // 3+4+5+6+7
        await Assert.That(stats.max).IsEqualTo(7);
    }

    [Test]
    public async Task EventForwarding_CopyFromOperations_ForwardsEvents()
    {
        LargeObservableCollection<int> inner = [1, 2, 3, 4, 5];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        // CopyFrom with single item should forward Replace event
        LargeArray<int> source = new(1);
        source[0] = 99;
        inner.CopyFrom(source, 0, 2, 1);

        await Assert.That(tracker.CollectionChangedCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(readOnly[2]).IsEqualTo(99);
    }

    [Test]
    public async Task EventForwarding_CopyFromArray_ForwardsEvents()
    {
        LargeObservableCollection<string> inner = ["A", "B", "C", "D"];
        ReadOnlyLargeObservableCollection<string> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        string[] sourceArray = ["X", "Y"];
        inner.CopyFromArray(sourceArray, 0, 1, 2);

        await Assert.That(tracker.CollectionChangedCount).IsGreaterThanOrEqualTo(1);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [Test]
    public async Task EventForwarding_CopyFromSpan_ForwardsEvents()
    {
        LargeObservableCollection<double> inner = [1.0, 2.0, 3.0, 4.0];
        ReadOnlyLargeObservableCollection<double> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        ReadOnlySpan<double> span = [9.9];
        inner.CopyFromSpan(span, 0, 1);

        await Assert.That(tracker.CollectionChangedCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(readOnly[0]).IsEqualTo(9.9);
    }
#endif

    [Test]
    public async Task BinarySearch_WithOffsetAndCount_OnReadOnlyCollection()
    {
        LargeObservableCollection<int> inner = [1, 3, 5, 7, 9, 11, 13, 15, 17, 19];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        // Search in specific range
        long index = readOnly.BinarySearch(11, (x, y) => x.CompareTo(y), 3, 5);
        await Assert.That(index).IsEqualTo(5L);

        // Search for item not in range
        long notFound = readOnly.BinarySearch(3, (x, y) => x.CompareTo(y), 5, 5);
        await Assert.That(notFound).IsLessThan(0L);
    }

    [Test]
    public async Task ComplexScenario_MultipleOperationsWithSuspension()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);
        SharedObservableTests.EventTracker tracker = SharedObservableTests.AttachEventTracker(readOnly);

        // Add initial data
        inner.AddRange([1, 2, 3, 4, 5]);
        tracker.Clear();

        // Suspend and perform multiple operations
        using (readOnly.SuspendNotifications())
        {
            inner.Sort((a, b) => b.CompareTo(a)); // Reverse sort
            inner.RemoveAt(0);
            inner.Add(10);

            await SharedObservableTests.VerifyNoEventsFire(tracker);
        }

        // Should fire event after suspension
        await Assert.That(tracker.CollectionChangedCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(readOnly.Count).IsEqualTo(5L);
    }

    [Test]
    public async Task AllReadOnlyMethods_WithEmptyCollection()
    {
        LargeObservableCollection<int> inner = [];
        ReadOnlyLargeObservableCollection<int> readOnly = new(inner);

        // Test all read-only methods with empty collection
        await SharedObservableTests.VerifyCount(readOnly, 0L);
        await SharedObservableTests.VerifyContains(readOnly, 1, false);
        await SharedObservableTests.VerifyGetAll(readOnly, []);

        List<int> enumerated = [];
        foreach (int item in readOnly)
        {
            enumerated.Add(item);
        }
        await Assert.That(enumerated.Count).IsEqualTo(0);

        // DoForEach should not execute action
        int callCount = 0;
        readOnly.DoForEach(item => callCount++);
        await Assert.That(callCount).IsEqualTo(0);
    }
}
