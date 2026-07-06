namespace Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberRoleBySlug;

public class UpdateMemberRoleBySlugCommandValidator : AbstractValidator<UpdateMemberRoleBySlugCommand>
{
    public UpdateMemberRoleBySlugCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Role)
            .IsInEnum();
    }
}
