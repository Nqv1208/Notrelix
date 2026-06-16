using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Document.Commands.Pages.UpdatePage;

public record UpdatePageCommand(
    Guid PageId,
    string? Title
) : IRequest<Result>;

public class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public UpdatePageCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdatePageCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.FirstOrDefaultAsync(page => page.Id == request.PageId && !page.IsDeleted, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        if (request.Title is not null) page.Rename(request.Title, _currentUser.UserId, _dateTimeProvider.UtcNow);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
