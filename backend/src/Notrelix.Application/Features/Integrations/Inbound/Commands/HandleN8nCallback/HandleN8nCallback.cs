using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Application.Features.Integrations.Inbound.Commands.HandleN8nCallback;

public record HandleN8nCallbackCommand(
    Guid ExecutionId,
    string Status,
    string? Response,
    string? Error) : ICommand<Result>, ITransactionalRequest, IGlobalRequest;

public class HandleN8nCallbackCommandHandler : IRequestHandler<HandleN8nCallbackCommand, Result>
{
    private readonly IIntegrationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public HandleN8nCallbackCommandHandler(IIntegrationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(HandleN8nCallbackCommand request, CancellationToken cancellationToken)
    {
        var execution = await _context.AutomationExecutions
            .FirstOrDefaultAsync(item => item.Id == request.ExecutionId, cancellationToken);
        if (execution is null) throw new NotFoundException(nameof(AutomationExecution), request.ExecutionId);

        var now = _dateTimeProvider.UtcNow;
        switch (request.Status.Trim().ToLowerInvariant())
        {
            case "delivered":
            case "success":
            case "succeeded":
                execution.Succeed(now);
                break;
            case "retried":
            case "retry":
                execution.Cancel(Guid.Empty, now);
                break;
            case "failed":
            case "error":
                execution.Fail(request.Error ?? "Unknown error", now);
                break;
            default:
                return Result.Failure(new ApplicationError("integrations.n8n.unsupported-status", $"Unsupported n8n callback status '{request.Status}'.", ApplicationErrorType.Validation));
        }

        return Result.Success();
    }
}
