namespace Notrelix.Domain.Common.Exceptions;

public class WorkspaceMismatchException : DomainException
{
    public WorkspaceMismatchException(string message) : base(message) { }
    
    public WorkspaceMismatchException(Guid expectedWorkspaceId, Guid actualWorkspaceId) 
        : base($"Workspace scope mismatch: Expected workspace {expectedWorkspaceId} but got {actualWorkspaceId}.") { }
}
