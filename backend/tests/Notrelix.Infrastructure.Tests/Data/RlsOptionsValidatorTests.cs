namespace Notrelix.Infrastructure.Tests.Data;

public class RlsOptionsValidatorTests
{
    private static RlsOptionsValidator CreateValidator(string environmentName)
    {
        var env = new Mock<IHostEnvironment>();
        env.Setup(x => x.EnvironmentName).Returns(environmentName);
        return new RlsOptionsValidator(env.Object);
    }

    [Fact]
    public void Validate_Production_RlsDisabled_Fails()
    {
        var validator = CreateValidator("Production");
        var options = new RlsOptions { Enabled = false, SetSessionContext = true, ApplyPoliciesOnStartup = false };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue("RLS must be enabled in Production");
        result.FailureMessage.Should().Contain("Rls:Enabled is false");
    }

    [Fact]
    public void Validate_Production_SetSessionContextDisabled_Fails()
    {
        var validator = CreateValidator("Production");
        var options = new RlsOptions { Enabled = true, SetSessionContext = false, ApplyPoliciesOnStartup = false };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue("SetSessionContext must be enabled in Production");
        result.FailureMessage.Should().Contain("Rls:SetSessionContext is false");
    }

    [Fact]
    public void Validate_Production_RlsFullyEnabled_Succeeds()
    {
        var validator = CreateValidator("Production");
        var options = new RlsOptions { Enabled = true, SetSessionContext = true, ApplyPoliciesOnStartup = false };

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeTrue("RLS fully enabled in Production should pass");
    }

    [Fact]
    public void Validate_Staging_RlsDisabled_Fails()
    {
        var validator = CreateValidator("Staging");
        var options = new RlsOptions { Enabled = false, SetSessionContext = true, ApplyPoliciesOnStartup = false };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue("RLS must be enabled in Staging");
    }

    [Fact]
    public void Validate_Staging_RlsFullyEnabled_Succeeds()
    {
        var validator = CreateValidator("Staging");
        var options = new RlsOptions { Enabled = true, SetSessionContext = true, ApplyPoliciesOnStartup = false };

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeTrue("RLS fully enabled in Staging should pass");
    }

    [Fact]
    public void Validate_Development_RlsDisabled_Warns()
    {
        var validator = CreateValidator("Development");
        var options = new RlsOptions { Enabled = false, SetSessionContext = false, ApplyPoliciesOnStartup = false };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue("RLS disabled in Development should produce warning");
        result.FailureMessage.Should().Contain("RLS is partially disabled");
    }

    [Fact]
    public void Validate_Development_RlsEnabled_Succeeds()
    {
        var validator = CreateValidator("Development");
        var options = new RlsOptions { Enabled = true, SetSessionContext = true, ApplyPoliciesOnStartup = false };

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeTrue("RLS enabled in Development should pass");
    }
}
