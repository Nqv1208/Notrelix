using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklistItem;

public record UpdateChecklistItemCommand(Guid ItemId, bool? IsChecked) : ICommand<Result>, ITransactionalRequest;

public class UpdateChecklistItemCommandHandler : IRequestHandler<UpdateChecklistItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateChecklistItemCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateChecklistItemCommand request, CancellationToken ct)
    {
        var item = await _context.ChecklistItems.FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);
        if (item is null) throw new NotFoundException(nameof(ChecklistItem), request.ItemId);

        if (request.IsChecked.HasValue)
        {
            var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == item.ChecklistId, ct);
            if (checklist is null) throw new NotFoundException(nameof(Checklist), item.ChecklistId);
            checklist.ToggleItem(request.ItemId, _currentUser.UserId, _dateTimeProvider.UtcNow);
        }

        return Result.Success();
    }
}
