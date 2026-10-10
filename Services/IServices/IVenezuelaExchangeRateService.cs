using System;
using System.Threading;
using System.Threading.Tasks;

namespace BuscaYa.Services.IServices;

public class VenezuelaRatesResponse
{
    public bool Success { get; set; }
    public decimal BcvRate { get; set; }
    public decimal EurBcvRate { get; set; }
    public decimal ParaleloRate { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "DolarApi";
    public string Currency { get; set; } = "VES";
}

public interface IVenezuelaExchangeRateService
{
    Task<VenezuelaRatesResponse> GetLatestRatesAsync(CancellationToken cancellationToken = default);
    Task<VenezuelaRatesResponse> GetLatestRatesAsync(bool forceRefresh, CancellationToken cancellationToken = default);
}
