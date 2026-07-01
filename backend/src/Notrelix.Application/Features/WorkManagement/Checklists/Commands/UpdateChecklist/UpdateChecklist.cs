using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklist;

public record UpdateChecklistCommand(Guid ChecklistId) : ICommand<Result>, ITransactionalRequest;

public class UpdateChecklistCommandHandler : IRequestHandler<UpdateChecklistCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateChecklistCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateChecklistCommand request, CancellationToken ct)
    {
        var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == request.ChecklistId, ct);
        if (checklist is null) throw new NotFoundException(nameof(Checklist), request.ChecklistId);
        return Result.Success();
    }
}
