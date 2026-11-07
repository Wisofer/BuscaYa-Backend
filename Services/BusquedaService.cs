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

        // Filtrar por país
        if (!string.IsNullOrWhiteSpace(request.CountryCode))
        {
            var countryCode = request.CountryCode.Trim().ToUpper();
            query = query.Where(p => p.Tienda.CountryCode == countryCode);
        }

        // Filtrar por categoría
        if (request.CategoriaId.HasValue)
        {
            query = query.Where(p => p.CategoriaId == request.CategoriaId.Value);
        }

        var productos = query.ToList();
        bool hasGps = request.Latitud.HasValue && request.Longitud.HasValue;

        // Filtrar por distancia si hay coordenadas GPS con Fallback inteligente
        if (hasGps)
        {
            var radioKm = (request.RadioKm.HasValue && request.RadioKm.Value > 0) ? request.RadioKm.Value : SD.RadioBusquedaDefault;
            var locales = productos
                .Where(p =>
                {
                    var distancia = GeolocationHelper.CalcularDistanciaKm(
                        request.Latitud!.Value, request.Longitud!.Value,
                        p.Tienda.Latitud, p.Tienda.Longitud);
                    return distancia <= radioKm;
                })
                .ToList();

            // Si hay resultados locales dentro del radio, los mostramos.
            // Si no hay resultados locales en ese radio, omitimos el corte estricto del radio (fallback)
            // para devolver los más cercanos del país y que la pantalla del usuario NUNCA quede en blanco.
            if (locales.Any())
            {
                productos = locales;
            }
        }

        // Ordenamiento por Proximidad (El punto clave) — 100% democrático y gratuito (sin discriminación de planes)
        if (hasGps)
        {
            // Con GPS:
            // 1. Boost para negocios abiertos en su hora local (EstaAbierta = true primero)
            // 2. Orden ascendente por cercanía hiperlocal pura (distanciaKm ASC)
            // 3. Popularidad orgánica (FavoritosCount DESC)
            // 4. Nombre
            productos = productos
                .OrderByDescending(p => TiendaHelper.CalcularEstaAbierta(p.Tienda.EstaAbiertaManual, p.Tienda.HorarioApertura, p.Tienda.HorarioCierre, p.Tienda.CountryCode))
                .ThenBy(p => GeolocationHelper.CalcularDistanciaKm(
                    request.Latitud!.Value, request.Longitud!.Value,
                    p.Tienda.Latitud, p.Tienda.Longitud))
                .ThenByDescending(p => p.FavoritosCount)
                .ThenBy(p => p.Nombre)
                .ToList();
        }
        else
        {
            // Sin GPS:
            // Tiendas abiertas primero, popularidad y fecha de creación más reciente
            productos = productos
                .OrderByDescending(p => TiendaHelper.CalcularEstaAbierta(p.Tienda.EstaAbiertaManual, p.Tienda.HorarioApertura, p.Tienda.HorarioCierre, p.Tienda.CountryCode))
                .ThenByDescending(p => p.FavoritosCount)
                .ThenByDescending(p => p.FechaCreacion)
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
            double? distancia = null;
            if (hasGps)
            {
                var d = GeolocationHelper.CalcularDistanciaKm(
                    request.Latitud!.Value, request.Longitud!.Value,
                    p.Tienda.Latitud, p.Tienda.Longitud);
                distancia = Math.Round(d, 2);
            }

            var estaAbierta = TiendaHelper.CalcularEstaAbierta(p.Tienda.EstaAbiertaManual, p.Tienda.HorarioApertura, p.Tienda.HorarioCierre, p.Tienda.CountryCode);

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
                DistanciaKm = distancia,
                Tienda = new TiendaInfoResponse
                {
                    Id = p.Tienda.Id,
                    Nombre = p.Tienda.Nombre,
                    Direccion = p.Tienda.Direccion,
                    Ciudad = p.Tienda.Ciudad,
                    CountryCode = p.Tienda.CountryCode,
                    Pais = p.Tienda.Pais,
                    CurrencyCode = p.Tienda.CurrencyCode,
                    CurrencySymbol = p.Tienda.CurrencySymbol,
                    WhatsApp = p.Tienda.WhatsApp,
                    Telefono = p.Tienda.Telefono,
                    LogoUrl = p.Tienda.LogoUrl,
                    FotoUrl = p.Tienda.FotoUrl,
                    Latitud = p.Tienda.Latitud,
                    Longitud = p.Tienda.Longitud,
                    FavoritosCount = p.Tienda.FavoritosCount,
                    EstaAbierta = estaAbierta,
                    DistanciaKm = distancia,
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
                }
            };
        }).ToList();

        // Búsqueda de Tiendas coincidente
        var queryTiendas = _context.Tiendas
            .Include(t => t.Productos.Where(p => p.Activo))
            .ThenInclude(p => p.Categoria)
            .Where(t => t.Activo);

        // Filtrar tiendas por país
        if (!string.IsNullOrWhiteSpace(request.CountryCode))
        {
            var countryCode = request.CountryCode.Trim().ToUpper();
            queryTiendas = queryTiendas.Where(t => t.CountryCode == countryCode);
        }

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

        // Filtrar tiendas por distancia con Fallback inteligente
        if (hasGps)
        {
            var radioKm = (request.RadioKm.HasValue && request.RadioKm.Value > 0) ? request.RadioKm.Value : SD.RadioBusquedaDefault;
            var localesTiendas = tiendasList
                .Where(t =>
                {
                    var distancia = GeolocationHelper.CalcularDistanciaKm(
                        request.Latitud!.Value, request.Longitud!.Value,
                        t.Latitud, t.Longitud);
                    return distancia <= radioKm;
                })
                .ToList();

            // Si hay tiendas dentro del radio, las usamos; si no, dejamos la lista completa del país para que la pantalla no quede en blanco
            if (localesTiendas.Any())
            {
                tiendasList = localesTiendas;
            }
        }

        var tiendasMapped = tiendasList.Select(t =>
        {
            double? distTienda = null;
            if (hasGps)
            {
                var d = GeolocationHelper.CalcularDistanciaKm(
                    request.Latitud!.Value, request.Longitud!.Value,
                    t.Latitud, t.Longitud);
                distTienda = Math.Round(d, 2);
            }

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
                CountryCode = t.CountryCode,
                Pais = t.Pais,
                CurrencyCode = t.CurrencyCode,
                CurrencySymbol = t.CurrencySymbol,
                HorarioApertura = t.HorarioApertura,
                HorarioCierre = t.HorarioCierre,
                DiasAtencion = t.DiasAtencion,
                LogoUrl = t.LogoUrl,
                FotoUrl = t.FotoUrl,
                Plan = t.Plan,
                EstaAbierta = TiendaHelper.CalcularEstaAbierta(t.EstaAbiertaManual, t.HorarioApertura, t.HorarioCierre, t.CountryCode),
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
                ExchangeRateMode = t.ExchangeRateMode,
                ExchangeRate = t.ExchangeRate,
                DistanciaKm = distTienda,
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

            return res;
        });

        // Ordenar Tiendas por Proximidad (Con GPS) o por Popularidad (Sin GPS) — Sin discriminación de planes
        if (hasGps)
        {
            tiendasMapped = tiendasMapped
                .OrderByDescending(t => t.EstaAbierta)
                .ThenBy(t => t.DistanciaKm ?? double.MaxValue)
                .ThenByDescending(t => t.FavoritosCount)
                .ThenBy(t => t.Nombre);
        }
        else
        {
            tiendasMapped = tiendasMapped
                .OrderByDescending(t => t.EstaAbierta)
                .ThenByDescending(t => t.FavoritosCount)
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

    public List<string> Sugerencias(string termino, int limite = 10, string? countryCode = null)
    {
        if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
            return new List<string>();

        var terminoLower = termino.ToLower();
        var code = string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.Trim().ToUpper();

        var queryProductos = _context.Productos
            .Where(p => p.Activo && p.Tienda.Activo && p.Nombre.ToLower().Contains(terminoLower));

        var queryTiendas = _context.Tiendas
            .Where(t => t.Activo && t.Nombre.ToLower().Contains(terminoLower));

        if (!string.IsNullOrWhiteSpace(code))
        {
            queryProductos = queryProductos.Where(p => p.Tienda.CountryCode == code);
            queryTiendas = queryTiendas.Where(t => t.CountryCode == code);
        }

        var sugerenciasProductos = queryProductos
            .Select(p => p.Nombre)
            .Distinct()
            .Take(limite)
            .ToList();

        var sugerenciasTiendas = queryTiendas
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
