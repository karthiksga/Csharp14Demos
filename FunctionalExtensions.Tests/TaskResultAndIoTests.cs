using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FunctionalExtensions;
using FunctionalExtensions.Computation;
using FunctionalExtensions.Tests.Support;

namespace FunctionalExtensions.Tests;

public class TaskResultAndIoTests
{
    [Fact]
    public async Task TaskResultsFactoryMethodsWork()
    {
        var ok = await TaskResults.Return(5).Invoke();
        Assert.True(ok.IsSuccess);

        var fail = await TaskResults.Fail<int>("broken").Invoke();
        Assert.False(fail.IsSuccess);

        var fromResult = await TaskResults.FromResult(Result<int>.Ok(7)).Invoke();
        Assert.Equal(7, fromResult.Value);

        var fromProducer = await TaskResults.From(() => Task.FromResult(Result<int>.Ok(9))).Invoke();
        Assert.Equal(9, fromProducer.Value);

        Func<Task<int>> successProducer = async () => 3;
        var fromTask = await TaskResults.From(successProducer).Invoke();
        Assert.Equal(3, fromTask.Value);

        Func<Task<int>> exceptionProducer = async () => throw new InvalidOperationException("fail");
        var fromTaskException = await TaskResults.From(exceptionProducer).Invoke();
        Assert.Equal("fail", fromTaskException.Error);

        Func<Task<int>> syncThrow = () => throw new InvalidOperationException("early");
        var fromTaskSyncThrow = await TaskResults.From(syncThrow).Invoke();
        Assert.Equal("early", fromTaskSyncThrow.Error);

        var fromExistingTask = await TaskResults.FromTask(Task.FromResult(11)).Invoke();
        Assert.Equal(11, fromExistingTask.Value);

        Func<int> failingRun = () => throw new InvalidOperationException("boom");
        var fromExistingTaskFailure = await TaskResults.FromTask(Task.Run(failingRun)).Invoke();
        Assert.Equal("boom", fromExistingTaskFailure.Error);
    }

