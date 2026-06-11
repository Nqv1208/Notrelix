using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record DuplicateBoardItemCommand(Guid BoardItemId) : IRequest<Result<Guid>>;

public class DuplicateBoardItemCommandHandler : IRequestHandler<DuplicateBoardItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DuplicateBoardItemCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(DuplicateBoardItemCommand request, CancellationToken ct)
    {
        var source = await _context.BoardItems
            .Include(c => c.Group)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId && !c.IsDeleted && !c.IsArchived, ct);
        if (source is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var nextPosition = await _context.BoardItems
            .Where(c => c.GroupId == source.GroupId && !c.IsDeleted && !c.IsArchived)
            .MaxAsync(c => (double?)c.Position, ct) + 1 ?? source.Position + 1;

        var duplicate = DuplicateBoardGroupCommandHandler.CloneCard(
            source,
            source.GroupId,
            source.Group.BoardId,
            source.WorkspaceId,
            _currentUser.UserId,
            $"{source.Title} copy",
            nextPosition);

        _context.BoardItems.Add(duplicate);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(duplicate.Id);
    }
}
