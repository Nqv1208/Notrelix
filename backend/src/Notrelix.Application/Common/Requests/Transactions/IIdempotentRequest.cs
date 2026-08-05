namespace Notrelix.Application.Common.Requests;

/// <summary>
/// Marker interface. Business commands carry no transport metadata — the raw
/// execution key lives in the scoped <c>IIdempotencyExecutionContext</c> and is
/// bound by the transport before dispatch.
/// </summary>
public interface IIdempotentRequest
{
}
