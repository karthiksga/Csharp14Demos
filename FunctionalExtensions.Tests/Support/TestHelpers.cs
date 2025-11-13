using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FunctionalExtensions.Tests.Support;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _handler(request, cancellationToken);
}

internal sealed class StubDbConnection : DbConnection
{
    private ConnectionState _state;

    public Func<IsolationLevel, StubDbTransaction> TransactionFactory { get; set; } = _ => new StubDbTransaction();

    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => "Stub";
    public override string DataSource => "Stub";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => _state;

    public bool OpenCalled { get; private set; }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        var transaction = TransactionFactory(isolationLevel);
        transaction.Attach(this);
        return transaction;
    }

    public override void ChangeDatabase(string databaseName)
        => throw new NotSupportedException();

    public override void Close()
        => _state = ConnectionState.Closed;

    public override void Open()
    {
        OpenCalled = true;
        _state = ConnectionState.Open;
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        Open();
        return Task.CompletedTask;
    }

    protected override DbCommand CreateDbCommand()
        => throw new NotSupportedException();
}

internal sealed class StubDbTransaction : DbTransaction
{
    private DbConnection? _connection;
    private readonly bool _throwOnCommit;
    private readonly bool _throwOnRollback;
    private readonly bool _throwOnDispose;

    public StubDbTransaction(bool throwOnCommit = false, bool throwOnRollback = false, bool throwOnDispose = false)
    {
        _throwOnCommit = throwOnCommit;
        _throwOnRollback = throwOnRollback;
        _throwOnDispose = throwOnDispose;
    }

    public int CommitCount { get; private set; }
    public int RollbackCount { get; private set; }
    public int DisposeCount { get; private set; }

    public bool IsDisposed => DisposeCount > 0;

    public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

    protected override DbConnection DbConnection => _connection ?? throw new InvalidOperationException("Transaction is not attached.");

    public void Attach(DbConnection connection)
        => _connection = connection;

    public override void Commit()
    {
        CommitCount++;
        if (_throwOnCommit)
        {
            throw new InvalidOperationException("Commit failure");
        }
    }

    public override Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Commit();
        return Task.CompletedTask;
    }

    public override void Rollback()
    {
        RollbackCount++;
        if (_throwOnRollback)
        {
            throw new InvalidOperationException("Rollback failure");
        }
    }

    public override Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        Rollback();
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisposeCount++;
        if (_throwOnDispose)
        {
            throw new InvalidOperationException("Dispose failure");
        }
    }

    public override ValueTask DisposeAsync()
    {
        Dispose(true);
        return ValueTask.CompletedTask;
    }
}

internal sealed class ConsoleCapture : IDisposable
{
    private readonly TextWriter _original;
    private readonly StringWriter _buffer = new();

    public ConsoleCapture()
    {
        _original = Console.Out;
        Console.SetOut(_buffer);
    }

    public string Output => _buffer.ToString();

    public void Dispose()
    {
        Console.SetOut(_original);
        _buffer.Dispose();
    }
}

internal sealed class ThrowingChannelWriter<T> : ChannelWriter<T>
{
    private readonly Exception _exception;

    public ThrowingChannelWriter(Exception exception)
        => _exception = exception;

    public override ValueTask WriteAsync(T item, CancellationToken cancellationToken = default)
        => ValueTask.FromException(_exception);

    public override bool TryWrite(T item)
        => throw _exception;

    public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromException<bool>(_exception);

    public override bool TryComplete(Exception? error = null) => false;
}

internal sealed class RecordingAsyncDisposable : IAsyncDisposable
{
    public bool Disposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}

internal static class AsyncEnumerableFactory
{
    public static async IAsyncEnumerable<T> From<T>(params T[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            yield return value;
        }
    }
}
