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
}

public class ItemTemplate : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public JsonValue Values { get; private set; } = null!;
    public TemplateStatus Status { get; private set; }

    private ItemTemplate() : base() { }

    public static ItemTemplate Create(Guid workspaceId, Guid boardId, string name, JsonValue values, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(values);

        var template = new ItemTemplate
        {
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Name = name.Trim(),
            Values = values,
            Status = TemplateStatus.Published
        };

        template.AddDomainEvent(new ItemTemplateCreatedDomainEvent(workspaceId, template.Id, template.Name, createdAt));
        return template;
    }
}
