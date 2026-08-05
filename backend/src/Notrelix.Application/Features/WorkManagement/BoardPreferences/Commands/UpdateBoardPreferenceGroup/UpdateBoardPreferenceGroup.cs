using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.UpdateBoardPreferenceGroup;

public record UpdateBoardPreferenceGroupCommand(
    Guid BoardId,
    Guid ViewId,
    GroupRule? Group)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class UpdateBoardPreferenceGroupCommandHandler : IRequestHandler<UpdateBoardPreferenceGroupCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBoardPreferenceGroupCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateBoardPreferenceGroupCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;

        var preference = await _context.BoardViewUserPreferences
            .FirstOrDefaultAsync(p => p.BoardId == request.BoardId && p.ViewId == request.ViewId && p.UserId == userId, ct);

        if (preference is null)
            throw new NotFoundException("BoardViewUserPreference", $"{request.BoardId}:{request.ViewId}:{userId}");

        preference.ApplyGroup(request.Group, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
