using Notrelix.Domain.Common;

namespace Notrelix.Domain.Documents.Templates;

public enum PageTemplateStatus
{
    Draft,
    Published,
    Archived
}

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

    public static PageTemplate Create(string name, JsonValue pageSnapshot, JsonValue blocksSnapshot, Guid? workspaceId = null)
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

        template.AddDomainEvent(new PageTemplateCreatedEvent(template.Id, template.Name));
        return template;
    }
}
