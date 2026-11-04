namespace BuscaYa.Utils;

/// <summary>
/// Catálogo centralizado de Países de Latinoamérica (LATAM) con soporte para monedas, banderas, zonas horarias, divisiones políticas y coordenadas centrales para OpenStreetMap.
/// </summary>
public static class RegionCatalog
{
    public sealed record CountryEntry(
        string Code,
        string NameEs,
        string CurrencyIso,
        string CurrencySymbol,
        string TimezoneId,
        double CenterLat,
        double CenterLng,
        double DefaultZoom
    );

    /// <summary>
    /// Lista fija de 21 Países de Latinoamérica y el Caribe hispano.
    /// </summary>
    public static readonly IReadOnlyList<CountryEntry> Countries = new[]
    {
        new CountryEntry("AR", "Argentina", "ARS", "$", "America/Argentina/Buenos_Aires", -34.6037, -58.3816, 11),
        new CountryEntry("BZ", "Belice", "BZD", "BZ$", "America/Belize", 17.4986, -88.1886, 11),
        new CountryEntry("BO", "Bolivia", "BOB", "Bs.", "America/La_Paz", -16.5001, -68.1193, 11),
        new CountryEntry("BR", "Brasil", "BRL", "R$", "America/Sao_Paulo", -23.5505, -46.6333, 11),
        new CountryEntry("CL", "Chile", "CLP", "$", "America/Santiago", -33.4489, -70.6693, 11),
        new CountryEntry("CO", "Colombia", "COP", "$", "America/Bogota", 4.7110, -74.0721, 11),
        new CountryEntry("CR", "Costa Rica", "CRC", "₡", "America/Costa_Rica", 9.9281, -84.0907, 12),
        new CountryEntry("CU", "Cuba", "CUP", "$", "America/Havana", 23.1136, -82.3666, 11),
        new CountryEntry("EC", "Ecuador", "USD", "$", "America/Guayaquil", -2.1894, -79.8891, 11),
        new CountryEntry("SV", "El Salvador", "USD", "$", "America/El_Salvador", 13.6929, -89.2182, 12),
        new CountryEntry("GT", "Guatemala", "GTQ", "Q", "America/Guatemala", 14.6349, -90.5069, 12),
        new CountryEntry("HT", "Haití", "HTG", "G", "America/Port-au-Prince", 18.5944, -72.3074, 11),
        new CountryEntry("HN", "Honduras", "HNL", "L", "America/Tegucigalpa", 14.0723, -87.1921, 12),
        new CountryEntry("MX", "México", "MXN", "$", "America/Mexico_City", 19.4326, -99.1332, 11),
        new CountryEntry("NI", "Nicaragua", "NIO", "C$", "America/Managua", 12.1364, -86.2514, 12),
        new CountryEntry("PA", "Panamá", "USD", "$", "America/Panama", 8.9824, -79.5199, 12),
        new CountryEntry("PY", "Paraguay", "PYG", "₲", "America/Asuncion", -25.2637, -57.5759, 11),
        new CountryEntry("PE", "Perú", "PEN", "S/", "America/Lima", -12.0464, -77.0428, 11),
        new CountryEntry("DO", "República Dominicana", "DOP", "RD$", "America/Santo_Domingo", 18.4861, -69.9312, 11),
        new CountryEntry("UY", "Uruguay", "UYU", "$", "America/Montevideo", -34.9011, -56.1645, 11),
        new CountryEntry("VE", "Venezuela", "USD", "$", "America/Caracas", 10.4806, -66.9036, 11)
    };

