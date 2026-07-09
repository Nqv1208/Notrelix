using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;

public record DuplicateBoardItemCommand(Guid BoardItemId) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemId);
}

public class DuplicateBoardItemCommandHandler : IRequestHandler<DuplicateBoardItemCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;

    public DuplicateBoardItemCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(DuplicateBoardItemCommand request, CancellationToken ct)
    {
        var source = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId && !c.IsDeleted, ct);
        if (source is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var lastItem = await _context.BoardItems
            .Where(c => c.GroupId == source.GroupId && !c.IsDeleted)
            .OrderByDescending(c => c.Position)
            .FirstOrDefaultAsync(ct);

        var nextPosition = lastItem != null
            ? FractionalIndex.Create(lastItem.Position.Value + "1")
            : FractionalIndex.Initial();

        var now = _timeProvider.UtcNow;

        var duplicate = DuplicateBoardGroupCommandHandler.CloneCard(
            source,
            _requestContext.RequireAccountId(),
            source.GroupId,
            source.BoardId,
            source.WorkspaceId,
            _requestContext.UserId,
            $"{source.Name} copy",
            nextPosition,
            now);

        _context.BoardItems.Add(duplicate);
        return Result<Guid>.Success(duplicate.Id);
    }
}
