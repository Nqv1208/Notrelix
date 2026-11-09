namespace Notrelix.Domain.Workspaces.Rules;

public static class TeamRules
{
    public static void ValidateName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
    }

    /// <summary>
    /// Team names are unique, case-insensitively, among the non-deleted Teams of one Workspace. Archived
    /// Teams keep their name. Application supplies the other Teams' names; the Domain never queries them.
    /// </summary>
    public static void EnsureUniqueName(string name, IReadOnlyCollection<string> otherTeamNames)
    {
        if (otherTeamNames.Any(other => string.Equals(other.Trim(), name, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Team_DuplicateName,
                $"A team named '{name}' already exists in this workspace.");
    }
}
