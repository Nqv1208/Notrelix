using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class DuplicateListCommandValidator : AbstractValidator<DuplicateBoardGroupCommand>
{
    public DuplicateListCommandValidator()
    {
    }
}
