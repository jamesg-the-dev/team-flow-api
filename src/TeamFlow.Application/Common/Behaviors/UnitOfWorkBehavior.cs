using MediatR;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Application.Common.Behaviors;

/// <summary>
/// Wraps every <see cref="ICommand"/> in an implicit unit-of-work commit on success.
/// Queries (<see cref="IQuery{T}"/>) are not committed.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkBehavior(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct
    )
    {
        var response = await next();

        if (request is ICommandBase)
            await _unitOfWork.SaveChangesAsync(ct);

        return response;
    }
}
