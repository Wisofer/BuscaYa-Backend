using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuscaYa.Services.IServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace BuscaYa.Controllers.API;

[ApiController]
[Route("api/s3")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class S3BucketController : ControllerBase
{
    private readonly IS3BucketService _s3Service;

    public S3BucketController(IS3BucketService s3Service)
    {
        _s3Service = s3Service;
    }

    [HttpPost("image/webp")]
    public async Task<IActionResult> UploadImageToWebP([FromForm] string prefix, [FromForm] IFormFile image, [FromForm] string? previousImageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return BadRequest(new { error = "Prefix es requerido" });

        if (image == null || image.Length == 0)
            return BadRequest(new { error = "Imagen es requerida" });

        if (!_s3Service.IsValidImage(image))
            return BadRequest(new { error = "Formato de imagen no válido" });

        var url = await _s3Service.UploadImageToWebPAsync(prefix, image, previousImageUrl);

        if (url == null)
            return BadRequest(new { error = "No se pudo subir la imagen" });

        return Ok(new { url });
    }

    [HttpPost("image/jpg")]
    public async Task<IActionResult> UploadImageToJpg([FromForm] string prefix, [FromForm] IFormFile image, [FromForm] string? previousImageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return BadRequest(new { error = "Prefix es requerido" });

        if (image == null || image.Length == 0)
            return BadRequest(new { error = "Imagen es requerida" });

        if (!_s3Service.IsValidImage(image))
            return BadRequest(new { error = "Formato de imagen no válido" });

        var url = await _s3Service.UploadImageToJpgAsync(prefix, image, previousImageUrl);

        if (url == null)
            return BadRequest(new { error = "No se pudo subir la imagen" });

        return Ok(new { url });
    }

    [HttpPost("image")]
    public async Task<IActionResult> UploadImage([FromForm] string prefix, [FromForm] IFormFile image, [FromForm] string? previousImageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return BadRequest(new { error = "Prefix es requerido" });

        if (image == null || image.Length == 0)
            return BadRequest(new { error = "Imagen es requerida" });

        if (!_s3Service.IsValidImage(image))
            return BadRequest(new { error = "Formato de imagen no válido" });

        var url = await _s3Service.UploadImageAsync(prefix, image, previousImageUrl);

        if (url == null)
            return BadRequest(new { error = "No se pudo subir la imagen" });

        return Ok(new { url });
    }

    [HttpPost("image/base64")]
    public async Task<IActionResult> UploadImageFromBase64([FromBody] UploadBase64Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Prefix))
            return BadRequest(new { error = "Prefix es requerido" });

        if (string.IsNullOrWhiteSpace(request.ImageBase64))
            return BadRequest(new { error = "ImageBase64 es requerido" });

        var url = await _s3Service.UploadImageFromBase64ToJpgAsync(request.Prefix, request.ImageBase64, request.PreviousImageUrl);

        if (url == null)
            return BadRequest(new { error = "No se pudo subir la imagen" });

        return Ok(new { url });
    }

    [HttpPost("icon")]
    public async Task<IActionResult> UploadIcon([FromForm] string prefix, [FromForm] IFormFile image, [FromForm] string? previousImageUrl = null, [FromForm] int size = 200)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return BadRequest(new { error = "Prefix es requerido" });

        if (image == null || image.Length == 0)
            return BadRequest(new { error = "Imagen es requerida" });

        if (!_s3Service.IsValidImage(image))
            return BadRequest(new { error = "Formato de imagen no válido" });

        if (size < 32 || size > 512)
            return BadRequest(new { error = "El tamaño debe estar entre 32 y 512 píxeles" });

        var url = await _s3Service.UploadIconAsync(prefix, image, previousImageUrl, size);

        if (url == null)
            return BadRequest(new { error = "No se pudo generar el ícono" });

        return Ok(new { url });
    }

    [HttpDelete("image")]
    public async Task<IActionResult> DeleteImage([FromQuery] string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { error = "URL es requerida" });

        await _s3Service.DeleteFileIfExistsAsync(url);
        return Ok(new { mensaje = "Imagen eliminada" });
    }

    [HttpGet("folders")]
    public async Task<IActionResult> ListFolders([FromQuery] string? prefix = null)
    {
        var folders = await _s3Service.ListFoldersAsync(prefix);
        return Ok(folders);
    }

    [HttpGet("files")]
    public async Task<IActionResult> ListFiles([FromQuery] string? prefix = null, [FromQuery] bool recursive = false)
    {
        var files = await _s3Service.ListFilesAsync(prefix, recursive);
        return Ok(files);
    }
}

public class UploadBase64Request
{
    public string Prefix { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
    public string? PreviousImageUrl { get; set; }
}
