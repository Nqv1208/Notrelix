using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.ReorderBoardFields;

public class ReorderBoardColumnsCommandValidator : AbstractValidator<ReorderBoardFieldsCommand>
{
    public ReorderBoardColumnsCommandValidator()
    {
    }
}
