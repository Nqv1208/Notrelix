namespace Notrelix.Infrastructure.Data.Rls;

public class RlsOptionsValidator : IValidateOptions<RlsOptions>
{
    private readonly IHostEnvironment _environment;

    public RlsOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, RlsOptions options)
    {
        if (_environment.IsDevelopment())
        {
            if (!options.Enabled || !options.SetSessionContext)
            {
                return ValidateOptionsResult.Fail(
                    $"RLS is partially disabled in {_environment.EnvironmentName}. " +
                    "Set Rls:Enabled and Rls:SetSessionContext to true for tenant isolation. " +
                    "Disable only if explicitly documented and acceptance-tested.");
            }

            return ValidateOptionsResult.Success;
        }

        // Staging / Production: RLS must be fully enabled
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Fail(
                $"FATAL: Rls:Enabled is false in {_environment.EnvironmentName}. " +
                "Tenant isolation is required in non-development environments. " +
                "Set Rls:Enabled to true and ensure RLS policies are applied.");
        }

        if (!options.SetSessionContext)
        {
            return ValidateOptionsResult.Fail(
                $"FATAL: Rls:SetSessionContext is false in {_environment.EnvironmentName}. " +
                "Session context must be set for tenant-scoped queries. " +
                "Set Rls:SetSessionContext to true.");
        }

        return ValidateOptionsResult.Success;
    }
}
