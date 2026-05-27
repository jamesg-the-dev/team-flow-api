namespace TeamFlow.Domain.SeedWork;

/// <summary>
/// Domain-level error. The Application layer translates these into transport errors.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message)
        : base(message) => Code = code;

    public static DomainException Invariant(string message) => new("domain.invariant", message);

    public static DomainException NotFound(string entity, object key) =>
        new("domain.not_found", $"{entity} '{key}' was not found.");
}
