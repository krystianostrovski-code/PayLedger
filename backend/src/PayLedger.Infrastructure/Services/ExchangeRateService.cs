using PayLedger.Application.Interfaces;
using PayLedger.Domain.Exceptions;

namespace PayLedger.Infrastructure.Services;

public class ExchangeRateService : IExchangeRateService
{
    // Mid-market baseline FX rates quoted against PLN (PLN is base unit 1.0)
    private static readonly Dictionary<string, decimal> RatesToPln = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PLN"] = 1.0000m,
        ["EUR"] = 4.2850m,
        ["USD"] = 3.9620m,
        ["GBP"] = 5.1240m,
        ["CHF"] = 4.4510m
    };

    public decimal GetRate(string fromCurrency, string toCurrency)
    {
        var from = fromCurrency.Trim().ToUpperInvariant();
        var to = toCurrency.Trim().ToUpperInvariant();

        if (from == to) return 1.0m;

        if (!RatesToPln.TryGetValue(from, out var fromPln))
            throw new CurrencyMismatchException("Supported currencies: PLN, EUR, USD, GBP, CHF", from);

        if (!RatesToPln.TryGetValue(to, out var toPln))
            throw new CurrencyMismatchException("Supported currencies: PLN, EUR, USD, GBP, CHF", to);

        return Math.Round(fromPln / toPln, 4);
    }

    public decimal Convert(decimal amount, string fromCurrency, string toCurrency)
    {
        var rate = GetRate(fromCurrency, toCurrency);
        return Math.Round(amount * rate, 4);
    }

    public IReadOnlyDictionary<string, decimal> GetRatesAgainstPln() => RatesToPln;
}
