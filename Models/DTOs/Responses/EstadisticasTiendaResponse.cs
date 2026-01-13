namespace BuscaYa.Models.DTOs.Responses;

public class EstadisticasTiendaResponse
{
    public int TiendaId { get; set; }
    public string NombreTienda { get; set; } = string.Empty;
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public int TotalVistas { get; set; }
    public int TotalClicksWhatsApp { get; set; }
    public int TotalClicksLlamar { get; set; }
    public int TotalClicksDireccion { get; set; }
    public int TotalBusquedas { get; set; }
    public List<ProductoBuscadoResponse> ProductosMasBuscados { get; set; } = new();
}

public class ProductoBuscadoResponse
{
    public int ProductoId { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public int VecesBuscado { get; set; }
}
