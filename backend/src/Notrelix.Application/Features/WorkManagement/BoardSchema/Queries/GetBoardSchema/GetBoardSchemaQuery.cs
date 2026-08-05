using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardSchema.Queries.GetBoardSchema;

public sealed record GetBoardSchemaCacheIdentity(Guid BoardId);

public record GetBoardSchemaQuery(Guid BoardId) : IQuery<BoardSchemaDto>, IRequirePermission, IResourceScopedRequest, IAuthorizedCacheableRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
    public AuthorizedCacheScope CacheScope => AuthorizedCacheScope.Workspace;
    public object CacheIdentity => new GetBoardSchemaCacheIdentity(BoardId);
    public TimeSpan? CacheTtl => TimeSpan.FromMinutes(5);
}

public class GetBoardSchemaQueryHandler : IRequestHandler<GetBoardSchemaQuery, BoardSchemaDto>
{
    private readonly IWorkManagementDbContext _context;

    public GetBoardSchemaQueryHandler(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<BoardSchemaDto> Handle(GetBoardSchemaQuery request, CancellationToken cancellationToken)
    {
        var boardId = request.BoardId;

        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);

        if (board == null)
            throw new NotFoundException(nameof(Board), boardId);

        var fields = await _context.BoardFields
            .AsNoTracking()
            .Where(f => f.BoardId == boardId)
            .OrderBy(f => f.Position)
            .Select(f => new BoardFieldSchemaDto(
                f.Id,
                f.Name,
                f.Type.ToString(),
                f.Settings.Data.Value,
                f.DefaultValue == null ? null : f.DefaultValue.Data.Value,
                f.Position.Value,
                f.IsSystem
            )).ToListAsync(cancellationToken);

        var groups = await _context.BoardGroups
            .AsNoTracking()
            .Where(g => g.BoardId == boardId)
            .OrderBy(g => g.Position)
            .Select(g => new BoardGroupSchemaDto(
                g.Id,
                g.Title,
                g.Color.ToString(),
                g.Position.Value,
                g.IsCollapsed
            )).ToListAsync(cancellationToken);

        return new BoardSchemaDto(
            board.Id,
            board.Title,
            board.Description,
            fields,
            groups
        );
    }
}
