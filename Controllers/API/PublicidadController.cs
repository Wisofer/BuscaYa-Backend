using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;

namespace BuscaYa.Controllers.API;

// ─────────────────────────────────────────────────────────────────────────────
// ENDPOINTS PÚBLICOS  →  GET /api/public/publicidades
// ─────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/public")]
public class PublicidadPublicController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PublicidadPublicController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retorna la lista de publicidades activas ordenadas para el carrusel de inicio.
    /// La app móvil usa el campo ImageUrl para decidir el modo de visualización:
    ///   - ImageUrl presente → imagen full-bleed.
    ///   - ImageUrl nulo     → gradiente nativo con Titulo y Subtitulo.
    /// </summary>
    [HttpGet("publicidades")]
    public async Task<IActionResult> ObtenerPublicidades()
    {
        try
        {
            var publicidades = await _db.Publicidades
                .Where(p => p.Activo)
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    p.ImageUrl,
                    p.Titulo,
                    p.Subtitulo,
                    p.AccionUrl,
                    p.Orden
                })
                .ToListAsync();

            return Ok(publicidades);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                errorMessages = new[] { "Error al obtener las publicidades" },
                error = "Error al obtener las publicidades",
                mensaje = ex.Message
            });
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ENDPOINTS ADMINISTRATIVOS  →  /api/admin/publicidades
// ─────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/admin/publicidades")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Administrador")]
public class PublicidadAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IS3BucketService? _s3;

    // Prefijo de carpeta en R2 para los banners publicitarios
    private const string R2Prefix = "publicidad/banners/";

    public PublicidadAdminController(ApplicationDbContext db, IS3BucketService? s3 = null)
    {
        _db = db;
        _s3 = s3;
    }

    // ── GET /api/admin/publicidades ──────────────────────────────────────────

    /// <summary>Lista todas las publicidades (activas e inactivas) para el panel.</summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerTodas()
    {
        try
        {
            var publicidades = await _db.Publicidades
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Id)
                .ToListAsync();

            return Ok(publicidades);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener publicidades", mensaje = ex.Message });
        }
    }

    // ── POST /api/admin/publicidades ─────────────────────────────────────────

    /// <summary>
    /// Crea una nueva publicidad.
    /// Si se adjunta un archivo en el campo "imagen" del multipart/form-data,
    /// se sube a Cloudflare R2 y su URL se guarda en ImageUrl
    /// (Titulo y Subtitulo quedan en null en ese caso).
    /// Si no se adjunta imagen, se usan Titulo y Subtitulo para el modo gradiente.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Crear([FromForm] CrearPublicidadRequest request)
    {
        try
        {
            var publicidad = new Publicidad
            {
                AccionUrl = request.AccionUrl?.Trim(),
                Orden     = request.Orden,
                Activo    = request.Activo,
                FechaCreacion = DateTime.UtcNow
            };

            if (request.Imagen != null && _s3 != null)
            {
                // Modo imagen: subir a R2 y guardar URL
                var url = await _s3.UploadImageToWebPAsync(R2Prefix, request.Imagen);
                if (url == null)
                    return BadRequest(new { error = "No se pudo subir la imagen. Verifique el formato (JPG, PNG, WebP)." });

                publicidad.ImageUrl = url;
                // En modo imagen los textos se dejan en null intencionalmente
                publicidad.Titulo    = null;
                publicidad.Subtitulo = null;
            }
            else
            {
                // Modo texto / gradiente nativo
                publicidad.Titulo    = request.Titulo?.Trim();
                publicidad.Subtitulo = request.Subtitulo?.Trim();
            }

            _db.Publicidades.Add(publicidad);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(ObtenerTodas), new { id = publicidad.Id }, publicidad);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al crear la publicidad", mensaje = ex.Message });
        }
    }

    // ── PUT /api/admin/publicidades/{id} ─────────────────────────────────────

    /// <summary>
    /// Modifica un anuncio existente.
    /// Si se envía una nueva imagen, reemplaza la anterior en R2 y actualiza ImageUrl.
    /// Si se envía solo texto (sin imagen), elimina ImageUrl existente y guarda los campos de texto.
    /// </summary>
    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Actualizar(int id, [FromForm] ActualizarPublicidadRequest request)
    {
        try
        {
            var publicidad = await _db.Publicidades.FindAsync(id);
            if (publicidad == null)
                return NotFound(new { error = "Publicidad no encontrada" });

            publicidad.AccionUrl = request.AccionUrl?.Trim();
            publicidad.Orden     = request.Orden;
            publicidad.Activo    = request.Activo;

            if (request.Imagen != null && _s3 != null)
            {
                // Nueva imagen: reemplaza la anterior en R2
                var urlAnterior = publicidad.ImageUrl;
                var url = await _s3.UploadImageToWebPAsync(R2Prefix, request.Imagen, urlAnterior);
                if (url == null)
                    return BadRequest(new { error = "No se pudo subir la imagen. Verifique el formato (JPG, PNG, WebP)." });

                publicidad.ImageUrl  = url;
                publicidad.Titulo    = null;
                publicidad.Subtitulo = null;
            }
            else if (request.EliminarImagen && _s3 != null && publicidad.ImageUrl != null)
            {
                // Forzar modo texto: borrar imagen de R2 y limpiar URL
                await _s3.DeleteFileIfExistsAsync(publicidad.ImageUrl);
                publicidad.ImageUrl  = null;
                publicidad.Titulo    = request.Titulo?.Trim();
                publicidad.Subtitulo = request.Subtitulo?.Trim();
            }
            else if (publicidad.ImageUrl == null)
            {
                // Sin imagen previa y sin nueva imagen → actualizar textos
                publicidad.Titulo    = request.Titulo?.Trim();
                publicidad.Subtitulo = request.Subtitulo?.Trim();
            }
            // Si ya había imagen y no se envía nueva ni se solicita eliminarla,
            // se conserva la imagen existente y se ignoran los campos de texto.

            await _db.SaveChangesAsync();

            return Ok(publicidad);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al actualizar la publicidad", mensaje = ex.Message });
        }
    }

    // ── DELETE /api/admin/publicidades/{id} ──────────────────────────────────

    /// <summary>
    /// Elimina un anuncio de la base de datos y, si tiene imagen, la borra de Cloudflare R2.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var publicidad = await _db.Publicidades.FindAsync(id);
            if (publicidad == null)
                return NotFound(new { error = "Publicidad no encontrada" });

            // Borrar imagen de R2 si existe
            if (_s3 != null && !string.IsNullOrEmpty(publicidad.ImageUrl))
                await _s3.DeleteFileIfExistsAsync(publicidad.ImageUrl);

            _db.Publicidades.Remove(publicidad);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Publicidad eliminada correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al eliminar la publicidad", mensaje = ex.Message });
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// DTOs de request
// ─────────────────────────────────────────────────────────────────────────────

