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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LargeCollections.DiskCache;

public static class ParallelEnumerableExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long LongCount<T>(this IEnumerable<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (items is IReadOnlyLargeArray<T> largeArray)
        {
            return largeArray.Count;
        }

        long count = 0;

        foreach (T item in items)
        {
            count++;
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long LongCountParallel<T>(this IEnumerable<T>[] parallelItems)
    {
        object lockObject = new();
        long count = 0;

        parallelItems.ToParallel((uint)parallelItems.Length, item =>
        {
            long subCount = item.LongCount();
            Interlocked.Add(ref count, subCount);
        });

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T>[] WhereParallel<T>(this IEnumerable<T>[] parallelItems, Func<T, bool> function)
    {
        if (parallelItems is null)
        {
            throw new ArgumentNullException(nameof(parallelItems));
        }
        if (function is null)
        {
            throw new ArgumentNullException(nameof(function));
        }

        IEnumerable<T>[] result = new IEnumerable<T>[parallelItems.Length];

        for (int i = 0; i < parallelItems.Length; i++)
        {
            result[i] = parallelItems[i]
                .Where(function);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Tout>[] Select<Tin, Tout, TParam>(this IEnumerable<Tin>[] parallelItems, TParam[] arguments, Func<Tin, TParam, Tout> function)
    {
        if (parallelItems is null)
        {
            throw new ArgumentNullException(nameof(parallelItems));
        }
        if (function is null)
        {
            throw new ArgumentNullException(nameof(function));
        }
        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }
        if (arguments.Length != parallelItems.Length)
        {
            throw new ArgumentException("Number of arguments does not match");
        }

        IEnumerable<Tout>[] result = new IEnumerable<Tout>[parallelItems.Length];

        for (int i = 0; i < parallelItems.Length; i++)
        {
            TParam argument = arguments[i];
            result[i] = parallelItems[i]
                .Select(item =>
                {
                    return function.Invoke(item, argument);
                });
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Tout>[] Select<Tin, Tout>(this IEnumerable<Tin>[] parallelItems, Func<Tin, Tout> function)
    {
        if (parallelItems is null)
        {
            throw new ArgumentNullException(nameof(parallelItems));
        }
        if (function is null)
        {
            throw new ArgumentNullException(nameof(function));
        }

        IEnumerable<Tout>[] result = new IEnumerable<Tout>[parallelItems.Length];

        for (int i = 0; i < parallelItems.Length; i++)
        {
            result[i] = parallelItems[i].Select(function);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DoParallel<T>(this IEnumerable<T>[] parallelItems, Action<T> action)
    {
        if (parallelItems is null)
        {
            throw new ArgumentNullException(nameof(parallelItems));
        }
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        Task[] tasks = new Task[parallelItems.Length];
        for (int i = 0; i < parallelItems.Length; i++)
        {
            IEnumerable<T> items = parallelItems[i];
            tasks[i] = Task.Run(() =>
            {
                if (items is IReadOnlyList<T> list)
                {
                    int count = list.Count;
                    for (int j = 0; j < count; j++)
                    {
                        T item = list[j];
                        action(item);
                    }
                    return;
                }
                else
                {
                    foreach (T item in items)
                    {
                        action(item);
                    }
                }
            });
        }
        Task.WaitAll(tasks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DoParallel<Tin, TParam>(this IEnumerable<Tin>[] parallelItems, TParam[] arguments, Action<Tin, TParam> action)
    {
        if (parallelItems is null)
        {
            throw new ArgumentNullException(nameof(parallelItems));
        }
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }
        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }
        if (arguments.Length != parallelItems.Length)
        {
            throw new ArgumentException("Number of arguments does not match");
        }

        Task[] tasks = new Task[parallelItems.Length];
        for (int i = 0; i < parallelItems.Length; i++)
        {
            IEnumerable<Tin> items = parallelItems[i];
            TParam argument = arguments[i];
            tasks[i] = Task.Run(() =>
            {
                if (items is IReadOnlyList<Tin> list)
                {
                    int count = list.Count;
                    for (int j = 0; j < count; j++)
                    {
                        Tin item = list[j];
                        action(item, argument);
                    }
                    return;
                }
                else
                {
                    foreach (Tin item in items)
                    {
                        action(item, argument);
                    }
                }

            });
        }
        Task.WaitAll(tasks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ParallelToSequential<T>(this IEnumerable<T>[] parallelItems)
    {
        if (parallelItems is null)
        {
            throw new ArgumentNullException(nameof(parallelItems));
        }

        for (int i = 0; i < parallelItems.Length; i++)
        {
            IEnumerable<T> items = parallelItems[i];
            foreach (T item in items)
            {
                yield return item;
            }
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T>[] ToParallel<T>(this IEnumerable<T> items, uint degreeOfParallelism)
    {
        if (degreeOfParallelism == 0)
        {
            degreeOfParallelism = 1;
        }
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        IEnumerator<T> itemEnumerator = items.GetEnumerator();

        IEnumerable<T> GetItems()
        {
            if (itemEnumerator is null)
            {
                yield break;
            }

            T item;
            while (true)
            {
                lock (itemEnumerator)
                {
                    if (!itemEnumerator.MoveNext())
                    {
                        yield break;
                    }

                    item = itemEnumerator.Current;
                }

                yield return item;
            }
        }

        IEnumerable<T>[] result = new IEnumerable<T>[degreeOfParallelism];

        for (int i = 0; i < degreeOfParallelism; i++)
        {
            result[i] = GetItems();
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Tout>[] ToParallel<Tin, Tout, TParam>(this IEnumerable<Tin> items, uint degreeOfParallelism, TParam[] arguments, Func<Tin, TParam, Tout> function)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }
        if (function is null)
        {
            throw new ArgumentNullException(nameof(function));
        }
        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }
        if (arguments.Length != degreeOfParallelism)
        {
            throw new ArgumentException("Number of arguments does not match");
        }

        IEnumerable<Tin>[] parallelItems = items.ToParallel(degreeOfParallelism);

        return parallelItems.Select(arguments, function);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Tout>[] ToParallel<Tin, Tout>(this IEnumerable<Tin> items, uint degreeOfParallelism, Func<Tin, Tout> function)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }
        if (function is null)
        {
            throw new ArgumentNullException(nameof(function));
        }

        IEnumerable<Tin>[] parallelItems = items.ToParallel(degreeOfParallelism);

        return parallelItems.Select(function);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToParallel<T>(this IEnumerable<T> items, uint degreeOfParallelism, Action<T> action)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        IEnumerable<T>[] parallelItems = items.ToParallel(degreeOfParallelism);

        parallelItems.DoParallel(action);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToParallel<Tin, TParam>(this IEnumerable<Tin> items, uint degreeOfParallelism, TParam[] arguments, Action<Tin, TParam> action)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }
        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }
        if (arguments.Length != degreeOfParallelism)
        {
            throw new ArgumentException("Number of arguments does not match");
        }

        IEnumerable<Tin>[] parallelItems = items.ToParallel(degreeOfParallelism);

        parallelItems.DoParallel(arguments, action);
    }
}
