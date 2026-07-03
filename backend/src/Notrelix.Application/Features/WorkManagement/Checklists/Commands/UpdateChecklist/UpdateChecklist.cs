using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklist;

public record UpdateChecklistCommand(Guid ChecklistId, string? Title, double? Position) : ICommand<Result>, ITransactionalRequest;

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
            checklist.UpdatePosition(FractionalIndex.Create(request.Position.Value.ToString("F0")), _currentUser.UserId, now);

        return Result.Success();
    }
}
