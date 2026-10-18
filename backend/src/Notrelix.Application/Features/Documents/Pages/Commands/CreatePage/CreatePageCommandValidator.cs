namespace Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;

public class CreatePageCommandValidator : AbstractValidator<CreatePageCommand>
{
    public CreatePageCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ParentId).NotEqual(Guid.Empty).When(x => x.ParentId.HasValue);
        RuleFor(x => x.SpaceId).NotEqual(Guid.Empty).When(x => x.SpaceId.HasValue);
        RuleFor(x => x.SpaceId)
            .Null()
            .When(x => x.ParentId.HasValue)
            .WithMessage("A nested page follows its parent's space; do not send spaceId together with parentId.");
    }
}
