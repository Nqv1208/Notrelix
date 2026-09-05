using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Caching;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Features.Automation.Events;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Email;
using Notrelix.Application.Common.Entitlements;
using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.WorkManagement.Public.Commands;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Application.Features.Integrations.N8n.Providers;
using Notrelix.Application.Features.Integrations.Public.Commands;
using Notrelix.Infrastructure.CrossContext.Automation.WorkManagement;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Storage;
using Notrelix.API;
using Notrelix.Infrastructure.Billing;
using Notrelix.Infrastructure.Caching;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Email;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Realtime;
using Notrelix.Infrastructure.Storage.Providers;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Integration.Production;

/// <summary>
/// FZ-INF-06 — Real Production DI graph certification.
///
/// Boots the actual Notrelix.API <see cref="Program"/> with
/// ASPNETCORE_ENVIRONMENT=Production, real PostgreSQL + Redis containers,
/// ValidateOnBuild + ValidateScopes enabled, and proves that every production
/// capability resolves to a real (non-fake) implementation. Network calls are
/// never performed: services are resolved, not invoked.
///
/// Negative tests prove that missing production configuration fails fast at
/// host startup (never silently degrades to a dev fallback).
/// </summary>
[Collection("Cache")]
public sealed class ProductionGraphTests : IAsyncLifetime
{
    private readonly CacheTestContainer _fixture;

    public ProductionGraphTests(CacheTestContainer fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void Production_host_builds_and_every_capability_resolves_real_implementation()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.Should().NotBeNull();

        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        services.GetRequiredService<IRequestDataSession>().Should().BeOfType<EfRequestDataSession>();
        services.GetRequiredService<IIdempotencyStore>().Should().BeOfType<EfIdempotencyStore>();
        services.GetRequiredService<IRlsSessionContext>().Should().BeOfType<RlsSessionContext>();
        services.GetRequiredService<IRedisCacheService>().Should().BeOfType<RedisCacheService>();

        services.GetRequiredService<IRealtimePublisher>()
            .Should().BeOfType<RedisRealtimePublisher>()
            .And.NotBeOfType<DevNullRealtimePublisher>();
        services.GetRequiredService<IOutboxWakeSignal>().Should().NotBeNull();

        services.GetRequiredService<IMessageDeduplicationStore>().Should().BeOfType<MessageDeduplicationStore>();
        services.GetRequiredService<IEmailOutboxWriter>().Should().NotBeNull();
        services.GetRequiredService<IEmailService>().Should().BeOfType<SmtpEmailService>();
        services.GetRequiredService<IStorageService>().Should().BeOfType<LocalStorageProvider>();

        services.GetRequiredService<ISubscriptionChecker>().Should().BeOfType<DatabaseSubscriptionChecker>();
        services.GetRequiredService<IFeatureGateChecker>().Should().BeOfType<DatabaseFeatureGateChecker>();

        services.GetRequiredService<IAuditService>().Should().NotBeNull();

        // Producer-owned public semantic surfaces resolve in production DI.
        services.GetRequiredService<IIdentityUserFacts>().Should().NotBeNull();
        services.GetRequiredService<IAccountMembershipFacts>().Should().NotBeNull();
        services.GetRequiredService<IAccountMembershipActions>().Should().NotBeNull();
        services.GetRequiredService<IWorkItemActions>().Should().NotBeNull();
        services.GetRequiredService<IBillingCapabilityFacts>().Should().NotBeNull();

        // Single request-authorization authority: the pure policy engine.
        services.GetRequiredService<IAccessPolicyEvaluator>().Should().BeOfType<AccessPolicyEngine>();

        // Durable automation graph: evaluator + narrow network adapter are
        // registered for the outbox/broker consumer path.
        services.GetRequiredService<IN8nClient>().Should().NotBeNull();
        services.GetRequiredService<IN8nWebhookActions>().Should().NotBeNull();
        services.GetRequiredService<N8nAutomationRuleEvaluator>().Should().NotBeNull();
        services.GetRequiredService<N8nDispatchUseCase>().Should().NotBeNull();
        services.GetRequiredService<
            Notrelix.Application.Features.Automation.Ports.WorkManagement.IWorkActionPort>()
            .Should().BeOfType<WorkItemActionAdapter>();
    }

