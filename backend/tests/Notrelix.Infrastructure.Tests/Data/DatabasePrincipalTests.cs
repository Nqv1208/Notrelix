using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Tests.Data;

public class DatabasePrincipalTests
{
    private const string App = "Host=db;Database=notrelix;Username=app;Password=a";
    private const string Worker = "Host=db;Database=notrelix;Username=worker;Password=w";

    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void WithoutAWorkerString_TheWorkerFallsBackToTheApplicationString()
    {
        var strings = DatabaseConnectionStrings.FromConfiguration(Config(("ConnectionStrings:NotrelixDb", App)));

        strings.Worker.Should().Be(App);
        strings.WorkerUsesApplicationFallback.Should().BeTrue("single-login hosts keep working");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ABlankWorkerString_FallsBackToo(string blank)
    {
        var strings = DatabaseConnectionStrings.FromConfiguration(
            Config(("ConnectionStrings:NotrelixDb", App), ("ConnectionStrings:NotrelixWorkerDb", blank)));

        strings.Worker.Should().Be(App);
        strings.WorkerUsesApplicationFallback.Should().BeTrue();
    }

    [Fact]
    public void AWorkerString_IsUsedForTheWorkerAndNeverForTheApplication()
    {
        var strings = DatabaseConnectionStrings.FromConfiguration(
            Config(("ConnectionStrings:NotrelixDb", App), ("ConnectionStrings:NotrelixWorkerDb", Worker)));

        strings.Application.Should().Be(App);
        strings.Worker.Should().Be(Worker);
        strings.WorkerUsesApplicationFallback.Should().BeFalse();
    }

    [Fact]
    public void ARequestScope_IsTheApplicationPrincipal_ByDefault()
    {
        var principal = new DatabasePrincipal(new DatabaseConnectionStrings(App, Worker, false));

        principal.Kind.Should().Be(DatabasePrincipalKind.Application);
        principal.IsWorker.Should().BeFalse();
    }

    [Fact]
    public void AWorkerScope_ResolvesTheWorkerString_AndARequestScopeTheApplicationString()
    {
        var strings = new DatabaseConnectionStrings(App, Worker, false);
        var worker = new DatabasePrincipal(strings);
        worker.UseWorker();
        var request = new DatabasePrincipal(strings);

        worker.IsWorker.Should().BeTrue();
        worker.ResolveConnectionString().Should().Be(Worker);
        request.ResolveConnectionString().Should().Be(App);
    }

    [Fact]
    public void SwitchingToTheWorker_AfterTheConnectionWasResolvedAsTheApplication_Throws()
    {
        var principal = new DatabasePrincipal(new DatabaseConnectionStrings(App, Worker, false));
        principal.ResolveConnectionString();

        var act = () => principal.UseWorker();

        act.Should().Throw<InvalidOperationException>();
        principal.IsWorker.Should().BeFalse("a failed switch changes nothing");
    }

    [Fact]
    public void SwitchingToTheWorkerTwice_IsAllowed_BeforeAndAfterResolution()
    {
        var principal = new DatabasePrincipal(new DatabaseConnectionStrings(App, Worker, false));
        principal.UseWorker();
        principal.ResolveConnectionString();

        principal.Invoking(p => p.UseWorker()).Should().NotThrow("the scope is already the worker");
    }

    [Fact]
    public async Task CreateWorkerAsyncScope_SwitchesTheScopeBeforeAnyUse()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new DatabaseConnectionStrings(App, Worker, false));
        services.AddScoped<DatabasePrincipal>();
        await using var provider = services.BuildServiceProvider();

        await using var worker = provider.GetRequiredService<IServiceScopeFactory>().CreateWorkerAsyncScope();
        await using var request = provider.CreateAsyncScope();

        worker.ServiceProvider.GetRequiredService<DatabasePrincipal>().IsWorker.Should().BeTrue();
        request.ServiceProvider.GetRequiredService<DatabasePrincipal>().IsWorker.Should().BeFalse();
    }
}
