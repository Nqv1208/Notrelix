using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Notrelix.API.ErrorHandling;
using Notrelix.API.Middleware;
using Notrelix.API.OpenApi;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.Infrastructure.Auth.Csrf;
using Notrelix.Infrastructure.Observability.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApiLayer(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.Configure<OutboxHealthCheckOptions>(
            configuration.GetSection("HealthChecks:Outbox"));

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"])
            .AddCheck<OutboxHealthCheck>("outbox", tags: ["ready"]);

        services.AddApiProblemDetails();
        services.AddApiCors(configuration, environment);
        services.AddApiSwagger();
        services.AddApiRouting();
        services.AddApiForwardedHeaders(configuration, environment);

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.Configure<RateLimitingOptions>(
            configuration.GetSection("RateLimiting:Policies"));
        services.AddSingleton<IRateLimitPolicyProvider, RateLimitPolicyProvider>();

        services.Configure<SecurityHeaderOptions>(
            configuration.GetSection(SecurityHeaderOptions.SectionName));

        services.Configure<OAuthRedirectOptions>(
            configuration.GetSection("OAuth"));

        services.Configure<CsrfOptions>(
            configuration.GetSection("Security:Csrf"));
        services.AddSingleton<CsrfProtector>();

        return services;
    }

    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(ProblemDetailsOptionsSetup.Customize);
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (!environment.IsDevelopment())
        {
            if (allowedOrigins.Length == 0)
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins must be configured in non-Development environments.");
            if (allowedOrigins.Contains("*"))
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins wildcard '*' is not allowed in non-Development environments.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", builder =>
            {
                if (allowedOrigins.Length == 1 && allowedOrigins[0] == "*")
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    builder.WithOrigins(allowedOrigins)
                        .WithHeaders(
                            "Authorization",
                            "Content-Type",
                            "X-Correlation-Id",
                            "X-Workspace-Id",
                            "X-Requested-With",
                            "Idempotency-Key",
                            "If-Match")
                        .WithExposedHeaders(
                            "X-Correlation-Id",
                            "ETag",
                            "Location",
                            "Retry-After")
                        .WithMethods(
                            "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                        .AllowCredentials();
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Notrelix API",
                Version = "v1",
                Description = "Notrelix Enterprise Work Management API",
            });

            options.CustomSchemaIds(type =>
                type.FullName!.Replace("+", ".", StringComparison.Ordinal));

            // Include all endpoints in v1 document (endpoints without explicit GroupName)
            options.DocInclusionPredicate((docName, apiDesc) => docName == "v1");

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            });

            // Per-operation security: Bearer applied only to non-anonymous operations
            options.OperationFilter<SecurityRequirementsOperationFilter>();

            // Idempotency contract: required header, 409/503 responses and the
            // replay header, only for endpoints marked with WithIdempotencyKey()
            options.OperationFilter<IdempotencyOperationFilter>();
        });
        return services;
    }

    public static IServiceCollection AddApiRouting(this IServiceCollection services)
    {
        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });
        return services;
    }

    public static IServiceCollection AddApiForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var settings = configuration
            .GetSection("ForwardedHeaders")
            .Get<ForwardedHeadersSettings>() ?? new ForwardedHeadersSettings();

        if (!environment.IsDevelopment()
            && settings.RequireKnownProxyInProduction
            && settings.KnownProxies.Count == 0
            && settings.KnownNetworks.Count == 0)
        {
            throw new InvalidOperationException(
                "ForwardedHeaders: either KnownProxies or KnownNetworks must be configured " +
                "in non-Development environments when RequireKnownProxyInProduction is true. " +
                "Set KnownProxies/KnownNetworks to the proxy IPs/networks or set " +
                "ForwardedHeaders:RequireKnownProxyInProduction to false if not behind a proxy.");
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;

            if (environment.IsDevelopment() && settings.TrustAllInDevelopment)
            {
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            }
            else
            {
                options.ForwardLimit = settings.ForwardLimit;

                foreach (var proxy in settings.KnownProxies)
                {
                    if (System.Net.IPAddress.TryParse(proxy, out var address))
                        options.KnownProxies.Add(address);
                }

                foreach (var network in settings.KnownNetworks)
                {
                    var parts = network.Split('/');
                    if (parts.Length == 2
                        && System.Net.IPAddress.TryParse(parts[0], out var prefix)
                        && int.TryParse(parts[1], out var prefixLength))
                    {
                        options.KnownNetworks.Add(
                            new AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
                    }
                }
            }
        });

        return services;
    }
}
