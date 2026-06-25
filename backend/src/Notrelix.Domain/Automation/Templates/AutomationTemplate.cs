namespace Notrelix.Domain.Automation.Templates;

public class AutomationTemplate : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Category { get; private set; } = null!;
    public JsonValue Definition { get; private set; } = null!;
    public AutomationTemplateStatus Status { get; private set; }

    private AutomationTemplate() : base() { }

    public static AutomationTemplate Create(string name, string category, JsonValue definition, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNullOrWhiteSpace(category);
        Guard.NotNull(definition);

        var template = new AutomationTemplate
        {
            Name = name.Trim(),
            Category = category.Trim(),
            Definition = definition,
            Status = AutomationTemplateStatus.Published
        };

        template.SetAuditOnCreate(createdBy, createdAt);
        template.AddDomainEvent(new Events.AutomationTemplateCreatedDomainEvent(
            Guid.Empty, template.Id, template.Name, createdAt));

        return template;
    }

    public void UpdateName(string newName, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newName);
        Name = newName.Trim();
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new Events.AutomationTemplateUpdatedDomainEvent(Id, updatedAt));
    }

    public void UpdateDefinition(JsonValue newDefinition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newDefinition);
        Definition = newDefinition;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new Events.AutomationTemplateUpdatedDomainEvent(Id, updatedAt));
    }

    public void Publish(DateTimeOffset publishedAt)
    {
        EnsureNotDeleted();
        if (Status == AutomationTemplateStatus.Published) return;
        Status = AutomationTemplateStatus.Published;
        SetAuditOnUpdate(null, publishedAt);
        AddDomainEvent(new Events.AutomationTemplatePublishedDomainEvent(Guid.Empty, Id, publishedAt));
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        if (Status == AutomationTemplateStatus.Archived)
            throw new BusinessRuleException("Template is already archived.");
        Status = AutomationTemplateStatus.Archived;
        SetAuditOnUpdate(null, archivedAt);
        AddDomainEvent(new Events.AutomationTemplateArchivedDomainEvent(Id, archivedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        EnsureNotDeleted();
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new Events.AutomationTemplateSoftDeletedDomainEvent(Id, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        AddDomainEvent(new Events.AutomationTemplateRestoredDomainEvent(Id, restoredAt));
    }
}
