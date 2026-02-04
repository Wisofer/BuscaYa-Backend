using Microsoft.AspNetCore.Mvc;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;

namespace BuscaYa.Controllers.Web;

public class PublicController : Controller
{
    private readonly IProductoService _productoService;
    private readonly ITiendaService _tiendaService;

    public PublicController(IProductoService productoService, ITiendaService tiendaService)
    {
        _productoService = productoService;
        _tiendaService = tiendaService;
    }

    [HttpGet("/producto/{id}")]
    public IActionResult VerProducto(int id)
    {
        try
        {
            var producto = _productoService.ObtenerPorId(id);
            
            if (producto == null || !producto.Activo)
            {
                return View("ProductoNoEncontrado");
            }

            if (producto.Tienda == null || !producto.Tienda.Activo)
            {
                return View("ProductoNoEncontrado");
            }

            // Generar link de WhatsApp con mensaje personalizado
            var whatsappUrl = WhatsAppHelper.GenerarLinkProducto(
                producto.Tienda.WhatsApp,
                producto.Nombre,
                producto.Precio,
                producto.Moneda ?? "C$"
            );

            // Preparar datos para la vista
            ViewData["Producto"] = producto;
            ViewData["WhatsAppUrl"] = whatsappUrl;
            ViewData["DeepLink"] = $"buscaya://producto/{id}";

            return View("Producto");
        }
        catch
        {
            // Log del error (puedes agregar ILogger si quieres)
            return View("ProductoNoEncontrado");
        }
    }

    /// <summary>
    /// Detalle público de tienda para compartir:
    /// GET /tienda/{id}
    /// </summary>
    [HttpGet("/tienda/{id}")]
    public IActionResult VerTienda(int id)
    {
        try
        {
            var tienda = _tiendaService.ObtenerDetalle(id);
            if (tienda == null)
            {
                return View("TiendaNoEncontrada");
            }

            ViewData["Tienda"] = tienda;
            ViewData["DeepLink"] = $"buscaya://tienda/{id}";

            return View("Tienda");
        }
        catch
        {
            // Si ocurre algún error, mostramos una página amigable
            return View("TiendaNoEncontrada");
        }
    }
}
