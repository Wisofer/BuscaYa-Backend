namespace BuscaYa.Models.Entities;

/// <summary>Dispositivo registrado para recibir notificaciones push (FCM).</summary>
public class Device
{
    public int Id { get; set; }
    public string FcmToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // android, ios, web, unknown
    public DateTime? LastActiveAt { get; set; }
    public int UsuarioId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Usuario Usuario { get; set; } = null!;
}
