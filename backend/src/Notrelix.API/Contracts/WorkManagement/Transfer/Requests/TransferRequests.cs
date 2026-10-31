namespace Notrelix.API.Contracts.WorkManagement.Transfer.Requests;

/// <summary>
/// OpenAPI description of the multipart/form-data body of the import upload. The endpoint reads the body as a
/// stream and never binds this type; the format is decided from the bytes, not from the part's content type.
/// </summary>
public sealed class ImportFileForm
{
    /// <summary>A CSV or XLSX file of at most 10 MB and 5,000 data rows.</summary>
    public required IFormFile File { get; init; }
}

/// <param name="Mapping">Which file column (0-based) goes to which target: a field id, "$title" or "$group".</param>
public record ImportMappingRequest(IReadOnlyList<ImportColumnMapRequest> Mapping);

public record ImportColumnMapRequest(int Column, string Target);

/// <param name="Format">csv or xlsx.</param>
/// <param name="Scope">view: the current view's filter and sort; all: every live item of the board.</param>
/// <param name="Filter">The view's compound filter, as the board read takes it.</param>
/// <param name="Sort">The view's sort, as the board read takes it.</param>
/// <param name="Locale">vi or en: the language of the file's fixed column names.</param>
public record ExportRequest(string Format, string Scope, string? Filter, string? Sort, string? Locale);
