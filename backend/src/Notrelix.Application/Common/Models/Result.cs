namespace Notrelix.Application.Common.Models;

public class Result
{
    internal Result(bool succeeded, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors.ToArray();
        TypedErrors = Array.Empty<ApplicationError>();
    }

    internal Result(bool succeeded, IEnumerable<ApplicationError> typedErrors)
    {
        Succeeded = succeeded;
        TypedErrors = typedErrors.ToArray();
        Errors = TypedErrors.Select(e => e.Message).ToArray();
    }

    public bool Succeeded { get; }
    public string[] Errors { get; }
    public IReadOnlyList<ApplicationError> TypedErrors { get; }

    public static Result Success() => new(true, Array.Empty<string>());
    public static Result Failure(IEnumerable<string> errors) => new(false, errors);
    public static Result Failure(string error) => new(false, new[] { error });
    public static Result Failure(IEnumerable<ApplicationError> errors) => new(false, errors);
    public static Result Failure(ApplicationError error) => new(false, new[] { error });
}

public class Result<T> : Result
{
    internal Result(bool succeeded, T? data, IEnumerable<string> errors)
        : base(succeeded, errors)
    {
        Data = data;
    }

    internal Result(bool succeeded, T? data, IEnumerable<ApplicationError> typedErrors)
        : base(succeeded, typedErrors)
    {
        Data = data;
    }

    public T? Data { get; }

    public static Result<T> Success(T data) => new(true, data, Array.Empty<string>());
    public new static Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors);
    public new static Result<T> Failure(string error) => new(false, default, new[] { error });
    public new static Result<T> Failure(IEnumerable<ApplicationError> errors) => new(false, default, errors);
    public new static Result<T> Failure(ApplicationError error) => new(false, default, new[] { error });
}

/// <summary>
/// A successful write on one versioned resource that tells the caller the version it now has, so a chain of
/// writes names the right one at each step. It is still a plain <see cref="Result"/> to every handler caller.
/// </summary>
public sealed class VersionedResult : Result
{
    private VersionedResult(long version) : base(true, Array.Empty<string>())
    {
        Version = version;
    }

    public long Version { get; }

    public static VersionedResult Success(long version) => new(version);
}
