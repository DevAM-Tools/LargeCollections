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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LargeCollections.Test;

public class LargeEnumerableTest
{
    [Test]
    public async Task Range_WithStartCountStep_ReturnsCorrectSequence()
    {
        List<long> result = LargeEnumerable.Range(5, 3, 2).ToList();
        await Assert.That(result).IsEquivalentTo(new long[] { 5, 7, 9 });
    }

    [Test]
    public async Task Range_WithStartCountStep_ZeroCount_ReturnsEmpty()
    {
        List<long> result = LargeEnumerable.Range(0, 0, 1).ToList();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Range_WithStartCountStep_NegativeStep_ReturnsCorrectSequence()
    {
        List<long> result = LargeEnumerable.Range(10, 3, -2).ToList();
        await Assert.That(result).IsEquivalentTo(new long[] { 10, 8, 6 });
    }

    [Test]
    public async Task Range_WithStartCountStep_LargeValues_ReturnsCorrectSequence()
    {
        List<long> result = LargeEnumerable.Range(1000000000L, 3, 1000000000L).ToList();
        await Assert.That(result).IsEquivalentTo(new long[] { 1000000000L, 2000000000L, 3000000000L });
    }

    [Test]
    public async Task Range_WithEnd_ReturnsCorrectSequence()
    {
        List<long> result = LargeEnumerable.Range(5).ToList();
        await Assert.That(result).IsEquivalentTo(new long[] { 0, 1, 2, 3, 4 });
    }

    [Test]
    public async Task Range_WithEnd_Zero_ReturnsEmpty()
    {
        List<long> result = LargeEnumerable.Range(0).ToList();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Range_WithEnd_NegativeValue_ReturnsEmpty()
    {
        List<long> result = LargeEnumerable.Range(-5).ToList();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Repeat_WithPositiveCount_ReturnsCorrectSequence()
    {
        List<string> result = LargeEnumerable.Repeat("test", 3).ToList();
        await Assert.That(result).IsEquivalentTo(new[] { "test", "test", "test" });
    }

    [Test]
    public async Task Repeat_WithZeroCount_ReturnsEmpty()
    {
        List<int> result = LargeEnumerable.Repeat(42, 0).ToList();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Repeat_WithNegativeCount_ReturnsEmpty()
    {
        List<int> result = LargeEnumerable.Repeat(42, -5).ToList();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Repeat_WithNullValue_ReturnsCorrectSequence()
    {
        List<string> result = LargeEnumerable.Repeat<string>(null, 2).ToList();
        await Assert.That(result).IsEquivalentTo(new string[] { null, null });
    }

    [Test]
    public async Task Repeat_WithLargeCount_FirstFewElements()
    {
        List<int> result = LargeEnumerable.Repeat(123, 1000000L).Take(3).ToList();
        await Assert.That(result).IsEquivalentTo(new[] { 123, 123, 123 });
    }
}