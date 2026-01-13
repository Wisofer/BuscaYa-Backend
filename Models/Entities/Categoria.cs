namespace BuscaYa.Models.Entities;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Icono { get; set; }
    public bool Activo { get; set; } = true;
    public int Orden { get; set; } = 0;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Relaciones
    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
