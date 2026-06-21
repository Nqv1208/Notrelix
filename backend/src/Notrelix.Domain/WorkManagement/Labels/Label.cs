using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Labels;

public sealed class LabelColor : ValueObject
{
    public string Hex { get; }

    private LabelColor() { }    private LabelColor(string hex)
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

public class Label : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public LabelColor Color { get; private set; } = null!;
    public LabelStatus Status { get; private set; }

    private Label() : base() { }

    public static Label Create(Guid workspaceId, Guid boardId, string name, LabelColor color, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(color);

        var label = new Label
        {
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Name = name.Trim(),
            Color = color,
            Status = LabelStatus.Active
        };

        label.SetAuditOnCreate(createdBy, createdAt);
        label.AddDomainEvent(new LabelCreatedDomainEvent(workspaceId, boardId, label.Id, label.Name, createdAt));

        return label;
    }

    public void Update(string name, LabelColor color, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Name = name.Trim();
        Color = color;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new LabelUpdatedDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new LabelSoftDeletedDomainEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new LabelRestoredDomainEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
