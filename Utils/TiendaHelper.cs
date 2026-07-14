using System;

namespace BuscaYa.Utils;

/// <summary>Helpers para tienda (estado, horarios, etc).</summary>
public static class TiendaHelper
{
    /// <summary>Calcula dinámicamente si la tienda está abierta en la hora local de Nicaragua (UTC-6).</summary>
    public static bool CalcularEstaAbierta(bool estaAbiertaManual, TimeSpan? horarioApertura, TimeSpan? horarioCierre)
    {
        if (!estaAbiertaManual)
            return false;

        if (!horarioApertura.HasValue || !horarioCierre.HasValue)
            return estaAbiertaManual;

        try
        {
            // Obtener hora local de Nicaragua (UTC-6)
            var zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById("America/Managua");
            var horaLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaHoraria);
            var horaActual = horaLocal.TimeOfDay;

            if (horarioApertura.Value <= horarioCierre.Value)
            {
                // Horario de día (ej: 08:00 a 18:00)
                return horaActual >= horarioApertura.Value && horaActual <= horarioCierre.Value;
            }
            else
            {
                // Horario nocturno (ej: 20:00 a 03:00)
                return horaActual >= horarioApertura.Value || horaActual <= horarioCierre.Value;
            }
        }
        catch (Exception)
        {
            // Fallback simple si la zona horaria no está registrada en el sistema operativo del host.
            // Nicaragua está siempre en UTC-6 (no hay horario de verano).
            var horaLocalFallback = DateTime.UtcNow.AddHours(-6);
            var horaActual = horaLocalFallback.TimeOfDay;

            if (horarioApertura.Value <= horarioCierre.Value)
            {
                return horaActual >= horarioApertura.Value && horaActual <= horarioCierre.Value;
            }
            else
            {
                return horaActual >= horarioApertura.Value || horaActual <= horarioCierre.Value;
            }
        }
    }
}
