using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.FieldOptions.Commands.CreateFieldOption;

public record CreateFieldOptionCommand(
    Guid FieldId,
    string Name,
    string ColorHex,
    double Position) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-field"), FieldId);
    public PermissionAction Action => PermissionAction.UpdateField;
}

public class CreateFieldOptionCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider timeProvider) : IRequestHandler<CreateFieldOptionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateFieldOptionCommand request, CancellationToken cancellationToken)
    {
        var field = await context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && !f.DeletedAt.HasValue, cancellationToken);

        if (field is null)
            throw new NotFoundException(nameof(BoardField), request.FieldId);

        var now = timeProvider.UtcNow;
        var position = FractionalIndexGenerator.GenerateKeyBetween(null, null);
        var color = Color.Create(request.ColorHex);

        field.AddOption(request.Name, color, position, requestContext.UserId, now);

        return Result<Guid>.Success(field.Id);
    }
}
