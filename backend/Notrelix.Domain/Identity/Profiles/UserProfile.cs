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

    public static UserProfile Create(Guid userId, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(userId);
        var profile = new UserProfile
        {
            UserId = userId
        };
        profile.SetAuditOnCreate(userId, createdAt);
        return profile;
    }

    public void UpdateTimezone(string timezone, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Timezone = string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone.Trim();
        SetAuditOnUpdate(UserId, updatedAt);
        AddDomainEvent(new UserProfileUpdatedEvent(UserId, updatedAt));
    }

    public void UpdateLocale(string locale, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Locale = string.IsNullOrWhiteSpace(locale) ? "vi" : locale.Trim();
        SetAuditOnUpdate(UserId, updatedAt);
        AddDomainEvent(new UserProfileUpdatedEvent(UserId, updatedAt));
    }

    public void UpdateTheme(string theme, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
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
        SetAuditOnUpdate(UserId, updatedAt);
        AddDomainEvent(new UserProfileUpdatedEvent(UserId, updatedAt));
    }

    public void UpdatePreferences(string preferences, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
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
        SetAuditOnUpdate(UserId, updatedAt);
        AddDomainEvent(new UserProfileUpdatedEvent(UserId, updatedAt));
    }
}
