namespace Notrelix.Application.Common.CQRS;

public interface IWorkspaceRequest : IUseCaseSecurityRequirement
{
    Guid WorkspaceId { get; }

    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind => UseCaseSecurityKind.WorkspaceScoped;
}
