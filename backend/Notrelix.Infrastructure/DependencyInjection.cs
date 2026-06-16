using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Resend;
using StackExchange.Redis;
using Notrelix.Application.Common.Abstractions;
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
using Microsoft.Extensions.DependencyInjection;

namespace Notrelix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Placeholders for future tasks
        // services.AddPersistence(configuration);
        // services.AddCache(configuration);
        // services.AddAuth(configuration);
        // services.AddStorage(configuration);
        // services.AddEmail(configuration);
        // services.AddRealtime(configuration);
        // services.AddBackgroundJobs(configuration);
        
        return services;
    }

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
        services.AddScoped<ICurrentWorkspace, CurrentWorkspace>();
        
        // Register new services
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<INotificationService, RedisNotificationService>();
        services.AddSingleton<InMemoryJobQueue>();
        services.AddSingleton<IJobQueue>(provider => provider.GetRequiredService<InMemoryJobQueue>());
        services.AddSingleton<IBackgroundJobQueueReader>(provider => provider.GetRequiredService<InMemoryJobQueue>());
        services.AddScoped<N8nDispatchService>();
        services.AddHostedService<QueuedJobWorker>();
        services.AddHostedService<OutboxDispatcher>();
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
