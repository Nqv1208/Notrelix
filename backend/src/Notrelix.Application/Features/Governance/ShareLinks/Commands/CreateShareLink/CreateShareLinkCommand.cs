using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;

namespace Notrelix.Application.Features.Governance.ShareLinks.Commands.CreateShareLink;

public record CreateShareLinkResponse(
    ShareLinkDto ShareLink,
    string RawToken
);

public record CreateShareLinkCommand(
    string ResourceKind,
    Guid ResourceId,
    string Level,
    DateTime? ExpiresAt = null) : ICommand<Result<CreateShareLinkResponse>>, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IWriteRequest
{
    internal ResourceKind Kind => ParseKind(ResourceKind);

    PermissionAction IRequirePermission.Action => Kind.Value switch
    {
        "work-management.board" => PermissionAction.ShareBoardView,
        "documents.page" => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IResourceScopedRequest.Resource => ResourceRef.Create(Kind, ResourceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(Kind, ResourceId);

    private static ResourceKind ParseKind(string value) =>
        global::Notrelix.Domain.SharedKernel.ResourceKind.TryCreate(value, out var kind)
            ? kind
            : throw new ArgumentException($"Invalid resource kind '{value}'. Expected a canonical kind such as 'work-management.board'.", nameof(value));
}

public class CreateShareLinkCommandHandler : IRequestHandler<CreateShareLinkCommand, Result<CreateShareLinkResponse>>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceSharingPolicy _sharingPolicy;

    public CreateShareLinkCommandHandler(
        IGovernanceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceSharingPolicy sharingPolicy)
    {
        _sharingPolicy = sharingPolicy;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CreateShareLinkResponse>> Handle(
        CreateShareLinkCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _requestContext.UserId;
        var workspaceId = _requestContext.RequireWorkspaceId();

        // The workspace owner and admins may turn links off for the whole workspace; while off none is made.
        if (!await _sharingPolicy.AllowsShareLinksAsync(workspaceId, cancellationToken))
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                "ShareLinksTurnedOff",
                "Share links are turned off for this workspace.");
        }

        var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var tokenHash = ShareLinkTokenHash.Create(rawToken);

        var shareLink = ShareLink.Create(
            _requestContext.RequireAccountId(),
            workspaceId,
            request.Kind,
            request.ResourceId,
            tokenHash,
            ShareLinkAccessMode.WorkspaceOnly,
            actorId,
            _dateTimeProvider.UtcNow,
            request.ExpiresAt
        );

        _context.ShareLinks.Add(shareLink);

        var dto = new ShareLinkDto(
            shareLink.Id,
            shareLink.WorkspaceId,
            shareLink.ResourceKind.ToString(),
            shareLink.ResourceId,
            shareLink.TokenHash.Hash,
            shareLink.AccessMode.ToString(),
            shareLink.Status == ShareLinkStatus.Active,
            shareLink.ExpiresAt);

        return Result<CreateShareLinkResponse>.Success(
            new CreateShareLinkResponse(dto, rawToken));
    }

}
