namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

public record UpdateBoardItemFieldValuesRequest(long ExpectedVersion, Dictionary<Guid, object?>? Values = null, Guid? FieldDefinitionId = null, object? Value = null);
