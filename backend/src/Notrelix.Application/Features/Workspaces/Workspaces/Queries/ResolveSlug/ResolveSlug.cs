using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.ResolveSlug;

public record ResolveSlugQuery(
    Guid AccountId,
    string Slug
) : IQuery<Result<WorkspaceDto>>, IAnonymousRequest, IReadRequest, IGlobalRequest;

public class ResolveSlugQueryHandler : IRequestHandler<ResolveSlugQuery, Result<WorkspaceDto>>
{
    private readonly IWorkspaceSlugResolver _resolver;

    public ResolveSlugQueryHandler(IWorkspaceSlugResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<Result<WorkspaceDto>> Handle(ResolveSlugQuery request, CancellationToken ct)
    {
        // The caller is anonymous and has no tenant: the card is read through the Workspaces read port, which
        // answers one active workspace by account and slug and nothing else.
        var card = await _resolver.ResolveAsync(request.AccountId, request.Slug, ct);

        if (card is null)
            return Result<WorkspaceDto>.Failure("Workspace not found.");

        return Result<WorkspaceDto>.Success(new WorkspaceDto(
            card.Id,
            card.Name,
            card.Slug,
            card.Description,
            card.IsPersonal,
            "free",
            null,
            null,
            null,
            false,
            card.MemberCount,
            card.CreatedAt.UtcDateTime,
            null,
            card.Version,
            card.Size,
            card.Language
        ));
    }
}
