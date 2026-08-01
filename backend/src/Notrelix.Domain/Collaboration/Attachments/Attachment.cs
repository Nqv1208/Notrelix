using Notrelix.Domain.Collaboration.Attachments.Events;
namespace Notrelix.Domain.Collaboration.Attachments;

public class Attachment : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public AttachmentType Type { get; private set; }
    public FileMetadata Metadata { get; private set; } = null!;

    private Attachment() : base() { }

    public static Attachment Create(Guid accountId, Guid workspaceId, ResourceRef target, AttachmentType type, FileMetadata metadata, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotNull(metadata);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new BusinessRuleException(CommonRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{workspaceId}', got '{target.WorkspaceId.Value}'.");

        var attachment = new Attachment
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Target = target,
            Type = type,
            Metadata = metadata
        };

        attachment.SetAuditOnCreate(createdBy, createdAt);
        attachment.RaiseDomainEvent(new AttachmentCreatedDomainEvent(accountId, workspaceId, attachment.Id, target, createdAt));
        return attachment;
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new AttachmentDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new AttachmentRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
