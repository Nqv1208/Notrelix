using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Resend;
using StackExchange.Redis;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Infrastructure.Caching;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Email;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Infrastructure.Jwt;
using Notrelix.Infrastructure.Otp;
using Notrelix.Infrastructure.RateLimit;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Services;
using Notrelix.Application.Common.Models;

namespace Microsoft.Extensions.DependencyInjection;
public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.Configure<SeedDataOptions>(configuration.GetSection("SeedData"));
        services.Configure<N8nOptions>(configuration.GetSection("N8n"));

        services.AddDatabaseContext(configuration);
        services.AddRedisCache(configuration);
        services.AddJwt(configuration);
        services.AddEmail(configuration);

        services.AddSingleton<IRedisCacheService, RedisCacheService>();
        services.AddSingleton<IOtpService, OtpService>();
        services.AddSingleton<IRateLimitService, RateLimitService>();
        services.AddSingleton<IJwtBlacklistService, JwtBlacklistService>();

        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ApplicationDbContextInitialiser>();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        
        // Register new services
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<INotificationService, RedisNotificationService>();
        services.AddSingleton<InMemoryJobQueue>();
        services.AddSingleton<IJobQueue>(provider => provider.GetRequiredService<InMemoryJobQueue>());
        services.AddSingleton<IBackgroundJobQueueReader>(provider => provider.GetRequiredService<InMemoryJobQueue>());
        services.AddScoped<N8nDispatchService>();
        services.AddHostedService<QueuedJobWorker>();
        services.AddHttpClient<IN8nClient, N8nClient>((_, client) =>
        {
            var baseUrl = configuration.GetSection("N8n")["InternalBaseUrl"] ?? "http://n8n:5678";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        
        // Register Interceptors
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventInterceptor>();
    }

    public static IServiceCollection AddDatabaseContext(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotrelixDb");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            // Add Interceptors
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>());

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
            options.InstanceName = "Notrelix_";
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

        var smtpOptions = configuration
            .GetSection(SmtpOptions.SectionName)
            .Get<SmtpOptions>() ?? new SmtpOptions();

        if (smtpOptions.Enabled)
        {
            services.AddTransient<IEmailService, SmtpEmailService>();
        }
        else
        {
            services.AddTransient<IEmailService, NoopEmailService>();
        }
        
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
                    ValidAudience = jwtSettings!.Audience,
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
