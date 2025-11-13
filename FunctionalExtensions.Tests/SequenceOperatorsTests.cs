using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalExtensions.Tests;

public class SequenceOperatorsTests
{
    [Fact]
    public void SequencePipeOperatorsComposeAndTransform()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        var generator = Enumerable.Range(1, 5);

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, generator | SequenceOperators.Map<int, int>(x => x));
        var doubles = numbers | SequenceOperators.Map<int, int>(x => x * 2);
        Assert.Equal(new[] { 2, 4, 6, 8, 10 }, doubles);

        var even = numbers | SequenceOperators.Filter<int>(x => x % 2 == 0);
        Assert.Equal(new[] { 2, 4 }, even);

        var flattened = numbers | SequenceOperators.Bind<int, int>(x => Enumerable.Repeat(x, 2));
        Assert.Equal(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 }, flattened);

        var distinct = new[] { 1, 1, 2, 2 } | SequenceOperators.Distinct<int>();
        Assert.Equal(new[] { 1, 2 }, distinct);

        var distinctBy = new[] { "aa", "b", "cc" } | SequenceOperators.DistinctBy<string, int>(s => s.Length);
        Assert.Equal(new[] { "aa", "b" }, distinctBy);

        var appended = new[] { 1, 2 } | SequenceOperators.Append(3);
        Assert.Equal(new[] { 1, 2, 3 }, appended);

        var prepended = new[] { 2, 3 } | SequenceOperators.Prepend(1);
        Assert.Equal(new[] { 1, 2, 3 }, prepended);

        var concatenated = new[] { 1 } | SequenceOperators.ConcatWith(new[] { 2, 3 });
        Assert.Equal(new[] { 1, 2, 3 }, concatenated);

        var defaulted = Array.Empty<int>() | SequenceOperators.DefaultIfEmpty(42);
        Assert.Equal(new[] { 42 }, defaulted);

        Assert.Equal(new[] { 1, 2 }, numbers | SequenceOperators.Take<int>(2));
        Assert.Equal(new[] { 3, 4, 5 }, numbers | SequenceOperators.Skip<int>(2));
        Assert.Equal(new[] { 1, 2, 3 }, numbers | SequenceOperators.SkipLast<int>(2));
        Assert.Equal(new[] { 4, 5 }, numbers | SequenceOperators.TakeLast<int>(2));
        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, numbers | SequenceOperators.Reverse<int>());

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, numbers | SequenceOperators.OrderBy<int, int>(x => x));
        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, numbers | SequenceOperators.OrderByDescending<int, int>(x => x));

        var comparer = StringComparer.OrdinalIgnoreCase;
        var union = new[] { "a" } | SequenceOperators.UnionWith(new[] { "A", "b" }, comparer);
        Assert.Equal(new[] { "a", "b" }, union);

        var intersect = new[] { "a", "b" } | SequenceOperators.IntersectWith(new[] { "B" }, comparer);
        Assert.Equal(new[] { "b" }, intersect);

        var except = new[] { "a", "b" } | SequenceOperators.ExceptWith(new[] { "A" }, comparer);
        Assert.Equal(new[] { "b" }, except);

        var symmetric = new[] { "a", "b" } | SequenceOperators.SymmetricExceptWith(new[] { "B", "c" }, comparer);
        Assert.Equal(new[] { "a", "c" }, symmetric);

        var grouped = numbers | SequenceOperators.GroupBy<int, int>(x => x % 2);
        Assert.Equal(2, grouped.Count());

        var projectedGroup = numbers | SequenceOperators.GroupBy<int, int, string>(x => x % 2, (key, values) => $"{key}:{string.Join('-', values)}");
        Assert.Contains("0:2-4", projectedGroup);

        var join = numbers | SequenceOperators.Join<int, string, int, string>(new[] { "1", "2", "7" }, x => x, s => int.Parse(s), (outer, inner) => $"{outer}:{inner}");
        Assert.Contains("1:1", join);

        var wordNumbers = new[] { "one", "two", "three" };
        var joinComparer = wordNumbers | SequenceOperators.Join<string, string, string, string>(new[] { "ONE", "THREE" }, x => x, s => s, (outer, inner) => inner, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("ONE", joinComparer);

        var groupJoin = numbers | SequenceOperators.GroupJoin<int, int, int, int>(new[] { 1, 2, 2 }, x => x, x => x, (outer, inners) => outer + inners.Sum());
        Assert.Contains(3, groupJoin);

        var groupJoinComparer = wordNumbers | SequenceOperators.GroupJoin<string, string, string, string>(new[] { "ONE" }, x => x, s => s, (outer, inners) => string.Join(',', inners), StringComparer.OrdinalIgnoreCase);
        Assert.Contains("ONE", groupJoinComparer);

        var leftJoin = numbers | SequenceOperators.LeftJoin<int, int, int>(new[] { 2 }, x => x, x => x);
        Assert.Contains(leftJoin, tuple => tuple.Item == 1 && !tuple.Matches.Any());
        Assert.Contains(leftJoin, tuple => tuple.Item == 2 && tuple.Matches.Single() == 2);

        var rightJoin = numbers | SequenceOperators.RightJoin<int, int, int>(new[] { 10 }, x => x, x => x);
        Assert.Contains(rightJoin, tuple => tuple.Item == 1);

        var rightJoinMatches = numbers | SequenceOperators.RightJoin<int, int, int>(new[] { 2 }, x => x, x => x);
        Assert.Contains(rightJoinMatches, tuple =>
        {
            int? match = tuple.Match;
            return tuple.Item == 2 && match == 2;
        });

        var zipped = numbers | SequenceOperators.Zip<int, int, int>(new[] { 10, 20, 30 }, (a, b) => a + b);
        Assert.Equal(new[] { 11, 22, 33 }, zipped);

        var chunked = numbers | SequenceOperators.Chunk<int>(2);
        Assert.Equal(new[] { new[] { 1, 2 }, new[] { 3, 4 }, new[] { 5 } }, System.Linq.Enumerable.Select(chunked, chunk => chunk.ToArray()));

        var pairwise = numbers | SequenceOperators.Pairwise<int>();
        Assert.Equal(new[] { (1, 2), (2, 3), (3, 4), (4, 5) }, pairwise);

        var window = numbers | SequenceOperators.Window<int>(2, 2, allowPartial: true);
        Assert.Equal(3, window.Count());
        var strictWindow = numbers | SequenceOperators.Window<int>(2);
        Assert.Equal(4, strictWindow.Count());

        var scanned = numbers | SequenceOperators.Scan<int, int>(0, (acc, value) => acc + value);
        Assert.Equal(new[] { 1, 3, 6, 10, 15 }, scanned);

        var pipe = SequenceOperators.Map<int, int>(x => x + 1).Then(SequenceOperators.Map<int, int>(x => x * 2));
        Assert.Equal(new[] { 4, 6, 8, 10, 12 }, numbers | pipe);

        var terminal = SequenceOperators.Sum<int>();
        var pipeline = SequenceOperators.Map<int, int>(x => x) .Then(terminal);
        Assert.Equal(15, numbers | pipeline);

        var composed = (SequenceOperators.Take<int>(2) + SequenceOperators.Skip<int>(3));
        Assert.Equal(new[] { 1, 2, 4, 5 }, numbers | composed);

        var difference = SequenceOperators.Take<int>(4) - SequenceOperators.Skip<int>(2);
        Assert.Equal(new[] { 1, 2 }, numbers | difference);

        var intersection = SequenceOperators.Take<int>(4) & SequenceOperators.Skip<int>(2);
        Assert.Equal(new[] { 3, 4 }, numbers | intersection);

        var symmetricPipe = SequenceOperators.Take<int>(3) ^ SequenceOperators.Skip<int>(2);
        Assert.Equal(new[] { 1, 2, 4, 5 }, numbers | symmetricPipe);

        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, numbers | ~SequenceOperators.Map<int, int>(x => x));

        var repeated = numbers | (SequenceOperators.Take<int>(1) * 3);
        Assert.Equal(new[] { 1, 1, 1 }, repeated);

        Assert.Throws<ArgumentOutOfRangeException>(() => numbers | (SequenceOperators.Take<int>(1) * -1));

        Assert.Equal(new[] { 1, 2, 4, 5 }, generator | (SequenceOperators.Take<int>(3) ^ SequenceOperators.Skip<int>(2)));
        Assert.Equal(new[] { 1, 2, 4, 5 }, generator | (SequenceOperators.Take<int>(2) + SequenceOperators.Skip<int>(3)));
        Assert.Equal(new[] { 1, 2 }, generator | (SequenceOperators.Take<int>(4) - SequenceOperators.Skip<int>(2)));
        Assert.Equal(new[] { 3, 4 }, generator | (SequenceOperators.Take<int>(4) & SequenceOperators.Skip<int>(2)));
        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, generator | ~SequenceOperators.Map<int, int>(x => x));
        Assert.Equal(new[] { 1, 1 }, generator | (SequenceOperators.Take<int>(1) * 2));
    }

    [Fact]
    public void SequenceTerminalsSummarizeSequences()
    {
        var numbers = Enumerable.Range(1, 5).ToList();

        Assert.Equal(numbers, numbers | SequenceOperators.ToList<int>());
        Assert.Equal(numbers.ToArray(), numbers | SequenceOperators.ToArray<int>());
        Assert.Equal(numbers.ToHashSet(), numbers | SequenceOperators.ToHashSet<int>());

        var dictionary = numbers | SequenceOperators.ToDictionary<int, int, int>(x => x, x => x * 2);
        Assert.Equal(4, dictionary[2]);

        Assert.Equal(5, numbers | SequenceOperators.Count<int>());
        Assert.True(numbers | SequenceOperators.Contains(3));
        Assert.True(numbers | SequenceOperators.Any<int>());
        Assert.True(numbers | SequenceOperators.Any<int>(x => x > 4));
        Assert.True(numbers | SequenceOperators.All<int>(x => x > 0));
        Assert.Equal(1, numbers | SequenceOperators.First<int>());
        Assert.Equal(3, numbers | SequenceOperators.ElementAt<int>(2));
        Assert.Equal(5, numbers | SequenceOperators.Last<int>());
        Assert.Equal(42, new[] { 42 } | SequenceOperators.Single<int>());
        Assert.Equal(5, numbers | SequenceOperators.Max<int>());
        Assert.Equal(1, numbers | SequenceOperators.Min<int>());

        var maxByLength = new[] { "hello", "bb" } | SequenceOperators.MaxBy<string, int>(s => s.Length);
        Assert.Equal(5, maxByLength!.Length);

        var minByLength = new[] { "aaa", "bb" } | SequenceOperators.MinBy<string, int>(s => s.Length);
        Assert.Equal(2, minByLength!.Length);

        Assert.Equal(15, numbers | SequenceOperators.Sum<int>());
        Assert.Equal(3, numbers | SequenceOperators.Average<int>());
        Assert.Throws<InvalidOperationException>(() => Array.Empty<int>() | SequenceOperators.Average<int>());

        Assert.Equal(15, numbers | SequenceOperators.Aggregate<int, int>(0, (acc, value) => acc + value));
        var aggregateSelector = numbers | SequenceOperators.Aggregate<int, int, string>(0, (acc, value) => acc + value, sum => sum.ToString());
        Assert.Equal("15", aggregateSelector);

        Assert.Equal("1,2,3,4,5", numbers | SequenceOperators.Join<int>(","));
        Assert.True(numbers | SequenceOperators.SequenceEqual(new[] { 1, 2, 3, 4, 5 }, EqualityComparer<int>.Default));

        var terminal = SequenceOperators.Count<int>();
        var doubled = terminal.Select(count => count * 2);
        Assert.Equal(10, numbers | doubled);

        var combined = SequenceOperators.Count<int>() & SequenceOperators.Sum<int>();
        var (count, sum) = numbers | combined;
        Assert.Equal(5, count);
        Assert.Equal(15, sum);
    }
}
