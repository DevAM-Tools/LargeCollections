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

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LargeCollections.Observable.Test;

/// <summary>
/// Shared test methods for IReadOnlyLargeObservableCollection implementations.
/// These methods can be used to test both LargeObservableCollection and ReadOnlyLargeObservableCollection.
/// </summary>
public static class SharedObservableTests
{
    public class EventTracker
    {
        public List<NotifyCollectionChangedEventArgs> CollectionChangedEvents { get; } = [];
        public List<PropertyChangedEventArgs> PropertyChangedEvents { get; } = [];
        public int CollectionChangedCount => CollectionChangedEvents.Count;
        public int PropertyChangedCount => PropertyChangedEvents.Count;

        public void Clear()
        {
            CollectionChangedEvents.Clear();
            PropertyChangedEvents.Clear();
        }
    }

    public static EventTracker AttachEventTracker<T>(IReadOnlyLargeObservableCollection<T> collection)
    {
        EventTracker tracker = new();
        collection.CollectionChanged += (sender, e) => tracker.CollectionChangedEvents.Add(e);
        collection.PropertyChanged += (sender, e) => tracker.PropertyChangedEvents.Add(e);
        return tracker;
    }

    /// <summary>
    /// Verifies that the collection provides correct indexer access.
    /// </summary>
    public static async Task VerifyIndexerAccess<T>(IReadOnlyLargeObservableCollection<T> collection, long index, T expectedValue)
    {
        await Assert.That(collection[index]).IsEqualTo(expectedValue);
    }

    /// <summary>
    /// Verifies that the Count property works correctly.
    /// </summary>
    public static async Task VerifyCount<T>(IReadOnlyLargeObservableCollection<T> collection, long expectedCount)
    {
        await Assert.That(collection.Count).IsEqualTo(expectedCount);
    }

    /// <summary>
    /// Verifies that Contains method works correctly.
    /// </summary>
    public static async Task VerifyContains<T>(IReadOnlyLargeObservableCollection<T> collection, T item, bool shouldContain)
    {
        await Assert.That(collection.Contains(item)).IsEqualTo(shouldContain);
    }

    /// <summary>
    /// Verifies that Contains with range works correctly.
    /// </summary>
    public static async Task VerifyContainsWithRange<T>(IReadOnlyLargeObservableCollection<T> collection, T item, long offset, long count, bool shouldContain)
    {
        await Assert.That(collection.Contains(item, offset, count)).IsEqualTo(shouldContain);
    }

    /// <summary>
    /// Verifies that Get method returns correct value.
    /// </summary>
    public static async Task VerifyGet<T>(IReadOnlyLargeObservableCollection<T> collection, long index, T expectedValue)
    {
        await Assert.That(collection.Get(index)).IsEqualTo(expectedValue);
    }

    /// <summary>
    /// Verifies that GetAll without parameters returns all elements.
    /// </summary>
    public static async Task VerifyGetAll<T>(IReadOnlyLargeObservableCollection<T> collection, T[] expectedValues)
    {
        List<T> result = collection.GetAll().ToList();
        await Assert.That(result).IsEquivalentTo(expectedValues);
    }

    /// <summary>
    /// Verifies that GetAll with range returns correct elements.
    /// </summary>
    public static async Task VerifyGetAllWithRange<T>(IReadOnlyLargeObservableCollection<T> collection, long offset, long count, T[] expectedValues)
    {
        List<T> result = collection.GetAll(offset, count).ToList();
        await Assert.That(result).IsEquivalentTo(expectedValues);
    }

    /// <summary>
    /// Verifies that enumeration works correctly.
    /// </summary>
    public static async Task VerifyEnumeration<T>(IReadOnlyLargeObservableCollection<T> collection, T[] expectedValues)
    {
        List<T> result = [];
        foreach (T item in collection)
        {
            result.Add(item);
        }
        await Assert.That(result).IsEquivalentTo(expectedValues);
    }

    /// <summary>
    /// Verifies that non-generic enumeration works correctly.
    /// </summary>
    public static async Task VerifyNonGenericEnumeration<T>(IReadOnlyLargeObservableCollection<T> collection, T[] expectedValues)
    {
        IEnumerable enumerable = (IEnumerable)collection;
        List<object> result = [];
        foreach (object item in enumerable)
        {
            result.Add(item);
        }
        await Assert.That(result).IsEquivalentTo(expectedValues.Cast<object>().ToArray());
    }

