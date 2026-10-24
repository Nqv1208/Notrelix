using Notrelix.Application.Features.Identity.Registration.Ports;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Identity.Registration;

/// <summary>Answers through the owner-defined <c>ops.email_registered</c> function: a boolean, never a row.</summary>
public sealed class EmailRegistrationProbe : IEmailRegistrationProbe
{
    private readonly ApplicationDbContext _context;

    public EmailRegistrationProbe(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsRegisteredAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var rows = await _context.Database
            .SqlQuery<bool>($"""SELECT ops.email_registered({normalizedEmail}) AS "Value" """)
            .ToListAsync(cancellationToken);
        return rows.Count == 1 && rows[0];
    }
}
