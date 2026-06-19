using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.ReorderBoardFields;

public record ReorderBoardFieldsCommand(Guid BoardId, List<ReorderItem> Items) : ICommand<Result>, ITransactionalRequest;

public class ReorderBoardFieldsCommandHandler : IRequestHandler<ReorderBoardFieldsCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReorderBoardFieldsCommandHandler(
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

    public async Task<Result> Handle(ReorderBoardFieldsCommand request, CancellationToken ct)
    {
        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        var now = _dateTimeProvider.UtcNow;
        foreach (var item in request.Items)
        {
            var column = await _context.BoardFields
                .FirstOrDefaultAsync(value => value.Id == item.Id && value.BoardId == request.BoardId, ct);
            if (column is not null)
            {
                column.UpdateSettings(column.Settings, _currentUser.UserId, now);
            }
        }

        return Result.Success();
    }
}
