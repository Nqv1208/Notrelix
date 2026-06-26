namespace Notrelix.Domain.Workspaces.Rules;

public static class WorkspaceRules
{
    public static void ValidateName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
        if (name.Length > 160)
            throw new BusinessRuleException("Workspace name is too long.");
    }
}
