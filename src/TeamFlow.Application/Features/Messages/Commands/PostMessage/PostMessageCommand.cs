using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Messages.DTOs;
using TeamFlow.Domain.Discussions;
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

    public PostMessageHandler(
        IMessageRepository messages,
        IChannelRepository channels,
        ICurrentUser currentUser
    )
    {
        _messages = messages;
        _channels = channels;
        _currentUser = currentUser;
    }

    public async Task<Result<MessageDto>> Handle(PostMessageCommand request, CancellationToken ct)
    {
        var channel = await _channels.GetByIdAsync(request.ChannelId, ct);
        if (channel is null)
            return Error.NotFound("Channel not found.");

        var author = _currentUser.RequireUserId();
        if (!channel.Members.Any(m => m.UserId == author))
            return Error.Forbidden("You are not a member of this channel.");

        if (request.ParentMessageId is { } parentId)
        {
            var parent = await _messages.GetByIdAsync(parentId, ct);
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
        return message.ToDto();
    }
}
