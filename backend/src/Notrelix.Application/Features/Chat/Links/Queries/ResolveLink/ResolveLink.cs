using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Links.DTOs;
using Notrelix.Application.Features.Chat.Links.Services;
using Notrelix.Application.Features.Documents.Public.PageTitles;
using Notrelix.Application.Features.Documents.Public.PageViewAccess;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;

namespace Notrelix.Application.Features.Chat.Links.Queries.ResolveLink;

/// <summary>
/// The preview of a link to a Board item or a Page (UC-CHT-G05, CHT-023): the title only when the caller can view the resource now, otherwise
/// a generic link without title, the same answer for a resource that is hidden, trashed or gone. The link is named by a product URL of this
/// Workspace or by a ResourceRef (kind and id); exactly one form is sent. Reading a link never changes the resource and grants nothing: each
/// reader resolves with their own access, through the producing contexts' published facts.
/// </summary>
public record ResolveLinkQuery(Guid WorkspaceId, string? Url, string? Kind, Guid? ResourceId)
    : IQuery<Result<LinkPreviewDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ResolveLinkQueryHandler(
    ICurrentRequestContext requestContext,
    IWorkItemNotificationFacts items,
    IResourceVisibility visibility,
    IPageViewAccess pageAccess,
    IPageTitleFacts pageTitles) : IRequestHandler<ResolveLinkQuery, Result<LinkPreviewDto>>
{
    public async Task<Result<LinkPreviewDto>> Handle(ResolveLinkQuery request, CancellationToken cancellationToken)
    {
        var target = request.Url is not null
            ? ChatLinkTargets.FromUrl(request.Url, request.WorkspaceId)
            : request.ResourceId is { } id && ChatLinkTargets.IsSupported(request.Kind) ? (request.Kind!, id) : null;
        if (target is not var (kind, resourceId))
            return Result<LinkPreviewDto>.Failure("This link is not a Board item or Page of this Workspace.");

        var userId = requestContext.UserId;
        var title = kind == ChatLinkTargets.BoardItemKind
            ? await ItemTitleAsync(request.WorkspaceId, userId, resourceId, cancellationToken)
            : await PageTitleAsync(requestContext.RequireAccountId(), request.WorkspaceId, userId, resourceId, cancellationToken);

        return Result<LinkPreviewDto>.Success(new LinkPreviewDto(kind, resourceId, title is not null, title));
    }

    private async Task<string?> ItemTitleAsync(Guid workspaceId, Guid userId, Guid itemId, CancellationToken cancellationToken)
    {
        var facts = await items.FindAsync(workspaceId, itemId, null, cancellationToken);
        if (facts is null)
            return null;

        var boards = await visibility.VisibleBoardIdsAsync(workspaceId, userId, cancellationToken);
        return boards.Contains(facts.BoardId) ? facts.Title : null;
    }

    private async Task<string?> PageTitleAsync(Guid accountId, Guid workspaceId, Guid userId, Guid pageId, CancellationToken cancellationToken)
    {
        if (!await pageAccess.CanViewAsync(accountId, workspaceId, pageId, userId, cancellationToken))
            return null;

        var titles = await pageTitles.TitlesAsync(workspaceId, [pageId], cancellationToken);
        return titles.TryGetValue(pageId, out var title) ? title : null;
    }
}

public class ResolveLinkQueryValidator : AbstractValidator<ResolveLinkQuery>
{
    public ResolveLinkQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.Url is not null ^ (x.Kind is not null || x.ResourceId is not null))
            .WithMessage("Send either a url or a kind with a resourceId.");
        RuleFor(x => x.Url).MaximumLength(ChatLinkTargets.MaxUrlLength).When(x => x.Url is not null);
        RuleFor(x => x.Kind).Must(ChatLinkTargets.IsSupported)
            .When(x => x.Kind is not null)
            .WithMessage("The kind must be work-management.board-item or documents.page.");
        RuleFor(x => x.ResourceId).NotEmpty().When(x => x.Kind is not null);
        RuleFor(x => x.Kind).NotEmpty().When(x => x.ResourceId is not null);
    }
}
