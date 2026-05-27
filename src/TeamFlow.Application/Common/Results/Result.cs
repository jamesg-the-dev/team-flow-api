namespace TeamFlow.Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error.Type != ErrorType.None)
            throw new InvalidOperationException("Successful result cannot have an error.");
        if (!isSuccess && error.Type == ErrorType.None)
            throw new InvalidOperationException("Failure result must carry an error.");
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);

    public static Result<T> Failure<T>(Error error) => new(default!, false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T _value;
    public T Value =>
        IsSuccess
            ? _value
            : throw new InvalidOperationException("Cannot access Value on a failed result.");

    internal Result(T value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
