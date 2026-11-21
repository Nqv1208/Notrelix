using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Security.Commands.SetAccountMfaPolicy;

namespace Notrelix.Application.Features.Accounts.Security;

/// <summary>
/// Whether the caller may change the Account's security policy: the action the set-MFA-policy endpoint requires
/// (ManageAccountSecurity). It is evaluated with the contract of <see cref="SetAccountMfaPolicyCommand"/> through the one
/// facts provider and policy engine, so the read flag can never disagree with the write.
/// </summary>
public interface IAccountSecurityManageAuthorizer
{
    Task<bool> CanManageAsync(CancellationToken cancellationToken);
}

public sealed class AccountSecurityManageAuthorizer(
    IRequestDescriptorRegistry descriptors,
    IAccessFactsProvider factsProvider,
    IAccessPolicyEvaluator policy,
    IExecutionContextReader executionContext) : IAccountSecurityManageAuthorizer
{
    public async Task<bool> CanManageAsync(CancellationToken cancellationToken)
    {
        var snapshot = executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Account security authorization requires the request execution context.");

        // Authorization descriptor only: the command is never dispatched here.
        var request = new SetAccountMfaPolicyCommand(false);
        var descriptor = descriptors.GetRequired(typeof(SetAccountMfaPolicyCommand));
        var facts = await factsProvider.ResolveAsync(descriptor, snapshot, request, cancellationToken);
        return policy.Evaluate(descriptor, snapshot, facts, request).Kind == AccessDecisionKind.Allowed;
    }
}
