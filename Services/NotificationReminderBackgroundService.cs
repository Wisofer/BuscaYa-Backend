using BuscaYa.Services.IServices;

namespace BuscaYa.Services;

/// <summary>Envía recordatorio diario de favoritos en oferta (una vez por día UTC).</summary>
public class NotificationReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationReminderBackgroundService> _logger;

    public NotificationReminderBackgroundService(
        IServiceProvider services,
        ILogger<NotificationReminderBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        try
        {
            await RunOnceAsync(stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Error en primera pasada de recordatorios de favoritos.");
        }

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error en pasada de recordatorios de favoritos.");
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        // Ventana UTC recomendada para no enviar de madrugada en LATAM.
        var utcHour = DateTime.UtcNow.Hour;
        if (utcHour < 14 || utcHour > 18) return;

        using var scope = _services.CreateScope();
        var trigger = scope.ServiceProvider.GetRequiredService<INotificationTriggerService>();
        await trigger.SendDailyFavoritesReminderAsync(cancellationToken);
    }
}
