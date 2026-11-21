using Notrelix.Application.Features.Workspaces.Invitations.Services;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMembers;

/// <summary>
/// Invites up to 50 people at once. Each address is its own invitation made by the same logic as the single
/// invitation (<see cref="InviteMemberCommand"/>), and each gets its own outcome: the batch is not all-or-nothing,
/// so one bad address never blocks the others. Addresses repeated inside the batch collapse to the first one.
/// The caller needs <c>InviteMember</c> through the policy engine, like the single invitation. The Guest gate
/// (workspace setting <c>AllowGuestInvites</c>) cannot be one verdict for the whole request, so the batch
/// itself is not marked as introducing a Guest; the handler asks the SAME engine, with the same facts, about a
/// real single Guest invitation and refuses the Guest entries one by one with <c>workspace.guest_invites_off</c>
/// while the Member entries still go through. No seat is checked, because the single invitation checks none.
/// A retry under the same Idempotency-Key returns the first result and sends nothing again.
/// </summary>
[IdempotencyOperation("workspaces.invitation.invite-members.v1")]
public record InviteMembersCommand(
    Guid WorkspaceId,
    IReadOnlyList<MemberInvitationEntry> Entries
) : ICommand<Result<MemberInvitationBatchResultDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail, IIdempotentRequest, IRequireAdditionalPermission
{
    public const int MaxEntries = 50;

    // Initial entries are authorized as a single invitation does: ManageTeams for Team entries, ManageSpaces for
    // Space entries, both when the batch names both kinds. The union over all addresses decides, so one
    // unauthorized entry refuses the whole request (nothing is sent) instead of leaking per-address outcomes.
    private bool NamesKind(WorkspaceInvitationEntryKind kind) =>
        Entries?.Any(e => e.InitialEntries?.Any(i => string.Equals(i.Kind, kind.ToString(), StringComparison.OrdinalIgnoreCase)) == true) == true;

    PermissionAction IRequirePermission.Action =>
        NamesKind(WorkspaceInvitationEntryKind.Team) ? PermissionAction.ManageTeams
        : NamesKind(WorkspaceInvitationEntryKind.Space) ? PermissionAction.ManageSpaces
        : PermissionAction.InviteMember;

    IReadOnlyList<string> IRequireAdditionalPermission.AdditionalActionNames =>
        NamesKind(WorkspaceInvitationEntryKind.Team) && NamesKind(WorkspaceInvitationEntryKind.Space)
            ? [nameof(PermissionAction.ManageSpaces)]
            : [];

    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class InviteMembersCommandValidator : AbstractValidator<InviteMembersCommand>
{
    public InviteMembersCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Entries).NotNull().Must(e => e is { Count: >= 1 and <= InviteMembersCommand.MaxEntries })
            .WithMessage($"Invite between 1 and {InviteMembersCommand.MaxEntries} people at a time.");
        // A bad address or role is that entry's own outcome, not a failure of the batch.
    }
}

public class InviteMembersCommandHandler : IRequestHandler<InviteMembersCommand, Result<MemberInvitationBatchResultDto>>
{
    private static readonly InlineValidator<string> EmailRule = BuildEmailRule();

    private readonly IRequestHandler<InviteMemberCommand, Result<Guid>> _single;
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IAccessFactsProvider _factsProvider;
    private readonly IAccessPolicyEvaluator _policy;
    private readonly IExecutionContextReader _executionContext;

    public InviteMembersCommandHandler(
        IRequestHandler<InviteMemberCommand, Result<Guid>> single,
        IRequestDescriptorRegistry descriptors,
        IAccessFactsProvider factsProvider,
        IAccessPolicyEvaluator policy,
        IExecutionContextReader executionContext)
    {
        _single = single;
        _descriptors = descriptors;
        _factsProvider = factsProvider;
        _policy = policy;
        _executionContext = executionContext;
    }

