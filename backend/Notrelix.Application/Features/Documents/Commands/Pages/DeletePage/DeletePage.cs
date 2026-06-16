using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Document.Commands.Pages.DeletePage;

public record DeletePageCommand(Guid PageId) : IRequest<Result>;

public class DeletePageCommandHandler : IRequestHandler<DeletePageCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public DeletePageCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeletePageCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.FirstOrDefaultAsync(page => page.Id == request.PageId && !page.IsDeleted, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        page.SoftDelete(_currentUser.UserId, _dateTimeProvider.UtcNow);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
