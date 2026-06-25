namespace Notrelix.Domain.WorkManagement.Rules;

public static class BoardViewRules
{
    public static void ValidateName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
    }
}
