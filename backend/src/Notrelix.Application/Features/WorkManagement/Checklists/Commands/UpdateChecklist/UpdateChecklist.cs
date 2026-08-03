using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklist;

[IdempotencyOperation("work-management.checklists.update-checklist.v1")]
public record UpdateChecklistCommand(Guid ChecklistId, string? Title, double? Position) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Checklist, ChecklistId);
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
        var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == request.ChecklistId, ct);
        if (checklist is null) throw new NotFoundException(nameof(Checklist), request.ChecklistId);

        var now = _dateTimeProvider.UtcNow;

        if (request.Title is not null)
            checklist.Rename(request.Title, _currentUser.UserId, now);

        if (request.Position.HasValue)
            checklist.UpdatePosition(FractionalIndexGenerator.GenerateKeyBetween(null, null), _currentUser.UserId, now);

        return Result.Success();
    }
}
