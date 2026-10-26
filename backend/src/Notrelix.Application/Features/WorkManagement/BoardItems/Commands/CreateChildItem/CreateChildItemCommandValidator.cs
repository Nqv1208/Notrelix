namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateChildItem;

public class CreateChildItemCommandValidator : AbstractValidator<CreateChildItemCommand>
{
    public CreateChildItemCommandValidator()
    {
        RuleFor(x => x.ParentItemId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}
