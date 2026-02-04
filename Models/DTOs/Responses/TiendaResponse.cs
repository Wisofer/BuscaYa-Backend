namespace BuscaYa.Models.DTOs.Responses;

public class TiendaResponse
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
    public string Plan { get; set; } = "Free";
    public double? DistanciaKm { get; set; }
    /// <summary>Estado manual del dueño (Abierto/Cerrado). No depende del horario.</summary>
    public bool EstaAbierta { get; set; }
    public double CalificacionPromedio { get; set; }
    public int TotalCalificaciones { get; set; }
    public List<ProductoSimpleResponse> Productos { get; set; } = new();

    /// <summary>
    /// Dirección completa en un solo campo, pensada para el frontend móvil.
    /// Ejemplo: "Dirección..., Ciudad, Departamento".
    /// </summary>
    public string DireccionCompleta
    {
        get
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(Direccion)) partes.Add(Direccion);
            if (!string.IsNullOrWhiteSpace(Ciudad)) partes.Add(Ciudad);
            if (!string.IsNullOrWhiteSpace(Departamento)) partes.Add(Departamento);
            return string.Join(", ", partes);
        }
    }

    /// <summary>
    /// Horario formateado para mostrar en el detalle público y compartir tienda.
    /// Ejemplo: "Lun-Dom 08:00-17:00".
    /// </summary>
    public string HorarioFormateado
    {
        get
        {
            if (!HorarioApertura.HasValue || !HorarioCierre.HasValue)
            {
                return DiasAtencion ?? string.Empty;
            }

            var inicio = HorarioApertura.Value.ToString(@"hh\:mm");
            var fin = HorarioCierre.Value.ToString(@"hh\:mm");

            if (!string.IsNullOrWhiteSpace(DiasAtencion))
            {
                return $"{DiasAtencion} {inicio}-{fin}";
            }

            return $"{inicio}-{fin}";
        }
    }
}

public class ProductoSimpleResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal? Precio { get; set; }
    public string Moneda { get; set; } = "C$";
    public string? FotoUrl { get; set; }
}
