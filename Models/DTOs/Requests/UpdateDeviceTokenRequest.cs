namespace BuscaYa.Models.DTOs.Requests;

public class UpdateDeviceTokenRequest
{
    public string CurrentFcmToken { get; set; } = string.Empty;
    public string NewFcmToken { get; set; } = string.Empty;
    public string? Platform { get; set; }
}
