namespace Notrelix.Application.Features.Identity.Profiles.DTOs;

/// <summary>
/// The signed-in user's account-level presentation defaults (Identity-owned).
/// <see cref="Version"/> is the concurrency token to send back as <c>expectedVersion</c>;
/// 0 means the user has not saved preferences yet and the values are the defaults.
/// <see cref="Phone"/> is the person's own phone in E.164 form (null when none); it is returned to
/// the person only. <see cref="PhoneVisibleToWorkspaceMembers"/> (default false) lets members of
/// Workspaces they share see the phone; it never exposes it to guests, share links or public views.
/// </summary>
public sealed record AccountPreferencesDto(
    string Locale,
    string Timezone,
    string Theme,
    long Version,
    string? Phone = null,
    bool PhoneVisibleToWorkspaceMembers = false,
    bool ColourBlindLabels = false)
{
    public const string DefaultLocale = "vi";
    public const string DefaultTimezone = "UTC";
    public const string DefaultTheme = "system";

    public static AccountPreferencesDto Defaults { get; } = new(DefaultLocale, DefaultTimezone, DefaultTheme, 0);
}