    /// <summary>
    /// Verifies that CopyTo methods work correctly.
    /// </summary>
    public static async Task VerifyCopyTo<T>(IReadOnlyLargeObservableCollection<T> collection, long sourceOffset, long count)
    {
        // Test CopyTo with ILargeArray
        LargeArray<T> targetArray = new(count + 5);
        collection.CopyTo(targetArray, sourceOffset, 2, count);

        for (long i = 0; i < count; i++)
        {
            await Assert.That(targetArray[i + 2]).IsEqualTo(collection[sourceOffset + i]);
        }

        // Test CopyToArray
        if (count <= int.MaxValue)
        {
            T[] arrayTarget = new T[count + 3];
            collection.CopyToArray(arrayTarget, sourceOffset, 1, (int)count);

            for (long i = 0; i < count; i++)
            {
                await Assert.That(arrayTarget[i + 1]).IsEqualTo(collection[sourceOffset + i]);
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        // Test CopyToSpan
        if (count <= int.MaxValue)
        {
            T[] spanTarget = new T[count];
            collection.CopyToSpan(spanTarget.AsSpan(), sourceOffset, (int)count);

            for (long i = 0; i < count; i++)
            {
                await Assert.That(spanTarget[(int)i]).IsEqualTo(collection[sourceOffset + i]);
            }
        }
#endif
    }

    /// <summary>
    /// Verifies that DoForEach methods work correctly.
    /// </summary>
    public static async Task VerifyDoForEach<T>(IReadOnlyLargeObservableCollection<T> collection, Action<T, List<T>> collectAction)
    {
        // Test DoForEach with Action
        List<T> processed = [];
        collection.DoForEach(item => collectAction(item, processed));

        await Assert.That(processed.Count).IsEqualTo((int)collection.Count);
        for (long i = 0; i < collection.Count; i++)
        {
            await Assert.That(processed[(int)i]).IsEqualTo(collection[i]);
        }
    }

    /// <summary>
    /// Verifies that DoForEach with range works correctly.
    /// </summary>
    public static async Task VerifyDoForEachWithRange<T>(IReadOnlyLargeObservableCollection<T> collection, long offset, long count, Action<T, List<T>> collectAction)
    {
        List<T> processed = [];
        collection.DoForEach(item => collectAction(item, processed), offset, count);

        await Assert.That(processed.Count).IsEqualTo((int)count);
        for (long i = 0; i < count; i++)
        {
            await Assert.That(processed[(int)i]).IsEqualTo(collection[offset + i]);
        }
    }

    /// <summary>
    /// Verifies that DoForEach with user data works correctly.
    /// </summary>
    public static async Task VerifyDoForEachWithUserData<T>(IReadOnlyLargeObservableCollection<T> collection)
    {
        long sum = 0;
        collection.DoForEach((T item, ref long userSum) =>
        {
            if (item is int intValue)
            {
                userSum += intValue;
            }
        }, ref sum);

        long expectedSum = 0;
        foreach (T item in collection)
        {
            if (item is int intValue)
            {
                expectedSum += intValue;
            }
        }
        await Assert.That(sum).IsEqualTo(expectedSum);
    }

    /// <summary>
    /// Verifies that BinarySearch works correctly on a sorted collection.
    /// </summary>
    public static async Task VerifyBinarySearch<T>(IReadOnlyLargeObservableCollection<T> sortedCollection, T searchItem, Func<T, T, int> comparer, long expectedIndex)
    {
        long foundIndex = sortedCollection.BinarySearch(searchItem, comparer);
        await Assert.That(foundIndex).IsEqualTo(expectedIndex);
    }

    /// <summary>
    /// Verifies that BinarySearch with range works correctly.
    /// </summary>
    public static async Task VerifyBinarySearchWithRange<T>(IReadOnlyLargeObservableCollection<T> sortedCollection, T searchItem, Func<T, T, int> comparer, long offset, long count, long expectedIndex)
    {
        long foundIndex = sortedCollection.BinarySearch(searchItem, comparer, offset, count);
        await Assert.That(foundIndex).IsEqualTo(expectedIndex);
    }

    /// <summary>
    /// Verifies that invalid index access throws appropriate exception.
    /// </summary>
    public static async Task VerifyInvalidIndexThrows<T>(IReadOnlyLargeObservableCollection<T> collection, long invalidIndex)
    {
        await Assert.That(() => collection[invalidIndex]).ThrowsExactly<IndexOutOfRangeException>();
    }

    /// <summary>
    /// Verifies that Get with invalid index throws appropriate exception.
    /// </summary>
    public static async Task VerifyGetInvalidIndexThrows<T>(IReadOnlyLargeObservableCollection<T> collection, long invalidIndex)
    {
        await Assert.That(() => collection.Get(invalidIndex)).ThrowsExactly<IndexOutOfRangeException>();
    }

    /// <summary>
    /// Verifies that GetAll with invalid range throws appropriate exception.
    /// </summary>
    public static async Task VerifyGetAllInvalidRangeThrows<T>(IReadOnlyLargeObservableCollection<T> collection, long offset, long count)
    {
        await Assert.That(() => collection.GetAll(offset, count).ToList()).ThrowsExactly<ArgumentException>();
    }

    /// <summary>
    /// Verifies that CollectionChanged event fires when expected.
    /// </summary>
    public static async Task VerifyCollectionChangedEventFires(EventTracker tracker, int expectedCount, NotifyCollectionChangedAction? expectedAction = null)
    {
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(expectedCount);

        if (expectedAction.HasValue && expectedCount > 0)
        {
            await Assert.That(tracker.CollectionChangedEvents[0].Action).IsEqualTo(expectedAction.Value);
        }
    }

    /// <summary>
    /// Verifies that PropertyChanged event fires for Count property.
    /// </summary>
    public static async Task VerifyPropertyChangedEventForCount(EventTracker tracker)
    {
        PropertyChangedEventArgs countEvent = tracker.PropertyChangedEvents.FirstOrDefault(e => e.PropertyName == "Count");
        await Assert.That(countEvent).IsNotNull();
    }

    /// <summary>
    /// Verifies that no events fire when expected.
    /// </summary>
    public static async Task VerifyNoEventsFire(EventTracker tracker)
    {
        await Assert.That(tracker.CollectionChangedCount).IsEqualTo(0);
        await Assert.That(tracker.PropertyChangedCount).IsEqualTo(0);
    }
}
