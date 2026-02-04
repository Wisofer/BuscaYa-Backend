using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Models.DTOs.Responses;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Services;

public class TiendaService : ITiendaService
{
    private readonly ApplicationDbContext _context;
    private readonly IProductoService _productoService;

    public TiendaService(ApplicationDbContext context, IProductoService productoService)
    {
        _context = context;
        _productoService = productoService;
    }

    public List<Tienda> ObtenerTodas()
    {
        return _context.Tiendas
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .ToList();
    }

    public Tienda? ObtenerPorId(int id)
    {
        return _context.Tiendas
            .Include(t => t.Productos.Where(p => p.Activo))
            .ThenInclude(p => p.Categoria)
            .FirstOrDefault(t => t.Id == id);
    }

    public List<Tienda> ObtenerPorCiudad(string ciudad)
    {
        return _context.Tiendas
            .Where(t => t.Ciudad == ciudad && t.Activo)
            .OrderBy(t => t.Nombre)
            .ToList();
    }

    public List<Tienda> BuscarCercanas(decimal lat, decimal lng, double radioKm)
    {
        var todas = _context.Tiendas
            .Where(t => t.Activo)
            .ToList();

        return todas
            .Where(t =>
            {
                var distancia = GeolocationHelper.CalcularDistanciaKm(lat, lng, t.Latitud, t.Longitud);
                return distancia <= radioKm;
            })
            .OrderBy(t => GeolocationHelper.CalcularDistanciaKm(lat, lng, t.Latitud, t.Longitud))
            .ToList();
    }

    public TiendaResponse? ObtenerDetalle(int id, decimal? latUsuario = null, decimal? lngUsuario = null)
    {
        var tienda = ObtenerPorId(id);
        if (tienda == null) return null;

        var response = new TiendaResponse
        {
            Id = tienda.Id,
            Nombre = tienda.Nombre,
            Descripcion = tienda.Descripcion,
            Telefono = tienda.Telefono,
            WhatsApp = tienda.WhatsApp,
            Email = tienda.Email,
            Direccion = tienda.Direccion,
            Latitud = tienda.Latitud,
            Longitud = tienda.Longitud,
            Ciudad = tienda.Ciudad,
            Departamento = tienda.Departamento,
            HorarioApertura = tienda.HorarioApertura,
            HorarioCierre = tienda.HorarioCierre,
            DiasAtencion = tienda.DiasAtencion,
            LogoUrl = tienda.LogoUrl,
            FotoUrl = tienda.FotoUrl,
            Plan = tienda.Plan,
            EstaAbierta = tienda.EstaAbiertaManual,
            CalificacionPromedio = tienda.CalificacionPromedio,
            TotalCalificaciones = tienda.TotalCalificaciones,
            Productos = tienda.Productos.Select(p => new ProductoSimpleResponse
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Precio = p.Precio,
                Moneda = p.Moneda,
                FotoUrl = p.FotoUrl
            }).ToList()
        };

        if (latUsuario.HasValue && lngUsuario.HasValue)
        {
            response.DistanciaKm = GeolocationHelper.CalcularDistanciaKm(
                latUsuario.Value, lngUsuario.Value,
                tienda.Latitud, tienda.Longitud);
        }

