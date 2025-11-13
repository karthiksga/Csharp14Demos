using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FunctionalExtensions;
using FunctionalExtensions.Effects;
using FunctionalExtensions.Tests.Support;

namespace FunctionalExtensions.Tests;

public class EffectsTests
{
    [Fact]
    public async Task ChannelWriterTaskResultsCoverAllBranches()
    {
        var channel = Channel.CreateUnbounded<int>();
        var success = await channel.Writer.WriteTaskResult(1).Invoke();
        Assert.True(success.IsSuccess);
        Assert.Equal(1, await channel.Reader.ReadAsync());

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancelled = await channel.Writer.WriteTaskResult(2, cts.Token).Invoke();
        Assert.False(cancelled.IsSuccess);
        Assert.Equal("Channel write was cancelled.", cancelled.Error);

        var throwingWriter = new ThrowingChannelWriter<int>(new InvalidOperationException("boom"));
        var failed = await throwingWriter.WriteTaskResult(1).Invoke();
        Assert.Equal("boom", failed.Error);

        var complete = await channel.Writer.CompleteTaskResult().Invoke();
        Assert.True(complete.IsSuccess);
        var completeAgain = await channel.Writer.CompleteTaskResult().Invoke();
        Assert.False(completeAgain.IsSuccess);

        var secondChannel = Channel.CreateUnbounded<int>();
        var readerEffect = secondChannel.Writer.ToReaderTaskResult(5);
        var outcome = await readerEffect.RunAsync(secondChannel.Writer);
        Assert.True(outcome.IsSuccess);
        Assert.Equal(5, await secondChannel.Reader.ReadAsync());
        secondChannel.Writer.TryComplete();
    }