    [Fact]
    public async Task Production_host_fails_when_database_is_unreachable()
    {
        // Production forbids startup migration (explicit --migrate command mode),
        // so an unreachable database is surfaced at request time: the first
        // data-session query against it must fail.
        using var factory = CreateFactory(config =>
            config["ConnectionStrings:NotrelixDb"] =
                "Host=127.0.0.1;Port=9;Database=missing;Username=x;Password=y");
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();

        var session = scope.ServiceProvider.GetRequiredService<IRequestDataSession>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var act = async () => await session.ExecuteAsync(
            new RequestDataSessionOptions(RequestDataAccess.ReadOnly, false, false),
            _ => context.Database.ExecuteSqlRawAsync("SELECT 1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<Exception>(
            "the database is mandatory in production");
    }

    [Fact]
    public void Production_host_fails_fast_when_redis_connection_is_missing()
    {
        using var factory = CreateFactory(config =>
            config["ConnectionStrings:Redis"] = " ");
        var act = () => factory.CreateClient();
        act.Should().Throw<Exception>("the realtime cache is mandatory in production");
    }

    [Fact]
    public void Production_host_fails_fast_when_jwt_secret_is_too_short()
    {
        using var factory = CreateFactory(config =>
            config["JwtSettings:SecretKey"] = "short");
        var act = () => factory.CreateClient();
        act.Should().Throw<OptionsValidationException>(
            "JWT signing key must be at least 32 characters in production");
    }

    [Fact]
    public void Production_host_fails_fast_when_rls_is_disabled()
    {
        using var factory = CreateFactory(config =>
            config["Rls:Enabled"] = "false");
        var act = () => factory.CreateClient();
        act.Should().Throw<OptionsValidationException>("RLS is mandatory in production");
    }

    [Fact]
    public void Production_host_fails_fast_when_data_protection_persists_no_keys()
    {
        using var factory = CreateFactory(config =>
            config["DataProtection:PersistKeys"] = "false");
        var act = () => factory.CreateClient();
        act.Should().Throw<OptionsValidationException>(
            "production requires persisted data-protection keys");
    }

    [Fact]
    public void Production_host_fails_fast_when_messaging_transport_is_dev_null()
    {
        using var factory = CreateFactory(config =>
            config["Messaging:Transport"] = "DevNull");
        var act = () => factory.CreateClient();
        act.Should().Throw<Exception>("DevNull transport is not allowed in production");
    }

    [Fact]
    public void Production_host_fails_fast_when_billing_mode_is_dev_null()
    {
        using var factory = CreateFactory(config =>
            config["Billing:Mode"] = "DevNull");
        var act = () => factory.CreateClient();
        act.Should().Throw<Exception>("DevNull billing is not allowed in production");
    }

    private WebApplicationFactory<Program> CreateFactory(Action<Dictionary<string, string?>>? configure = null)
    {
        var keysPath = Path.Combine(
            Path.GetTempPath(), "notrelix-graph-test-keys", Guid.NewGuid().ToString("N"));

        var baseConfig = new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotrelixDb"] = _fixture.PostgresConnectionString,
            ["ConnectionStrings:Redis"] = _fixture.RedisConnectionString,
            ["JwtSettings:SecretKey"] = "production-graph-test-signing-key-ThisIsNotASecret-0123456789",
            ["JwtSettings:Issuer"] = "https://notrelix.test",
            ["JwtSettings:Audience"] = "notrelix-api",
            ["Cors:AllowedOrigins:0"] = "https://app.notrelix.test",
            ["Frontend:AppBaseUrl"] = "https://app.notrelix.test",
            ["Rls:Enabled"] = "true",
            ["Rls:SetSessionContext"] = "true",
            ["Smtp:Enabled"] = "true",
            ["Smtp:Host"] = "localhost",
            ["Smtp:Port"] = "587",
            ["Smtp:FromEmail"] = "graph@notrelix.test",
            ["DataProtection:PersistKeys"] = "true",
            ["DataProtection:KeysPath"] = keysPath,
            ["Messaging:Transport"] = "InMemory",
            ["Billing:Mode"] = "Database",
            ["Storage:Mode"] = "Local",
            ["ForwardedHeaders:Enabled"] = "true",
            ["ForwardedHeaders:KnownProxies:0"] = "127.0.0.1",
        };

        var overrides = new Dictionary<string, string?>();
        configure?.Invoke(overrides);

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");

                // Settings are visible to Program.Main before builder.Build(),
                // which AddInfrastructure relies on (registration-time reads).
                foreach (var (key, value) in baseConfig)
                {
                    if (!overrides.ContainsKey(key))
                    {
                        builder.UseSetting(key, value);
                    }
                }

                foreach (var (key, value) in overrides)
                {
                    if (value is null)
                    {
                        // Late overrides run at build time, so a null value
                        // cannot remove an earlier setting; flag removal
                        // explicitly by writing an unusable value.
                        throw new InvalidOperationException(
                            $"Negative override for '{key}' must provide a value, not null.");
                    }

                    builder.UseSetting(key, value);
                }

                builder.UseDefaultServiceProvider(options =>
                {
                    options.ValidateOnBuild = true;
                    options.ValidateScopes = true;
                });
            });
    }
}
