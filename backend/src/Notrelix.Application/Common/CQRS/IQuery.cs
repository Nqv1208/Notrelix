namespace Notrelix.Application.Common.CQRS;

public interface IQuery<out TResponse> : IRequest<TResponse>;
