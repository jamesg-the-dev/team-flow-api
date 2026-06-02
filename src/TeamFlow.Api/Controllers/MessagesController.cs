using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Features.Messages.Commands.DeleteMessage;
using TeamFlow.Application.Features.Messages.Commands.EditMessage;
using TeamFlow.Application.Features.Messages.Commands.PostMessage;
using TeamFlow.Application.Features.Messages.Commands.SetMessagePin;
using TeamFlow.Application.Features.Messages.Commands.SetMessageReaction;
using TeamFlow.Application.Features.Messages.DTOs;
using TeamFlow.Application.Features.Messages.Queries.GetMessage;
using TeamFlow.Application.Features.Messages.Queries.ListChannelMessages;
using TeamFlow.Application.Features.Messages.Queries.ListChannelPins;
using TeamFlow.Application.Features.Messages.Queries.ListThreadMessages;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = "authenticated")]
[Produces("application/json")]
public sealed class MessagesController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    // ---- channel-scoped collections --------------------------------------------------------

    /// <summary>Channel timeline (root messages only), newest first. Use <c>before</c> for cursor paging.</summary>
    [HttpGet("api/v1/channels/{channelId:guid}/messages")]
    [ProducesResponseType(typeof(IReadOnlyList<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> List(
        Guid channelId,
        [FromQuery] DateTimeOffset? before,
        [FromQuery] int take = 50,
        CancellationToken ct = default
    )
    {
        var result = await _sender.Send(new ListChannelMessagesQuery(channelId, before, take), ct);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/channels/{channelId:guid}/messages")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MessageDto>> Post(
        Guid channelId,
        [FromBody] PostMessageRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(
            new PostMessageCommand(channelId, body.Body, body.ParentMessageId, body.Mentions),
            ct
        );
        if (result.IsFailure)
            return result.ToActionResult();
        return CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value);
    }

    [HttpGet("api/v1/channels/{channelId:guid}/pins")]
    [ProducesResponseType(typeof(IReadOnlyList<MessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> ListPins(
        Guid channelId,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new ListChannelPinsQuery(channelId), ct);
        return result.ToActionResult();
    }

    // ---- single message --------------------------------------------------------------------

    [HttpGet("api/v1/messages/{id:guid}")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetMessageQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPatch("api/v1/messages/{id:guid}")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> Edit(
        Guid id,
        [FromBody] EditMessageRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new EditMessageCommand(id, body.Body), ct);
        return result.ToActionResult();
    }

    [HttpDelete("api/v1/messages/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteMessageCommand(id), ct);
        return result.ToActionResult();
    }

    // ---- threads ---------------------------------------------------------------------------

    [HttpGet("api/v1/messages/{id:guid}/thread")]
    [ProducesResponseType(typeof(IReadOnlyList<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> Thread(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new ListThreadMessagesQuery(id), ct);
        return result.ToActionResult();
    }

    // ---- pins ------------------------------------------------------------------------------

    [HttpPost("api/v1/messages/{id:guid}/pin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> Pin(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new SetMessagePinCommand(id, Pinned: true), ct);
        return result.ToActionResult();
    }

    [HttpDelete("api/v1/messages/{id:guid}/pin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> Unpin(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new SetMessagePinCommand(id, Pinned: false), ct);
        return result.ToActionResult();
    }

    // ---- reactions -------------------------------------------------------------------------

    [HttpPost("api/v1/messages/{id:guid}/reactions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> React(
        Guid id,
        [FromBody] ReactionRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(
            new SetMessageReactionCommand(id, body.Emoji, Reacted: true),
            ct
        );
        return result.ToActionResult();
    }

    [HttpDelete("api/v1/messages/{id:guid}/reactions/{emoji}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Unreact(Guid id, string emoji, CancellationToken ct)
    {
        var result = await _sender.Send(
            new SetMessageReactionCommand(id, Uri.UnescapeDataString(emoji), Reacted: false),
            ct
        );
        return result.ToActionResult();
    }
}

public sealed record PostMessageRequest(
    string Body,
    Guid? ParentMessageId,
    IReadOnlyList<Guid>? Mentions
);

public sealed record EditMessageRequest(string Body);

public sealed record ReactionRequest(string Emoji);
