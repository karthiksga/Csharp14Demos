using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FunctionalExtensions;

namespace FunctionalExtensions.Tests;

public class StateReaderWriterTests
{
    [Fact]
    public void ReaderExtensionsBehave()
    {
        var reader = Reader.Return<int, string>("value");
        Assert.Equal("value", reader.Run(42));

        var ask = Reader.Ask<int>();
        Assert.Equal(10, ask.Run(10));

        var from = Reader.From<int, int>(env => env * 2);
        Assert.Equal(20, from.Run(10));

        var mapped = reader.Map(value => value.ToUpperInvariant());
        Assert.Equal("VALUE", mapped.Run(0));

        var bound = from.Bind(value => Reader.Return<int, int>(value + 1));
        Assert.Equal(21, bound.Run(10));

        var localized = from.Local(env => env + 1);
        Assert.Equal(22, localized.Run(10));

        var applicative = reader.Apply(Reader.Return<int, Func<string, string>>(value => value + "!"));
        Assert.Equal("value!", applicative.Run(0));

        var query = from x in Reader.Return<int, int>(2)
                    from y in Reader.Return<int, int>(3)
                    select x + y;
        Assert.Equal(5, query.Run(0));

        var projected = reader.SelectMany(value => Reader.Return<int, string>(value + "!"), (value, intermediate) => value + intermediate);
        Assert.Equal("valuevalue!", projected.Run(0));

        var applicativeOperator = (Reader.Return<int, Func<int, int>>(env => env) * Reader.Return<int, int>(5)).Run(10);
        Assert.Equal(5, applicativeOperator);

        Assert.IsType<Func<int, string>>(reader.ToFunc());
    }

    [Fact]
    public async Task ReaderTaskResultExtensionsWork()
    {
        var reader = ReaderTaskResults.Return<string, int>(5);
        var result = await reader.RunAsync("env");
        Assert.True(result.IsSuccess);

        var ask = ReaderTaskResults.Ask<string>();
        Assert.Equal("env", (await ask.RunAsync("env")).Value);

        var fromReader = ReaderTaskResults.From<string, int>(env => TaskResults.Return(env.Length));
        Assert.Equal(3, (await fromReader.RunAsync("hey")).Value);

        var mapped = reader.Map(value => value * 2);
        Assert.Equal(10, (await mapped.RunAsync("env")).Value);

        var bound = reader.Bind(value => ReaderTaskResults.Return<string, int>(value + 5));
        Assert.Equal(10, (await bound.RunAsync("env")).Value);

        var localized = reader.Local(env => env.ToUpperInvariant());
        Assert.Equal(5, (await localized.RunAsync("env")).Value);

        var applicative = reader.Apply(ReaderTaskResults.Return<string, Func<int, int>>(value => value + 1));
        Assert.Equal(6, (await applicative.RunAsync("env")).Value);

        var query = from x in ReaderTaskResults.Return<string, int>(2)
                    from y in ReaderTaskResults.Return<string, int>(3)
                    select x + y;
        Assert.Equal(5, (await query.RunAsync("env")).Value);

        var projected = reader.SelectMany(value => ReaderTaskResults.Return<string, int>(value + 1), (value, intermediate) => value + intermediate);
        Assert.Equal(11, (await projected.RunAsync("env")).Value);

        var applicativeOperator = await (ReaderTaskResults.Return<string, Func<int, int>>(x => x * 2) * ReaderTaskResults.Return<string, int>(5)).RunAsync("env");
        Assert.Equal(10, applicativeOperator.Value);

        Assert.Equal(5, (await reader.ToTaskResult("env").Invoke()).Value);
    }

    [Fact]
    public void StateExtensionsCoverBranches()
    {
        var state = State.Return<int, string>("value");
        Assert.Equal(("value", 0), state.RunState(0));

        var get = State.Get<int>();
        Assert.Equal(5, get.Evaluate(5));

        var put = State.Put(10);
        Assert.Equal(10, put.Execute(0));

        var modify = State.Modify<int>(value => value + 1);
        Assert.Equal(1, modify.Execute(0));

        var fromState = State.From<int, int>(state => (state + 1, state + 2));
        Assert.Equal((6, 7), fromState.RunState(5));

        var mapped = state.Map(value => value.ToUpperInvariant());
        Assert.Equal("VALUE", mapped.Evaluate(0));

        var bound = get.Bind(value => State.Return<int, int>(value + 1));
        Assert.Equal(6, bound.Evaluate(5));

        var projected = get.SelectMany(x => State.Return<int, int>(x + 1), (value, intermediate) => value + intermediate);
        Assert.Equal(11, projected.Evaluate(5));

        var applicative = get.Apply(State.Return<int, Func<int, int>>(value => value * 2));
        Assert.Equal(10, applicative.Evaluate(5));

        Assert.Equal(5, get.ToIO(5).Run());
        Assert.True(get.ToResult(5).IsSuccess);

        var failingState = State.From<int, int>(_ => throw new InvalidOperationException("boom"));
        Assert.False(failingState.ToResult(0).IsSuccess);

        var applicativeOperator = (State.Return<int, Func<int, int>>(x => x + 1) * State.Return<int, int>(2)).Evaluate(0);
        Assert.Equal(3, applicativeOperator);
    }

