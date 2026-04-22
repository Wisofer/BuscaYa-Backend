using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BuscaYa.Options;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;

namespace BuscaYa.Controllers.Web;

public class PublicController : Controller
{
    private readonly IProductoService _productoService;
    private readonly ITiendaService _tiendaService;
    private readonly AppDownloadOptions _appDownload;

    public PublicController(
        IProductoService productoService,
        ITiendaService tiendaService,
        IOptions<AppDownloadOptions> appDownload)
    {
        _productoService = productoService;
        _tiendaService = tiendaService;
        _appDownload = appDownload.Value;
    }

    private void SetAppDownloadViewData()
    {
        ViewData["GooglePlayUrl"] = _appDownload.GooglePlayUrl;
        ViewData["AppStoreUrl"] = _appDownload.AppStoreUrl;
        ViewData["AppleAppId"] = _appDownload.AppleAppId;
    }

    /// <summary>Página de descarga: en móvil redirige a la tienda correcta; en escritorio muestra ambos enlaces.</summary>
    [HttpGet("/descargar")]
    public IActionResult Descargar()
    {
        var ua = Request.Headers.UserAgent.ToString();
        if (LooksLikeIos(ua) && !string.IsNullOrEmpty(_appDownload.AppStoreUrl))
            return Redirect(_appDownload.AppStoreUrl);
        if (LooksLikeAndroid(ua) && !string.IsNullOrEmpty(_appDownload.GooglePlayUrl))
            return Redirect(_appDownload.GooglePlayUrl);

        SetAppDownloadViewData();
        return View("DescargarApp");
    }

    private static bool LooksLikeIos(string ua) =>
        ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
        || ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)
        || ua.Contains("iPod", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeAndroid(string ua) =>
        ua.Contains("Android", StringComparison.OrdinalIgnoreCase);

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
            SetAppDownloadViewData();

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
            SetAppDownloadViewData();

            return View("Tienda");
        }
        catch
        {
            // Si ocurre algún error, mostramos una página amigable
            return View("TiendaNoEncontrada");
        }
    }
}
