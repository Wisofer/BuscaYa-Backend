namespace BuscaYa.Utils;

/// <summary>Helpers para producto (ofertas, descuentos).</summary>
public static class ProductoHelper
{
    /// <summary>Calcula el porcentaje de descuento cuando hay precio actual y precio anterior (oferta). Devuelve null si no aplica.</summary>
    public static int? CalcularPorcentajeDescuento(decimal? precio, decimal? precioAnterior)
    {
        if (!precio.HasValue || !precioAnterior.HasValue || precioAnterior.Value <= 0 || precio.Value >= precioAnterior.Value)
            return null;
        return (int)Math.Round((1 - precio.Value / precioAnterior.Value) * 100);
    }
}
