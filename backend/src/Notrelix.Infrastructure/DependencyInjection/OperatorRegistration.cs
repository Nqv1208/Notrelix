using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Operators.Abstractions;
using Notrelix.Application.Features.Operators.Public.Principal;
using Notrelix.Infrastructure.Auth.Jwt;
using Notrelix.Infrastructure.Operators;

namespace Notrelix.Infrastructure;

/// <summary>
/// The platform operator console's identity: its own bearer scheme (own key, issuer and audience, Authorization
/// header only, no cookie), its own policy, its token issuer and the operator-request context. The operator
/// database login is chosen per request by the API (see <c>DatabasePrincipal.UseOperator</c>).
/// </summary>
public static class OperatorRegistration
{
    public static IServiceCollection AddOperatorConsole(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OperatorOptions>()
            .Bind(configuration.GetSection(OperatorOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OperatorOptions>, OperatorOptionsValidator>();

        services.AddScoped<IOperatorTokenIssuer, OperatorTokenIssuer>();
        services.AddScoped<CurrentOperator>();
        services.AddScoped<ICurrentOperator>(sp => sp.GetRequiredService<CurrentOperator>());
        services.AddScoped<IOperatorAccountSummaryReader, OperatorAccountSummaryReader>();
        services.AddScoped<IOperatorsDbContext>(sp => sp.GetRequiredService<Data.ApplicationDbContext>());

        var options = configuration.GetSection(OperatorOptions.SectionName).Get<OperatorOptions>() ?? new OperatorOptions();
        if (!options.IsConfigured)
        {
            return services;
        }

        services.AddAuthentication()
            .AddJwtBearer(OperatorOptions.AuthenticationScheme, bearer =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(JwtKeyMaterial.DeriveKeyBytes(options.Jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                };
            });

        services.AddAuthorizationBuilder().AddPolicy(OperatorOptions.PolicyName, policy => policy
            .AddAuthenticationSchemes(OperatorOptions.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireClaim(OperatorOptions.TokenUseClaim, OperatorOptions.TokenUseOperator));

        return services;
    }
}
