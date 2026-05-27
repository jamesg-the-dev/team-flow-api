using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;
using TeamFlow.Domain.Workspaces.Events;

namespace TeamFlow.Domain.Workspaces;

/// <summary>
/// Workspace (tenant) aggregate root. Owns membership, invites and workspace-scoped tags.
/// All multi-tenant business data hangs off a workspace.
/// </summary>
public sealed class Workspace : AuditableAggregateRoot, ISoftDeletable
{
    private readonly List<WorkspaceMember> _members = new();
    private readonly List<WorkspaceInvite> _invites = new();
    private readonly List<Tag> _tags = new();

    public string Slug { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? LogoUrl { get; private set; }
    public string Plan { get; private set; } = "free";
    public Guid OwnerId { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public IReadOnlyCollection<WorkspaceMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<WorkspaceInvite> Invites => _invites.AsReadOnly();
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();

    private Workspace() { }

    public static Workspace Create(string slug, string name, Guid ownerId, string plan = "free")
    {
        if (string.IsNullOrWhiteSpace(slug)) throw DomainException.Invariant("Workspace slug is required.");
        if (string.IsNullOrWhiteSpace(name)) throw DomainException.Invariant("Workspace name is required.");
        if (ownerId == Guid.Empty) throw DomainException.Invariant("Owner is required.");

        var ws = new Workspace
        {
            Id = Guid.CreateVersion7(),
            Slug = slug.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            OwnerId = ownerId,
            Plan = plan
        };
        // Owner is always a member with Owner role
        ws._members.Add(new WorkspaceMember(ws.Id, ownerId, WorkspaceRole.Owner, joinedAt: DateTimeOffset.UtcNow, invitedBy: null));
        ws.Raise(new WorkspaceCreated(ws.Id, ws.OwnerId, ws.Slug));
        return ws;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw DomainException.Invariant("Name required.");
        Name = name.Trim();
    }

    public void SetLogo(string? logoUrl) => LogoUrl = logoUrl;
    public void ChangePlan(string plan) => Plan = plan;

    public WorkspaceMember AddMember(Guid userId, WorkspaceRole role, Guid invitedBy, string? title = null)
    {
        if (_members.Any(m => m.UserId == userId))
            throw DomainException.Invariant("User is already a member of this workspace.");

        var member = new WorkspaceMember(Id, userId, role, DateTimeOffset.UtcNow, invitedBy) { Title = title };
        _members.Add(member);
        Raise(new WorkspaceMemberAdded(Id, userId, role));
        return member;
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == OwnerId) throw DomainException.Invariant("Workspace owner cannot be removed.");
        var member = _members.FirstOrDefault(m => m.UserId == userId)
                     ?? throw DomainException.NotFound(nameof(WorkspaceMember), userId);
        _members.Remove(member);
        Raise(new WorkspaceMemberRemoved(Id, userId));
    }

    public void ChangeMemberRole(Guid userId, WorkspaceRole role)
    {
        if (userId == OwnerId && role != WorkspaceRole.Owner)
            throw DomainException.Invariant("The owner's role cannot be downgraded. Transfer ownership first.");
        var member = _members.FirstOrDefault(m => m.UserId == userId)
                     ?? throw DomainException.NotFound(nameof(WorkspaceMember), userId);
        member.ChangeRole(role);
    }

    public void TransferOwnership(Guid newOwnerId)
    {
        var newOwner = _members.FirstOrDefault(m => m.UserId == newOwnerId)
                       ?? throw DomainException.Invariant("New owner must already be a workspace member.");
        var current = _members.First(m => m.UserId == OwnerId);
        current.ChangeRole(WorkspaceRole.Admin);
        newOwner.ChangeRole(WorkspaceRole.Owner);
        OwnerId = newOwnerId;
        Raise(new WorkspaceOwnershipTransferred(Id, current.UserId, newOwnerId));
    }

    public WorkspaceInvite InviteUser(string email, WorkspaceRole role, string tokenHash, Guid invitedBy, DateTimeOffset expiresAt)
    {
        if (_invites.Any(i => i.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && i.AcceptedAt is null))
            throw DomainException.Invariant("An active invite already exists for that email.");
        var invite = new WorkspaceInvite(Id, email, role, tokenHash, invitedBy, expiresAt);
        _invites.Add(invite);
        Raise(new WorkspaceInviteIssued(Id, invite.Id, email));
        return invite;
    }

    public Tag CreateTag(string name, string colorHex = "#94A3B8")
    {
        if (_tags.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw DomainException.Invariant($"Tag '{name}' already exists.");
        var tag = Tag.Create(Id, name, colorHex);
        _tags.Add(tag);
        return tag;
    }

    public void SoftDelete(DateTimeOffset at) => DeletedAt = at;
}
