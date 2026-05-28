using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Channels.Commands.UpdateChannel;

public sealed record UpdateChannelCommand(Guid ChannelId, string? Name, string? Topic, bool ClearTopic = false)
    : ICommand;

public sealed class UpdateChannelValidator : AbstractValidator<UpdateChannelCommand>
{
    public UpdateChannelValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.Name)
            .Matches("^[a-z0-9][a-z0-9-_]{0,79}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Name))
            .WithMessage("Channel name must be lowercase alphanumerics, dashes or underscores.");
        RuleFor(x => x.Topic).MaximumLength(500);
        RuleFor(x => x)
            .Must(x =>
                !string.IsNullOrWhiteSpace(x.Name)
                || !string.IsNullOrWhiteSpace(x.Topic)
                || x.ClearTopic
            )
            .WithMessage("Provide at least one of 'name', 'topic', or set 'clearTopic'.");
    }
}

internal sealed class UpdateChannelHandler : ICommandHandler<UpdateChannelCommand>
{
    private readonly IChannelRepository _channels;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public UpdateChannelHandler(
        IChannelRepository channels,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _channels = channels;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateChannelCommand request, CancellationToken ct)
    {
        var channel = await _channels.GetByIdAsync(request.ChannelId, ct);
        if (channel is null)
            return Error.NotFound("Channel not found.");
        if (channel.Type == ChannelType.Direct)
            return Error.Validation("Direct-message channels cannot be renamed.");

        var actor = _currentUser.RequireUserId();
        if (
            !ChannelAuthorization.IsCreator(channel, actor)
            && !await _workspaces.IsOwnerOrAdminAsync(channel.WorkspaceId, actor, ct)
        )
            return Error.Forbidden(
                "Only the channel creator or workspace owners/admins can rename channels or change the topic."
            );

        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != channel.Name)
        {
            if (await _channels.NameExistsAsync(channel.WorkspaceId, request.Name, ct))
                return Error.Conflict($"A channel named '{request.Name}' already exists.");
            channel.Rename(request.Name);
        }

        if (request.ClearTopic)
            channel.SetTopic(null);
        else if (!string.IsNullOrWhiteSpace(request.Topic))
            channel.SetTopic(request.Topic);

        return Result.Success();
    }
}
