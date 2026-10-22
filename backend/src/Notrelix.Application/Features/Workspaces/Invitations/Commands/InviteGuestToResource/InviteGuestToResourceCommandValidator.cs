namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteGuestToResource;

public class InviteGuestToResourceCommandValidator : AbstractValidator<InviteGuestToResourceCommand>
{
    public InviteGuestToResourceCommandValidator()
    {
        RuleFor(x => x.ResourceKind)
            .Must(k => k is WorkspaceInvitationResourceGrant.BoardKind or WorkspaceInvitationResourceGrant.PageKind)
            .WithMessage("Only a Board or a Page can be shared through a resource invitation.");
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(x => x.Level).NotEmpty().MaximumLength(16);
    }
}
