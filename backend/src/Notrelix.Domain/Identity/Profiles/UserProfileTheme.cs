namespace Notrelix.Domain.Identity.Profiles;

public static class UserProfileTheme
{
    public const string Light = "light";
    public const string Dark = "dark";
    public const string System = "system";

    public static bool IsValid(string theme)
    {
        var lower = theme.Trim().ToLowerInvariant();
        return lower == Light || lower == Dark || lower == System;
    }
}
