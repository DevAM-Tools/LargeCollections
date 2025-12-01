using System.Collections.Generic;
using System.Linq;

namespace LargeCollections.Test.Helpers;

public static class Parameters
{
    private static readonly long[] BaseCapacities =
    [
        0L,
        1L,
        5L,
        Constants.MaxStorageCapacity - 1L,
        Constants.MaxStorageCapacity,
        Constants.MaxStorageCapacity + 1L,
        2L * Constants.MaxStorageCapacity,
        Constants.MaxLargeCollectionCount - 1L,
        Constants.MaxLargeCollectionCount,
    ];

    public static IEnumerable<long> Capacities
    {
        get
        {
            HashSet<long> seen = new HashSet<long>();
            foreach (long capacity in BaseCapacities.SelectMany(c => new[] { c - 1, c, c + 1 }).Distinct())
            {
                if (capacity >= 0 && capacity <= Constants.MaxLargeCollectionCount)
                {
                    if (seen.Add(capacity))
                    {
                        yield return capacity;
                    }
                }
            }
        }
    }
}
