namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveChildItem;

public class MoveChildItemCommandValidator : AbstractValidator<MoveChildItemCommand>
{
    public MoveChildItemCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.ItemId).NotEmpty();
    }
}
