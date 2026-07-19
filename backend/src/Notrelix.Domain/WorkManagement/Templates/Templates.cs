namespace Notrelix.Domain.WorkManagement.Templates;

public class BoardTemplate : AggregateRoot
{
    public Guid? WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public JsonValue Structure { get; private set; } = null!;
    public TemplateStatus Status { get; private set; }

    private BoardTemplate() : base() { }

    public static BoardTemplate Create(string name, JsonValue structure, DateTimeOffset createdAt, Guid? workspaceId = null)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(structure);

        var template = new BoardTemplate
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Structure = structure,
            Status = TemplateStatus.Published
        };

        template.AddDomainEvent(new BoardTemplateCreatedDomainEvent(template.Id, template.Name, createdAt));
        return template;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 255);

        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Draft(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Status == TemplateStatus.Draft) return;
        if (Status == TemplateStatus.Archived)
            throw new BusinessRuleException("Cannot draft an archived template. Restore it first.");

        Status = TemplateStatus.Draft;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Publish(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Status == TemplateStatus.Published) return;
        if (Status == TemplateStatus.Archived)
            throw new BusinessRuleException("Cannot publish an archived template. Restore it first.");

        Status = TemplateStatus.Published;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        if (Status == TemplateStatus.Archived) return;

        Status = TemplateStatus.Archived;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (Status != TemplateStatus.Archived)
            throw new BusinessRuleException("Only archived templates can be restored.");

        Status = TemplateStatus.Draft;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
    }
}

public class ItemTemplate : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public JsonValue Values { get; private set; } = null!;
    public TemplateStatus Status { get; private set; }

    private ItemTemplate() : base() { }

    public static ItemTemplate Create(Guid accountId, Guid workspaceId, Guid boardId, string name, JsonValue values, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(values);
        Guard.NotEmpty(accountId);

        var template = new ItemTemplate
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Name = name.Trim(),
            Values = values,
            Status = TemplateStatus.Published
        };

        template.AddDomainEvent(new ItemTemplateCreatedDomainEvent(accountId, workspaceId, template.Id, template.Name, createdAt));
        return template;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 255);

        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Draft(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Status == TemplateStatus.Draft) return;
        if (Status == TemplateStatus.Archived)
            throw new BusinessRuleException("Cannot draft an archived template. Restore it first.");

        Status = TemplateStatus.Draft;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Publish(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Status == TemplateStatus.Published) return;
        if (Status == TemplateStatus.Archived)
            throw new BusinessRuleException("Cannot publish an archived template. Restore it first.");

        Status = TemplateStatus.Published;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        if (Status == TemplateStatus.Archived) return;

        Status = TemplateStatus.Archived;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (Status != TemplateStatus.Archived)
            throw new BusinessRuleException("Only archived templates can be restored.");

        Status = TemplateStatus.Draft;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
    }
}
