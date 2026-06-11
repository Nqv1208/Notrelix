using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Profiles.Events;

namespace Notrelix.Domain.Identity.Profiles;

public class UserProfile : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string Timezone { get; private set; } = "UTC";
    public string Locale { get; private set; } = "vi";
    public string Theme { get; private set; } = "system";
    public string Preferences { get; private set; } = "{}";

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
        AddDomainEvent(new UserProfileUpdatedEvent(UserId, updatedAt));
    }

    public void UpdateLocale(string locale, DateTimeOffset updatedAt)
    {
        Locale = string.IsNullOrWhiteSpace(locale) ? "vi" : locale.Trim();
        UpdatedAt = updatedAt;
        AddDomainEvent(new UserProfileUpdatedEvent(UserId, updatedAt));
    }

    public void UpdateTheme(string theme, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(theme))
        {
            Theme = "system";
        }
        else
        {
            if (!UserProfileTheme.IsValid(theme))
            {
                throw new BusinessRuleException($"Invalid profile theme: {theme}.");
            }
            Theme = theme.Trim().ToLowerInvariant();
        }
        UpdatedAt = updatedAt;
        AddDomainEvent(new UserProfileUpdatedEvent(UserId, updatedAt));
    }

    public void UpdatePreferences(string preferences, DateTimeOffset updatedAt)
    {
        var json = string.IsNullOrWhiteSpace(preferences) ? "{}" : preferences;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
        }
        catch (System.Text.Json.JsonException)
        {
            throw new BusinessRuleException("Preferences must be a valid JSON string.");
        }
        Preferences = json;
        UpdatedAt = updatedAt;
        AddDomainEvent(new UserProfileUpdatedEvent(UserId, updatedAt));
    }
}
