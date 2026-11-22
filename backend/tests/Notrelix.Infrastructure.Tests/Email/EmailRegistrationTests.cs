using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Resend;
using Notrelix.Application.Common.Email;
using Notrelix.Infrastructure.Email;

namespace Notrelix.Infrastructure.Tests.Email;

/// <summary>
/// Builds the container the way the API and the worker do (both call AddInfrastructure, which calls AddEmail)
/// and resolves the sender, so a missing dependency of the chosen sender fails here and not in a worker batch.
/// </summary>
public sealed class EmailRegistrationTests
{
    private static IEmailService Resolve(Dictionary<string, string?> settings) =>
        Resolve(settings, out _);

    private static IEmailService Resolve(Dictionary<string, string?> settings, out string? resendToken)
    {
        settings["Frontend:AppBaseUrl"] = "https://x.test";
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(environment.Object)
            .AddEmail(config);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailService>();
        resendToken = sender is ResendEmailService
            ? scope.ServiceProvider.GetRequiredService<IOptions<ResendClientOptions>>().Value.ApiToken
            : null;
        return sender;
    }

    [Fact]
    public void Resend_enabled_resolves_ResendEmailService_with_its_client()
    {
        var sender = Resolve(new()
        {
            ["Email:Enabled"] = "true",
            ["Email:ApiKey"] = "test-key",
            ["Email:FromEmail"] = "a@b.test",
        }, out var token);

        Assert.IsType<ResendEmailService>(sender);
        Assert.Equal("test-key", token);
    }

    [Fact]
    public void Smtp_enabled_resolves_SmtpEmailService_even_when_Resend_is_also_enabled()
    {
        var sender = Resolve(new()
        {
            ["Smtp:Enabled"] = "true",
            ["Smtp:Host"] = "localhost",
            ["Smtp:FromEmail"] = "a@b.test",
            ["Email:Enabled"] = "true",
            ["Email:ApiKey"] = "test-key",
            ["Email:FromEmail"] = "a@b.test",
        });

        Assert.IsType<SmtpEmailService>(sender);
    }

    [Fact]
    public void Both_off_resolves_NoopEmailService() =>
        Assert.IsType<NoopEmailService>(Resolve(new()));
}
