namespace Notrelix.Application.Common.CQRS;

public sealed record RealtimeTopic(string Namespace, string ResourceType, Guid ResourceId);
