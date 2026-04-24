using MediatR;
using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Calendar.Commands;

// ──────── Connect Calendar ────────
public record ConnectCalendarCommand(
    string Provider,
    string AccessToken,
    string? RefreshToken,
    Guid? WorkspaceId,
    string? SyncDirection
) : IRequest<Result<Guid>>;

// ──────── Disconnect Calendar ────────
public record DisconnectCalendarCommand(Guid IntegrationId) : IRequest<Result>;

// ──────── Trigger Sync ────────
public record TriggerCalendarSyncCommand(Guid IntegrationId) : IRequest<Result>;

// ──────── Handle Webhook ────────
public record HandleCalendarWebhookCommand(string Provider, string Payload) : IRequest<Result>;
