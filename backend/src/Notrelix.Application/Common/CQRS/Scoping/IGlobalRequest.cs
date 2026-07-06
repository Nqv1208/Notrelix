namespace Notrelix.Application.Common.CQRS.Scoping;

/// <summary>
/// Marks a request as global/non-tenant scoped.
/// Global means the request does not have AccountId, WorkspaceId, or Resource scope before the handler.
/// It does NOT mean anonymous and does NOT bypass authorization.
/// </summary>
public interface IGlobalRequest;