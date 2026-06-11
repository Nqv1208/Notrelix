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
        var board = await _context.Boards
            .AsNoTracking()
            .Include(b => b.Groups)
            .Include(b => b.Fields)
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken);

        if (board == null)
            throw new NotFoundException(nameof(Board), request.BoardId);

        var fields = board.Fields
            .OrderBy(f => f.Position)
            .Select(f => new BoardFieldSchemaDto(
                f.Id,
                f.Key,
                f.Name,
                f.Type.ToString(),
                f.Settings.ToJson(),
                f.DefaultValue,
                f.Position,
                f.IsRequired,
                f.IsSystem,
                f.IsHidden
            )).ToList();

        var groups = board.Groups
            .OrderBy(g => g.Position)
            .Select(g => new BoardGroupSchemaDto(
                g.Id,
                g.Title,
                g.Color,
                g.Position,
                g.IsCollapsed
            )).ToList();

        return new BoardSchemaDto(
            board.Id,
            board.Title,
            board.Description,
            fields,
            groups
        );
    }
}
