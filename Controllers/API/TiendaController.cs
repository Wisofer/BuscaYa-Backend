using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Models.DTOs.Responses;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace BuscaYa.Controllers.API;

[ApiController]
[Route("api/tienda")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TiendaController : ControllerBase
{
    private readonly ITiendaService _tiendaService;
    private readonly IProductoService _productoService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IAuthService _authService;

    public TiendaController(
        ITiendaService tiendaService,
        IProductoService productoService,
        IAnalyticsService analyticsService,
        IAuthService authService)
    {
        _tiendaService = tiendaService;
        _productoService = productoService;
        _analyticsService = analyticsService;
        _authService = authService;
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private int? GetTiendaId()
    {
        var userId = GetUserId();
        if (!userId.HasValue) return null;

        var usuario = _authService.ObtenerUsuarioPorId(userId.Value);
        return usuario?.TiendaId;
    }

    [HttpGet("perfil")]
    public IActionResult ObtenerPerfil()
    {
        try
        {
            var tiendaId = GetTiendaId();
            if (!tiendaId.HasValue)
                return Unauthorized(new { error = "No tienes una tienda asociada" });

            var tienda = _tiendaService.ObtenerDetalle(tiendaId.Value);
            if (tienda == null)
                return NotFound(new { error = "Tienda no encontrada" });

            return Ok(tienda);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener perfil", mensaje = ex.Message });
        }
    }

    [HttpPut("perfil")]
    public IActionResult ActualizarPerfil([FromBody] ActualizarTiendaRequest request)
    {
        try
        {
            var tiendaId = GetTiendaId();
            if (!tiendaId.HasValue)
                return Unauthorized(new { error = "No tienes una tienda asociada" });

            var actualizado = _tiendaService.Actualizar(tiendaId.Value, request);
            if (!actualizado)
                return NotFound(new { error = "Tienda no encontrada" });

            return Ok(new { mensaje = "Perfil actualizado correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al actualizar perfil", mensaje = ex.Message });
        }
    }

    [HttpPatch("estado")]
    public IActionResult ActualizarEstado([FromBody] ActualizarEstadoTiendaRequest request)
    {
        try
        {
            var tiendaId = GetTiendaId();
            if (!tiendaId.HasValue)
                return Unauthorized(new { error = "No tienes una tienda asociada" });

            var actualizado = _tiendaService.ActualizarEstado(tiendaId.Value, request.EstaAbiertaManual);
            if (!actualizado)
                return NotFound(new { error = "Tienda no encontrada" });

            return Ok(new
            {
                mensaje = "Estado actualizado correctamente",
                estaAbierta = request.EstaAbiertaManual
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al actualizar estado", mensaje = ex.Message });
        }
    }

    [HttpGet("productos")]
    public IActionResult ObtenerProductos()
    {
        try
        {
            var tiendaId = GetTiendaId();
            if (!tiendaId.HasValue)
                return Unauthorized(new { error = "No tienes una tienda asociada" });

            var productos = _productoService.ObtenerPorTienda(tiendaId.Value);
            return Ok(productos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener productos", mensaje = ex.Message });
        }
    }

    [HttpPost("productos")]
    public IActionResult CrearProducto([FromBody] CrearProductoRequest request)
    {
        try
        {
            var tiendaId = GetTiendaId();
            if (!tiendaId.HasValue)
                return Unauthorized(new { error = "No tienes una tienda asociada" });

            var producto = _productoService.Crear(tiendaId.Value, request);
            return CreatedAtAction(nameof(ObtenerProducto), new { id = producto.Id }, producto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al crear producto", mensaje = ex.Message });
        }
    }

    [HttpGet("productos/{id}")]
    public IActionResult ObtenerProducto(int id)
    {
        try
        {
            var tiendaId = GetTiendaId();
            if (!tiendaId.HasValue)
                return Unauthorized(new { error = "No tienes una tienda asociada" });

            var producto = _productoService.ObtenerPorId(id);
            if (producto == null)
                return NotFound(new { error = "Producto no encontrado" });

            if (!_productoService.VerificarPerteneceATienda(id, tiendaId.Value))
                return Forbid();

            return Ok(producto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener producto", mensaje = ex.Message });
        }
    }

    [HttpPut("productos/{id}")]
    public IActionResult ActualizarProducto(int id, [FromBody] ActualizarProductoRequest request)
    {
        try
        {
            var tiendaId = GetTiendaId();
            if (!tiendaId.HasValue)
                return Unauthorized(new { error = "No tienes una tienda asociada" });

            if (!_productoService.VerificarPerteneceATienda(id, tiendaId.Value))
                return Forbid();

            var actualizado = _productoService.Actualizar(id, request);
            if (!actualizado)
                return NotFound(new { error = "Producto no encontrado" });

            return Ok(new { mensaje = "Producto actualizado correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al actualizar producto", mensaje = ex.Message });
        }
    }

    [HttpDelete("productos/{id}")]
    public IActionResult EliminarProducto(int id)
    {
        try
        {
            var tiendaId = GetTiendaId();
            if (!tiendaId.HasValue)
                return Unauthorized(new { error = "No tienes una tienda asociada" });

            if (!_productoService.VerificarPerteneceATienda(id, tiendaId.Value))
                return Forbid();

            var eliminado = _productoService.Eliminar(id);
            if (!eliminado)
                return NotFound(new { error = "Producto no encontrado" });

            return Ok(new { mensaje = "Producto eliminado correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al eliminar producto", mensaje = ex.Message });
        }
    }

    [HttpGet("estadisticas")]
    public IActionResult ObtenerEstadisticas([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        try
        {
            var tiendaId = GetTiendaId();
            if (!tiendaId.HasValue)
                return Unauthorized(new { error = "No tienes una tienda asociada" });

            var desdeFecha = desde ?? DateTime.Now.AddDays(-30);
            var hastaFecha = hasta ?? DateTime.Now;

            var estadisticas = _analyticsService.ObtenerEstadisticasTienda(tiendaId.Value, desdeFecha, hastaFecha);
            return Ok(estadisticas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener estadísticas", mensaje = ex.Message });
        }
    }
}
