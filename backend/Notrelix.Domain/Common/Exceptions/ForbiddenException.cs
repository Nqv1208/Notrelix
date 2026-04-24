namespace Notrelix.Domain.Common.Exceptions;

/// <summary>
/// Exception khi không có quyền truy cập
/// </summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException() : base("Bạn không có quyền thực hiện hành động này.") { }
    public ForbiddenException(string message) : base(message) { }
    public ForbiddenException(string resource, string action)
        : base($"Không có quyền '{action}' trên resource '{resource}'.") { }
}
