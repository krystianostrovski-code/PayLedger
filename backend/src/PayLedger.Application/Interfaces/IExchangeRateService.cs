namespace PayLedger.Application.Interfaces;

public interface IExchangeRateService
{
    decimal GetRate(string fromCurrency, string toCurrency);
    decimal Convert(decimal amount, string fromCurrency, string toCurrency);
    IReadOnlyDictionary<string, decimal> GetRatesAgainstPln();
}
