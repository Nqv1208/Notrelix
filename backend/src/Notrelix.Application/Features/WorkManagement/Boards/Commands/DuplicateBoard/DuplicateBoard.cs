using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Services;
using Notrelix.Application.Features.WorkManagement.Templates.Services;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.DuplicateBoard;

/// <summary>
/// Copies a Board into a new, independent one (UC-WM-A11): its settings, fields, groups, labels and views, and the
/// root items only when asked (at most <see cref="BoardTemplateStructure.MaxItems"/>, else refused). Members and
/// automations are never copied; the duplicator becomes the new Board's manager. An assignee who would have no
/// access to the copy is dropped from the copied items. All or nothing: one transaction, so a failure leaves no
/// half board. Needs Board management on the source and the right to create a Board in the target.
/// </summary>
[IdempotencyOperation("work-management.boards.duplicate-board.v1")]
public record DuplicateBoardCommand(Guid BoardId, string Name, Guid? SpaceId, bool IncludeItems)
    : ICommand<Result<DuplicateBoardResult>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

/// <param name="DroppedAssignees">Assignments left out because the person has no access to the copy.</param>
public sealed record DuplicateBoardResult(Guid Id, int DroppedAssignees);

/// <summary>Which of the assignees of the copied items would reach the new Board.</summary>
internal static class DuplicateAudience
{
    public static async Task<HashSet<Guid>> KeepersAsync(
        IBoardSharingSummary sharing,
        IWorkspaceAccessResolver access,
        Guid workspaceId,
        Guid? spaceId,
        Guid duplicator,
        IEnumerable<Guid> candidates,
        CancellationToken ct)
    {
        var audience = await sharing.GetNewBoardAudienceAsync(spaceId, ct);
        var keep = new HashSet<Guid>();
        foreach (var userId in candidates.Distinct())
        {
            if (userId == duplicator || audience.UserIds.Contains(userId)) { keep.Add(userId); continue; }
            if (audience.EveryMember && !(await access.ResolveAsync(workspaceId, userId, ct)).IsGuest) keep.Add(userId);
        }

        return keep;
    }
}

public class DuplicateBoardCommandHandler : IRequestHandler<DuplicateBoardCommand, Result<DuplicateBoardResult>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IBoardSpacePlacement _placement;
    private readonly IBoardSharingSummary _sharing;
    private readonly IWorkspaceAccessResolver _access;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public DuplicateBoardCommandHandler(
        IWorkManagementDbContext context,
        IBoardSpacePlacement placement,
        IBoardSharingSummary sharing,
        IWorkspaceAccessResolver access,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _context = context;
        _placement = placement;
        _sharing = sharing;
        _access = access;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<DuplicateBoardResult>> Handle(DuplicateBoardCommand request, CancellationToken ct)
    {
        var source = await _context.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.BoardId, ct)
            ?? throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        await _placement.AuthorizeTargetAsync(source.WorkspaceId, request.SpaceId, ct);
        if (request.SpaceId is { } spaceId)
            await _placement.EnsureActiveTargetAsync(source.WorkspaceId, spaceId, ct);

        var structure = await BoardStructureCapture.CaptureAsync(
            _context, source, request.IncludeItems, includeAssignees: request.IncludeItems,
            count => new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Board_DuplicateTooManyItems,
                $"A board is copied with at most {BoardTemplateStructure.MaxItems} items; this one has {count}. Copy it without items or archive some first."),
            ct);

        var userId = _requestContext.UserId;
        var keepers = await DuplicateAudience.KeepersAsync(
            _sharing, _access, source.WorkspaceId, request.SpaceId, userId,
            structure.Items.SelectMany(i => i.Assignees ?? []), ct);

        var now = _clock.UtcNow;
        var board = BoardEntity.Create(
            _requestContext.RequireAccountId(), source.WorkspaceId, userId, request.Name.Trim(), source.Description, now,
            spaceId: request.SpaceId);
        _context.Boards.Add(board);
        var dropped = BoardStructureInstantiator.Instantiate(
            _context, board, structure, _requestContext.RequireAccountId(), source.WorkspaceId, userId, now, keepers.Contains);

        return Result<DuplicateBoardResult>.Success(new DuplicateBoardResult(board.Id, dropped));
    }
}

public class DuplicateBoardCommandValidator : AbstractValidator<DuplicateBoardCommand>
{
    public DuplicateBoardCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.SpaceId).NotEqual(Guid.Empty).When(x => x.SpaceId.HasValue);
    }
}
