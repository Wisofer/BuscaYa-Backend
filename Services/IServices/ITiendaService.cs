using BuscaYa.Models.Entities;
using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Models.DTOs.Responses;

namespace BuscaYa.Services.IServices;

public interface ITiendaService
{
    List<Tienda> ObtenerTodas();
    Tienda? ObtenerPorId(int id);
    List<Tienda> ObtenerPorCiudad(string ciudad);
    List<Tienda> BuscarCercanas(decimal lat, decimal lng, double radioKm);
    TiendaResponse? ObtenerDetalle(int id, decimal? latUsuario = null, decimal? lngUsuario = null);
    Tienda Crear(CrearTiendaRequest request, int? usuarioId = null);
    bool Actualizar(int id, ActualizarTiendaRequest request);
    bool Activar(int id);
    bool Desactivar(int id);
    bool CambiarPlan(int tiendaId, string plan);
    bool VerificarLimiteProductos(int tiendaId);

    // Calificaciones
    void CalificarTienda(int tiendaId, int usuarioId, int valor);
    int? ObtenerCalificacionUsuario(int tiendaId, int usuarioId);
}
