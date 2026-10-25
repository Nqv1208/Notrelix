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

    /// <summary>The time zones the settings page offers first, as IANA ids; any IANA id is accepted and a board inherits the workspace's.</summary>
    public static readonly IReadOnlyList<string> TimeZones = ["Asia/Ho_Chi_Minh", "Asia/Singapore", "Asia/Tokyo"];

    /// <summary>The time zone a workspace has until one is chosen.</summary>
    public const string DefaultTimeZone = "Asia/Ho_Chi_Minh";

    /// <summary>The first day of the week a workspace may use.</summary>
    public static readonly IReadOnlyList<string> WeekStarts = ["monday", "sunday"];

    /// <summary>The first day of the week a workspace has until one is chosen.</summary>
    public const string DefaultWeekStart = "monday";

    /// <summary>
    /// A workspace time zone is any IANA id the runtime knows. The id is stored as the runtime names it, so
    /// a different casing of a known id is accepted and kept in its canonical spelling.
    /// </summary>
    public static string? NormalizeTimeZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone)) return null;
        var value = timeZone.Trim();
        if (value.Length <= 64 && (value.Contains('/') || string.Equals(value, "UTC", StringComparison.OrdinalIgnoreCase)))
        {
            // The runtime's own spelling of the id, whatever casing the caller used.
            var listed = TimeZoneInfo.GetSystemTimeZones()
                .FirstOrDefault(candidate => string.Equals(candidate.Id, value, StringComparison.OrdinalIgnoreCase));
            if (listed is not null) return listed.Id;

            // An alias the runtime resolves but does not list.
            if (TimeZoneInfo.TryFindSystemTimeZoneById(value, out var zone)) return zone.Id;
        }

        throw new BusinessRuleException(
            WorkspaceRuleCodes.Workspaces_Settings_TimeZoneInvalid,
            "Workspace time zone must be an IANA time zone id such as Asia/Ho_Chi_Minh.");
    }

    public static string? NormalizeWeekStart(string? weekStart)
    {
        if (string.IsNullOrWhiteSpace(weekStart)) return null;
        var value = weekStart.Trim().ToLowerInvariant();
        if (!WeekStarts.Contains(value))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Settings_WeekStartInvalid,
                "Workspace week start must be monday or sunday.");
        return value;
    }

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
