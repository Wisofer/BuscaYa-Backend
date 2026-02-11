using System.Text.Json;
using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PushNotificationService> _logger;
    private const int BatchSize = 500;

    public PushNotificationService(ApplicationDbContext context, ILogger<PushNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendPushNotificationAsync(
        NotificationTemplate? template,
        List<Device> devices,
        IDictionary<string, string>? extraData = null,
        bool dataOnly = false)
    {
        var validDevices = devices.Where(d => !string.IsNullOrWhiteSpace(d.FcmToken)).ToList();
        if (validDevices.Count == 0)
        {
            _logger.LogWarning("No devices with valid FcmToken to send notification");
            return;
        }

        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogError("Firebase not initialized. Skipping push notification.");
            throw new InvalidOperationException("Firebase no está inicializado. Configure FIREBASE_CREDENTIALS_JSON o Secrets/firebase_credentials.json");
        }

        var messaging = FirebaseMessaging.GetMessaging(FirebaseApp.DefaultInstance);

        string title = template?.Title ?? "BuscaYa";
        string body = template?.Body ?? "";
        string? imageUrl = null;
        if (!string.IsNullOrWhiteSpace(template?.ImageUrl) &&
            (template.ImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
             template.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            imageUrl = template.ImageUrl;
        }

        var typeVal = "announcement";
        if (extraData != null && extraData.TryGetValue("type", out var t))
            typeVal = t;
        var data = new Dictionary<string, string>
        {
            ["title"] = title,
            ["body"] = body,
            ["type"] = typeVal
        };
        if (!string.IsNullOrEmpty(imageUrl))
            data["imageUrl"] = imageUrl;
        if (template != null)
            data["templateId"] = template.Id.ToString();
        if (extraData != null)
        {
            foreach (var kv in extraData)
                data[kv.Key] = kv.Value;
        }

        Notification? notification = null;
        if (!dataOnly && !string.IsNullOrEmpty(body))
        {
            notification = new Notification
            {
                Title = title,
                Body = body,
                ImageUrl = imageUrl
            };
        }

        var androidConfig = new AndroidConfig
        {
            Priority = Priority.High,
            Notification = new AndroidNotification
            {
                ChannelId = "default",
                Sound = "default"
            }
        };

        ApnsConfig? apnsConfig = null;
        if (dataOnly)
        {
            apnsConfig = new ApnsConfig
            {
                Aps = new Aps
                {
                    ContentAvailable = true
                }
            };
        }
        else
        {
            apnsConfig = new ApnsConfig
            {
                Aps = new Aps
                {
                    ContentAvailable = false,
                    Sound = "default",
                    Badge = 1,
                    Alert = new ApsAlert
                    {
                        Title = title,
                        Body = body
                    }
                }
            };
        }

        WebpushConfig? webpushConfig = null;
        if (!dataOnly && notification != null)
        {
            webpushConfig = new WebpushConfig
            {
                Notification = new WebpushNotification
                {
                    Title = title,
                    Body = body,
                    Image = imageUrl ?? ""
                }
            };
        }

        var tokens = validDevices.Select(d => d.FcmToken).ToList();
        var sentAt = DateTime.UtcNow;

        for (int offset = 0; offset < tokens.Count; offset += BatchSize)
        {
            var batchTokens = tokens.Skip(offset).Take(BatchSize).ToList();
            var batchDevices = validDevices.Skip(offset).Take(BatchSize).ToList();

            var multicast = new MulticastMessage
            {
                Tokens = batchTokens,
                Data = data,
                Notification = notification,
                Android = androidConfig,
                Apns = apnsConfig,
                Webpush = webpushConfig
            };

            BatchResponse batchResponse;
            try
            {
                batchResponse = await messaging.SendEachForMulticastAsync(multicast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending FCM multicast batch");
                var failedPayload = JsonSerializer.Serialize(new { title, body, error = ex.Message });
                foreach (var dev in batchDevices)
                {
                    _context.NotificationLogs.Add(new NotificationLog
                    {
                        Status = "failed",
                        Payload = failedPayload,
                        SentAt = sentAt,
                        DeviceId = dev.Id,
                        TemplateId = template?.Id,
                        UsuarioId = dev.UsuarioId
                    });
                }
                await _context.SaveChangesAsync();
                throw;
            }

            var responses = batchResponse.Responses;
            for (int i = 0; i < responses.Count; i++)
            {
                var device = batchDevices[i];
                var sendResponse = responses[i];
                string status = sendResponse.IsSuccess ? "sent" : "failed";
                string payload = sendResponse.IsSuccess && !string.IsNullOrEmpty(sendResponse.MessageId)
                    ? JsonSerializer.Serialize(new { messageId = sendResponse.MessageId, title, body })
                    : JsonSerializer.Serialize(new { title, body, error = sendResponse.Exception?.Message ?? "unknown" });

                _context.NotificationLogs.Add(new NotificationLog
                {
                    Status = status,
                    Payload = payload,
                    SentAt = sentAt,
                    DeviceId = device.Id,
                    TemplateId = template?.Id,
                    UsuarioId = device.UsuarioId
                });

                if (!sendResponse.IsSuccess && sendResponse.Exception != null)
                {
                    var code = sendResponse.Exception.MessagingErrorCode;
                    if (code == MessagingErrorCode.InvalidArgument || code == MessagingErrorCode.Unregistered)
                    {
                        _context.Devices.Remove(device);
                        _logger.LogInformation("Removed invalid device Id={DeviceId}, FcmToken invalid or unregistered", device.Id);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task SendPushNotificationAsync(
        List<Device> devices,
        string title,
        string body,
        IDictionary<string, string>? extraData = null,
        string? notificationType = null,
        int? entityId = null)
    {
        var validDevices = devices.Where(d => !string.IsNullOrWhiteSpace(d.FcmToken)).ToList();
        if (validDevices.Count == 0)
        {
            _logger.LogWarning("No devices with valid FcmToken to send notification");
            return;
        }

        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogError("Firebase not initialized. Skipping push notification.");
            throw new InvalidOperationException("Firebase no está inicializado. Configure FIREBASE_CREDENTIALS_JSON o Secrets/firebase_credentials.json");
        }

        var typeVal = extraData != null && extraData.TryGetValue("type", out var t) ? t : (notificationType ?? "announcement");
        var data = new Dictionary<string, string>
        {
            ["title"] = title,
            ["body"] = body,
            ["type"] = typeVal
        };
        if (extraData != null)
        {
            foreach (var kv in extraData)
                data[kv.Key] = kv.Value;
        }

        var notification = new Notification { Title = title, Body = body };
        var androidConfig = new AndroidConfig
        {
            Priority = Priority.High,
            Notification = new AndroidNotification { ChannelId = "default", Sound = "default" }
        };
        var apnsConfig = new ApnsConfig
        {
            Aps = new Aps
            {
                ContentAvailable = false,
                Sound = "default",
                Badge = 1,
                Alert = new ApsAlert { Title = title, Body = body }
            }
        };
        var webpushConfig = new WebpushConfig
        {
            Notification = new WebpushNotification { Title = title, Body = body, Image = "" }
        };

        var messaging = FirebaseMessaging.GetMessaging(FirebaseApp.DefaultInstance);
        var sentAt = DateTime.UtcNow;

        for (int offset = 0; offset < validDevices.Count; offset += BatchSize)
        {
            var batchDevices = validDevices.Skip(offset).Take(BatchSize).ToList();
            var batchTokens = batchDevices.Select(d => d.FcmToken!).ToList();
            var multicast = new MulticastMessage
            {
                Tokens = batchTokens,
                Data = data,
                Notification = notification,
                Android = androidConfig,
                Apns = apnsConfig,
                Webpush = webpushConfig
            };

            try
            {
                var batchResponse = await messaging.SendEachForMulticastAsync(multicast);
                for (int i = 0; i < batchResponse.Responses.Count; i++)
                {
                    var device = batchDevices[i];
                    var sendResponse = batchResponse.Responses[i];
                    var status = sendResponse.IsSuccess ? "sent" : "failed";
                    var payload = sendResponse.IsSuccess && !string.IsNullOrEmpty(sendResponse.MessageId)
                        ? JsonSerializer.Serialize(new { messageId = sendResponse.MessageId, title, body })
                        : JsonSerializer.Serialize(new { title, body, error = sendResponse.Exception?.Message ?? "unknown" });
                    _context.NotificationLogs.Add(new NotificationLog
                    {
                        Status = status,
                        Payload = payload,
                        SentAt = sentAt,
                        DeviceId = device.Id,
                        UsuarioId = device.UsuarioId,
                        NotificationType = notificationType,
                        EntityId = entityId
                    });
                    if (!sendResponse.IsSuccess && sendResponse.Exception != null)
                    {
                        var code = sendResponse.Exception.MessagingErrorCode;
                        if (code == MessagingErrorCode.InvalidArgument || code == MessagingErrorCode.Unregistered)
                        {
                            _context.Devices.Remove(device);
                            _logger.LogInformation("Removed invalid device Id={DeviceId}", device.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending FCM batch (trigger notification)");
                var failedPayload = JsonSerializer.Serialize(new { title, body, error = ex.Message });
                foreach (var dev in batchDevices)
                {
                    _context.NotificationLogs.Add(new NotificationLog
                    {
                        Status = "failed",
                        Payload = failedPayload,
                        SentAt = sentAt,
                        DeviceId = dev.Id,
                        UsuarioId = dev.UsuarioId,
                        NotificationType = notificationType,
                        EntityId = entityId
                    });
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}
