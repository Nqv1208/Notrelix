namespace Notrelix.Application.Features.WorkManagement.Relations.Commands.DeleteBoardRelation;

public class DeleteBoardRelationCommandValidator : AbstractValidator<DeleteBoardRelationCommand>
{
    public DeleteBoardRelationCommandValidator()
    {
        RuleFor(x => x.RelationId).NotEmpty();
    }
}
