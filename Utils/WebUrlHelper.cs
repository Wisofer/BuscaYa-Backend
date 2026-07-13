namespace BuscaYa.Utils;

/// <summary>
/// Helper para generar URLs públicas de la plataforma web (Next.js) de BuscaYa.
/// Evita tener URLs hardcodeadas tanto en el backend como en los clientes móviles.
/// </summary>
public static class WebUrlHelper
{
    private static string _webBaseUrl = "https://buscaya.cowib.es"; // Valor por defecto

    /// <summary>
    /// Inicializa la URL base desde la configuración del sistema (Program.cs).
    /// </summary>
    public static void Initialize(string? webBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(webBaseUrl))
        {
            _webBaseUrl = webBaseUrl.Trim().TrimEnd('/');
        }
    }

    /// <summary>
    /// Genera la URL pública de una tienda/negocio.
    /// </summary>
    public static string GenerarUrlTienda(string? tiendaSlug)
    {
        if (string.IsNullOrWhiteSpace(tiendaSlug))
            return string.Empty;

        return $"{_webBaseUrl}/s/{tiendaSlug}";
    }

    /// <summary>
    /// Genera la URL pública de un producto.
    /// </summary>
    public static string GenerarUrlProducto(string? tiendaSlug, string? productoSlug)
    {
        if (string.IsNullOrWhiteSpace(tiendaSlug) || string.IsNullOrWhiteSpace(productoSlug))
            return string.Empty;

        return $"{_webBaseUrl}/s/{tiendaSlug}/{productoSlug}";
    }
}
