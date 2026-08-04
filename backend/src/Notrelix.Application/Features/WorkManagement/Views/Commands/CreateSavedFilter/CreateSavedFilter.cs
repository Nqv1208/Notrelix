using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Views.Commands.CreateSavedFilter;

[IdempotencyOperation("work-management.views.create-saved-filter.v1")]
public record CreateSavedFilterCommand(Guid BoardId, string Name, List<FilterRule>? Rules = null, Guid? ViewId = null)
    : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class CreateSavedFilterCommandHandler : IRequestHandler<CreateSavedFilterCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateSavedFilterCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateSavedFilterCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException("Board", request.BoardId);

        var filter = SavedFilter.Create(
            _requestContext.RequireAccountId(),
            board.WorkspaceId,
            request.BoardId,
            request.Name,
            request.Rules ?? [],
            _requestContext.UserId,
            _dateTimeProvider.UtcNow,
            request.ViewId);

        _context.SavedFilters.Add(filter);
        return Result<Guid>.Success(filter.Id);
    }
}
