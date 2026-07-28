using Notrelix.Domain.Identity.Profiles.Events;

namespace Notrelix.Domain.Identity.Profiles;

public class UserProfile : SoftDeletableAggregateRoot
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
        var pending = PrepareAuditUpdate(UserId, updatedAt);
        Timezone = string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone.Trim();
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(UserId, UserId, updatedAt));
    }

    public void UpdateLocale(string locale, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        var pending = PrepareAuditUpdate(UserId, updatedAt);
        Locale = string.IsNullOrWhiteSpace(locale) ? "vi" : locale.Trim();
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(UserId, UserId, updatedAt));
    }

    public void UpdateTheme(string theme, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        var pending = PrepareAuditUpdate(UserId, updatedAt);
        if (string.IsNullOrWhiteSpace(theme))
        {
            Theme = "system";
        }
        else
        {
            if (!UserProfileTheme.IsValid(theme))
            {
                throw new BusinessRuleException(IdentityRuleCodes.Identity_Profile_InvalidTheme, $"Invalid profile theme: {theme}.");
            }
            Theme = theme.Trim().ToLowerInvariant();
        }
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(UserId, UserId, updatedAt));
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
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Profile_InvalidPreferencesJson, "Preferences must be a valid JSON string.");
        }
        var pending = PrepareAuditUpdate(UserId, updatedAt);
        Preferences = json;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(UserId, UserId, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileSoftDeletedDomainEvent(Id, UserId, deletedBy, deletedAt, reason));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileRestoredDomainEvent(Id, UserId, restoredBy, restoredAt));
    }
}
