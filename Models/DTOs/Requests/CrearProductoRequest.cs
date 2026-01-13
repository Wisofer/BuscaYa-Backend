namespace BuscaYa.Models.DTOs.Requests;

public class CrearProductoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string Moneda { get; set; } = "C$";
    public int CategoriaId { get; set; }
    public string? FotoUrl { get; set; }
}
