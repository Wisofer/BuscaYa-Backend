namespace BuscaYa.Services.IServices;

/// <summary>Disparos automáticos de notificaciones: tienda nueva cerca, bajó de precio, volvió a haber stock.</summary>
public interface INotificationTriggerService
{
    /// <summary>Notifica a usuarios con dirección guardada dentro del radio de la tienda.</summary>
    Task NotifyNewStoreNearbyAsync(int tiendaId);

    /// <summary>Notifica a usuarios que tienen el producto en favoritos cuando baja el precio.</summary>
    Task NotifyPriceDropAsync(int productoId, decimal precioAnterior, decimal precioNuevo);

    /// <summary>Notifica a usuarios que tienen el producto en favoritos cuando vuelve a haber stock.</summary>
    Task NotifyBackInStockAsync(int productoId);
}
