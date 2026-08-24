using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Security;

public interface IAccessFactsProvider
{
    Task<AccessFacts> ResolveAsync(
        RequestDescriptor descriptor,
        ExecutionContextSnapshot context,
        object request,
        CancellationToken cancellationToken);
}
