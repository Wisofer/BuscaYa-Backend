using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;

namespace BuscaYa.Services;

/// <summary>
/// Servicio en segundo plano enterprise que monitorea las tasas de cambio de Venezuela (BCV, Euro, Paralelo).
/// Utiliza persistencia en base de datos para garantizar idempotencia total, cero notificaciones duplicadas
/// por reinicio de servidor y segmentación estricta por el modo de tasa configurado por cada comerciante.
/// </summary>
public class VenezuelaRatePushBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<VenezuelaRatePushBackgroundService> _logger;

    public VenezuelaRatePushBackgroundService(
        IServiceProvider services,
        ILogger<VenezuelaRatePushBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Se ejecuta cada 15 minutos para monitorear actualizaciones oficiales
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));

        try
        {
            await CheckAndNotifyRateChangeAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en la verificación inicial de la tasa de Venezuela.");
        }

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await CheckAndNotifyRateChangeAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el ciclo de monitoreo de la tasa de Venezuela.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagando servicio
        }
    }

    private async Task CheckAndNotifyRateChangeAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var rateService = scope.ServiceProvider.GetRequiredService<IVenezuelaExchangeRateService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Solicitar la tasa fresca
        var rates = await rateService.GetLatestRatesAsync(forceRefresh: true, stoppingToken);

        if (rates == null || !rates.Success || rates.BcvRate <= 0m)
            return;

        // Fecha oficial en zona horaria de Venezuela (VET / UTC-4)
        var vetTime = DateTime.UtcNow.AddHours(-4);
        var todayStr = vetTime.ToString("yyyy-MM-dd");
        bool isWeekend = vetTime.DayOfWeek == DayOfWeek.Saturday || vetTime.DayOfWeek == DayOfWeek.Sunday;

        // Obtener la última tasa registrada históricamente en la base de datos
        var lastBcvLog = await GetLastLoggedRateRecordAsync(db, "bcv", stoppingToken);
        var lastEurLog = await GetLastLoggedRateRecordAsync(db, "eur_bcv", stoppingToken);
        var lastParaleloLog = await GetLastLoggedRateRecordAsync(db, "paralelo", stoppingToken);

        var prevBcv = lastBcvLog?.Rate ?? rates.BcvRate;
        var prevEur = lastEurLog?.Rate ?? rates.EurBcvRate;
        var prevParalelo = lastParaleloLog?.Rate ?? rates.ParaleloRate;

        // Comprobar si el VALOR de la tasa varió al menos Bs. 0.05
        // Nota Enterprise: Los fines de semana (Sáb/Dom) el BCV Oficial no publica nuevas tasas.
        bool bcvRateChanged = !isWeekend && (lastBcvLog == null || Math.Abs(rates.BcvRate - prevBcv) >= 0.05m);
        bool eurRateChanged = !isWeekend && (lastEurLog == null || Math.Abs(rates.EurBcvRate - prevEur) >= 0.05m);
        bool paraleloRateChanged = lastParaleloLog == null || Math.Abs(rates.ParaleloRate - prevParalelo) >= 0.05m;

        // Actualizar/Registrar en la base de datos de forma transparente para mantener el diario al día (SIEMPRE)
        await SaveOrUpdateRateLogAsync(db, todayStr, "bcv", rates.BcvRate, rates.Source, stoppingToken);
        await SaveOrUpdateRateLogAsync(db, todayStr, "eur_bcv", rates.EurBcvRate, rates.Source, stoppingToken);
        await SaveOrUpdateRateLogAsync(db, todayStr, "paralelo", rates.ParaleloRate, rates.Source, stoppingToken);
        await db.SaveChangesAsync(stoppingToken);

        // Si ninguna tasa cambió de VALOR numérico con respecto al historial, NO enviamos notificación Push
        if (!bcvRateChanged && !eurRateChanged && !paraleloRateChanged)
        {
            return;
        }

        // Horario Respetuoso de Notificación (7:00 AM a 8:30 PM hora de Venezuela / VET)
        // En horas nocturnas (ej: 10:00 PM), la tasa se guarda en la BD pero NO se envían push para no molestar al comerciante
        bool isNightTime = vetTime.Hour < 7 || vetTime.Hour >= 20 || (vetTime.Hour == 20 && vetTime.Minute > 30);
        if (isNightTime)
        {
            _logger.LogInformation("🌙 Registro de tasa guardado en BD. Notificación Push omitida por ser horario nocturno ({Hour}:{Minute} VET).", vetTime.Hour, vetTime.Minute);
            return;
        }

        // Buscar comerciantes activos de Venezuela
        var push = scope.ServiceProvider.GetRequiredService<BuscaYa.Services.IServices.IPushNotificationService>();
        var veTiendas = await db.Tiendas
            .AsNoTracking()
            .Include(b => b.Usuario)
            .ThenInclude(u => u.Dispositivos)
            .Where(b => b.Activo && b.CountryCode == "VE")
            .ToListAsync(stoppingToken);

        if (veTiendas.Count == 0) return;

        // Optimizacion Multicast Batch (Estándar Google/Apple):
        // En lugar de enviar peticiones individuales por usuario, agrupar todos los dispositivos por su modo de tasa
        var bcvDevices = new List<Device>();
        var eurDevices = new List<Device>();
        var paraleloDevices = new List<Device>();
        int totalNotifiedUsers = 0;

        foreach (var tienda in veTiendas)
        {
            string mode = (tienda.ExchangeRateMode ?? "manual").Trim().ToLowerInvariant();
            if (mode == "manual") continue;

            if (tienda.Usuario == null) continue;
            var userDevices = tienda.Usuario.Dispositivos.Where(d => !string.IsNullOrWhiteSpace(d.FcmToken)).ToList();
            if (userDevices.Count == 0) continue;

            if (mode == "bcv" && bcvRateChanged)
            {
                bcvDevices.AddRange(userDevices);
                totalNotifiedUsers++;
            }
            else if ((mode == "eur_bcv" || mode == "eur") && eurRateChanged)
            {
                eurDevices.AddRange(userDevices);
                totalNotifiedUsers++;
            }
            else if (mode == "paralelo" && paraleloRateChanged)
            {
                paraleloDevices.AddRange(userDevices);
                totalNotifiedUsers++;
            }
        }

        var typeTag = "venezuela_rate_change";
        var sendBatchId = Guid.NewGuid();

        // 1. Enviar lote masivo BCV Dólar
        if (bcvRateChanged && bcvDevices.Count > 0)
        {
            decimal diff = rates.BcvRate - prevBcv;
            var title = "🇻🇪 ¡Nueva Tasa Oficial BCV!";
            var body = diff > 0m
                ? $"La tasa oficial se ubicó en Bs. {FormatRateExact(rates.BcvRate)} (+Bs. {FormatRateExact(diff)}). Tus precios en Bolívares están actualizados. 📈"
                : (diff < 0m
                    ? $"La tasa oficial se ubicó en Bs. {FormatRateExact(rates.BcvRate)} (-Bs. {FormatRateExact(Math.Abs(diff))}). Tus precios en Bolívares están actualizados. 📉"
                    : $"La tasa oficial se ubicó en Bs. {FormatRateExact(rates.BcvRate)}. Tus precios en Bolívares están actualizados. 📈");

            
            var extraData = new Dictionary<string, string>
            {
                ["type"] = typeTag,
                ["mode"] = "bcv",
                ["rate"] = FormatRateExact(rates.BcvRate),
                ["diff"] = FormatRateExact(diff)
            };

            await push.SendPushNotificationAsync(null, bcvDevices, extraData, dataOnly: false);
        }

        // 2. Enviar lote masivo Euro BCV
        if (eurRateChanged && eurDevices.Count > 0)
        {
            decimal diff = rates.EurBcvRate - prevEur;
            var title = "🇪🇺 ¡Nueva Tasa Euro BCV!";
            var body = diff > 0m
                ? $"La tasa oficial del Euro se ubicó en Bs. {FormatRateExact(rates.EurBcvRate)} (+Bs. {FormatRateExact(diff)}). Tus precios en Bolívares están actualizados. 📈"
                : (diff < 0m
                    ? $"La tasa oficial del Euro se ubicó en Bs. {FormatRateExact(rates.EurBcvRate)} (-Bs. {FormatRateExact(Math.Abs(diff))}). Tus precios en Bolívares están actualizados. 📉"
                    : $"La tasa oficial del Euro se ubicó en Bs. {FormatRateExact(rates.EurBcvRate)}. Tus precios en Bolívares están actualizados. 📈");

            
            var extraData = new Dictionary<string, string>
            {
                ["type"] = typeTag,
                ["mode"] = "eur_bcv",
                ["rate"] = FormatRateExact(rates.EurBcvRate),
                ["diff"] = FormatRateExact(diff)
            };

            await push.SendPushNotificationAsync(null, eurDevices, extraData, dataOnly: false);
        }

        // 3. Enviar lote masivo Dólar Paralelo
        if (paraleloRateChanged && paraleloDevices.Count > 0)
        {
            decimal diff = rates.ParaleloRate - prevParalelo;
            var title = "📊 ¡Nueva Tasa Paralelo!";
            var body = diff > 0m
                ? $"El dólar paralelo subió a Bs. {FormatRateExact(rates.ParaleloRate)} (+Bs. {FormatRateExact(diff)}). Tus precios están actualizados. 📈"
                : (diff < 0m
                    ? $"El dólar paralelo bajó a Bs. {FormatRateExact(rates.ParaleloRate)} (-Bs. {FormatRateExact(Math.Abs(diff))}). Tus precios están actualizados. 📉"
                    : $"El dólar paralelo se ubicó en Bs. {FormatRateExact(rates.ParaleloRate)}. Tus precios están actualizados. 📈");

            
            var extraData = new Dictionary<string, string>
            {
                ["type"] = typeTag,
                ["mode"] = "paralelo",
                ["rate"] = FormatRateExact(rates.ParaleloRate),
                ["diff"] = FormatRateExact(diff)
            };

            await push.SendPushNotificationAsync(null, paraleloDevices, extraData, dataOnly: false);
        }

        int totalSentDevices = bcvDevices.Count + eurDevices.Count + paraleloDevices.Count;
        if (totalNotifiedUsers > 0)
        {
            _logger.LogInformation("✅ Notificación Multicast Batch enviada exitosamente a {UsersCount} comerciantes ({DevicesCount} dispositivos).",
                totalNotifiedUsers, totalSentDevices);
        }
    }

    private static string FormatRateExact(decimal val)
    {
        return val.ToString("0.####");
    }

    private static async Task<ExchangeRateLog?> GetLastLoggedRateRecordAsync(ApplicationDbContext db, string mode, CancellationToken ct)
    {
        return await db.ExchangeRateLogs
            .AsNoTracking()
            .Where(l => l.Mode == mode)
            .OrderByDescending(l => l.Id)
            .FirstOrDefaultAsync(ct);
    }

    private static async Task SaveOrUpdateRateLogAsync(ApplicationDbContext db, string fecha, string mode, decimal rate, string? source, CancellationToken ct)
    {
        var existing = await db.ExchangeRateLogs
            .FirstOrDefaultAsync(l => l.Fecha == fecha && l.Mode == mode, ct);

        if (existing != null)
        {
            existing.Rate = rate;
            existing.Source = source;
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            db.ExchangeRateLogs.Add(new ExchangeRateLog
            {
                Fecha = fecha,
                Mode = mode,
                Rate = rate,
                Source = source,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
