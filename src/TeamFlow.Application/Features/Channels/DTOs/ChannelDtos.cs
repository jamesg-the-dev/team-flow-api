using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Channels.DTOs;

public sealed record ChannelDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string? Topic,
    ChannelType Type,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    int MemberCount
);

/// <summary>Channel row in the user's channel list, enriched with their per-membership state.</summary>
public sealed record MyChannelDto(
    Guid Id,
    string Name,
    string? Topic,
    ChannelType Type,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastReadAt,
    bool IsMuted,
    int UnreadCount,
    DateTimeOffset? LastMessageAt
);

public sealed record ChannelMemberDto(
    Guid UserId,
    DateTimeOffset JoinedAt,
    DateTimeOffset LastReadAt,
    bool IsMuted
);
