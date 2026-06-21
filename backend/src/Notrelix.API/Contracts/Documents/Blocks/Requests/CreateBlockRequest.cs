namespace Notrelix.API.Contracts.Documents.Blocks.Requests;

public record CreateBlockRequest(string Type, string? Properties = null, double? Position = null, Guid? ParentId = null);
