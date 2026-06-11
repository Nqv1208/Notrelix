using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Documents.Templates;

public class PageTemplate : AggregateRoot
{
    public Guid? WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public JsonValue PageSnapshot { get; private set; } = null!;
    public JsonValue BlocksSnapshot { get; private set; } = null!;
    public PageTemplateStatus Status { get; private set; }

    private PageTemplate() : base() { }

    public static PageTemplate Create(string name, JsonValue pageSnapshot, JsonValue blocksSnapshot, DateTimeOffset createdAt, Guid? workspaceId = null)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(pageSnapshot);

        var template = new PageTemplate
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            PageSnapshot = pageSnapshot,
            BlocksSnapshot = blocksSnapshot,
            Status = PageTemplateStatus.Draft
        };

        template.SetAuditOnCreate(Guid.Empty, createdAt);
        template.AddDomainEvent(new PageTemplateCreatedEvent(template.WorkspaceId, template.Id, template.Name, createdAt));
        return template;
    }

    public void Publish(Guid publishedBy, DateTimeOffset publishedAt)
    {
        if (Status == PageTemplateStatus.Archived)
            throw new BusinessRuleException("Cannot publish an archived template.");

        Status = PageTemplateStatus.Published;
        SetAuditOnUpdate(publishedBy, publishedAt);
        AddDomainEvent(new PageTemplatePublishedEvent(WorkspaceId, Id, publishedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        if (Status == PageTemplateStatus.Archived) return;

        Status = PageTemplateStatus.Archived;
        SetAuditOnUpdate(archivedBy, archivedAt);
    }
}
