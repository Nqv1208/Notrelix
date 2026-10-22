using Notrelix.Domain.Identity.Profiles.Events;

namespace Notrelix.Domain.Identity.Profiles;

public sealed class UserProfile : SoftDeletableAggregateRoot
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

    public const int MaxTimezoneLength = 64;
    public const int MaxLocaleLength = 10;

    /// <summary>
    /// Applies the account presentation defaults (locale, time zone, theme) as one atomic change:
    /// every supplied value is validated before anything is written, an unsupplied value is left
    /// alone, and a request that changes nothing is a semantic no-op (no version bump, no event).
    /// Returns true when state changed. Whether a time zone id exists in the tz database is an
    /// external fact checked by Application; here only the stored shape is guarded.
    /// </summary>
    public bool ApplyAccountPreferences(string? locale, string? timezone, string? theme, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        var nextLocale = Locale;
        var nextTimezone = Timezone;
        var nextTheme = Theme;

        if (locale is not null)
        {
            var candidate = locale.Trim();
            if (!IsLocaleShape(candidate))
            {
                throw new BusinessRuleException(IdentityRuleCodes.Identity_Profile_InvalidLocale, $"Invalid profile locale: {locale}.");
            }
            nextLocale = candidate;
        }

        if (timezone is not null)
        {
            var candidate = timezone.Trim();
            if (!IsTimezoneShape(candidate))
            {
                throw new BusinessRuleException(IdentityRuleCodes.Identity_Profile_InvalidTimezone, $"Invalid profile time zone: {timezone}.");
            }
            nextTimezone = candidate;
        }

        if (theme is not null)
        {
            if (!UserProfileTheme.IsValid(theme))
            {
                throw new BusinessRuleException(IdentityRuleCodes.Identity_Profile_InvalidTheme, $"Invalid profile theme: {theme}.");
            }
            nextTheme = theme.Trim().ToLowerInvariant();
        }

        if (string.Equals(nextLocale, Locale, StringComparison.Ordinal)
            && string.Equals(nextTimezone, Timezone, StringComparison.Ordinal)
            && string.Equals(nextTheme, Theme, StringComparison.Ordinal))
        {
            return false;
        }

        var pending = PrepareAuditUpdate(UserId, updatedAt);
        Locale = nextLocale;
        Timezone = nextTimezone;
        Theme = nextTheme;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(UserId, UserId, updatedAt));
        return true;
    }

    private static bool IsLocaleShape(string value)
    {
        // BCP 47 language with an optional region/script subtag: "vi", "en", "en-US", "zh-Hant".
        if (value.Length is < 2 or > MaxLocaleLength) return false;
        var parts = value.Split('-');
        if (parts.Length > 2) return false;
        if (parts[0].Length is < 2 or > 3 || !parts[0].All(char.IsAsciiLetter)) return false;
        return parts.Length == 1 || (parts[1].Length is >= 2 and <= 8 && parts[1].All(char.IsAsciiLetterOrDigit));
    }

    private static bool IsTimezoneShape(string value)
    {
        // IANA id such as "UTC", "Asia/Ho_Chi_Minh", "America/Argentina/Buenos_Aires".
        if (value.Length is 0 or > MaxTimezoneLength) return false;
        var segments = value.Split('/');
        if (segments.Length > 3) return false;
        return segments.All(segment =>
            segment.Length > 0
            && segment.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-' or '+'));
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

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileDeletedDomainEvent(Id, UserId, deletedBy, deletedAt, pendingDeletion.Reason));
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
