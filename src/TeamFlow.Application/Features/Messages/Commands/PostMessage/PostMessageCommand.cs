using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Realtime;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Messages.DTOs;
using TeamFlow.Application.Features.Notifications.Services;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Application.Features.Messages.Commands.PostMessage;

/// <summary>
/// Posts a message to a channel. When <paramref name="ParentMessageId"/> is provided the message
/// is a thread reply; the parent must belong to the same channel and not itself be a reply.
/// </summary>
public sealed record PostMessageCommand(
    Guid ChannelId,
    string Body,
    Guid? ParentMessageId,
    IReadOnlyList<Guid>? Mentions
) : ICommand<MessageDto>;

public sealed class PostMessageValidator : AbstractValidator<PostMessageCommand>
{
    public PostMessageValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10_000);
    }
}

internal sealed class PostMessageHandler : ICommandHandler<PostMessageCommand, MessageDto>
{
    private readonly IMessageRepository _messages;
    private readonly IChannelRepository _channels;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IRealtimePublishQueue _realtime;

    public PostMessageHandler(
        IMessageRepository messages,
        IChannelRepository channels,
        ICurrentUser currentUser,
        INotificationDispatcher dispatcher,
        IRealtimePublishQueue realtime
    )
    {
        _messages = messages;
        _channels = channels;
        _currentUser = currentUser;
        _dispatcher = dispatcher;
        _realtime = realtime;
    }

    public async Task<Result<MessageDto>> Handle(PostMessageCommand request, CancellationToken ct)
    {
        var channel = await _channels.GetByIdAsync(request.ChannelId, ct);
        if (channel is null)
            return Error.NotFound("Channel not found.");

        var author = _currentUser.RequireUserId();
        if (!channel.Members.Any(m => m.UserId == author))
            return Error.Forbidden("You are not a member of this channel.");

        Message? parent = null;
        if (request.ParentMessageId is { } parentId)
        {
            parent = await _messages.GetByIdAsync(parentId, ct);
            if (parent is null)
                return Error.NotFound("Parent message not found.");
            if (parent.ChannelId != channel.Id)
                return Error.Validation("Parent message is not in this channel.");
            if (parent.ParentId is not null)
                return Error.Validation("Cannot reply to a thread reply; reply to the root.");
        }

        // Validate any mentions are channel members; silently drop invalid mentions.
        var memberIds = channel.Members.Select(m => m.UserId).ToHashSet();
        var validMentions = request.Mentions?.Where(memberIds.Contains).Distinct().ToList();

        Message message;
        try
        {
            message = Message.Post(channel.Id, author, request.Body, request.ParentMessageId, validMentions);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        _messages.Add(message);

        // ---- Fan-out notifications ---------------------------------------------------------
        var notifications = new List<NotificationRequest>();
        var snippet = request.Body.Length > 140 ? request.Body[..140] + "…" : request.Body;
        var url = $"/channels/{channel.Id}/messages/{message.Id}";

        if (validMentions is { Count: > 0 })
        {
            foreach (var recipient in validMentions.Where(u => u != author))
            {
                notifications.Add(
                    new NotificationRequest(
                        channel.WorkspaceId,
                        recipient,
                        NotificationKind.Mention,
                        Title: "You were mentioned",
                        ActorId: author,
                        Body: snippet,
                        TargetKind: "message",
                        TargetId: message.Id,
                        Url: url
                    )
                );
            }
        }

        // Thread reply → notify the root author (if not the same person, and not already mentioned).
        if (parent is not null && parent.AuthorId != author)
        {
            var alreadyMentioned = validMentions is not null && validMentions.Contains(parent.AuthorId);
            if (!alreadyMentioned)
            {
                notifications.Add(
                    new NotificationRequest(
                        channel.WorkspaceId,
                        parent.AuthorId,
                        NotificationKind.Comment,
                        Title: "New reply in your thread",
                        ActorId: author,
                        Body: snippet,
                        TargetKind: "message",
                        TargetId: parent.Id,
                        Url: $"/channels/{channel.Id}/messages/{parent.Id}"
                    )
                );
            }
        }

        if (notifications.Count > 0)
            await _dispatcher.NotifyManyAsync(notifications, ct);

        var dto = message.ToDto();
        _realtime.Enqueue(
            new RealtimeEvent(
                RealtimeTarget.Channel,
                channel.Id,
                RealtimeEvents.MessagePosted,
                dto
            )
        );
        return dto;
    }
}