    public async Task<Result<MemberInvitationBatchResultDto>> Handle(InviteMembersCommand request, CancellationToken ct)
    {
        var outcomes = new List<MemberInvitationOutcomeDto>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        AccessDecision? guestGate = null;

        foreach (var entry in request.Entries)
        {
            var email = (entry.Email ?? string.Empty).Trim().ToLowerInvariant();
            var roleText = (entry.Role ?? string.Empty).Trim();

            if (email.Length > 0 && !seen.Add(email))
                continue; // the same address twice collapses to the first entry

            if (!IsValidEmail(email) || !TryParseRole(roleText, out var role))
            {
                outcomes.Add(new MemberInvitationOutcomeDto(email, roleText, MemberInvitationOutcomes.Invalid, null, null));
                continue;
            }

            if (role.IntroducesGuest())
            {
                guestGate ??= await EvaluateSingleGuestInvitationAsync(request.WorkspaceId, role, ct);
                if (guestGate.Kind != AccessDecisionKind.Allowed)
                {
                    outcomes.Add(new MemberInvitationOutcomeDto(email, role.ToString(), MemberInvitationOutcomes.Refused, guestGate.Code, null));
                    continue;
                }
            }

            outcomes.Add(await InviteOneAsync(request.WorkspaceId, email, role, entry.InitialEntries, ct));
        }

        return Result<MemberInvitationBatchResultDto>.Success(new MemberInvitationBatchResultDto(outcomes));
    }

    private async Task<MemberInvitationOutcomeDto> InviteOneAsync(Guid workspaceId, string email, WorkspaceRole role, IReadOnlyList<InvitationInitialEntryInput>? initialEntries, CancellationToken ct)
    {
        try
        {
            var result = await _single.Handle(new InviteMemberCommand(workspaceId, email, role, initialEntries), ct);
            return result.Succeeded
                ? new MemberInvitationOutcomeDto(email, role.ToString(), MemberInvitationOutcomes.Sent, null, result.Data)
                : new MemberInvitationOutcomeDto(email, role.ToString(), MemberInvitationOutcomes.Refused, null, null);
        }
        catch (ConflictException conflict) when (conflict.ErrorCode == InviteMemberCommandHandler.AlreadyMemberErrorCode)
        {
            return new MemberInvitationOutcomeDto(email, role.ToString(), MemberInvitationOutcomes.AlreadyMember, conflict.ErrorCode, null);
        }
        catch (ConflictException conflict) when (conflict.ErrorCode == InviteMemberCommandHandler.PendingInvitationExistsErrorCode)
        {
            return new MemberInvitationOutcomeDto(email, role.ToString(), MemberInvitationOutcomes.AlreadyPending, conflict.ErrorCode, null);
        }
        catch (DomainBusinessRuleException rule)
        {
            return new MemberInvitationOutcomeDto(email, role.ToString(), MemberInvitationOutcomes.Refused, rule.RuleCode, null);
        }
    }

    // The very contract a single Guest invitation is decided with, through the one facts provider and policy engine:
    // nothing is dispatched, so the engine stays the only place the guest-invites setting is read.
    private async Task<AccessDecision> EvaluateSingleGuestInvitationAsync(Guid workspaceId, WorkspaceRole guestRole, CancellationToken ct)
    {
        var current = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Guest invitation authorization requires the request execution context.");
        var probe = new InviteMemberCommand(workspaceId, "guest@probe.invalid", guestRole);
        var descriptor = _descriptors.GetRequired(typeof(InviteMemberCommand));
        var facts = await _factsProvider.ResolveAsync(descriptor, current, probe, ct);
        return _policy.Evaluate(descriptor, current, facts, probe);
    }

    // Names only: Enum.TryParse also takes numbers ("3"), which would let a client name a role by its ordinal.
    // The Domain still refuses Owner and Admin whatever the spelling.
    private static bool TryParseRole(string text, out WorkspaceRole role)
    {
        role = default;
        return Enum.GetNames<WorkspaceRole>().Contains(text, StringComparer.OrdinalIgnoreCase)
            && Enum.TryParse(text, ignoreCase: true, out role);
    }

    private static bool IsValidEmail(string email) =>
        email.Length is > 0 and <= 256 && EmailRule.Validate(email).IsValid;

    private static InlineValidator<string> BuildEmailRule()
    {
        var rule = new InlineValidator<string>();
        rule.RuleFor(x => x).EmailAddress();
        return rule;
    }
}
