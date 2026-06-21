namespace Notrelix.API.Contracts.Documents.Blocks.Requests;

public record BatchUpdateBlocksRequest(List<BatchUpdateBlockItemRequest> Blocks);
