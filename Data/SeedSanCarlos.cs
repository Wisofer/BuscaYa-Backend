using BuscaYa.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Data;

public static class SeedSanCarlos
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Solo sincroniza categorías base.
        // Se deshabilitó el seed de usuarios/tiendas demo de San Carlos
        // para trabajar con datos reales en producción.
        await EnsureDefaultCategoriesAsync(context);
    }

    private static async Task EnsureDefaultCategoriesAsync(ApplicationDbContext context)
    {
        var now = DateTime.Now;
        var defaults = new (string Nombre, string Icono)[]
        {
            ("Ropa", "👕"),
            ("Calzado", "👟"),
            ("Accesorios", "👜"),
            ("Belleza", "💄"),
            ("Bebés", "🍼"),
            ("Juguetes", "🧸"),
            ("Deportes", "⚽"),
            ("Celulares", "📱"),
            ("Electrónica", "💻"),
            ("Repuestos", "🔩"),
            ("Hogar", "🏠"),
            ("Mascotas", "🐾"),
            ("Super", "🛒"),
            ("Farmacia", "💊"),
            ("Ferretería", "🔨")
        };
        var defaultNames = defaults.Select(d => d.Nombre).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existing = await context.Categorias.ToListAsync();
        var byName = existing.ToDictionary(c => c.Nombre, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < defaults.Length; i++)
        {
            var def = defaults[i];
            var orden = i + 1;
            if (byName.TryGetValue(def.Nombre, out var categoria))
            {
                categoria.Icono = def.Icono;
                categoria.Orden = orden;
                categoria.Activo = true;
            }
            else
            {
                context.Categorias.Add(new Categoria
                {
                    Nombre = def.Nombre,
                    Icono = def.Icono,
                    Orden = orden,
                    Activo = true,
                    FechaCreacion = now
                });
            }
        }

        await context.SaveChangesAsync();

        // Eliminar categorías viejas para conservar solo las 15 oficiales.
        // Si una categoría vieja está en uso, sus productos se reasignan a "Hogar".
        var hogar = await context.Categorias.FirstAsync(c => c.Nombre == "Hogar");
        var oldCategories = await context.Categorias
            .Where(c => !defaultNames.Contains(c.Nombre))
            .ToListAsync();

        if (oldCategories.Count > 0)
        {
            var oldCategoryIds = oldCategories.Select(c => c.Id).ToList();
            var productos = await context.Productos
                .Where(p => oldCategoryIds.Contains(p.CategoriaId))
                .ToListAsync();

            foreach (var producto in productos)
            {
                producto.CategoriaId = hogar.Id;
            }

            context.Categorias.RemoveRange(oldCategories);
        }

        await context.SaveChangesAsync();
    }
}
