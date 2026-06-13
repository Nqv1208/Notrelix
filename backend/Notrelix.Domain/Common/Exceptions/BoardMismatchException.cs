namespace Notrelix.Domain.Common.Exceptions;

public class BoardMismatchException : DomainException
{
    public BoardMismatchException(string message) : base(message) { }
    
    public BoardMismatchException(Guid expectedBoardId, Guid actualBoardId) 
        : base($"Board scope mismatch: Expected board {expectedBoardId} but got {actualBoardId}.") { }
}
