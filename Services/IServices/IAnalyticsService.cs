using BuscaYa.Models.DTOs.Responses;

namespace BuscaYa.Services.IServices;

public interface IAnalyticsService
{
    void RegistrarEvento(int tiendaId, string tipoEvento, string? datosAdicionales = null);
    EstadisticasTiendaResponse ObtenerEstadisticasTienda(int tiendaId, DateTime desde, DateTime hasta);
    List<ProductoBuscadoResponse> ObtenerProductosMasBuscados(int? tiendaId, int top = 10);
}
