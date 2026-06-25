using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Notrelix.API.ErrorHandling;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
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
        services.AddApiCors(configuration);
        services.AddApiSwagger();
        services.AddApiRouting();
        services.AddApiForwardedHeaders(configuration, environment);

        services.Configure<RateLimitingOptions>(
            configuration.GetSection("RateLimiting:Policies"));
        services.AddSingleton<IRateLimitPolicyProvider, RateLimitPolicyProvider>();

        services.Configure<SecurityHeaderOptions>(
            configuration.GetSection(SecurityHeaderOptions.SectionName));

        return services;
    }

    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(ProblemDetailsOptionsSetup.Customize);
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

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
                            "X-Requested-With")
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

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    Array.Empty<string>()
                },
            });
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
