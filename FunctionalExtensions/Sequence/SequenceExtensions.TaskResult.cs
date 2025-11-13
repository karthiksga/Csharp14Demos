using System;
using System.Threading.Tasks;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // TaskResult monad helpers.
    extension<T>(TaskResult<T> taskResult)
    {
        public Task<Result<T>> RunAsync()
            => taskResult.Invoke();

        public TaskResult<TResult> Map<TResult>(Func<T, TResult> selector)
            => TaskResults.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                return result.Map(selector);
            });

        public TaskResult<TResult> Bind<TResult>(Func<T, TaskResult<TResult>> binder)
            => TaskResults.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    return Result<TResult>.Fail(result.Error ?? "Unknown error");
                }

                return await binder(result.Value!).Invoke().ConfigureAwait(false);
            });

        public TaskResult<TResult> Apply<TResult>(TaskResult<Func<T, TResult>> applicative)
            => TaskResults.From(async () =>
            {
                var funcResult = await applicative.Invoke().ConfigureAwait(false);
                var valueResult = await taskResult.Invoke().ConfigureAwait(false);

                if (!funcResult.IsSuccess)
                {
                    return Result<TResult>.Fail(funcResult.Error ?? "Unknown error");
                }

                if (!valueResult.IsSuccess)
                {
                    return Result<TResult>.Fail(valueResult.Error ?? "Unknown error");
                }

                return Result<TResult>.Ok(funcResult.Value!(valueResult.Value!));
            });

        public TaskResult<T> Tap(Action<T> inspector)
            => TaskResults.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    inspector(result.Value!);
                }

                return result;
            });

        public TaskResult<T> Tap(Func<T, Task> inspector)
            => TaskResults.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await inspector(result.Value!).ConfigureAwait(false);
                }

                return result;
            });

        public TaskResult<T> OrElse(Func<TaskResult<T>> fallback)
            => TaskResults.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                return result.IsSuccess ? result : await fallback().Invoke().ConfigureAwait(false);
            });

        public TaskResult<TResult> Select<TResult>(Func<T, TResult> selector)
            => taskResult.Map(selector);

        public TaskResult<TResult> SelectMany<TResult>(Func<T, TaskResult<TResult>> binder)
            => taskResult.Bind(binder);

        public TaskResult<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, TaskResult<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
            => TaskResults.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    return Result<TResult>.Fail(result.Error ?? "Unknown error");
                }

                var intermediate = await binder(result.Value!).Invoke().ConfigureAwait(false);
                if (!intermediate.IsSuccess)
                {
                    return Result<TResult>.Fail(intermediate.Error ?? "Unknown error");
                }

                return Result<TResult>.Ok(projector(result.Value!, intermediate.Value!));
            });

        public Task<Option<T>> ToOptionAsync()
            => taskResult.Invoke().ContinueWith(static t => t.Result.IsSuccess ? Option<T>.Some(t.Result.Value!) : Option<T>.None, TaskContinuationOptions.ExecuteSynchronously);

        public Task<Result<T>> ToResultAsync()
            => taskResult.Invoke();

        public TaskResult<T> Ensure(Func<T, bool> predicate, string error)
            => TaskResults.From(async () =>
            {
                var current = await taskResult.Invoke().ConfigureAwait(false);
                return current.IsSuccess && predicate(current.Value!)
                    ? current
                    : Result<T>.Fail(error);
            });

        public TaskIO<T> ToTaskIO(Func<string?, Exception>? errorFactory = null)
            => TaskIO.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    return result.Value!;
                }

                throw errorFactory?.Invoke(result.Error) ?? new InvalidOperationException(result.Error ?? "Unknown error");
            });
    }
}
