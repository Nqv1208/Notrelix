namespace Notrelix.Domain.Analytics.Rules;

public static class DashboardRules
{
    public static void ValidateName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
    }
}
