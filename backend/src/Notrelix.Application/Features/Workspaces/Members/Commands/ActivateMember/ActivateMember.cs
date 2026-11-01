using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.Billing.Public.Seats;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.ActivateMember;

/// <summary>
/// Brings a suspended member back. Guarded by the target member's expected version, so a decision made on a stale view of the
/// member (already changed by someone else) is refused instead of overwriting that change.
/// </summary>
public record ActivateMemberCommand(
    Guid WorkspaceId,
    Guid UserId,
    long ExpectedVersion
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.RemoveMember;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    // The concurrency authority is the target WorkspaceMember; authorization stays on the Workspace.
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace-member"), UserId, WorkspaceId);
}

public class ActivateMemberCommandHandler : IRequestHandler<ActivateMemberCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceGrantProjectionService _grantProjection;
    private readonly IBillingSeatGate _seatGate;
    private readonly IAccessChangeSignal _access;

    public ActivateMemberCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IWorkspaceGrantProjectionService grantProjection, IBillingSeatGate seatGate, IAccessChangeSignal access)
    {
        _access = access;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
        _seatGate = seatGate;
    }

    public async Task<Result> Handle(ActivateMemberCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && w.DeletedAt == null, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == request.UserId, ct);

        if (member is null)
            throw new NotFoundException("WorkspaceMember", request.UserId);

        var actorRole = await WorkspaceActorRoleLookup.GetActiveActorRoleAsync(
            _context, request.WorkspaceId, _requestContext.UserId, ct);
        WorkspaceMemberRules.EnsureActorCanActOnMember(actorRole, member.Role);

        // A suspended member takes a seat again when they return; an active one is a no-op and a
        // removed one is refused by the Domain, so neither asks Billing. Billing owns the decision.
        if (member.Status == WorkspaceMemberStatus.Suspended
            && !await _seatGate.CanAdmitAsync(
                workspace.AccountId, workspace.Id, request.UserId, WorkspaceMemberRules.TakesGuestSeat(member.Role), ct))
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                Notrelix.Domain.Workspaces.WorkspaceRuleCodes.Workspaces_Member_SeatUnavailable,
                "No seat of this account's plan is available for this role.");
        }

        member.Activate(_requestContext.UserId, _dateTimeProvider.UtcNow);

        await _grantProjection.SyncWorkspaceMemberGrantAsync(
            workspace.AccountId,
            workspace.Id,
            request.UserId,
            member.Role,
            _dateTimeProvider.UtcNow,
            ct);

        // A reactivated member returns to their built-in role and is configured again.
        await _grantProjection.ClearCustomRoleAssignmentsAsync(
            workspace.AccountId, workspace.Id, request.UserId, ct);

        // The person's own open client learns it is active again at once.
        _access.WorkspacePerson(request.WorkspaceId, request.UserId);
        return Result.Success();
    }
}
