namespace Notrelix.Domain.Workspaces.Rules;

public static class SpaceRules
{
    public static void ValidateName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
    }
}
