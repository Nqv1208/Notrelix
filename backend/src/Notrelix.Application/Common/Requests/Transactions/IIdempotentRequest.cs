namespace Notrelix.Application.Common.Requests;

public interface IIdempotentRequest
{
    string IdempotencyKey { get; }
}
