using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Email;

namespace Notrelix.Infrastructure;

/// <summary>
/// Email delivery: SMTP provider when enabled, no-op otherwise.
/// </summary>
public static class EmailRegistration
{
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
}
