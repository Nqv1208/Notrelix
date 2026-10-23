namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;

public class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.InitialEntries)
            .Must(e => e is null || e.Count <= WorkspaceInvitationInitialEntry.MaxEntries)
            .WithMessage($"An invitation can name at most {WorkspaceInvitationInitialEntry.MaxEntries} initial entries.");
        RuleForEach(x => x.InitialEntries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.Kind).NotEmpty().MaximumLength(16);
            entry.RuleFor(e => e.TargetId).NotEmpty();
            entry.RuleFor(e => e.Role).NotEmpty().MaximumLength(16);
        });
    }
}
