using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Automation;

namespace Notrelix.Application.Features.Integrations.Commands.HandleN8nCallback;

public record HandleN8nCallbackCommand(
    Guid ExecutionId,
    string Status,
    string? Response,
    string? Error) : IRequest<Result>;

public class HandleN8nCallbackCommandHandler : IRequestHandler<HandleN8nCallbackCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public HandleN8nCallbackCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(HandleN8nCallbackCommand request, CancellationToken cancellationToken)
    {
        var execution = await _context.AutomationExecutions
            .FirstOrDefaultAsync(item => item.Id == request.ExecutionId, cancellationToken);
        if (execution is null) throw new NotFoundException(nameof(AutomationExecution), request.ExecutionId);

        switch (request.Status.Trim().ToLowerInvariant())
        {
            case "delivered":
            case "success":
            case "succeeded":
                execution.MarkDelivered(request.Response);
                break;
            case "retried":
            case "retry":
                execution.MarkRetried(request.Error);
                break;
            case "failed":
            case "error":
                execution.MarkFailed(request.Error);
                break;
            default:
                return Result.Failure($"Unsupported n8n callback status '{request.Status}'.");
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
