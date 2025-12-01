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
using System.Collections.Generic;

namespace LargeCollections.Observable;

/// <summary>
/// Extension methods for creating filtered and sorted views over observable collections.
/// All views use the unified <see cref="FilteredSortedReadOnlyLargeObservableCollection{T, TPredicate, TComparer}"/>
/// with <see cref="NoFilter{T}"/> or <see cref="NoSort{T}"/> for optimal JIT elimination of unused operations.
/// </summary>
public static class ObservableCollectionExtensions
{
    #region Filter Only (uses NoSort<T> - JIT eliminates sort operation)

    /// <summary>
    /// Creates a filtered view over the observable collection using a struct predicate for optimal performance.
    /// Uses <see cref="NoSort{T}"/> internally - the JIT compiler eliminates the sort operation entirely.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <typeparam name="TPredicate">The predicate type. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="predicate">The predicate used to filter items.</param>
    /// <returns>A filtered view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, NoSort<T>> CreateFilteredView<T, TPredicate>(
        this IReadOnlyLargeObservableCollection<T> source,
        TPredicate predicate)
        where TPredicate : ILargePredicate<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, NoSort<T>>(source, predicate, default);
    }

    /// <summary>
    /// Creates a filtered view over the observable collection using a struct predicate for optimal performance.
    /// Uses <see cref="NoSort{T}"/> internally - the JIT compiler eliminates the sort operation entirely.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <typeparam name="TPredicate">The predicate type. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="predicate">The predicate used to filter items.</param>
    /// <param name="suppressEventExceptions">Whether to suppress exceptions from event handlers.</param>
    /// <returns>A filtered view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, NoSort<T>> CreateFilteredView<T, TPredicate>(
        this IReadOnlyLargeObservableCollection<T> source,
        TPredicate predicate,
        bool suppressEventExceptions)
        where TPredicate : ILargePredicate<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, NoSort<T>>(source, predicate, default, suppressEventExceptions);
    }

    /// <summary>
    /// Creates a filtered view over the observable collection using a delegate predicate.
    /// For maximum performance, use the struct-based overload with a custom predicate type.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="predicate">The predicate delegate used to filter items.</param>
    /// <returns>A filtered view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, DelegatePredicate<T>, NoSort<T>> CreateFilteredView<T>(
        this IReadOnlyLargeObservableCollection<T> source,
        Func<T, bool> predicate)
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, DelegatePredicate<T>, NoSort<T>>(
            source,
            new DelegatePredicate<T>(predicate),
            default);
    }

    /// <summary>
    /// Creates a filtered view over the observable collection using a delegate predicate.
    /// For maximum performance, use the struct-based overload with a custom predicate type.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="predicate">The predicate delegate used to filter items.</param>
    /// <param name="suppressEventExceptions">Whether to suppress exceptions from event handlers.</param>
    /// <returns>A filtered view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, DelegatePredicate<T>, NoSort<T>> CreateFilteredView<T>(
        this IReadOnlyLargeObservableCollection<T> source,
        Func<T, bool> predicate,
        bool suppressEventExceptions)
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, DelegatePredicate<T>, NoSort<T>>(
            source,
            new DelegatePredicate<T>(predicate),
            default,
            suppressEventExceptions);
    }

    #endregion

    #region Sort Only (uses NoFilter<T> - JIT eliminates filter operation)

    /// <summary>
    /// Creates a sorted view over the observable collection using a struct comparer for optimal performance.
    /// Uses <see cref="NoFilter{T}"/> internally - the JIT compiler eliminates the filter operation entirely.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <typeparam name="TComparer">The comparer type. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="comparer">The comparer used to sort items.</param>
    /// <returns>A sorted view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, TComparer> CreateSortedView<T, TComparer>(
        this IReadOnlyLargeObservableCollection<T> source,
        TComparer comparer)
        where TComparer : IComparer<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, TComparer>(source, default, comparer);
    }

    /// <summary>
    /// Creates a sorted view over the observable collection using a struct comparer for optimal performance.
    /// Uses <see cref="NoFilter{T}"/> internally - the JIT compiler eliminates the filter operation entirely.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <typeparam name="TComparer">The comparer type. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="comparer">The comparer used to sort items.</param>
    /// <param name="suppressEventExceptions">Whether to suppress exceptions from event handlers.</param>
    /// <returns>A sorted view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, TComparer> CreateSortedView<T, TComparer>(
        this IReadOnlyLargeObservableCollection<T> source,
        TComparer comparer,
        bool suppressEventExceptions)
        where TComparer : IComparer<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, TComparer>(source, default, comparer, suppressEventExceptions);
    }

    /// <summary>
    /// Creates a sorted view over the observable collection using the default ascending comparer.
    /// The type must implement <see cref="IComparable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <returns>A sorted view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, DefaultComparer<T>> CreateSortedView<T>(
        this IReadOnlyLargeObservableCollection<T> source)
        where T : IComparable<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, DefaultComparer<T>>(source, default, default);
    }

    /// <summary>
    /// Creates a sorted view over the observable collection using the default ascending comparer.
    /// The type must implement <see cref="IComparable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="suppressEventExceptions">Whether to suppress exceptions from event handlers.</param>
    /// <returns>A sorted view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, DefaultComparer<T>> CreateSortedView<T>(
        this IReadOnlyLargeObservableCollection<T> source,
        bool suppressEventExceptions)
        where T : IComparable<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, DefaultComparer<T>>(source, default, default, suppressEventExceptions);
    }

    /// <summary>
    /// Creates a sorted view over the observable collection in descending order.
    /// The type must implement <see cref="IComparable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <returns>A sorted view in descending order that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, DescendingComparer<T>> CreateSortedViewDescending<T>(
        this IReadOnlyLargeObservableCollection<T> source)
        where T : IComparable<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, DescendingComparer<T>>(source, default, default);
    }

    /// <summary>
    /// Creates a sorted view over the observable collection in descending order.
    /// The type must implement <see cref="IComparable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="suppressEventExceptions">Whether to suppress exceptions from event handlers.</param>
    /// <returns>A sorted view in descending order that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, DescendingComparer<T>> CreateSortedViewDescending<T>(
        this IReadOnlyLargeObservableCollection<T> source,
        bool suppressEventExceptions)
        where T : IComparable<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, DescendingComparer<T>>(source, default, default, suppressEventExceptions);
    }

    /// <summary>
    /// Creates a sorted view over the observable collection using a delegate comparer.
    /// For maximum performance, use the struct-based overload with a custom comparer type.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="comparer">The comparison delegate used to sort items.</param>
    /// <returns>A sorted view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, DelegateComparer<T>> CreateSortedView<T>(
        this IReadOnlyLargeObservableCollection<T> source,
        Func<T, T, int> comparer)
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, DelegateComparer<T>>(
            source,
            default,
            new DelegateComparer<T>(comparer));
    }

    /// <summary>
    /// Creates a sorted view over the observable collection using a delegate comparer.
    /// For maximum performance, use the struct-based overload with a custom comparer type.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="comparer">The comparison delegate used to sort items.</param>
    /// <param name="suppressEventExceptions">Whether to suppress exceptions from event handlers.</param>
    /// <returns>A sorted view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, DelegateComparer<T>> CreateSortedView<T>(
        this IReadOnlyLargeObservableCollection<T> source,
        Func<T, T, int> comparer,
        bool suppressEventExceptions)
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, NoFilter<T>, DelegateComparer<T>>(
            source,
            default,
            new DelegateComparer<T>(comparer),
            suppressEventExceptions);
    }

    #endregion

    #region Filter + Sort Combined

    /// <summary>
    /// Creates a filtered and sorted view over the observable collection using struct predicate and comparer for optimal performance.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <typeparam name="TPredicate">The predicate type. Struct implementations enable JIT optimizations.</typeparam>
    /// <typeparam name="TComparer">The comparer type. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="predicate">The predicate used to filter items.</param>
    /// <param name="comparer">The comparer used to sort items.</param>
    /// <returns>A filtered and sorted view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, TComparer> CreateView<T, TPredicate, TComparer>(
        this IReadOnlyLargeObservableCollection<T> source,
        TPredicate predicate,
        TComparer comparer)
        where TPredicate : ILargePredicate<T>
        where TComparer : IComparer<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, TComparer>(source, predicate, comparer);
    }

    /// <summary>
    /// Creates a filtered and sorted view over the observable collection using struct predicate and comparer for optimal performance.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <typeparam name="TPredicate">The predicate type. Struct implementations enable JIT optimizations.</typeparam>
    /// <typeparam name="TComparer">The comparer type. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="predicate">The predicate used to filter items.</param>
    /// <param name="comparer">The comparer used to sort items.</param>
    /// <param name="suppressEventExceptions">Whether to suppress exceptions from event handlers.</param>
    /// <returns>A filtered and sorted view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, TComparer> CreateView<T, TPredicate, TComparer>(
        this IReadOnlyLargeObservableCollection<T> source,
        TPredicate predicate,
        TComparer comparer,
        bool suppressEventExceptions)
        where TPredicate : ILargePredicate<T>
        where TComparer : IComparer<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, TComparer>(source, predicate, comparer, suppressEventExceptions);
    }

    /// <summary>
    /// Creates a filtered and sorted view over the observable collection using delegate predicate and default ascending comparer.
    /// The type must implement <see cref="IComparable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="predicate">The predicate delegate used to filter items.</param>
    /// <returns>A filtered and sorted view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, DelegatePredicate<T>, DefaultComparer<T>> CreateFilteredSortedView<T>(
        this IReadOnlyLargeObservableCollection<T> source,
        Func<T, bool> predicate)
        where T : IComparable<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, DelegatePredicate<T>, DefaultComparer<T>>(
            source,
            new DelegatePredicate<T>(predicate),
            default);
    }

    /// <summary>
    /// Creates a filtered and sorted view over the observable collection using delegate predicate and delegate comparer.
    /// For maximum performance, use the struct-based overload with custom predicate and comparer types.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="predicate">The predicate delegate used to filter items.</param>
    /// <param name="comparer">The comparison delegate used to sort items.</param>
    /// <returns>A filtered and sorted view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, DelegatePredicate<T>, DelegateComparer<T>> CreateView<T>(
        this IReadOnlyLargeObservableCollection<T> source,
        Func<T, bool> predicate,
        Func<T, T, int> comparer)
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, DelegatePredicate<T>, DelegateComparer<T>>(
            source,
            new DelegatePredicate<T>(predicate),
            new DelegateComparer<T>(comparer));
    }

    /// <summary>
    /// Creates a filtered and sorted view over the observable collection using delegate predicate and delegate comparer.
    /// For maximum performance, use the struct-based overload with custom predicate and comparer types.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="predicate">The predicate delegate used to filter items.</param>
    /// <param name="comparer">The comparison delegate used to sort items.</param>
    /// <param name="suppressEventExceptions">Whether to suppress exceptions from event handlers.</param>
    /// <returns>A filtered and sorted view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, DelegatePredicate<T>, DelegateComparer<T>> CreateView<T>(
        this IReadOnlyLargeObservableCollection<T> source,
        Func<T, bool> predicate,
        Func<T, T, int> comparer,
        bool suppressEventExceptions)
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, DelegatePredicate<T>, DelegateComparer<T>>(
            source,
            new DelegatePredicate<T>(predicate),
            new DelegateComparer<T>(comparer),
            suppressEventExceptions);
    }

    /// <summary>
    /// Creates a filtered and sorted view over the observable collection using a struct predicate and default ascending comparer.
    /// The type must implement <see cref="IComparable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <typeparam name="TPredicate">The predicate type. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="predicate">The predicate used to filter items.</param>
    /// <returns>A filtered and sorted view that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, DefaultComparer<T>> CreateFilteredSortedView<T, TPredicate>(
        this IReadOnlyLargeObservableCollection<T> source,
        TPredicate predicate)
        where T : IComparable<T>
        where TPredicate : ILargePredicate<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, DefaultComparer<T>>(
            source,
            predicate,
            default);
    }

    /// <summary>
    /// Creates a filtered and sorted view in descending order over the observable collection using a struct predicate.
    /// The type must implement <see cref="IComparable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <typeparam name="TPredicate">The predicate type. Struct implementations enable JIT optimizations.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="predicate">The predicate used to filter items.</param>
    /// <returns>A filtered and sorted view in descending order that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, DescendingComparer<T>> CreateFilteredSortedViewDescending<T, TPredicate>(
        this IReadOnlyLargeObservableCollection<T> source,
        TPredicate predicate)
        where T : IComparable<T>
        where TPredicate : ILargePredicate<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, TPredicate, DescendingComparer<T>>(
            source,
            predicate,
            default);
    }

    /// <summary>
    /// Creates a filtered and sorted view in descending order over the observable collection using delegate predicate.
    /// The type must implement <see cref="IComparable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="predicate">The predicate delegate used to filter items.</param>
    /// <returns>A filtered and sorted view in descending order that automatically updates when the source changes.</returns>
    public static FilteredSortedReadOnlyLargeObservableCollection<T, DelegatePredicate<T>, DescendingComparer<T>> CreateFilteredSortedViewDescending<T>(
        this IReadOnlyLargeObservableCollection<T> source,
        Func<T, bool> predicate)
        where T : IComparable<T>
    {
        return new FilteredSortedReadOnlyLargeObservableCollection<T, DelegatePredicate<T>, DescendingComparer<T>>(
            source,
            new DelegatePredicate<T>(predicate),
            default);
    }

    #endregion
}
