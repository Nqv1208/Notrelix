namespace Notrelix.Domain.Common.Exceptions;

/// <summary>
/// Exception khi có conflict (duplicate, concurrent modification)
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }

    public ConflictException(string entityName, string conflictField, string conflictValue)
        : base($"'{entityName}' đã tồn tại với {conflictField} = '{conflictValue}'.") { }
}
