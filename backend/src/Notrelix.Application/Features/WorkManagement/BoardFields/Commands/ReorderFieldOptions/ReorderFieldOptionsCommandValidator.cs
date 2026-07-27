namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.ReorderFieldOptions;

public class ReorderFieldOptionsCommandValidator : AbstractValidator<ReorderFieldOptionsCommand>
{
    public ReorderFieldOptionsCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.FieldId).NotEmpty();
        RuleFor(x => x.OrderedOptionIds).NotNull().Must(ids => ids.Count > 0);
    }
}
