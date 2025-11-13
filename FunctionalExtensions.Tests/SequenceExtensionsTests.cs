using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FunctionalExtensions;
using FunctionalExtensions.Tests.Support;
using FunctionalExtensions.TypeClasses;

namespace FunctionalExtensions.Tests;

public class SequenceExtensionsTests
{
    [Fact]
    public void EnumerableInstanceAndStaticMembersBehave()
    {
        var numbers = new[] { 1, 2, 3 };
        Assert.False(numbers.IsEmpty);
        Assert.True(Array.Empty<int>().IsEmpty);

        Assert.Equal(new[] { 2 }, numbers.Filter(x => x % 2 == 0));
        Assert.Equal(1, numbers.FirstOption().Value);
        Assert.False(numbers.FirstOption(x => x > 5).HasValue);

        Assert.Empty(IEnumerable<int>.Identity);
        Assert.Equal(new[] { 1, 2, 3, 4 }, IEnumerable<int>.Combine(new[] { 1, 2 }, new[] { 3, 4 }));

        Assert.Equal(new[] { 1, 2, 3, 4 }, new[] { 1, 2 } | new[] { 3, 4 });
        Assert.Equal(new[] { 1, 2, 3 }, (new[] { 1, 2 } + new[] { 2, 3 }).Distinct().ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, (new[] { 1, 2 } + 3));
        Assert.Equal(new[] { 0, 1, 2 }, 0 + new[] { 1, 2 });

        Assert.Equal(new[] { 1 }, new[] { 1, 2 } - new[] { 2 });
        Assert.Equal(new[] { 2 }, new[] { 1, 2 } & new[] { 2, 3 });
        Assert.Equal(new[] { 1, 3 }, new[] { 1, 2 } ^ new[] { 2, 3 });
        Assert.Equal(new[] { 3, 2, 1 }, ~new[] { 1, 2, 3 });

        Assert.Equal(new[] { 1, 2, 1, 2 }, new[] { 1, 2 } * 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new[] { 1 } * -1);

        Assert.Equal(new[] { 1, 2 }, !new[] { 1, 2 });
        Assert.Equal("1-2", new[] { 1, 2 } | "-" );

        Assert.Equal(new[] { new[] { 1, 2 }, new[] { 3, 4 } }, System.Linq.Enumerable.Select(new[] { 1, 2, 3, 4 } / 2, chunk => chunk.ToArray()));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new[] { 1 } / 0);

        Assert.Equal(new[] { 2, 3 }, new[] { 1, 2, 3 } % 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new[] { 1 } % -1);

        Assert.Equal(new[] { 1, 2 }, new[] { 1, 2, 3 } << 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new[] { 1 } << -1);

