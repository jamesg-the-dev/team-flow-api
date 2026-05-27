using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Attachments;

/// <summary>
/// Polymorphic attachment scoped by (owner_kind, owner_id). The owning aggregate
/// is responsible for cleanup; we keep this aggregate intentionally small.
/// </summary>
public sealed class Attachment : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public AttachmentOwner OwnerKind { get; private set; }
    public Guid OwnerId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string MimeType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public Guid UploadedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Attachment() { }

    public static Attachment Create(
        Guid workspaceId,
        AttachmentOwner ownerKind,
        Guid ownerId,
        string fileName,
        string mimeType,
        long sizeBytes,
        string storageKey,
        Guid uploadedBy
    )
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw DomainException.Invariant("File name required.");
        if (string.IsNullOrWhiteSpace(mimeType))
            throw DomainException.Invariant("MIME type required.");
        if (sizeBytes < 0)
            throw DomainException.Invariant("Size must be non-negative.");
        if (string.IsNullOrWhiteSpace(storageKey))
            throw DomainException.Invariant("Storage key required.");
        return new Attachment
        {
            Id = Guid.CreateVersion7(),
            WorkspaceId = workspaceId,
            OwnerKind = ownerKind,
            OwnerId = ownerId,
            FileName = fileName,
            MimeType = mimeType,
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            UploadedBy = uploadedBy,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}

public interface IAttachmentRepository : IRepository<Attachment>
{
    Task<Attachment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Attachment>> ListForOwnerAsync(
        AttachmentOwner kind,
        Guid ownerId,
        CancellationToken ct = default
    );
    void Add(Attachment attachment);
    void Remove(Attachment attachment);
}
