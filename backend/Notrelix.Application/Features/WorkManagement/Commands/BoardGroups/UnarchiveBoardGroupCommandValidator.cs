using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class UnarchiveListCommandValidator : AbstractValidator<UnarchiveBoardGroupCommand>
{
    public UnarchiveListCommandValidator()
    {
    }
}
