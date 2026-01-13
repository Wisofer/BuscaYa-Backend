namespace BuscaYa.Models.DTOs.Responses;

public class TiendaResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Telefono { get; set; }
    public string WhatsApp { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public string Ciudad { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public TimeSpan? HorarioApertura { get; set; }
    public TimeSpan? HorarioCierre { get; set; }
    public string? DiasAtencion { get; set; }
    public string? LogoUrl { get; set; }
    public string? FotoUrl { get; set; }
    public string Plan { get; set; } = "Free";
    public double? DistanciaKm { get; set; }
    public List<ProductoSimpleResponse> Productos { get; set; } = new();
}

public class ProductoSimpleResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal? Precio { get; set; }
    public string Moneda { get; set; } = "C$";
    public string? FotoUrl { get; set; }
}
