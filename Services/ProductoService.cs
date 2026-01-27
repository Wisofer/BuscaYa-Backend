using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Services;

public class ProductoService : IProductoService
{
    private readonly ApplicationDbContext _context;

    public ProductoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Producto> ObtenerPorTienda(int tiendaId)
    {
        return _context.Productos
            .Where(p => p.TiendaId == tiendaId && p.Activo)
            .Include(p => p.Categoria)
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .ToList();
    }

    public Producto? ObtenerPorId(int id)
    {
        return _context.Productos
            .Include(p => p.Tienda)
            .Include(p => p.Categoria)
            .AsNoTracking()
            .FirstOrDefault(p => p.Id == id);
    }

    public Producto Crear(int tiendaId, CrearProductoRequest request)
    {
        // Verificar límite de productos según plan
        var tienda = _context.Tiendas.Find(tiendaId);
        if (tienda == null)
        {
            throw new InvalidOperationException("Tienda no encontrada");
        }

        if (tienda.Plan != SD.PlanPro)
        {
            var cantidadProductos = _context.Productos
                .Count(p => p.TiendaId == tiendaId && p.Activo);

            if (cantidadProductos >= SD.LimiteProductosPlanFree)
            {
                throw new InvalidOperationException("Has alcanzado el límite de productos para tu plan. Actualiza a Plan Pro.");
            }
        }

        var producto = new Producto
        {
            TiendaId = tiendaId,
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Precio = request.Precio,
            Moneda = request.Moneda,
            CategoriaId = request.CategoriaId,
            FotoUrl = request.FotoUrl,
            Activo = true
        };

        _context.Productos.Add(producto);
        _context.SaveChanges();
        return producto;
    }

    public bool Actualizar(int id, ActualizarProductoRequest request)
    {
        var producto = _context.Productos.Find(id);
        if (producto == null) return false;

        if (request.Nombre != null) producto.Nombre = request.Nombre;
        if (request.Descripcion != null) producto.Descripcion = request.Descripcion;
        if (request.Precio.HasValue) producto.Precio = request.Precio;
        if (request.Moneda != null) producto.Moneda = request.Moneda;
        if (request.CategoriaId.HasValue) producto.CategoriaId = request.CategoriaId.Value;
        if (request.FotoUrl != null) producto.FotoUrl = request.FotoUrl;
        if (request.Activo.HasValue) producto.Activo = request.Activo.Value;
        
        producto.FechaActualizacion = DateTime.Now;
        _context.SaveChanges();
        return true;
    }

    public bool Eliminar(int id)
    {
        var producto = _context.Productos.Find(id);
        if (producto == null) return false;

        // Soft delete
        producto.Activo = false;
        producto.FechaActualizacion = DateTime.Now;
        _context.SaveChanges();
        return true;
    }

    public bool VerificarPerteneceATienda(int productoId, int tiendaId)
    {
        return _context.Productos
            .Any(p => p.Id == productoId && p.TiendaId == tiendaId);
    }
}
