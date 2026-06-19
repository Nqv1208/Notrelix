using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;

public class DuplicateListCommandValidator : AbstractValidator<DuplicateBoardGroupCommand>
{
    public DuplicateListCommandValidator()
    {
    }
}
