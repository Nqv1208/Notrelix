using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Templates;

public class PermissionTemplate : AggregateRoot
{
    public Guid? WorkspaceId { get; private set; } // Null for system templates
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ResourceType? TargetResourceType { get; private set; }
    public JsonValue PermissionsJson { get; private set; } = null!;
    public bool IsSystem { get; private set; }
    public PermissionTemplateStatus Status { get; private set; }

    private PermissionTemplate() : base() { }

    public static PermissionTemplate Create(string name, JsonValue permissions, Guid createdBy, DateTimeOffset createdAt, bool isSystem = false, Guid? workspaceId = null)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(permissions);

        var template = new PermissionTemplate
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            PermissionsJson = permissions,
            IsSystem = isSystem,
            Status = PermissionTemplateStatus.Active
        };

        template.AddDomainEvent(new PermissionTemplateCreatedEvent(template.WorkspaceId, template.Id, template.Name, createdBy, createdAt));
        return template;
    }
}
