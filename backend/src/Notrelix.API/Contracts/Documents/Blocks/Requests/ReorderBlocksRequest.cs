namespace Notrelix.API.Contracts.Documents.Blocks.Requests;

public record ReorderBlocksRequest(Guid PageId, List<ReorderBlockItemRequest> Items);
