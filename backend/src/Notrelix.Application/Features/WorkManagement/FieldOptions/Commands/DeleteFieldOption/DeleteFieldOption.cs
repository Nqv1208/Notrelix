using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.FieldOptions.Commands.DeleteFieldOption;

public record DeleteFieldOptionCommand(
    Guid FieldId,
    Guid OptionId) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-field"), FieldId);
    public PermissionAction Action => PermissionAction.UpdateField;
}

public class DeleteFieldOptionCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider timeProvider) : IRequestHandler<DeleteFieldOptionCommand, Result>
{
    public async Task<Result> Handle(DeleteFieldOptionCommand request, CancellationToken cancellationToken)
    {
        var field = await context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && !f.DeletedAt.HasValue, cancellationToken);

        if (field is null)
            throw new NotFoundException(nameof(BoardField), request.FieldId);

        var now = timeProvider.UtcNow;

        field.RemoveOption(request.OptionId, requestContext.UserId, now);

        return Result.Success();
    }
}
