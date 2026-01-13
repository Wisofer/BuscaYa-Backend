namespace BuscaYa.Models.DTOs.Responses;

public class BuscarResponse
{
    public List<ProductoResponse> Productos { get; set; } = new();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalPaginas { get; set; }
}
