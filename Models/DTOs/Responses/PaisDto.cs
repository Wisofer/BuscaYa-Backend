namespace BuscaYa.Models.DTOs.Responses;

public class PaisDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CurrencyIso { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public string FlagEmoji { get; set; } = string.Empty;
    public string TimezoneId { get; set; } = string.Empty;
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public double DefaultZoom { get; set; }
}
