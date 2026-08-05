using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.FieldOptions.Commands.ReorderFieldOptions;

public record ReorderFieldOptionsCommand(
    Guid FieldId,
    List<Guid> OrderedOptionIds) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-field"), FieldId);
    public PermissionAction Action => PermissionAction.UpdateField;
}

public class ReorderFieldOptionsCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider timeProvider) : IRequestHandler<ReorderFieldOptionsCommand, Result>
{
    public async Task<Result> Handle(ReorderFieldOptionsCommand request, CancellationToken cancellationToken)
    {
        var field = await context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && !f.DeletedAt.HasValue, cancellationToken);

        if (field is null)
            throw new NotFoundException(nameof(BoardField), request.FieldId);

        var now = timeProvider.UtcNow;

        field.ReorderOptions(request.OrderedOptionIds, requestContext.UserId, now);

        return Result.Success();
    }
}
