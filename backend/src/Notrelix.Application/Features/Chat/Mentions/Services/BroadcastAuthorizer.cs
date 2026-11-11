using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Chat.Mentions.Queries.CheckBroadcastAccess;

namespace Notrelix.Application.Features.Chat.Mentions.Services;

/// <summary>
/// Whether the caller holds the broadcast action (chat.mention.broadcast) in the Workspace (UC-CHT-G03). A send that carries an
/// @channel or @here asks it; a caller without the action sends the token as plain text. It is evaluated with the contract of
/// <see cref="CheckBroadcastAccessQuery"/> through the one facts provider and policy engine, so a custom role grant counts and
/// there is no second evaluator.
/// </summary>
public interface IBroadcastAuthorizer
{
    Task<bool> CanBroadcastAsync(Guid workspaceId, CancellationToken cancellationToken);
}

public sealed class BroadcastAuthorizer(
    IRequestDescriptorRegistry descriptors,
    IAccessFactsProvider factsProvider,
    IAccessPolicyEvaluator policy,
    IExecutionContextReader executionContext) : IBroadcastAuthorizer
{
    public async Task<bool> CanBroadcastAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var current = executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Broadcast authorization requires the request execution context.");
        if (current.WorkspaceId != workspaceId)
            throw new SecurityMisconfigurationException("Broadcast authorization must stay in the request workspace.");

        // Authorization descriptor only: the query is never dispatched here.
        var request = new CheckBroadcastAccessQuery(workspaceId);
        var descriptor = descriptors.GetRequired(typeof(CheckBroadcastAccessQuery));
        var snapshot = current with { Resource = null };

        var facts = await factsProvider.ResolveAsync(descriptor, snapshot, request, cancellationToken);
        return policy.Evaluate(descriptor, snapshot, facts, request).Kind == AccessDecisionKind.Allowed;
    }
}
