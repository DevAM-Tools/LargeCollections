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

        // Test 6: Test AddRange with empty collection
        largeDictionary.AddRange([]);
        await Assert.That(largeDictionary.Count).IsEqualTo(count);

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
            largeDictionary.Remove(key);
            await Assert.That(largeDictionary.ContainsKey(key)).IsFalse();
            await Assert.That(largeDictionary.Count).IsEqualTo(count - i - 1);
        }

        // Test 3: Test Remove(TKey key) - non-existing keys
        for (long i = 0; i < count / 2; i++)
        {
            string key = i.ToString();
            long countBefore = largeDictionary.Count;
            largeDictionary.Remove(key); // Already removed
            await Assert.That(largeDictionary.Count).IsEqualTo(countBefore);
        }

        // Test 4: Test Remove(TKey key) - keys that never existed
        long countBeforeNonExistent = largeDictionary.Count;
        largeDictionary.Remove((count + 100).ToString());
        largeDictionary.Remove((-1).ToString());
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
                largeDictionary.Remove(new KeyValuePair<string, long>(remainingKey, wrongValue));
                await Assert.That(largeDictionary.Count).IsEqualTo(countBefore); // Should not remove
                await Assert.That(largeDictionary.ContainsKey(remainingKey)).IsTrue();
                await Assert.That(largeDictionary[remainingKey]).IsEqualTo(correctValue);
            }
        }

        // Test 7: Test Remove(IEnumerable<TKey> keys) - multiple keys at once
        List<string> keysToRemove = [];
        long remainingCount = largeDictionary.Count;

        foreach (string key in largeDictionary.Keys.Take(Math.Min(5, (int)remainingCount)))
        {
            keysToRemove.Add(key);
        }

        if (keysToRemove.Count > 0)
        {
            largeDictionary.Remove(keysToRemove);
            await Assert.That(largeDictionary.Count).IsEqualTo(remainingCount - keysToRemove.Count);

            foreach (string key in keysToRemove)
            {
                await Assert.That(largeDictionary.ContainsKey(key)).IsFalse();
            }
        }

        // Test 8: Test Remove(IEnumerable<TKey> keys) - empty collection
        long countBeforeEmpty = largeDictionary.Count;
        largeDictionary.Remove([]);
        await Assert.That(largeDictionary.Count).IsEqualTo(countBeforeEmpty);

        // Test 9: Test Remove(IEnumerable<TKey> keys) - non-existing keys
        long countBeforeNonExisting = largeDictionary.Count;
        largeDictionary.Remove(["-1", "-2", (count + 100).ToString(), (count + 200).ToString()]);
        await Assert.That(largeDictionary.Count).IsEqualTo(countBeforeNonExisting);

        // Test 10: Test Remove(IEnumerable<TKey> keys) - mix of existing and non-existing keys
        List<string> mixedKeys = [];
        int existingKeysCount = 0;

        foreach (string key in largeDictionary.Keys.Take(3))
        {
            mixedKeys.Add(key);
            existingKeysCount++;
        }
        mixedKeys.Add("-999"); // Non-existing key
        mixedKeys.Add((count + 500).ToString()); // Non-existing key

        if (mixedKeys.Count > 0)
        {
            long countBeforeMixed = largeDictionary.Count;
            largeDictionary.Remove(mixedKeys);
            await Assert.That(largeDictionary.Count).IsEqualTo(countBeforeMixed - existingKeysCount);
        }

        // Test 11: Test removing all remaining items
        List<string> allRemainingKeys = largeDictionary.Keys.ToList();
        if (allRemainingKeys.Count > 0)
        {
            largeDictionary.Remove(allRemainingKeys);
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
}
