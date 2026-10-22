using Notrelix.Domain.Identity.Profiles;

namespace Notrelix.Application.Features.Identity.Profiles.Commands.UpdateAccountPreferences;

public sealed class UpdateAccountPreferencesCommandValidator : AbstractValidator<UpdateAccountPreferencesCommand>
{
    public UpdateAccountPreferencesCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion)
            .GreaterThanOrEqualTo(0)
            .WithMessage("ExpectedVersion must be the version from the last read (0 when preferences were never saved).");

        RuleFor(x => x)
            .Must(x => x.Locale is not null || x.Timezone is not null || x.Theme is not null)
            .WithName("Preferences")
            .WithMessage("Provide at least one of locale, timezone or theme.");

        RuleFor(x => x.Locale!)
            .NotEmpty().MaximumLength(UserProfile.MaxLocaleLength)
            .Matches("^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})?$")
            .WithMessage("Locale must be a language tag such as 'vi' or 'en-US'.")
            .When(x => x.Locale is not null)
            .OverridePropertyName(nameof(UpdateAccountPreferencesCommand.Locale));

        RuleFor(x => x.Timezone!)
            .NotEmpty().MaximumLength(UserProfile.MaxTimezoneLength)
            .Must(BeKnownTimeZone)
            .WithMessage("Unknown time zone.")
            .When(x => x.Timezone is not null)
            .OverridePropertyName(nameof(UpdateAccountPreferencesCommand.Timezone));

        RuleFor(x => x.Theme!)
            .Must(theme => UserProfileTheme.IsValid(theme))
            .WithMessage("Theme must be one of: light, dark, system.")
            .When(x => x.Theme is not null)
            .OverridePropertyName(nameof(UpdateAccountPreferencesCommand.Theme));
    }

    // The tz database is an external fact; resolving the id here keeps the Domain free of it.
    private static bool BeKnownTimeZone(string id)
        => TimeZoneInfo.TryFindSystemTimeZoneById(id.Trim(), out _);
}
