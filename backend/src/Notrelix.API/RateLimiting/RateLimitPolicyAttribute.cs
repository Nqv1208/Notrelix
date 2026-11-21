namespace Notrelix.API.RateLimiting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
/// <remarks>An endpoint may declare several policies (for example per caller and per resource); each is enforced in order.</remarks>
public sealed class RateLimitPolicyAttribute : Attribute
{
    public string PolicyName { get; }

    public RateLimitPolicyAttribute(string policyName)
    {
        PolicyName = policyName;
    }
}
