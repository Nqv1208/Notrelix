using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UnarchiveBoardGroup;

public record UnarchiveBoardGroupCommand(Guid GroupId) : ICommand<Result>, ITransactionalRequest;

public class UnarchiveBoardGroupCommandHandler : IRequestHandler<UnarchiveBoardGroupCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspacePermissionService _permissions;

    public UnarchiveBoardGroupCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UnarchiveBoardGroupCommand request, CancellationToken ct)
    {
        var list = await _context.BoardGroups.FirstOrDefaultAsync(l => l.Id == request.GroupId, ct);
        if (list is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);
        await _permissions.EnsureCanEditBoardAsync(list.BoardId, _currentUser.UserId, ct);
        list.Restore(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
