using Notrelix.Application.Features.WorkManagement.Ordering;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;
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
    private readonly IWorkspaceAccessResolver _workspaceAccess;
    private readonly IDateTimeProvider _clock;
    private readonly IBoardRealtimeNotifier? _realtime;

    public WorkItemActions(
        MoveBoardItemUseCase useCase,
        IWorkManagementDbContext context,
        IWorkItemActionAuthorizer authorizer,
        IIdempotencyStore idempotencyStore,
        IWorkspaceAccessResolver workspaceAccess,
        IDateTimeProvider clock,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _workspaceAccess = workspaceAccess;
        _clock = clock;
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

        await NotifyBoardAsync(item.BoardId, "AutomationMoveItem", execution, cancellationToken);
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

    public async Task ChangeStatusAsync(
        WorkItemActionIdentity execution, Guid itemId, Guid optionId, CancellationToken cancellationToken)
    {
        await _authorizer.AuthorizeItemCommandAsync(
            execution, itemId, new UpdateBoardItemFieldValueCommand(itemId, Guid.Empty, null, 0), cancellationToken);

        var item = await LoadItemAsync(execution, itemId, cancellationToken);
        var statusField = await BoardItemFieldWrites.LoadStatusFieldAsync(_context, item.BoardId, cancellationToken)
            ?? throw new NotFoundException("BoardField", item.BoardId);
        if (statusField.Options.All(option => option.Id != optionId))
        {
            throw new NotFoundException("BoardFieldOption", optionId);
        }

        var changed = await DedupedAsync("work-management.public.change-status.v1", execution, $"{itemId:N}:{optionId:N}", () =>
        {
            var value = FieldValue.Create(Notrelix.Domain.SharedKernel.JsonValue.Create(JsonSerializer.Serialize(optionId.ToString())));
            BoardItemFieldWrites.Write(item, statusField, value, statusField, execution.ExecutorUserId, _clock.UtcNow);
            return Task.FromResult(true);
        }, cancellationToken);
        if (changed) await NotifyBoardAsync(item.BoardId, "AutomationChangeStatus", execution, cancellationToken);
    }

    public async Task SetFieldValueAsync(
        WorkItemActionIdentity execution, Guid itemId, Guid fieldId, string valueJson, CancellationToken cancellationToken)
    {
        await _authorizer.AuthorizeItemCommandAsync(
            execution, itemId, new UpdateBoardItemFieldValueCommand(itemId, fieldId, null, 0), cancellationToken);

        var item = await LoadItemAsync(execution, itemId, cancellationToken);
        var field = await _context.BoardFields
            .Include(f => f.Options)
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.BoardId == item.BoardId && f.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("BoardField", fieldId);
        var statusField = await BoardItemFieldWrites.LoadStatusFieldAsync(_context, item.BoardId, cancellationToken);

        var changed = await DedupedAsync("work-management.public.set-field-value.v1", execution, $"{itemId:N}:{fieldId:N}:{valueJson}", () =>
        {
            var value = FieldValue.Create(Notrelix.Domain.SharedKernel.JsonValue.Create(valueJson));
            BoardItemFieldWrites.Write(item, field, value, statusField, execution.ExecutorUserId, _clock.UtcNow);
            return Task.FromResult(true);
        }, cancellationToken);
        if (changed) await NotifyBoardAsync(item.BoardId, "AutomationSetFieldValue", execution, cancellationToken);
    }

    public async Task AssignMemberAsync(
        WorkItemActionIdentity execution, Guid itemId, Guid userId, CancellationToken cancellationToken)
    {
        await _authorizer.AuthorizeItemCommandAsync(
            execution, itemId, new AssignBoardItemMemberCommand(itemId, userId, 0), cancellationToken);

        var item = await LoadItemAsync(execution, itemId, cancellationToken);
        await EnsureWorkspaceMemberAsync(execution.WorkspaceId, userId, cancellationToken);

        var changed = await DedupedAsync("work-management.public.assign-member.v1", execution, $"{itemId:N}:{userId:N}", async () =>
        {
            var already = await _context.BoardItemMembers
                .AnyAsync(m => m.ItemId == item.Id && m.UserId == userId, cancellationToken);
            if (already) return false;

            _context.BoardItemMembers.Add(BoardItemMember.Create(
                execution.AccountId, item.WorkspaceId, item.BoardId, item.Id, userId, execution.ExecutorUserId, _clock.UtcNow));
            return true;
        }, cancellationToken);
        if (changed) await NotifyBoardAsync(item.BoardId, "AutomationAssignMember", execution, cancellationToken);
    }

    public async Task<WorkItemFacts> GetItemFactsAsync(
        Guid workspaceId, Guid itemId, Guid boardId, CancellationToken cancellationToken)
    {
        var item = await _context.BoardItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == itemId && i.BoardId == boardId && i.WorkspaceId == workspaceId, cancellationToken)
            ?? throw new NotFoundException("BoardItem", itemId);
        var statusField = await BoardItemFieldWrites.LoadStatusFieldAsync(_context, boardId, cancellationToken);
        var values = await _context.BoardItemValues.AsNoTracking()
            .Where(v => v.ItemId == itemId)
            .ToListAsync(cancellationToken);
        var statusOption = statusField is null
            ? null
            : WorkItemSemanticFields.ReadOptionId(values.FirstOrDefault(v => v.FieldId == statusField.Id)?.Value);
        return new WorkItemFacts(
            item.GroupId,
            statusOption,
            item.Name,
            values.Where(v => !v.Value.IsEmpty).ToDictionary(v => v.FieldId, v => v.Value.Data.Value));
    }

    public async Task EnsureStatusOptionAsync(
        Guid workspaceId, Guid boardId, Guid optionId, CancellationToken cancellationToken)
    {
        await EnsureBoardInWorkspaceAsync(workspaceId, boardId, cancellationToken);
        var statusField = await BoardItemFieldWrites.LoadStatusFieldAsync(_context, boardId, cancellationToken)
            ?? throw new NotFoundException("BoardField", boardId);
        if (statusField.Options.All(option => option.Id != optionId))
        {
            throw new NotFoundException("BoardFieldOption", optionId);
        }
    }

    public async Task EnsureFieldValueAsync(
        Guid workspaceId, Guid boardId, Guid fieldId, string valueJson, CancellationToken cancellationToken)
    {
        await EnsureBoardInWorkspaceAsync(workspaceId, boardId, cancellationToken);
        var field = await _context.BoardFields.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.BoardId == boardId && f.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("BoardField", fieldId);
        FieldValueValidator.Validate(
            FieldValue.Create(Notrelix.Domain.SharedKernel.JsonValue.Create(valueJson)), field.Type, field.Settings);
    }

    public async Task EnsureWorkspaceMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var access = await _workspaceAccess.ResolveAsync(workspaceId, userId, cancellationToken);
        if (!access.CanAccess)
        {
            throw new ForbiddenException("The person is not a member of this workspace.");
        }
    }

    private async Task EnsureBoardInWorkspaceAsync(Guid workspaceId, Guid boardId, CancellationToken cancellationToken)
    {
        var inWorkspace = await _context.Boards.AsNoTracking()
            .AnyAsync(b => b.Id == boardId && b.WorkspaceId == workspaceId, cancellationToken);
        if (!inWorkspace)
        {
            throw new NotFoundException("Board", boardId);
        }
    }

    private async Task<BoardItem> LoadItemAsync(
        WorkItemActionIdentity execution, Guid itemId, CancellationToken cancellationToken)
    {
        var item = await _context.BoardItems
            .Include(i => i.FieldValues)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken)
            ?? throw new NotFoundException("BoardItem", itemId);
        if (item.WorkspaceId != execution.WorkspaceId)
        {
            throw new ForbiddenException("The item does not belong to the declared workspace.");
        }

        return item;
    }

    /// <summary>One logical mutation per operation id: a replay changes nothing, a different payload conflicts.</summary>
    private async Task<bool> DedupedAsync(
        string operation, WorkItemActionIdentity execution, string payload, Func<Task<bool>> mutate, CancellationToken cancellationToken)
    {
        var identity = new IdempotencyIdentity(
            Operation: operation,
            Scope: $"account:{execution.AccountId:N}:workspace:{execution.WorkspaceId:N}",
            KeyHash: Sha256($"{operation}:{execution.OperationId:N}"),
            RequestHash: Sha256($"{payload}:{execution.ExecutorUserId:N}"));
        var begin = await _idempotencyStore.BeginAsync(identity, cancellationToken);
        switch (begin.Status)
        {
            case IdempotencyBeginStatus.Completed:
                return false;
            case IdempotencyBeginStatus.PayloadMismatch:
                throw new WorkItemOperationConflictException(execution.OperationId);
        }

        var changed = await mutate();
        await _idempotencyStore.CompleteAsync(identity, "true", "bool", cancellationToken);
        return changed;
    }

    /// <summary>The viewers of the board converge on what the rule changed; a replay or a no-op sends no frame.</summary>
    private Task NotifyBoardAsync(Guid boardId, string changeKind, WorkItemActionIdentity execution, CancellationToken cancellationToken) =>
        _realtime is null
            ? Task.CompletedTask
            : _realtime.NotifyBoardAsync(
                boardId, changeKind, execution.AccountId, execution.WorkspaceId, execution.ExecutorUserId,
                execution.CorrelationId, execution.CausationId, cancellationToken);

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
