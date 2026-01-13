namespace BuscaYa.Models.DTOs.Requests;

public class ActualizarTiendaRequest
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? Telefono { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string? Ciudad { get; set; }
    public string? Departamento { get; set; }
    public TimeSpan? HorarioApertura { get; set; }
    public TimeSpan? HorarioCierre { get; set; }
    public string? DiasAtencion { get; set; }
    public string? LogoUrl { get; set; }
    public string? FotoUrl { get; set; }
}
