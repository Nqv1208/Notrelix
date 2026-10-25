using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;

public record UpdateBlockCommand(
    Guid PageId,
    Guid BlockId,
    string? Type,
    string? Properties
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    // A block is edited with the page it belongs to: the page decides who may (Editor level).
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class UpdateBlockCommandHandler : IRequestHandler<UpdateBlockCommand, Result>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public UpdateBlockCommandHandler(IDocumentDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateBlockCommand request, CancellationToken ct)
    {
        var block = await _context.Blocks.FirstOrDefaultAsync(block => block.Id == request.BlockId && block.PageId == request.PageId && block.DeletedAt == null, ct);
        if (block is null) throw new NotFoundException(nameof(Block), request.BlockId);

        // A block keeps its type: the domain has no type change, so a different type is refused.
        if (request.Type is not null
            && !string.Equals(request.Type, block.Type.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(new ApplicationError(
                "docs.block.type-immutable", "A block keeps its type; create a block of the new type instead.", ApplicationErrorType.Validation));
        }

        if (request.Properties is not null)
        {
            block.UpdateContent(BlockContent.Create(JsonValue.Create(request.Properties)), _currentUser.UserId, _dateTimeProvider.UtcNow);
        }

        return Result.Success();
    }
}
