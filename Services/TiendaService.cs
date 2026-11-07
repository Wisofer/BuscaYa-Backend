using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Models.DTOs.Responses;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BuscaYa.Services;

public class TiendaService : ITiendaService
{
    private readonly ApplicationDbContext _context;
    private readonly IProductoService _productoService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TiendaService> _logger;

    public TiendaService(ApplicationDbContext context, IProductoService productoService, IServiceScopeFactory scopeFactory, ILogger<TiendaService> logger)
    {
        _context = context;
        _productoService = productoService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public List<Tienda> ObtenerTodas(string? countryCode = null)
    {
        var query = _context.Tiendas
            .AsNoTracking()
            .Where(t => t.Activo);

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            var code = countryCode.Trim().ToUpper();
            query = query.Where(t => t.CountryCode == code);
        }

        return query
            .OrderBy(t => t.Nombre)
            .ToList();
    }

    public Tienda? ObtenerPorId(int id)
    {
        return _context.Tiendas
            .AsNoTracking()
            .Include(t => t.Productos.Where(p => p.Activo))
            .ThenInclude(p => p.Categoria)
            .FirstOrDefault(t => t.Id == id);
    }

    public List<Tienda> ObtenerPorCiudad(string ciudad)
    {
        return _context.Tiendas
            .AsNoTracking()
            .Where(t => t.Ciudad == ciudad && t.Activo)
            .OrderBy(t => t.Nombre)
            .ToList();
    }

    public List<Tienda> BuscarCercanas(decimal lat, decimal lng, double radioKm, string? countryCode = null)
    {
        var query = _context.Tiendas
            .AsNoTracking()
            .Where(t => t.Activo);

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            var code = countryCode.Trim().ToUpper();
            query = query.Where(t => t.CountryCode == code);
        }

        var todas = query.ToList();

        var locales = todas
            .Where(t =>
            {
                var distancia = GeolocationHelper.CalcularDistanciaKm(lat, lng, t.Latitud, t.Longitud);
                return distancia <= radioKm;
            })
            .ToList();

        // Fallback: Si no hay en el radio especificado, no dejamos la pantalla en blanco y devolvemos las más cercanas del país
        var resultado = locales.Any() ? locales : todas;

