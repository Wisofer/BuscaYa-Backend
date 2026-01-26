using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Controllers.API;

[ApiController]
[Route("api/cliente")]
[Authorize]
public class ClienteController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;

    public ClienteController(ApplicationDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [HttpGet("favoritos")]
    public IActionResult ObtenerFavoritos()
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var favoritos = _context.Favoritos
                .Where(f => f.UsuarioId == userId.Value)
                .Include(f => f.Tienda)
                .Include(f => f.Producto)
                .ThenInclude(p => p!.Tienda)
                .OrderByDescending(f => f.FechaCreacion)
                .ToList();

            return Ok(favoritos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener favoritos", mensaje = ex.Message });
        }
    }

    [HttpPost("favoritos/tienda/{tiendaId}")]
    public IActionResult AgregarFavoritoTienda(int tiendaId)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var existe = _context.Favoritos
                .Any(f => f.UsuarioId == userId.Value && f.TiendaId == tiendaId);

            if (existe)
                return BadRequest(new { error = "Ya está en favoritos" });

            var favorito = new Favorito
            {
                UsuarioId = userId.Value,
                TiendaId = tiendaId
            };

            _context.Favoritos.Add(favorito);
            _context.SaveChanges();

            return Ok(new { mensaje = "Tienda agregada a favoritos" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al agregar favorito", mensaje = ex.Message });
        }
    }

    [HttpPost("favoritos/producto/{productoId}")]
    public IActionResult AgregarFavoritoProducto(int productoId)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var existe = _context.Favoritos
                .Any(f => f.UsuarioId == userId.Value && f.ProductoId == productoId);

            if (existe)
                return BadRequest(new { error = "Ya está en favoritos" });

            var favorito = new Favorito
            {
                UsuarioId = userId.Value,
                ProductoId = productoId
            };

            _context.Favoritos.Add(favorito);
            _context.SaveChanges();

            return Ok(new { mensaje = "Producto agregado a favoritos" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al agregar favorito", mensaje = ex.Message });
        }
    }

    [HttpDelete("favoritos/{id}")]
    public IActionResult EliminarFavorito(int id)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var favorito = _context.Favoritos
                .FirstOrDefault(f => f.Id == id && f.UsuarioId == userId.Value);

            if (favorito == null)
                return NotFound(new { error = "Favorito no encontrado" });

            _context.Favoritos.Remove(favorito);
            _context.SaveChanges();

            return Ok(new { mensaje = "Favorito eliminado" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al eliminar favorito", mensaje = ex.Message });
        }
    }

    [HttpGet("historial")]
    public IActionResult ObtenerHistorial([FromQuery] int limite = 20)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var historial = _context.HistorialBusquedas
                .Where(h => h.UsuarioId == userId.Value)
                .OrderByDescending(h => h.Fecha)
                .Take(limite)
                .ToList();

            return Ok(historial);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener historial", mensaje = ex.Message });
        }
    }

    [HttpGet("direcciones")]
    public IActionResult ObtenerDirecciones()
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var direcciones = _context.DireccionesGuardadas
                .Where(d => d.UsuarioId == userId.Value)
                .OrderByDescending(d => d.EsPrincipal)
                .ThenBy(d => d.Nombre)
                .ToList();

            return Ok(direcciones);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener direcciones", mensaje = ex.Message });
        }
    }

    [HttpPost("direcciones")]
    public IActionResult CrearDireccion([FromBody] CrearDireccionRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var direccion = new DireccionGuardada
            {
                UsuarioId = userId.Value,
                Nombre = request.Nombre,
                Direccion = request.Direccion,
                Latitud = request.Latitud,
                Longitud = request.Longitud,
                EsPrincipal = request.EsPrincipal
            };

            // Si es principal, quitar principal de otras
            if (request.EsPrincipal)
            {
                var otras = _context.DireccionesGuardadas
                    .Where(d => d.UsuarioId == userId.Value && d.EsPrincipal);
                foreach (var otra in otras)
                {
                    otra.EsPrincipal = false;
                }
            }

            _context.DireccionesGuardadas.Add(direccion);
            _context.SaveChanges();

            return Ok(direccion);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al crear dirección", mensaje = ex.Message });
        }
    }

    [HttpPost("crear-tienda")]
    public IActionResult CrearTienda([FromBody] CrearTiendaRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized(new { error = "Usuario no autenticado" });

            // Obtener usuario actual
            var usuario = _authService.ObtenerUsuarioPorId(userId.Value);
            if (usuario == null)
                return NotFound(new { error = "Usuario no encontrado" });

            // Validar que sea Cliente (no puede crear tienda si ya es TiendaOwner)
            if (usuario.Rol != SD.RolCliente)
            {
                return BadRequest(new { error = "Ya tienes una tienda asociada" });
            }

            // Validar campos requeridos de la tienda
            if (string.IsNullOrWhiteSpace(request.NombreTienda) || 
                string.IsNullOrWhiteSpace(request.WhatsAppTienda) || 
                string.IsNullOrWhiteSpace(request.DireccionTienda) ||
                string.IsNullOrWhiteSpace(request.Ciudad) ||
                string.IsNullOrWhiteSpace(request.Departamento))
            {
                return BadRequest(new { error = "Los datos de la tienda son requeridos" });
            }

            // Convertir Cliente a TiendaOwner y crear tienda
            var usuarioActualizado = _authService.ConvertirClienteATienda(
                userId.Value,
                request.NombreTienda,
                request.DescripcionTienda,
                request.TelefonoTienda,
                request.WhatsAppTienda,
                request.EmailTienda,
                request.DireccionTienda,
                request.Latitud,
                request.Longitud,
                request.Ciudad,
                request.Departamento,
                request.HorarioApertura,
                request.HorarioCierre,
                request.DiasAtencion,
                request.LogoTienda,
                request.FotoTienda
            );

            if (usuarioActualizado == null)
            {
                return BadRequest(new { error = "Error al crear la tienda. Verifica que no tengas una tienda ya asociada." });
            }

            return Ok(new
            {
                mensaje = "Tienda creada exitosamente",
                usuario = new
                {
                    id = usuarioActualizado.Id,
                    nombreUsuario = usuarioActualizado.NombreUsuario,
                    nombreCompleto = usuarioActualizado.NombreCompleto,
                    rol = usuarioActualizado.Rol,
                    tiendaId = usuarioActualizado.TiendaId
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al crear tienda", mensaje = ex.Message });
        }
    }
}

public class CrearDireccionRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public bool EsPrincipal { get; set; } = false;
}

public class CrearTiendaRequest
{
    public string NombreTienda { get; set; } = string.Empty;
    public string? DescripcionTienda { get; set; }
    public string? TelefonoTienda { get; set; }
    public string WhatsAppTienda { get; set; } = string.Empty;
    public string? EmailTienda { get; set; }
    public string DireccionTienda { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public string Ciudad { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public TimeSpan? HorarioApertura { get; set; }
    public TimeSpan? HorarioCierre { get; set; }
    public string? DiasAtencion { get; set; }
    public string? LogoTienda { get; set; }
    public string? FotoTienda { get; set; }
}
