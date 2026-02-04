namespace BuscaYa.Models.Entities;

public class Tienda
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
    /// <summary>Control manual del dueño: true = Abierto, false = Cerrado. No depende del horario.</summary>
    public bool EstaAbiertaManual { get; set; } = true;
    public string Plan { get; set; } = "Free"; // Free, Pro
    public bool Activo { get; set; } = true;
    public int? UsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaActualizacion { get; set; }

    // Calificaciones
    public double CalificacionPromedio { get; set; }
    public int TotalCalificaciones { get; set; }

    // Relaciones
    public virtual Usuario? Usuario { get; set; }
    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    public virtual ICollection<Estadistica> Estadisticas { get; set; } = new List<Estadistica>();
    public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();
    public virtual ICollection<CalificacionTienda> Calificaciones { get; set; } = new List<CalificacionTienda>();
}
