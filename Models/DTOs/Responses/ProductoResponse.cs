namespace BuscaYa.Models.DTOs.Responses;

public class ProductoResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string Moneda { get; set; } = "C$";
    /// <summary>Producto en oferta (badge, tachado de precio anterior).</summary>
    public bool EnOferta { get; set; }
    /// <summary>Precio anterior cuando EnOferta es true (mostrar tachado).</summary>
    public decimal? PrecioAnterior { get; set; }
    /// <summary>Porcentaje de descuento cuando EnOferta y PrecioAnterior tienen valor. Ej: 15.</summary>
    public int? PorcentajeDescuento { get; set; }
    public string? FotoUrl { get; set; }
    /// <summary>Galería completa: imagen principal + resto en orden.</summary>
    public List<string> GaleriaUrls { get; set; } = new();
    public TiendaInfoResponse Tienda { get; set; } = null!;
    public CategoriaInfoResponse Categoria { get; set; } = null!;
    public double? DistanciaKm { get; set; }
    public string? TokenPublico { get; set; }
    public string? Slug { get; set; }
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
    /// <summary>Foto de portada del negocio. Se usa como fallback cuando no hay LogoUrl.</summary>
    public string? FotoUrl { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public int FavoritosCount { get; set; }
    public string? WhatsAppUrl { get; set; } // Link de WhatsApp con mensaje personalizado (para productos)
    public bool EstaAbierta { get; set; }
    public string? Email { get; set; }
    public string? DiasAtencion { get; set; }
    public string? HorarioApertura { get; set; }
    public string? HorarioCierre { get; set; }
    public string? Descripcion { get; set; }
    public string? TokenPublico { get; set; }
    public string? Slug { get; set; }
}

public class CategoriaInfoResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Icono { get; set; }
}
