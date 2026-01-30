using BuscaYa.Data;
using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Services;

public class ReporteService : IReporteService
{
    private readonly ApplicationDbContext _context;

    public ReporteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Reporte> CrearReporteAsync(int usuarioId, CrearReporteRequest request)
    {
        // Validar que el recurso existe
        var recursoExiste = await ValidarRecursoExisteAsync(request.Tipo, request.RecursoId);
        if (!recursoExiste)
        {
            throw new ArgumentException($"El {request.Tipo} con ID {request.RecursoId} no existe.");
        }

        var reporte = new Reporte
        {
            UsuarioId = usuarioId,
            Tipo = request.Tipo.ToLower(),
            RecursoId = request.RecursoId,
            Razon = request.Razon,
            Detalle = string.IsNullOrWhiteSpace(request.Detalle) ? null : request.Detalle.Trim(),
            FechaCreacion = DateTime.Now
        };

        _context.Reportes.Add(reporte);
        await _context.SaveChangesAsync();

        return reporte;
    }

    public async Task<List<Reporte>> ObtenerTodosAsync(bool? soloNoRevisados = null)
    {
        var query = _context.Reportes
            .Include(r => r.Usuario)
            .AsQueryable();

        if (soloNoRevisados == true)
        {
            query = query.Where(r => !r.Revisado);
        }

        return await query
            .OrderByDescending(r => r.FechaCreacion)
            .ToListAsync();
    }

    public async Task<Reporte?> ObtenerPorIdAsync(int id)
    {
        return await _context.Reportes
            .Include(r => r.Usuario)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> MarcarComoRevisadoAsync(int id, string? notaAdmin = null)
    {
        var reporte = await _context.Reportes.FindAsync(id);
        if (reporte == null)
        {
            return false;
        }

        reporte.Revisado = true;
        reporte.FechaRevisado = DateTime.Now;
        reporte.NotaAdmin = string.IsNullOrWhiteSpace(notaAdmin) ? null : notaAdmin.Trim();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ValidarRecursoExisteAsync(string tipo, int recursoId)
    {
        return tipo.ToLower() switch
        {
            "producto" => await _context.Productos.AnyAsync(p => p.Id == recursoId && p.Activo),
            "tienda" => await _context.Tiendas.AnyAsync(t => t.Id == recursoId && t.Activo),
            _ => false
        };
    }
}
