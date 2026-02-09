namespace BuscaYa.Models.DTOs.Requests;

public class RegisterDeviceRequest
{
    public string FcmToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // android, ios, web
}
