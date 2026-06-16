using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record ArchiveBoardGroupCommand(Guid GroupId) : IRequest<Result>;

public class ArchiveBoardGroupCommandHandler : IRequestHandler<ArchiveBoardGroupCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveBoardGroupCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveBoardGroupCommand request, CancellationToken ct)
    {
        var list = await _context.BoardGroups.FirstOrDefaultAsync(l => l.Id == request.GroupId, ct);
        if (list is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);
        await _permissions.EnsureCanEditBoardAsync(list.BoardId, _currentUser.UserId, ct);
        list.SoftDelete(_currentUser.UserId, _dateTimeProvider.UtcNow);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
