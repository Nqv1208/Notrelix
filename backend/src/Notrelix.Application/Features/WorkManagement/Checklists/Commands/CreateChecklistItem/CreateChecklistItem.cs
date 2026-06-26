using MediatR;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;

public record CreateChecklistItemCommand(Guid ChecklistId, string Title) : ICommand<Result<Guid>>, ITransactionalRequest;

public class CreateChecklistItemCommandHandler : IRequestHandler<CreateChecklistItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    public CreateChecklistItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateChecklistItemCommand request, CancellationToken ct)
    {
        var position = FractionalIndex.Initial();
        var item = ChecklistItem.Create(request.ChecklistId, request.Title, position);
        _context.ChecklistItems.Add(item);
        return Result<Guid>.Success(item.Id);
    }
}
