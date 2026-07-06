namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMemberBySlug;

public class InviteMemberBySlugCommandValidator : AbstractValidator<InviteMemberBySlugCommand>
{
    public InviteMemberBySlugCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();

        RuleFor(x => x.Role)
            .IsInEnum();
    }
}
