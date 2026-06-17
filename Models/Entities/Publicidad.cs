namespace BuscaYa.Models.Entities;

/// <summary>
/// Representa un banner/anuncio publicitario para el carrusel de la pantalla de inicio.
/// Soporta dos modos de visualización en la app móvil:
/// - Modo imagen: Se sube una imagen a R2 (ImageUrl != null). Flutter muestra la imagen full-bleed.
/// - Modo texto: Sin imagen (ImageUrl == null). Flutter muestra un gradiente nativo con Titulo y Subtitulo.
/// </summary>
public class Publicidad
{
    public int Id { get; set; }

    /// <summary>
    /// URL pública de la imagen en Cloudflare R2 (opcional).
    /// Si está presente, la app mostrará la imagen cubriendo el 100% de la tarjeta.
    /// Si es null, la app usará el diseño de gradiente nativo con los campos de texto.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Título principal de la promoción. Se usa solo si no hay ImageUrl.
    /// </summary>
    public string? Titulo { get; set; }

    /// <summary>
    /// Subtítulo descriptivo. Se usa solo si no hay ImageUrl.
    /// </summary>
    public string? Subtitulo { get; set; }

    /// <summary>
    /// Deeplink interno (ej. buscaya://tienda/12) o URL web a donde se redirige al pulsar el banner.
    /// Opcional en ambos modos.
    /// </summary>
    public string? AccionUrl { get; set; }

    /// <summary>
    /// Orden de aparición en el carrusel (menor = primero).
    /// </summary>
    public int Orden { get; set; } = 0;

    /// <summary>
    /// Indica si el banner está activo y debe mostrarse en la app.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Fecha de creación del registro.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
