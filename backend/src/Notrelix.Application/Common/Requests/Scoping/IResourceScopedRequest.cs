namespace Notrelix.Application.Common.Requests;

public interface IResourceScopedRequest : IUseCaseSecurityRequirement
{
    ResourceRef Resource { get; }

    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind => UseCaseSecurityKind.WorkspaceScoped;
}