    private static readonly Dictionary<string, List<string>> DepartmentsByCountry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NI"] = new() { "Managua", "León", "Granada", "Masaya", "Carazo", "Chinandega", "Rivas", "Estelí", "Matagalpa", "Jinotega", "Madriz", "Nueva Segovia", "Chontales", "Boaco", "Río San Juan", "RACCN", "RACCS" },
        ["GT"] = new() { "Guatemala", "Sacatepéquez", "Quetzaltenango", "Alta Verapaz", "Escuintla", "Izabal", "Huehuetenango", "San Marcos", "Chimaltenango", "Suchitepéquez", "Santa Rosa", "Retalhuleu", "Sololá", "Petén", "Chiquimula", "El Progreso", "Jalapa", "Jutiapa", "Quiché", "Totonicapán", "Zacapa", "Baja Verapaz" },
        ["SV"] = new() { "San Salvador", "La Libertad", "Santa Ana", "San Miguel", "Sonsonate", "Usulután", "Ahuachapán", "La Paz", "Cuscatlán", "Chalatenango", "Morazán", "San Vicente", "Cabañas", "La Unión" },
        ["HN"] = new() { "Francisco Morazán", "Cortés", "Atlántida", "Comayagua", "Copán", "Choluteca", "El Paraíso", "Gracias a Dios", "Intibucá", "Islas de la Bahía", "La Paz", "Lempira", "Ocotepeque", "Olancho", "Santa Bárbara", "Valle", "Yoro", "Colón" },
        ["CR"] = new() { "San José", "Alajuela", "Cartago", "Heredia", "Guanacaste", "Puntarenas", "Limón" },
        ["PA"] = new() { "Panamá", "Chiriquí", "Colón", "Coclé", "Veraguas", "Herrera", "Los Santos", "Bocas del Toro", "Darién", "Panamá Oeste" },
        ["CO"] = new() { "Bogotá D.C.", "Antioquia", "Valle del Cauca", "Cundinamarca", "Atlántico", "Santander", "Bolívar", "Caldas", "Risaralda", "Nariño", "Tolima", "Huila", "Boyacá", "Norte de Santander", "Meta" },
        ["MX"] = new() { "Ciudad de México", "Jalisco", "Nuevo León", "Estado de México", "Puebla", "Guanajuato", "Veracruz", "Yucatán", "Quintana Roo", "Querétaro", "Baja California", "Michoacán", "Chihuahua" },
        ["PE"] = new() { "Lima", "Arequipa", "La Libertad", "Piura", "Cusco", "Junín", "Lambayeque", "Puno", "Ancash", "Ica", "Tacna" },
        ["EC"] = new() { "Pichincha", "Guayas", "Azuay", "Manabí", "El Oro", "Loja", "Tungurahua", "Los Ríos", "Santo Domingo", "Imbabura" },
        ["AR"] = new() { "Buenos Aires", "Ciudad Autónoma de Buenos Aires", "Córdoba", "Santa Fe", "Mendoza", "Tucumán", "Salta", "Entre Ríos", "Misiones", "Neuquén", "Río Negro", "Chaco" },
        ["CL"] = new() { "Región Metropolitana", "Valparaíso", "Biobío", "La Araucanía", "Coquimbo", "Maule", "Los Lagos", "Antofagasta" }
    };

    public static CountryEntry? FindByCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return null;
        var c = countryCode.Trim().ToUpperInvariant();
        return Countries.FirstOrDefault(x => x.Code == c);
    }

    public static CountryEntry GetDefault()
    {
        return FindByCode("NI")!;
    }

    public static List<string> GetDepartments(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return DepartmentsByCountry["NI"];
        var code = countryCode.Trim().ToUpperInvariant();
        return DepartmentsByCountry.TryGetValue(code, out var list) ? list : new List<string>();
    }

    public static string ResolveTimezoneId(string? countryCode)
    {
        var entry = FindByCode(countryCode);
        return entry?.TimezoneId ?? "America/Managua";
    }

    public static bool TryResolveCurrency(string? countryCode, out string currencyIso, out string symbol)
    {
        currencyIso = "USD";
        symbol = "$";
        var entry = FindByCode(countryCode);
        if (entry == null) return false;
        currencyIso = entry.CurrencyIso;
        symbol = entry.CurrencySymbol;
        return true;
    }

    public static string GetFlagEmoji(string? code)
    {
        return (code ?? "").Trim().ToUpperInvariant() switch
        {
            "AR" => "🇦🇷",
            "BZ" => "🇧🇿",
            "BO" => "🇧🇴",
            "BR" => "🇧🇷",
            "CL" => "🇨🇱",
            "CO" => "🇨🇴",
            "CR" => "🇨🇷",
            "CU" => "🇨🇺",
            "DO" => "🇩🇴",
            "EC" => "🇪🇨",
            "SV" => "🇸🇻",
            "GT" => "🇬🇹",
            "HT" => "🇭🇹",
            "HN" => "🇭🇳",
            "MX" => "🇲🇽",
            "NI" => "🇳🇮",
            "PA" => "🇵🇦",
            "PY" => "🇵🇾",
            "PE" => "🇵🇪",
            "UY" => "🇺🇾",
            "VE" => "🇻🇪",
            _ => "🌐"
        };
    }

    public static string GetTaxIdLabel(string? countryCode)
    {
        return (countryCode ?? "").Trim().ToUpperInvariant() switch
        {
            "VE" => "RIF",
            "CO" or "GT" or "SV" or "BO" => "NIT",
            "MX" => "RFC",
            "CL" or "UY" => "RUT",
            "AR" => "CUIT",
            "DO" => "RNC",
            "HN" => "RTN",
            "BR" => "CNPJ",
            "CR" => "Cédula",
            _ => "RUC"
        };
    }

    public static string GetDialCode(string? countryCode)
    {
        return (countryCode ?? "").Trim().ToUpperInvariant() switch
        {
            "AR" => "54",
            "BZ" => "501",
            "BO" => "591",
            "BR" => "55",
            "CL" => "56",
            "CO" => "57",
            "CR" => "506",
            "CU" => "53",
            "DO" => "1",
            "EC" => "593",
            "SV" => "503",
            "GT" => "502",
            "HT" => "509",
            "HN" => "504",
            "MX" => "52",
            "NI" => "505",
            "PA" => "507",
            "PY" => "595",
            "PE" => "51",
            "UY" => "598",
            "VE" => "58",
            "PR" => "1",
            "US" or "USA" => "1",
            "ES" => "34",
            _ => "505"
        };
    }

    /// <summary>
    /// Normaliza cualquier número de teléfono con el código de país del comercio para enlaces wa.me.
    /// Si un comercio en Nicaragua (NI) ingresa solo sus 8 dígitos locales (ej: 82310100),
    /// automáticamente le antepone 505, resultando en 50582310100.
    /// </summary>
    public static string FormatPhoneForWhatsApp(string? phone, string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits)) return string.Empty;

        var dialCode = GetDialCode(countryCode);

        // Si ya empieza con el código de país y tiene longitud internacional razonable
        if (digits.StartsWith(dialCode) && digits.Length >= dialCode.Length + 7)
        {
            return digits;
        }

        // Si empieza con 0 (común en Venezuela ej. 0414... o Ecuador ej. 09...), quitar el 0 inicial
        if (digits.StartsWith("0") && digits.Length >= 9)
        {
            digits = digits.Substring(1);
            if (digits.StartsWith(dialCode)) return digits;
        }

        // Si es un número local de 7 a 10 dígitos (ej. 8 dígitos en Centroamérica o 10 en Colombia/Venezuela)
        if (digits.Length >= 7 && digits.Length <= 10)
        {
            return dialCode + digits;
        }

        return digits;
    }
}
