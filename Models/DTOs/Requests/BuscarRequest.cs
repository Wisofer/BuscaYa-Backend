namespace BuscaYa.Models.DTOs.Requests;

public class BuscarRequest
{
    // Termino es opcional: si viene vacío o null, se interpreta como búsqueda amplia (Destacados)
    public string? Termino { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public double? RadioKm { get; set; } = 5.0;
    public int? CategoriaId { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}
