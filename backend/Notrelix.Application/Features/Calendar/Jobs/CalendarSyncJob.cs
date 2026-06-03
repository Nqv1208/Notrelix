using Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Calendar.Jobs;

public sealed record CalendarSyncJob(ResourceType ResourceType, Guid ResourceId);
