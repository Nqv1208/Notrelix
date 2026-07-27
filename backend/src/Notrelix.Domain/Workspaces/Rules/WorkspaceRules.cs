namespace Notrelix.Domain.Workspaces.Rules;

public static class WorkspaceRules
{
    public static void ValidateName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
        if (name.Length > 160)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Workspace_NameTooLong, "Workspace name is too long.");
    }
}
