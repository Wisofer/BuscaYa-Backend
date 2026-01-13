using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ApplicationDbContext _context;

    public CategoriaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Categoria> ObtenerTodas()
    {
        return _context.Categorias.OrderBy(c => c.Orden).ThenBy(c => c.Nombre).ToList();
    }

    public List<Categoria> ObtenerActivas()
    {
        return _context.Categorias
            .Where(c => c.Activo)
            .OrderBy(c => c.Orden)
            .ThenBy(c => c.Nombre)
            .ToList();
    }

    public Categoria? ObtenerPorId(int id)
    {
        return _context.Categorias.Find(id);
    }

    public Categoria Crear(string nombre, string? icono = null)
    {
        var categoria = new Categoria
        {
            Nombre = nombre,
            Icono = icono,
            Activo = true,
            Orden = _context.Categorias.Count() + 1
        };

        _context.Categorias.Add(categoria);
        _context.SaveChanges();
        return categoria;
    }

    public bool Actualizar(int id, string nombre, string? icono = null)
    {
        var categoria = _context.Categorias.Find(id);
        if (categoria == null) return false;

        categoria.Nombre = nombre;
        if (icono != null) categoria.Icono = icono;
        categoria.FechaCreacion = DateTime.Now;

        _context.SaveChanges();
        return true;
    }

    public bool Activar(int id)
    {
        var categoria = _context.Categorias.Find(id);
        if (categoria == null) return false;

        categoria.Activo = true;
        _context.SaveChanges();
        return true;
    }

    public bool Desactivar(int id)
    {
        var categoria = _context.Categorias.Find(id);
        if (categoria == null) return false;

        categoria.Activo = false;
        _context.SaveChanges();
        return true;
    }
}