    [Fact]
    public async Task HttpClientTaskResultsHandleScenarios()
    {
        HttpResponseMessage SuccessResponse(HttpRequestMessage _, CancellationToken __)
            => new(HttpStatusCode.OK) { Content = JsonContent.Create(new { value = 5 }) };

        var client = new HttpClient(new FakeHttpMessageHandler((request, token) => Task.FromResult(SuccessResponse(request, token))));
        var sendResult = await client.SendTaskResult(new HttpRequestMessage(HttpMethod.Get, "https://example.com")).Invoke();
        Assert.True(sendResult.IsSuccess);

        var getJson = await client.GetJsonTaskResult<TestPayload>("https://example.com").Invoke();
        Assert.Equal(5, getJson.Value.Value);

        var postJson = await client.PostJsonTaskResult<TestPayload, TestPayload>("https://example.com", new TestPayload(10)).Invoke();
        Assert.Equal(5, postJson.Value.Value);

        var failureClient = new HttpClient(new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("failure body")
        })));
        var failure = await failureClient.SendTaskResult(new HttpRequestMessage(HttpMethod.Get, "https://example.com")).Invoke();
        Assert.Contains("failure body", failure.Error);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancelledClient = new HttpClient(new FakeHttpMessageHandler((_, token) => throw new OperationCanceledException(token)));
        var cancelled = await cancelledClient.SendTaskResult(new HttpRequestMessage(HttpMethod.Get, "https://example.com"), cts.Token).Invoke();
        Assert.Equal("HTTP request was cancelled.", cancelled.Error);

        var nullPayloadClient = new HttpClient(new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
        })));
        var nullPayload = await nullPayloadClient.GetJsonTaskResult<TestPayload>("https://example.com").Invoke();
        Assert.False(nullPayload.IsSuccess);
        Assert.Equal("HTTP payload was empty.", nullPayload.Error);

        var invalidJsonClient = new HttpClient(new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json")
        })));
        var invalidJson = await invalidJsonClient.GetJsonTaskResult<TestPayload>("https://example.com").Invoke();
        Assert.False(invalidJson.IsSuccess);

        using var cancelReadCts = new CancellationTokenSource();
        cancelReadCts.Cancel();
        var cancelledRead = await client.GetJsonTaskResult<TestPayload>("https://example.com", cancelReadCts.Token).Invoke();
        Assert.Equal("HTTP request was cancelled.", cancelledRead.Error);

        var readerEffect = client.ToReaderTaskResult(http => http.GetJsonTaskResult<TestPayload>("https://example.com"));
        var readerResult = await readerEffect.RunAsync(client);
        Assert.True(readerResult.IsSuccess);
    }

    [Fact]
    public async Task DbConnectionStateTaskResultsManageTransactions()
    {
        var connection = new StubDbConnection();
        var state = DbTransactionState.Empty;

        var effect = connection.ToStateTaskResult(async (conn, tx, _) =>
        {
            Assert.NotNull(conn);
            Assert.NotNull(tx);
            return conn.State == ConnectionState.Open ? 1 : 0;
        });

        var firstRun = await effect.Invoke(state).Invoke();
        Assert.True(firstRun.IsSuccess);
        Assert.True(firstRun.Value.State.HasTransaction);

        var reuseState = await effect.Invoke(firstRun.Value.State).Invoke();
        Assert.True(reuseState.IsSuccess);
        Assert.True(connection.OpenCalled);

        var noTransactionAllowed = await connection.ToStateTaskResult((_, _, _) => Task.FromResult(0), beginIfMissing: false).Invoke(DbTransactionState.Empty).Invoke();
        Assert.False(noTransactionAllowed.IsSuccess);

        var failingTransaction = new StubDbTransaction(throwOnRollback: true);
        failingTransaction.Attach(connection);
        var failureState = new DbTransactionState(failingTransaction, true);
        var failingEffect = connection.ToStateTaskResult<int>((_, _, _) => throw new InvalidOperationException("boom"));
        var failed = await failingEffect.Invoke(failureState).Invoke();
        Assert.Equal("boom", failed.Error);
        Assert.True(failingTransaction.DisposeCount > 0);

        var existingTransaction = new StubDbTransaction();
        existingTransaction.Attach(connection);
        var existingState = new DbTransactionState(existingTransaction, false);
        var reuseEffect = await effect.Invoke(existingState).Invoke();
        Assert.True(reuseEffect.IsSuccess);

        var dbState = new DbTransactionState(existingTransaction, true);
        var commit = await connection.CommitTransaction().Invoke(dbState).Invoke();
        Assert.True(commit.IsSuccess);
        Assert.Equal(DbTransactionState.Empty, commit.Value.State);
        Assert.Equal(1, existingTransaction.CommitCount);
        Assert.True(existingTransaction.DisposeCount > 0);

        var commitNoTransaction = await connection.CommitTransaction().Invoke(DbTransactionState.Empty).Invoke();
        Assert.False(commitNoTransaction.IsSuccess);

        var notOwnedTransaction = new DbTransactionState(existingTransaction, false);
        var skippedCommit = await connection.CommitTransaction().Invoke(notOwnedTransaction).Invoke();
        Assert.True(skippedCommit.IsSuccess);
        Assert.Equal(notOwnedTransaction, skippedCommit.Value.State);

        var failingCommitTxn = new StubDbTransaction(throwOnCommit: true);
        failingCommitTxn.Attach(connection);
        var commitFailure = await connection.CommitTransaction().Invoke(new DbTransactionState(failingCommitTxn, true)).Invoke();
        Assert.False(commitFailure.IsSuccess);

        var noDisposeTxn = new StubDbTransaction();
        noDisposeTxn.Attach(connection);
        var noDisposeCommit = await connection.CommitTransaction(dispose: false).Invoke(new DbTransactionState(noDisposeTxn, true)).Invoke();
        Assert.True(noDisposeCommit.IsSuccess);
        Assert.Equal(0, noDisposeTxn.DisposeCount);

        var rollbackTransaction = new StubDbTransaction();
        var rollbackState = new DbTransactionState(rollbackTransaction, true);
        rollbackTransaction.Attach(connection);
        var rollback = await connection.RollbackTransaction().Invoke(rollbackState).Invoke();
        Assert.True(rollback.IsSuccess);
        Assert.Equal(DbTransactionState.Empty, rollback.Value.State);
        Assert.Equal(1, rollbackTransaction.RollbackCount);
        Assert.True(rollbackTransaction.IsDisposed);

        var noRollbackNeeded = await connection.RollbackTransaction().Invoke(DbTransactionState.Empty).Invoke();
        Assert.True(noRollbackNeeded.IsSuccess);

        var rollbackFailureTransaction = new StubDbTransaction(throwOnRollback: true);
        rollbackFailureTransaction.Attach(connection);
        var rollbackFailure = await connection.RollbackTransaction().Invoke(new DbTransactionState(rollbackFailureTransaction, true)).Invoke();
        Assert.False(rollbackFailure.IsSuccess);

        var rollbackNotOwned = await connection.RollbackTransaction().Invoke(notOwnedTransaction).Invoke();
        Assert.Equal(notOwnedTransaction, rollbackNotOwned.Value.State);

        Assert.True(DbTransactionState.Empty.HasTransaction is false);
    }

    private sealed record TestPayload(int Value);

}
