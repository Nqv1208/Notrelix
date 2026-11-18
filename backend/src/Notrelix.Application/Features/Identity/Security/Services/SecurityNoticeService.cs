using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Security.Abstractions;

namespace Notrelix.Application.Features.Identity.Security.Services;

/// <summary>
/// Producer-owned <see cref="ISecurityNotice"/>: stages the notice as an integration event in the request's transaction (the
/// same outbox as the change). A worker consumer renders and queues the mail, because the application login may not write
/// the email outbox for a request that has no Workspace.
/// </summary>
public sealed class SecurityNoticeService : ISecurityNotice
{
    private const int MaxDeviceLength = 200;

    private readonly IIdentityDbContext _context;
    private readonly IClientMetadata _client;
    private readonly IIntegrationEventCollector _events;

    public SecurityNoticeService(IIdentityDbContext context, IClientMetadata client, IIntegrationEventCollector events)
    {
        _context = context;
        _client = client;
        _events = events;
    }

    public async Task QueueAsync(
        Guid userId,
        SecurityNoticeKind kind,
        DateTimeOffset occurredAt,
        Guid? subjectId,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Name, Email = u.Email.Value })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            return;
        }

        var eventId = Guid.CreateVersion7();
        _events.Add(new SecurityNoticeRequestedIntegrationEventV1(
            EventId: eventId,
            UserId: userId,
            Email: user.Email,
            UserName: user.Name,
            Kind: kind.ToString(),
            Device: DeviceText(),
            CorrelationId: eventId,
            SubjectId: subjectId,
            ActorUserId: userId,
            OccurredAt: occurredAt));
    }

    private string DeviceText()
    {
        var agent = string.IsNullOrWhiteSpace(_client.UserAgent) ? null : _client.UserAgent.Trim();
        if (agent is { Length: > MaxDeviceLength })
        {
            agent = agent[..MaxDeviceLength];
        }

        var parts = new[] { agent, string.IsNullOrWhiteSpace(_client.IpAddress) ? null : _client.IpAddress.Trim() }
            .Where(p => p is not null);
        var text = string.Join(" · ", parts);
        return text.Length == 0 ? "Unknown device" : text;
    }
}
