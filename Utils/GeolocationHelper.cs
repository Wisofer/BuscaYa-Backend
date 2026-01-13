namespace BuscaYa.Utils;

/// <summary>
/// Helper para cálculos geográficos (distancias, etc.)
/// </summary>
public static class GeolocationHelper
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Calcula la distancia entre dos puntos geográficos usando la fórmula de Haversine
    /// </summary>
    /// <param name="lat1">Latitud del primer punto</param>
    /// <param name="lon1">Longitud del primer punto</param>
    /// <param name="lat2">Latitud del segundo punto</param>
    /// <param name="lon2">Longitud del segundo punto</param>
    /// <returns>Distancia en kilómetros</returns>
    public static double CalcularDistanciaKm(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        var dLat = ToRadians((double)(lat2 - lat1));
        var dLon = ToRadians((double)(lon2 - lon1));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    /// <summary>
    /// Convierte grados a radianes
    /// </summary>
    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    /// <summary>
    /// Formatea la distancia para mostrar al usuario
    /// </summary>
    public static string FormatearDistancia(double distanciaKm)
    {
        if (distanciaKm < 1)
        {
            return $"{(int)(distanciaKm * 1000)} m";
        }
        return $"{distanciaKm:F1} km";
    }
}
