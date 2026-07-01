namespace Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;

public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.ResourceType).IsInEnum();
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.ContentMd).NotEmpty().MaximumLength(50000);
    }
}
