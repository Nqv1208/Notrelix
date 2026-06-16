using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.Commands.Labels.UpdateLabel;

public record UpdateLabelCommand(Guid LabelId, string? Name, string? Color) : IRequest<Result>;

public class UpdateLabelCommandHandler : IRequestHandler<UpdateLabelCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateLabelCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
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
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
