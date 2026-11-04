using System.Net;

namespace BuscaYa.Utils;

/// <summary>
/// Helper para generar links de WhatsApp con mensajes personalizados
/// </summary>
public static class WhatsAppHelper
{
    /// <summary>
    /// Genera un link de WhatsApp con un mensaje personalizado para un producto
    /// </summary>
    /// <param name="whatsappNumber">Número de WhatsApp (puede ser local o internacional)</param>
    /// <param name="nombreProducto">Nombre del producto</param>
    /// <param name="precio">Precio del producto (opcional)</param>
    /// <param name="moneda">Moneda del producto (opcional, default: C$)</param>
    /// <param name="countryCode">Código ISO del país de la tienda (opcional, ej: NI, CR, VE)</param>
    /// <returns>URL de WhatsApp con mensaje personalizado</returns>
    public static string GenerarLinkProducto(string whatsappNumber, string nombreProducto, decimal? precio = null, string moneda = "C$", string? countryCode = null)
    {
        // Normalizar el número con el código de país internacional
        var numeroLimpio = RegionCatalog.FormatPhoneForWhatsApp(whatsappNumber, countryCode);
        if (string.IsNullOrEmpty(numeroLimpio))
        {
            numeroLimpio = LimpiarNumeroWhatsApp(whatsappNumber);
        }
        
        // Generar mensaje personalizado
        var mensaje = GenerarMensajeProducto(nombreProducto, precio, moneda);
        
        // Codificar el mensaje para URL
        var mensajeCodificado = WebUtility.UrlEncode(mensaje);
        
        // Generar link de WhatsApp
        return $"https://wa.me/{numeroLimpio}?text={mensajeCodificado}";
    }

    /// <summary>
    /// Genera un mensaje personalizado para un producto
    /// </summary>
    private static string GenerarMensajeProducto(string nombreProducto, decimal? precio, string moneda)
    {
        var mensaje = $"Hola! 👋\n\n";
        mensaje += $"Vi tu producto *{nombreProducto}* en BuscaYa";
        
        if (precio.HasValue)
        {
            mensaje += $" ({moneda} {precio.Value:N2})";
        }
        
        mensaje += " y me gustaría saber si aún lo tienes disponible.\n\n";
        mensaje += "¿Podrías darme más información?\n\n";
        mensaje += "Gracias! 😊";
        
        return mensaje;
    }

    /// <summary>
    /// Limpia el número de WhatsApp removiendo caracteres no numéricos
    /// </summary>
    private static string LimpiarNumeroWhatsApp(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return string.Empty;

        // Remover todos los caracteres que no sean dígitos
        var numeroLimpio = new string(numero.Where(char.IsDigit).ToArray());
        
        return numeroLimpio;
    }

    /// <summary>
    /// Genera un link de WhatsApp simple (sin mensaje personalizado)
    /// </summary>
    /// <param name="whatsappNumber">Número de WhatsApp</param>
    /// <param name="countryCode">Código ISO del país de la tienda (opcional, ej: NI, CR, VE)</param>
    /// <returns>URL de WhatsApp</returns>
    public static string GenerarLinkSimple(string whatsappNumber, string? countryCode = null)
    {
        var numeroLimpio = RegionCatalog.FormatPhoneForWhatsApp(whatsappNumber, countryCode);
        if (string.IsNullOrEmpty(numeroLimpio))
        {
            numeroLimpio = LimpiarNumeroWhatsApp(whatsappNumber);
        }
        return $"https://wa.me/{numeroLimpio}";
    }
}
