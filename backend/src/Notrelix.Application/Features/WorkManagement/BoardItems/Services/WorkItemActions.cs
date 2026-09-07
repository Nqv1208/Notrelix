using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.ItemMovement;
using Notrelix.Application.Features.Workspaces.Public.Membership;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>
/// Producer-owned implementation of the WorkManagement public item action.
/// Enforces target-owned scope and executor authority, deduplicates on the
/// caller-supplied OperationId through the producer idempotency store (same
/// payload replays one logical move, conflicting payloads fail
/// deterministically), and delegates the mutation to the single producer-local
/// move use case shared with the HTTP command handler.
/// </summary>
public sealed class WorkItemActions : IWorkItemActions
{
    private const string MoveOperation = "work-management.public.move-item.v1";

    private readonly MoveBoardItemUseCase _useCase;
    private readonly IWorkManagementDbContext _context;
    private readonly IWorkspaceMembershipFacts _membershipFacts;
    private readonly IIdempotencyStore _idempotencyStore;

    public WorkItemActions(
        MoveBoardItemUseCase useCase,
        IWorkManagementDbContext context,
        IWorkspaceMembershipFacts membershipFacts,
        IIdempotencyStore idempotencyStore)
    {
        _useCase = useCase;
        _context = context;
        _membershipFacts = membershipFacts;
        _idempotencyStore = idempotencyStore;
    }

    public async Task<WorkItemMoveResult> MoveItemAsync(
        WorkItemMoveRequest request,
        CancellationToken cancellationToken)
    {
        var execution = request.Execution;

        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new NotFoundException("BoardItem", request.ItemId);

        // Target-owned scope: the item must live inside the declared workspace.
        if (item.WorkspaceId != execution.WorkspaceId)
        {
            throw new ForbiddenException("The item does not belong to the declared workspace.");
        }

        // Target-owned executor authority: the executor must be an active
        // workspace member, checked through the Workspaces-owned membership
        // facts contract — never the caller's claim alone.
        var membership = await _membershipFacts.ResolveAsync(
            execution.AccountId, execution.WorkspaceId, execution.ExecutorUserId, cancellationToken);
        if (membership is not { IsActiveMember: true })
        {
            throw new ForbiddenException("The executor is not an active member of the workspace.");
        }

        // Producer-owned OperationId dedup. Begin participates in the caller's
        // transaction so the dedup record and the Work mutation commit or roll
        // back together.
        var identity = new IdempotencyIdentity(
            Operation: MoveOperation,
            Scope: $"account:{execution.AccountId:N}:workspace:{execution.WorkspaceId:N}",
            KeyHash: Sha256($"move-item:{execution.OperationId:N}"),
            RequestHash: Sha256($"{request.ItemId:N}:{request.NewGroupId:N}"));

        var begin = await _idempotencyStore.BeginAsync(identity, cancellationToken);
        switch (begin.Status)
        {
            case IdempotencyBeginStatus.Completed:
                return JsonSerializer.Deserialize<WorkItemMoveResult>(begin.SerializedResult!)!;
            case IdempotencyBeginStatus.PayloadMismatch:
                throw new WorkItemOperationConflictException(execution.OperationId);
        }

        var outcome = await _useCase.MoveAsync(
            request.ItemId,
            request.NewGroupId,
            execution.ExecutorUserId,
            cancellationToken);

        var result = new WorkItemMoveResult(
            outcome.Item.Id,
            outcome.Item.GroupId,
            outcome.Item.Position);

        await _idempotencyStore.CompleteAsync(
            identity,
            JsonSerializer.Serialize(result),
            nameof(WorkItemMoveResult),
            cancellationToken);

        return result;
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
