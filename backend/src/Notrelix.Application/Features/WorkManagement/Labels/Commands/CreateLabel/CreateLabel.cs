using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.CreateLabel;

[IdempotencyOperation("work-management.labels.create-label.v1")]
public record CreateLabelCommand(Guid BoardId, string Color, string? Name)
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    // A label is part of the Board schema: an Editor change, not Board management.
    public PermissionAction Action => PermissionAction.CreateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class CreateLabelCommandHandler : IRequestHandler<CreateLabelCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public CreateLabelCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateLabelCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result<Guid>> HandleCore(CreateLabelCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException("Board", request.BoardId);

        var label = Label.Create(_requestContext.RequireAccountId(), board.WorkspaceId, request.BoardId, request.Name ?? "", LabelColor.Create(request.Color), _requestContext.UserId, _dateTimeProvider.UtcNow);
        _context.Labels.Add(label);
        return Result<Guid>.Success(label.Id);
    }
}
