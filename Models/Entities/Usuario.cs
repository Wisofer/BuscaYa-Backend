namespace BuscaYa.Models.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty; // "Administrador", "TiendaOwner", "Cliente"
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? FotoPerfilUrl { get; set; }
    public int? TiendaId { get; set; } // Si es dueño de tienda
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Relaciones
    public virtual Tienda? Tienda { get; set; }
    public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();
    public virtual ICollection<HistorialBusqueda> HistorialBusquedas { get; set; } = new List<HistorialBusqueda>();
    public virtual ICollection<DireccionGuardada> DireccionesGuardadas { get; set; } = new List<DireccionGuardada>();
    public virtual ICollection<Reporte> Reportes { get; set; } = new List<Reporte>();
}

