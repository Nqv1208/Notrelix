namespace Notrelix.API.Contracts.Governance.ShareLinks.Requests;

public record CreateShareLinkRequest(string Level, DateTime? ExpiresAt = null);
