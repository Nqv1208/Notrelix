using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Security;

public interface IAccessPolicyEvaluator
{
    AccessDecision Evaluate(
        RequestDescriptor descriptor,
        ExecutionContextSnapshot context,
        AccessFacts facts,
        object request);
}
