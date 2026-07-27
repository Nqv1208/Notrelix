namespace Notrelix.Application.Features.WorkManagement.Relations.Commands.CreateBoardRelation;

public class CreateBoardRelationCommandValidator : AbstractValidator<CreateBoardRelationCommand>
{
    public CreateBoardRelationCommandValidator()
    {
        RuleFor(x => x.SourceBoardId).NotEmpty();
        RuleFor(x => x.TargetBoardId).NotEmpty();
        RuleFor(x => x.RelationType).NotEmpty().MaximumLength(50);
    }
}
