namespace BuscaYa.Models.Entities;

public class Favorito
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int? TiendaId { get; set; }
    public int? ProductoId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Relaciones
    public virtual Usuario Usuario { get; set; } = null!;
    public virtual Tienda? Tienda { get; set; }
    public virtual Producto? Producto { get; set; }
}
