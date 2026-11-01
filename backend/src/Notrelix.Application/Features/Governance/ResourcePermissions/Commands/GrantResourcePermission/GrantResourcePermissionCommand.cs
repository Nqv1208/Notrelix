using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Public.BoardAccess;

namespace Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;

public record GrantResourcePermissionCommand(
    string ResourceKind,
    Guid ResourceId,
    string SubjectType,
    Guid SubjectId,
    string Level,
    DateTime? ExpiresAt = null,
    long? ExpectedVersion = null) : ICommand<Result<ResourcePermissionDto>>, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IRequireGrantPermission, IRequirePermissionTarget, IWriteRequest
{
    internal ResourceKind Kind => ParseKind(ResourceKind);

    PermissionAction IRequirePermission.Action => Kind.Value switch
    {
        "work-management.board" => PermissionAction.ManageBoardPermission,
        "documents.page" => PermissionAction.ManagePagePermission,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IResourceScopedRequest.Resource => ResourceRef.Create(Kind, ResourceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(Kind, ResourceId);

    // Governance owns the level vocabulary and its rank mapping; the pipeline
    // seam compares technical integer ranks only. The mapping mirrors the
    // PermissionLevel enum ordering: None=0, Viewer=1, Commenter=2, Editor=3,
    // Manager=4, Owner=5.
    int IRequireGrantPermission.RequestedPermissionRank =>
        Enum.TryParse<PermissionLevel>(Level, true, out var level) ? (int)level : 0;

    string? IRequirePermissionTarget.TargetSubjectType => SubjectType;
    Guid? IRequirePermissionTarget.TargetSubjectId => SubjectId;
    Guid? IRequirePermissionTarget.TargetPermissionId => null;

    private static ResourceKind ParseKind(string value) =>
        global::Notrelix.Domain.SharedKernel.ResourceKind.TryCreate(value, out var kind)
            ? kind
            : throw new ArgumentException($"Invalid resource kind '{value}'. Expected a canonical kind such as 'work-management.board'.", nameof(value));
}

public class GrantResourcePermissionCommandHandler : IRequestHandler<GrantResourcePermissionCommand, Result<ResourcePermissionDto>>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;
    private readonly IResourceAccessChangeRecorder _changes;
    private readonly IWorkspaceAccessResolver _workspaceAccess;
    private readonly IBoardSharingSummary _sharing;
    private readonly IBoardAccessSubjectFacts _boards;
    private readonly IAccessChangeSignal _access;

    public GrantResourcePermissionCommandHandler(
        IGovernanceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IAuditService auditService,
        IResourceAccessChangeRecorder changes,
        IWorkspaceAccessResolver workspaceAccess,
        IBoardSharingSummary sharing,
        IBoardAccessSubjectFacts boards,
        IAccessChangeSignal access)
    {
        _sharing = sharing;
        _access = access;
        _boards = boards;
        _workspaceAccess = workspaceAccess;
        _changes = changes;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
    }

    public async Task<Result<ResourcePermissionDto>> Handle(
        GrantResourcePermissionCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PermissionSubjectType>(request.SubjectType, true, out var subjectType) ||
            !Enum.TryParse<PermissionLevel>(request.Level, true, out var level))
        {
            return Result<ResourcePermissionDto>.Failure("Invalid format for enum parameters.");
        }

        var workspaceId = _requestContext.RequireWorkspaceId();
        var accountId = _requestContext.RequireAccountId();
        var actorId = _requestContext.UserId;
        var kind = request.Kind;
        var now = _dateTimeProvider.UtcNow;

        // A user subject must belong to the workspace, as AddBoardMember requires.
        if (subjectType == PermissionSubjectType.User)
        {
            var access = await _workspaceAccess.ResolveAsync(workspaceId, request.SubjectId, cancellationToken);
            if (!access.CanAccess)
            {
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                    "PermissionSubjectMustBelongToWorkspace",
                    "Permission subject must belong to the same workspace.");
            }
        }

        // A read-only board role (Observer, Guest) caps the person at Viewer, so a higher explicit grant would be
        // inert: it is refused instead, and the board role is the one source to change.
        if (subjectType == PermissionSubjectType.User && kind.Value == BoardResourceKind && (int)level > (int)PermissionLevel.Viewer)
        {
            var board = await _boards.FindAsync(workspaceId, request.ResourceId, cancellationToken);
            var role = board?.Members?.FirstOrDefault(m => m.UserId == request.SubjectId)?.Role;
            if (BoardRoleLevels.RankOf(role) is > 0 and <= 1)
            {
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                    Notrelix.Domain.Governance.GovernanceRuleCodes.Governance_ResourcePermission_ViewerRoleCapsGrant,
                    "This person is a viewer of the board, so they can only view. Change their board role first.");
            }
        }

        var existingPermission = await _context.ResourcePermissions
            .FirstOrDefaultAsync(p => p.WorkspaceId == workspaceId &&
                                      p.ResourceKind == kind &&
                                      p.ResourceId == request.ResourceId &&
                                      p.SubjectType == subjectType &&
                                      p.SubjectId == request.SubjectId, cancellationToken);

        ResourcePermission permission;
        string? previousLevel = null;
        var isBoard = kind.Value == BoardResourceKind;
        if (existingPermission != null)
        {
            if (existingPermission.Level == level)
            {
                // Same subject, same level: nothing changed, so nothing is recorded.
                return Result<ResourcePermissionDto>.Success(ToDto(existingPermission));
            }
            else
            {
                // Two managers changing one grant at once: the second names a version that is gone.
                if (request.ExpectedVersion != existingPermission.Version)
                {
                    throw new PreconditionFailedException(
                        "The access entry changed since it was read. Reload and try again.",
                        "governance.permission.version-conflict",
                        request.ExpectedVersion,
                        existingPermission.Version);
                }

                var previous = existingPermission.Level;
                previousLevel = AccessLevels.OfRank((int)previous);
                // Lowering the only Manager would leave the Board without one.
                if (isBoard && previous >= PermissionLevel.Manager && level < PermissionLevel.Manager)
                {
                    await BoardManagerFloor.EnsureAnotherManagerAsync(
                        _sharing, request.ResourceId, subjectType, request.SubjectId, cancellationToken);
                }
                existingPermission.ChangeLevel(level, actorId, now);
                permission = existingPermission;
                if (isBoard)
                {
                    // A changed level cuts live subscriptions that held the higher one and tells the person's client.
                    if (subjectType == PermissionSubjectType.User) _access.BoardPerson(request.ResourceId, request.SubjectId);
                    else _access.Board(request.ResourceId);
                    await _changes.RecordAsync(
                        new ResourceAccessChangeRecord(kind.Value, request.ResourceId, AccessChangeKinds.Changed, subjectType.ToString(), request.SubjectId, LevelToken(previous), LevelToken(level)),
                        cancellationToken);
                }
            }
        }
        else
        {
            permission = ResourcePermission.Grant(
                accountId,
                workspaceId,
                kind,
                request.ResourceId,
                subjectType,
                request.SubjectId,
                level,
                actorId,
                now);

            _context.ResourcePermissions.Add(permission);
            if (isBoard)
            {
                await _changes.RecordAsync(
                    new ResourceAccessChangeRecord(kind.Value, request.ResourceId, AccessChangeKinds.Added, subjectType.ToString(), request.SubjectId, null, LevelToken(level)),
                    cancellationToken);
                // A new grant reaches the person's open client at once.
                if (subjectType == PermissionSubjectType.User) _access.BoardPerson(request.ResourceId, request.SubjectId);
                else _access.Board(request.ResourceId);
            }
        }

        await _auditService.RecordAsync(
            workspaceId,
            actorId,
            "GrantResourcePermission",
            ResourceRef.Create(kind, request.ResourceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: cancellationToken,
            change: new AuditChange(
                subjectType.ToString(),
                request.SubjectId,
                previousLevel is null ? null : new { level = previousLevel },
                new { level = LevelToken(level) }));

        return Result<ResourcePermissionDto>.Success(ToDto(permission));
    }

    private static ResourcePermissionDto ToDto(ResourcePermission permission) => new(
        permission.Id,
        permission.WorkspaceId,
        permission.ResourceKind.Value,
        permission.ResourceId,
        permission.SubjectType.ToString(),
        permission.SubjectId,
        permission.Level.ToString(),
        permission.CreatedBy,
        permission.IsDeleted,
        permission.DeletedAt,
        permission.Version);

    private const string BoardResourceKind = "work-management.board";

    private static string LevelToken(PermissionLevel level) => AccessLevels.OfRank((int)level);
}
