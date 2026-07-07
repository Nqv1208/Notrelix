using Notrelix.Application.Common.CQRS.Security;

namespace Notrelix.Application.Common.CQRS;

public interface IResourceScopedRequest : IUseCaseSecurityRequirement
{
    ResourceRef Resource { get; }

    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind => UseCaseSecurityKind.WorkspaceScoped;
}
