using BuscaYa.Data;
using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Models.DTOs.Responses;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Services;

public class BusquedaService : IBusquedaService
{
    private readonly ApplicationDbContext _context;
    private readonly ITiendaService _tiendaService;

    public BusquedaService(ApplicationDbContext context, ITiendaService tiendaService)
    {
        _context = context;
        _tiendaService = tiendaService;
    }

    public BuscarResponse BuscarProductos(BuscarRequest request)
    {
        var query = _context.Productos
            .Include(p => p.Tienda)
            .Include(p => p.Categoria)
            .Where(p => p.Activo && p.Tienda.Activo);

        // Filtrar por término de búsqueda
        if (!string.IsNullOrWhiteSpace(request.Termino))
        {
            var termino = request.Termino.ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(termino) ||
                (p.Descripcion != null && p.Descripcion.ToLower().Contains(termino)) ||
                p.Tienda.Nombre.ToLower().Contains(termino));
        }

        // Filtrar por categoría
        if (request.CategoriaId.HasValue)
        {
            query = query.Where(p => p.CategoriaId == request.CategoriaId.Value);
        }

        var productos = query.ToList();

        // Filtrar por distancia si hay coordenadas
        if (request.Latitud.HasValue && request.Longitud.HasValue)
        {
            var radioKm = request.RadioKm ?? SD.RadioBusquedaDefault;
            productos = productos
                .Where(p =>
                {
                    var distancia = GeolocationHelper.CalcularDistanciaKm(
                        request.Latitud.Value, request.Longitud.Value,
                        p.Tienda.Latitud, p.Tienda.Longitud);
                    return distancia <= radioKm;
                })
                .ToList();
        }

        // Ordenar: Plan Pro primero, luego por distancia (si hay coordenadas)
        if (request.Latitud.HasValue && request.Longitud.HasValue)
        {
            productos = productos
                .OrderByDescending(p => p.Tienda.Plan == SD.PlanPro)
                .ThenBy(p =>
                {
                    return GeolocationHelper.CalcularDistanciaKm(
                        request.Latitud.Value, request.Longitud.Value,
                        p.Tienda.Latitud, p.Tienda.Longitud);
                })
                .ToList();
        }
        else
        {
            productos = productos
                .OrderByDescending(p => p.Tienda.Plan == SD.PlanPro)
                .ThenBy(p => p.Nombre)
                .ToList();
        }

        var total = productos.Count;
        var tamanoPagina = Math.Min(request.TamanoPagina, SD.TamanoPaginaMaximo);
        var pagina = Math.Max(1, request.Pagina);
        var totalPaginas = (int)Math.Ceiling(total / (double)tamanoPagina);

        var productosPaginados = productos
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        var productosResponse = productosPaginados.Select(p =>
        {
            var distancia = request.Latitud.HasValue && request.Longitud.HasValue
                ? GeolocationHelper.CalcularDistanciaKm(
                    request.Latitud.Value, request.Longitud.Value,
                    p.Tienda.Latitud, p.Tienda.Longitud)
                : (double?)null;

            return new ProductoResponse
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Precio = p.Precio,
                Moneda = p.Moneda,
                FotoUrl = p.FotoUrl,
                Tienda = new TiendaInfoResponse
                {
                    Id = p.Tienda.Id,
                    Nombre = p.Tienda.Nombre,
                    Direccion = p.Tienda.Direccion,
                    Ciudad = p.Tienda.Ciudad,
                    WhatsApp = p.Tienda.WhatsApp,
                    Telefono = p.Tienda.Telefono,
                    LogoUrl = p.Tienda.LogoUrl,
                    Latitud = p.Tienda.Latitud,
                    Longitud = p.Tienda.Longitud
                },
                Categoria = new CategoriaInfoResponse
                {
                    Id = p.Categoria.Id,
                    Nombre = p.Categoria.Nombre,
                    Icono = p.Categoria.Icono
                },
                DistanciaKm = distancia
            };
        }).ToList();

        return new BuscarResponse
        {
            Productos = productosResponse,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalPaginas = totalPaginas
        };
    }

    public List<string> Sugerencias(string termino, int limite = 10)
    {
        if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
            return new List<string>();

        var terminoLower = termino.ToLower();

        var sugerencias = _context.Productos
            .Where(p => p.Activo && p.Tienda.Activo &&
                       (p.Nombre.ToLower().Contains(terminoLower) ||
                        (p.Descripcion != null && p.Descripcion.ToLower().Contains(terminoLower))))
            .Select(p => p.Nombre)
            .Distinct()
            .Take(limite)
            .ToList();

        return sugerencias;
    }
}
