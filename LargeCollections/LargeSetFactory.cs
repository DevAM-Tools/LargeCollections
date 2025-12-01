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

namespace LargeCollections;

/// <summary>
/// Factory class for creating <see cref="LargeSet{T, TComparer}"/> instances with convenient methods.
/// </summary>
public static class LargeSet
{
    /// <summary>
    /// Creates a new LargeSet with the default equality comparer (uses <see cref="System.Collections.Generic.EqualityComparer{T}.Default"/>).
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    /// <param name="capacity">Initial bucket capacity.</param>
    /// <param name="capacityGrowFactor">Factor by which capacity grows when needed.</param>
    /// <param name="fixedCapacityGrowAmount">Fixed amount to grow capacity by for small sets.</param>
    /// <param name="fixedCapacityGrowLimit">Capacity limit below which fixed growth is used.</param>
    /// <param name="minLoadFactor">Minimum load factor before shrinking.</param>
    /// <param name="maxLoadFactor">Maximum load factor before growing.</param>
    /// <param name="minLoadFactorTolerance">Tolerance for minimum load factor.</param>
    /// <returns>A new <see cref="LargeSet{T, TComparer}"/> with <see cref="ObjectEqualityComparer{T}"/>.</returns>
    public static LargeSet<T, ObjectEqualityComparer<T>> Create<T>(
        long capacity = 1L,
        double capacityGrowFactor = Constants.DefaultCapacityGrowFactor,
        long fixedCapacityGrowAmount = Constants.DefaultFixedCapacityGrowAmount,
        long fixedCapacityGrowLimit = Constants.DefaultFixedCapacityGrowLimit,
        double minLoadFactor = Constants.DefaultMinLoadFactor,
        double maxLoadFactor = Constants.DefaultMaxLoadFactor,
        double minLoadFactorTolerance = Constants.DefaultMinLoadFactorTolerance)
    {
        return new LargeSet<T, ObjectEqualityComparer<T>>(
            new ObjectEqualityComparer<T>(),
            capacity,
            capacityGrowFactor,
            fixedCapacityGrowAmount,
            fixedCapacityGrowLimit,
            minLoadFactor,
            maxLoadFactor,
            minLoadFactorTolerance);
    }

    /// <summary>
    /// Creates a new LargeSet with custom equality functions using a <see cref="DelegateEqualityComparer{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    /// <param name="equalsFunction">The function to compare two items for equality.</param>
    /// <param name="hashCodeFunction">The function to compute the hash code of an item.</param>
    /// <param name="capacity">Initial bucket capacity.</param>
    /// <param name="capacityGrowFactor">Factor by which capacity grows when needed.</param>
    /// <param name="fixedCapacityGrowAmount">Fixed amount to grow capacity by for small sets.</param>
    /// <param name="fixedCapacityGrowLimit">Capacity limit below which fixed growth is used.</param>
    /// <param name="minLoadFactor">Minimum load factor before shrinking.</param>
    /// <param name="maxLoadFactor">Maximum load factor before growing.</param>
    /// <param name="minLoadFactorTolerance">Tolerance for minimum load factor.</param>
    /// <returns>A new <see cref="LargeSet{T, TComparer}"/> with <see cref="DelegateEqualityComparer{T}"/>.</returns>
    public static LargeSet<T, DelegateEqualityComparer<T>> Create<T>(
        Func<T, T, bool> equalsFunction,
        Func<T, int> hashCodeFunction,
        long capacity = 1L,
        double capacityGrowFactor = Constants.DefaultCapacityGrowFactor,
        long fixedCapacityGrowAmount = Constants.DefaultFixedCapacityGrowAmount,
        long fixedCapacityGrowLimit = Constants.DefaultFixedCapacityGrowLimit,
        double minLoadFactor = Constants.DefaultMinLoadFactor,
        double maxLoadFactor = Constants.DefaultMaxLoadFactor,
        double minLoadFactorTolerance = Constants.DefaultMinLoadFactorTolerance)
    {
        if (equalsFunction is null)
        {
            throw new ArgumentNullException(nameof(equalsFunction));
        }
        if (hashCodeFunction is null)
        {
            throw new ArgumentNullException(nameof(hashCodeFunction));
        }

        return new LargeSet<T, DelegateEqualityComparer<T>>(
            new DelegateEqualityComparer<T>(equalsFunction, hashCodeFunction),
            capacity,
            capacityGrowFactor,
            fixedCapacityGrowAmount,
            fixedCapacityGrowLimit,
            minLoadFactor,
            maxLoadFactor,
            minLoadFactorTolerance);
    }
}
