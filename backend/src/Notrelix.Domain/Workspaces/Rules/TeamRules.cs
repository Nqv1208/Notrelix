using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Rules;

public static class TeamRules
{
    public static void ValidateName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
    }
}
