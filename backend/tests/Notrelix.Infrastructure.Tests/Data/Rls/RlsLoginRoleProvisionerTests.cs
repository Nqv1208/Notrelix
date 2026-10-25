using Microsoft.Extensions.Configuration;

namespace Notrelix.Infrastructure.Tests.Data.Rls;

public class RlsLoginRoleProvisionerTests
{
    private const string App = "Host=db;Database=notrelix;Username=notrelix_app;Password=app-secret";
    private const string Worker = "Host=db;Database=notrelix;Username=notrelix_worker;Password=worker-secret";

    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void Credentials_AreTakenFromTheConnectionStringsThatServeTheLogins()
    {
        var credentials = RlsLoginRoleProvisioner.ResolveCredentials(
            Config(("ConnectionStrings:NotrelixDb", App), ("ConnectionStrings:NotrelixWorkerDb", Worker)));

        credentials.Should().BeEquivalentTo(
        [
            new LoginRoleCredential("notrelix_app", "app-secret"),
            new LoginRoleCredential("notrelix_worker", "worker-secret"),
        ]);
    }

    [Theory]
    [InlineData("ConnectionStrings:NotrelixDb", null)]
    [InlineData("ConnectionStrings:NotrelixWorkerDb", "")]
    [InlineData("ConnectionStrings:NotrelixWorkerDb", "Host=db;Username=notrelix_app;Password=x")]
    [InlineData("ConnectionStrings:NotrelixDb", "Host=db;Username=notrelix_app")]
    [InlineData("ConnectionStrings:NotrelixDb", "Host=db;Username=postgres;Password=x")]
    public void Credentials_AreRefused_WhenALoginIsMissingMisnamedOrHasNoPassword(string key, string? value)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotrelixDb"] = App,
            ["ConnectionStrings:NotrelixWorkerDb"] = Worker,
            [key] = value,
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var act = () => RlsLoginRoleProvisioner.ResolveCredentials(config);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ARefusalNeverEchoesAPassword()
    {
        var act = () => RlsLoginRoleProvisioner.ResolveCredentials(
            Config(("ConnectionStrings:NotrelixDb", "Host=db;Username=postgres;Password=top-secret-value"),
                   ("ConnectionStrings:NotrelixWorkerDb", Worker)));

        act.Should().Throw<InvalidOperationException>().Which.Message.Should().NotContain("top-secret-value");
        new LoginRoleCredential("notrelix_app", "top-secret-value").ToString().Should().NotContain("top-secret-value");
    }
}
