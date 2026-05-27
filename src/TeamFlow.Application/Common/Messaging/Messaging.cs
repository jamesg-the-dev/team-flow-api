using MediatR;
using TeamFlow.Application.Common.Results;

namespace TeamFlow.Application.Common.Messaging;

/// <summary>Marker — used by the unit-of-work pipeline behavior to identify write requests.</summary>
public interface ICommandBase { }

public interface ICommand : IRequest<Result>, ICommandBase { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, ICommandBase { }

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand { }

public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }
