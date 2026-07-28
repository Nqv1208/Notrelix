namespace Notrelix.Domain.Automation.Templates;

public class AutomationTemplate : SoftDeletableAggregateRoot
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
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(newName);
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = newName.Trim();
        ApplyAuditUpdate(pending);
        RaiseDomainEvent(new Events.AutomationTemplateUpdatedDomainEvent(Id, updatedAt));
    }

    public void UpdateDefinition(JsonValue newDefinition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newDefinition);
        Guard.NotEmpty(updatedBy);
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Definition = newDefinition;
        ApplyAuditUpdate(pending);
        RaiseDomainEvent(new Events.AutomationTemplateUpdatedDomainEvent(Id, updatedAt));
    }

    public void Publish(DateTimeOffset publishedAt)
    {
        EnsureNotDeleted();
        if (Status == AutomationTemplateStatus.Published) return;
        var pending = PrepareAuditUpdate(null, publishedAt);
        Status = AutomationTemplateStatus.Published;
        ApplyAuditUpdate(pending);
        RaiseDomainEvent(new Events.AutomationTemplatePublishedDomainEvent(Id, publishedAt));
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        if (Status == AutomationTemplateStatus.Archived)
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Template_AlreadyArchived, "Template is already archived.");
        var pending = PrepareAuditUpdate(null, archivedAt);
        Status = AutomationTemplateStatus.Archived;
        ApplyAuditUpdate(pending);
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
