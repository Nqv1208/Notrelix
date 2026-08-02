using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.RestoreBoardView;

[IdempotencyOperation("work-management.board-views.restore-board-view.v1")]
public record RestoreBoardViewCommand(Guid ViewId, string? IdempotencyKey = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardView, ViewId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"restore-view:{ViewId}";
}

public class RestoreBoardViewCommandHandler : IRequestHandler<RestoreBoardViewCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestoreBoardViewCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RestoreBoardViewCommand request, CancellationToken ct)
    {
        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.Id == request.ViewId, ct);
        if (view is null) throw new NotFoundException(nameof(BoardView), request.ViewId);

        view.Restore(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
