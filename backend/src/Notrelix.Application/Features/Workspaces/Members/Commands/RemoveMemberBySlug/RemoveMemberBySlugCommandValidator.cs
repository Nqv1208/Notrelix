using FluentValidation;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMemberBySlug;

public class RemoveMemberBySlugCommandValidator : AbstractValidator<RemoveMemberBySlugCommand>
{
    public RemoveMemberBySlugCommandValidator()
    {
    }
}
