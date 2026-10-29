using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Application.Features.Billing.Public.Seats;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;

public record AddMemberCommand(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole Role
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail
{
    public PermissionAction Action => PermissionAction.InviteMember;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class AddMemberCommandHandler : IRequestHandler<AddMemberCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceGrantProjectionService _grantProjection;
    private readonly IBillingSeatGate _seatGate;
    private readonly IAccountMembershipFacts _accountFacts;

    public AddMemberCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IWorkspaceGrantProjectionService grantProjection, IBillingSeatGate seatGate, IAccountMembershipFacts accountFacts)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
        _seatGate = seatGate;
        _accountFacts = accountFacts;
    }

    public async Task<Result> Handle(AddMemberCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && w.DeletedAt == null, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var actorRole = await WorkspaceActorRoleLookup.GetActiveActorRoleAsync(
            _context, request.WorkspaceId, _requestContext.UserId, ct);
        WorkspaceMemberRules.EnsureActorCanAssignRole(actorRole, request.Role);

        // Direct add is for people already in the Account; the refusal is the same for an unknown
        // user, another Account's user and an inactive member, so it reveals nothing about the user.
        if (!await _accountFacts.IsActiveMemberAsync(workspace.AccountId, request.UserId, ct))
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                Notrelix.Domain.Workspaces.WorkspaceRuleCodes.Workspaces_Member_NotInAccount,
                "This person is not a member of this account. Invite them instead.");

        // Billing owns the seat decision; a Guest takes a guest seat, every other role a member seat.
        if (!await _seatGate.CanAdmitAsync(
                workspace.AccountId, request.WorkspaceId, request.UserId, WorkspaceMemberRules.TakesGuestSeat(request.Role), ct))
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                Notrelix.Domain.Workspaces.WorkspaceRuleCodes.Workspaces_Member_SeatUnavailable,
                "No seat of this account's plan is available for this role.");

        var existingMember = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == request.UserId, ct);

        if (existingMember is not null)
        {
            if (existingMember.Status == WorkspaceMemberStatus.Active)
                throw new BusinessRuleException("User is already a member of this workspace.");

            // Re-adding a returning member also acts on that member's existing row.
            WorkspaceMemberRules.EnsureActorCanActOnMember(actorRole, existingMember.Role);

            existingMember.Activate(_requestContext.UserId, _dateTimeProvider.UtcNow);
            existingMember.ChangeRole(request.Role, _requestContext.UserId, 1, _dateTimeProvider.UtcNow);
        }
        else
        {
            var member = WorkspaceMember.Create(
                workspace.AccountId,
                request.WorkspaceId,
                request.UserId,
                request.Role,
                _requestContext.UserId,
                _dateTimeProvider.UtcNow);

            _context.WorkspaceMembers.Add(member);
        }

        await _grantProjection.SyncWorkspaceMemberGrantAsync(
            workspace.AccountId,
            request.WorkspaceId,
            request.UserId,
            request.Role,
            _dateTimeProvider.UtcNow,
            ct);

        return Result.Success();
    }
}
