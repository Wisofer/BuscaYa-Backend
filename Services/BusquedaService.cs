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
            var locales = productos
                .Where(p =>
                {
                    var distancia = GeolocationHelper.CalcularDistanciaKm(
                        request.Latitud.Value, request.Longitud.Value,
                        p.Tienda.Latitud, p.Tienda.Longitud);
                    return distancia <= radioKm;
                })
                .ToList();

            // Si hay resultados locales, los mostramos.
            // Si no hay resultados locales pero hay un término de búsqueda específico,
            // mostramos los resultados globales (lejanos) para que el usuario no vea una pantalla vacía.
            if (locales.Any() || string.IsNullOrWhiteSpace(request.Termino))
            {
                productos = locales;
            }
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
                EnOferta = p.EnOferta,
                PrecioAnterior = p.PrecioAnterior,
                PorcentajeDescuento = ProductoHelper.CalcularPorcentajeDescuento(p.Precio, p.PrecioAnterior),
                FotoUrl = p.FotoUrl,
                TokenPublico = p.TokenPublico,
                Slug = p.Slug,
                FavoritosCount = p.FavoritosCount,
                CompartirUrl = WebUrlHelper.GenerarUrlProducto(p.Tienda.Slug, p.Slug),
                Tienda = new TiendaInfoResponse
                {
                    Id = p.Tienda.Id,
                    Nombre = p.Tienda.Nombre,
                    Direccion = p.Tienda.Direccion,
                    Ciudad = p.Tienda.Ciudad,
                    WhatsApp = p.Tienda.WhatsApp,
                    Telefono = p.Tienda.Telefono,
                    LogoUrl = p.Tienda.LogoUrl,
                    FotoUrl = p.Tienda.FotoUrl,
                    Latitud = p.Tienda.Latitud,
                    Longitud = p.Tienda.Longitud,
                    FavoritosCount = p.Tienda.FavoritosCount,
                    EstaAbierta = TiendaHelper.CalcularEstaAbierta(p.Tienda.EstaAbiertaManual, p.Tienda.HorarioApertura, p.Tienda.HorarioCierre),
                    Email = p.Tienda.Email,
                    DiasAtencion = p.Tienda.DiasAtencion,
                    HorarioApertura = p.Tienda.HorarioApertura?.ToString(@"hh\:mm"),
                    HorarioCierre = p.Tienda.HorarioCierre?.ToString(@"hh\:mm"),
                    Descripcion = p.Tienda.Descripcion,
                    TokenPublico = p.Tienda.TokenPublico,
                    Slug = p.Tienda.Slug,
                    CompartirUrl = WebUrlHelper.GenerarUrlTienda(p.Tienda.Slug)
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

        // Búsqueda de Tiendas coincidente
        var queryTiendas = _context.Tiendas
            .Include(t => t.Productos.Where(p => p.Activo))
            .ThenInclude(p => p.Categoria)
            .Where(t => t.Activo);

        if (!string.IsNullOrWhiteSpace(request.Termino))
        {
            var termino = request.Termino.ToLower();
            queryTiendas = queryTiendas.Where(t => 
                t.Nombre.ToLower().Contains(termino) || 
                (t.Descripcion != null && t.Descripcion.ToLower().Contains(termino)) ||
                t.Ciudad.ToLower().Contains(termino) ||
                (t.Departamento != null && t.Departamento.ToLower().Contains(termino)));
        }

        if (request.CategoriaId.HasValue)
        {
            queryTiendas = queryTiendas.Where(t => t.Productos.Any(p => p.CategoriaId == request.CategoriaId.Value));
        }

        var tiendasList = queryTiendas.ToList();

        // Filtrar tiendas por distancia
        if (request.Latitud.HasValue && request.Longitud.HasValue)
        {
            var radioKm = request.RadioKm ?? SD.RadioBusquedaDefault;
            var localesTiendas = tiendasList
                .Where(t =>
                {
                    var distancia = GeolocationHelper.CalcularDistanciaKm(
                        request.Latitud.Value, request.Longitud.Value,
                        t.Latitud, t.Longitud);
                    return distancia <= radioKm;
                })
                .ToList();

            if (localesTiendas.Any() || string.IsNullOrWhiteSpace(request.Termino))
            {
                tiendasList = localesTiendas;
            }
        }

        var tiendasMapped = tiendasList.Select(t =>
        {
            var res = new TiendaResponse
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Descripcion = t.Descripcion,
                Telefono = t.Telefono,
                WhatsApp = t.WhatsApp,
                Email = t.Email,
                Direccion = t.Direccion,
                Latitud = t.Latitud,
                Longitud = t.Longitud,
                Ciudad = t.Ciudad,
                Departamento = t.Departamento,
                HorarioApertura = t.HorarioApertura,
                HorarioCierre = t.HorarioCierre,
                DiasAtencion = t.DiasAtencion,
                LogoUrl = t.LogoUrl,
                FotoUrl = t.FotoUrl,
                Plan = t.Plan,
                EstaAbierta = TiendaHelper.CalcularEstaAbierta(t.EstaAbiertaManual, t.HorarioApertura, t.HorarioCierre),
                EstaAbiertaManual = t.EstaAbiertaManual,
                CalificacionPromedio = t.CalificacionPromedio,
                TotalCalificaciones = t.TotalCalificaciones,
                FavoritosCount = t.FavoritosCount,
                TokenPublico = t.TokenPublico,
                Slug = t.Slug,
                CompartirUrl = WebUrlHelper.GenerarUrlTienda(t.Slug),
                FacebookUrl = t.FacebookUrl,
                InstagramUrl = t.InstagramUrl,
                TikTokUrl = t.TikTokUrl,
                Productos = t.Productos.Select(p => new ProductoSimpleResponse
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Precio = p.Precio,
                    Moneda = p.Moneda,
                    EnOferta = p.EnOferta,
                    PrecioAnterior = p.PrecioAnterior,
                    PorcentajeDescuento = ProductoHelper.CalcularPorcentajeDescuento(p.Precio, p.PrecioAnterior),
                    FotoUrl = p.FotoUrl,
                    CategoriaNombre = p.Categoria?.Nombre,
                    TokenPublico = p.TokenPublico,
                    Slug = p.Slug,
                    FavoritosCount = p.FavoritosCount,
                    CompartirUrl = WebUrlHelper.GenerarUrlProducto(t.Slug, p.Slug)
                }).ToList()
            };

            if (request.Latitud.HasValue && request.Longitud.HasValue)
            {
                res.DistanciaKm = GeolocationHelper.CalcularDistanciaKm(
                    request.Latitud.Value, request.Longitud.Value,
                    t.Latitud, t.Longitud);
            }

            return res;
        });

        // Ordenar Tiendas
        if (request.Latitud.HasValue && request.Longitud.HasValue)
        {
            tiendasMapped = tiendasMapped
                .OrderByDescending(t => t.Plan == SD.PlanPro)
                .ThenBy(t => t.DistanciaKm ?? double.MaxValue)
                .ThenBy(t => t.Nombre);
        }
        else
        {
            tiendasMapped = tiendasMapped
                .OrderByDescending(t => t.Plan == SD.PlanPro)
                .ThenBy(t => t.Nombre);
        }

        var finalTiendasList = tiendasMapped.Take(50).ToList();

        return new BuscarResponse
        {
            Productos = productosResponse,
            Tiendas = finalTiendasList,
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

        var sugerenciasProductos = _context.Productos
            .Where(p => p.Activo && p.Tienda.Activo && p.Nombre.ToLower().Contains(terminoLower))
            .Select(p => p.Nombre)
            .Distinct()
            .Take(limite)
            .ToList();

        var sugerenciasTiendas = _context.Tiendas
            .Where(t => t.Activo && t.Nombre.ToLower().Contains(terminoLower))
            .Select(t => t.Nombre)
            .Distinct()
            .Take(limite)
            .ToList();

        var sugerencias = sugerenciasProductos
            .Concat(sugerenciasTiendas)
            .Distinct()
            .Take(limite)
            .ToList();

        return sugerencias;
    }
}
