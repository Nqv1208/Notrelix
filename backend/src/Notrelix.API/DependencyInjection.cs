using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Notrelix.API.ErrorHandling;
using Notrelix.API.Middleware;
using Notrelix.API.OpenApi;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.API.Realtime;
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
        services.AddRealtimeGateway(configuration, environment);

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection("RateLimiting"))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RateLimitingOptions>, RateLimitingOptionsValidator>();
        services.AddSingleton<IRateLimitPolicyProvider, RateLimitPolicyProvider>();

        services.Configure<SecurityHeaderOptions>(
            configuration.GetSection(SecurityHeaderOptions.SectionName));

        services.Configure<OAuthRedirectOptions>(
            configuration.GetSection("OAuth"));

        services.AddApiCsrf(configuration);

        return services;
    }

    /// <summary>
    /// Browser CSRF enforcement (ADR-005). Options default to enforced and are
    /// validated on host start (see <see cref="CsrfOptionsValidator"/>).
    /// </summary>
    public static IServiceCollection AddApiCsrf(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CsrfOptions>()
            .Bind(configuration.GetSection("Security:Csrf"))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CsrfOptions>, CsrfOptionsValidator>();
        services.AddSingleton<CsrfProtector>();
        services.AddSingleton<ICsrfApplicabilityClassifier, CsrfApplicabilityClassifier>();

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
                            "X-CSRF-Token",
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

    /// <summary>
    /// OpenAPI schema id: the CLR full name (nested '+' as '.'). A closed generic
    /// such as <c>CursorPage&lt;MyWorkItemDto&gt;</c> becomes
    /// <c>Notrelix.Application.Common.Models.CursorPageOfMyWorkItemDto</c> instead of
    /// an assembly-qualified CLR name, so generated clients get a readable key.
    /// </summary>
    private static string SchemaIdFor(Type type)
    {
        if (!type.IsGenericType)
            return type.FullName!.Replace("+", ".", StringComparison.Ordinal);

        var definition = type.GetGenericTypeDefinition().FullName!;
        definition = definition[..definition.IndexOf('`', StringComparison.Ordinal)]
            .Replace("+", ".", StringComparison.Ordinal);
        var arguments = type.GetGenericArguments().Select(argument => SchemaIdFor(argument).Split('.')[^1]);
        return $"{definition}Of{string.Join("And", arguments)}";
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

            options.CustomSchemaIds(SchemaIdFor);

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

            // Browser sessions authenticate with the httpOnly access-token cookie that login sets
            // (the JWT bearer handler reads it when no Authorization header is present).
            options.AddSecurityDefinition("Cookie", new OpenApiSecurityScheme
            {
                Description = "httpOnly access-token cookie set by the sign-in operations. Browser requests also send the CSRF token header on unsafe methods.",
                Name = "accessToken",
                In = ParameterLocation.Cookie,
                Type = SecuritySchemeType.ApiKey,
            });

            // Per-operation security: Bearer or Cookie, applied only to non-anonymous operations
            options.OperationFilter<SecurityRequirementsOperationFilter>();

            // Idempotency contract: required header, 409/503 responses and the
            // replay header, only for endpoints marked with WithIdempotencyKey()
            options.OperationFilter<IdempotencyOperationFilter>();

            // Enums the HTTP pipeline serializes as names but the generator would export as integers.
            options.SchemaFilter<StringEnumSchemaFilter>();
            options.SchemaFilter<ProblemDetailsSchemaFilter>();

            // Endpoints declaring their canonical success code (201/204) must
            // not also export the default inferred 200 response.
            options.OperationFilter<CanonicalSuccessOperationFilter>();
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