        Assert.Equal(new[] { 3 }, new[] { 1, 2, 3 } >> 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new[] { 1 } >> -1);
    }

    [Fact]
    public void AdvancedEnumerableHelpersCoverAllBranches()
    {
        var numbers = new[] { 1, 2, 3 };
        var sum = numbers.FoldMap(value => new SumInt(value));
        Assert.Equal(6, (int)sum);

        var traversed = numbers.TraverseOption(value => value > 0 ? Option<int>.Some(value) : Option<int>.None);
        Assert.True(traversed.HasValue);
        var failedTraverse = numbers.TraverseOption(value => value > 2 ? Option<int>.None : Option<int>.Some(value));
        Assert.False(failedTraverse.HasValue);

        var chunked = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(numbers.ChunkWhile((prev, current) => current == prev + 1), chunk => chunk.ToArray()));
        Assert.Single(chunked);
        var splitChunks = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(new[] { 1, 3, 4 }.ChunkWhile((prev, current) => current - prev == 1), chunk => chunk.ToArray()));
        Assert.Equal(2, splitChunks.Length);
        Assert.Empty(Array.Empty<int>().ChunkWhile((a, b) => true));

        Assert.Equal(new[] { 2, 3 }, numbers.Tail());
        Assert.Equal(Array.Empty<int>(), Array.Empty<int>().Tail());

        ReadOnlySpan<int> span = stackalloc[] { 1, 2, 3 };
        Assert.Equal(new[] { 2, 3 }, span.Tail().ToArray());
        ReadOnlySpan<int> emptySpan = ReadOnlySpan<int>.Empty;
        Assert.True(emptySpan.Tail().IsEmpty);
    }

    [Fact]
    public void StringHelpersProvideExpressiveOperators()
    {
        const string text = " Hello World ";
        Assert.Equal("HelloWorld", text.WithoutWhitespace);
        Assert.Equal(new[] { "Hello", "World" }, text.Words);

        Assert.True(!" ");
        Assert.False(!"value");
        Assert.Equal("Hello World", ~text);
        Assert.Equal("xxx", "x" * 3);
        Assert.Equal("", "x" * 0);
        Assert.Equal("zzz", 3 * "z");
        Assert.Equal(new[] { "a", "b" }, "a,b" / ',');
        Assert.Equal(new[] { "a", "b" }, "a|b" / "|");
        Assert.Equal("axc", "abc" / ("b", "x"));
        Assert.Equal("ac", "abc" - "b");
        Assert.Equal("bc", "abc" % 2);
        Assert.Equal("ab", "abc" << 2);
        Assert.Equal("c", "abc" >> 2);
        Assert.Equal("lo", "hello" & "world");
        Assert.Equal("hewrd", "hello" ^ "world");
        Assert.Equal("HELLO", "hello" | (s => s.ToUpperInvariant()));
    }

    [Fact]
    public async Task ValueBasedHelpersCoverUseCases()
    {
        using var console = new ConsoleCapture();
        var echoed = 5.WriteLine();
        Assert.Equal(5, echoed);
        Assert.Contains("5", console.Output);

        var piped = 10 | new Action<int>(_ => { });
        Assert.Equal(10, piped);
        Assert.Equal(20, 20 >> new Action<int>(_ => { }));

        string? maybe = null;
        Assert.False(maybe.ToOption().HasValue);
        Assert.True("value".ToOption().HasValue);
        Assert.True(42.ToOption(x => x > 0).HasValue);
        Assert.False(1.ToOption(x => x > 1).HasValue);

        Assert.True(5.ToOk().IsSuccess);
        Assert.True(5.Validate(x => x > 0, _ => "error").IsSuccess);
        Assert.False(0.Validate(x => x > 0, v => $"value {v}").IsSuccess);

        var io = 7.ToIO();
        Assert.Equal(7, io.Invoke());
        Assert.True(8.ToTaskResult().Invoke().Result.IsSuccess);
    }

    [Fact]
    public async Task TaskAndValueTaskExtensionsBehave()
    {
        var task = Task.FromResult(3);
        Assert.Equal(6, await task.Map(x => x * 2));
        Assert.Equal(9, await task.Bind(x => Task.FromResult(x * 3)));
        Assert.Equal(4, await task.Select(x => x + 1));

        var tapped = 0;
        await task.Tap(x => tapped = x);
        Assert.Equal(3, tapped);

        var tapAsync = 0;
        await task.Tap(async x => { await Task.Delay(1); tapAsync = x; });
        Assert.Equal(3, tapAsync);

        Assert.Equal(9, await task.SelectMany(x => Task.FromResult(x * 2), (value, intermediate) => value + intermediate));

        var valueTask = new ValueTask<int>(4);
        Assert.Equal(8, await valueTask.Map(x => x * 2));
        Assert.Equal(16, await valueTask.Bind(x => new ValueTask<int>(x * 4)));
        Assert.Equal(5, await valueTask.Select(x => x + 1));

        Assert.Equal(12, await valueTask.SelectMany(x => new ValueTask<int>(x * 3)));

        var tappedValue = 0;
        await valueTask.Tap(x => { tappedValue = x; return new ValueTask(); });
        Assert.Equal(4, tappedValue);

        Assert.Equal(12, await valueTask.SelectMany(x => new ValueTask<int>(x * 2), (value, intermediate) => value + intermediate));
    }

    [Fact]
    public void LazyExtensionsSupportComposition()
    {
        var lazy = new Lazy<int>(() => 10);
        Assert.Equal(20, lazy.Map(x => x * 2).Value);
        Assert.Equal(30, lazy.Bind(x => new Lazy<int>(() => x * 3)).Value);

        var tapped = 0;
        lazy.Tap(x => tapped = x).Value.ToString();
        Assert.Equal(10, tapped);

        var projected = lazy.SelectMany(x => new Lazy<int>(() => x + 1), (value, intermediate) => value + intermediate);
        Assert.Equal(21, projected.Value);
    }
}
