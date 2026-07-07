namespace Notrelix.Application.Common.Requests;

public interface IQuery<out TResponse> : IRequest<TResponse>;
