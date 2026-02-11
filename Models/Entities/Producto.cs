namespace BuscaYa.Models.Entities;

public class Producto
{
    public int Id { get; set; }
    public int TiendaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    /// <summary>Precio anterior cuando el producto está en oferta (para mostrar "antes X, ahora Y" y % descuento).</summary>
    public decimal? PrecioAnterior { get; set; }
    /// <summary>Si está activada la oferta; el frontend muestra badge y puede usar PrecioAnterior para tachado y %.</summary>
    public bool EnOferta { get; set; }
    public string Moneda { get; set; } = "C$"; // C$ o $
    public int CategoriaId { get; set; }
    public string? FotoUrl { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaActualizacion { get; set; }

    // Relaciones
    public virtual Tienda Tienda { get; set; } = null!;
    public virtual Categoria Categoria { get; set; } = null!;
    public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();
    public virtual ICollection<ProductoImagen> Imagenes { get; set; } = new List<ProductoImagen>();
}
