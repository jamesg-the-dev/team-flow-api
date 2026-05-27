using FluentValidation;
using MediatR;
using TeamFlow.Application.Common.Results;

namespace TeamFlow.Application.Common.Behaviors;

/// <summary>
/// Runs FluentValidation against the incoming command/query. On failure, short-circuits with a
/// Validation <see cref="Result"/> when the response type supports it; otherwise throws.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Count == 0) return await next();

        var message = string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));
        var error = Error.Validation(message);

        // If TResponse is Result or Result<T>, return failure result; otherwise throw.
        if (typeof(Result).IsAssignableFrom(typeof(TResponse)))
        {
            if (typeof(TResponse) == typeof(Result))
                return (TResponse)(object)Result.Failure(error);

            var inner = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result).GetMethod(nameof(Result.Failure), 1, new[] { typeof(Error) })!
                .MakeGenericMethod(inner);
            return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
        }

        throw new ValidationException(failures);
    }
}
