using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Profiles;

public class UserProfile : Entity
{
    public Guid UserId { get; private set; }
    public string Timezone { get; private set; } = "UTC";
    public string Locale { get; private set; } = "vi";
    public string Theme { get; private set; } = "system";
    public string Preferences { get; private set; } = "{}";
    public DateTimeOffset? UpdatedAt { get; private set; }

    private UserProfile() : base() { }

    public static UserProfile Create(Guid userId)
    {
        Guard.NotEmpty(userId);
        return new UserProfile
        {
            UserId = userId
        };
    }

    public void UpdateTimezone(string timezone, DateTimeOffset updatedAt)
    {
        Timezone = string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone.Trim();
        UpdatedAt = updatedAt;
    }

    public void UpdateLocale(string locale, DateTimeOffset updatedAt)
    {
        Locale = string.IsNullOrWhiteSpace(locale) ? "vi" : locale.Trim();
        UpdatedAt = updatedAt;
    }

    public void UpdateTheme(string theme, DateTimeOffset updatedAt)
    {
        Theme = string.IsNullOrWhiteSpace(theme) ? "system" : theme.Trim();
        UpdatedAt = updatedAt;
    }

    public void UpdatePreferences(string preferences, DateTimeOffset updatedAt)
    {
        Preferences = string.IsNullOrWhiteSpace(preferences) ? "{}" : preferences;
        UpdatedAt = updatedAt;
    }
}
