using Microsoft.AspNetCore.Mvc;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using Microsoft.Extensions.Configuration;

namespace BuscaYa.Controllers.API;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthApiController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
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
                TiendaId = usuario.TiendaId
            },
            ExpiraEn = expirationMinutes
        });
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        // Por ahora solo permitir registro de clientes
        // Los dueños de tienda y admin se crean desde el panel admin
        return BadRequest(new { error = "Registro no disponible. Contacta al administrador." });
    }
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
    public int? TiendaId { get; set; }
}
