using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Common.Idempotency;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.UpdateLabel;

[IdempotencyOperation("work-management.labels.update-label.v1")]
public record UpdateLabelCommand(Guid LabelId, string? Name, string? Color, string? IdempotencyKey = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Label, LabelId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"update-label:{LabelId}";
}

public class UpdateLabelCommandHandler : IRequestHandler<UpdateLabelCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateLabelCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateLabelCommand request, CancellationToken ct)
    {
        var label = await _context.Labels.FirstOrDefaultAsync(l => l.Id == request.LabelId, ct);
        if (label is null) throw new NotFoundException(nameof(Label), request.LabelId);
        var name = request.Name ?? label.Name;
        var color = request.Color is not null ? LabelColor.Create(request.Color) : label.Color;
        label.Update(name, color, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
