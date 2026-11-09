namespace Notrelix.Application.Features.Automation.Rules.Services;

/// <summary>
/// A target a rule names that no longer exists or is no longer reachable (archived or deleted), found by
/// the enable check. <paramref name="Kind"/> is the target vocabulary (board, group, option, field,
/// member); <paramref name="Field"/> is where the rule names it (for example <c>actions[1]</c>).
/// </summary>
public sealed record AutomationBrokenReference(string Kind, Guid Id, string Field);
