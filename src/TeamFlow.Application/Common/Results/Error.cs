namespace TeamFlow.Application.Common.Results;

/// <summary>Lightweight error description carried by <see cref="Result"/>.</summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    public static Error Validation(string message, string code = "validation") =>
        new(code, message, ErrorType.Validation);

    public static Error NotFound(string message, string code = "not_found") =>
        new(code, message, ErrorType.NotFound);

    public static Error Conflict(string message, string code = "conflict") =>
        new(code, message, ErrorType.Conflict);

    public static Error Forbidden(string message, string code = "forbidden") =>
        new(code, message, ErrorType.Forbidden);

    public static Error Unexpected(string message, string code = "unexpected") =>
        new(code, message, ErrorType.Unexpected);
}

public enum ErrorType
{
    None,
    Failure,
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unexpected,
}
