using BuscaYa.Services.IServices;

namespace BuscaYa.Services;

/// <summary>Ejecuta cada hora la eliminación de cuentas cuya fecha programada ya venció.</summary>
public class AccountDeletionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AccountDeletionBackgroundService> _logger;

    public AccountDeletionBackgroundService(IServiceProvider services, ILogger<AccountDeletionBackgroundService> logger)
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
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en primera pasada de eliminación de cuentas");
        }

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en pasada de eliminación de cuentas");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // apagado
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var deletion = scope.ServiceProvider.GetRequiredService<IAccountDeletionService>();
        var result = await deletion.ProcessDueDeletionsAsync(stoppingToken);
        if (result.users_deleted > 0)
            _logger.LogInformation("Eliminación programada: {Count} cuenta(s) borrada(s).", result.users_deleted);
    }
}
