namespace BuscaYa.Models.Entities;

public class HistorialBusqueda
{
    public int Id { get; set; }
    public int? UsuarioId { get; set; } // Nullable para búsquedas anónimas
    public string Termino { get; set; } = string.Empty;
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;

    // Relaciones
    public virtual Usuario? Usuario { get; set; }
}
