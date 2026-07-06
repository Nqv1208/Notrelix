namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.ReorderBoardFields;

public class ReorderBoardFieldsCommandValidator : AbstractValidator<ReorderBoardFieldsCommand>
{
    public ReorderBoardFieldsCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Items).NotNull().Must(items => items.Count > 0);
    }
}
