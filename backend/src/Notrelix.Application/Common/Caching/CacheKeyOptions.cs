namespace Notrelix.Application.Common.Caching;

public sealed class CacheKeyOptions
{
    public string Environment { get; set; } = string.Empty;
    public int SchemaVersion { get; set; } = 1;
    public string Prefix { get; set; } = "notrelix";
}
