using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;

namespace Notrelix.Application.Features.Governance.ShareLinks.Commands.CreateShareLink;

/// <summary>
/// The raw token is returned once, here, and never again: the link list and the DTO carry only the
/// link's metadata (Lead decision L6, secret rule GOV-010 section 35).
/// </summary>
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
    internal ResourceKind Kind => ShareLinkResources.ParseKind(ResourceKind);

    PermissionAction IRequirePermission.Action => ShareLinkResources.ActionFor(Kind);
    ResourceRef IResourceScopedRequest.Resource => ResourceRef.Create(Kind, ResourceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(Kind, ResourceId);
}

public class CreateShareLinkCommandHandler : IRequestHandler<CreateShareLinkCommand, Result<CreateShareLinkResponse>>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceSharingPolicy _sharingPolicy;
    private readonly IAuditService _auditService;

    public CreateShareLinkCommandHandler(
        IGovernanceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceSharingPolicy sharingPolicy,
        IAuditService auditService)
    {
        _sharingPolicy = sharingPolicy;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
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

        var now = _dateTimeProvider.UtcNow;
        if (request.ExpiresAt is null)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                "ShareLinkExpiryRequired",
                "A share link needs an expiry.");
        }

        DateTimeOffset expiresAt = request.ExpiresAt.Value;
        if (expiresAt <= now)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                "ShareLinkExpiryInPast",
                "A share link cannot expire in the past.");
        }

        var level = Enum.Parse<PermissionLevel>(request.Level, ignoreCase: true);

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
            now,
            expiresAt,
            level
        );

        _context.ShareLinks.Add(shareLink);

        await _auditService.RecordAsync(
            workspaceId,
            actorId,
            "CreateShareLink",
            ResourceRef.Create(request.Kind, request.ResourceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: cancellationToken,
            change: new AuditChange(
                null,
                null,
                null,
                new { linkId = shareLink.Id, level = level.ToString(), accessMode = shareLink.AccessMode.ToString(), expiresAt }));

        var dto = new ShareLinkDto(
            shareLink.Id,
            shareLink.WorkspaceId,
            shareLink.ResourceKind.ToString(),
            shareLink.ResourceId,
            shareLink.AccessMode.ToString(),
            shareLink.Level.ToString(),
            shareLink.Status == ShareLinkStatus.Active,
            shareLink.ExpiresAt);

        return Result<CreateShareLinkResponse>.Success(
            new CreateShareLinkResponse(dto, rawToken));
    }

}
