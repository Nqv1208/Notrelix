using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;

public record UpdateBlockCommand(
    Guid BlockId,
    string? Type,
    string? Properties
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.block"), BlockId);
}

public class UpdateBlockCommandHandler : IRequestHandler<UpdateBlockCommand, Result>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentUser _currentUser;
    public UpdateBlockCommandHandler(IDocumentDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateBlockCommand request, CancellationToken ct)
    {
        var block = await _context.Blocks.FirstOrDefaultAsync(block => block.Id == request.BlockId && !block.IsDeleted, ct);
        if (block is null) throw new NotFoundException(nameof(Block), request.BlockId);

        return Result.Success();
    }
}
