namespace Notrelix.Domain.WorkManagement.Rules;

public static class FieldEngineRules
{
    private static readonly Dictionary<FieldType, HashSet<FieldType>> AllowedTransitions = new()
    {
        [FieldType.Text] = new() { FieldType.LongText, FieldType.Link },
        [FieldType.LongText] = new() { FieldType.Text, FieldType.Link },
        [FieldType.Link] = new() { FieldType.Text, FieldType.LongText },
        [FieldType.Number] = new() { FieldType.Text },
        [FieldType.Checkbox] = new() { FieldType.Text, FieldType.Number },
        [FieldType.Select] = new() { FieldType.MultiSelect, FieldType.Status },
        [FieldType.MultiSelect] = new() { FieldType.Select },
        [FieldType.Status] = new() { FieldType.Select },
        [FieldType.Date] = new() { FieldType.Text },
        [FieldType.Person] = new() { FieldType.Text },
    };

    public static void EnsureValidTypeTransition(FieldType oldType, FieldType newType)
    {
        if (oldType == newType) return;

        if (!AllowedTransitions.TryGetValue(oldType, out var allowed) || !allowed.Contains(newType))
            throw new BusinessRuleException($"Cannot change field type from {oldType} to {newType}.");
    }
}
