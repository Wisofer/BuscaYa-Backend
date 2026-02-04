using Microsoft.AspNetCore.Mvc;
using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Models.DTOs.Responses;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace BuscaYa.Controllers.API;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly IBusquedaService _busquedaService;
    private readonly ITiendaService _tiendaService;
    private readonly ICategoriaService _categoriaService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IProductoService _productoService;

    public PublicController(
        IBusquedaService busquedaService,
        ITiendaService tiendaService,
        ICategoriaService categoriaService,
        IAnalyticsService analyticsService,
        IProductoService productoService)
    {
        _busquedaService = busquedaService;
        _tiendaService = tiendaService;
        _categoriaService = categoriaService;
        _analyticsService = analyticsService;
        _productoService = productoService;
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
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
            // El frontend móvil espera un arreglo "errorMessages" para mostrar el mensaje al usuario.
            return StatusCode(500, new
            {
                errorMessages = new[] { "Error al realizar la búsqueda" },
                error = "Error al realizar la búsqueda",
                mensaje = ex.Message
            });
        }
    }

    [HttpGet("tienda/{id}")]
    public IActionResult ObtenerTienda(int id, [FromQuery] decimal? lat, [FromQuery] decimal? lng)
    {
        try
        {
            var tienda = _tiendaService.ObtenerDetalle(id, lat, lng);
            if (tienda == null)
            {
                return NotFound(new
                {
                    errorMessages = new[] { "Tienda no encontrada" },
                    error = "Tienda no encontrada"
                });
            }

            // Registrar vista
            _analyticsService.RegistrarEvento(id, SD.EventoVistaTienda);

            return Ok(tienda);
        }
        catch (Exception ex)
        {
            // Estructura de error compatible con el frontend móvil
            return StatusCode(500, new
            {
                errorMessages = new[] { "Error al obtener la tienda" },
                error = "Error al obtener la tienda",
                mensaje = ex.Message
            });
        }
    }

    [HttpPost("tienda/{id}/calificar")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult CalificarTienda(int id, [FromBody] CalificarTiendaRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized(new { error = "Usuario no autenticado" });

            _tiendaService.CalificarTienda(id, userId.Value, request.Valor);

            return Ok(new { mensaje = "Calificación registrada correctamente" });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al calificar la tienda", mensaje = ex.Message });
        }
    }

    [HttpGet("tienda/{id}/calificacion")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult ObtenerCalificacionUsuario(int id)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized(new { error = "Usuario no autenticado" });

            var valor = _tiendaService.ObtenerCalificacionUsuario(id, userId.Value);
            return Ok(new { valor });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener la calificación", mensaje = ex.Message });
        }
    }

    [HttpGet("producto/{id}")]
    public IActionResult ObtenerProducto(int id, [FromQuery] decimal? lat, [FromQuery] decimal? lng)
    {
        try
        {
            var producto = _productoService.ObtenerPorId(id);
            
            if (producto == null || !producto.Activo)
                return NotFound(new { error = "Producto no encontrado" });

            if (producto.Tienda == null || !producto.Tienda.Activo)
                return NotFound(new { error = "Tienda no encontrada o inactiva" });

            // Calcular distancia si se proporcionan coordenadas
            double? distancia = null;
            if (lat.HasValue && lng.HasValue)
            {
                distancia = GeolocationHelper.CalcularDistanciaKm(
                    lat.Value, lng.Value,
                    producto.Tienda.Latitud, producto.Tienda.Longitud);
            }

            // Generar link de WhatsApp con mensaje personalizado para el producto
            var whatsappUrl = WhatsAppHelper.GenerarLinkProducto(
                producto.Tienda.WhatsApp,
                producto.Nombre,
                producto.Precio,
                producto.Moneda
            );

            var galeriaUrls = new List<string>();
            if (!string.IsNullOrEmpty(producto.FotoUrl))
                galeriaUrls.Add(producto.FotoUrl);
            if (producto.Imagenes != null)
            {
                var otras = producto.Imagenes.OrderBy(i => i.Orden).Select(i => i.Url).Where(u => u != producto.FotoUrl).ToList();
                galeriaUrls.AddRange(otras);
            }

            // Mapear a ProductoResponse
            var response = new ProductoResponse
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Moneda = producto.Moneda,
                FotoUrl = producto.FotoUrl,
                GaleriaUrls = galeriaUrls,
                Tienda = new TiendaInfoResponse
                {
                    Id = producto.Tienda.Id,
                    Nombre = producto.Tienda.Nombre,
                    Direccion = producto.Tienda.Direccion,
                    Ciudad = producto.Tienda.Ciudad,
                    WhatsApp = producto.Tienda.WhatsApp,
                    Telefono = producto.Tienda.Telefono,
                    LogoUrl = producto.Tienda.LogoUrl,
                    Latitud = producto.Tienda.Latitud,
                    Longitud = producto.Tienda.Longitud,
                    WhatsAppUrl = whatsappUrl // Link personalizado con mensaje del producto
                },
                Categoria = new CategoriaInfoResponse
                {
                    Id = producto.Categoria.Id,
                    Nombre = producto.Categoria.Nombre,
                    Icono = producto.Categoria.Icono
                },
                DistanciaKm = distancia
            };

            return Ok(response);
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
