using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BuscaYa.Data;
using BuscaYa.Services.IServices;

namespace BuscaYa.Services;

/// <summary>
/// Servicio enterprise de Tasa de Cambio para Venezuela siguiendo estÃ¡ndares de alta disponibilidad (Google/Apple).
/// Implementa cachÃ© caliente de alto rendimiento, protecciÃ³n contra estampidas de cachÃ© (Single-Flight con SemaphoreSlim),
/// fallback resiliente a base de datos y precisiÃ³n decimal exacta sin redondeos.
/// </summary>
public class VenezuelaExchangeRateService : IVenezuelaExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VenezuelaExchangeRateService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    
    private static VenezuelaRatesResponse? _cachedRates;
    private static DateTime _cacheExpiresAt = DateTime.MinValue;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    public VenezuelaExchangeRateService(
        IHttpClientFactory httpClientFactory,
        ILogger<VenezuelaExchangeRateService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _httpClient = httpClientFactory.CreateClient("VenezuelaRateClient");
    }

    public Task<VenezuelaRatesResponse> GetLatestRatesAsync(CancellationToken cancellationToken = default)
        => GetLatestRatesAsync(forceRefresh: false, cancellationToken);

    public async Task<VenezuelaRatesResponse> GetLatestRatesAsync(bool forceRefresh, CancellationToken cancellationToken = default)
    {
        // 1. Si el cachÃ© estÃ¡ caliente y no se fuerza refresco, responder INMEDIATAMENTE (< 1ms)
        if (!forceRefresh && _cachedRates != null && DateTime.UtcNow < _cacheExpiresAt)
        {
            return _cachedRates;
        }

        // 2. ProtecciÃ³n contra Estampida de CachÃ© (Single-Flight Pattern como en Google/Stripe)
        // Si hay miles de peticiones simultÃ¡neas al expirar el cachÃ©, solo 1 refresca y las demÃ¡s reciben la tasa en memoria inmediatamente
        if (!forceRefresh && _cachedRates != null)
        {
            if (!_refreshLock.Wait(0))
            {
                // Otra peticiÃ³n ya estÃ¡ refrescando en segundo plano; devolver el cachÃ© existente sin hacer esperar al usuario
                return _cachedRates;
            }

            try
            {
                if (DateTime.UtcNow < _cacheExpiresAt)
                {
                    return _cachedRates;
                }

                return await FetchAndCacheFreshRatesAsync(cancellationToken);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        // Si se fuerza refresco o no hay ningÃºn cachÃ© inicial
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _cachedRates != null && DateTime.UtcNow < _cacheExpiresAt)
            {
                return _cachedRates;
            }

            return await FetchAndCacheFreshRatesAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<VenezuelaRatesResponse> FetchAndCacheFreshRatesAsync(CancellationToken cancellationToken)
    {
        var result = new VenezuelaRatesResponse();
        decimal bcvRate = 0m;
        decimal eurBcvRate = 0m;
        decimal paraleloRate = 0m;
        string source = "Desconocido";

        // Timeout estricto de 3.5 segundos en paralelo para evitar latencia
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(3.5));

        try
        {
            // EjecuciÃ³n PARALELA de consultas a proveedores externos
            var dolarVzlaTask = FetchDolarVzlaRateAsync(cts.Token);
            var bcvDolarApiTask = FetchDolarApiRateAsync("https://ve.dolarapi.com/v1/dolares/oficial", cts.Token);
            var eurDolarApiTask = FetchDolarApiRateAsync("https://ve.dolarapi.com/v1/euros/oficial", cts.Token);
            var paraleloDolarApiTask = FetchDolarApiRateAsync("https://ve.dolarapi.com/v1/dolares/paralelo", cts.Token);

            await Task.WhenAll(dolarVzlaTask, bcvDolarApiTask, eurDolarApiTask, paraleloDolarApiTask);

            var (dvBcv, dvEur) = dolarVzlaTask.Result;
            decimal apiBcv = bcvDolarApiTask.Result;
            decimal apiEur = eurDolarApiTask.Result;
            decimal apiParalelo = paraleloDolarApiTask.Result;

            // 1. Evaluar Tasa BCV (DolarVZLA preferencia, DolarApi respaldo)
            if (IsValidSanityRate(dvBcv))
            {
                bcvRate = dvBcv;
                source = "DolarVZLA (Oficial BCV)";
            }
            else if (IsValidSanityRate(apiBcv))
            {
                bcvRate = apiBcv;
                source = "DolarApi (Oficial BCV)";
            }

            // 2. Evaluar Tasa Euro BCV
            if (IsValidSanityRate(dvEur))
            {
                eurBcvRate = dvEur;
            }
            else if (IsValidSanityRate(apiEur))
            {
                eurBcvRate = apiEur;
            }

            // 3. Evaluar Tasa Paralelo
            if (IsValidSanityRate(apiParalelo))
            {
                paraleloRate = apiParalelo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Advertencia al consultar APIs externas de tasa de cambio...");
        }

        // Si obtuvimos la tasa BCV desde internet
        if (IsValidSanityRate(bcvRate))
        {
            result.Success = true;
            result.BcvRate = bcvRate;
            result.EurBcvRate = IsValidSanityRate(eurBcvRate) ? eurBcvRate : (await GetLastDbRateAsync("eur_bcv", cancellationToken) ?? bcvRate);
            result.ParaleloRate = IsValidSanityRate(paraleloRate) ? paraleloRate : (await GetLastDbRateAsync("paralelo", cancellationToken) ?? bcvRate);
            result.Source = source;
            result.LastUpdated = DateTime.UtcNow;

            _cachedRates = result;
            _cacheExpiresAt = DateTime.UtcNow.AddMinutes(1);
            return result;
        }

        // FALLBACK DINÃMICO: Buscar la Ãºltima tasa registrada en la Base de Datos
        var dbBcv = await GetLastDbRateAsync("bcv", cancellationToken);
        if (dbBcv.HasValue && IsValidSanityRate(dbBcv.Value))
        {
            var dbEur = await GetLastDbRateAsync("eur_bcv", cancellationToken);
            var dbParalelo = await GetLastDbRateAsync("paralelo", cancellationToken);

            result.Success = true;
            result.BcvRate = dbBcv.Value;
            result.EurBcvRate = (dbEur.HasValue && IsValidSanityRate(dbEur.Value)) ? dbEur.Value : dbBcv.Value;
            result.ParaleloRate = (dbParalelo.HasValue && IsValidSanityRate(dbParalelo.Value)) ? dbParalelo.Value : dbBcv.Value;
            result.Source = "Base de Datos (Ãšltimo Registro HistÃ³rico)";
            result.LastUpdated = DateTime.UtcNow;

            _cachedRates = result;
            _cacheExpiresAt = DateTime.UtcNow.AddMinutes(1);
            return result;
        }

        // Sin datos en internet ni en la base de datos
        result.Success = false;
        result.BcvRate = 0m;
        result.EurBcvRate = 0m;
        result.ParaleloRate = 0m;
        result.Source = "No disponible (Sin conexiÃ³n a APIs externas ni historial en BD)";

        return result;
    }

    private async Task<decimal?> GetLastDbRateAsync(string mode, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var record = await db.ExchangeRateLogs
                .AsNoTracking()
                .Where(x => x.Mode == mode)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(ct);

            return record?.Rate;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsValidSanityRate(decimal rate)
    {
        return rate > 1.0m && rate < 100000.0m;
    }

    private async Task<(decimal Usd, decimal Eur)> FetchDolarVzlaRateAsync(CancellationToken ct)
    {
        try
        {
            using var response = await _httpClient.GetAsync("https://rates.dolarvzla.com/bcv/current.json", ct);
            if (!response.IsSuccessStatusCode) return (0m, 0m);

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            decimal usd = 0m;
            decimal eur = 0m;

            if (root.TryGetProperty("current", out var currentElem))
            {
                if (currentElem.TryGetProperty("usd", out var usdElem) && usdElem.ValueKind == JsonValueKind.Number && usdElem.TryGetDecimal(out var usdVal) && IsValidSanityRate(usdVal))
                    usd = usdVal;

                if (currentElem.TryGetProperty("eur", out var eurElem) && eurElem.ValueKind == JsonValueKind.Number && eurElem.TryGetDecimal(out var eurVal) && IsValidSanityRate(eurVal))
                    eur = eurVal;
            }
            return (usd, eur);
        }
        catch
        {
            return (0m, 0m);
        }
    }

    private async Task<decimal> FetchDolarApiRateAsync(string url, CancellationToken ct)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return 0m;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("promedio", out var promElem))
            {
                if (promElem.ValueKind == JsonValueKind.Number && promElem.TryGetDecimal(out var dVal) && IsValidSanityRate(dVal))
                    return dVal;
                if (promElem.ValueKind == JsonValueKind.String && decimal.TryParse(promElem.GetString(), out var sVal) && IsValidSanityRate(sVal))
                    return sVal;
            }
        }
        catch
        {
            // Ignore single fetch failure
        }
        return 0m;
    }
}
