namespace Notrelix.API.Contracts.Governance.ResourcePermissions.Requests;

public record GrantPermissionRequest(string SubjectType, Guid SubjectId, string Level, DateTime? ExpiresAt = null);
