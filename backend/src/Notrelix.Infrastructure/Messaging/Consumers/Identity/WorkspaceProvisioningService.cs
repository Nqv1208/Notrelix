using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Outbox;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity;

public sealed class WorkspaceProvisioningService
{
    private readonly IWorkspaceDbContext _context;
    private readonly ApplicationDbContext _appContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WorkspaceProvisioningService(IWorkspaceDbContext context, ApplicationDbContext appContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _appContext = appContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task ProvisionPersonalWorkspace(Guid userId, string email, Guid eventId, DateTimeOffset occurredAt, CancellationToken ct)
    {
        // Business idempotency: check if already processed (by event_id + consumer_name)
        if (await _appContext.ProcessedEvents
            .AnyAsync(pe => pe.EventId == eventId && pe.ConsumerName == nameof(WorkspaceProvisioningConsumer), ct))
            return;

        // Business idempotency: check if personal workspace already exists
        if (await _context.Workspaces.AnyAsync(w => w.IsPersonal && w.CreatedBy == userId, ct))
            return;

        var slug = Slug.GenerateFromName($"{email}'s Workspace");
        var workspace = WorkspaceFactory.CreateWithOwner(userId, $"{email}'s Workspace", slug.Value, occurredAt);

        _context.Workspaces.Add(workspace.Workspace);
        _context.WorkspaceMembers.Add(workspace.OwnerMember);

        // Persist ProcessedEvent IN THE SAME TRANSACTION as workspace + member
        // This is CRITICAL: if the process crashes after saving workspace but before
        // recording the processed event, retry would create a duplicate workspace.
        _appContext.ProcessedEvents.Add(
            ProcessedEvent.Create(eventId, nameof(WorkspaceProvisioningConsumer), "user.registered", 1, null, null, occurredAt));

        // Single SaveChangesAsync — workspace, member, and processed event commit together
        await _appContext.SaveChangesAsync(ct);
    }
}
