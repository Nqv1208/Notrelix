using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.WorkManagement.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Queries;

public record GetBoardSchemaQuery(Guid WorkspaceId, Guid BoardId) : IRequest<BoardSchemaDto>, IAuthorizeableRequest
{
    public ResourceType ResourceType => ResourceType.Board;
    public Guid ResourceId => BoardId;
    public PermissionAction Action => PermissionAction.ViewBoard;
}

public class GetBoardSchemaQueryHandler : IRequestHandler<GetBoardSchemaQuery, BoardSchemaDto>
{
    private readonly IApplicationDbContext _context;

    public GetBoardSchemaQueryHandler(IApplicationDbContext context)
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
                f.DefaultValue,
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
