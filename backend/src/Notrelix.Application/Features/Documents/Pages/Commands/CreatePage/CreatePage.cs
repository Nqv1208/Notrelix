using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;

public record CreatePageCommand(
    Guid WorkspaceId,
    string Title,
    Guid? ParentId
) : ICommand<Result<Guid>>, ITransactionalRequest;

public class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, Result<Guid>>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentTenantContext _tenant;

    public CreatePageCommandHandler(IDocumentDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, ICurrentTenantContext tenant)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _tenant = tenant;
    }

    public async Task<Result<Guid>> Handle(CreatePageCommand request, CancellationToken ct)
    {
        // Workspace existence is verified by workspace-scoped authorization at a higher layer.

        var page = Page.Create(_tenant.RequireAccountId(), request.WorkspaceId, request.Title, _currentUser.UserId, _dateTimeProvider.UtcNow, request.ParentId);
        _context.Pages.Add(page);

        return Result<Guid>.Success(page.Id);
    }
}
