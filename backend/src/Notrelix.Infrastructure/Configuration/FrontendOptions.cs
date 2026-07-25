namespace Notrelix.Infrastructure.Configuration;

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    public required Uri AppBaseUrl { get; init; }
}
