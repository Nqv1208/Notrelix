using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Email;

internal sealed class EmailOutboxWriter : IEmailOutboxWriter
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EmailOutboxWriter(
        ApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task QueueRenderedEmailAsync(
        QueueRenderedEmailRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var message = EmailOutboxMessage.CreateRendered(
            request,
            _dateTimeProvider.UtcNow);

        _context.EmailOutboxMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task QueueTemplatedEmailAsync<TPayload>(
        QueueTemplatedEmailRequest<TPayload> request,
        CancellationToken cancellationToken)
        where TPayload : IEmailTemplatePayload
    {
        cancellationToken.ThrowIfCancellationRequested();
        var message = EmailOutboxMessage.CreateTemplated(
            request,
            _dateTimeProvider.UtcNow);

        _context.EmailOutboxMessages.Add(message);
        return Task.CompletedTask;
    }
}
