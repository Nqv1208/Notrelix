using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;

public record CreateBlockCommand(
    Guid PageId,
    BlockType Type,
    string Properties,
    string Position,
    Guid? ParentBlockId
) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Page, PageId);
}

public class CreateBlockCommandHandler : IRequestHandler<CreateBlockCommand, Result<Guid>>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBlockCommandHandler(IDocumentDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBlockCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PageId && !p.IsDeleted, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        var content = BlockContent.Create(JsonValue.Create(request.Properties ?? "{}"));
        var position = FractionalIndex.Create(request.Position);

        var block = Block.Create(_requestContext.RequireAccountId(), page.WorkspaceId, request.PageId, request.Type, content, position, _requestContext.UserId, _dateTimeProvider.UtcNow, parentId: request.ParentBlockId);
        _context.Blocks.Add(block);
        return Result<Guid>.Success(block.Id);
    }
}
