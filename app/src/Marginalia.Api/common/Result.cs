namespace Marginalia.Api.Common;

public sealed record Error(string Message, string Code);

public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Ok() => new(true, null);
    public static Result Fail(string message, string code) => new(false, new Error(message, code));
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, Error? error) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, value, null);
    public new static Result<T> Fail(string message, string code) => new(false, default, new Error(message, code));
}
