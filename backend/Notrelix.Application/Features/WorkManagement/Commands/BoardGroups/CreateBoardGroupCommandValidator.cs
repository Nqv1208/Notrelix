using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class CreateListCommandValidator : AbstractValidator<CreateBoardGroupCommand>
{
    public CreateListCommandValidator()
    {
    }
}
