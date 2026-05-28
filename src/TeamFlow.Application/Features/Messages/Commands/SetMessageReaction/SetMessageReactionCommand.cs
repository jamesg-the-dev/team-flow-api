using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Realtime;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Application.Features.Messages.Commands.SetMessageReaction;

/// <summary>
/// Adds or removes the caller's reaction with the given emoji on a message. Idempotent.
/// </summary>
public sealed record SetMessageReactionCommand(Guid MessageId, string Emoji, bool Reacted) : ICommand;

public sealed class SetMessageReactionValidator : AbstractValidator<SetMessageReactionCommand>
{
    public SetMessageReactionValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Emoji).NotEmpty().MaximumLength(32);
    }
}

internal sealed class SetMessageReactionHandler : ICommandHandler<SetMessageReactionCommand>
{
    private readonly IMessageRepository _messages;
    private readonly IChannelRepository _channels;
    private readonly ICurrentUser _currentUser;
    private readonly IRealtimePublishQueue _realtime;

    public SetMessageReactionHandler(
        IMessageRepository messages,
        IChannelRepository channels,
        ICurrentUser currentUser,
        IRealtimePublishQueue realtime
    )
    {
        _messages = messages;
        _channels = channels;
        _currentUser = currentUser;
        _realtime = realtime;
    }

    public async Task<Result> Handle(SetMessageReactionCommand request, CancellationToken ct)
    {
        var message = await _messages.GetByIdAsync(request.MessageId, ct);
        if (message is null)
            return Error.NotFound("Message not found.");

        var actor = _currentUser.RequireUserId();
        if (!await _channels.IsMemberAsync(message.ChannelId, actor, ct))
            return Error.Forbidden("You are not a member of this channel.");

        try
        {
            if (request.Reacted) message.React(actor, request.Emoji);
            else message.Unreact(actor, request.Emoji);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        _realtime.Enqueue(
            new RealtimeEvent(
                RealtimeTarget.Channel,
                message.ChannelId,
                RealtimeEvents.ReactionChanged,
                new
                {
                    messageId = message.Id,
                    channelId = message.ChannelId,
                    userId = actor,
                    emoji = request.Emoji,
                    reacted = request.Reacted,
                }
            )
        );
        return Result.Success();
    }
}
