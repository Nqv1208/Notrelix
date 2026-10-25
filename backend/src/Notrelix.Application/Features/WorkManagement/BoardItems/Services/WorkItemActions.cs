using Notrelix.Application.Features.WorkManagement.Ordering;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.ItemMovement;

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
    private readonly IWorkItemActionAuthorizer _authorizer;
    private readonly IIdempotencyStore _idempotencyStore;

    public WorkItemActions(
        MoveBoardItemUseCase useCase,
        IWorkManagementDbContext context,
        IWorkItemActionAuthorizer authorizer,
        IIdempotencyStore idempotencyStore)
    {
        _useCase = useCase;
        _context = context;
        _authorizer = authorizer;
        _idempotencyStore = idempotencyStore;
    }

    public async Task<WorkItemMoveResult> MoveItemAsync(
        WorkItemMoveRequest request,
        CancellationToken cancellationToken)
    {
        var execution = request.Execution;

        // Canonical authorization comes first: the public target action is
        // governed by the exact same MoveItem decision as the HTTP command.
        // A deny must never reach the dedup store or the mutation.
        await _authorizer.AuthorizeMoveItemAsync(
            execution, request.ItemId, request.NewGroupId, cancellationToken);

        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new NotFoundException("BoardItem", request.ItemId);

        // Target-owned scope: the item must live inside the declared workspace.
        if (item.WorkspaceId != execution.WorkspaceId)
        {
            throw new ForbiddenException("The item does not belong to the declared workspace.");
        }

        // Producer-owned OperationId dedup. Begin participates in the caller's
        // transaction so the dedup record and the Work mutation commit or roll
        // back together. The executor belongs to the execution semantics: a
        // retry by a different actor is a deterministic conflict, never a
        // replay of someone else's move.
        var identity = new IdempotencyIdentity(
            Operation: MoveOperation,
            Scope: $"account:{execution.AccountId:N}:workspace:{execution.WorkspaceId:N}",
            KeyHash: Sha256($"move-item:{execution.OperationId:N}"),
            RequestHash: Sha256($"{request.ItemId:N}:{request.NewGroupId:N}:{execution.ExecutorUserId:N}"));

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
            WorkPlacementIntent.Append,
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

    public async Task EnsureMoveItemAllowedAsync(
        WorkItemMoveRequest request,
        CancellationToken cancellationToken)
    {
        var execution = request.Execution;

        await _authorizer.AuthorizeMoveItemAsync(
            execution, request.ItemId, request.NewGroupId, cancellationToken);

        var item = await _context.BoardItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new NotFoundException("BoardItem", request.ItemId);

        if (item.WorkspaceId != execution.WorkspaceId)
        {
            throw new ForbiddenException("The item does not belong to the declared workspace.");
        }

        var groupOnBoard = await _context.BoardGroups
            .AsNoTracking()
            .AnyAsync(g => g.Id == request.NewGroupId && g.BoardId == item.BoardId, cancellationToken);
        if (!groupOnBoard)
        {
            throw new NotFoundException("BoardGroup", request.NewGroupId);
        }
    }

    public async Task EnsureBoardGroupAsync(
        WorkItemActionIdentity execution,
        Guid boardId,
        Guid? groupId,
        CancellationToken cancellationToken)
    {
        await _authorizer.AuthorizeBoardEditAsync(execution, boardId, cancellationToken);

        var boardInWorkspace = await _context.Boards
            .AsNoTracking()
            .AnyAsync(b => b.Id == boardId && b.WorkspaceId == execution.WorkspaceId, cancellationToken);
        if (!boardInWorkspace)
        {
            throw new NotFoundException("Board", boardId);
        }

        if (groupId is { } group)
        {
            var groupOnBoard = await _context.BoardGroups
                .AsNoTracking()
                .AnyAsync(g => g.Id == group && g.BoardId == boardId, cancellationToken);
            if (!groupOnBoard)
            {
                throw new NotFoundException("BoardGroup", group);
            }
        }
    }

    public async Task EnsureItemOnBoardAsync(
        Guid workspaceId,
        Guid itemId,
        Guid boardId,
        CancellationToken cancellationToken)
    {
        var onBoard = await _context.BoardItems
            .AsNoTracking()
            .AnyAsync(i => i.Id == itemId && i.BoardId == boardId && i.WorkspaceId == workspaceId, cancellationToken);
        if (!onBoard)
        {
            throw new NotFoundException("BoardItem", itemId);
        }
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
