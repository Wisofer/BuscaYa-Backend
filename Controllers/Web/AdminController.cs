using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuscaYa.Data;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using BuscaYa.Attributes;

namespace BuscaYa.Controllers.Web;

[Authorize(Policy = "Administrador")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ITiendaService _tiendaService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ICategoriaService _categoriaService;
    private readonly IReporteService _reporteService;

    public AdminController(
        ApplicationDbContext context,
        ITiendaService tiendaService,
        IAnalyticsService analyticsService,
        ICategoriaService categoriaService,
        IReporteService reporteService)
    {
        _context = context;
        _tiendaService = tiendaService;
        _analyticsService = analyticsService;
        _categoriaService = categoriaService;
        _reporteService = reporteService;
    }

    [HttpGet("/admin")]
    [HttpGet("/admin/dashboard")]
    public IActionResult Dashboard()
    {
        // Estadísticas generales
        var totalUsuarios = _context.Usuarios.Count();
        var totalClientes = _context.Usuarios.Count(u => u.Rol == SD.RolCliente);
        var totalTiendas = _context.Tiendas.Count();
        var totalProductos = _context.Productos.Count();
        var totalCategorias = _context.Categorias.Count();
        var tiendasActivas = _context.Tiendas.Count(t => t.Activo);
        var usuariosConTienda = _context.Usuarios.Count(u => u.TiendaId != null);
        var usuariosRecientes = _context.Usuarios
            .OrderByDescending(u => u.Id)
            .Take(5)
            .Select(u => new
            {
                u.Id,
                u.NombreUsuario,
                u.NombreCompleto,
                u.Rol,
                u.Email,
                u.Telefono,
                TieneTienda = u.TiendaId != null
            })
            .ToList();

        var totalReportesNoRevisados = _context.Reportes.Count(r => !r.Revisado);

        ViewBag.TotalUsuarios = totalUsuarios;
        ViewBag.TotalClientes = totalClientes;
        ViewBag.TotalTiendas = totalTiendas;
        ViewBag.TotalProductos = totalProductos;
        ViewBag.TotalCategorias = totalCategorias;
        ViewBag.TiendasActivas = tiendasActivas;
        ViewBag.UsuariosConTienda = usuariosConTienda;
        ViewBag.UsuariosRecientes = usuariosRecientes;
        ViewBag.TotalReportesNoRevisados = totalReportesNoRevisados;

        return View();
    }

    [HttpGet("/admin/usuarios")]
    public IActionResult Usuarios(string? rol, string? busqueda, int pagina = 1, int tamanoPagina = 20)
    {
        var query = _context.Usuarios.AsQueryable();

        // Filtrar por rol
        if (!string.IsNullOrEmpty(rol) && rol != "Todos")
        {
            query = query.Where(u => u.Rol == rol);
        }

        // Buscar por nombre o usuario
        if (!string.IsNullOrEmpty(busqueda))
        {
            query = query.Where(u => 
                u.NombreUsuario.Contains(busqueda) || 
                u.NombreCompleto.Contains(busqueda) ||
                (u.Email != null && u.Email.Contains(busqueda)));
        }

        var total = query.Count();
        var usuarios = query
            .Include(u => u.Tienda)
            .OrderByDescending(u => u.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(u => new
            {
                u.Id,
                u.NombreUsuario,
                u.NombreCompleto,
                u.Rol,
                u.Email,
                u.Telefono,
                u.Activo,
                TiendaId = u.TiendaId,
                TiendaNombre = u.Tienda != null ? u.Tienda.Nombre : null,
                LoginConGoogle = u.GoogleId != null
            })
            .ToList();

        ViewBag.Usuarios = usuarios;
        ViewBag.Total = total;
        ViewBag.Pagina = pagina;
        ViewBag.TamanoPagina = tamanoPagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)tamanoPagina);
        ViewBag.RolFiltro = rol ?? "Todos";
        ViewBag.Busqueda = busqueda ?? "";

        return View();
    }

    [HttpGet("/admin/usuarios/{id}")]
    public IActionResult DetalleUsuario(int id)
    {
        var usuario = _context.Usuarios
            .Include(u => u.Tienda)
            .Include(u => u.Favoritos)
            .Include(u => u.HistorialBusquedas)
            .Include(u => u.DireccionesGuardadas)
            .FirstOrDefault(u => u.Id == id);

        if (usuario == null)
        {
            return NotFound();
        }

        ViewBag.Usuario = usuario;
        return View();
    }

    [HttpPost("/admin/usuarios/{id}/activar")]
    public IActionResult ActivarUsuario(int id)
    {
        var usuario = _context.Usuarios.Find(id);
        if (usuario == null)
        {
            return NotFound();
        }

        usuario.Activo = true;
        _context.SaveChanges();

        TempData["Mensaje"] = "Usuario activado correctamente";
        return RedirectToAction("DetalleUsuario", new { id });
    }

    [HttpPost("/admin/usuarios/{id}/desactivar")]
    public IActionResult DesactivarUsuario(int id)
    {
        var usuario = _context.Usuarios.Find(id);
        if (usuario == null)
        {
            return NotFound();
        }

        usuario.Activo = false;
        _context.SaveChanges();

        TempData["Mensaje"] = "Usuario desactivado correctamente";
        return RedirectToAction("DetalleUsuario", new { id });
    }

    [HttpGet("/admin/tiendas")]
    public IActionResult Tiendas(string? ciudad, bool? activo, string? busqueda, int pagina = 1, int tamanoPagina = 20)
    {
        var query = _context.Tiendas
            .Include(t => t.Usuario)
            .Include(t => t.Productos)
            .AsQueryable();

        // Filtros
        if (!string.IsNullOrEmpty(ciudad) && ciudad != "Todas")
        {
            query = query.Where(t => t.Ciudad == ciudad);
        }

        if (activo.HasValue)
        {
            query = query.Where(t => t.Activo == activo.Value);
        }

        if (!string.IsNullOrEmpty(busqueda))
        {
            query = query.Where(t => 
                t.Nombre.Contains(busqueda) || 
                t.Descripcion != null && t.Descripcion.Contains(busqueda));
        }

        var total = query.Count();
        var tiendas = query
            .OrderByDescending(t => t.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(t => new
            {
                t.Id,
                t.Nombre,
                t.Descripcion,
                t.Ciudad,
                t.Departamento,
                t.Activo,
                t.Plan,
                UsuarioNombre = t.Usuario != null ? t.Usuario.NombreUsuario : null,
                TotalProductos = t.Productos.Count
            })
            .ToList();

        var ciudades = _context.Tiendas
            .Select(t => t.Ciudad)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        ViewBag.Tiendas = tiendas;
        ViewBag.Total = total;
        ViewBag.Pagina = pagina;
        ViewBag.TamanoPagina = tamanoPagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)tamanoPagina);
        ViewBag.Ciudades = ciudades;
        ViewBag.CiudadFiltro = ciudad ?? "Todas";
        ViewBag.ActivoFiltro = activo;
        ViewBag.Busqueda = busqueda ?? "";

        return View();
    }

    [HttpGet("/admin/tiendas/{id}")]
    public IActionResult DetalleTienda(int id)
    {
        var tienda = _context.Tiendas
            .Include(t => t.Usuario)
            .Include(t => t.Productos)
            .ThenInclude(p => p.Categoria)
            .FirstOrDefault(t => t.Id == id);

        if (tienda == null)
        {
            return NotFound();
        }

        // Estadísticas de la tienda
        var desde = DateTime.Now.AddDays(-30);
        var hasta = DateTime.Now;
        var estadisticas = _analyticsService.ObtenerEstadisticasTienda(id, desde, hasta);

        ViewBag.Tienda = tienda;
        ViewBag.Estadisticas = estadisticas;
        return View();
    }

    [HttpPost("/admin/tiendas/{id}/activar")]
    public IActionResult ActivarTienda(int id)
    {
        var activado = _tiendaService.Activar(id);
        if (!activado)
        {
            return NotFound();
        }

        TempData["Mensaje"] = "Tienda activada correctamente";
        return RedirectToAction("DetalleTienda", new { id });
    }

    [HttpPost("/admin/tiendas/{id}/desactivar")]
    public IActionResult DesactivarTienda(int id)
    {
        var desactivado = _tiendaService.Desactivar(id);
        if (!desactivado)
        {
            return NotFound();
        }

        TempData["Mensaje"] = "Tienda desactivada correctamente";
        return RedirectToAction("DetalleTienda", new { id });
    }

    [HttpPost("/admin/tiendas/{id}/plan")]
    public IActionResult CambiarPlan(int id, string plan)
    {
        if (plan != SD.PlanFree && plan != SD.PlanPro)
        {
            TempData["Error"] = "Plan inválido";
            return RedirectToAction("DetalleTienda", new { id });
        }

        var cambiado = _tiendaService.CambiarPlan(id, plan);
        if (!cambiado)
        {
            return NotFound();
        }

        TempData["Mensaje"] = $"Plan cambiado a {plan}";
        return RedirectToAction("DetalleTienda", new { id });
    }

    [HttpGet("/admin/configuraciones")]
    public IActionResult Configuraciones()
    {
        var categorias = _categoriaService.ObtenerTodas();
        ViewBag.Categorias = categorias;
        return View();
    }

    [HttpPost("/admin/configuraciones/categoria")]
    public IActionResult CrearCategoria(string nombre, string? icono)
    {
        try
        {
            var categoria = _categoriaService.Crear(nombre, icono);
            TempData["Mensaje"] = "Categoría creada correctamente";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al crear categoría: {ex.Message}";
        }

        return RedirectToAction("Configuraciones");
    }

    [HttpPost("/admin/configuraciones/categoria/{id}/activar")]
    public IActionResult ActivarCategoria(int id)
    {
        try
        {
            _categoriaService.Activar(id);
            TempData["Mensaje"] = "Categoría activada correctamente";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return RedirectToAction("Configuraciones");
    }

    [HttpPost("/admin/configuraciones/categoria/{id}/desactivar")]
    public IActionResult DesactivarCategoria(int id)
    {
        try
        {
            _categoriaService.Desactivar(id);
            TempData["Mensaje"] = "Categoría desactivada correctamente";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return RedirectToAction("Configuraciones");
    }

    [HttpGet("/admin/reportes")]
    public async Task<IActionResult> Reportes(bool? soloNoRevisados = null, string? tipo = null, int pagina = 1, int tamanoPagina = 20)
    {
        var reportes = await _reporteService.ObtenerTodosAsync(soloNoRevisados);

        // Filtrar por tipo si se especifica
        if (!string.IsNullOrEmpty(tipo) && tipo != "Todos")
        {
            reportes = reportes.Where(r => r.Tipo.ToLower() == tipo.ToLower()).ToList();
        }

        var total = reportes.Count;
        var reportesPaginados = reportes
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        // Obtener información adicional de productos/tiendas reportados
        var reportesConInfo = reportesPaginados.Select(r =>
        {
            string? nombreRecurso = null;
            if (r.Tipo.ToLower() == "producto")
            {
                var producto = _context.Productos.Find(r.RecursoId);
                nombreRecurso = producto?.Nombre;
            }
            else if (r.Tipo.ToLower() == "tienda")
            {
                var tienda = _context.Tiendas.Find(r.RecursoId);
                nombreRecurso = tienda?.Nombre;
            }

            return new
            {
                r.Id,
                r.Tipo,
                r.RecursoId,
                NombreRecurso = nombreRecurso,
                r.Razon,
                r.Detalle,
                r.Revisado,
                r.NotaAdmin,
                r.FechaCreacion,
                FechaRevisadoTexto = r.FechaRevisado.HasValue ? r.FechaRevisado.Value.ToString("dd/MM/yyyy") : (string?)null,
                UsuarioNombre = r.Usuario?.NombreUsuario,
                UsuarioCompleto = r.Usuario?.NombreCompleto
            };
        }).ToList();

        ViewBag.Reportes = reportesConInfo;
        ViewBag.Total = total;
        ViewBag.Pagina = pagina;
        ViewBag.TamanoPagina = tamanoPagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)tamanoPagina);
        ViewBag.SoloNoRevisados = soloNoRevisados;
        ViewBag.TipoFiltro = tipo ?? "Todos";
        ViewBag.TotalNoRevisados = reportes.Count(r => !r.Revisado);

        return View();
    }

    [HttpPost("/admin/reportes/{id}/revisar")]
    public async Task<IActionResult> MarcarReporteComoRevisado(int id, string? notaAdmin)
    {
        var revisado = await _reporteService.MarcarComoRevisadoAsync(id, notaAdmin);
        if (!revisado)
        {
            TempData["Error"] = "Reporte no encontrado";
            return RedirectToAction("Reportes");
        }

        TempData["Mensaje"] = "Reporte marcado como revisado";
        return RedirectToAction("Reportes");
    }

    [HttpGet("/admin/reportes/{id}")]
    public async Task<IActionResult> DetalleReporte(int id)
    {
        var reporte = await _reporteService.ObtenerPorIdAsync(id);
        if (reporte == null)
        {
            return NotFound();
        }

        // Obtener información del recurso reportado
        object? recurso = null;
        if (reporte.Tipo.ToLower() == "producto")
        {
            recurso = _context.Productos
                .Include(p => p.Tienda)
                .Include(p => p.Categoria)
                .FirstOrDefault(p => p.Id == reporte.RecursoId);
        }
        else if (reporte.Tipo.ToLower() == "tienda")
        {
            recurso = _context.Tiendas
                .Include(t => t.Usuario)
                .FirstOrDefault(t => t.Id == reporte.RecursoId);
        }

        ViewBag.Reporte = reporte;
        ViewBag.Recurso = recurso;

        return View();
    }
}
