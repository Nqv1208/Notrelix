namespace Notrelix.Application.Features.Identity.Profiles.DTOs;

/// <summary>
/// The signed-in user's account-level presentation defaults (Identity-owned).
/// <see cref="Version"/> is the concurrency token to send back as <c>expectedVersion</c>;
/// 0 means the user has not saved preferences yet and the values are the defaults.
/// </summary>
public sealed record AccountPreferencesDto(
    string Locale,
    string Timezone,
    string Theme,
    long Version)
{
    public const string DefaultLocale = "vi";
    public const string DefaultTimezone = "UTC";
    public const string DefaultTheme = "system";

    public static AccountPreferencesDto Defaults { get; } = new(DefaultLocale, DefaultTimezone, DefaultTheme, 0);
}
