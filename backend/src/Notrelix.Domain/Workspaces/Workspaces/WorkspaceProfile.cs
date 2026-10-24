namespace Notrelix.Domain.Workspaces.Workspaces;

/// <summary>
/// The stable codes of the two organisation facts a Workspace records about itself (accepted 2026-10-24):
/// how many people the company has, and the language of the workspace. Both belong to Workspaces and are
/// optional: a Workspace created before they existed has neither until an Owner or Admin sets them.
/// </summary>
public static class WorkspaceProfile
{
    /// <summary>The company-size bands, in ascending order.</summary>
    public static readonly IReadOnlyList<string> Sizes = ["1-10", "11-50", "51-200", "201-1000", "1000+"];

    /// <summary>The workspace languages.</summary>
    public static readonly IReadOnlyList<string> Languages = ["vi", "en"];

    public static string? NormalizeSize(string? size)
    {
        if (string.IsNullOrWhiteSpace(size)) return null;
        var value = size.Trim();
        if (!Sizes.Contains(value))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Settings_SizeInvalid,
                "Workspace size must be one of 1-10, 11-50, 51-200, 201-1000 or 1000+.");
        return value;
    }

    public static string? NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)) return null;
        var value = language.Trim().ToLowerInvariant();
        if (!Languages.Contains(value))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Settings_LanguageInvalid,
                "Workspace language must be vi or en.");
        return value;
    }
}
