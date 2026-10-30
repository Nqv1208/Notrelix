namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;

public class DuplicateBoardGroupCommandValidator : AbstractValidator<DuplicateBoardGroupCommand>
{
    public DuplicateBoardGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty();
        RuleFor(x => x.Title)
            .MaximumLength(255)
            .When(x => x.Title is not null);
    }
}
