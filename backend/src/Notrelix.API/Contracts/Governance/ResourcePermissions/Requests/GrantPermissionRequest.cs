namespace Notrelix.API.Contracts.Governance.ResourcePermissions.Requests;

public record GrantPermissionRequest(string SubjectType, Guid SubjectId, string Level, long? ExpectedVersion = null, DateTime? ExpiresAt = null);