        return resultado
            .OrderBy(t => GeolocationHelper.CalcularDistanciaKm(lat, lng, t.Latitud, t.Longitud))
            .ToList();
    }

    public List<TiendaResponse> ExplorarTiendas(ObtenerTiendasRequest request)
    {
        var query = _context.Tiendas
            .AsNoTracking()
            .Include(t => t.Productos.Where(p => p.Activo))
            .ThenInclude(p => p.Categoria)
            .Where(t => t.Activo);

        // 1. Filtrar por país
        if (!string.IsNullOrWhiteSpace(request.CountryCode))
        {
            var code = request.CountryCode.Trim().ToUpper();
            query = query.Where(t => t.CountryCode == code);
        }

        // 2. Filtrar por término (nombre, descripción, ciudad o departamento)
        if (!string.IsNullOrWhiteSpace(request.Termino))
        {
            var termino = request.Termino.Trim().ToLower();
            query = query.Where(t =>
                t.Nombre.ToLower().Contains(termino) ||
                (t.Descripcion != null && t.Descripcion.ToLower().Contains(termino)) ||
                t.Ciudad.ToLower().Contains(termino) ||
                (t.Departamento != null && t.Departamento.ToLower().Contains(termino)));
        }

        // 3. Filtrar por categoría
        if (request.CategoriaId.HasValue)
        {
            query = query.Where(t => t.Productos.Any(p => p.CategoriaId == request.CategoriaId.Value));
        }

        var tiendas = query.ToList();
        bool hasGps = request.Latitud.HasValue && request.Longitud.HasValue;

        // 4. Filtrar por radio con Fallback inteligente (evita que la app quede en blanco)
        if (hasGps)
        {
            var radioKm = (request.RadioKm.HasValue && request.RadioKm.Value > 0) ? request.RadioKm.Value : SD.RadioBusquedaDefault;
            var locales = tiendas
                .Where(t =>
                {
                    var distancia = GeolocationHelper.CalcularDistanciaKm(
                        request.Latitud!.Value, request.Longitud!.Value,
                        t.Latitud, t.Longitud);
                    return distancia <= radioKm;
                })
                .ToList();

            // Si hay tiendas en el radio, las usamos; si no, omitimos el corte estricto (fallback hiperlocal)
            if (locales.Any())
            {
                tiendas = locales;
            }
        }

        // 5. Proyección a TiendaResponse con cálculo geodésico y estado dinámico
        var tiendasMapped = tiendas.Select(t =>
        {
            double? dist = null;
            if (hasGps)
            {
                var d = GeolocationHelper.CalcularDistanciaKm(
                    request.Latitud!.Value, request.Longitud!.Value,
                    t.Latitud, t.Longitud);
                dist = double.IsNaN(d) || double.IsInfinity(d) ? null : Math.Round(d, 2);
            }

            var estaAbierta = TiendaHelper.CalcularEstaAbierta(t.EstaAbiertaManual, t.HorarioApertura, t.HorarioCierre, t.CountryCode);

            return new TiendaResponse
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
                EstaAbierta = estaAbierta,
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
                DistanciaKm = dist,
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
        }).AsEnumerable();

        // 6. Ordenamiento por Proximidad (El punto clave) — Sin discriminación de planes (100% gratuito)
        if (hasGps)
        {
            // Con GPS:
            // 1. Boost para abiertas (EstaAbierta = true primero)
            // 2. Orden ascendente por cercanía (distanciaKm ASC)
            // 3. Popularidad y Calificaciones
            // 4. Nombre
            tiendasMapped = tiendasMapped
                .OrderByDescending(t => t.EstaAbierta)
                .ThenBy(t => t.DistanciaKm ?? double.MaxValue)
                .ThenByDescending(t => t.FavoritosCount)
                .ThenByDescending(t => t.CalificacionPromedio)
                .ThenBy(t => t.Nombre);
        }
        else
        {
            // Sin GPS:
            // 1. Tiendas abiertas primero
            // 2. Popularidad y calificaciones
            // 3. Nombre
            tiendasMapped = tiendasMapped
                .OrderByDescending(t => t.EstaAbierta)
                .ThenByDescending(t => t.FavoritosCount)
                .ThenByDescending(t => t.CalificacionPromedio)
                .ThenBy(t => t.Nombre);
        }

        // 7. Paginación si fue requerida
        if (request.Pagina.HasValue && request.TamanoPagina.HasValue && request.TamanoPagina.Value > 0)
        {
            var pagina = Math.Max(1, request.Pagina.Value);
            var tamano = Math.Min(request.TamanoPagina.Value, SD.TamanoPaginaMaximo);
            tiendasMapped = tiendasMapped.Skip((pagina - 1) * tamano).Take(tamano);
        }

        return tiendasMapped.ToList();
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
            CountryCode = tienda.CountryCode,
            Pais = tienda.Pais,
            CurrencyCode = tienda.CurrencyCode,
            CurrencySymbol = tienda.CurrencySymbol,
            HorarioApertura = tienda.HorarioApertura,
            HorarioCierre = tienda.HorarioCierre,
            DiasAtencion = tienda.DiasAtencion,
            LogoUrl = tienda.LogoUrl,
            FotoUrl = tienda.FotoUrl,
            Plan = tienda.Plan,
            EstaAbierta = TiendaHelper.CalcularEstaAbierta(tienda.EstaAbiertaManual, tienda.HorarioApertura, tienda.HorarioCierre, tienda.CountryCode),
            EstaAbiertaManual = tienda.EstaAbiertaManual,
            CalificacionPromedio = tienda.CalificacionPromedio,
            TotalCalificaciones = tienda.TotalCalificaciones,
            FavoritosCount = tienda.FavoritosCount,
            TokenPublico = tienda.TokenPublico,
            Slug = tienda.Slug,
            CompartirUrl = WebUrlHelper.GenerarUrlTienda(tienda.Slug),
            FacebookUrl = tienda.FacebookUrl,
            InstagramUrl = tienda.InstagramUrl,
            TikTokUrl = tienda.TikTokUrl,
            ExchangeRateMode = tienda.ExchangeRateMode,
            ExchangeRate = tienda.ExchangeRate,
            Productos = tienda.Productos.Select(p => new ProductoSimpleResponse
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
                CompartirUrl = WebUrlHelper.GenerarUrlProducto(tienda.Slug, p.Slug)
            }).ToList()
        };

        if (latUsuario.HasValue && lngUsuario.HasValue)
        {
            var d = GeolocationHelper.CalcularDistanciaKm(
                latUsuario.Value, lngUsuario.Value,
                tienda.Latitud, tienda.Longitud);
            response.DistanciaKm = double.IsNaN(d) || double.IsInfinity(d) ? null : Math.Round(d, 2);
        }

        return response;
    }

    public TiendaResponse? ObtenerDetallePorToken(string token, decimal? latUsuario = null, decimal? lngUsuario = null)
    {
        var tienda = _context.Tiendas
            .Include(t => t.Productos.Where(p => p.Activo))
            .ThenInclude(p => p.Categoria)
            .FirstOrDefault(t => t.TokenPublico == token);
        if (tienda == null) return null;
        return ObtenerDetallePorSlug(tienda.Slug, latUsuario, lngUsuario);
    }

    public TiendaResponse? ObtenerDetallePorSlug(string slug, decimal? latUsuario = null, decimal? lngUsuario = null)
    {
        var tienda = _context.Tiendas
            .Include(t => t.Productos.Where(p => p.Activo))
            .ThenInclude(p => p.Categoria)
            .FirstOrDefault(t => t.Slug == slug && t.Activo);
            
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
            CountryCode = tienda.CountryCode,
            Pais = tienda.Pais,
            CurrencyCode = tienda.CurrencyCode,
            CurrencySymbol = tienda.CurrencySymbol,
            HorarioApertura = tienda.HorarioApertura,
            HorarioCierre = tienda.HorarioCierre,
            DiasAtencion = tienda.DiasAtencion,
            LogoUrl = tienda.LogoUrl,
            FotoUrl = tienda.FotoUrl,
            Plan = tienda.Plan,
            EstaAbierta = TiendaHelper.CalcularEstaAbierta(tienda.EstaAbiertaManual, tienda.HorarioApertura, tienda.HorarioCierre, tienda.CountryCode),
            EstaAbiertaManual = tienda.EstaAbiertaManual,
            CalificacionPromedio = tienda.CalificacionPromedio,
            TotalCalificaciones = tienda.TotalCalificaciones,
            FavoritosCount = tienda.FavoritosCount,
            TokenPublico = tienda.TokenPublico,
            Slug = tienda.Slug,
            CompartirUrl = WebUrlHelper.GenerarUrlTienda(tienda.Slug),
            FacebookUrl = tienda.FacebookUrl,
            InstagramUrl = tienda.InstagramUrl,
            TikTokUrl = tienda.TikTokUrl,
            ExchangeRateMode = tienda.ExchangeRateMode,
            ExchangeRate = tienda.ExchangeRate,
            Productos = tienda.Productos.Select(p => new ProductoSimpleResponse
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
                CompartirUrl = WebUrlHelper.GenerarUrlProducto(tienda.Slug, p.Slug)
            }).ToList()
        };

        if (latUsuario.HasValue && lngUsuario.HasValue)
        {
            var d = GeolocationHelper.CalcularDistanciaKm(
                latUsuario.Value, lngUsuario.Value,
                tienda.Latitud, tienda.Longitud);
            response.DistanciaKm = double.IsNaN(d) || double.IsInfinity(d) ? null : Math.Round(d, 2);
        }

        return response;
    }

    public Tienda Crear(CrearTiendaRequest request, int? usuarioId = null)
    {
        // Resolver país, moneda y datos regionales
        string? targetCountryCode = request.CountryCode;
        if (string.IsNullOrWhiteSpace(targetCountryCode) && usuarioId.HasValue)
        {
            var user = _context.Usuarios.Find(usuarioId.Value);
            targetCountryCode = user?.CountryCode;
        }

        var regionEntry = RegionCatalog.FindByCode(targetCountryCode) ?? RegionCatalog.GetDefault();
        var formattedWhatsApp = RegionCatalog.FormatPhoneForWhatsApp(request.WhatsApp, regionEntry.Code);

        var tienda = new Tienda
        {
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Telefono = request.Telefono,
            WhatsApp = formattedWhatsApp,
            Email = request.Email,
            Direccion = request.Direccion,
            Latitud = request.Latitud,
            Longitud = request.Longitud,
            Ciudad = request.Ciudad,
            Departamento = request.Departamento,
            CountryCode = regionEntry.Code,
            Pais = regionEntry.NameEs,
            CurrencyCode = regionEntry.CurrencyIso,
            CurrencySymbol = regionEntry.CurrencySymbol,
            HorarioApertura = request.HorarioApertura,
            HorarioCierre = request.HorarioCierre,
            DiasAtencion = request.DiasAtencion,
            LogoUrl = request.LogoUrl,
            FotoUrl = request.FotoUrl,
            EstaAbiertaManual = request.EstaAbiertaManual,
            FacebookUrl = request.FacebookUrl,
            InstagramUrl = request.InstagramUrl,
            TikTokUrl = request.TikTokUrl,
            Plan = SD.PlanFree,
            Activo = true,
            TokenPublico = TokenHelper.GenerarToken(24),
            Slug = SlugHelper.GenerarUnico(request.Nombre, _context.Tiendas, t => t.Slug),
            UsuarioId = usuarioId
        };

        _context.Tiendas.Add(tienda);
        _context.SaveChanges();

        // Sincronizar país con el usuario si aún no tenía uno configurado
        if (usuarioId.HasValue)
        {
            var user = _context.Usuarios.Find(usuarioId.Value);
            if (user != null && string.IsNullOrEmpty(user.CountryCode))
            {
                user.CountryCode = regionEntry.Code;
                _context.SaveChanges();
            }
        }

        FireAndForgetNotifyNewStoreNearby(tienda.Id);
        return tienda;
    }

    private void FireAndForgetNotifyNewStoreNearby(int tiendaId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<INotificationTriggerService>().NotifyNewStoreNearbyAsync(tiendaId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error en disparo NotifyNewStoreNearby para tienda {TiendaId}", tiendaId);
            }
        });
    }

    public bool Actualizar(int id, ActualizarTiendaRequest request)
    {
        var tienda = _context.Tiendas.Find(id);
        if (tienda == null) return false;

        if (request.Nombre != null) tienda.Nombre = request.Nombre;
        if (request.Descripcion != null) tienda.Descripcion = request.Descripcion;
        if (request.Telefono != null) tienda.Telefono = request.Telefono;
        if (request.WhatsApp != null)
        {
            tienda.WhatsApp = RegionCatalog.FormatPhoneForWhatsApp(request.WhatsApp, tienda.CountryCode);
        }
        if (request.Email != null) tienda.Email = request.Email;
        if (request.Direccion != null) tienda.Direccion = request.Direccion;
        if (request.Latitud.HasValue) tienda.Latitud = request.Latitud.Value;
        if (request.Longitud.HasValue) tienda.Longitud = request.Longitud.Value;
        if (request.Ciudad != null) tienda.Ciudad = request.Ciudad;
        if (request.Departamento != null) tienda.Departamento = request.Departamento;
        if (!string.IsNullOrWhiteSpace(request.CountryCode))
        {
            var entry = RegionCatalog.FindByCode(request.CountryCode);
            if (entry != null)
            {
                tienda.CountryCode = entry.Code;
                tienda.Pais = entry.NameEs;
                tienda.CurrencyCode = entry.CurrencyIso;
                tienda.CurrencySymbol = entry.CurrencySymbol;
                // Re-normalizar WhatsApp con el nuevo país si no se pasó uno nuevo
                if (request.WhatsApp == null && !string.IsNullOrEmpty(tienda.WhatsApp))
                {
                    tienda.WhatsApp = RegionCatalog.FormatPhoneForWhatsApp(tienda.WhatsApp, entry.Code);
                }
            }
        }
        if (request.HorarioApertura.HasValue) tienda.HorarioApertura = request.HorarioApertura;
        if (request.HorarioCierre.HasValue) tienda.HorarioCierre = request.HorarioCierre;
        if (request.DiasAtencion != null) tienda.DiasAtencion = request.DiasAtencion;
        if (request.LogoUrl != null) tienda.LogoUrl = request.LogoUrl;
        if (request.FotoUrl != null) tienda.FotoUrl = request.FotoUrl;
        if (request.EstaAbiertaManual.HasValue) tienda.EstaAbiertaManual = request.EstaAbiertaManual.Value;
        if (request.FacebookUrl != null) tienda.FacebookUrl = request.FacebookUrl;
        if (request.InstagramUrl != null) tienda.InstagramUrl = request.InstagramUrl;
        if (request.TikTokUrl != null) tienda.TikTokUrl = request.TikTokUrl;
        if (request.ExchangeRateMode != null) tienda.ExchangeRateMode = request.ExchangeRateMode;
        if (request.ExchangeRate.HasValue) tienda.ExchangeRate = request.ExchangeRate.Value;

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
        // [Plan desactivado temporalmente] Mientras BuscaYa sea gratis, no hay tope lógico.
        // Código original comentado para reactivar límite Free/Pro:
        // var tienda = _context.Tiendas.Find(tiendaId);
        // if (tienda == null) return false;
        // if (tienda.Plan == SD.PlanPro) return true;
        // var cantidadProductos = _context.Productos
        //     .Count(p => p.TiendaId == tiendaId && p.Activo);
        // return cantidadProductos < SD.LimiteProductosPlanFree;
        _ = tiendaId;
        return true;
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
