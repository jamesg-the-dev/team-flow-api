using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Channels.DTOs;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Channels.Commands.CreateChannel;

/// <summary>
/// Creates a Public or Private channel. DMs are not created here — use the dedicated
/// <c>POST /workspaces/{ws}/dms</c> endpoint.
/// </summary>
public sealed record CreateChannelCommand(
    Guid WorkspaceId,
    string Name,
    string? Topic,
    ChannelType Type
) : ICommand<ChannelDto>;

public sealed class CreateChannelValidator : AbstractValidator<CreateChannelCommand>
{
    public CreateChannelValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(80)
            .Matches("^[a-z0-9][a-z0-9-_]{0,79}$")
            .WithMessage("Channel name must be lowercase alphanumerics, dashes or underscores.");
        RuleFor(x => x.Topic).MaximumLength(500);
        RuleFor(x => x.Type)
            .Must(t => t == ChannelType.Public || t == ChannelType.Private)
            .WithMessage("Use the DMs endpoint to create direct-message channels.");
    }
}

internal sealed class CreateChannelHandler : ICommandHandler<CreateChannelCommand, ChannelDto>
{
    private readonly IChannelRepository _channels;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public CreateChannelHandler(
        IChannelRepository channels,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _channels = channels;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result<ChannelDto>> Handle(CreateChannelCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        if (!await _workspaces.IsMemberAsync(request.WorkspaceId, userId, ct))
            return Error.Forbidden("You are not a member of this workspace.");

        if (await _channels.NameExistsAsync(request.WorkspaceId, request.Name, ct))
            return Error.Conflict($"A channel named '{request.Name}' already exists.");

        var channel = Channel.Create(
            request.WorkspaceId,
            request.Name,
            request.Type,
            createdBy: userId,
            topic: request.Topic
        );
        _channels.Add(channel);

        return new ChannelDto(
            channel.Id,
            channel.WorkspaceId,
            channel.Name,
            channel.Topic,
            channel.Type,
            channel.CreatedBy,
            channel.CreatedAt,
            channel.Members.Count
        );
    }
}
