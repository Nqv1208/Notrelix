namespace Notrelix.Infrastructure.Options;

public class CacheKeyOptionsValidator : IValidateOptions<CacheKeyOptions>
{
    public ValidateOptionsResult Validate(string? name, CacheKeyOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Environment))
            return ValidateOptionsResult.Fail("CacheKeyOptions: Environment must not be empty.");

        if (options.Environment.Contains(':'))
            return ValidateOptionsResult.Fail("CacheKeyOptions: Environment must not contain ':'.");

        if (string.IsNullOrWhiteSpace(options.Prefix))
            return ValidateOptionsResult.Fail("CacheKeyOptions: Prefix must not be empty.");

        if (options.Prefix.Contains(':'))
            return ValidateOptionsResult.Fail("CacheKeyOptions: Prefix must not contain ':'.");

        if (options.SchemaVersion <= 0)
            return ValidateOptionsResult.Fail("CacheKeyOptions: SchemaVersion must be greater than 0.");

        return ValidateOptionsResult.Success;
    }
}
