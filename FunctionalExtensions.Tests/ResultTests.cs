using System;
using System.Threading.Tasks;
using FunctionalExtensions;

namespace FunctionalExtensions.Tests;

public class ResultTests
{
    [Fact]
    public void ResultFactoriesAndToStringWork()
    {
        var ok = Result<int>.Ok(5);
        Assert.True(ok.IsSuccess);
        Assert.Equal("Ok(5)", ok.ToString());

        var fail = Result<int>.Fail("error");
        Assert.False(fail.IsSuccess);
        Assert.Equal("Error(error)", fail.ToString());

        var tried = Result.Try(() => 10);
        Assert.True(tried.IsSuccess);

        var exception = Result.Try<int>(() => throw new InvalidOperationException("broken"));
        Assert.False(exception.IsSuccess);
        Assert.Equal("broken", exception.Error);
    }

    [Fact]
    public async Task ResultExtensionsCoverAllBranches()
    {
        var ok = Result<int>.Ok(10);
        var fail = Result<int>.Fail("bad");

        Assert.True(ok.IsOk);
        Assert.False(fail.IsOk);
        Assert.True(fail.IsError);
        Assert.Equal("bad", fail.Error);

        Assert.Equal(10, ok.ValueOr(5));
        Assert.Equal(5, fail.ValueOr(5));
        Assert.Equal(10, ok.ValueOrElse(_ => 3));
        Assert.Equal(3, fail.ValueOrElse(error => error!.Length));

        Assert.Equal(20, ok.Map(x => x * 2).Value);
        Assert.False(fail.Map(x => x * 2).IsSuccess);

        Assert.Equal(15, ok.Bind(x => Result<int>.Ok(x + 5)).Value);
        Assert.False(fail.Bind(x => Result<int>.Ok(x)).IsSuccess);

        var tapped = 0;
        ok.Tap(x => tapped = x);
        Assert.Equal(10, tapped);

        var skipped = false;
        fail.Tap(_ => skipped = true);
        Assert.False(skipped);

        var recovered = fail.Recover(_ => 42);
        Assert.True(recovered.IsSuccess);
        Assert.Equal(42, recovered.Value);

        var recoveredWith = fail.RecoverWith(_ => Result<int>.Ok(7));
        Assert.True(recoveredWith.IsSuccess);
        Assert.Equal(7, recoveredWith.Value);

        var fallback = fail.OrElse(() => Result<int>.Ok(99));
        Assert.Equal(99, fallback.Value);

        var matched = ok.Match(value => value * 2, _ => -1);
        Assert.Equal(20, matched);
        Assert.Equal(-1, fail.Match(value => value, _ => -1));

        var query = from x in Result<int>.Ok(2)
                    from y in Result<int>.Ok(3)
                    select x + y;
        Assert.Equal(5, query.Value);

        var projected = Result<int>.Ok(2).SelectMany(
            x => Result<int>.Ok(x * 2),
            (value, intermediate) => value + intermediate);
        Assert.Equal(6, projected.Value);

        var failedProjection = fail.SelectMany(
            x => Result<int>.Ok(x),
            (value, intermediate) => value + intermediate);
        Assert.False(failedProjection.IsSuccess);

        var applicative = Result<Func<int, int>>.Ok(x => x + 1);
        Assert.Equal(11, ok.Apply(applicative).Value);

        var applicativeFailure = Result<Func<int, int>>.Fail("missing");
        Assert.False(ok.Apply(applicativeFailure).IsSuccess);

        Assert.Equal(ok, ok | Result<int>.Ok(1));
        Assert.Equal(1, (fail | Result<int>.Ok(1)).Value);

        Assert.True(ok.ToOption().HasValue);
        Assert.False(fail.ToOption().HasValue);

        Assert.Equal(10, ok.ToIO().Invoke());
        var ioEx = Assert.Throws<InvalidOperationException>(() => fail.ToIO().Invoke());
        Assert.Equal("bad", ioEx.Message);

        Assert.True(ok.ToTry().IsSuccess);
        Assert.False(fail.ToTry().IsSuccess);

        var taskResult = ok.ToTaskResult().Invoke();
        var taskValue = await taskResult;
        Assert.True(taskValue.IsSuccess);
    }
}
