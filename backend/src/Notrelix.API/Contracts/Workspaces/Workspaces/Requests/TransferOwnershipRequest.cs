namespace Notrelix.API.Contracts.Workspaces.Workspaces.Requests;

public sealed record TransferOwnershipRequest(Guid NewOwnerId, long ExpectedVersion);