    [Fact]
    public async Task TaskResultExtensionsCoverAllBranches()
    {
        var taskResult = TaskResults.Return(10);
        var mapped = await taskResult.Map(x => x * 2).Invoke();
        Assert.Equal(20, mapped.Value);

        var runResult = await taskResult.RunAsync();
        Assert.True(runResult.IsSuccess);
        var directResult = await taskResult.ToResultAsync();
        Assert.True(directResult.IsSuccess);
        var failDirectResult = await TaskResults.Fail<int>("bad").ToResultAsync();
        Assert.False(failDirectResult.IsSuccess);

        var bound = await taskResult.Bind(x => TaskResults.Return(x + 5)).Invoke();
        Assert.Equal(15, bound.Value);

        var bindFailure = await TaskResults.Fail<int>("bind").Bind(_ => TaskResults.Return(1)).Invoke();
        Assert.Equal("bind", bindFailure.Error);

        var applicative = await taskResult.Apply(TaskResults.Return<Func<int, int>>(x => x - 2)).Invoke();
        Assert.Equal(8, applicative.Value);

        var applicativeFuncFailure = await taskResult.Apply(TaskResults.Fail<Func<int, int>>("fn fail")).Invoke();
        Assert.Equal("fn fail", applicativeFuncFailure.Error);

        var applicativeValueFailure = await TaskResults.Fail<int>("val fail").Apply(TaskResults.Return<Func<int, int>>(x => x)).Invoke();
        Assert.Equal("val fail", applicativeValueFailure.Error);

        var tapped = 0;
        await taskResult.Tap(x => tapped = x).Invoke();
        Assert.Equal(10, tapped);

        var notTapped = false;
        await TaskResults.Fail<int>("tap").Tap(_ => notTapped = true).Invoke();
        Assert.False(notTapped);

        var tapAsync = 0;
        await taskResult.Tap(async x => { await Task.Delay(1); tapAsync = x; }).Invoke();
        Assert.Equal(10, tapAsync);

        var notTappedAsync = false;
        await TaskResults.Fail<int>("tap").Tap(_ => { notTappedAsync = true; return Task.CompletedTask; }).Invoke();
        Assert.False(notTappedAsync);

        var fallback = await TaskResults.Fail<int>("error").OrElse(() => TaskResults.Return(99)).Invoke();
        Assert.Equal(99, fallback.Value);

        var select = await taskResult.Select(x => x + 1).Invoke();
        Assert.Equal(11, select.Value);

        var selectMany = await taskResult.SelectMany(x => TaskResults.Return(x * 2)).Invoke();
        Assert.Equal(20, selectMany.Value);

        var projected = await taskResult.SelectMany(x => TaskResults.Return(x + 1), (value, intermediate) => value + intermediate).Invoke();
        Assert.Equal(21, projected.Value);

        var projectedFailure = await TaskResults.Fail<int>("bad").SelectMany(x => TaskResults.Return(x), (value, inner) => value + inner).Invoke();
        Assert.False(projectedFailure.IsSuccess);

        var option = await taskResult.ToOptionAsync();
        Assert.True(option.HasValue);
        Assert.False((await TaskResults.Fail<int>("oops").ToOptionAsync()).HasValue);

        var ensured = await taskResult.Ensure(x => x == 10, "not ten").Invoke();
        Assert.True(ensured.IsSuccess);
        var ensuredFail = await taskResult.Ensure(x => x == 0, "nope").Invoke();
        Assert.Equal("nope", ensuredFail.Error);

        var taskIO = taskResult.ToTaskIO();
        Assert.Equal(10, await taskIO.Invoke());

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await TaskResults.Fail<int>("boom").ToTaskIO(error => new InvalidOperationException(error ?? "?"))
            .Invoke());
    }

    [Fact]
    public async Task TaskResultDoBuilderSupportsAdvancedWorkflows()
    {
        var disposableResource = new RecordingAsyncDisposable();

        var valueTaskDriven = await TaskResults.Do.Run(scope => scope.Return(1)).Invoke();
        Assert.Equal(1, valueTaskDriven.Value);

        var valueTaskExecution = await TaskResults.Do.ExecuteAsync(scope => scope.Return(2));
        Assert.True(valueTaskExecution.IsSuccess);

        Func<TaskResultDoScope, Task<int>> complexWorkflow = async scope =>
        {
            var value = await scope.Bind(TaskResults.Return(5));
            scope.Ensure(value == 5, "should pass");
            await foreach (var item in scope.ForEach(AsyncEnumerableFactory.From(1, 2)))
            {
                scope.Ensure(item > 0, "positive");
            }

            await using (await scope.Use(TaskResults.Return<IAsyncDisposable>(disposableResource)))
            {
            }

            await foreach (var _ in scope.ForEach(TaskResults.Return<IAsyncEnumerable<int>>(AsyncEnumerableFactory.From(1))))
            {
            }

            await foreach (var _ in scope.ForEach(AsyncEnumerableFactory.From(1)))
            {
            }

            await scope.Await(TaskResults.Return(1));
            await scope.FromTask(Task.FromResult(2));
            await scope.FromResult(Result<int>.Ok(3));
            var final = await scope.Return(value * 2);
            return final;
        };

        var result = await TaskResults.Do.Run(complexWorkflow).Invoke();

        Assert.Equal(10, result.Value);
        Assert.True(disposableResource.Disposed);

        Func<TaskResultDoScope, Task<int>> shortCircuitWorkflow = scope =>
        {
            scope.Ensure(false, "broken");
            return Task.FromResult(0);
        };
        var shortCircuited = await TaskResults.Do.Run(shortCircuitWorkflow).Invoke();
        Assert.Equal("broken", shortCircuited.Error);

        Func<TaskResultDoScope, Task<int>> unknownErrorWorkflow = scope =>
        {
            scope.Ensure(false, " ");
            return Task.FromResult(0);
        };
        var unknownError = await TaskResults.Do.Run(unknownErrorWorkflow).Invoke();
        Assert.Equal("Unknown error", unknownError.Error);

        Func<TaskResultDoScope, Task<int>> fromTaskFailureWorkflow = async scope =>
        {
            await scope.FromTask(Task.FromException<int>(new InvalidOperationException("boom")));
            return 0;
        };
        var fromTaskFailure = await TaskResults.Do.Run(fromTaskFailureWorkflow).Invoke();
        Assert.Equal("boom", fromTaskFailure.Error);

        var fromResultFailure = await TaskResults.Do.Run(scope => scope.FromResult(Result<int>.Fail("fail"))).Invoke();
        Assert.Equal("fail", fromResultFailure.Error);

        Func<TaskResultDoScope, Task<int>> bindFailureWorkflow = async scope =>
        {
            await scope.Bind(TaskResults.Fail<int>("bad"));
            return 0;
        };
        var bindFailure = await TaskResults.Do.Run(bindFailureWorkflow).Invoke();
        Assert.Equal("bad", bindFailure.Error);

        Func<TaskResultDoScope, ValueTask<int>> crashingWorkflow = async scope =>
        {
            await scope.Return(0);
            throw new InvalidOperationException("crash");
        };
        var executeException = await TaskResults.Do.ExecuteAsync(crashingWorkflow);
        Assert.Equal("crash", executeException.Error);
    }

    [Fact]
    public async Task IoHelpersCompose()
    {
        var io = IO.Return(5);
        Assert.Equal(5, io.Run());
        Assert.Equal(42, IO.From(() => 42).Run());

        var tapped = 0;
        var actionIo = IO.From(() => { tapped++; });
        Assert.Equal(Unit.Value, actionIo.Run());
        Assert.Equal(1, tapped);

        var mapped = io.Map(x => x * 2);
        Assert.Equal(10, mapped.Run());
        Assert.Equal(6, io.Select(x => x + 1).Run());

        var bound = io.Bind(x => IO.Return(x + 5));
        Assert.Equal(10, bound.Run());

        Assert.Equal(7, io.SelectMany(x => IO.Return(x + 2)).Run());

        io.Tap(x => tapped = x).Run();
        Assert.Equal(5, tapped);

        Assert.Equal(15, io.Apply(IO.Return<Func<int, int>>(x => x * 3)).Run());

        Assert.Equal(5, io.Select(x => x).Run());
        Assert.Equal(20, io.SelectMany(x => IO.Return(x + 10), (value, intermediate) => value + intermediate).Run());

        Assert.Equal(5, io.Then(IO.Return(5)).Run());

        Assert.True(io.ToResult().IsSuccess);
        Assert.Equal(5, io.ToOption().Value);
        Assert.True(io.ToTry().IsSuccess);
        Assert.False(IO.From<int>(() => throw new InvalidOperationException()).ToOption().HasValue);

        var taskResult = io.ToTaskResult(errorFactory: null);
        Assert.Equal(5, (await taskResult.Invoke()).Value);

        Func<int> factory = () => 42;
        Assert.Equal(42, factory.ToIO().Run());
        Assert.True(factory.ToResult().IsSuccess);
        Assert.True(factory.ToTry().IsSuccess);

        Action action = () => { };
        Assert.Equal(Unit.Value, action.ToIO().Run());
        Assert.True(action.ToResult().IsSuccess);
        Assert.True(action.ToTry().IsSuccess);

        var failingResult = IO.From(() => throw new InvalidOperationException()).ToResult();
        Assert.False(failingResult.IsSuccess);

        var failingIo = IO.From(() => throw new InvalidOperationException("io fail"));
        var failTaskResult = await failingIo.ToTaskResult(ex => ex.Message).Invoke();
        Assert.Equal("io fail", failTaskResult.Error);
    }

    [Fact]
    public async Task TaskIoExtensionsCoverScenarios()
    {
        var taskIO = TaskIO.Return(5);
        Assert.Equal(5, await taskIO.RunAsync());
        Assert.Equal(10, await taskIO.Map(x => x * 2).RunAsync());
        Assert.Equal(15, await taskIO.Bind(x => TaskIO.Return(x + 10)).RunAsync());
        Assert.Equal(6, await taskIO.Select(x => x + 1).RunAsync());

        var tapped = 0;
        await taskIO.Tap(x => { tapped = x; return Task.CompletedTask; }).RunAsync();
        Assert.Equal(5, tapped);

        Assert.Equal(10, await taskIO.Apply(TaskIO.Return<Func<int, int>>(x => x * 2)).RunAsync());

        Assert.Equal(15, await taskIO.SelectMany(x => TaskIO.Return(x + 5), (value, intermediate) => value + intermediate).RunAsync());
        Assert.Equal(7, await taskIO.SelectMany(x => TaskIO.Return(x + 2)).RunAsync());

        Assert.Equal(5, await taskIO.Then(TaskIO.Return(5)).RunAsync());

        Assert.Equal(5, await taskIO.Delay(TimeSpan.Zero).RunAsync());

        var pending = new TaskCompletionSource<int>();
        var slow = TaskIO.From(() => pending.Task);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await slow.WithCancellation(cts.Token).RunAsync());

        var disposable = new RecordingAsyncDisposable();
        var usingResult = await taskIO.Using(_ => TaskIO.Return(disposable), (value, resource) => TaskIO.Return(value + (resource.Disposed ? 0 : 1))).RunAsync();
        Assert.Equal(6, usingResult);
        Assert.True(disposable.Disposed);

        Assert.True(taskIO.ToOption().HasValue);
        Assert.True((await taskIO.ToOptionAsync()).HasValue);

        var nullTaskIO = TaskIO.Return<string?>(null);
        Assert.False(nullTaskIO.ToOption().HasValue);
        Assert.False((await nullTaskIO.ToOptionAsync()).HasValue);

        var failingIo = TaskIO.From<int>(async () => throw new InvalidOperationException("nope"));
        Assert.False(failingIo.ToOption().HasValue);
        Assert.False((await failingIo.ToOptionAsync()).HasValue);

        var result = await taskIO.ToResultAsync();
        Assert.True(result.IsSuccess);
        var failedResult = await failingIo.ToResultAsync(ex => $"{ex.Message}!");
        Assert.Equal("nope!", failedResult.Error);

        Assert.True(taskIO.ToTry().IsSuccess);
        Assert.False((await failingIo.ToTryAsync()).IsSuccess);

        Assert.Equal(taskIO, taskIO.ToTaskIO());

        var toTaskResult = taskIO.ToTaskResult(ex => ex.Message);
        Assert.Equal(5, (await toTaskResult.Invoke()).Value);
        var failingTaskResult = failingIo.ToTaskResult(ex => ex.Message);
        Assert.Equal("nope", (await failingTaskResult.Invoke()).Error);

        var fromAction = TaskIO.From(async () => { await Task.Delay(1); });
        Assert.Equal(Unit.Value, await fromAction.RunAsync());
        Assert.Throws<ArgumentNullException>(() => TaskIO.From<int>(null!));

        Assert.False(failingIo.ToTry().IsSuccess);
    }

    [Fact]
    public void TryExtensionsBehave()
    {
        var attempt = Try.Run(() => 5);
        Assert.True(attempt.IsSuccess);
        Assert.Equal(5, attempt.GetOrThrow());
        Assert.Contains("Success", attempt.ToString());

        Assert.Equal(6, attempt.Select(x => x + 1).Value);

        var failure = Try.Run<int>(() => throw new InvalidOperationException("broken"));
        Assert.True(failure.IsFailure);
        Assert.Equal("broken", failure.Exception?.Message);

        Assert.Equal(10, attempt.Map(x => x * 2).Value);
        Assert.Equal(15, attempt.Bind(x => Try<int>.Success(x + 10)).Value);

        Assert.Equal(5, attempt.Recover(_ => 5).Value);
        Assert.True(attempt.RecoverWith(_ => Try<int>.Success(5)).IsSuccess);

        Assert.Equal(5, attempt.Match(x => x, _ => 0));

        Assert.True(attempt.ToResult().IsSuccess);
        Assert.True(attempt.ToOption().HasValue);
        Assert.Equal(5, attempt.ToIO().Run());
        Assert.True(attempt.ToTaskResult().Invoke().Result.IsSuccess);

        var fallback = failure | attempt;
        Assert.True(fallback.IsSuccess);

        var projected = attempt.SelectMany(x => Try<int>.Success(x + 1), (value, inner) => value + inner);
        Assert.Equal(11, projected.Value);

        var applied = (Try<Func<int, int>>.Success(x => x + 1) * attempt).Value;
        Assert.Equal(6, applied);

        var multi = Try<Func<int, Func<int, int>>>.Success(x => y => x + y) * Try<int>.Success(2) * Try<int>.Success(3);
        Assert.Equal(5, multi.Value);
    }

    [Fact]
    public void ContinuationHelpersSupportCallCc()
    {
        var cont = Continuation.Return<int, int>(5);
        Assert.Equal(5, cont.Run(x => x));

        var constructed = Continuation.From<int, int>(k => k(7));
        Assert.Equal(7, constructed.Run(x => x));

        var mapped = cont.Map(x => x * 2);
        Assert.Equal(10, mapped.Run(x => x));

        var bound = cont.Bind(x => Continuation.Return<int, int>(x + 5));
        Assert.Equal(10, bound.Run(x => x));

        var applied = cont.Apply(Continuation.Return<int, Func<int, int>>(x => x * 3));
        Assert.Equal(15, applied.Run(x => x));

        var sequenced = cont.Then(Continuation.Return<int, int>(2));
        Assert.Equal(2, sequenced.Run(x => x));

        Assert.Equal(5, cont.ToIO(x => x).Run());
        Assert.True(cont.ToResult(x => x).IsSuccess);
        var failingResult = cont.ToResult(_ => throw new InvalidOperationException("fail"));
        Assert.False(failingResult.IsSuccess);

        var callCc = Continuation.CallCC<int, int>(escape => Continuation.Return<int, int>(escape(42).Run(x => x)));
        Assert.Equal(42, callCc.Run(x => x));

        var query = from x in Continuation.Return<int, int>(2)
                    from y in Continuation.Return<int, int>(3)
                    select x + y;
        Assert.Equal(5, query.Run(x => x));
    }
}
