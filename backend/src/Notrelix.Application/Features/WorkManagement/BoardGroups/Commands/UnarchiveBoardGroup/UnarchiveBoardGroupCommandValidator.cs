using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UnarchiveBoardGroup;

public class UnarchiveListCommandValidator : AbstractValidator<UnarchiveBoardGroupCommand>
{
    public UnarchiveListCommandValidator()
    {
    }
}
