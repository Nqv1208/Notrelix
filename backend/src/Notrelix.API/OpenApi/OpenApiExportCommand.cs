using Notrelix.API.Endpoints;

namespace Notrelix.API.OpenApi;

/// <summary>
/// Deterministic OpenAPI export command.
/// Generates the v1 spec without database, Redis, workers, or network.
/// Usage: dotnet run --project src/Notrelix.API -- --export-openapi [output-path]
/// </summary>
public static class OpenApiExportCommand
{
    private const string Flag = "--export-openapi";

    public static bool IsExportMode(string[] args) => args.Contains(Flag);

    public static void Execute(WebApplication app, string[] args)
    {
        var outputPath = args.SkipWhile(a => a != Flag).Skip(1).FirstOrDefault()
            ?? "contracts/openapi/notrelix.v1.json";

        app.MapEndpoints();

        var swaggerProvider = app.Services
            .GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>();

        var document = swaggerProvider.GetSwagger("v1");

        var fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var writer = new StreamWriter(fullPath, false, new System.Text.UTF8Encoding(false));
        document.SerializeAsV3(new Microsoft.OpenApi.Writers.OpenApiJsonWriter(writer));

        Console.WriteLine($"OpenAPI spec exported to {fullPath}");
    }
}
