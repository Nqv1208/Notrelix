using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Application.Features.Integrations.Connections;

public static class IntegrationConnectionLookup
{
    public static async Task<IntegrationConnection> RequireAsync(
        IIntegrationDbContext context, Guid workspaceId, Guid connectionId, CancellationToken cancellationToken)
        => await context.IntegrationConnections.FirstOrDefaultAsync(c =>
                c.Id == connectionId
                && c.WorkspaceId == workspaceId
                && c.DeletedAt == null
                && c.Status != IntegrationConnectionStatus.Revoked, cancellationToken)
            ?? throw new NotFoundException(nameof(IntegrationConnection), connectionId);
}
