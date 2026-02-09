namespace BuscaYa.Models.DTOs.Responses;

public class NotificationLogDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Payload { get; set; }
    /// <summary>Título del mensaje enviado (también en payload). Null en logs antiguos.</summary>
    public string? Title { get; set; }
    /// <summary>Cuerpo del mensaje enviado (también en payload). Null en logs antiguos.</summary>
    public string? Body { get; set; }
    public DateTime SentAt { get; set; }
    public int? DeviceId { get; set; }
    public int? TemplateId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime CreatedAt { get; set; }
}
