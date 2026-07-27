using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Domain.Governance.Templates;

public sealed class PermissionTemplateEntry : ValueObject
{
    public ResourceType Resource { get; }
    public PermissionAction Action { get; }
    public PermissionEffect Effect { get; }

    private PermissionTemplateEntry(ResourceType resource, PermissionAction action, PermissionEffect effect)
    {
        Resource = resource;
        Action = action;
        Effect = effect;
    }

    public static PermissionTemplateEntry Create(ResourceType resource, PermissionAction action, PermissionEffect effect)
    {
        return new PermissionTemplateEntry(resource, action, effect);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Resource;
        yield return Action;
        yield return Effect;
    }
}
