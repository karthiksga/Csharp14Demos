using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // Validation applicative helpers.
    extension<TValue>(Validation<TValue> validation)
    {
        public bool IsValid => validation.IsValid;
        public IReadOnlyList<string> Errors => validation.Errors;

        public Validation<TResult> Map<TResult>(Func<TValue, TResult> selector)
            => validation.IsValid
                ? Validation<TResult>.Success(selector(validation.Value!))
                : Validation<TResult>.Failure(validation.Errors.ToArray());

        public Validation<TResult> Apply<TResult>(Validation<Func<TValue, TResult>> applicative)
        {
            if (validation.IsValid && applicative.IsValid)
            {
                return Validation<TResult>.Success(applicative.Value!(validation.Value!));
            }

            return Validation<TResult>.Failure(CombineLogs(applicative.Errors, validation.Errors).ToArray());
        }

        public Validation<TValue> Combine(Validation<TValue> other, Func<TValue, TValue, TValue> combiner)
        {
            if (validation.IsValid && other.IsValid)
            {
                return Validation<TValue>.Success(combiner(validation.Value!, other.Value!));
            }

            return Validation<TValue>.Failure(CombineLogs(validation.Errors, other.Errors).ToArray());
        }

        public Validation<TResult> Select<TResult>(Func<TValue, TResult> selector)
            => validation.IsValid
                ? Validation<TResult>.Success(selector(validation.Value!))
                : Validation<TResult>.Failure(validation.Errors.ToArray());

        public Validation<TValue> Ensure(Func<TValue, bool> requirement, string error)
            => validation.IsValid && requirement(validation.Value!)
                ? validation
                : Validation<TValue>.Failure(CombineLogs(validation.Errors, new[] { error }).ToArray());

        public Validation<TValue> Ensure(Func<TValue, bool> requirement, Func<string> errorFactory)
            => validation.IsValid && requirement(validation.Value!)
                ? validation
                : Validation<TValue>.Failure(CombineLogs(validation.Errors, new[] { errorFactory() }).ToArray());

        public Option<TValue> ToOption()
            => validation.IsValid ? Option<TValue>.Some(validation.Value!) : Option<TValue>.None;

        public Result<TValue> ToResult(string? errorMessage = null)
            => validation.IsValid
                ? Result<TValue>.Ok(validation.Value!)
                : Result<TValue>.Fail(errorMessage ?? string.Join(", ", validation.Errors));

        public TaskResult<TValue> ToTaskResult(string? errorMessage = null)
            => validation.IsValid
                ? TaskResults.Return(validation.Value!)
                : TaskResults.Fail<TValue>(errorMessage ?? string.Join(", ", validation.Errors));
    }

    extension<TValue, TResult>(Validation<Func<TValue, TResult>> applicative)
    {
        public static Validation<TResult> operator *(Validation<Func<TValue, TResult>> function, Validation<TValue> value)
            => value.Apply(function);
    }
}
