using Microsoft.AspNetCore.Mvc;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace BuscaYa.Controllers.API;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly IS3BucketService _s3Service;

    public AuthApiController(IAuthService authService, IConfiguration configuration, IS3BucketService s3Service)
    {
        _authService = authService;
        _configuration = configuration;
        _s3Service = s3Service;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NombreUsuario) || string.IsNullOrWhiteSpace(request.Contrasena))
        {
            return BadRequest(new { error = "El nombre de usuario y la contraseña son requeridos" });
        }

        var usuario = _authService.ValidarUsuario(request.NombreUsuario, request.Contrasena);

        if (usuario == null)
        {
            return Unauthorized(new { error = "Usuario o contraseña incorrectos" });
        }

        if (!usuario.Activo)
        {
            return Unauthorized(new { error = "Usuario inactivo" });
        }

        // Obtener configuración JWT
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurada");
        var issuer = jwtSettings["Issuer"] ?? "BuscaYa";
        var audience = jwtSettings["Audience"] ?? "BuscaYaUsers";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60");

        // Generar token JWT
        var token = JwtHelper.GenerateToken(
            usuario.Id,
            usuario.NombreUsuario,
            usuario.Rol,
            usuario.NombreCompleto,
            secretKey,
            issuer,
            audience,
            expirationMinutes
        );

        return Ok(new LoginResponse
        {
            Token = token,
            Usuario = new UsuarioInfoResponse
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = usuario.NombreCompleto,
                Rol = usuario.Rol,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                FotoPerfilUrl = usuario.FotoPerfilUrl,
                TiendaId = usuario.TiendaId
            },
            ExpiraEn = expirationMinutes
        });
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Validar campos requeridos
            if (string.IsNullOrWhiteSpace(request.NombreUsuario) || 
                string.IsNullOrWhiteSpace(request.Contrasena) || 
                string.IsNullOrWhiteSpace(request.NombreCompleto))
            {
                return BadRequest(new { error = "El nombre de usuario, contraseña y nombre completo son requeridos" });
            }

            // Validar que el nombre de usuario no exista
            if (_authService.ExisteNombreUsuario(request.NombreUsuario))
            {
                return BadRequest(new { error = "El nombre de usuario ya está en uso" });
            }

            // Normalizar email: trim si existe
            string? emailNormalizado = string.IsNullOrWhiteSpace(request.Email) 
                ? null 
                : request.Email.Trim();

            // Registrar cliente
            var usuario = _authService.RegistrarCliente(
                request.NombreUsuario,
                request.Contrasena,
                request.NombreCompleto,
                request.Telefono,
                emailNormalizado
            );

            if (usuario == null)
            {
                return BadRequest(new { error = "Error al crear la cuenta" });
            }

            // Generar token JWT automáticamente después del registro
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurada");
            var issuer = jwtSettings["Issuer"] ?? "BuscaYa";
            var audience = jwtSettings["Audience"] ?? "BuscaYaUsers";
            var expirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60");

            var token = JwtHelper.GenerateToken(
                usuario.Id,
                usuario.NombreUsuario,
                usuario.Rol,
                usuario.NombreCompleto,
                secretKey,
                issuer,
                audience,
                expirationMinutes
            );

            return Ok(new LoginResponse
            {
                Token = token,
                Usuario = new UsuarioInfoResponse
                {
                    Id = usuario.Id,
                    NombreUsuario = usuario.NombreUsuario,
                    NombreCompleto = usuario.NombreCompleto,
                    Rol = usuario.Rol,
                    Email = usuario.Email,
                    Telefono = usuario.Telefono,
                    FotoPerfilUrl = usuario.FotoPerfilUrl,
                    TiendaId = usuario.TiendaId
                },
                ExpiraEn = expirationMinutes
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al registrar usuario", mensaje = ex.Message });
        }
    }

    [HttpGet("user")]
    [HttpGet("profile")] // Alias para compatibilidad con frontend
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult ObtenerPerfil()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { error = "Usuario no autenticado" });

            var usuario = _authService.ObtenerUsuarioPorId(userId);
            if (usuario == null)
                return NotFound(new { error = "Usuario no encontrado" });

            return Ok(new UsuarioInfoResponse
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = usuario.NombreCompleto,
                Rol = usuario.Rol,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                FotoPerfilUrl = usuario.FotoPerfilUrl,
                TiendaId = usuario.TiendaId
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener perfil", mensaje = ex.Message });
        }
    }

    [HttpPut("user")]
    [HttpPut("profile")] // Alias para compatibilidad con frontend
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult ActualizarPerfil([FromBody] ActualizarUsuarioRequest request)
    {
        try
        {
            // Obtener ID del usuario desde el token JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Usuario no autenticado" });
            }

            // Validar que el nombre completo no esté vacío
            if (string.IsNullOrWhiteSpace(request.NombreCompleto))
            {
                return BadRequest(new { error = "El nombre completo es requerido" });
            }

            // Normalizar email: trim
            string? emailNormalizado = null;
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                emailNormalizado = request.Email.Trim();
            }

            // Actualizar perfil (incl. fotoPerfilUrl si viene)
            var actualizado = _authService.ActualizarPerfil(userId, request.NombreCompleto, request.Telefono, emailNormalizado, request.FotoPerfilUrl);
            
            if (!actualizado)
            {
                return NotFound(new { error = "Usuario no encontrado" });
            }

            // Obtener usuario actualizado para devolverlo
            var usuario = _authService.ObtenerUsuarioPorId(userId);
            if (usuario == null)
            {
                return NotFound(new { error = "Usuario no encontrado" });
            }

            return Ok(new
            {
                mensaje = "Perfil actualizado correctamente",
                usuario = new UsuarioInfoResponse
                {
                    Id = usuario.Id,
                    NombreUsuario = usuario.NombreUsuario,
                    NombreCompleto = usuario.NombreCompleto,
                    Rol = usuario.Rol,
                    Email = usuario.Email,
                    Telefono = usuario.Telefono,
                    FotoPerfilUrl = usuario.FotoPerfilUrl,
                    TiendaId = usuario.TiendaId
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al actualizar perfil", mensaje = ex.Message });
        }
    }

    /// <summary>Sube la foto de perfil del usuario (base64). Sube a S3 y actualiza el perfil en una sola llamada.</summary>
    [HttpPost("user/foto")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> SubirFotoPerfilAsync([FromBody] SubirFotoPerfilRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { error = "Usuario no autenticado" });

            if (string.IsNullOrWhiteSpace(request.ImageBase64))
                return BadRequest(new { error = "ImageBase64 es requerido" });

            var usuario = _authService.ObtenerUsuarioPorId(userId);
            if (usuario == null)
                return NotFound(new { error = "Usuario no encontrado" });

            var url = await _s3Service.UploadImageFromBase64ToJpgAsync("perfil/", request.ImageBase64, usuario.FotoPerfilUrl);
            if (url == null)
                return BadRequest(new { error = "No se pudo subir la imagen. Formato o tamaño no válido." });

            _authService.ActualizarPerfil(userId, usuario.NombreCompleto, usuario.Telefono, usuario.Email, url);
            var actualizado = _authService.ObtenerUsuarioPorId(userId);

            return Ok(new
            {
                mensaje = "Foto de perfil actualizada",
                url,
                usuario = new UsuarioInfoResponse
                {
                    Id = actualizado!.Id,
                    NombreUsuario = actualizado.NombreUsuario,
                    NombreCompleto = actualizado.NombreCompleto,
                    Rol = actualizado.Rol,
                    Email = actualizado.Email,
                    Telefono = actualizado.Telefono,
                    FotoPerfilUrl = actualizado.FotoPerfilUrl,
                    TiendaId = actualizado.TiendaId
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al subir foto de perfil", mensaje = ex.Message });
        }
    }
}

public class SubirFotoPerfilRequest
{
    public string ImageBase64 { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
}


public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UsuarioInfoResponse Usuario { get; set; } = null!;
    public int ExpiraEn { get; set; }
}

public class UsuarioInfoResponse
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? FotoPerfilUrl { get; set; }
    public int? TiendaId { get; set; }
}

public class ActualizarUsuarioRequest
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? FotoPerfilUrl { get; set; }
}
