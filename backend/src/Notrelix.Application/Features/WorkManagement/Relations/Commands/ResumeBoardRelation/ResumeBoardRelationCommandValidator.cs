namespace Notrelix.Application.Features.WorkManagement.Relations.Commands.ResumeBoardRelation;

public class ResumeBoardRelationCommandValidator : AbstractValidator<ResumeBoardRelationCommand>
{
    public ResumeBoardRelationCommandValidator()
    {
        RuleFor(x => x.RelationId).NotEmpty();
    }
}