    [Fact]
    public async Task StateTaskResultExtensionsCoverAllPaths()
    {
        var state = StateTaskResults.Return<int, int>(5);
        var run = await state.RunAsync(0);
        Assert.Equal(5, run.Value.Value);

        var get = StateTaskResults.Get<int>();
        Assert.Equal(5, (await get.Evaluate(5).Invoke()).Value);

        var put = StateTaskResults.Put(10);
        Assert.Equal(10, (await put.Execute(0).Invoke()).Value);

        var modify = StateTaskResults.Modify<int>(value => value + 1);
        Assert.Equal(1, (await modify.Execute(0).Invoke()).Value);

        var fromStateTask = StateTaskResults.From<int, int>(state => TaskResults.Return((state + 1, state + 2)));
        Assert.Equal(6, (await fromStateTask.Evaluate(5).Invoke()).Value);

        var mapped = state.Map(value => value * 2);
        Assert.Equal(10, (await mapped.Evaluate(0).Invoke()).Value);

        var bound = state.Bind(value => StateTaskResults.Return<int, int>(value + 5));
        Assert.Equal(10, (await bound.Evaluate(0).Invoke()).Value);

        var projected = state.SelectMany(value => StateTaskResults.Return<int, int>(value + 1), (value, intermediate) => value + intermediate);
        Assert.Equal(11, (await projected.Evaluate(0).Invoke()).Value);

        var applicative = state.Apply(StateTaskResults.Return<int, Func<int, int>>(value => value + 1));
        Assert.Equal(6, (await applicative.Evaluate(0).Invoke()).Value);

        var applicativeOperator = await (StateTaskResults.Return<int, Func<int, int>>(x => x * 2) * StateTaskResults.Return<int, int>(5)).Evaluate(0).Invoke();
        Assert.Equal(10, applicativeOperator.Value);

        var asTaskResult = state.ToTaskResult(0);
        Assert.Equal(5, (await asTaskResult.Invoke()).Value.Value);
    }

    [Fact]
    public async Task WriterExtensionsAccumulateLogs()
    {
        var writer = Writer.Return<int, string>(5);
        Assert.Equal(5, writer.Value);
        Assert.Empty(writer.Logs);

        var told = Writer.Tell("log");
        Assert.Equal("log", told.Logs.Single());

        var from = Writer.From(5, "a", "b");
        Assert.Equal(new[] { "a", "b" }, from.Logs);

        var mapped = writer.Map(value => value * 2);
        Assert.Equal(10, mapped.Value);

        var bound = from.Bind(value => Writer.From(value + 1, "c"));
        Assert.Equal(new[] { "a", "b", "c" }, bound.Logs);

        var appended = writer.AppendLog("extra");
        Assert.Equal(new[] { "extra" }, appended.Logs);

        var appendedMany = writer.AppendLogs();
        Assert.Empty(appendedMany.Logs);
        var appendedMultiple = writer.AppendLogs("x", "y");
        Assert.Equal(new[] { "x", "y" }, appendedMultiple.Logs);
        var preserved = from.AppendLogs();
        Assert.Equal(from.Logs, preserved.Logs);

        var tap = 0;
        writer.Tap(value => tap = value);
        Assert.Equal(5, tap);

        var tapLogs = new List<string>();
        from.TapLogs(logs => tapLogs.AddRange(logs));
        Assert.Equal(new[] { "a", "b" }, tapLogs);

        var query = from x in Writer.Return<int, string>(2)
                    from y in Writer.Return<int, string>(3)
                    select x + y;
        Assert.Equal(5, query.Value);

        var projected = writer.SelectMany(value => Writer.From(value + 1, "p"), (value, intermediate) => value + intermediate);
        Assert.Equal(11, projected.Value);

        var applicative = writer.Apply(Writer.From<Func<int, int>, string>(x => x * 2, "fn"));
        Assert.Equal(new[] { "fn" }, applicative.Logs);

        var applicativeOperator = Writer.From<Func<int, int>, string>(x => x + 1, "a") * Writer.From(2, "b");
        Assert.Equal(new[] { "a", "b" }, applicativeOperator.Logs);

        var sinkLogs = new List<string>();
        var io = from.ToIO(sinkLogs.Add);
        Assert.Equal(5, io.Run());
        Assert.Equal(new[] { "a", "b" }, sinkLogs);
        Assert.Contains("extra", writer.AppendLog("extra").Logs);

        Assert.Contains("logs", writer.PrettyPrint());

        var writerTaskResult = new WriterTaskResult<int, string>(TaskResults.Return((5, (IReadOnlyList<string>)new[] { "log" })));
        var writerResult = await writerTaskResult.Invoke().Invoke();
        Assert.True(writerResult.IsSuccess);
        Assert.Equal(5, writerResult.Value.Value);
    }
}
