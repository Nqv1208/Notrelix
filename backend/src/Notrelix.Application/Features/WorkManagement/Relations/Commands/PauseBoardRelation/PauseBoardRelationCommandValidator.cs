namespace Notrelix.Application.Features.WorkManagement.Relations.Commands.PauseBoardRelation;

public class PauseBoardRelationCommandValidator : AbstractValidator<PauseBoardRelationCommand>
{
    public PauseBoardRelationCommandValidator()
    {
        RuleFor(x => x.RelationId).NotEmpty();
    }
}
