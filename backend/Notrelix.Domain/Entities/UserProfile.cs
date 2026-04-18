using Notrelix.Domain.Common;

namespace Notrelix.Domain.Entities;

public class UserProfile : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Timezone { get; private set; } = "UTC";
    public string Locale { get; private set; } = "en";
    public string Preferences { get; private set; } = "{}";
    public DateTime? UpdatedAt { get; private set; }

    public User User { get; private set; } = null!;

    private UserProfile() : base() { }

    public static UserProfile Create(Guid userId, string timezone = "UTC", string locale = "en")
    {
        return new UserProfile
        {
            UserId = userId,
            Timezone = timezone,
            Locale = locale,
            Preferences = "{}"
        };
    }

    public void UpdatePreferences(string preferencesJson, string timezone, string locale)
    {
        Preferences = string.IsNullOrWhiteSpace(preferencesJson) ? "{}" : preferencesJson;
        Timezone = string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone;
        Locale = string.IsNullOrWhiteSpace(locale) ? "en" : locale;
        UpdatedAt = DateTime.UtcNow;
    }
}
