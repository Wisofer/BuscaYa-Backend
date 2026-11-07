namespace BuscaYa.Models.DTOs.Requests;

/// <summary>
/// Parámetros de consulta para exploración de tiendas y mapa de negocios (GET /api/public/tiendas)
/// </summary>
public class ObtenerTiendasRequest
{
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }

    // Compatibilidad para query params abreviados "lat" y "lng"
    public decimal? Lat { get => Latitud; set => Latitud ??= value; }
    public decimal? Lng { get => Longitud; set => Longitud ??= value; }

    public double? RadioKm { get; set; }
    public int? CategoriaId { get; set; }
    public string? CountryCode { get; set; }
    public string? Termino { get; set; }
    public bool? SoloAbiertas { get; set; }
    public int? Pagina { get; set; }
    public int? TamanoPagina { get; set; }
}

