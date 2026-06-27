using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Outbox;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity;

public sealed class WorkspaceProvisioningService
{
    private readonly IWorkspaceDbContext _context;
    private readonly IApplicationDbContext _appContext;
    private readonly IProcessedEventStore _processedEvents;

    public WorkspaceProvisioningService(IWorkspaceDbContext context, IApplicationDbContext appContext, IProcessedEventStore processedEvents)
    {
        _context = context;
        _appContext = appContext;
        _processedEvents = processedEvents;
    }

    public async Task ProvisionPersonalWorkspace(Guid userId, string email, Guid eventId, DateTimeOffset occurredAt, CancellationToken ct)
    {
        if (await _processedEvents.IsProcessedAsync(eventId, nameof(WorkspaceProvisioningConsumer), ct))
            return;

        if (await _context.Workspaces.AnyAsync(w => w.IsPersonal && w.CreatedBy == userId, ct))
            return;

        var slug = Slug.GenerateFromName($"{email}'s Workspace");
        var workspace = WorkspaceFactory.CreateWithOwner(userId, $"{email}'s Workspace", slug.Value, occurredAt);

        _context.Workspaces.Add(workspace.Workspace);
        _context.WorkspaceMembers.Add(workspace.OwnerMember);
        await _appContext.SaveChangesAsync(ct);

        await _processedEvents.MarkProcessedAsync(
            ProcessedEvent.Create(eventId, nameof(WorkspaceProvisioningConsumer), "user.registered", 1, null, null, occurredAt), ct);
    }
}
