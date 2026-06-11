using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Workspaces.Rules;

public static class WorkspaceOwnerRules
{
    public static void EnsureNotLastOwner(Guid workspaceId, Guid userId, int ownerCount)
    {
        if (ownerCount <= 1)
            throw new BusinessRuleException("Cannot perform this action on the last owner of the workspace.");
    }
}

public static class WorkspaceRules
{
    public static void ValidateName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
        if (name.Length > 160)
            throw new BusinessRuleException("Workspace name is too long.");
    }
}

public static class TeamRules
{
    public static void ValidateName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
    }
}

public static class SpaceRules
{
    public static void ValidateName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
    }
}
