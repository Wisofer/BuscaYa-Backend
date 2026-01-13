using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Models.DTOs.Responses;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;

    public AnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public void RegistrarEvento(int tiendaId, string tipoEvento, string? datosAdicionales = null)
    {
        var estadistica = new Estadistica
        {
            TiendaId = tiendaId,
            TipoEvento = tipoEvento,
            DatosAdicionales = datosAdicionales,
            Fecha = DateTime.Now
        };

        _context.Estadisticas.Add(estadistica);
        _context.SaveChanges();
    }

    public EstadisticasTiendaResponse ObtenerEstadisticasTienda(int tiendaId, DateTime desde, DateTime hasta)
    {
        var tienda = _context.Tiendas.Find(tiendaId);
        if (tienda == null)
        {
            throw new ArgumentException("Tienda no encontrada");
        }

        var estadisticas = _context.Estadisticas
            .Where(e => e.TiendaId == tiendaId && e.Fecha >= desde && e.Fecha <= hasta)
            .ToList();

        var response = new EstadisticasTiendaResponse
        {
            TiendaId = tiendaId,
            NombreTienda = tienda.Nombre,
            Desde = desde,
            Hasta = hasta,
            TotalVistas = estadisticas.Count(e => e.TipoEvento == SD.EventoVistaTienda),
            TotalClicksWhatsApp = estadisticas.Count(e => e.TipoEvento == SD.EventoClickWhatsApp),
            TotalClicksLlamar = estadisticas.Count(e => e.TipoEvento == SD.EventoClickLlamar),
            TotalClicksDireccion = estadisticas.Count(e => e.TipoEvento == SD.EventoClickDireccion),
            TotalBusquedas = estadisticas.Count(e => e.TipoEvento == SD.EventoBusqueda),
            ProductosMasBuscados = ObtenerProductosMasBuscados(tiendaId, 10)
        };

        return response;
    }

    public List<ProductoBuscadoResponse> ObtenerProductosMasBuscados(int? tiendaId, int top = 10)
    {
        var query = _context.Estadisticas
            .Where(e => e.TipoEvento == SD.EventoBusqueda && !string.IsNullOrEmpty(e.ProductoBuscado));

        if (tiendaId.HasValue)
        {
            query = query.Where(e => e.TiendaId == tiendaId.Value);
        }

        var productosBuscados = query
            .GroupBy(e => e.ProductoBuscado)
            .Select(g => new ProductoBuscadoResponse
            {
                NombreProducto = g.Key!,
                VecesBuscado = g.Count()
            })
            .OrderByDescending(p => p.VecesBuscado)
            .Take(top)
            .ToList();

        return productosBuscados;
    }
}
