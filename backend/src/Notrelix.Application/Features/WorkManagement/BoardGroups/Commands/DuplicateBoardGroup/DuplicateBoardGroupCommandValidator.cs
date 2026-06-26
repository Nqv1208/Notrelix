using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;

public class DuplicateBoardGroupCommandValidator : AbstractValidator<DuplicateBoardGroupCommand>
{
    public DuplicateBoardGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty();
    }
}
