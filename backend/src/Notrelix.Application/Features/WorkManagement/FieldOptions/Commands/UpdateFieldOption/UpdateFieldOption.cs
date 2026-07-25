using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.FieldOptions.Commands.UpdateFieldOption;

public record UpdateFieldOptionCommand(
    Guid FieldId,
    Guid OptionId,
    string Name,
    string ColorHex) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardField, FieldId);
    public PermissionAction Action => PermissionAction.UpdateField;
}

public class UpdateFieldOptionCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider timeProvider) : IRequestHandler<UpdateFieldOptionCommand, Result>
{
    public async Task<Result> Handle(UpdateFieldOptionCommand request, CancellationToken cancellationToken)
    {
        var field = await context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && !f.DeletedAt.HasValue, cancellationToken);

        if (field is null)
            throw new NotFoundException(nameof(BoardField), request.FieldId);

        var now = timeProvider.UtcNow;
        var color = Color.Create(request.ColorHex);

        field.UpdateOption(request.OptionId, request.Name, color, requestContext.UserId, now);

        return Result.Success();
    }
}
