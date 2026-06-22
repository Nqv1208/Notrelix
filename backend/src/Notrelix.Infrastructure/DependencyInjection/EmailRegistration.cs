using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Email;
using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure;

public static class EmailRegistration
{
    public static IServiceCollection AddEmail(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection("Smtp"))
            .Validate(
                o => !o.Enabled || !string.IsNullOrWhiteSpace(o.Host),
                "Smtp:Host is required when Smtp:Enabled is true.")
            .Validate(
                o => !o.Enabled || !string.IsNullOrWhiteSpace(o.FromEmail),
                "Smtp:FromEmail is required when Smtp:Enabled is true.")
            .Validate(
                o => !o.Enabled || o.Port is > 0 and <= 65535,
                "Smtp:Port must be between 1 and 65535 when Smtp:Enabled is true.")
            .ValidateOnStart();

        services
            .AddOptions<EmailOptions>()
            .Bind(configuration.GetSection("Email"))
            .Validate(
                o => !o.Enabled || !string.IsNullOrWhiteSpace(o.ApiKey),
                "Email:ApiKey is required when Email:Enabled is true.")
            .Validate(
                o => !o.Enabled || !string.IsNullOrWhiteSpace(o.FromEmail),
                "Email:FromEmail is required when Email:Enabled is true.")
            .ValidateOnStart();

        var smtpOptions = configuration
            .GetSection("Smtp")
            .Get<SmtpOptions>() ?? new SmtpOptions();

        var emailOptions = configuration
            .GetSection("Email")
            .Get<EmailOptions>() ?? new EmailOptions();

        if (smtpOptions.Enabled)
        {
            services.AddTransient<IEmailService, SmtpEmailService>();
        }
        else if (emailOptions.Enabled)
        {
            services.AddTransient<IEmailService, ResendEmailService>();
        }
        else
        {
            services.AddTransient<IEmailService, NoopEmailService>();
        }

        return services;
    }
}
