namespace BuscaYa.Models.Entities;

/// <summary>Registro de envío de una notificación push.</summary>
public class NotificationLog
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty; // sent, opened, failed
    public string? Payload { get; set; }
    public DateTime SentAt { get; set; }
    public int? DeviceId { get; set; }
    public int? TemplateId { get; set; }
    /// <summary>Tipo de disparo: NEW_STORE_NEARBY, PRICE_DROP, BACK_IN_STOCK. Null para envíos por plantilla.</summary>
    public string? NotificationType { get; set; }
    /// <summary>Id de tienda o producto según NotificationType. Para anti-spam.</summary>
    public int? EntityId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Device? Device { get; set; }
    public virtual NotificationTemplate? Template { get; set; }
    public virtual Usuario Usuario { get; set; } = null!;
}
