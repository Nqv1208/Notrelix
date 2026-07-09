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
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePageCommandHandler(IDocumentDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreatePageCommand request, CancellationToken ct)
    {
        // Workspace existence is verified by workspace-scoped authorization at a higher layer.

        var page = Page.Create(_requestContext.RequireAccountId(), request.WorkspaceId, request.Title, _requestContext.UserId, _dateTimeProvider.UtcNow, request.ParentId);
        _context.Pages.Add(page);

        return Result<Guid>.Success(page.Id);
    }
}
