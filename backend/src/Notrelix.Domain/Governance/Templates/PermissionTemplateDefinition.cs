namespace Notrelix.Domain.Governance.Templates;

public sealed class PermissionTemplateDefinition : ValueObject
{
    public int SchemaVersion { get; }
    public IReadOnlyList<PermissionTemplateEntry> Entries { get; }

    private PermissionTemplateDefinition(int schemaVersion, IReadOnlyList<PermissionTemplateEntry> entries)
    {
        SchemaVersion = schemaVersion;
        Entries = entries;
    }

    public static PermissionTemplateDefinition Create(IReadOnlyList<PermissionTemplateEntry> entries)
    {
        Guard.NotNull(entries);
        if (entries.Count == 0)
            throw new BusinessRuleException(GovernanceRuleCodes.Governance_PermissionTemplate_EntriesRequired, "Permission template must have at least one entry.");

        var seen = new HashSet<(ResourceType, Governance.Permissions.PermissionAction, Governance.Permissions.PermissionEffect)>();
        foreach (var entry in entries)
        {
            var key = (entry.Resource, entry.Action, entry.Effect);
            if (!seen.Add(key))
                throw new BusinessRuleException(GovernanceRuleCodes.Governance_PermissionTemplate_DuplicateEntry, $"Duplicate entry for {entry.Resource}/{entry.Action}/{entry.Effect}.");
        }

        return new PermissionTemplateDefinition(1, entries);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SchemaVersion;
        foreach (var entry in Entries)
            yield return entry;
    }
}
