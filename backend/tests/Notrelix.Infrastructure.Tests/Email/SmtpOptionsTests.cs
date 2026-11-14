using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Infrastructure.Email;

namespace Notrelix.Infrastructure.Tests.Email;

public sealed class SmtpOptionsTests
{
    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("[::1]")]
    public void Loopback_host_is_allowed_without_tls(string host) =>
        Assert.True(new SmtpOptions { Host = host, EnableSsl = false }.IsEncryptionSatisfied());

    [Theory]
    [InlineData("smtp.example.com")]
    [InlineData("10.0.0.5")]
    [InlineData("localhost.evil.com")]
    public void Remote_host_without_tls_is_rejected(string host) =>
        Assert.False(new SmtpOptions { Host = host, EnableSsl = false }.IsEncryptionSatisfied());

    [Fact]
    public void Remote_host_with_tls_is_accepted() =>
        Assert.True(new SmtpOptions { Host = "smtp.example.com", EnableSsl = true }.IsEncryptionSatisfied());

    [Fact]
    public void Startup_fails_for_enabled_remote_host_without_tls()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Smtp:Enabled"] = "true",
            ["Smtp:Host"] = "smtp.example.com",
            ["Smtp:FromEmail"] = "a@b.c",
            ["Smtp:EnableSsl"] = "false",
            ["Frontend:AppBaseUrl"] = "https://app.example.com",
        }).Build();
        var sp = new ServiceCollection().AddEmail(config).BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<SmtpOptions>>().Value);
        Assert.Contains("Smtp:EnableSsl", ex.Message);
    }
}
