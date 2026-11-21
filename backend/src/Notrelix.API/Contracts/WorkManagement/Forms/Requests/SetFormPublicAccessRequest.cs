namespace Notrelix.API.Contracts.WorkManagement.Forms.Requests;

/// <summary>Switches a form public (a public link exists) or private (every link stops working).</summary>
public record SetFormPublicAccessRequest(bool Enabled);
