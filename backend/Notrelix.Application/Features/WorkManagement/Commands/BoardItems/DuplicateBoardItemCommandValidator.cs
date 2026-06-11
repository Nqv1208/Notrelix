using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class DuplicateCardCommandValidator : AbstractValidator<DuplicateBoardItemCommand>
{
    public DuplicateCardCommandValidator()
    {
    }
}
