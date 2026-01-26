using BuscaYa.Models.Entities;
using BuscaYa.Utils;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Data;

public static class SeedSanCarlos
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Verificar si ya hay datos
        if (await context.Tiendas.AnyAsync(t => t.Ciudad == "San Carlos"))
        {
            return; // Ya hay datos, no hacer nada
        }

        // Hash de la contraseña "wisofer17"
        var passwordHash = PasswordHelper.HashPassword("wisofer17");

        // Coordenadas de San Carlos, Río San Juan (centro de la ciudad)
        // Variaciones pequeñas para que las tiendas estén en diferentes ubicaciones
        var coordenadas = new[]
        {
            (Lat: 11.1333m, Lng: 84.7833m), // Centro
            (Lat: 11.1400m, Lng: 84.7900m), // Norte
            (Lat: 11.1266m, Lng: 84.7766m), // Sur
            (Lat: 11.1333m, Lng: 84.8000m), // Este
            (Lat: 11.1333m, Lng: 84.7666m), // Oeste
        };

        // ========== USUARIOS CON TIENDA (5) ==========
        
        // 1. Ferretería El Constructor
        var usuario1 = new Usuario
        {
            NombreUsuario = "ferreteria_constructor",
            Contrasena = passwordHash,
            Rol = SD.RolTiendaOwner,
            NombreCompleto = "Carlos Ramírez",
            Telefono = "50588881234",
            Email = "carlos.ramirez@ferreteria.com",
            Activo = true,
            FechaCreacion = DateTime.Now
        };
        context.Usuarios.Add(usuario1);
        await context.SaveChangesAsync();

        var tienda1 = new Tienda
        {
            Nombre = "Ferretería El Constructor",
            Descripcion = "Todo en materiales de construcción, herramientas y pinturas. Servicio de calidad desde 2010.",
            Telefono = "50588881234",
            WhatsApp = "50588881234",
            Email = "carlos.ramirez@ferreteria.com",
            Direccion = "Calle Principal, 2 cuadras al sur del parque central",
            Latitud = coordenadas[0].Lat,
            Longitud = coordenadas[0].Lng,
            Ciudad = "San Carlos",
            Departamento = "Río San Juan",
            HorarioApertura = new TimeSpan(7, 0, 0),
            HorarioCierre = new TimeSpan(18, 0, 0),
            DiasAtencion = "Lunes a Sábado",
            Plan = SD.PlanFree,
            Activo = true,
            UsuarioId = usuario1.Id,
            FechaCreacion = DateTime.Now
        };
        context.Tiendas.Add(tienda1);
        await context.SaveChangesAsync();
        usuario1.TiendaId = tienda1.Id;
        await context.SaveChangesAsync();

        // 2. Farmacia San Carlos
        var usuario2 = new Usuario
        {
            NombreUsuario = "farmacia_sancarlos",
            Contrasena = passwordHash,
            Rol = SD.RolTiendaOwner,
            NombreCompleto = "María González",
            Telefono = "50588882345",
            Email = "maria.gonzalez@farmacia.com",
            Activo = true,
            FechaCreacion = DateTime.Now
        };
        context.Usuarios.Add(usuario2);
        await context.SaveChangesAsync();

        var tienda2 = new Tienda
        {
            Nombre = "Farmacia San Carlos",
            Descripcion = "Medicamentos, productos de higiene personal y cuidado de la salud. Atención las 24 horas.",
            Telefono = "50588882345",
            WhatsApp = "50588882345",
            Email = "maria.gonzalez@farmacia.com",
            Direccion = "Avenida Central, frente al mercado municipal",
            Latitud = coordenadas[1].Lat,
            Longitud = coordenadas[1].Lng,
            Ciudad = "San Carlos",
            Departamento = "Río San Juan",
            HorarioApertura = new TimeSpan(0, 0, 0),
            HorarioCierre = new TimeSpan(23, 59, 59),
            DiasAtencion = "Todos los días",
            Plan = SD.PlanFree,
            Activo = true,
            UsuarioId = usuario2.Id,
            FechaCreacion = DateTime.Now
        };
        context.Tiendas.Add(tienda2);
        await context.SaveChangesAsync();
        usuario2.TiendaId = tienda2.Id;
        await context.SaveChangesAsync();

        // 3. Supermercado El Ahorro
        var usuario3 = new Usuario
        {
            NombreUsuario = "super_ahorro",
            Contrasena = passwordHash,
            Rol = SD.RolTiendaOwner,
            NombreCompleto = "José Martínez",
            Telefono = "50588883456",
            Email = "jose.martinez@super.com",
            Activo = true,
            FechaCreacion = DateTime.Now
        };
        context.Usuarios.Add(usuario3);
        await context.SaveChangesAsync();

        var tienda3 = new Tienda
        {
            Nombre = "Supermercado El Ahorro",
            Descripcion = "Productos de primera necesidad, abarrotes, carnes, frutas y verduras frescas.",
            Telefono = "50588883456",
            WhatsApp = "50588883456",
            Email = "jose.martinez@super.com",
            Direccion = "Barrio El Centro, 1 cuadra al este de la iglesia",
            Latitud = coordenadas[2].Lat,
            Longitud = coordenadas[2].Lng,
            Ciudad = "San Carlos",
            Departamento = "Río San Juan",
            HorarioApertura = new TimeSpan(6, 0, 0),
            HorarioCierre = new TimeSpan(20, 0, 0),
            DiasAtencion = "Lunes a Domingo",
            Plan = SD.PlanFree,
            Activo = true,
            UsuarioId = usuario3.Id,
            FechaCreacion = DateTime.Now
        };
        context.Tiendas.Add(tienda3);
        await context.SaveChangesAsync();
        usuario3.TiendaId = tienda3.Id;
        await context.SaveChangesAsync();

        // 4. Ferretería La Esquina
        var usuario4 = new Usuario
        {
            NombreUsuario = "ferreteria_esquina",
            Contrasena = passwordHash,
            Rol = SD.RolTiendaOwner,
            NombreCompleto = "Julio Pérez",
            Telefono = "50588884567",
            Email = "julio.perez@esquina.com",
            Activo = true,
            FechaCreacion = DateTime.Now
        };
        context.Usuarios.Add(usuario4);
        await context.SaveChangesAsync();

        var tienda4 = new Tienda
        {
            Nombre = "Ferretería La Esquina",
            Descripcion = "Herramientas, materiales eléctricos, plomería y más. Los mejores precios de la ciudad.",
            Telefono = "50588884567",
            WhatsApp = "50588884567",
            Email = "julio.perez@esquina.com",
            Direccion = "Esquina de la Calle Principal y Avenida del Lago",
            Latitud = coordenadas[3].Lat,
            Longitud = coordenadas[3].Lng,
            Ciudad = "San Carlos",
            Departamento = "Río San Juan",
            HorarioApertura = new TimeSpan(7, 30, 0),
            HorarioCierre = new TimeSpan(17, 30, 0),
            DiasAtencion = "Lunes a Viernes",
            Plan = SD.PlanFree,
            Activo = true,
            UsuarioId = usuario4.Id,
            FechaCreacion = DateTime.Now
        };
        context.Tiendas.Add(tienda4);
        await context.SaveChangesAsync();
        usuario4.TiendaId = tienda4.Id;
        await context.SaveChangesAsync();

        // 5. Tienda de Ropa y Calzado Moda Joven
        var usuario5 = new Usuario
        {
            NombreUsuario = "moda_joven",
            Contrasena = passwordHash,
            Rol = SD.RolTiendaOwner,
            NombreCompleto = "Ana López",
            Telefono = "50588885678",
            Email = "ana.lopez@moda.com",
            Activo = true,
            FechaCreacion = DateTime.Now
        };
        context.Usuarios.Add(usuario5);
        await context.SaveChangesAsync();

        var tienda5 = new Tienda
        {
            Nombre = "Moda Joven",
            Descripcion = "Ropa, calzado y accesorios para toda la familia. Las últimas tendencias de moda.",
            Telefono = "50588885678",
            WhatsApp = "50588885678",
            Email = "ana.lopez@moda.com",
            Direccion = "Calle Comercial, local #15",
            Latitud = coordenadas[4].Lat,
            Longitud = coordenadas[4].Lng,
            Ciudad = "San Carlos",
            Departamento = "Río San Juan",
            HorarioApertura = new TimeSpan(8, 0, 0),
            HorarioCierre = new TimeSpan(19, 0, 0),
            DiasAtencion = "Lunes a Sábado",
            Plan = SD.PlanFree,
            Activo = true,
            UsuarioId = usuario5.Id,
            FechaCreacion = DateTime.Now
        };
        context.Tiendas.Add(tienda5);
        await context.SaveChangesAsync();
        usuario5.TiendaId = tienda5.Id;
        await context.SaveChangesAsync();

        // ========== USUARIOS SIN TIENDA (3 CLIENTES) ==========

        // Cliente 1: Martín (el que busca)
        var cliente1 = new Usuario
        {
            NombreUsuario = "martin_busca",
            Contrasena = passwordHash,
            Rol = SD.RolCliente,
            NombreCompleto = "Martín Rodríguez",
            Telefono = "50588886789",
            Email = "martin.rodriguez@email.com",
            Activo = true,
            FechaCreacion = DateTime.Now
        };
        context.Usuarios.Add(cliente1);

        // Cliente 2
        var cliente2 = new Usuario
        {
            NombreUsuario = "laura_cliente",
            Contrasena = passwordHash,
            Rol = SD.RolCliente,
            NombreCompleto = "Laura Sánchez",
            Telefono = "50588887890",
            Email = "laura.sanchez@email.com",
            Activo = true,
            FechaCreacion = DateTime.Now
        };
        context.Usuarios.Add(cliente2);

        // Cliente 3
        var cliente3 = new Usuario
        {
            NombreUsuario = "pedro_cliente",
            Contrasena = passwordHash,
            Rol = SD.RolCliente,
            NombreCompleto = "Pedro Hernández",
            Telefono = "50588888901",
            Email = "pedro.hernandez@email.com",
            Activo = true,
            FechaCreacion = DateTime.Now
        };
        context.Usuarios.Add(cliente3);

        await context.SaveChangesAsync();

        // ========== CATEGORÍAS ==========
        var categorias = new[]
        {
            new Categoria { Nombre = "Construcción", Icono = "🔨", Orden = 1, Activo = true, FechaCreacion = DateTime.Now },
            new Categoria { Nombre = "Farmacia", Icono = "💊", Orden = 2, Activo = true, FechaCreacion = DateTime.Now },
            new Categoria { Nombre = "Supermercado", Icono = "🛒", Orden = 3, Activo = true, FechaCreacion = DateTime.Now },
            new Categoria { Nombre = "Ropa y Calzado", Icono = "👕", Orden = 4, Activo = true, FechaCreacion = DateTime.Now },
            new Categoria { Nombre = "Herramientas", Icono = "🔧", Orden = 5, Activo = true, FechaCreacion = DateTime.Now },
        };

        foreach (var categoria in categorias)
        {
            if (!await context.Categorias.AnyAsync(c => c.Nombre == categoria.Nombre))
            {
                context.Categorias.Add(categoria);
            }
        }
        await context.SaveChangesAsync();

        // Obtener categorías para productos
        var catConstruccion = await context.Categorias.FirstAsync(c => c.Nombre == "Construcción");
        var catFarmacia = await context.Categorias.FirstAsync(c => c.Nombre == "Farmacia");
        var catSupermercado = await context.Categorias.FirstAsync(c => c.Nombre == "Supermercado");
        var catRopa = await context.Categorias.FirstAsync(c => c.Nombre == "Ropa y Calzado");
        var catHerramientas = await context.Categorias.FirstAsync(c => c.Nombre == "Herramientas");

        // ========== PRODUCTOS ==========

        // Productos para Ferretería El Constructor
        var productos1 = new[]
        {
            new Producto { TiendaId = tienda1.Id, Nombre = "Cemento Portland", Descripcion = "Saco de 50kg", Precio = 250.00m, Moneda = SD.MonedaCordoba, CategoriaId = catConstruccion.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda1.Id, Nombre = "Ladrillos", Descripcion = "Ladrillos rojos, unidad", Precio = 2.50m, Moneda = SD.MonedaCordoba, CategoriaId = catConstruccion.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda1.Id, Nombre = "Pintura Acrílica", Descripcion = "Galón de 4 litros", Precio = 350.00m, Moneda = SD.MonedaCordoba, CategoriaId = catConstruccion.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda1.Id, Nombre = "Martillo", Descripcion = "Martillo de acero", Precio = 180.00m, Moneda = SD.MonedaCordoba, CategoriaId = catHerramientas.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda1.Id, Nombre = "Destornilladores", Descripcion = "Juego de 6 piezas", Precio = 120.00m, Moneda = SD.MonedaCordoba, CategoriaId = catHerramientas.Id, Activo = true, FechaCreacion = DateTime.Now },
        };
        context.Productos.AddRange(productos1);

        // Productos para Farmacia San Carlos
        var productos2 = new[]
        {
            new Producto { TiendaId = tienda2.Id, Nombre = "Paracetamol 500mg", Descripcion = "Caja de 20 tabletas", Precio = 45.00m, Moneda = SD.MonedaCordoba, CategoriaId = catFarmacia.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda2.Id, Nombre = "Ibuprofeno 400mg", Descripcion = "Caja de 20 tabletas", Precio = 50.00m, Moneda = SD.MonedaCordoba, CategoriaId = catFarmacia.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda2.Id, Nombre = "Alcohol 70%", Descripcion = "Botella de 500ml", Precio = 35.00m, Moneda = SD.MonedaCordoba, CategoriaId = catFarmacia.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda2.Id, Nombre = "Jabón Antibacterial", Descripcion = "Barra de 125g", Precio = 25.00m, Moneda = SD.MonedaCordoba, CategoriaId = catFarmacia.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda2.Id, Nombre = "Vitamina C", Descripcion = "Frasco de 100 tabletas", Precio = 120.00m, Moneda = SD.MonedaCordoba, CategoriaId = catFarmacia.Id, Activo = true, FechaCreacion = DateTime.Now },
        };
        context.Productos.AddRange(productos2);

        // Productos para Supermercado El Ahorro
        var productos3 = new[]
        {
            new Producto { TiendaId = tienda3.Id, Nombre = "Arroz", Descripcion = "Saco de 25kg", Precio = 450.00m, Moneda = SD.MonedaCordoba, CategoriaId = catSupermercado.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda3.Id, Nombre = "Frijoles", Descripcion = "Saco de 25kg", Precio = 550.00m, Moneda = SD.MonedaCordoba, CategoriaId = catSupermercado.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda3.Id, Nombre = "Aceite de Cocina", Descripcion = "Botella de 1 litro", Precio = 85.00m, Moneda = SD.MonedaCordoba, CategoriaId = catSupermercado.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda3.Id, Nombre = "Azúcar", Descripcion = "Saco de 25kg", Precio = 320.00m, Moneda = SD.MonedaCordoba, CategoriaId = catSupermercado.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda3.Id, Nombre = "Sal", Descripcion = "Bolsa de 1kg", Precio = 15.00m, Moneda = SD.MonedaCordoba, CategoriaId = catSupermercado.Id, Activo = true, FechaCreacion = DateTime.Now },
        };
        context.Productos.AddRange(productos3);

        // Productos para Ferretería La Esquina
        var productos4 = new[]
        {
            new Producto { TiendaId = tienda4.Id, Nombre = "Cable Eléctrico", Descripcion = "Rollo de 100 metros, calibre 12", Precio = 850.00m, Moneda = SD.MonedaCordoba, CategoriaId = catHerramientas.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda4.Id, Nombre = "Tubería PVC", Descripcion = "Tubo de 1/2 pulgada, 6 metros", Precio = 120.00m, Moneda = SD.MonedaCordoba, CategoriaId = catConstruccion.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda4.Id, Nombre = "Llave Inglesa", Descripcion = "Llave ajustable 8 pulgadas", Precio = 150.00m, Moneda = SD.MonedaCordoba, CategoriaId = catHerramientas.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda4.Id, Nombre = "Bombillos LED", Descripcion = "Pack de 4 unidades, 9W", Precio = 200.00m, Moneda = SD.MonedaCordoba, CategoriaId = catHerramientas.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda4.Id, Nombre = "Cinta Aislante", Descripcion = "Rollo de 20 metros", Precio = 30.00m, Moneda = SD.MonedaCordoba, CategoriaId = catHerramientas.Id, Activo = true, FechaCreacion = DateTime.Now },
        };
        context.Productos.AddRange(productos4);

        // Productos para Moda Joven
        var productos5 = new[]
        {
            new Producto { TiendaId = tienda5.Id, Nombre = "Camiseta", Descripcion = "Camiseta de algodón, varios colores", Precio = 180.00m, Moneda = SD.MonedaCordoba, CategoriaId = catRopa.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda5.Id, Nombre = "Pantalón Jeans", Descripcion = "Jeans clásico, varios tallas", Precio = 450.00m, Moneda = SD.MonedaCordoba, CategoriaId = catRopa.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda5.Id, Nombre = "Zapatos Deportivos", Descripcion = "Tenis para correr, varios modelos", Precio = 850.00m, Moneda = SD.MonedaCordoba, CategoriaId = catRopa.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda5.Id, Nombre = "Vestido", Descripcion = "Vestido casual, varios estilos", Precio = 350.00m, Moneda = SD.MonedaCordoba, CategoriaId = catRopa.Id, Activo = true, FechaCreacion = DateTime.Now },
            new Producto { TiendaId = tienda5.Id, Nombre = "Mochila", Descripcion = "Mochila escolar, varios colores", Precio = 280.00m, Moneda = SD.MonedaCordoba, CategoriaId = catRopa.Id, Activo = true, FechaCreacion = DateTime.Now },
        };
        context.Productos.AddRange(productos5);

        await context.SaveChangesAsync();
    }
}
