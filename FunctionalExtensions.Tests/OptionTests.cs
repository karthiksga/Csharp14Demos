using System;
using System.Globalization;
using System.Reflection;
using FunctionalExtensions;
using FunctionalExtensions.Patterns;
using FunctionalExtensions.TypeClasses;

namespace FunctionalExtensions.Tests;

public class OptionTests
{
    [Fact]
    public void OptionFactoriesHandleNullables()
    {
        var some = Option<int>.Some(42);
        Assert.True(some.HasValue);
        Assert.Equal(42, some.Value);

        var none = Option<int>.None;
        Assert.False(none.HasValue);

        string? text = "hello";
        Assert.True(Option.FromNullable(text).HasValue);
        text = null;
        Assert.False(Option.FromNullable(text).HasValue);

        int? number = 5;
        Assert.True(Option.FromNullable(number).HasValue);
        number = null;
        Assert.False(Option.FromNullable(number).HasValue);
    }

    [Fact]
    public void OptionExtensionsCoverAllBranches()
    {
        var option = Option<int>.Some(10);
        var none = Option<int>.None;

        Assert.True(option.IsSome);
        Assert.False(option.IsNone);
        Assert.Equal(10, option.ValueOr(0));
        Assert.Equal(10, option.ValueOrElse(() => -1));
        Assert.Equal(20, option.Map(x => x * 2).Value);
        Assert.Equal(40, option.Bind(x => Option<int>.Some(x * 4)).Value);

        var applicative = Option<Func<int, int>>.Some(x => x + 1);
        Assert.Equal(11, option.Apply(applicative).Value);
        Assert.True(option.Where(x => x > 5).HasValue);
        Assert.False(option.Where(x => x < 0).HasValue);

        var orElseValue = option.OrElse(() => Option<int>.Some(99)).Value;
        var orElseFallback = none.OrElse(() => Option<int>.Some(99)).Value;
        Assert.Equal(10, orElseValue);
        Assert.Equal(99, orElseFallback);

        var matchSome = option.Match(x => x, () => -1);
        var matchNone = none.Match(x => x, () => -1);
        Assert.Equal(10, matchSome);
        Assert.Equal(-1, matchNone);

        Assert.True(option.ToResult("err").IsSuccess);
        Assert.False(none.ToResult("missing").IsSuccess);

        var ioValue = option.ToIO(errorFactory: null).Invoke();
        Assert.Equal(10, ioValue);
        var ioEx = Assert.Throws<InvalidOperationException>(() => none.ToIO(() => "boom").Invoke());
        Assert.Equal("boom", ioEx.Message);

        Assert.True(option.ToTry().IsSuccess);
        Assert.False(none.ToTry(() => "oops").IsSuccess);

        Assert.True(option.ToTaskResult("err").Invoke().Result.IsSuccess);
        var failed = none.ToTaskResult(() => "missing").Invoke().Result;
        Assert.False(failed.IsSuccess);
        Assert.Equal("missing", failed.Error);
        var failedConstant = none.ToTaskResult("constant").Invoke().Result;
        Assert.Equal("constant", failedConstant.Error);

        var applied = Option<Func<int, Func<int, int>>>.Some(x => y => x + y) * option * Option<int>.Some(5);
        Assert.Equal(15, applied.Value);

        var pipeline = (from x in Option<int>.Some(2)
                        from y in Option<int>.Some(3)
                        select x * y).Value;
        Assert.Equal(6, pipeline);

        var projected = option.SelectMany(x => Option<int>.Some(x + 2), (value, intermediate) => value + intermediate);
        Assert.Equal(22, projected.Value);

        Assert.False(none.SelectMany(x => Option<int>.Some(x)).HasValue);

        var fallbackChoice = none | option;
        Assert.Equal(option, fallbackChoice);
    }

    [Fact]
    public void OptionTypeClassesBehave()
    {
        var option = Option<int>.Some(5);

        Assert.Equal(5, option.FMap(x => x).Value);
        Assert.Equal("constant", option.As("constant").Value);

        var tapped = 0;
        option.Tap(x => tapped = x);
        Assert.Equal(5, tapped);

        var pure = OptionApplicative.Pure(7);
        Assert.Equal(7, pure.Value);

        var apResult = option.Ap(Option<Func<int, int>>.Some(x => x * 3));
        Assert.Equal(15, apResult.Value);

        var liftA2 = option.LiftA2(Option<int>.Some(4), (a, b) => a + b);
        Assert.Equal(9, liftA2.Value);

        var applyOperator = Option<Func<int, int>>.Some(x => x * 2).Apply(option);
        Assert.Equal(10, applyOperator.Value);

        var bound = option.Bind(x => Option<int>.Some(x + 1));
        Assert.Equal(6, bound.Value);

        var selective = option.SelectMany(x => Option<int>.Some(x + 1));
        Assert.Equal(6, selective.Value);

        var projected = option.SelectMany(x => Option<int>.Some(x + 1), (value, intermediate) => value + intermediate);
        Assert.Equal(11, projected.Value);

        var joined = Option<Option<int>>.Some(Option<int>.Some(9)).Join();
        Assert.Equal(9, joined.Value);
    }

    [Fact]
    public void OptionPatternsAndSpanHelpersWork()
    {
        string? name = "codex";
        var optionFromClass = name.AsOption();
        Assert.True(optionFromClass.Some(out var text));
        Assert.Equal("codex", text);

        int? maybe = null;
        Assert.True(GetOptionPatternNone(maybe.AsOption()));

        Option<int> number = Option<int>.Some(7);
        Assert.True(number.Some(out var actual));
        Assert.Equal(7, actual);
        number = Option<int>.None;
        Assert.True(GetOptionPatternNone(number));

        ReadOnlySpan<char> digits = "123";
        Assert.True(digits.TryParseInt(out var parsed));
        Assert.Equal(123, parsed);

        ReadOnlySpan<char> invalid = "abc";
        Assert.False(invalid.TryParseInt(out _));
        Assert.False(invalid.ToIntOption().HasValue);

        ReadOnlySpan<char> hex = "FF";
        Assert.True(hex.TryParseInt(out var hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        Assert.Equal(255, hexValue);
    }

    private static bool GetOptionPatternNone<T>(Option<T> option)
    {
        MethodInfo? method = null;
        foreach (var type in typeof(OptionPatterns).Assembly.GetTypes())
        {
            foreach (var candidate in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (candidate.Name == "get_None" && candidate.IsGenericMethodDefinition)
                {
                    method = candidate;
                    break;
                }
            }

            if (method is not null)
            {
                break;
            }
        }

        if (method is null)
        {
            throw new InvalidOperationException("Unable to locate None extension.");
        }

        var generic = method.MakeGenericMethod(typeof(T));
        return (bool)generic.Invoke(null, new object[] { option })!;
    }
}