public class CrearPublicidadRequest
{
    /// <summary>Archivo de imagen (JPG, PNG, WebP). Si se envía, activa el modo imagen.</summary>
    public IFormFile? Imagen { get; set; }

    /// <summary>Título para el modo gradiente (ignorado si se envía imagen).</summary>
    public string? Titulo { get; set; }

    /// <summary>Subtítulo para el modo gradiente (ignorado si se envía imagen).</summary>
    public string? Subtitulo { get; set; }

    /// <summary>Deeplink o URL de destino al pulsar el banner.</summary>
    public string? AccionUrl { get; set; }

    /// <summary>Posición en el carrusel (menor = primero). Default: 0.</summary>
    public int Orden { get; set; } = 0;

    /// <summary>Si es false, el banner no se muestra en la app. Default: true.</summary>
    public bool Activo { get; set; } = true;
}

public class ActualizarPublicidadRequest
{
    /// <summary>Nueva imagen para reemplazar la existente en R2.</summary>
    public IFormFile? Imagen { get; set; }

    /// <summary>
    /// Si es true y no se envía nueva imagen, elimina la imagen actual de R2
    /// y cambia el banner a modo texto/gradiente.
    /// </summary>
    public bool EliminarImagen { get; set; } = false;

    /// <summary>Nuevo título (solo aplica en modo gradiente).</summary>
    public string? Titulo { get; set; }

    /// <summary>Nuevo subtítulo (solo aplica en modo gradiente).</summary>
    public string? Subtitulo { get; set; }

    /// <summary>Deeplink o URL de destino al pulsar el banner.</summary>
    public string? AccionUrl { get; set; }

    /// <summary>Posición en el carrusel.</summary>
    public int Orden { get; set; } = 0;

    /// <summary>Activo/Inactivo.</summary>
    public bool Activo { get; set; } = true;
}
