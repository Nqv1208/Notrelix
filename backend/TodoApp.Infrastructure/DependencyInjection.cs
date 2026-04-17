using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Resend;
using StackExchange.Redis;
using TodoApp.Application.Common.Interfaces;
using TodoApp.Infrastructure.Caching;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Email;
using TodoApp.Infrastructure.Identity.Services;
using TodoApp.Infrastructure.Jwt;
using TodoApp.Infrastructure.Otp;
using TodoApp.Infrastructure.RateLimit;

namespace Microsoft.Extensions.DependencyInjection;
public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddDatabaseContext(configuration);
        services.AddRedisCache(configuration);
        services.AddJwt(configuration);
        services.AddEmail(configuration);
        // services.AddEmailService(configuration);

        services.AddSingleton<IRedisCacheService, RedisCacheService>();
        services.AddSingleton<IOtpService, OtpService>();
        services.AddSingleton<IRateLimitService, RateLimitService>();
        services.AddSingleton<IJwtBlacklistService, JwtBlacklistService>();

        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ApplicationDbContextInitialiser>();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
    }

    public static IServiceCollection AddDatabaseContext(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TodoAppDb");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
        });

        return services;
    }

    public static IServiceCollection AddRedisCache(
        this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is missing");

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "TodoApp_";
        });

        return services;
    }

    public static IServiceCollection AddEmail(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection("Smtp"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddTransient<IEmailService, SmtpEmailService>();
        
        return services;
    }

    // public static IServiceCollection AddEmailService(
    //     this IServiceCollection services, IConfiguration configuration)
    // {
    //     services.Configure<EmailSettings>(configuration.GetSection("Email"));

    //     services.AddOptions();
    //     services.AddHttpClient<ResendClient>();
    //     services.Configure<ResendClientOptions>(o =>
    //     {
    //         o.ApiToken = configuration.GetSection("Email")["ApiKey"] ?? "";
    //     });
    //     services.AddTransient<IResend, ResendClient>();
    //     services.AddTransient<IEmailService, ResendEmailService>();

    //     return services;
    // }

    public static IServiceCollection AddJwt(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

        if (jwtSettings == null)
        {
            throw new InvalidOperationException(
                "JwtSettings section is missing in appsettings.json");
        }

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

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
                    ValidAudience = jwtSettings!.Audience,
                    ValidIssuer = jwtSettings.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
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
                    }
                };
            });

        return services;
    }
}
