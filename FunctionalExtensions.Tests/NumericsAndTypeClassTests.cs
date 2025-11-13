using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using FunctionalExtensions.Numerics;
using FunctionalExtensions.TypeClasses;

namespace FunctionalExtensions.Tests;

public class NumericsAndTypeClassTests
{
    [Fact]
    public void RationalSupportsArithmetic()
    {
        var half = new Rational<int>(1, 2);
        var third = new Rational<int>(1, 3);

        Assert.Equal(new Rational<int>(5, 6), half + third);
        Assert.Equal(new Rational<int>(1, 6), half - third);
        Assert.Equal(new Rational<int>(1, 6), half * third);
        Assert.Equal(new Rational<int>(3, 2), half / third);

        Rational<int> fromInt = 2;
        int backToInt = fromInt;
        Assert.Equal(2, backToInt);

        var negative = new Rational<int>(-2, -4) + Rational<int>.Zero;
        Assert.Equal(new Rational<int>(1, 2), negative);

        var inverted = new Rational<int>(1, -2) + Rational<int>.Zero;
        Assert.Equal(new Rational<int>(-1, 2), inverted);

        Assert.Equal(0, Rational<int>.Zero.CompareTo(new Rational<int>(0, 5)));
        Assert.Equal(Rational<int>.One, new Rational<int>(5, 5) + Rational<int>.Zero);

        Assert.Throws<DivideByZeroException>(() => new Rational<int>(1, 0) + Rational<int>.Zero);
    }

    [Fact]
    public void ComplexExtensionsExposeConveniences()
    {
        var value = new Complex(3, 4);
        Assert.Equal(25, InvokeComplexExtension<double>("MagnitudeSquared", value));
        Assert.Equal(5, InvokeComplexExtension<double>("Magnitude", value));
        Assert.Equal(new Complex(3, -4), InvokeComplexExtension<Complex>("Conjugate", value));
    }

    [Fact]
    public async Task EnumerableTypeClassesBehave()
    {
        var source = new[] { 1, 2, 3 };
        Assert.Equal(new[] { 2, 4, 6 }, source.FMap(x => x * 2).ToArray());
        Assert.Equal(new[] { 5, 5, 5 }, source.As(5).ToArray());

        var tapped = new List<int>();
        source.Tap(tapped.Add).ToList();
        Assert.Equal(source, tapped);

        Assert.Equal(new[] { 4, 5, 6 }, source.FMap(x => x + 3).ToArray());

        Assert.Equal(new[] { 9 }, EnumerableApplicative.Pure(9));
        Assert.Equal(new[] { 2, 4, 6, 3, 6, 9 }, source.Ap(new Func<int, int>[] { x => x * 2, x => x * 3 }));
        Assert.Equal(new[] { 0, 1, 2, 1, 0, 1, 2, 1, 0 }, source.LiftA2(new[] { 1, 2, 3 }, (a, b) => Math.Abs(a - b)));
        Assert.Equal(new[] { 2, 4, 6 }, source.Map(x => x * 2));
        Assert.Equal(new[] { 10, 20 }, new[] { new Func<int, int>(x => x * 10) }.Apply(new[] { 1, 2 }));

        Assert.Equal(new[] { 42 }, EnumerableMonad.Return(42));
        Assert.Equal(new[] { 1, 1, 2, 2, 3, 3 }, source.Bind(x => Enumerable.Repeat(x, 2)));
        Assert.Equal(new[] { 4, 6, 6, 8, 8, 10 }, System.Linq.Enumerable.SelectMany(source, x => new[] { x + 2, x + 4 }, (value, other) => value + other));
        Assert.Equal(new[] { 1, 2, 3, 4 }, new[] { new[] { 1, 2 }, new[] { 3, 4 } }.Join());

        var task = Task.FromResult(21);
        Assert.Equal(42, await task.FMap(x => x * 2));
        Assert.Equal(63, await task.FMapAsync(async x => { await Task.Yield(); return x * 3; }));

        var tappedTask = 0;
        await task.Tap(x => tappedTask = x);
        Assert.Equal(21, tappedTask);

        var tapAsync = 0;
        await task.TapAsync(async x => { await Task.Delay(1); tapAsync = x; });
        Assert.Equal(21, tapAsync);

        Assert.Equal(84, await task.Select(x => x * 4));

        Assert.Equal(5, await TaskApplicative.Pure(5));
        Assert.Equal(8, await task.Ap(Task.FromResult<Func<int, int>>(x => x - 13)));
        Assert.Equal(42, await task.LiftA2(Task.FromResult(2), (a, b) => a * b));
        Assert.Equal(84, await task.Map(x => x * 4));
        Assert.Equal(126, await task.MapAsync(async x => { await Task.Yield(); return x * 6; }));
        Assert.Equal(11, await Task.FromResult<Func<int, int>>(x => x + 1).Apply(Task.FromResult(10)));

        Assert.Equal(7, await TaskMonad.Return(7));
        Assert.Equal(42, await task.Bind(x => Task.FromResult(x * 2)));
        Assert.Equal(63, await task.SelectMany(x => Task.FromResult(x * 3)));
        Assert.Equal(63, await task.SelectMany(x => Task.FromResult(x * 2), (value, intermediate) => value + intermediate));

        var nested = Task.FromResult(Task.FromResult(9));
        Assert.Equal(9, await nested.Join());
    }

    [Fact]
    public void MonoidHelpersCoverAllBranches()
    {
        var sum = new SumInt(2).Append(new SumInt(3));
        Assert.Equal(5, (int)sum);
        Assert.Equal(0, (int)SumInt.Empty);

        var product = new ProductInt(2).Append(new ProductInt(3));
        Assert.Equal(6, (int)product);
        Assert.Equal(1, (int)ProductInt.Empty);

        var concatenated = new[] { SumInt.Empty, new SumInt(5) }.ConcatAll();
        Assert.Equal(5, (int)concatenated);

        var stringMonoid = MonoidModule.Create(string.Empty, (left, right) => left + right);
        Assert.Equal(string.Empty, stringMonoid.Empty);
        Assert.Equal("ab", stringMonoid.Combine("a", "b"));
    }

    private static T InvokeComplexExtension<T>(string memberName, Complex value)
    {
        var assembly = typeof(ComplexExtensions).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (type.FullName is null || !type.FullName.Contains("ComplexExtensions", StringComparison.Ordinal))
            {
                continue;
            }

            var method = type.GetMethod(memberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                         ?? type.GetMethod($"get_{memberName}", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method is not null && method.GetParameters() is [{ ParameterType: var parameterType }]
                && parameterType == typeof(Complex))
            {
                return (T)method.Invoke(null, new object[] { value })!;
            }
        }

        throw new InvalidOperationException($"Unable to locate Complex extension member '{memberName}'.");
    }
}
