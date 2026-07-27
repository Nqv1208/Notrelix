using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.RenameBoardView;

public record RenameBoardViewCommand(
    Guid ViewId,
    string Name,
    string? IdempotencyKey = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardView, ViewId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"rename-view:{ViewId}";
}

public class RenameBoardViewCommandHandler : IRequestHandler<RenameBoardViewCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RenameBoardViewCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RenameBoardViewCommand request, CancellationToken ct)
    {
        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.Id == request.ViewId, ct);
        if (view is null) throw new NotFoundException(nameof(BoardView), request.ViewId);

        view.Rename(request.Name, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
