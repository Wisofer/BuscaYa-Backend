using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Services;

public class NotificationTriggerService : INotificationTriggerService
{
    private const double RadiusKm = 5;
    private const int MaxNotificationsPerUserPerDay = 3;
    private const string TypeNewStoreNearby = "NEW_STORE_NEARBY";
    private const string TypePriceDrop = "PRICE_DROP";
    private const string TypeBackInStock = "BACK_IN_STOCK";

    private readonly ApplicationDbContext _context;
    private readonly IPushNotificationService _push;
    private readonly ILogger<NotificationTriggerService> _logger;

    public NotificationTriggerService(
        ApplicationDbContext context,
        IPushNotificationService push,
        ILogger<NotificationTriggerService> logger)
    {
        _context = context;
        _push = push;
        _logger = logger;
    }

    public async Task NotifyNewStoreNearbyAsync(int tiendaId)
    {
        var tienda = await _context.Tiendas.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tiendaId);
        if (tienda == null) return;

        var direcciones = await _context.DireccionesGuardadas
            .AsNoTracking()
            .Where(d => d.UsuarioId != tienda.UsuarioId)
            .ToListAsync();

        var userIdsCercanos = direcciones
            .Where(d => GeolocationHelper.CalcularDistanciaKm(d.Latitud, d.Longitud, tienda.Latitud, tienda.Longitud) <= RadiusKm)
            .Select(d => d.UsuarioId)
            .Distinct()
            .ToList();

        if (userIdsCercanos.Count == 0) return;

        var since = DateTime.UtcNow.AddHours(-24);
        var logsRecientes = await _context.NotificationLogs
            .AsNoTracking()
            .Where(l => userIdsCercanos.Contains(l.UsuarioId) && l.SentAt >= since)
            .Select(l => new { l.UsuarioId, l.NotificationType, l.EntityId })
            .ToListAsync();

        var yaEnviadoMismaTienda = logsRecientes
            .Where(l => l.NotificationType == TypeNewStoreNearby && l.EntityId == tiendaId)
            .Select(l => l.UsuarioId)
            .ToHashSet();
        var conteoPorUsuario = logsRecientes.GroupBy(l => l.UsuarioId).ToDictionary(g => g.Key, g => g.Count());
        var userIdsFinal = userIdsCercanos
            .Where(id => !yaEnviadoMismaTienda.Contains(id) && conteoPorUsuario.GetValueOrDefault(id, 0) < MaxNotificationsPerUserPerDay)
            .ToList();

        if (userIdsFinal.Count == 0) return;

        var devices = await _context.Devices
            .Where(d => userIdsFinal.Contains(d.UsuarioId) && !string.IsNullOrWhiteSpace(d.FcmToken))
            .ToListAsync();

        if (devices.Count == 0) return;

        var title = "Nueva tienda cerca de ti";
        var body = $"Nueva tienda cerca: {tienda.Nombre}";
        var extraData = new Dictionary<string, string>
        {
            ["type"] = TypeNewStoreNearby,
            ["storeId"] = tiendaId.ToString(),
            ["storeName"] = tienda.Nombre
        };
        await _push.SendPushNotificationAsync(devices, title, body, extraData, TypeNewStoreNearby, tiendaId);
        _logger.LogInformation("Notificación NEW_STORE_NEARBY enviada a {Count} dispositivos por tienda {TiendaId}", devices.Count, tiendaId);
    }

    public async Task NotifyPriceDropAsync(int productoId, decimal precioAnterior, decimal precioNuevo)
    {
        var producto = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Tienda)
            .FirstOrDefaultAsync(p => p.Id == productoId);
        if (producto == null) return;

        var userIds = await _context.Favoritos
            .Where(f => f.ProductoId == productoId)
            .Select(f => f.UsuarioId)
            .Distinct()
            .ToListAsync();
        if (userIds.Count == 0) return;

        var filtered = await FilterByAntiSpamAsync(userIds, TypePriceDrop, productoId);
        if (filtered.Count == 0) return;

        var devices = await _context.Devices
            .Where(d => filtered.Contains(d.UsuarioId) && !string.IsNullOrWhiteSpace(d.FcmToken))
            .ToListAsync();
        if (devices.Count == 0) return;

        var title = "Bajó de precio";
        var body = $"{producto.Nombre} ahora {producto.Moneda}{precioNuevo:N0}";
        var extraData = new Dictionary<string, string>
        {
            ["type"] = TypePriceDrop,
            ["productId"] = productoId.ToString(),
            ["storeId"] = producto.TiendaId.ToString(),
            ["productName"] = producto.Nombre,
            ["storeName"] = producto.Tienda.Nombre,
            ["oldPrice"] = precioAnterior.ToString("F2"),
            ["newPrice"] = precioNuevo.ToString("F2")
        };
        await _push.SendPushNotificationAsync(devices, title, body, extraData, TypePriceDrop, productoId);
        _logger.LogInformation("Notificación PRICE_DROP enviada a {Count} dispositivos por producto {ProductoId}", devices.Count, productoId);
    }

    public async Task NotifyBackInStockAsync(int productoId)
    {
        var producto = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Tienda)
            .FirstOrDefaultAsync(p => p.Id == productoId);
        if (producto == null) return;

        var userIds = await _context.Favoritos
            .Where(f => f.ProductoId == productoId)
            .Select(f => f.UsuarioId)
            .Distinct()
            .ToListAsync();
        if (userIds.Count == 0) return;

        var filtered = await FilterByAntiSpamAsync(userIds, TypeBackInStock, productoId);
        if (filtered.Count == 0) return;

        var devices = await _context.Devices
            .Where(d => filtered.Contains(d.UsuarioId) && !string.IsNullOrWhiteSpace(d.FcmToken))
            .ToListAsync();
        if (devices.Count == 0) return;

        var title = "Volvió a haber stock";
        var body = $"{producto.Nombre} ya está disponible en {producto.Tienda.Nombre}";
        var extraData = new Dictionary<string, string>
        {
            ["type"] = TypeBackInStock,
            ["productId"] = productoId.ToString(),
            ["storeId"] = producto.TiendaId.ToString(),
            ["productName"] = producto.Nombre,
            ["storeName"] = producto.Tienda.Nombre
        };
        await _push.SendPushNotificationAsync(devices, title, body, extraData, TypeBackInStock, productoId);
        _logger.LogInformation("Notificación BACK_IN_STOCK enviada a {Count} dispositivos por producto {ProductoId}", devices.Count, productoId);
    }

    private async Task<List<int>> FilterByAntiSpamAsync(List<int> userIds, string notificationType, int entityId)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var logs = await _context.NotificationLogs
            .AsNoTracking()
            .Where(l => userIds.Contains(l.UsuarioId) && l.SentAt >= since)
            .Select(l => new { l.UsuarioId, l.NotificationType, l.EntityId })
            .ToListAsync();

        var yaEnviado = logs.Where(l => l.NotificationType == notificationType && l.EntityId == entityId).Select(l => l.UsuarioId).ToHashSet();
        var conteo = logs.GroupBy(l => l.UsuarioId).ToDictionary(g => g.Key, g => g.Count());
        return userIds.Where(id => !yaEnviado.Contains(id) && conteo.GetValueOrDefault(id, 0) < MaxNotificationsPerUserPerDay).ToList();
    }
}
