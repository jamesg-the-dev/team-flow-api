using MediatR;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Realtime;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Application.Common.Behaviors;

/// <summary>
/// Wraps every <see cref="ICommand"/> in an implicit unit-of-work commit on success.
/// Queries (<see cref="IQuery{T}"/>) are not committed.
/// After a successful commit, any realtime events buffered during the request are flushed
/// to <see cref="IRealtimePublisher"/>. Publish failures are intentionally swallowed and not
/// allowed to fail the command — the database is already the source of truth.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimePublishQueue _realtimeQueue;
    private readonly IRealtimePublisher _realtimePublisher;

    public UnitOfWorkBehavior(
        IUnitOfWork unitOfWork,
        IRealtimePublishQueue realtimeQueue,
        IRealtimePublisher realtimePublisher
    )
    {
        _unitOfWork = unitOfWork;
        _realtimeQueue = realtimeQueue;
        _realtimePublisher = realtimePublisher;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct
    )
    {
        var response = await next();

        if (request is ICommandBase)
        {
            await _unitOfWork.SaveChangesAsync(ct);

            var events = _realtimeQueue.Drain();
            if (events.Count > 0)
            {
                try
                {
                    await _realtimePublisher.PublishAsync(events, ct);
                }
                catch
                {
                    // Intentionally swallowed: realtime is best-effort. Clients refetch on
                    // reconnect, and we don't want a hub outage to fail business commands.
                }
            }
        }

        return response;
    }
}