        return response;
    }

    public Tienda Crear(CrearTiendaRequest request, int? usuarioId = null)
    {
        var tienda = new Tienda
        {
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Telefono = request.Telefono,
            WhatsApp = request.WhatsApp,
            Email = request.Email,
            Direccion = request.Direccion,
            Latitud = request.Latitud,
            Longitud = request.Longitud,
            Ciudad = request.Ciudad,
            Departamento = request.Departamento,
            HorarioApertura = request.HorarioApertura,
            HorarioCierre = request.HorarioCierre,
            DiasAtencion = request.DiasAtencion,
            LogoUrl = request.LogoUrl,
            FotoUrl = request.FotoUrl,
            EstaAbiertaManual = request.EstaAbiertaManual,
            Plan = SD.PlanFree,
            Activo = true,
            UsuarioId = usuarioId
        };

        _context.Tiendas.Add(tienda);
        _context.SaveChanges();
        return tienda;
    }

    public bool Actualizar(int id, ActualizarTiendaRequest request)
    {
        var tienda = _context.Tiendas.Find(id);
        if (tienda == null) return false;

        if (request.Nombre != null) tienda.Nombre = request.Nombre;
        if (request.Descripcion != null) tienda.Descripcion = request.Descripcion;
        if (request.Telefono != null) tienda.Telefono = request.Telefono;
        if (request.WhatsApp != null) tienda.WhatsApp = request.WhatsApp;
        if (request.Email != null) tienda.Email = request.Email;
        if (request.Direccion != null) tienda.Direccion = request.Direccion;
        if (request.Latitud.HasValue) tienda.Latitud = request.Latitud.Value;
        if (request.Longitud.HasValue) tienda.Longitud = request.Longitud.Value;
        if (request.Ciudad != null) tienda.Ciudad = request.Ciudad;
        if (request.Departamento != null) tienda.Departamento = request.Departamento;
        if (request.HorarioApertura.HasValue) tienda.HorarioApertura = request.HorarioApertura;
        if (request.HorarioCierre.HasValue) tienda.HorarioCierre = request.HorarioCierre;
        if (request.DiasAtencion != null) tienda.DiasAtencion = request.DiasAtencion;
        if (request.LogoUrl != null) tienda.LogoUrl = request.LogoUrl;
        if (request.FotoUrl != null) tienda.FotoUrl = request.FotoUrl;
        if (request.EstaAbiertaManual.HasValue) tienda.EstaAbiertaManual = request.EstaAbiertaManual.Value;

        tienda.FechaActualizacion = DateTime.Now;
        _context.SaveChanges();
        return true;
    }

    public bool ActualizarEstado(int id, bool estaAbiertaManual)
    {
        var tienda = _context.Tiendas.Find(id);
        if (tienda == null) return false;

        tienda.EstaAbiertaManual = estaAbiertaManual;
        tienda.FechaActualizacion = DateTime.Now;
        _context.SaveChanges();
        return true;
    }

    public bool Activar(int id)
    {
        var tienda = _context.Tiendas.Find(id);
        if (tienda == null) return false;

        tienda.Activo = true;
        _context.SaveChanges();
        return true;
    }

    public bool Desactivar(int id)
    {
        var tienda = _context.Tiendas.Find(id);
        if (tienda == null) return false;

        tienda.Activo = false;
        _context.SaveChanges();
        return true;
    }

    public bool CambiarPlan(int tiendaId, string plan)
    {
        var tienda = _context.Tiendas.Find(tiendaId);
        if (tienda == null) return false;

        tienda.Plan = plan;
        _context.SaveChanges();
        return true;
    }

    public bool VerificarLimiteProductos(int tiendaId)
    {
        var tienda = _context.Tiendas.Find(tiendaId);
        if (tienda == null) return false;

        if (tienda.Plan == SD.PlanPro) return true;

        var cantidadProductos = _context.Productos
            .Count(p => p.TiendaId == tiendaId && p.Activo);

        return cantidadProductos < SD.LimiteProductosPlanFree;
    }

    public void CalificarTienda(int tiendaId, int usuarioId, int valor)
    {
        if (valor < 1 || valor > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "La calificación debe ser entre 1 y 5.");
        }

        var tienda = _context.Tiendas.Find(tiendaId);
        if (tienda == null || !tienda.Activo)
        {
            throw new InvalidOperationException("Tienda no encontrada o inactiva.");
        }

        var calificacion = _context.CalificacionesTiendas
            .FirstOrDefault(c => c.TiendaId == tiendaId && c.UsuarioId == usuarioId);

        if (calificacion == null)
        {
            calificacion = new CalificacionTienda
            {
                TiendaId = tiendaId,
                UsuarioId = usuarioId,
                Valor = valor
            };
            _context.CalificacionesTiendas.Add(calificacion);
        }
        else
        {
            calificacion.Valor = valor;
            calificacion.FechaActualizacion = DateTime.Now;
        }

        _context.SaveChanges();

        // Recalcular promedio y total
        var calificaciones = _context.CalificacionesTiendas
            .Where(c => c.TiendaId == tiendaId)
            .ToList();

        tienda.TotalCalificaciones = calificaciones.Count;
        tienda.CalificacionPromedio = tienda.TotalCalificaciones > 0
            ? calificaciones.Average(c => c.Valor)
            : 0;

        _context.SaveChanges();
    }

    public int? ObtenerCalificacionUsuario(int tiendaId, int usuarioId)
    {
        var calificacion = _context.CalificacionesTiendas
            .FirstOrDefault(c => c.TiendaId == tiendaId && c.UsuarioId == usuarioId);

        return calificacion?.Valor;
    }
}
