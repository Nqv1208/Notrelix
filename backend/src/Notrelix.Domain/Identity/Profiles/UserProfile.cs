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
        profile.RaiseDomainEvent(new UserProfileCreatedDomainEvent(profile.Id, userId, createdAt));
        return profile;
    }

    public void UpdateTimezone(string timezone, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Timezone = string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone.Trim();
        SetAuditOnUpdate(UserId, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(UserId, updatedAt));
    }

    public void UpdateLocale(string locale, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Locale = string.IsNullOrWhiteSpace(locale) ? "vi" : locale.Trim();
        SetAuditOnUpdate(UserId, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(UserId, updatedAt));
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
                throw new BusinessRuleException(BusinessRuleCodes.Identity_Profile_InvalidTheme, $"Invalid profile theme: {theme}.");
            }
            Theme = theme.Trim().ToLowerInvariant();
        }
        SetAuditOnUpdate(UserId, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(UserId, updatedAt));
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
            throw new BusinessRuleException(BusinessRuleCodes.Identity_Profile_InvalidPreferencesJson, "Preferences must be a valid JSON string.");
        }
        Preferences = json;
        SetAuditOnUpdate(UserId, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(UserId, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
    }
}
