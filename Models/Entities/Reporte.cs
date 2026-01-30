namespace BuscaYa.Models.Entities;

public class Reporte
{
    public int Id { get; set; }
    public int UsuarioId { get; set; } // Usuario que reporta
    public string Tipo { get; set; } = string.Empty; // "producto" o "tienda"
    public int RecursoId { get; set; } // ID del producto o tienda reportado
    public string Razon { get; set; } = string.Empty; // Razón del reporte
    public string? Detalle { get; set; } // Detalle opcional
    public bool Revisado { get; set; } = false; // Si el admin ya lo revisó
    public string? NotaAdmin { get; set; } // Nota del admin al revisar
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaRevisado { get; set; }

    // Relaciones
    public virtual Usuario Usuario { get; set; } = null!;
}
