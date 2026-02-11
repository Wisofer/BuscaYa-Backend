using BuscaYa.Models.Entities;

namespace BuscaYa.Services.IServices;

public interface IPushNotificationService
{
    Task SendPushNotificationAsync(
        NotificationTemplate? template,
        List<Device> devices,
        IDictionary<string, string>? extraData = null,
        bool dataOnly = false);

    /// <summary>Envía push con título y cuerpo (sin plantilla). Para disparos automáticos: tienda cerca, bajó precio, restock.</summary>
    Task SendPushNotificationAsync(
        List<Device> devices,
        string title,
        string body,
        IDictionary<string, string>? extraData = null,
        string? notificationType = null,
        int? entityId = null);
}
