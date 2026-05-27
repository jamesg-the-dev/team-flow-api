using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Workspaces;

public sealed class WorkspaceInvite : Entity
{
    public Guid WorkspaceId { get; private set; }
    public string Email { get; private set; } = null!;
    public WorkspaceRole Role { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public Guid InvitedBy { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WorkspaceInvite() { }

    internal WorkspaceInvite(
        Guid workspaceId,
        string email,
        WorkspaceRole role,
        string tokenHash,
        Guid invitedBy,
        DateTimeOffset expiresAt
    )
        : base(Guid.CreateVersion7())
    {
        if (string.IsNullOrWhiteSpace(email))
            throw DomainException.Invariant("Email required.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw DomainException.Invariant("Token hash required.");
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw DomainException.Invariant("Expiry must be in the future.");
        WorkspaceId = workspaceId;
        Email = email.Trim().ToLowerInvariant();
        Role = role;
        TokenHash = tokenHash;
        InvitedBy = invitedBy;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Accept(DateTimeOffset at)
    {
        if (AcceptedAt is not null)
            throw DomainException.Invariant("Invite already accepted.");
        if (at > ExpiresAt)
            throw DomainException.Invariant("Invite has expired.");
        AcceptedAt = at;
    }
}
