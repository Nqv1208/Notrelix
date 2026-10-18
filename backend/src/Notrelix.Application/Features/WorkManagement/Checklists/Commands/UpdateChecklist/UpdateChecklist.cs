using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklist;

/// <summary>
/// Renames a checklist. Placement is not part of PATCH: a checklist moves only
/// through <c>MoveChecklistCommand</c> (WM-V3-APP-CHK-001).
/// </summary>
[IdempotencyOperation("work-management.checklists.update-checklist.v1")]
public record UpdateChecklistCommand(Guid ChecklistId, string? Title, long ExpectedVersion) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.checklist"), ChecklistId);
}

public class UpdateChecklistCommandHandler : IRequestHandler<UpdateChecklistCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateChecklistCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateChecklistCommand request, CancellationToken ct)
    {
        var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == request.ChecklistId, ct)
            ?? throw new NotFoundException(nameof(Checklist), request.ChecklistId);

        if (request.Title is not null)
            checklist.Rename(request.Title, _currentUser.UserId, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
