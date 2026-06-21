namespace Notrelix.Application.Common.Exceptions;

// Exception khi không tìm thấy resource
public class NotFoundException : Exception
{
    public NotFoundException() : base("Resource not found") { }
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string name, object key) : base($"Entity \"{name}\" ({key}) was not found.") { }
}
