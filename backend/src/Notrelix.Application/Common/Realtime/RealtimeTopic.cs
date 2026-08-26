namespace Notrelix.Application.Common.Realtime;

public sealed record RealtimeTopic(string Namespace, string ResourceKind, Guid ResourceId);
