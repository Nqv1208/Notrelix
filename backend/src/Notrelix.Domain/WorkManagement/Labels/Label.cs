using Notrelix.Domain.WorkManagement.Labels.Events;
namespace Notrelix.Domain.WorkManagement.Labels;

public sealed class LabelColor : ValueObject
{
    public string Hex { get; } = null!;

    private LabelColor() { }
    private LabelColor(string hex)
    {
        Hex = hex;
    }

    public static LabelColor Create(string hex)
    {
        Guard.NotNullOrWhiteSpace(hex);
        return new LabelColor(hex.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hex;
    }
}

public class Label : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public LabelColor Color { get; private set; } = null!;
    public LabelStatus Status { get; private set; }

    private Label() : base() { }

    public static Label Create(Guid accountId, Guid workspaceId, Guid boardId, string name, LabelColor color, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(color);
        Guard.NotEmpty(accountId);

        var label = new Label
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Name = name.Trim(),
            Color = color,
            Status = LabelStatus.Active
        };

        label.SetAuditOnCreate(createdBy, createdAt);
        label.RaiseDomainEvent(new LabelCreatedDomainEvent(accountId, workspaceId, boardId, label.Id, label.Name, createdAt));

        return label;
    }

    public void Update(string name, LabelColor color, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(color);

        var normalizedName = name.Trim();
        if (Name == normalizedName && Color == color) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = normalizedName;
        Color = color;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new LabelUpdatedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new LabelDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new LabelRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
