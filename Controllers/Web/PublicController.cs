using Microsoft.AspNetCore.Mvc;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;

namespace BuscaYa.Controllers.Web;

public class PublicController : Controller
{
    private readonly IProductoService _productoService;

    public PublicController(IProductoService productoService)
    {
        _productoService = productoService;
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
}
