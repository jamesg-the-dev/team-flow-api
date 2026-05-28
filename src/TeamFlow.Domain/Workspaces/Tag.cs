using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Workspaces;

/// <summary>Reusable tag scoped to a workspace; referenced by Project and Task aggregates.</summary>
public sealed class Tag : Entity
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string ColorHex { get; private set; } = "#94A3B8";

    private Tag() { }

    private Tag(Guid id, Guid workspaceId, string name, string colorHex)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Name = name;
        ColorHex = colorHex;
    }

    internal static Tag Create(Guid workspaceId, string name, string colorHex)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw DomainException.Invariant("Tag name required.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(colorHex, "^#[0-9A-Fa-f]{6}$"))
            throw DomainException.Invariant("Tag color must be a 7-char hex value.");
        return new Tag(Guid.CreateVersion7(), workspaceId, name.Trim(), colorHex);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw DomainException.Invariant("Tag name required.");
        Name = name.Trim();
    }

    public void ChangeColor(string colorHex)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(colorHex, "^#[0-9A-Fa-f]{6}$"))
            throw DomainException.Invariant("Tag color must be a 7-char hex value.");
        ColorHex = colorHex;
    }
}
