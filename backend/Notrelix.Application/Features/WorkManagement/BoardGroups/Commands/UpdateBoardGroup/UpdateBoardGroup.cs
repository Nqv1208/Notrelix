using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UpdateBoardGroup;

public record UpdateBoardGroupCommand(Guid GroupId, string? Title, string? Color = null) : ICommand<Result>, ITransactionalRequest;

public class UpdateBoardGroupCommandHandler : IRequestHandler<UpdateBoardGroupCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspacePermissionService _permissions;

    public UpdateBoardGroupCommandHandler(
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

    public async Task<Result> Handle(UpdateBoardGroupCommand request, CancellationToken ct)
    {
        var list = await _context.BoardGroups.FirstOrDefaultAsync(l => l.Id == request.GroupId, ct);
        if (list is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);

        await _permissions.EnsureCanEditBoardAsync(list.BoardId, _currentUser.UserId, ct);

        var now = _dateTimeProvider.UtcNow;
        if (request.Title is not null) list.Rename(request.Title, _currentUser.UserId, now);
        if (request.Color is not null) list.UpdateColor(Color.Create(request.Color), _currentUser.UserId, now);
        return Result.Success();
    }
}
