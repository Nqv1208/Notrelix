using System.Globalization;
using System.Text.Json;
using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Application.Features.Integrations.Calendar.Processing;

/// <summary>
/// Integrations-owned semantic target for a verified calendar callback.
/// Provider adapters expose the stable external event identity plus the
/// mapped Notrelix resource; this use case reconciles the durable calendar
/// projection and its link under the tenant restored by the message pipeline.
/// </summary>
public sealed class CalendarWebhookProcessingUseCase(IIntegrationDbContext db)
    : ICalendarWebhookProcessingUseCase
{
    public async Task<CalendarWebhookProcessingOutcome> ProcessAsync(
        CalendarWebhookProcessingInput input,
        CancellationToken cancellationToken)
    {
        CalendarWebhookPayload payload;
        try
        {
            payload = CalendarWebhookPayload.Parse(input);
        }
        catch (JsonException)
        {
            return CalendarWebhookProcessingOutcome.TerminalFailure;
        }
        catch (FormatException)
        {
            return CalendarWebhookProcessingOutcome.TerminalFailure;
        }
        catch (KeyNotFoundException)
        {
            return CalendarWebhookProcessingOutcome.TerminalFailure;
        }
        catch (InvalidOperationException)
        {
            return CalendarWebhookProcessingOutcome.TerminalFailure;
        }

        var integration = await db.CalendarIntegrations
            .Include(x => x.EventLinks)
            .SingleOrDefaultAsync(
                x => x.ConnectionId == input.ConnectionId
                    && x.WorkspaceId == input.WorkspaceId
                    && x.IsActive
                    && x.DeletedAt == null,
                cancellationToken);

        if (integration is null
            || !string.Equals(integration.Provider.ToString(), input.Provider, StringComparison.OrdinalIgnoreCase))
        {
            return CalendarWebhookProcessingOutcome.TerminalFailure;
        }

        var target = ResourceRef.Create(payload.ResourceKind, payload.ResourceId, input.WorkspaceId);
        var syncHash = CalendarSyncFingerprint.Create(payload.Title, payload.DueDate);

        var existingExternalLink = integration.EventLinks
            .SingleOrDefault(x => x.ExternalEventId == input.ExternalEventId);
        var conflictingInternalLink = integration.EventLinks
            .SingleOrDefault(x => x.InternalEventId == payload.ResourceId);

        if (existingExternalLink is not null && existingExternalLink.InternalEventId != payload.ResourceId)
        {
            return CalendarWebhookProcessingOutcome.TerminalFailure;
        }

        if (existingExternalLink is null && conflictingInternalLink is not null)
        {
            return CalendarWebhookProcessingOutcome.TerminalFailure;
        }

        var calendarEvent = await db.CalendarEvents
            .SingleOrDefaultAsync(
                x => x.IntegrationId == integration.Id
                    && x.ExternalEventId == input.ExternalEventId,
                cancellationToken);

        if (calendarEvent is null)
        {
            db.CalendarEvents.Add(CalendarEvent.Create(
                integration.Id,
                input.ExternalEventId,
                target,
                syncHash));
        }
        else
        {
            if (!calendarEvent.Target.Equals(target))
            {
                return CalendarWebhookProcessingOutcome.TerminalFailure;
            }

            calendarEvent.UpdateSyncHash(syncHash);
        }

        if (existingExternalLink is not null)
        {
            existingExternalLink.UpdateETag(payload.ETag);
        }
        else
        {
            integration.LinkEvent(payload.ResourceId, input.ExternalEventId, payload.ETag);
        }

        return CalendarWebhookProcessingOutcome.Completed;
    }

    private sealed record CalendarWebhookPayload(
        ResourceKind ResourceKind,
        Guid ResourceId,
        string? Title,
        DateTime? DueDate,
        string? ETag)
    {
        public static CalendarWebhookPayload Parse(CalendarWebhookProcessingInput input)
        {
            using var document = JsonDocument.Parse(input.DecryptedRawPayload);
            var root = document.RootElement;

            var eventId = root.GetProperty("eventId").GetString();
            if (!string.Equals(eventId, input.ExternalEventId, StringComparison.Ordinal))
            {
                throw new FormatException("Calendar payload event identity does not match the verified receipt.");
            }

            var resourceKindValue = root.GetProperty("resourceKind").GetString();
            if (!ResourceKind.TryCreate(resourceKindValue, out var resourceKind))
            {
                throw new FormatException("Calendar payload resource kind is invalid.");
            }

            if (!Guid.TryParse(root.GetProperty("resourceId").GetString(), out var resourceId)
                || resourceId == Guid.Empty)
            {
                throw new FormatException("Calendar payload resource id is invalid.");
            }

            DateTime? dueDate = null;
            if (root.TryGetProperty("dueDate", out var dueDateElement)
                && dueDateElement.ValueKind is not JsonValueKind.Null
                && dueDateElement.ValueKind != JsonValueKind.Undefined)
            {
                if (!DateTime.TryParse(
                    dueDateElement.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsedDueDate))
                {
                    throw new FormatException("Calendar payload due date is invalid.");
                }

                dueDate = parsedDueDate;
            }

            var title = root.TryGetProperty("title", out var titleElement)
                ? titleElement.GetString()
                : null;
            var etag = root.TryGetProperty("etag", out var etagElement)
                ? etagElement.GetString()
                : null;

            return new CalendarWebhookPayload(resourceKind, resourceId, title, dueDate, etag);
        }
    }
}
