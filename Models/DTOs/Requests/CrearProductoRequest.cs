namespace BuscaYa.Models.DTOs.Requests;

public class CrearProductoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string Moneda { get; set; } = "C$";
    public int CategoriaId { get; set; }
    /// <summary>URL de la imagen principal (para listados). Si no se envía, se usa la primera de ImagenesUrls.</summary>
    public string? FotoUrl { get; set; }
    /// <summary>Lista de URLs de imágenes del producto (galería). La primera se usa como principal si FotoUrl no viene.</summary>
    public List<string>? ImagenesUrls { get; set; }
}
