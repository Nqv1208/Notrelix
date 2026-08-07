namespace Notrelix.Application.Common.PostCommit;

public sealed record RealtimeTopic(string Namespace, string ResourceKind, Guid ResourceId);
