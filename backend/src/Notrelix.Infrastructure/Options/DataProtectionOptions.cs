namespace Notrelix.Infrastructure.Options;

public sealed class DataProtectionOptions
{
    public string ApplicationName { get; init; } = "Notrelix";
    public bool PersistKeys { get; init; }
    public string KeysPath { get; init; } = "App_Data/DataProtection-Keys";
}
