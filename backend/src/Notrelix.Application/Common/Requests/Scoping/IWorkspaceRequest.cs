namespace Notrelix.Application.Common.Requests;

public interface IWorkspaceRequest : IUseCaseSecurityRequirement
{
    Guid WorkspaceId { get; }

    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind => UseCaseSecurityKind.WorkspaceScoped;
}
