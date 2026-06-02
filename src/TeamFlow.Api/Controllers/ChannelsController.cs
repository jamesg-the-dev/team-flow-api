using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Features.Channels.Commands.AddChannelMember;
using TeamFlow.Application.Features.Channels.Commands.CreateChannel;
using TeamFlow.Application.Features.Channels.Commands.CreateOrGetDm;
using TeamFlow.Application.Features.Channels.Commands.DeleteChannel;
using TeamFlow.Application.Features.Channels.Commands.MarkChannelRead;
using TeamFlow.Application.Features.Channels.Commands.RemoveChannelMember;
using TeamFlow.Application.Features.Channels.Commands.SetChannelMute;
using TeamFlow.Application.Features.Channels.Commands.UpdateChannel;
using TeamFlow.Application.Features.Channels.DTOs;
using TeamFlow.Application.Features.Channels.Queries.GetChannel;
using TeamFlow.Application.Features.Channels.Queries.ListChannelMembers;
using TeamFlow.Application.Features.Channels.Queries.ListMyChannels;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = "authenticated")]
[Produces("application/json")]
public sealed class ChannelsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    /// <summary>Lists every channel the current user belongs to in a workspace, with unread counts.</summary>
    [HttpGet("api/v1/workspaces/{workspaceId:guid}/channels")]
    [ProducesResponseType(typeof(IReadOnlyList<MyChannelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyChannelDto>>> ListMine(
        Guid workspaceId,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new ListMyChannelsQuery(workspaceId), ct);
        return result.ToActionResult();
    }

    /// <summary>Creates a Public or Private channel. Use the DMs endpoint for direct messages.</summary>
    [HttpPost("api/v1/workspaces/{workspaceId:guid}/channels")]
    [ProducesResponseType(typeof(ChannelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ChannelDto>> Create(
        Guid workspaceId,
        [FromBody] CreateChannelRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(
            new CreateChannelCommand(workspaceId, body.Name, body.Topic, body.Type),
            ct
        );
        if (result.IsFailure)
            return result.ToActionResult();
        return CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>Idempotently fetches or creates the 1-1 DM channel between caller and <c>userId</c>.</summary>
    [HttpPost("api/v1/workspaces/{workspaceId:guid}/dms")]
    [ProducesResponseType(typeof(ChannelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChannelDto>> CreateOrGetDm(
        Guid workspaceId,
        [FromBody] CreateDmRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new CreateOrGetDmCommand(workspaceId, body.UserId), ct);
        return result.ToActionResult();
    }

    [HttpGet("api/v1/channels/{id:guid}")]
    [ProducesResponseType(typeof(ChannelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChannelDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetChannelQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPatch("api/v1/channels/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Update(
        Guid id,
        [FromBody] UpdateChannelRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(
            new UpdateChannelCommand(id, body.Name, body.Topic, body.ClearTopic),
            ct
        );
        return result.ToActionResult();
    }

    [HttpDelete("api/v1/channels/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteChannelCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpGet("api/v1/channels/{id:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<ChannelMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ChannelMemberDto>>> ListMembers(
        Guid id,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new ListChannelMembersQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/channels/{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddMember(
        Guid id,
        [FromBody] AddChannelMemberRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new AddChannelMemberCommand(id, body.UserId), ct);
        return result.ToActionResult();
    }

    [HttpDelete("api/v1/channels/{id:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        var result = await _sender.Send(new RemoveChannelMemberCommand(id, userId), ct);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/channels/{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new MarkChannelReadCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/channels/{id:guid}/mute")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Mute(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new SetChannelMuteCommand(id, Muted: true), ct);
        return result.ToActionResult();
    }

    [HttpDelete("api/v1/channels/{id:guid}/mute")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Unmute(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new SetChannelMuteCommand(id, Muted: false), ct);
        return result.ToActionResult();
    }
}

public sealed record CreateChannelRequest(string Name, string? Topic, ChannelType Type);

public sealed record UpdateChannelRequest(string? Name, string? Topic, bool ClearTopic = false);

public sealed record AddChannelMemberRequest(Guid UserId);

public sealed record CreateDmRequest(Guid UserId);
