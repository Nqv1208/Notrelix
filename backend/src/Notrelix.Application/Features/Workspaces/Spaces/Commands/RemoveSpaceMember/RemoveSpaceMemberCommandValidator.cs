namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.RemoveSpaceMember;

public class RemoveSpaceMemberCommandValidator : AbstractValidator<RemoveSpaceMemberCommand>
{
    public RemoveSpaceMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.SubjectType)
            .Must(t => Enum.TryParse<SpaceMemberSubjectType>(t, ignoreCase: true, out _))
            .WithMessage("Subject type must be User or Team.");
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
