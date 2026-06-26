namespace Notrelix.Domain.WorkManagement.Rules;

public static class BoardFieldRules
{
    public static void ValidateName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
    }
}
