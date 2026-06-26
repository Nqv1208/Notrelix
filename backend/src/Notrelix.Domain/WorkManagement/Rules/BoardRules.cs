namespace Notrelix.Domain.WorkManagement.Rules;

public static class BoardRules
{
    public static void ValidateTitle(string title)
    {
        Guard.NotNullOrWhiteSpace(title);
    }
}
