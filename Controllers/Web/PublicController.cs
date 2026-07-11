using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BuscaYa.Options;
using BuscaYa.Services.IServices;

namespace BuscaYa.Controllers.Web;

public class PublicController : Controller
{
    private readonly AppDownloadOptions _appDownload;
    private readonly ITiendaService _tiendaService;
    private readonly IProductoService _productoService;

    private const string WebPublicaBase = "https://buscaya.cowib.es";

    public PublicController(
        IOptions<AppDownloadOptions> appDownload,
        ITiendaService tiendaService,
        IProductoService productoService)
    {
        _appDownload = appDownload.Value;
        _tiendaService = tiendaService;
        _productoService = productoService;
    }

    /// <summary>
    /// Página de descarga: en móvil redirige a la tienda correcta; en escritorio muestra ambos enlaces.
    /// </summary>
    [HttpGet("/descargar")]
    public IActionResult Descargar()
    {
        var ua = Request.Headers.UserAgent.ToString();
        if (LooksLikeIos(ua) && !string.IsNullOrEmpty(_appDownload.AppStoreUrl))
            return Redirect(_appDownload.AppStoreUrl);
        if (LooksLikeAndroid(ua) && !string.IsNullOrEmpty(_appDownload.GooglePlayUrl))
            return Redirect(_appDownload.GooglePlayUrl);

        return Redirect(WebPublicaBase);
    }

    /// <summary>
    /// Redirect permanente 301: links viejos /tienda/{token} → nueva URL /s/{slug}
    /// Cualquiera que tenga un QR o link viejo compartido sigue funcionando.
    /// </summary>
    [HttpGet("/tienda/{token}")]
    public IActionResult RedirectTienda(string token)
    {
        var tienda = _tiendaService.ObtenerDetallePorToken(token);
        if (tienda == null || string.IsNullOrEmpty(tienda.Slug))
            return RedirectPermanent(WebPublicaBase);

        return RedirectPermanent($"{WebPublicaBase}/s/{tienda.Slug}");
    }

    /// <summary>
    /// Redirect permanente 301: links viejos /producto/{token} → nueva URL /s/{tiendaSlug}/{productoSlug}
    /// </summary>
    [HttpGet("/producto/{token}")]
    public IActionResult RedirectProducto(string token)
    {
        var producto = _productoService.ObtenerPorToken(token);
        if (producto == null || string.IsNullOrEmpty(producto.Slug)
            || producto.Tienda == null || string.IsNullOrEmpty(producto.Tienda.Slug))
            return RedirectPermanent(WebPublicaBase);

        return RedirectPermanent($"{WebPublicaBase}/s/{producto.Tienda.Slug}/{producto.Slug}");
    }

    private static bool LooksLikeIos(string ua) =>
        ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
        || ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)
        || ua.Contains("iPod", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeAndroid(string ua) =>
        ua.Contains("Android", StringComparison.OrdinalIgnoreCase);
}
