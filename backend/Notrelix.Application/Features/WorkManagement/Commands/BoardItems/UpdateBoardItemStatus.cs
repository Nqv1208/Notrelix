using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record UpdateBoardItemStatusCommand(Guid BoardItemId, string Status) : IRequest<Result>;

public class UpdateBoardItemStatusCommandHandler : IRequestHandler<UpdateBoardItemStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _timeProvider;

    public UpdateBoardItemStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions,
        IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(UpdateBoardItemStatusCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        await _permissions.EnsureCanEditBoardAsync(card.BoardId, _currentUser.UserId, ct);

        var statusFields = await _context.BoardFields
            .Where(f => f.BoardId == card.BoardId && f.Type == FieldType.Status && !f.IsDeleted)
            .ToListAsync(ct);

        var statusField = statusFields.FirstOrDefault();
        if (statusField == null)
            return Result.Failure("No status field found on this board.");

        var now = new DateTimeOffset(_timeProvider.UtcNow, TimeSpan.Zero);
        var fieldValue = FieldValue.Create(JsonValue.Create(System.Text.Json.JsonSerializer.Serialize(request.Status)));

        card.UpdateFieldValue(statusField, fieldValue, _currentUser.UserId, now);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
