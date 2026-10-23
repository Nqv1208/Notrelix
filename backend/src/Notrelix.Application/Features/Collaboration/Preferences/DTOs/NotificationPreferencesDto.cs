using Notrelix.Domain.Collaboration.Preferences;

namespace Notrelix.Application.Features.Collaboration.Preferences.DTOs;

/// <summary>One type to change in an update; a null channel keeps its current value.</summary>
public sealed record NotificationPreferenceRowChange(string Type, bool? InApp, bool? Email);

/// <summary>One notification type with its value on each channel.</summary>
public sealed record NotificationPreferenceRowDto(string Type, bool InApp, bool Email);

/// <summary>
/// The caller's notification preference matrix in one Workspace: one row per notification type in the
/// canonical order, the effective value on each channel (an explicit choice, else the default).
/// <paramref name="Version"/> is 0 until the first change is saved and is the expected version of the next update.
/// </summary>
public sealed record NotificationPreferencesDto(long Version, IReadOnlyList<NotificationPreferenceRowDto> Rows)
{
    public static NotificationPreferencesDto From(NotificationPreferenceSet? set) => new(
        set?.Version ?? 0,
        [.. NotificationTypes.All.Select(type => new NotificationPreferenceRowDto(
            type,
            Value(set, type, NotificationChannels.InApp),
            Value(set, type, NotificationChannels.Email)))]);

    private static bool Value(NotificationPreferenceSet? set, string type, string channel) =>
        set?.IsEnabled(type, channel) ?? NotificationPreferenceDefaults.IsEnabled(type, channel);
}
