namespace Notrelix.Application.Common.CQRS;

public interface IIdempotentRequest
{
    string IdempotencyKey { get; }
}
