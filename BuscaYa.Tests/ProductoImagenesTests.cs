using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Services;
using BuscaYa.Utils;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuscaYa.Tests;

/// <summary>
/// Tests que simulan y verifican el flujo de imágenes de producto (galería).
/// Comprueban que crear/actualizar con imagenesUrls y la construcción de galeriaUrls funcionan al 100%.
/// </summary>
public class ProductoImagenesTests
{
    private static ApplicationDbContext CrearContextoUnico()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static (ApplicationDbContext ctx, int tiendaId, int categoriaId) SeedTiendaYCategoria(ApplicationDbContext context)
    {
        var categoria = new Categoria
        {
            Nombre = "Test Categoria",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        context.Categorias.Add(categoria);

        var tienda = new Tienda
        {
            Nombre = "Tienda Test",
            WhatsApp = "88881234",
            Direccion = "Test 123",
            Ciudad = "Managua",
            Departamento = "Managua",
            Latitud = 12.136389m,
            Longitud = -86.251389m,
            Plan = SD.PlanFree,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        context.Tiendas.Add(tienda);
        context.SaveChanges();

        return (context, tienda.Id, categoria.Id);
    }

    [Fact]
    public void Crear_ConImagenesUrls_GuardaProductoYGalería()
    {
        using var context = CrearContextoUnico();
        var (_, tiendaId, categoriaId) = SeedTiendaYCategoria(context);
        var service = new ProductoService(context);

        var urls = new List<string>
        {
            "https://cdn.ejemplo.com/img1.jpg",
            "https://cdn.ejemplo.com/img2.jpg",
            "https://cdn.ejemplo.com/img3.jpg"
        };
        var request = new CrearProductoRequest
        {
            Nombre = "Producto con galería",
            Descripcion = "Desc",
            Precio = 100,
            Moneda = "C$",
            CategoriaId = categoriaId,
            ImagenesUrls = urls
        };

        var producto = service.Crear(tiendaId, request);

        Assert.NotNull(producto);
        Assert.True(producto.Id > 0);
        Assert.Equal("https://cdn.ejemplo.com/img1.jpg", producto.FotoUrl);

        var imagenesEnDb = context.ProductoImagenes.Where(pi => pi.ProductoId == producto.Id).OrderBy(pi => pi.Orden).ToList();
        Assert.Equal(3, imagenesEnDb.Count);
        Assert.Equal(urls[0], imagenesEnDb[0].Url);
        Assert.Equal(urls[1], imagenesEnDb[1].Url);
        Assert.Equal(urls[2], imagenesEnDb[2].Url);
        Assert.Equal(0, imagenesEnDb[0].Orden);
        Assert.Equal(1, imagenesEnDb[1].Orden);
        Assert.Equal(2, imagenesEnDb[2].Orden);
    }

    [Fact]
    public void Crear_SinImagenesUrls_SoloGuardaFotoUrlSiSeEnvía()
    {
        using var context = CrearContextoUnico();
        var (_, tiendaId, categoriaId) = SeedTiendaYCategoria(context);
        var service = new ProductoService(context);

        var request = new CrearProductoRequest
        {
            Nombre = "Producto una foto",
            CategoriaId = categoriaId,
            FotoUrl = "https://cdn.ejemplo.com/principal.jpg"
        };

        var producto = service.Crear(tiendaId, request);

        Assert.NotNull(producto);
        Assert.Equal("https://cdn.ejemplo.com/principal.jpg", producto.FotoUrl);
        var imagenesEnDb = context.ProductoImagenes.Count(pi => pi.ProductoId == producto.Id);
        Assert.Equal(0, imagenesEnDb);
    }

    [Fact]
    public void ObtenerPorId_ConImagenes_DevuelveImagenesOrdenadas()
    {
        using var context = CrearContextoUnico();
        var (_, tiendaId, categoriaId) = SeedTiendaYCategoria(context);
        var service = new ProductoService(context);

        var request = new CrearProductoRequest
        {
            Nombre = "Producto galería",
            CategoriaId = categoriaId,
            ImagenesUrls = new List<string> { "https://a.com/1.jpg", "https://a.com/2.jpg", "https://a.com/3.jpg" }
        };
        var creado = service.Crear(tiendaId, request);

        var producto = service.ObtenerPorId(creado.Id);

        Assert.NotNull(producto);
        Assert.NotNull(producto.Imagenes);
        var imagenesOrdenadas = producto.Imagenes.OrderBy(i => i.Orden).ToList();
        Assert.Equal(3, imagenesOrdenadas.Count);
        Assert.Equal("https://a.com/1.jpg", imagenesOrdenadas[0].Url);
        Assert.Equal("https://a.com/2.jpg", imagenesOrdenadas[1].Url);
        Assert.Equal("https://a.com/3.jpg", imagenesOrdenadas[2].Url);
        Assert.Equal(0, imagenesOrdenadas[0].Orden);
        Assert.Equal(1, imagenesOrdenadas[1].Orden);
        Assert.Equal(2, imagenesOrdenadas[2].Orden);
    }

    /// <summary>
    /// Simula exactamente cómo PublicController construye galeriaUrls para el detalle público.
    /// </summary>
    [Fact]
    public void GaleriaUrls_SeConstruyeComoEnPublicController()
    {
        using var context = CrearContextoUnico();
        var (_, tiendaId, categoriaId) = SeedTiendaYCategoria(context);
        var service = new ProductoService(context);

        var urls = new List<string> { "https://p.com/1.jpg", "https://p.com/2.jpg", "https://p.com/3.jpg" };
        var creado = service.Crear(tiendaId, new CrearProductoRequest
        {
            Nombre = "Prod",
            CategoriaId = categoriaId,
            ImagenesUrls = urls
        });

        var producto = service.ObtenerPorId(creado.Id);
        Assert.NotNull(producto);

        // Misma lógica que PublicController.ObtenerProducto
        var galeriaUrls = new List<string>();
        if (!string.IsNullOrEmpty(producto.FotoUrl))
            galeriaUrls.Add(producto.FotoUrl);
        if (producto.Imagenes != null)
        {
            var otras = producto.Imagenes.OrderBy(i => i.Orden).Select(i => i.Url).Where(u => u != producto.FotoUrl).ToList();
            galeriaUrls.AddRange(otras);
        }

        Assert.Equal(3, galeriaUrls.Count);
        Assert.Equal("https://p.com/1.jpg", galeriaUrls[0]);
        Assert.Equal("https://p.com/2.jpg", galeriaUrls[1]);
        Assert.Equal("https://p.com/3.jpg", galeriaUrls[2]);
    }

    [Fact]
    public void Actualizar_ConImagenesUrls_ReemplazaGalería()
    {
        using var context = CrearContextoUnico();
        var (_, tiendaId, categoriaId) = SeedTiendaYCategoria(context);
        var service = new ProductoService(context);

        var creado = service.Crear(tiendaId, new CrearProductoRequest
        {
            Nombre = "Original",
            CategoriaId = categoriaId,
            ImagenesUrls = new List<string> { "https://old.com/1.jpg", "https://old.com/2.jpg" }
        });

        var nuevasUrls = new List<string> { "https://new.com/a.jpg", "https://new.com/b.jpg", "https://new.com/c.jpg" };
        var actualizado = service.Actualizar(creado.Id, new ActualizarProductoRequest
        {
            ImagenesUrls = nuevasUrls
        });

        Assert.True(actualizado);

        var producto = service.ObtenerPorId(creado.Id);
        Assert.NotNull(producto);
        Assert.Equal("https://new.com/a.jpg", producto.FotoUrl);
        var imagenesOrdenadas = producto.Imagenes.OrderBy(i => i.Orden).ToList();
        Assert.Equal(3, imagenesOrdenadas.Count);
        Assert.Equal("https://new.com/a.jpg", imagenesOrdenadas[0].Url);
        Assert.Equal("https://new.com/b.jpg", imagenesOrdenadas[1].Url);
        Assert.Equal("https://new.com/c.jpg", imagenesOrdenadas[2].Url);

        var countOld = context.ProductoImagenes.Count(pi => pi.Url.Contains("old.com"));
        Assert.Equal(0, countOld);
    }

    [Fact]
    public void Actualizar_ConImagenesUrlsYFotoUrl_UsaFotoUrlComoPrincipal()
    {
        using var context = CrearContextoUnico();
        var (_, tiendaId, categoriaId) = SeedTiendaYCategoria(context);
        var service = new ProductoService(context);

        var creado = service.Crear(tiendaId, new CrearProductoRequest
        {
            Nombre = "Prod",
            CategoriaId = categoriaId,
            ImagenesUrls = new List<string> { "https://x.com/1.jpg", "https://x.com/2.jpg" }
        });

        service.Actualizar(creado.Id, new ActualizarProductoRequest
        {
            FotoUrl = "https://x.com/portada.jpg",
            ImagenesUrls = new List<string> { "https://x.com/1.jpg", "https://x.com/2.jpg" }
        });

        var producto = service.ObtenerPorId(creado.Id);
        Assert.NotNull(producto);
        Assert.Equal("https://x.com/portada.jpg", producto.FotoUrl);
    }
}
