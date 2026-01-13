namespace BuscaYa.Models.DTOs.Requests;

public class ActualizarProductoRequest
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? Moneda { get; set; }
    public int? CategoriaId { get; set; }
    public string? FotoUrl { get; set; }
    public bool? Activo { get; set; }
}
