using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Models.Entities;

namespace BuscaYa.Services.IServices;

public interface IReporteService
{
    Task<Reporte> CrearReporteAsync(int usuarioId, CrearReporteRequest request);
    Task<List<Reporte>> ObtenerTodosAsync(bool? soloNoRevisados = null);
    Task<Reporte?> ObtenerPorIdAsync(int id);
    Task<bool> MarcarComoRevisadoAsync(int id, string? notaAdmin = null);
    Task<bool> ValidarRecursoExisteAsync(string tipo, int recursoId);
}
