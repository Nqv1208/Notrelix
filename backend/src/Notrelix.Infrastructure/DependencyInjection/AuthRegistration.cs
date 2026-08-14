using Notrelix.Infrastructure.Auth.Cookies;
using Notrelix.Infrastructure.Auth.Jwt;
using Notrelix.Infrastructure.Auth.Passwords;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Infrastructure.Services;

namespace Notrelix.Infrastructure;

/// <summary>
/// Authentication: JWT bearer, cookies, password hashing, token blacklist and current-user context.
/// </summary>
public static class AuthRegistration
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtBlacklistService, JwtBlacklistService>();

        // Current-user / current-workspace / current-account context resolved from the HTTP request.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ICurrentWorkspace, CurrentWorkspace>();
        services.AddScoped<ICurrentAccount, CurrentAccount>();
        services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();
        services.AddScoped<ICurrentRequestContext, CurrentRequestContext>();

        // Correlation context for events/outbox/logs.
        services.AddScoped<ICorrelationContext, CurrentCorrelationContext>();

        // Post-commit action queue for cache invalidation and realtime dispatch.
        services.AddScoped<IPostCommitActionQueue, PostCommitActionQueue>();

        // Verification token locking (FOR UPDATE) — relational operation behind Application port.
        services.AddScoped<Notrelix.Application.Features.Identity.Verification.Abstractions.IActiveVerificationTokenLocker,
            ActiveVerificationTokenLocker>();

        services.AddJwtBearer(configuration);

        return services;
    }

    private static DateTimeOffset? ParseIssuedAt(string? value)
        => long.TryParse(value, out var epochSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(epochSeconds)
            : null;

    private static IServiceCollection AddJwtBearer(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                "JwtSettings section is missing in appsettings.json");

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("JwtSettings"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.SecretKey),
                "JwtSettings:SecretKey is required.")
            .Validate(o => o.SecretKey.Length >= 32,
                "JwtSettings:SecretKey must be at least 32 characters.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer),
                "JwtSettings:Issuer is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience),
                "JwtSettings:Audience is required.")
            .Validate(o => o.ExpireMinutes > 0,
                "JwtSettings:ExpireMinutes must be greater than zero.")
            .Validate(o => o.RefreshTokenExpireDays > 0,
                "JwtSettings:RefreshTokenExpireDays must be greater than zero.")
            .ValidateOnStart();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("SystemAdmin", policy =>
                policy.RequireRole("SystemAdmin"));

            options.AddPolicy("InternalService", policy =>
                policy.RequireRole("InternalService"));
        });
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidIssuer = jwtSettings.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        JwtKeyMaterial.DeriveKeyBytes(jwtSettings.SecretKey)),
                    NameClaimType = JwtRegisteredClaimNames.Sub
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var blacklist = context.HttpContext.RequestServices
                            .GetRequiredService<IJwtBlacklistService>();

                        var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                        if (jti is not null && await blacklist.IsBlacklistedAsync(jti))
                        {
                            context.Fail("Token has been revoked");
                            return;
                        }

                        var userIdClaim = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                        if (userIdClaim is not null && Guid.TryParse(userIdClaim, out var userId))
                        {
                            var revokedBefore = await blacklist.GetUserRevokedBeforeAsync(userId);
                            if (AccessTokenRevocationEvaluator.ShouldReject(
                                    ParseIssuedAt(context.Principal?.FindFirst(JwtRegisteredClaimNames.Iat)?.Value),
                                    revokedBefore))
                            {
                                context.Fail("User access has been revoked");
                            }
                        }
                    },
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Cookies["accessToken"];
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}
