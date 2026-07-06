namespace Notrelix.Application.Common.CQRS;

/// <summary>
/// Marker for queries that read RLS-protected source tables.
/// DbRequestScopeBehavior opens a read-only transaction and applies RLS session vars.
/// </summary>
public interface IRlsReadRequest;
