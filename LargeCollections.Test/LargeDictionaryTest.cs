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

using TUnit.Assertions.Enums;

namespace LargeCollections.Test;

public class LargeDictionaryTest
{
    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task AddAndAddRange(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        // Test 1: Initialize empty dictionary
        LargeDictionary<string, long> largeDictionary = [];

        // Test null parameter handling for key-based methods
        await Assert.That(() => largeDictionary.Add(new KeyValuePair<string, long>(null, 42)))
            .Throws<ArgumentNullException>();

        // Test 2: Test Add method with KeyValuePair
        KeyValuePair<string, long>[] keyValuePairs = LargeEnumerable.Range(count / 2)
            .Select(i => new KeyValuePair<string, long>(i.ToString(), i * 2))
            .ToArray();

        foreach (KeyValuePair<string, long> kvp in keyValuePairs)
        {
            largeDictionary.Add(kvp);
        }

        await Assert.That(largeDictionary.Count).IsEqualTo(count / 2);

        // Test 3: Test AddRange method
        KeyValuePair<string, long>[] additionalPairs = LargeEnumerable.Range(count / 2, count - count / 2)
            .Select(i => new KeyValuePair<string, long>(i.ToString(), i * 3))
            .ToArray();
        largeDictionary.AddRange(additionalPairs);

        await Assert.That(largeDictionary.Count).IsEqualTo(count);

        // Test 4: Verify all items were added correctly
        for (long i = 0; i < count / 2; i++)
        {
            string key = i.ToString();
            await Assert.That(largeDictionary.ContainsKey(key)).IsTrue();
            await Assert.That(largeDictionary[key]).IsEqualTo(i * 2);
        }

        for (long i = count / 2; i < count; i++)
        {
            string key = i.ToString();
            await Assert.That(largeDictionary.ContainsKey(key)).IsTrue();
            await Assert.That(largeDictionary[key]).IsEqualTo(i * 3);
        }

        // Test 5: Test duplicate key handling - should overwrite existing values
        if (count > 0)
        {
            string duplicateKey = (count / 4).ToString();
            long newValue = (count / 4) * 10;
            largeDictionary.Add(new KeyValuePair<string, long>(duplicateKey, newValue));

            await Assert.That(largeDictionary.Count).IsEqualTo(count); // Count should remain same
            await Assert.That(largeDictionary[duplicateKey]).IsEqualTo(newValue);
        }

        // Test 6: Test AddRange with empty collection (IEnumerable version)
        largeDictionary.AddRange([]);
        await Assert.That(largeDictionary.Count).IsEqualTo(count);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        // Test 6a: Test AddRange ReadOnlySpan version with empty span
        ReadOnlySpan<KeyValuePair<string, long>> emptySpan = [];
        largeDictionary.AddRange(emptySpan);
        await Assert.That(largeDictionary.Count).IsEqualTo(count);
#endif

        // Test 7: Test AddRange with duplicates
        if (count >= 2)
        {
            KeyValuePair<string, long>[] duplicatePairs = [
                new("0", 999),
                new("1", 888)
            ];
            largeDictionary.AddRange(duplicatePairs);

            await Assert.That(largeDictionary.Count).IsEqualTo(count);
            await Assert.That(largeDictionary["0"]).IsEqualTo(999);
            await Assert.That(largeDictionary["1"]).IsEqualTo(888);
        }
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task AddRangeReadOnlySpan(long count)
    {
        // input check
        if (count < 0 || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        LargeDictionary<string, long> largeDictionary = [];

        // Test AddRange with ReadOnlySpan
        KeyValuePair<string, long>[] pairs = LargeEnumerable.Range(count)
            .Select(i => new KeyValuePair<string, long>(i.ToString(), i * 5))
            .ToArray();

        ReadOnlySpan<KeyValuePair<string, long>> span = pairs.AsSpan();
        largeDictionary.AddRange(span);

        await Assert.That(largeDictionary.Count).IsEqualTo(count);

        // Verify all items were added correctly
        for (long i = 0; i < count; i++)
        {
            string key = i.ToString();
            await Assert.That(largeDictionary.ContainsKey(key)).IsTrue();
            await Assert.That(largeDictionary[key]).IsEqualTo(i * 5);
        }

        // Test AddRange ReadOnlySpan with partial span
        if (count >= 4)
        {
            KeyValuePair<string, long>[] newPairs = [
                new("new1", 1000),
                new("new2", 2000)
            ];
            ReadOnlySpan<KeyValuePair<string, long>> partialSpan = newPairs.AsSpan();
            long countBefore = largeDictionary.Count;
            
            largeDictionary.AddRange(partialSpan);
            await Assert.That(largeDictionary.Count).IsEqualTo(countBefore + 2);
            await Assert.That(largeDictionary["new1"]).IsEqualTo(1000);
            await Assert.That(largeDictionary["new2"]).IsEqualTo(2000);
        }
#else
        // For .NET Standard 2.0, just complete the async method
        await Task.CompletedTask;
#endif
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task Remove(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        // Test 1: Initialize and fill dictionary with test data
        LargeDictionary<string, long> largeDictionary = [];

        // Test null parameter handling for Remove methods
        await Assert.That(() => largeDictionary.Remove((string)null))
            .Throws<ArgumentNullException>();
        await Assert.That(() => largeDictionary.Remove((string)null, out _))
            .Throws<ArgumentNullException>();
        await Assert.That(() => largeDictionary.Remove(new KeyValuePair<string, long>(null, 42)))
            .Throws<ArgumentNullException>();

        for (long i = 0; i < count; i++)
        {
            largeDictionary[i.ToString()] = i * 2;
        }

        await Assert.That(largeDictionary.Count).IsEqualTo(count);

        // Test 2: Test Remove(TKey key) - existing keys
        for (long i = 0; i < count / 2; i++)
        {
            string key = i.ToString();
            await Assert.That(largeDictionary.ContainsKey(key)).IsTrue();
            bool wasRemoved = largeDictionary.Remove(key);
            await Assert.That(wasRemoved).IsTrue();
            await Assert.That(largeDictionary.ContainsKey(key)).IsFalse();
            await Assert.That(largeDictionary.Count).IsEqualTo(count - i - 1);
        }

        // Test 3: Test Remove(TKey key) - non-existing keys
        for (long i = 0; i < count / 2; i++)
        {
            string key = i.ToString();
            long countBefore = largeDictionary.Count;
            bool wasRemoved = largeDictionary.Remove(key); // Already removed
            await Assert.That(wasRemoved).IsFalse();
            await Assert.That(largeDictionary.Count).IsEqualTo(countBefore);
        }

        // Test 4: Test Remove(TKey key) - keys that never existed
        long countBeforeNonExistent = largeDictionary.Count;
        bool wasRemoved1 = largeDictionary.Remove((count + 100).ToString());
        bool wasRemoved2 = largeDictionary.Remove((-1).ToString());
        await Assert.That(wasRemoved1).IsFalse();
        await Assert.That(wasRemoved2).IsFalse();
        await Assert.That(largeDictionary.Count).IsEqualTo(countBeforeNonExistent);

        // Test 5: Test Remove(KeyValuePair<TKey, TValue>) - exact match
        if (count > 0)
        {
            string testKey = (count / 2).ToString();
            if (largeDictionary.ContainsKey(testKey))
            {
                long testValue = largeDictionary[testKey];
                long countBefore = largeDictionary.Count;
                largeDictionary.Remove(new KeyValuePair<string, long>(testKey, testValue));
                await Assert.That(largeDictionary.Count).IsEqualTo(countBefore - 1);
                await Assert.That(largeDictionary.ContainsKey(testKey)).IsFalse();
            }
        }

        // Test 6: Test Remove(KeyValuePair<TKey, TValue>) - key exists but value doesn't match
        if (count > 0)
        {
            string remainingKey = (count - 1).ToString();
            if (largeDictionary.ContainsKey(remainingKey))
            {
                long correctValue = largeDictionary[remainingKey];
                long wrongValue = correctValue + 999;
                long countBefore = largeDictionary.Count;
                bool wasRemoved = largeDictionary.Remove(new KeyValuePair<string, long>(remainingKey, wrongValue));
                await Assert.That(wasRemoved).IsTrue(); // Should remove because key exists (value is ignored)
                await Assert.That(largeDictionary.Count).IsEqualTo(countBefore - 1); // Should decrease by 1
                await Assert.That(largeDictionary.ContainsKey(remainingKey)).IsFalse();
            }
        }

        // Test 7: Test Remove(TKey key) with multiple keys - new API returns bool
        List<string> keysToRemove = [];
        long remainingCount = largeDictionary.Count;

        foreach (string key in largeDictionary.Keys.Take(Math.Min(5, (int)remainingCount)))
        {
            keysToRemove.Add(key);
        }

        if (keysToRemove.Count > 0)
        {
            int removedCount = 0;
            foreach (string key in keysToRemove)
            {
                bool wasRemoved = largeDictionary.Remove(key);
                if (wasRemoved)
                {
                    removedCount++;
                }
                await Assert.That(wasRemoved).IsTrue(); // All keys should exist
            }
            await Assert.That(largeDictionary.Count).IsEqualTo(remainingCount - removedCount);

            foreach (string key in keysToRemove)
            {
                await Assert.That(largeDictionary.ContainsKey(key)).IsFalse();
            }
        }

        // Test 8: Test Remove(TKey key) with non-existing keys
        string[] nonExistingKeys = ["-1", "-2", (count + 100).ToString(), (count + 200).ToString()];
        long countBeforeNonExisting = largeDictionary.Count;

        foreach (string key in nonExistingKeys)
        {
            bool wasRemoved = largeDictionary.Remove(key);
            await Assert.That(wasRemoved).IsFalse(); // Should not remove non-existing keys
        }
        await Assert.That(largeDictionary.Count).IsEqualTo(countBeforeNonExisting);

        // Test 9: Test Remove(TKey key, out TValue removedValue)
        if (largeDictionary.Count > 0)
        {
            string existingKey = largeDictionary.Keys.First();
            long expectedValue = largeDictionary[existingKey];
            long countBefore = largeDictionary.Count;

            bool wasRemoved = largeDictionary.Remove(existingKey, out long removedValue);
            await Assert.That(wasRemoved).IsTrue();
            await Assert.That(removedValue).IsEqualTo(expectedValue);
            await Assert.That(largeDictionary.Count).IsEqualTo(countBefore - 1);
            await Assert.That(largeDictionary.ContainsKey(existingKey)).IsFalse();
        }

        // Test 10: Test removing all remaining items
        List<string> allRemainingKeys = largeDictionary.Keys.ToList();
        if (allRemainingKeys.Count > 0)
        {
            foreach (string key in allRemainingKeys)
            {
                bool wasRemoved = largeDictionary.Remove(key);
                await Assert.That(wasRemoved).IsTrue();
            }
            await Assert.That(largeDictionary.Count).IsEqualTo(0);
            await Assert.That(largeDictionary.Keys).IsEmpty();
            await Assert.That(largeDictionary.Values).IsEmpty();
        }
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task Enumeration(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        // Test 1: Initialize empty dictionary and test enumeration
        LargeDictionary<string, long> largeDictionary = [];

        await Assert.That(largeDictionary.Keys).IsEmpty();
        await Assert.That(largeDictionary.Values).IsEmpty();
        await Assert.That(largeDictionary).IsEmpty();

        // Test 2: Fill dictionary with test data
        for (long i = 0; i < count; i++)
        {
            largeDictionary[i.ToString()] = i * 2;
        }

        // Test 3: Test Keys enumeration
        List<string> enumeratedKeys = new(largeDictionary.Keys);

        await Assert.That((long)enumeratedKeys.Count).IsEqualTo(count);
        await Assert.That((long)enumeratedKeys.Distinct().Count()).IsEqualTo(count); // All keys should be unique
        await Assert.That(enumeratedKeys).IsEquivalentTo(LargeEnumerable.Range(count).Select(i => i.ToString()), CollectionOrdering.Any);

        // Test 4: Test Values enumeration
        List<long> enumeratedValues = new(largeDictionary.Values);
        await Assert.That((long)enumeratedValues.Count).IsEqualTo(count);
        await Assert.That(enumeratedValues).IsEquivalentTo(LargeEnumerable.Range(count).Select(i => i * 2), CollectionOrdering.Any);

        // Test 5: Test KeyValuePair enumeration (IEnumerable<KeyValuePair<TKey, TValue>>)
        List<KeyValuePair<string, long>> enumeratedPairs = new(largeDictionary);
        await Assert.That((long)enumeratedPairs.Count).IsEqualTo(count);
        await Assert.That(enumeratedPairs).IsEquivalentTo(LargeEnumerable.Range(count).Select(i => new KeyValuePair<string, long>(i.ToString(), i * 2)), CollectionOrdering.Any);

        // Test 6: Test enumeration after modifications
        if (count > 2)
        {
            largeDictionary.Remove("0");
            largeDictionary.Remove("1");

            await Assert.That((long)largeDictionary.Keys.Count()).IsEqualTo(count - 2);
            await Assert.That((long)largeDictionary.Values.Count()).IsEqualTo(count - 2);
            await Assert.That((long)largeDictionary.Count()).IsEqualTo(count - 2);

            await Assert.That(largeDictionary.Keys.Contains("0")).IsFalse();
            await Assert.That(largeDictionary.Keys.Contains("1")).IsFalse();
        }

        // Test 7: Test enumeration with duplicate values (different keys, same values)
        largeDictionary.Clear();
        if (count > 0)
        {
            for (long i = 0; i < count; i++)
            {
                largeDictionary[i.ToString()] = 42; // Same value for all keys
            }

            await Assert.That((long)largeDictionary.Keys.Count()).IsEqualTo(count);
            await Assert.That((long)largeDictionary.Values.Count()).IsEqualTo(count);
            await Assert.That((long)largeDictionary.Values.Distinct().Count()).IsEqualTo(1);
            await Assert.That(largeDictionary.Values.All(v => v == 42)).IsTrue();
        }

        // Test 8: Test multiple enumerations (should be consistent)
        List<string> firstEnumeration = largeDictionary.Keys.ToList();
        List<string> secondEnumeration = largeDictionary.Keys.ToList();
        await Assert.That(firstEnumeration).IsEquivalentTo(secondEnumeration, CollectionOrdering.Any);

        List<long> firstValueEnumeration = largeDictionary.Values.ToList();
        List<long> secondValueEnumeration = largeDictionary.Values.ToList();
        await Assert.That(firstValueEnumeration).IsEquivalentTo(secondValueEnumeration, CollectionOrdering.Any);
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task DoForEach(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        // Test 1: Test DoForEach on empty dictionary
        LargeDictionary<string, long> largeDictionary = [];

        long counter = 0;
        largeDictionary.DoForEach(kvp => counter++);
        await Assert.That(counter).IsEqualTo(0);

        // Test 2: Fill dictionary with test data
        for (long i = 0; i < count; i++)
        {
            largeDictionary[i.ToString()] = i * 2;
        }

        // Test 3: Test DoForEach - count all items
        counter = 0;
        largeDictionary.DoForEach(kvp => counter++);
        await Assert.That(counter).IsEqualTo(count);

        // Test 4: Test DoForEach - verify keys and values
        HashSet<string> visitedKeys = [];
        HashSet<long> visitedValues = [];
        largeDictionary.DoForEach(kvp =>
        {
            visitedKeys.Add(kvp.Key);
            visitedValues.Add(kvp.Value);
        });

        await Assert.That((long)visitedKeys.Count).IsEqualTo(count);
        await Assert.That((long)visitedValues.Count).IsEqualTo(count);
        await Assert.That(visitedKeys).IsEquivalentTo(LargeEnumerable.Range(count).Select(i => i.ToString()), CollectionOrdering.Any);
        await Assert.That(visitedValues).IsEquivalentTo(LargeEnumerable.Range(count).Select(i => i * 2), CollectionOrdering.Any);

        // Test 5: Test DoForEach - sum calculation
        long sum = 0;
        largeDictionary.DoForEach(kvp => sum += long.Parse(kvp.Key) + kvp.Value);
        long expectedSum = LargeEnumerable.Range(count).Sum() + LargeEnumerable.Range(count).Select(i => i * 2).Sum();
        await Assert.That(sum).IsEqualTo(expectedSum);

        // Test 6: Test DoForEach - conditional processing
        long evenKeyCount = 0;
        largeDictionary.DoForEach(kvp =>
        {
            if (long.Parse(kvp.Key) % 2 == 0)
                evenKeyCount++;
        });
        long expectedEvenCount = LargeEnumerable.Range(count).Count(i => i % 2 == 0);
        await Assert.That(evenKeyCount).IsEqualTo(expectedEvenCount);

        // Test 7: Test DoForEach - string concatenation (small counts only)
        if (count <= 10) // Only for small counts to avoid memory issues
        {
            List<string> results = [];
            largeDictionary.DoForEach(kvp => results.Add($"{kvp.Key}:{kvp.Value}"));
            await Assert.That((long)results.Count).IsEqualTo(count);
            await Assert.That(results).IsEquivalentTo(LargeEnumerable.Range(count).Select(i => $"{i}:{i * 2}"), CollectionOrdering.Any);
        }

        // Test 8: Test DoForEach with exception in action
        if (count > 0)
        {
            bool exceptionThrown = false;
            try
            {
                largeDictionary.DoForEach(kvp =>
                {
                    if (long.Parse(kvp.Key) == count / 2)
                        throw new InvalidOperationException("Test exception");
                });
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }
            await Assert.That(exceptionThrown).IsTrue();
        }

        // Test 9: Test DoForEach - early exit simulation (count until condition)
        long countUntilHalf = 0;
        largeDictionary.DoForEach(kvp =>
        {
            countUntilHalf++;
            if (long.Parse(kvp.Key) >= count / 2)
                return; // This doesn't actually exit early in DoForEach, but tests the action
        });
        await Assert.That(countUntilHalf).IsEqualTo(count);

        // Test 10: Test DoForEach after modifications
        if (count > 2)
        {
            largeDictionary.Remove("0");
            largeDictionary.Remove("1");

            counter = 0;
            largeDictionary.DoForEach(kvp => counter++);
            await Assert.That(counter).IsEqualTo(count - 2);

            HashSet<string> modifiedKeys = [];
            largeDictionary.DoForEach(kvp => modifiedKeys.Add(kvp.Key));
            await Assert.That(modifiedKeys.Contains("0")).IsFalse();
            await Assert.That(modifiedKeys.Contains("1")).IsFalse();
        }

        // Test 11: Test DoForEach with null action should throw
        if (count >= 0)
        {
            await Assert.That(() => largeDictionary.DoForEach(null!))
                .Throws<ArgumentNullException>();
        }

        // Test 12: Test DoForEach - duplicate values scenario
        largeDictionary.Clear();
        if (count > 0)
        {
            for (long i = 0; i < count; i++)
            {
                largeDictionary[i.ToString()] = 42; // Same value for all keys
            }

            HashSet<string> uniqueKeys = [];
            HashSet<long> uniqueValues = [];
            largeDictionary.DoForEach(kvp =>
            {
                uniqueKeys.Add(kvp.Key);
                uniqueValues.Add(kvp.Value);
            });

            await Assert.That((long)uniqueKeys.Count).IsEqualTo(count);
            await Assert.That((long)uniqueValues.Count).IsEqualTo(1);
            await Assert.That(uniqueValues.First()).IsEqualTo(42);
        }
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task Clear(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        // Test 1: Test Clear on empty dictionary
        LargeDictionary<string, long> largeDictionary = [];

        largeDictionary.Clear();
        await Assert.That(largeDictionary.Count).IsEqualTo(0);
        await Assert.That(largeDictionary.Keys).IsEmpty();
        await Assert.That(largeDictionary.Values).IsEmpty();

        // Test 2: Fill dictionary with test data
        for (long i = 0; i < count; i++)
        {
            largeDictionary[i.ToString()] = i * 2;
        }

        await Assert.That(largeDictionary.Count).IsEqualTo(count);

        // Test 3: Test Clear on populated dictionary
        largeDictionary.Clear();
        await Assert.That(largeDictionary.Count).IsEqualTo(0);
        await Assert.That(largeDictionary.Keys).IsEmpty();
        await Assert.That(largeDictionary.Values).IsEmpty();

        // Test 4: Test that dictionary is usable after Clear
        largeDictionary["100"] = 200;
        await Assert.That(largeDictionary.Count).IsEqualTo(1);
        await Assert.That(largeDictionary["100"]).IsEqualTo(200);

        // Test 5: Test multiple Clear calls
        largeDictionary.Clear();
        largeDictionary.Clear();
        await Assert.That(largeDictionary.Count).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task Shrink(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        // Test 1: Test Shrink on empty dictionary
        LargeDictionary<string, long> largeDictionary = [];

        long initialCapacity = largeDictionary.Capacity;
        largeDictionary.Shrink();
        await Assert.That(largeDictionary.Count).IsEqualTo(0);

        // Test 2: Fill dictionary with test data
        for (long i = 0; i < count; i++)
        {
            largeDictionary[i.ToString()] = i * 2;
        }

        long capacityBeforeShrink = largeDictionary.Capacity;
        await Assert.That(largeDictionary.Count).IsEqualTo(count);

        // Test 3: Test Shrink on populated dictionary
        largeDictionary.Shrink();
        await Assert.That(largeDictionary.Count).IsEqualTo(count);

        // Test 4: Verify all data is still accessible after shrink
        for (long i = 0; i < count; i++)
        {
            await Assert.That(largeDictionary.ContainsKey(i.ToString())).IsTrue();
            await Assert.That(largeDictionary[i.ToString()]).IsEqualTo(i * 2);
        }

        // Test 5: Test Shrink after removing many items
        if (count > 10)
        {
            // Remove 3/4 of items
            for (long i = 0; i < (count * 3) / 4; i++)
            {
                largeDictionary.Remove(i.ToString());
            }

            long remainingCount = largeDictionary.Count;
            largeDictionary.Shrink();
            await Assert.That(largeDictionary.Count).IsEqualTo(remainingCount);

            // Verify remaining data is still accessible
            for (long i = (count * 3) / 4; i < count; i++)
            {
                await Assert.That(largeDictionary.ContainsKey(i.ToString())).IsTrue();
                await Assert.That(largeDictionary[i.ToString()]).IsEqualTo(i * 2);
            }
        }
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task Contains(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        // Test null parameter validation
        LargeDictionary<string, long> testDict = [];
        await Assert.That(() => testDict.ContainsKey(null!))
            .Throws<ArgumentNullException>();

        // Test 1: Test Contains on empty dictionary
        LargeDictionary<string, long> largeDictionary = [];

        await Assert.That(largeDictionary.Contains(new KeyValuePair<string, long>("0", 0))).IsFalse();
        await Assert.That(largeDictionary.ContainsKey("0")).IsFalse();

        // Test 2: Fill dictionary with test data
        for (long i = 0; i < count; i++)
        {
            largeDictionary[i.ToString()] = i * 2;
        }

        // Test 3: Test Contains with exact key-value pairs
        for (long i = 0; i < count; i++)
        {
            await Assert.That(largeDictionary.Contains(new KeyValuePair<string, long>(i.ToString(), i * 2))).IsTrue();
        }

        // Test 4: Test Contains with existing key but wrong value
        if (count > 0)
        {
            await Assert.That(largeDictionary.Contains(new KeyValuePair<string, long>("0", 999))).IsFalse();
            await Assert.That(largeDictionary.Contains(new KeyValuePair<string, long>((count / 2).ToString(), 999))).IsFalse();
        }

        // Test 5: Test Contains with non-existing key
        await Assert.That(largeDictionary.Contains(new KeyValuePair<string, long>((count + 100).ToString(), 999))).IsFalse();
        await Assert.That(largeDictionary.Contains(new KeyValuePair<string, long>("-1", 999))).IsFalse();

        // Test 6: Test ContainsKey
        for (long i = 0; i < count; i++)
        {
            await Assert.That(largeDictionary.ContainsKey(i.ToString())).IsTrue();
        }

        await Assert.That(largeDictionary.ContainsKey((count + 100).ToString())).IsFalse();
        await Assert.That(largeDictionary.ContainsKey("-1")).IsFalse();

        // Test 7: Test Contains after modifications
        if (count > 2)
        {
            largeDictionary.Remove("0");
            await Assert.That(largeDictionary.Contains(new KeyValuePair<string, long>("0", 0))).IsFalse();
            await Assert.That(largeDictionary.ContainsKey("0")).IsFalse();

            largeDictionary["1"] = 999; // Change value
            await Assert.That(largeDictionary.Contains(new KeyValuePair<string, long>("1", 2))).IsFalse(); // Old value
            await Assert.That(largeDictionary.Contains(new KeyValuePair<string, long>("1", 999))).IsTrue(); // New value
            await Assert.That(largeDictionary.ContainsKey("1")).IsTrue();
        }
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task GetAndTryGetValue(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        // Test null parameter validation
        LargeDictionary<string, long> testDict = [];
        await Assert.That(() => testDict.Get(null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => testDict.TryGetValue(null!, out _))
            .Throws<ArgumentNullException>();

        // Test 1: Test Get and TryGetValue on empty dictionary
        LargeDictionary<string, long> largeDictionary = [];

        bool found = largeDictionary.TryGetValue("0", out long value);
        await Assert.That(found).IsFalse();
        await Assert.That(value).IsEqualTo(default(long));

        if (count == 0)
        {
            bool exceptionThrown = false;
            try
            {
                largeDictionary.Get("0");
            }
            catch (KeyNotFoundException)
            {
                exceptionThrown = true;
            }
            await Assert.That(exceptionThrown).IsTrue();
        }

        // Test 2: Fill dictionary with test data
        for (long i = 0; i < count; i++)
        {
            largeDictionary[i.ToString()] = i * 2;
        }

        // Test 3: Test Get method
        for (long i = 0; i < count; i++)
        {
            long retrievedValue = largeDictionary.Get(i.ToString());
            await Assert.That(retrievedValue).IsEqualTo(i * 2);
        }

        // Test 4: Test Get with non-existing key
        if (count >= 0)
        {
            bool exceptionThrown = false;
            try
            {
                largeDictionary.Get((count + 100).ToString());
            }
            catch (KeyNotFoundException)
            {
                exceptionThrown = true;
            }
            await Assert.That(exceptionThrown).IsTrue();
        }

        // Test 5: Test TryGetValue method
        for (long i = 0; i < count; i++)
        {
            bool success = largeDictionary.TryGetValue(i.ToString(), out long retrievedValue);
            await Assert.That(success).IsTrue();
            await Assert.That(retrievedValue).IsEqualTo(i * 2);
        }

        // Test 6: Test TryGetValue with non-existing keys
        found = largeDictionary.TryGetValue((count + 100).ToString(), out value);
        await Assert.That(found).IsFalse();
        await Assert.That(value).IsEqualTo(default(long));

        found = largeDictionary.TryGetValue("-1", out value);
        await Assert.That(found).IsFalse();
        await Assert.That(value).IsEqualTo(default(long));

        // Test 7: Test indexer getter
        for (long i = 0; i < count; i++)
        {
            await Assert.That(largeDictionary[i.ToString()]).IsEqualTo(i * 2);
        }

        // Test 8: Test indexer with non-existing key
        if (count >= 0)
        {
            bool exceptionThrown = false;
            try
            {
                long nonExistentValue = largeDictionary[(count + 100).ToString()];
            }
            catch (KeyNotFoundException)
            {
                exceptionThrown = true;
            }
            await Assert.That(exceptionThrown).IsTrue();
        }

        // Test 9: Test indexer getter with null key
        await Assert.That(() => largeDictionary[null!])
            .Throws<ArgumentNullException>();
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task SetAndIndexer(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        // Test null parameter validation
        LargeDictionary<string, long> testDict = [];
        await Assert.That(() => testDict.Set(null!, 42))
            .Throws<ArgumentNullException>();
        await Assert.That(() => testDict[null!] = 42)
            .Throws<ArgumentNullException>();

        // Test 1: Test Set method
        LargeDictionary<string, long> largeDictionary = [];

        for (long i = 0; i < count; i++)
        {
            largeDictionary.Set(i.ToString(), i * 3);
        }

        await Assert.That(largeDictionary.Count).IsEqualTo(count);

        for (long i = 0; i < count; i++)
        {
            await Assert.That(largeDictionary[i.ToString()]).IsEqualTo(i * 3);
        }

        // Test 2: Test Set overwriting existing values
        for (long i = 0; i < count; i++)
        {
            largeDictionary.Set(i.ToString(), i * 5);
        }

        await Assert.That(largeDictionary.Count).IsEqualTo(count); // Count should remain same
        for (long i = 0; i < count; i++)
        {
            await Assert.That(largeDictionary[i.ToString()]).IsEqualTo(i * 5);
        }

        // Test 3: Test indexer setter
        for (long i = 0; i < count; i++)
        {
            largeDictionary[i.ToString()] = i * 7;
        }

        await Assert.That(largeDictionary.Count).IsEqualTo(count);
        for (long i = 0; i < count; i++)
        {
            await Assert.That(largeDictionary[i.ToString()]).IsEqualTo(i * 7);
        }

        // Test 4: Test adding new keys with indexer
        if (count < Constants.MaxLargeCollectionCount - 10)
        {
            for (long i = count; i < count + 5; i++)
            {
                largeDictionary[i.ToString()] = i * 10;
            }

            await Assert.That(largeDictionary.Count).IsEqualTo(count + 5);
            for (long i = count; i < count + 5; i++)
            {
                await Assert.That(largeDictionary[i.ToString()]).IsEqualTo(i * 10);
            }
        }

        // Test 5: Test with default values
        if (count > 0)
        {
            largeDictionary.Set("0", default(long));
            await Assert.That(largeDictionary["0"]).IsEqualTo(default(long));
            await Assert.That(largeDictionary.ContainsKey("0")).IsTrue();
        }
    }

    [Test]
    public async Task ConstructorWithItems()
    {
        // Test null parameter validation
        await Assert.That(() => new LargeDictionary<string, long>((IEnumerable<KeyValuePair<string, long>>)null!))
            .Throws<ArgumentNullException>();

        // Test 1: Test constructor with empty collection
        KeyValuePair<string, long>[] emptyItems = [];
        LargeDictionary<string, long> largeDictionary1 = new(emptyItems);
        await Assert.That(largeDictionary1.Count).IsEqualTo(0);

        // Test 2: Test constructor with items
        KeyValuePair<string, long>[] items = [
            new("1", 10),
            new("2", 20),
            new("3", 30)
        ];
        LargeDictionary<string, long> largeDictionary2 = new(items);
        await Assert.That(largeDictionary2.Count).IsEqualTo(3);
        await Assert.That(largeDictionary2["1"]).IsEqualTo(10);
        await Assert.That(largeDictionary2["2"]).IsEqualTo(20);
        await Assert.That(largeDictionary2["3"]).IsEqualTo(30);

        // Test 3: Test constructor with duplicate keys (should keep last value)
        KeyValuePair<string, long>[] itemsWithDuplicates = [
            new("1", 10),
            new("2", 20),
            new("1", 100) // Duplicate key
        ];
        LargeDictionary<string, long> largeDictionary3 = new(itemsWithDuplicates);
        await Assert.That(largeDictionary3.Count).IsEqualTo(2);
        await Assert.That(largeDictionary3["1"]).IsEqualTo(100); // Should have last value
        await Assert.That(largeDictionary3["2"]).IsEqualTo(20);

        // Test 4: Test constructor with null key in items should throw
        KeyValuePair<string, long>[] itemsWithNullKey = [
            new("1", 10),
            new(null!, 20) // Null key
        ];
        await Assert.That(() => new LargeDictionary<string, long>(itemsWithNullKey))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task CustomEqualityAndHashCode()
    {
        // Test 1: Test with custom key equality function
        Func<string, string, bool> caseInsensitiveEquals = (a, b) =>
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        Func<string, int> caseInsensitiveHash = key =>
            key?.ToUpperInvariant().GetHashCode() ?? 0;

        LargeDictionary<string, int> largeDictionary = new(
            caseInsensitiveEquals,
            caseInsensitiveHash);

        largeDictionary["Hello"] = 1;
        largeDictionary["WORLD"] = 2;

        // Test 2: Test case-insensitive behavior
        await Assert.That(largeDictionary.ContainsKey("hello")).IsTrue();
        await Assert.That(largeDictionary.ContainsKey("HELLO")).IsTrue();
        await Assert.That(largeDictionary.ContainsKey("world")).IsTrue();
        await Assert.That(largeDictionary["hello"]).IsEqualTo(1);
        await Assert.That(largeDictionary["WORLD"]).IsEqualTo(2);

        // Test 3: Overwriting with different case should update value
        largeDictionary["HELLO"] = 100;
        await Assert.That(largeDictionary["hello"]).IsEqualTo(100);
        await Assert.That(largeDictionary.Count).IsEqualTo(2);
    }

    [Test]
    [MethodDataSource(typeof(LargeArrayTest), nameof(LargeArrayTest.CapacitiesTestCasesArguments))]
    public async Task KeysAndValuesProperties(long count)
    {
        if (count < 0L || count > Constants.MaxLargeCollectionCount)
        {
            return;
        }

        // Test 1: Initialize dictionary with test data
        LargeDictionary<string, long> largeDictionary = [];

        // Add test data
        for (long i = 0; i < count; i++)
        {
            largeDictionary[i.ToString()] = i * 2;
        }

        // Test 2: Keys property enumeration
        List<string> keys = largeDictionary.Keys.ToList();
        await Assert.That(keys.Count).IsEqualTo((int)count);

        // Test 3: Values property enumeration
        List<long> values = largeDictionary.Values.ToList();
        await Assert.That(values.Count).IsEqualTo((int)count);

        // Test 4: Verify all keys are present
        for (long i = 0; i < count; i++)
        {
            await Assert.That(keys.Contains(i.ToString())).IsTrue();
        }

        // Test 5: Verify all values are present
        for (long i = 0; i < count; i++)
        {
            await Assert.That(values.Contains(i * 2)).IsTrue();
        }

        // Test 6: Empty dictionary should have empty Keys and Values
        LargeDictionary<string, int> emptyDict = [];
        await Assert.That(emptyDict.Keys.Any()).IsFalse();
        await Assert.That(emptyDict.Values.Any()).IsFalse();
    }

    [Test]
    public async Task ExceptionHandling()
    {
        LargeDictionary<string, int> largeDictionary = [];

        // Test 1: KeyNotFoundException for Get() with non-existent key
        await Assert.That(() => largeDictionary.Get("nonexistent")).Throws<KeyNotFoundException>();

        // Test 2: KeyNotFoundException for indexer with non-existent key
        await Assert.That(() => largeDictionary["nonexistent"]).Throws<KeyNotFoundException>();

        // Test 3: ArgumentOutOfRangeException for invalid capacity
        await Assert.That(() => new LargeDictionary<string, int>(capacity: -1L))
            .Throws<ArgumentOutOfRangeException>();

        // Test 4: ArgumentOutOfRangeException for invalid maxLoadFactor
        await Assert.That(() => new LargeDictionary<string, int>(maxLoadFactor: -0.1))
            .Throws<ArgumentOutOfRangeException>();

        // Test 5: ArgumentOutOfRangeException for invalid minLoadFactor (negative)
        await Assert.That(() => new LargeDictionary<string, int>(minLoadFactor: -0.1))
            .Throws<ArgumentOutOfRangeException>();

        // Test 6: ArgumentOutOfRangeException for minLoadFactor > maxLoadFactor
        await Assert.That(() => new LargeDictionary<string, int>(minLoadFactor: 0.8, maxLoadFactor: 0.5))
            .Throws<ArgumentOutOfRangeException>();

        // Test 7: ArgumentOutOfRangeException for invalid capacityGrowFactor
        await Assert.That(() => new LargeDictionary<string, int>(capacityGrowFactor: 0.5))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ConstructorParameterTests()
    {
        // Test 1: Constructor with custom capacity
        LargeDictionary<int, string> dict1 = new(capacity: 100L);
        await Assert.That(dict1.Capacity).IsGreaterThanOrEqualTo(100L);
        await Assert.That(dict1.Count).IsEqualTo(0L);

        // Test 2: Constructor with custom load factors
        LargeDictionary<int, string> dict2 = new(
            minLoadFactor: 0.2,
            maxLoadFactor: 0.6);
        await Assert.That(dict2.Count).IsEqualTo(0L);

        // Test 3: Constructor with custom growth parameters
        LargeDictionary<int, string> dict3 = new(
            capacityGrowFactor: 2.5,
            fixedCapacityGrowAmount: 50L,
            fixedCapacityGrowLimit: 1000L);
        await Assert.That(dict3.Count).IsEqualTo(0L);

        // Test 4: Constructor with IEnumerable and custom parameters
        KeyValuePair<string, int>[] items = [
            new("key1", 1),
            new("key2", 2)
        ];

        LargeDictionary<string, int> dict4 = new(
            items,
            capacity: 50L,
            maxLoadFactor: 0.7);

        await Assert.That(dict4.Count).IsEqualTo(2L);
        await Assert.That(dict4["key1"]).IsEqualTo(1);
        await Assert.That(dict4["key2"]).IsEqualTo(2);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        // Test 5: Constructor with ReadOnlySpan and custom parameters
        ReadOnlySpan<KeyValuePair<string, int>> spanItems = items.AsSpan();
        
        LargeDictionary<string, int> dict5 = new(
            spanItems,
            capacity: 50L,
            maxLoadFactor: 0.7);
        
        await Assert.That(dict5.Count).IsEqualTo(2L);
        await Assert.That(dict5["key1"]).IsEqualTo(1);
        await Assert.That(dict5["key2"]).IsEqualTo(2);
#endif
    }

    [Test]
    public async Task HashCollisionHandling()
    {
        // Test 1: Custom hash function that creates intentional collisions
        Func<string, int> collisionHash = key => key?.Length ?? 0;

        LargeDictionary<string, int> largeDictionary = new(
            keyEqualsFunction: (a, b) => string.Equals(a, b, StringComparison.Ordinal),
            hashCodeFunction: collisionHash);

        // Test 2: Add items with same hash but different keys
        largeDictionary["a"] = 1;     // hash = 1
        largeDictionary["b"] = 2;     // hash = 1 (collision!)
        largeDictionary["c"] = 3;     // hash = 1 (collision!)
        largeDictionary["ab"] = 4;    // hash = 2
        largeDictionary["abc"] = 5;   // hash = 3

        // Test 3: Verify all items are stored correctly despite collisions
        await Assert.That(largeDictionary.Count).IsEqualTo(5);
        await Assert.That(largeDictionary["a"]).IsEqualTo(1);
        await Assert.That(largeDictionary["b"]).IsEqualTo(2);
        await Assert.That(largeDictionary["c"]).IsEqualTo(3);
        await Assert.That(largeDictionary["ab"]).IsEqualTo(4);
        await Assert.That(largeDictionary["abc"]).IsEqualTo(5);

        // Test 4: Test removal with collisions
        bool removed = largeDictionary.Remove("b");
        await Assert.That(removed).IsTrue();
        await Assert.That(largeDictionary.Count).IsEqualTo(4);
        await Assert.That(largeDictionary.ContainsKey("b")).IsFalse();

        // Test 5: Verify other colliding items still work
        await Assert.That(largeDictionary["a"]).IsEqualTo(1);
        await Assert.That(largeDictionary["c"]).IsEqualTo(3);

        // Test 6: Test updating with collisions
        largeDictionary["a"] = 100;
        await Assert.That(largeDictionary["a"]).IsEqualTo(100);
        await Assert.That(largeDictionary["c"]).IsEqualTo(3); // Should be unchanged
    }

    [Test]
    public async Task LoadFactorManagement()
    {
        // Test 1: Create dictionary with low maxLoadFactor to force resizing
        LargeDictionary<int, string> largeDictionary = new(
            capacity: 4L,
            minLoadFactor: 0.3,
            maxLoadFactor: 0.6); // Force resize at around 2-3 items

        // Test 2: Add items up to load factor limit
        largeDictionary[1] = "one";
        largeDictionary[2] = "two";
        long capacityBefore = largeDictionary.Capacity;

        // Test 3: Adding another item should trigger resize
        largeDictionary[3] = "three";

        await Assert.That(largeDictionary.Capacity).IsGreaterThan(capacityBefore);
        await Assert.That(largeDictionary.Count).IsEqualTo(3L);
        await Assert.That(largeDictionary[1]).IsEqualTo("one");
        await Assert.That(largeDictionary[2]).IsEqualTo("two");
        await Assert.That(largeDictionary[3]).IsEqualTo("three");

        // Test 4: Test dictionary with default load factors and manual shrinking
        LargeDictionary<int, string> shrinkDict = new(capacity: 100L);

        // Fill dictionary
        for (int i = 0; i < 50; i++)
        {
            shrinkDict[i] = $"value{i}";
        }

        long capacityAfterFill = shrinkDict.Capacity;

        // Remove most items 
        for (int i = 0; i < 45; i++)
        {
            shrinkDict.Remove(i);
        }

        // Manually trigger shrink
        shrinkDict.Shrink();

        await Assert.That(shrinkDict.Count).IsEqualTo(5L);
        // After shrinking, capacity should accommodate remaining items efficiently
        await Assert.That(shrinkDict.Capacity).IsGreaterThanOrEqualTo(1L); // At least some capacity for the 5 items
    }

    [Test]
    public async Task EdgeCasesAndBoundaryConditions()
    {
        // Test 1: Empty dictionary operations
        LargeDictionary<string, int> emptyDict = [];

        await Assert.That(emptyDict.Count).IsEqualTo(0L);
        await Assert.That(emptyDict.ContainsKey("any")).IsFalse();
        await Assert.That(emptyDict.TryGetValue("any", out _)).IsFalse();
        await Assert.That(emptyDict.Remove("any")).IsFalse();

        // Test 2: Single item operations
        LargeDictionary<string, int> singleDict = [];
        singleDict["single"] = 42;

        await Assert.That(singleDict.Count).IsEqualTo(1L);
        await Assert.That(singleDict["single"]).IsEqualTo(42);
        await Assert.That(singleDict.ContainsKey("single")).IsTrue();

        bool removed = singleDict.Remove("single");
        await Assert.That(removed).IsTrue();
        await Assert.That(singleDict.Count).IsEqualTo(0L);

        // Test 3: Null value handling (null values should be allowed)
        LargeDictionary<string, string> nullValueDict = [];
        nullValueDict["key"] = null;

        await Assert.That(nullValueDict.Count).IsEqualTo(1L);
        await Assert.That(nullValueDict["key"]).IsNull();
        await Assert.That(nullValueDict.ContainsKey("key")).IsTrue();

        bool foundNull = nullValueDict.TryGetValue("key", out string nullValue);
        await Assert.That(foundNull).IsTrue();
        await Assert.That(nullValue).IsNull();

        // Test 4: Large key operations
        LargeDictionary<string, int> largeKeyDict = [];
        string largeKey = new('x', 10000); // 10k character key

        largeKeyDict[largeKey] = 123;
        await Assert.That(largeKeyDict[largeKey]).IsEqualTo(123);
        await Assert.That(largeKeyDict.ContainsKey(largeKey)).IsTrue();

        // Test 5: Key replacement behavior
        LargeDictionary<string, int> replaceDict = [];
        replaceDict["key"] = 1;
        replaceDict["key"] = 2; // Replace

        await Assert.That(replaceDict.Count).IsEqualTo(1L);
        await Assert.That(replaceDict["key"]).IsEqualTo(2);

        // Test 6: Multiple operations on same key
        LargeDictionary<int, string> multiOpDict = [];

        multiOpDict[42] = "initial";
        await Assert.That(multiOpDict.ContainsKey(42)).IsTrue();

        multiOpDict.Set(42, "updated");
        await Assert.That(multiOpDict[42]).IsEqualTo("updated");

        bool removedWithValue = multiOpDict.Remove(42, out string removedValue);
        await Assert.That(removedWithValue).IsTrue();
        await Assert.That(removedValue).IsEqualTo("updated");
        await Assert.That(multiOpDict.ContainsKey(42)).IsFalse();
    }
}
