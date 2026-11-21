using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.CancelInvitation;

namespace Notrelix.Application.Features.Workspaces.Invitations.Services;

/// <summary>
/// Whether the caller may resend or cancel ANY invitation of the Workspace (UC-WM-A20): the action the workspace resend and
/// cancel endpoints require (InviteMember). The Share dialog asks it to decide which rows of a Board's pending list the reader
/// may act on besides their own. It is evaluated with the contract of <see cref="CancelInvitationCommand"/> through the one
/// facts provider and policy engine, so a custom role grant counts and there is no second evaluator.
/// </summary>
public interface IInvitationManageAuthorizer
{
    Task<bool> CanManageAnyInvitationAsync(Guid workspaceId, CancellationToken cancellationToken);
}

public sealed class InvitationManageAuthorizer(
    IRequestDescriptorRegistry descriptors,
    IAccessFactsProvider factsProvider,
    IAccessPolicyEvaluator policy,
    IExecutionContextReader executionContext) : IInvitationManageAuthorizer
{
    public async Task<bool> CanManageAnyInvitationAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var current = executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Invitation authorization requires the request execution context.");
        if (current.WorkspaceId != workspaceId)
            throw new SecurityMisconfigurationException("Invitation authorization must stay in the request workspace.");

        // Authorization descriptor only: the command is never dispatched here.
        var request = new CancelInvitationCommand(workspaceId, Guid.Empty);
        var descriptor = descriptors.GetRequired(typeof(CancelInvitationCommand));
        var snapshot = current with { Resource = null };

        var facts = await factsProvider.ResolveAsync(descriptor, snapshot, request, cancellationToken);
        return policy.Evaluate(descriptor, snapshot, facts, request).Kind == AccessDecisionKind.Allowed;
    }
}
