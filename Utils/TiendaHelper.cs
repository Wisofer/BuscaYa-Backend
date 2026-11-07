using System;

namespace BuscaYa.Utils;

/// <summary>Helpers para tienda (estado, horarios, etc).</summary>
public static class TiendaHelper
{
    /// <summary>Calcula dinámicamente si la tienda está abierta en la hora local del país de la tienda (soporte multi-zona horaria).</summary>
    public static bool CalcularEstaAbierta(bool estaAbiertaManual, TimeSpan? horarioApertura, TimeSpan? horarioCierre, string? countryCode = null)
    {
        if (!estaAbiertaManual)
            return false;

        if (!horarioApertura.HasValue || !horarioCierre.HasValue)
            return estaAbiertaManual;

        try
        {
            var timezoneId = RegionCatalog.ResolveTimezoneId(countryCode);
            var zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            var horaLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaHoraria);
            var horaActual = horaLocal.TimeOfDay;

            if (horarioApertura.Value <= horarioCierre.Value)
            {
                // Horario diurno (ej: 08:00 a 18:00)
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
            // Fallback robusto si la zona horaria IANA no está registrada en el SO host
            var entry = RegionCatalog.FindByCode(countryCode);
            var tz = entry?.TimezoneId ?? "America/Managua";
            int offsetHours = GetApproximateUtcOffset(tz);

            var horaLocalFallback = DateTime.UtcNow.AddHours(offsetHours);
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

    private static int GetApproximateUtcOffset(string timezoneId)
    {
        return timezoneId switch
        {
            "America/Argentina/Buenos_Aires" or "America/Montevideo" => -3,
            "America/Caracas" or "America/Santiago" or "America/La_Paz" or "America/Santo_Domingo" or "America/Manaus" => -4,
            "America/Bogota" or "America/Lima" or "America/Guayaquil" or "America/Panama" or "America/Havana" or "America/Port-au-Prince" or "America/New_York" => -5,
            _ => -6 // Centroamérica y México central
        };
    }
}
