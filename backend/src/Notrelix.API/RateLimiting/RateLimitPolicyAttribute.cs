namespace Notrelix.API.RateLimiting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RateLimitPolicyAttribute : Attribute
{
    public string PolicyName { get; }

    public RateLimitPolicyAttribute(string policyName)
    {
        PolicyName = policyName;
    }
}
