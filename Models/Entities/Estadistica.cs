namespace BuscaYa.Models.Entities;

public class Estadistica
{
    public int Id { get; set; }
    public int TiendaId { get; set; }
    public string TipoEvento { get; set; } = string.Empty; // VistaTienda, ClickWhatsApp, ClickLlamar, ClickDireccion, Busqueda
    public string? ProductoBuscado { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string? DatosAdicionales { get; set; } // JSON opcional

    // Relaciones
    public virtual Tienda Tienda { get; set; } = null!;
}
