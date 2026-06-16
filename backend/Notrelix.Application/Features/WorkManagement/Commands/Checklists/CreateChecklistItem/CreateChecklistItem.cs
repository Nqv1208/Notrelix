using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.CreateChecklistItem;

public record CreateChecklistItemCommand(Guid ChecklistId, string Title) : IRequest<Result<Guid>>;

public class CreateChecklistItemCommandHandler : IRequestHandler<CreateChecklistItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    public CreateChecklistItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateChecklistItemCommand request, CancellationToken ct)
    {
        var position = FractionalIndex.Initial();
        var item = ChecklistItem.Create(request.ChecklistId, request.Title, position);
        _context.ChecklistItems.Add(item);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(item.Id);
    }
}
