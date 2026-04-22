namespace BuscaYa.Options;

/// <summary>Enlaces a las tiendas oficiales (configurables en appsettings).</summary>
public class AppDownloadOptions
{
    public const string SectionName = "AppDownload";

    /// <summary>URL pública de la app en Google Play.</summary>
    public string GooglePlayUrl { get; set; } =
        "https://play.google.com/store/apps/details?id=com.buscaya.app";

    /// <summary>URL pública de la app en App Store (cualquier región, Apple redirige).</summary>
    public string AppStoreUrl { get; set; } =
        "https://apps.apple.com/app/id6762116740";

    /// <summary>ID numérico en App Store (solo dígitos), para &lt;meta name="apple-itunes-app" /&gt;.</summary>
    public string AppleAppId { get; set; } = "6762116740";
}
