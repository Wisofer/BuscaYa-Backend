using BuscaYa.Models.Entities;

namespace BuscaYa.Services.IServices;

public interface IPushNotificationService
{
    Task SendPushNotificationAsync(
        NotificationTemplate? template,
        List<Device> devices,
        IDictionary<string, string>? extraData = null,
        bool dataOnly = false);
}
