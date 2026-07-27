namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ClearFieldValue;

public class ClearFieldValueCommandValidator : AbstractValidator<ClearFieldValueCommand>
{
    public ClearFieldValueCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.FieldId).NotEmpty();
    }
}
