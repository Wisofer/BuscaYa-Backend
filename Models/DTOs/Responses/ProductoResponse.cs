namespace BuscaYa.Models.DTOs.Responses;

public class ProductoResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string Moneda { get; set; } = "C$";
    public string? FotoUrl { get; set; }
    /// <summary>Galería completa: imagen principal + resto en orden.</summary>
    public List<string> GaleriaUrls { get; set; } = new();
    public TiendaInfoResponse Tienda { get; set; } = null!;
    public CategoriaInfoResponse Categoria { get; set; } = null!;
    public double? DistanciaKm { get; set; }
}

public class TiendaInfoResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? LogoUrl { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public string? WhatsAppUrl { get; set; } // Link de WhatsApp con mensaje personalizado (para productos)
}

public class CategoriaInfoResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Icono { get; set; }
}
