using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class ReorderBoardColumnsCommandValidator : AbstractValidator<ReorderBoardFieldsCommand>
{
    public ReorderBoardColumnsCommandValidator()
    {
    }
}
