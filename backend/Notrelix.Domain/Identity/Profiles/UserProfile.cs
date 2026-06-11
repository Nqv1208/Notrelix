namespace Notrelix.Domain.Identity.Profiles;

// Entity đại diện cho thông tin profile mở rộng của user
public class UserProfile
{
    public Guid UserId { get; private set; }
    public string Timezone { get; private set; } = "UTC";
    public string Locale { get; private set; } = "vi";
    public string Theme { get; private set; } = "system";
    public string Preferences { get; private set; } = "{}";
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private UserProfile() { }

    public static UserProfile Create(Guid userId)
    {
        return new UserProfile
        {
            UserId = userId
        };
    }

    public void UpdateTimezone(string timezone)
    {
        Timezone = string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLocale(string locale)
    {
        Locale = string.IsNullOrWhiteSpace(locale) ? "vi" : locale.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTheme(string theme)
    {
        Theme = string.IsNullOrWhiteSpace(theme) ? "system" : theme.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePreferences(string preferences)
    {
        Preferences = string.IsNullOrWhiteSpace(preferences) ? "{}" : preferences;
        UpdatedAt = DateTime.UtcNow;
    }
}
