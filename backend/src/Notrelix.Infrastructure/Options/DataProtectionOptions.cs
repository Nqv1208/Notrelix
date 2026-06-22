namespace Notrelix.Infrastructure.Options;

public sealed class DataProtectionOptions
{
    public string ApplicationName { get; init; } = "Notrelix";
    public bool PersistKeys { get; init; }
    public string KeysPath { get; init; } = "/root/.aspnet/DataProtection-Keys";
}
