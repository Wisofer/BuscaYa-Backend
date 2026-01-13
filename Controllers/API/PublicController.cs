using Microsoft.AspNetCore.Mvc;
using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Models.DTOs.Responses;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;

namespace BuscaYa.Controllers.API;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly IBusquedaService _busquedaService;
    private readonly ITiendaService _tiendaService;
    private readonly ICategoriaService _categoriaService;
    private readonly IAnalyticsService _analyticsService;

    public PublicController(
        IBusquedaService busquedaService,
        ITiendaService tiendaService,
        ICategoriaService categoriaService,
        IAnalyticsService analyticsService)
    {
        _busquedaService = busquedaService;
        _tiendaService = tiendaService;
        _categoriaService = categoriaService;
        _analyticsService = analyticsService;
    }

    [HttpGet("buscar")]
    public IActionResult Buscar([FromQuery] BuscarRequest request)
    {
        try
        {
            var response = _busquedaService.BuscarProductos(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al realizar la búsqueda", mensaje = ex.Message });
        }
    }

    [HttpGet("tienda/{id}")]
    public IActionResult ObtenerTienda(int id, [FromQuery] decimal? lat, [FromQuery] decimal? lng)
    {
        try
        {
            var tienda = _tiendaService.ObtenerDetalle(id, lat, lng);
            if (tienda == null)
                return NotFound(new { error = "Tienda no encontrada" });

            // Registrar vista
            _analyticsService.RegistrarEvento(id, SD.EventoVistaTienda);

            return Ok(tienda);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener la tienda", mensaje = ex.Message });
        }
    }

    [HttpGet("producto/{id}")]
    public IActionResult ObtenerProducto(int id)
    {
        try
        {
            // Implementar si es necesario
            return Ok(new { mensaje = "Endpoint en desarrollo" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener el producto", mensaje = ex.Message });
        }
    }

    [HttpGet("categorias")]
    public IActionResult ObtenerCategorias()
    {
        try
        {
            var categorias = _categoriaService.ObtenerActivas();
            return Ok(categorias);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener categorías", mensaje = ex.Message });
        }
    }

    [HttpGet("sugerencias")]
    public IActionResult Sugerencias([FromQuery] string termino, [FromQuery] int limite = 10)
    {
        try
        {
            var sugerencias = _busquedaService.Sugerencias(termino, limite);
            return Ok(sugerencias);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener sugerencias", mensaje = ex.Message });
        }
    }

    [HttpPost("analytics/evento")]
    public IActionResult RegistrarEvento([FromBody] RegistrarEventoRequest request)
    {
        try
        {
            _analyticsService.RegistrarEvento(
                request.TiendaId,
                request.TipoEvento,
                request.DatosAdicionales);

            return Ok(new { mensaje = "Evento registrado" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al registrar evento", mensaje = ex.Message });
        }
    }
}

public class RegistrarEventoRequest
{
    public int TiendaId { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public string? DatosAdicionales { get; set; }
}
