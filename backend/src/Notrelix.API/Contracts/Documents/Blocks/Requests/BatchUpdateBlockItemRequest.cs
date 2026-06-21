namespace Notrelix.API.Contracts.Documents.Blocks.Requests;

public record BatchUpdateBlockItemRequest(Guid Id, string? Type, string? Properties, double? Position, Guid? ParentId);
