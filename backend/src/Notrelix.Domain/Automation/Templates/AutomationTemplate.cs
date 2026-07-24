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
        template.RaiseDomainEvent(new Events.AutomationTemplateCreatedDomainEvent(
            template.Id, template.Name, createdAt));

        return template;
    }

    public void UpdateName(string newName, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newName);
        Name = newName.Trim();
        SetAuditOnUpdate(updatedBy, updatedAt);
        RaiseDomainEvent(new Events.AutomationTemplateUpdatedDomainEvent(Id, updatedAt));
    }

    public void UpdateDefinition(JsonValue newDefinition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newDefinition);
        Definition = newDefinition;
        SetAuditOnUpdate(updatedBy, updatedAt);
        RaiseDomainEvent(new Events.AutomationTemplateUpdatedDomainEvent(Id, updatedAt));
    }

    public void Publish(DateTimeOffset publishedAt)
    {
        EnsureNotDeleted();
        if (Status == AutomationTemplateStatus.Published) return;
        Status = AutomationTemplateStatus.Published;
        SetAuditOnUpdate(null, publishedAt);
        RaiseDomainEvent(new Events.AutomationTemplatePublishedDomainEvent(Id, publishedAt));
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        if (Status == AutomationTemplateStatus.Archived)
            throw new BusinessRuleException(BusinessRuleCodes.Automation_Template_AlreadyArchived, "Template is already archived.");
        Status = AutomationTemplateStatus.Archived;
        SetAuditOnUpdate(null, archivedAt);
        RaiseDomainEvent(new Events.AutomationTemplateArchivedDomainEvent(Id, archivedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        EnsureNotDeleted();
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        RaiseDomainEvent(new Events.AutomationTemplateSoftDeletedDomainEvent(Id, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        RaiseDomainEvent(new Events.AutomationTemplateRestoredDomainEvent(Id, restoredAt));
    }
}
