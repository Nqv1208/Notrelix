namespace Notrelix.Infrastructure.Options;

public sealed class DataProtectionOptionsValidator : IValidateOptions<DataProtectionOptions>
{
    private readonly string _environmentName;

    public DataProtectionOptionsValidator(string environmentName)
    {
        _environmentName = environmentName;
    }

    public DataProtectionOptionsValidator() : this("Production") { }

    public ValidateOptionsResult Validate(string? name, DataProtectionOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ApplicationName))
            failures.Add("DataProtection:ApplicationName is required.");

        if (options.PersistKeys && string.IsNullOrWhiteSpace(options.KeysPath))
            failures.Add("DataProtection:KeysPath is required when PersistKeys is true.");

        if (options.PersistKeys
            && !string.IsNullOrWhiteSpace(options.KeysPath)
            && !Path.IsPathRooted(options.KeysPath)
            && IsProduction())
        {
            failures.Add(
                "DataProtection:KeysPath must be an absolute path in production. " +
                $"Current value '{options.KeysPath}' is relative.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(string.Join(" ", failures))
            : ValidateOptionsResult.Success;
    }

    private bool IsProduction() =>
        string.Equals(_environmentName, "Production", StringComparison.OrdinalIgnoreCase);
}
