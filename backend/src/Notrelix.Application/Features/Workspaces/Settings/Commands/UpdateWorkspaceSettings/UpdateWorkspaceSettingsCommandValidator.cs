namespace Notrelix.Application.Features.Workspaces.Settings.Commands.UpdateWorkspaceSettings;

public class UpdateWorkspaceSettingsCommandValidator : AbstractValidator<UpdateWorkspaceSettingsCommand>
{
    public UpdateWorkspaceSettingsCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.DefaultMemberRole)
            .NotEmpty()
            .Must(role => role.Equals("Guest", StringComparison.OrdinalIgnoreCase)
                       || role.Equals("Member", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Default member role must be Guest or Member.");
        RuleFor(x => x.InvitationExpiryDays).InclusiveBetween(1, 30);
        RuleFor(x => x.GuestDefaultExpiryDays)
            .Must(days => days is null || WorkspaceSettings.AllowedGuestDefaultExpiryDays.Contains(days.Value))
            .WithMessage("Guest default expiry must be 30, 90 or 180 days, or none.");
        RuleFor(x => x.Size).MaximumLength(16);
        RuleFor(x => x.Language).MaximumLength(8);
        RuleFor(x => x.TimeZone).MaximumLength(64);
        RuleFor(x => x.WeekStart).MaximumLength(8);
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
