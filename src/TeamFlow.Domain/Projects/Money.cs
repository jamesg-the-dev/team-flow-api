using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Projects;

/// <summary>
/// Money value object stored as bigint cents + ISO-4217 currency code.
/// Equality is by amount + currency.
/// </summary>
public sealed class Money : ValueObject
{
    public long AmountCents { get; }
    public string Currency { get; }

    private Money(long amountCents, string currency)
    {
        AmountCents = amountCents;
        Currency = currency;
    }

    public static Money From(long amountCents, string currency)
    {
        if (amountCents < 0)
            throw DomainException.Invariant("Amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw DomainException.Invariant("Currency must be a 3-letter ISO code.");
        return new Money(amountCents, currency.ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AmountCents;
        yield return Currency;
    }

    public override string ToString() => $"{AmountCents / 100m:F2} {Currency}";
}
