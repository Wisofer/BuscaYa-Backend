namespace BuscaYa.Models.DTOs.Responses;

public class SendNotificationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalDevices { get; set; }
}
