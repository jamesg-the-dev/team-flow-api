using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Messages.DTOs;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Application.Features.Messages.Commands.EditMessage;

public sealed record EditMessageCommand(Guid MessageId, string Body) : ICommand<MessageDto>;

public sealed class EditMessageValidator : AbstractValidator<EditMessageCommand>
{
    public EditMessageValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10_000);
    }
}

internal sealed class EditMessageHandler : ICommandHandler<EditMessageCommand, MessageDto>
{
    private readonly IMessageRepository _messages;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public EditMessageHandler(
        IMessageRepository messages,
        ICurrentUser currentUser,
        IDateTimeProvider clock
    )
    {
        _messages = messages;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<MessageDto>> Handle(EditMessageCommand request, CancellationToken ct)
    {
        var message = await _messages.GetByIdAsync(request.MessageId, ct);
        if (message is null)
            return Error.NotFound("Message not found.");

        var actor = _currentUser.RequireUserId();
        if (actor != message.AuthorId)
            return Error.Forbidden("Only the author can edit a message.");

        try
        {
            message.Edit(actor, request.Body, _clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        return message.ToDto();
    }
}
