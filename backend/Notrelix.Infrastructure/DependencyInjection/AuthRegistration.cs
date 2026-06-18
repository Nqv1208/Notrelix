using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Auth.Cookies;
using Notrelix.Infrastructure.Auth.Jwt;
using Notrelix.Infrastructure.Auth.Passwords;
using Notrelix.Infrastructure.Identity.Services;

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

        // Current-user / current-workspace context resolved from the HTTP request.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ICurrentWorkspace, CurrentWorkspace>();

        services.AddJwtBearer(configuration);

        return services;
    }

    private static IServiceCollection AddJwtBearer(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                "JwtSettings section is missing in appsettings.json");

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services.AddAuthorization();
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
