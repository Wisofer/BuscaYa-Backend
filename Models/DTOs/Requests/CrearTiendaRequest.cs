namespace BuscaYa.Models.DTOs.Requests;

public class CrearTiendaRequest
{
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
    /// <summary>Estado manual: true = Abierto, false = Cerrado. Por defecto true al crear.</summary>
    public bool EstaAbiertaManual { get; set; } = true;
}
