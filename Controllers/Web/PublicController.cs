using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BuscaYa.Options;

namespace BuscaYa.Controllers.Web;

public class PublicController : Controller
{
    private readonly AppDownloadOptions _appDownload;

    public PublicController(IOptions<AppDownloadOptions> appDownload)
    {
        _appDownload = appDownload.Value;
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

        // Desktop: redirigir a la web pública
        return Redirect("https://buscaya.cowib.es");
    }

    private static bool LooksLikeIos(string ua) =>
        ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
        || ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)
        || ua.Contains("iPod", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeAndroid(string ua) =>
        ua.Contains("Android", StringComparison.OrdinalIgnoreCase);
}
