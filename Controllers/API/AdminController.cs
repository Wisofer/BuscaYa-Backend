using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace BuscaYa.Controllers.API;

[ApiController]
[Route("api/admin")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Administrador")]
public class AdminController : ControllerBase
{
    private readonly ITiendaService _tiendaService;
    private readonly ICategoriaService _categoriaService;
    private readonly IAnalyticsService _analyticsService;

    public AdminController(
        ITiendaService tiendaService,
        ICategoriaService categoriaService,
        IAnalyticsService analyticsService)
    {
        _tiendaService = tiendaService;
        _categoriaService = categoriaService;
        _analyticsService = analyticsService;
    }

    [HttpGet("tiendas")]
    public IActionResult ObtenerTiendas([FromQuery] string? ciudad, [FromQuery] bool? activo)
    {
        try
        {
            var tiendas = _tiendaService.ObtenerTodas();

            if (!string.IsNullOrEmpty(ciudad))
                tiendas = tiendas.Where(t => t.Ciudad == ciudad).ToList();

            if (activo.HasValue)
                tiendas = tiendas.Where(t => t.Activo == activo.Value).ToList();

            return Ok(tiendas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener tiendas", mensaje = ex.Message });
        }
    }

    [HttpPut("tiendas/{id}/activar")]
    public IActionResult ActivarTienda(int id)
    {
        try
        {
            var activado = _tiendaService.Activar(id);
            if (!activado)
                return NotFound(new { error = "Tienda no encontrada" });

            return Ok(new { mensaje = "Tienda activada correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al activar tienda", mensaje = ex.Message });
        }
    }

    [HttpPut("tiendas/{id}/desactivar")]
    public IActionResult DesactivarTienda(int id)
    {
        try
        {
            var desactivado = _tiendaService.Desactivar(id);
            if (!desactivado)
                return NotFound(new { error = "Tienda no encontrada" });

            return Ok(new { mensaje = "Tienda desactivada correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al desactivar tienda", mensaje = ex.Message });
        }
    }

    [HttpPut("tiendas/{id}/plan")]
    public IActionResult CambiarPlan(int id, [FromBody] CambiarPlanRequest request)
    {
        try
        {
            if (request.Plan != SD.PlanFree && request.Plan != SD.PlanPro)
                return BadRequest(new { error = "Plan inválido" });

            var cambiado = _tiendaService.CambiarPlan(id, request.Plan);
            if (!cambiado)
                return NotFound(new { error = "Tienda no encontrada" });

            return Ok(new { mensaje = $"Plan cambiado a {request.Plan}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al cambiar plan", mensaje = ex.Message });
        }
    }

    [HttpGet("estadisticas")]
    public IActionResult ObtenerEstadisticas()
    {
        try
        {
            var productosBuscados = _analyticsService.ObtenerProductosMasBuscados(null, 20);
            return Ok(new { productosMasBuscados = productosBuscados });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener estadísticas", mensaje = ex.Message });
        }
    }

    [HttpGet("categorias")]
    public IActionResult ObtenerCategorias()
    {
        try
        {
            var categorias = _categoriaService.ObtenerTodas();
            return Ok(categorias);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener categorías", mensaje = ex.Message });
        }
    }

    [HttpPost("categorias")]
    public IActionResult CrearCategoria([FromBody] CrearCategoriaRequest request)
    {
        try
        {
            var categoria = _categoriaService.Crear(request.Nombre, request.Icono);
            return CreatedAtAction(nameof(ObtenerCategorias), new { id = categoria.Id }, categoria);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al crear categoría", mensaje = ex.Message });
        }
    }
}

public class CambiarPlanRequest
{
    public string Plan { get; set; } = string.Empty;
}

public class CrearCategoriaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Icono { get; set; }
}
