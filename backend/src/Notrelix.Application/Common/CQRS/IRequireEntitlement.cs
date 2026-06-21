namespace Notrelix.Application.Common.CQRS;

public interface IRequireEntitlement
{
    FeatureCode Feature { get; }
    int Amount { get; }
}
