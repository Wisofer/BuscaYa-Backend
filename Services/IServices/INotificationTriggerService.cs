namespace BuscaYa.Services.IServices;

/// <summary>Disparos automáticos de notificaciones: tienda nueva cerca, precio, stock, interés y recordatorios.</summary>
public interface INotificationTriggerService
{
    /// <summary>Notifica a usuarios con dirección guardada dentro del radio de la tienda.</summary>
    Task NotifyNewStoreNearbyAsync(int tiendaId);

    /// <summary>Notifica a usuarios que tienen el producto en favoritos cuando baja el precio.</summary>
    Task NotifyPriceDropAsync(int productoId, decimal precioAnterior, decimal precioNuevo);

    /// <summary>Notifica a usuarios que tienen el producto en favoritos cuando vuelve a haber stock.</summary>
    Task NotifyBackInStockAsync(int productoId);

    /// <summary>Notifica a usuarios que tienen el producto en favoritos cuando entra en oferta.</summary>
    Task NotifyFavoriteOnOfferAsync(int productoId, decimal? precioOferta, decimal? precioAnterior);

    /// <summary>Notifica al dueño cuando alguien abre el detalle de su producto.</summary>
    Task NotifyProductInterestAsync(int productoId, int interestedUserId);

    /// <summary>Envía un recordatorio diario a usuarios con favoritos actualmente en oferta.</summary>
    Task SendDailyFavoritesReminderAsync(CancellationToken cancellationToken = default);
}
