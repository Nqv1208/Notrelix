using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class UpdateListCommandValidator : AbstractValidator<UpdateBoardGroupCommand>
{
    public UpdateListCommandValidator()
    {
    }
}
