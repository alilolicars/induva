namespace Induva.Application.Common;

public enum ErrorKind
{
    None,
    Validation,
    NotFound,
    Forbidden,
    RateLimited,
    AiUnavailable
}

public class Result
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public ErrorKind Kind { get; init; }

    public static Result Ok() => new() { Succeeded = true };
    public static Result Fail(ErrorKind kind, string error) => new() { Succeeded = false, Kind = kind, Error = error };
}

public class Result<T> : Result
{
    public T? Value { get; init; }

    public static Result<T> Ok(T value) => new() { Succeeded = true, Value = value };
    public new static Result<T> Fail(ErrorKind kind, string error) => new() { Succeeded = false, Kind = kind, Error = error };
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}

