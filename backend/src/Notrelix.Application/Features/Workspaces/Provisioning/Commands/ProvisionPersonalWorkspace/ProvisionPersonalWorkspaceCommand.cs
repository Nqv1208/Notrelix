using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.CQRS;
using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Provisioning.Commands.ProvisionPersonalWorkspace;

public sealed record ProvisionPersonalWorkspaceCommand(
    Guid UserId,
    string Email,
    Guid MessageId,
    Guid? SourceEventId,
    string SourceMessageName,
    int SourceMessageVersion,
    string? CorrelationId,
    string? CausationId,
    DateTimeOffset OccurredAt
) : ICommand<ProvisionPersonalWorkspaceResult>,
    ITransactionalRequest,
    IMessageTriggeredRequest
{
    public string ConsumerName => ConsumerNames.PersonalWorkspaceProvisioning;
    public Guid? WorkspaceId => null;
}

public sealed record ProvisionPersonalWorkspaceResult(
    Guid WorkspaceId,
    bool AlreadyExisted);

public sealed class ProvisionPersonalWorkspaceCommandHandler
    : IRequestHandler<ProvisionPersonalWorkspaceCommand, ProvisionPersonalWorkspaceResult>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IMessageDeduplicationStore _deduplicationStore;
    private readonly IDateTimeProvider _clock;

    public ProvisionPersonalWorkspaceCommandHandler(
        IWorkspaceDbContext workspaceContext,
        IMessageDeduplicationStore deduplicationStore,
        IDateTimeProvider clock)
    {
        _workspaceContext = workspaceContext;
        _deduplicationStore = deduplicationStore;
        _clock = clock;
    }

    public async Task<ProvisionPersonalWorkspaceResult> Handle(
        ProvisionPersonalWorkspaceCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        if (await _deduplicationStore.IsProcessedAsync(
            request.MessageId, request.ConsumerName, cancellationToken))
        {
            var existingId = await _workspaceContext.Workspaces
                .Where(w => w.IsPersonal && w.CreatedBy == request.UserId)
                .Select(w => w.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Message {request.MessageId} was marked processed by " +
                    $"{request.ConsumerName}, but personal workspace was not found. " +
                    "Data consistency violation.");
            }

            return new ProvisionPersonalWorkspaceResult(existingId, AlreadyExisted: true);
        }

        var existingWorkspace = await _workspaceContext.Workspaces
            .Where(w => w.IsPersonal && w.CreatedBy == request.UserId)
            .Select(w => new { w.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingWorkspace is not null)
        {
            _deduplicationStore.MarkProcessed(
                request.MessageId,
                request.ConsumerName,
                request.SourceMessageName,
                request.SourceMessageVersion,
                sourceEventId: request.SourceEventId,
                workspaceId: existingWorkspace.Id,
                processedAt: now);

            return new ProvisionPersonalWorkspaceResult(
                existingWorkspace.Id, AlreadyExisted: true);
        }

        var slug = Slug.GenerateFromName($"{request.Email}'s Workspace");
        var workspace = WorkspaceFactory.CreateWithOwner(
            request.UserId,
            $"{request.Email}'s Workspace",
            slug.Value,
            request.OccurredAt,
            isPersonal: true);

        _workspaceContext.Workspaces.Add(workspace.Workspace);
        _workspaceContext.WorkspaceMembers.Add(workspace.OwnerMember);

        _deduplicationStore.MarkProcessed(
            request.MessageId,
            request.ConsumerName,
            request.SourceMessageName,
            request.SourceMessageVersion,
            sourceEventId: request.SourceEventId,
            workspaceId: workspace.Workspace.Id,
            processedAt: now);

        return new ProvisionPersonalWorkspaceResult(
            workspace.Workspace.Id,
            AlreadyExisted: false);
    }
}
