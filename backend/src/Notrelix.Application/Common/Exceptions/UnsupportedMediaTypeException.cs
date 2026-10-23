namespace Notrelix.Application.Common.Exceptions;

/// <summary>
/// The content is of a kind the operation refuses (for example an executable uploaded as an
/// attachment). The decision is derived server-side from the bytes and the file name, never
/// from the client-declared content type. Public status: 415.
/// </summary>
public sealed class UnsupportedMediaTypeException : Exception
{
    public UnsupportedMediaTypeException(string message) : base(message) { }
}
