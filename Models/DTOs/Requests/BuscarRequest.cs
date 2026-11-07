namespace BuscaYa.Models.DTOs.Requests;

public class BuscarRequest
{
    // Termino es opcional: si viene vacío o null, se interpreta como búsqueda amplia (Destacados)
    public string? Termino { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }

    // Compatibilidad para query params abreviados "lat" y "lng"
    public decimal? Lat { get => Latitud; set => Latitud ??= value; }
    public decimal? Lng { get => Longitud; set => Longitud ??= value; }

    public double? RadioKm { get; set; }
    public int? CategoriaId { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}
