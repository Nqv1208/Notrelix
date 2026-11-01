namespace Notrelix.Infrastructure.Operators;

/// <summary>
/// Settings of the platform operator console. Every secret comes from configuration (environment or secret
/// store), never from code, and none is ever logged. With no operator database login and no signing key the
/// console is switched off.
/// </summary>
public sealed class OperatorOptions
{
    public const string SectionName = "Operator";
    public const string AuthenticationScheme = "NotrelixOperator";
    public const string PolicyName = "PlatformOperator";
    public const string TokenUseClaim = "token_use";
    public const string TokenUseOperator = "operator";

    public OperatorJwtOptions Jwt { get; set; } = new();
    public OperatorSeedOptions Seed { get; set; } = new();

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Jwt.SigningKey);
}

public sealed class OperatorJwtOptions
{
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "notrelix-operator-console";
    public string Audience { get; set; } = "notrelix-operator-console";
    public int ExpireMinutes { get; set; } = 30;
}

/// <summary>The one operator account seeded in advance for the Notrelix company. The password is configuration, never code.</summary>
public sealed class OperatorSeedOptions
{
    public const int MinimumPasswordLength = 12;

    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "Notrelix operator";
    public string Password { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrEmpty(Password);

    public override string ToString() => $"OperatorSeedOptions {{ Email = {Email} }}";
}

public sealed class OperatorOptionsValidator : IValidateOptions<OperatorOptions>
{
    private readonly IConfiguration _configuration;

    public OperatorOptionsValidator(IConfiguration configuration) => _configuration = configuration;

    public ValidateOptionsResult Validate(string? name, OperatorOptions options)
    {
        var errors = new List<string>();
        var tenant = _configuration.GetSection("JwtSettings").Get<Auth.Jwt.JwtSettings>();

        if (options.IsConfigured)
        {
            if (options.Jwt.SigningKey.Length < 32)
                errors.Add("Operator:Jwt:SigningKey must be at least 32 characters.");
            if (tenant is not null && string.Equals(options.Jwt.SigningKey, tenant.SecretKey, StringComparison.Ordinal))
                errors.Add("Operator:Jwt:SigningKey must differ from JwtSettings:SecretKey: an operator token and a tenant token never share a key.");
            if (tenant is not null && string.Equals(options.Jwt.Audience, tenant.Audience, StringComparison.Ordinal))
                errors.Add("Operator:Jwt:Audience must differ from JwtSettings:Audience.");
            if (options.Jwt.ExpireMinutes is < 1 or > 120)
                errors.Add("Operator:Jwt:ExpireMinutes must be between 1 and 120.");
        }

        if (options.Seed.IsConfigured && options.Seed.Password.Length < OperatorSeedOptions.MinimumPasswordLength)
            errors.Add($"Operator:Seed:Password must be at least {OperatorSeedOptions.MinimumPasswordLength} characters.");

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
