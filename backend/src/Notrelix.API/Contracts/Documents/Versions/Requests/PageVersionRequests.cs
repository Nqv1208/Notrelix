namespace Notrelix.API.Contracts.Documents.Versions.Requests;

public record CreatePageVersionRequest(string Name);

/// <param name="CurrentBlocks">Every live block of the page with the version the caller last read; a mismatch is a 412 and nothing is restored.</param>
public record RestorePageVersionRequest(List<RestoreBlockVersionRequest> CurrentBlocks);

public record RestoreBlockVersionRequest(Guid BlockId, long Version);
