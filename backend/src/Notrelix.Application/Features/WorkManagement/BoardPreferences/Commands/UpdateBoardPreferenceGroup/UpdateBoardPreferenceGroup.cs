using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.Authorization;

namespace Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.UpdateBoardPreferenceGroup;

public record UpdateBoardPreferenceGroupCommand(
    Guid BoardId,
    Guid ViewId,
    GroupRule? Group)
    : ICommand<Result>, IWriteRequest, IPersonalBoardRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    // A person's own settings of a view are decided on the view (a Board child): a view that is not there, or
    // is another person's private one, is the identical NotFound.
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-view"), ViewId);
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
