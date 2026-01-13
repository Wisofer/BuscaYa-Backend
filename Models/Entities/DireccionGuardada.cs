namespace BuscaYa.Models.Entities;

public class DireccionGuardada
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty; // "Casa", "Trabajo", etc.
    public string Direccion { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public bool EsPrincipal { get; set; } = false;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Relaciones
    public virtual Usuario Usuario { get; set; } = null!;
}
