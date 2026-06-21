namespace Notrelix.API.Contracts.Documents.Blocks.Requests;

public record ReorderBlockItemRequest(Guid BlockId, double Position, Guid? ParentId = null);
