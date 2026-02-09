namespace BuscaYa.Models.DTOs.Requests;

public class SendNotificationRequest
{
    public int TemplateId { get; set; }
    public List<int>? UserIds { get; set; }
    public Dictionary<string, string>? ExtraData { get; set; }
    public bool DataOnly { get; set; }
}
