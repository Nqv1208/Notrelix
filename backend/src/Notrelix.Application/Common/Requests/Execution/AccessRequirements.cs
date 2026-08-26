namespace Notrelix.Application.Common.Requests.Execution;

public sealed record AccessRequirements(
    bool RequiresPermission,
    bool RequiresVerifiedEmail,
    bool RequiresSubscription,
    bool RequiresFeature)
{
    public bool RequiresDatastoreFacts =>
        RequiresPermission || RequiresVerifiedEmail || RequiresSubscription || RequiresFeature;
}
