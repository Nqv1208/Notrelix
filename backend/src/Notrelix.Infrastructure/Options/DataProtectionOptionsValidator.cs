using Microsoft.Extensions.Options;

namespace Notrelix.Infrastructure.Options;

public sealed class DataProtectionOptionsValidator : IValidateOptions<DataProtectionOptions>
{
    public ValidateOptionsResult Validate(string? name, DataProtectionOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ApplicationName))
            failures.Add("DataProtection:ApplicationName is required.");

        if (options.PersistKeys && string.IsNullOrWhiteSpace(options.KeysPath))
            failures.Add("DataProtection:KeysPath is required when PersistKeys is true.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(string.Join(" ", failures))
            : ValidateOptionsResult.Success;
    }
}
