namespace BuscaYa.Models.Entities;

public class CalificacionTienda
{
    public int Id { get; set; }
    public int TiendaId { get; set; }
    public int UsuarioId { get; set; }
    public int Valor { get; set; } // 1 a 5 estrellas
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaActualizacion { get; set; }

    // Relaciones
    public virtual Tienda Tienda { get; set; } = null!;
    public virtual Usuario Usuario { get; set; } = null!;
}

