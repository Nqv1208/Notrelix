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

    private static PermissionTemplateEntry[] CopyEntries(IReadOnlyList<PermissionTemplateEntry> entries)
    {
        var result = new PermissionTemplateEntry[entries.Count];
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            Guard.NotNull(entry);
            result[i] = entry;
        }
        return result;
    }

    public static PermissionTemplateDefinition Create(IReadOnlyList<PermissionTemplateEntry> entries)
    {
        Guard.NotNull(entries);
        if (entries.Count == 0)
            throw new BusinessRuleException(GovernanceRuleCodes.Governance_PermissionTemplate_EntriesRequired, "Permission template must have at least one entry.");

        var copied = CopyEntries(entries);

        var seen = new HashSet<(ResourceKind, Governance.Permissions.PermissionAction, Governance.Permissions.PermissionEffect)>();
        foreach (var entry in copied)
        {
            var key = (entry.Resource, entry.Action, entry.Effect);
            if (!seen.Add(key))
                throw new BusinessRuleException(GovernanceRuleCodes.Governance_PermissionTemplate_DuplicateEntry, $"Duplicate entry for {entry.Resource}/{entry.Action}/{entry.Effect}.");
        }

        return new PermissionTemplateDefinition(1, copied);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SchemaVersion;
        foreach (var entry in Entries)
            yield return entry;
    }
}
