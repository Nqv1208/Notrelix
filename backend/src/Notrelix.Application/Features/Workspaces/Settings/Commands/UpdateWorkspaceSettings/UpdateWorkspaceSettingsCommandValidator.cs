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
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
