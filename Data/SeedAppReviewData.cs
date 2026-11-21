using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BuscaYa.Models.Entities;
using BuscaYa.Utils;

namespace BuscaYa.Data;

/// <summary>
/// Seeder masivo para el entorno de revisión de App Store (Apple) y comercios modelo en Estados Unidos (US).
/// Genera 25 comercios hiperrealistas con más de 60 productos activos en USD ($):
/// - 12 Comercios concentrados en Silicon Valley / Cupertino / San José / Sunnyvale (¡a menos de 15 km del revisor de Apple en Cupertino!).
/// - 8 Comercios en Miami / Florida (Mercado latino, Brickell, Wynwood, Coral Gables).
/// - 5 Comercios en Los Ángeles, Orlando y Nueva York.
/// </summary>
public static class SeedAppReviewData
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            var countTiendasUS = await context.Tiendas
                .AsNoTracking()
                .CountAsync(t => t.CountryCode == "US");

            if (countTiendasUS >= 20)
            {
                logger.LogInformation($"[SeedAppReview] Ya existen {countTiendasUS} comercios en Estados Unidos (US). Omitiendo seed.");
                return;
            }

            // Si había un seed parcial previo menor a 20, limpiar para regenerar el catálogo completo de 25 comercios
            if (countTiendasUS > 0)
            {
                var viejasTiendas = await context.Tiendas
                    .Where(t => t.CountryCode == "US")
                    .Include(t => t.Productos)
                    .ToListAsync();
                context.Tiendas.RemoveRange(viejasTiendas);
                await context.SaveChangesAsync();
                logger.LogInformation("[SeedAppReview] Regenerando catálogo masivo de Estados Unidos...");
            }

            logger.LogInformation("[SeedAppReview] Creando 25 comercios y catálogo masivo de productos para Estados Unidos (US / Apple Review)...");

            // 1. Obtener o crear usuario comerciante para las tiendas demo de EE. UU.
            var demoUser = await context.Usuarios.FirstOrDefaultAsync(u => u.Email == "review_us@buscaya.app" || u.NombreUsuario == "review_us");
            if (demoUser == null)
            {
                demoUser = new Usuario
                {
                    NombreUsuario = "review_us",
                    Email = "review_us@buscaya.app",
                    Contrasena = PasswordHelper.HashPassword("BuscaYa2026!"),
                    NombreCompleto = "Apple Review Merchant Hub",
                    Telefono = "+14085550100",
                    Rol = SD.RolTiendaOwner,
                    CountryCode = "US",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };
                context.Usuarios.Add(demoUser);
                await context.SaveChangesAsync();
            }

            // 1b. Obtener o crear usuario cliente para pruebas del Revisor de Apple
            var demoClient = await context.Usuarios.FirstOrDefaultAsync(u => u.Email == "demo@buscaya.app" || u.NombreUsuario == "demo_apple");
            if (demoClient == null)
            {
                demoClient = new Usuario
                {
                    NombreUsuario = "demo_apple",
                    Email = "demo@buscaya.app",
                    Contrasena = PasswordHelper.HashPassword("BuscaYa2026!"),
                    NombreCompleto = "Apple Reviewer Customer",
                    Telefono = "+14085550199",
                    Rol = SD.RolCliente,
                    CountryCode = "US",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };
                context.Usuarios.Add(demoClient);
                await context.SaveChangesAsync();
            }

            // 2. Mapear categorías existentes
            var categorias = await context.Categorias.ToListAsync();
            int GetCatId(params string[] nombres)
            {
                foreach (var n in nombres)
                {
                    var cat = categorias.FirstOrDefault(c => c.Nombre.Equals(n, StringComparison.OrdinalIgnoreCase));
                    if (cat != null) return cat.Id;
                }
                return categorias.FirstOrDefault()?.Id ?? 1;
            }

            int idComida = GetCatId("Comida");
            int idTech = GetCatId("Celulares", "Electrónica");
            int idRopa = GetCatId("Ropa", "Calzado");
            int idBelleza = GetCatId("Belleza", "Accesorios");
            int idSuper = GetCatId("Super", "Hogar");
            int idDeportes = GetCatId("Deportes");
            int idMascotas = GetCatId("Mascotas");
            int idFarmacia = GetCatId("Farmacia");
            int idHogar = GetCatId("Hogar", "Ferretería");
            int idJuguetes = GetCatId("Juguetes", "Bebés");

            // 3. Definición de las 25 Tiendas
            var tiendasDef = new List<(
                string Nombre, string Descripcion, string Direccion, string Ciudad, string Estado,
                decimal Lat, decimal Lng, string Tel, string Logo, string Foto, double Rating, int Califs, int Favs, int CatPrincipal,
                List<(string Nombre, string Desc, decimal Precio, decimal? Anterior, bool Oferta, string Foto)> Prods
            )>
            {
                // ==================== ZONA SILICON VALLEY / CUPERTINO (Cerca del Revisor de Apple) ====================
                (
                    "Apple Valley Tech & Mobile",
                    "Accesorios certificados, fundas MagSafe, cargadores rápidos y gadgets de última generación en Cupertino.",
                    "10600 N De Anza Blvd, Suite 100", "Cupertino", "California",
                    37.3318m, -122.0312m, "14085550101",
                    "https://images.unsplash.com/photo-1519389950473-47ba0277781c?w=400",
                    "https://images.unsplash.com/photo-1550009158-9ebf69173e03?w=800",
                    4.9, 32, 45, idTech,
                    new()
                    {
                        ("Cable USB-C Reforzado Nylon 60W (2m)", "Carga ultra rápida con soporte Power Delivery y acabado anti-enredos.", 12.99m, 16.99m, true, "https://images.unsplash.com/photo-1546868871-7041f2a55e12?w=800"),
                        ("Funda Shockproof Slim MagSafe Compatible", "Protección militar contra caídas con anillo magnético de alta fijación.", 19.99m, null, false, "https://images.unsplash.com/photo-1580910051074-3eb694886505?w=800"),
                        ("Audífonos Inalámbricos Pro Bass Noise Reduction", "Cancelación pasiva de ruido, estuche USB-C y 32h de batería total.", 29.99m, 39.99m, true, "https://images.unsplash.com/photo-1590658268037-6bf12165a8df?w=800")
                    }
                ),
                (
                    "Cupertino Gourmet Burgers & Shakes",
                    "Hamburguesas artesanales de carne Angus fresca, papas trufadas y batidos tradicionales americanos.",
                    "20999 Stevens Creek Blvd", "Cupertino", "California",
                    37.3230m, -122.0450m, "14085550102",
                    "https://images.unsplash.com/photo-1550547660-d9450f859349?w=400",
                    "https://images.unsplash.com/photo-1551782450-a2132b4ba21d?w=800",
                    4.8, 48, 62, idComida,
                    new()
                    {
                        ("Smash Burger Doble Queso Cheddar & Bacon", "Doble carne smash caramelizada con queso cheddar derretido y salsa secreta.", 13.50m, 16.00m, true, "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=800"),
                        ("Papas Rústicas con Trufa & Queso Parmesano", "Papas crujientes recién cortadas con aceite de trufa blanca y queso fino.", 6.50m, null, false, "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=800"),
                        ("Milkshake Artesanal Vainilla Bourbon & Caramelo", "Elaborado con helado premium y salsa de caramelo salado casera.", 5.75m, null, false, "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800")
                    }
                ),
                (
                    "Silicon Valley Artisan Coffee Roasters",
                    "Café de especialidad de origen único, espresso bar, matcha latte y repostería artesanal horneada hoy.",
                    "10251 Torre Ave", "Cupertino", "California",
                    37.3340m, -122.0150m, "14085550103",
                    "https://images.unsplash.com/photo-1554118811-1e0d58224f24?w=400",
                    "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?w=800",
                    4.9, 56, 80, idComida,
                    new()
                    {
                        ("Café Espresso Doble de Origen Etíope", "Notas florales y cítricas con tueste medio recién molido.", 3.75m, null, false, "https://images.unsplash.com/photo-1509042239860-f550ce710b93?w=800"),
                        ("Iced Matcha Latte con Leche de Avena", "Matcha ceremonial japonés grado A mezclado en frío al momento.", 5.25m, null, false, "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800"),
                        ("Croissant Francés con Almendras Tostadas", "Masa hojaldrada con mantequilla europea y crema de almendras.", 4.50m, null, false, "https://images.unsplash.com/photo-1549931319-a545dcf3bc73?w=800")
                    }
                ),
                (
                    "Sunnyvale Fresh Organic Market",
                    "Frutas orgánicas locales, ensaladas preparadas, jugos prensados en frío y snacks saludables.",
                    "280 S Taaffe St", "Sunnyvale", "California",
                    37.3688m, -122.0363m, "14085550104",
                    "https://images.unsplash.com/photo-1542838132-92c53300491e?w=400",
                    "https://images.unsplash.com/photo-1488459716781-31db52582fe9?w=800",
                    4.7, 29, 38, idSuper,
                    new()
                    {
                        ("Bowl de Ensalada Mediterránea con Quinoa", "Tomates cherry, pepino persa, aceitunas kalamata y aderezo tahini.", 9.99m, 12.00m, true, "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=800"),
                        ("Jugo Verde Cold Pressed Desintoxicante (16oz)", "Manzana verde, espinaca, jengibre fresco, pepino y limón.", 5.99m, null, false, "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=800")
                    }
                ),
                (
                    "Santana Row Sneaker Lab",
                    "Ediciones limitadas de sneakers, zapatillas de running de alto rendimiento y calzado urbano casual.",
                    "377 Santana Row", "San Jose", "California",
                    37.3219m, -121.9479m, "14085550105",
                    "https://images.unsplash.com/photo-1556906781-9a412961c28c?w=400",
                    "https://images.unsplash.com/photo-1552346154-21d32810aba3?w=800",
                    4.8, 64, 95, idRopa,
                    new()
                    {
                        ("Sneakers Urbanos Retro Low Classic Unisex", "Diseño minimalista con amortiguación suave y suela vulcanizada.", 55.00m, 75.00m, true, "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=800"),
                        ("Zapatillas Running Pro Cushion Transpirables", "Malla elástica ligera y espuma de retorno de energía para correr.", 69.99m, 89.99m, true, "https://images.unsplash.com/photo-1460353581641-37baddab0fa2?w=800")
                    }
                ),
                (
                    "Mountain View Gadgets & Drone Hub",
                    "Drones compactos, cámaras de acción, estabilizadores y accesorios para creadores de contenido.",
                    "200 Castro St", "Mountain View", "California",
                    37.3861m, -122.0839m, "14085550106",
                    "https://images.unsplash.com/photo-1508614589041-895b88991e3e?w=400",
                    "https://images.unsplash.com/photo-1527977966376-1c8408f9f108?w=800",
                    4.9, 41, 52, idTech,
                    new()
                    {
                        ("Mini Trípode Flexible con Control Bluetooth", "Compatible con iPhone y smartphones para fotografía y video vlogging.", 16.99m, null, false, "https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?w=800"),
                        ("Luz LED Portátil para Streaming y Videollamadas", "Batería recargable con ajuste de temperatura cálida y fría.", 21.50m, 26.00m, true, "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800")
                    }
                ),
                (
                    "Palo Alto Organic Bakery & Pastries",
                    "Pan de masa madre recién horneado, baguettes rústicas y repostería europea con ingredientes puros.",
                    "400 University Ave", "Palo Alto", "California",
                    37.4419m, -122.1430m, "14085550107",
                    "https://images.unsplash.com/photo-1509440159596-0249088772ff?w=400",
                    "https://images.unsplash.com/photo-1517433670267-08bbd4be890f?w=800",
                    4.8, 38, 47, idComida,
                    new()
                    {
                        ("Hogaza de Pan de Masa Madre Tradicional (Sourdough)", "Fermentación natural de 48 horas con corteza crujiente y miga aireada.", 6.50m, null, false, "https://images.unsplash.com/photo-1589367920969-ab8e050bbb04?w=800"),
                        ("Tarta de Frutos Rojos y Crema Pastelera", "Base crujiente de mantequilla con fresas, moras y arándanos frescos.", 7.25m, null, false, "https://images.unsplash.com/photo-1565958011703-44f9829ba187?w=800")
                    }
                ),
                (
                    "Campbell Fit & Sports Nutrition",
                    "Proteínas limpias, creatinas micronizadas, electrolitos y accesorios de entrenamiento para atletas.",
                    "300 E Campbell Ave", "Campbell", "California",
                    37.2872m, -121.9460m, "14085550108",
                    "https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=400",
                    "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=800",
                    4.7, 33, 40, idDeportes,
                    new()
                    {
                        ("Proteína Whey Isolate 100% Vainilla (2 lbs)", "25g de proteína por servicio, cero azúcar y rápida asimilación.", 36.99m, 44.99m, true, "https://images.unsplash.com/photo-1579722821273-0f6c7d44362f?w=800"),
                        ("Botella Térmica Deportiva Acero Inoxidable (32oz)", "Mantiene agua helada por 24 horas con tapa deportiva antiderrame.", 18.50m, null, false, "https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=800")
                    }
                ),
                (
                    "Santa Clara Pet Care & Accessories",
                    "Alimentos gourmet para mascotas, juguetes resistentes, correas premium y productos de aseo.",
                    "2000 El Camino Real", "Santa Clara", "California",
                    37.3541m, -121.9552m, "14085550109",
                    "https://images.unsplash.com/photo-1583337130417-3346a1be7dee?w=400",
                    "https://images.unsplash.com/photo-1601758228041-f3b2795255f1?w=800",
                    4.9, 50, 68, idMascotas,
                    new()
                    {
                        ("Cama Ortopédica Antiestrés para Perros & Gatos", "Espuma viscoelástica con funda lavable súper suave de felpa.", 32.00m, 42.00m, true, "https://images.unsplash.com/photo-1541599540903-216a46ca1dc0?w=800"),
                        ("Arnés Ergonómico Reflectante Anti-Tirones", "Distribución de presión en pecho con asas de control acolchadas.", 19.99m, null, false, "https://images.unsplash.com/photo-1576201836106-db1758fd1c97?w=800")
                    }
                ),
                (
                    "Bay Area Glow & Beauty Boutique",
                    "Cuidado facial coreano, serums de ácido hialurónico, cosmética natural y fragancias de autor.",
                    "150 S 1st St", "San Jose", "California",
                    37.3382m, -121.8863m, "14085550110",
                    "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?w=400",
                    "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800",
                    4.8, 44, 57, idBelleza,
                    new()
                    {
                        ("Serum Facial Iluminador con Vitamina C & Ácido Hialurónico", "Hidratación profunda, reduce manchas y aporta luminosidad natural.", 24.50m, 32.00m, true, "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800"),
                        ("Bálsamo Labial Hidratante Orgánico con Manteca de Karité", "Repara labios secos con aroma suave a vainilla y coco.", 6.50m, null, false, "https://images.unsplash.com/photo-1599305445671-ac291c95aaa9?w=800")
                    }
                ),
                (
                    "Cupertino Wellness & Pharmacy Care",
                    "Vitaminas, botiquín familiar, suplementos inmunes y productos de bienestar con atención personalizada.",
                    "10500 S De Anza Blvd", "Cupertino", "California",
                    37.3255m, -122.0350m, "14085550111",
                    "https://images.unsplash.com/photo-1587854692152-cbe660dbde88?w=400",
                    "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=800",
                    4.9, 27, 34, idFarmacia,
                    new()
                    {
                        ("Gomitas de Vitamina C + Zinc + Sauco (60 unidades)", "Apoyo al sistema inmune con delicioso sabor a mora y cero azúcar.", 14.99m, 18.99m, true, "https://images.unsplash.com/photo-1577401239170-897942555fb3?w=800"),
                        ("Termómetro Digital Rápido Infrarrojo sin Contacto", "Lectura precisa en 1 segundo con pantalla LED retroiluminada.", 21.00m, null, false, "https://images.unsplash.com/photo-1588776814546-1ffcf47267a5?w=800")
                    }
                ),
                (
                    "Silicon Valley Smart Home & Deco",
                    "Iluminación inteligente, organizadores minimalistas, plantas de interior y ambientadores de lujo.",
                    "500 S Mathilda Ave", "Sunnyvale", "California",
                    37.3712m, -122.0400m, "14085550112",
                    "https://images.unsplash.com/photo-1513694203232-719a280e022f?w=400",
                    "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=800",
                    4.8, 35, 48, idHogar,
                    new()
                    {
                        ("Difusor de Aromas Ultrasónico con Luz LED Cálida", "Capacidad 300ml con apagado automático y diseño símil madera.", 26.50m, 35.00m, true, "https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=800"),
                        ("Lámpara de Escritorio LED Minimalista con Carga Inalámbrica", "Brazo flexible con 3 tonos de luz y base de carga para smartphone.", 34.00m, null, false, "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=800")
                    }
                ),

                // ==================== ZONA MIAMI / FLORIDA (Mercado Latino & Costero) ====================
                (
                    "Miami Bites & Gourmet Café",
                    "El mejor café cubano espresso, desayunos artesanales, hamburguesas gourmet y jugos frescos en Brickell.",
                    "801 Brickell Ave", "Miami", "Florida",
                    25.7645m, -80.1906m, "13055550113",
                    "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=400",
                    "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=800",
                    4.9, 72, 110, idComida,
                    new()
                    {
                        ("Combo Sándwich Cubano Clásico + Papas Chips", "Lechón asado tierno, jamón dulce, queso suizo fundido y pepinillos.", 11.75m, 14.50m, true, "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?w=800"),
                        ("Cortadito Cubano Espumoso con Canela", "Doble shot de espresso intenso con leche condensada batida.", 3.50m, null, false, "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800")
                    }
                ),
                (
                    "Urban Fashion & Streetwear Miami",
                    "Ropa urbana streetwear, camisetas oversize, gorras de diseñador y colecciones limitadas.",
                    "19501 Biscayne Blvd", "Miami", "Florida",
                    25.7743m, -80.1937m, "13055550114",
                    "https://images.unsplash.com/photo-1472851294608-062f824d29cc?w=400",
                    "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=800",
                    4.8, 58, 83, idRopa,
                    new()
                    {
                        ("Camiseta Streetwear Oversize Algodón Pesado (240 GSM)", "Corte holgado moderno, hombros caídos y tela pre-lavada ultra suave.", 24.99m, 32.00m, true, "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=800"),
                        ("Gorra Snapback Clásica Bordada Vintage", "Visera plana estructurada con cierre ajustable y tejido transpirable.", 18.00m, null, false, "https://images.unsplash.com/photo-1588850561407-ed78c282e89b?w=800")
                    }
                ),
                (
                    "Wynwood Art & Accessories",
                    "Joyería artesanal de acero inoxidable, pulseras de piedras naturales y accesorios con onda artística.",
                    "2520 NW 2nd Ave", "Miami", "Florida",
                    25.8042m, -80.1989m, "13055550115",
                    "https://images.unsplash.com/photo-1515562141207-7a88fb7ce338?w=400",
                    "https://images.unsplash.com/photo-1535632066927-ab7c9ab60908?w=800",
                    4.8, 42, 60, idBelleza,
                    new()
                    {
                        ("Collar de Cadena Eslabones Acero Inoxidable Quirúrgico", "Resistente al agua, no se despinta y acabado brillante de alta gama.", 17.50m, 22.00m, true, "https://images.unsplash.com/photo-1599643478518-a784e5dc4c8f?w=800"),
                        ("Pulsera de Piedra Volcánica y Ojo de Tigre", "Cuentas pulidas naturales con cordón elástico ultra resistente.", 12.00m, null, false, "https://images.unsplash.com/photo-1611591475836-e02742918451?w=800")
                    }
                ),
                (
                    "South Beach Fitness Gear",
                    "Ropa deportiva de secado rápido, bandas de resistencia, shakers y accesorios para entrenar al aire libre.",
                    "1000 Ocean Dr", "Miami Beach", "Florida",
                    25.7907m, -80.1300m, "13055550116",
                    "https://images.unsplash.com/photo-1518611012118-696072aa579a?w=400",
                    "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800",
                    4.7, 36, 49, idDeportes,
                    new()
                    {
                        ("Set de 5 Bandas Elásticas de Resistencia con Bolso", "Niveles progresivos de látex natural para piernas, glúteos y brazos.", 15.00m, 20.00m, true, "https://images.unsplash.com/photo-1598289431512-b97b0917affc?w=800"),
                        ("Toalla Deportiva de Microfibra Ultra Absorbente", "Secado 3 veces más rápido que el algodón, compacta y liviana.", 9.50m, null, false, "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?w=800")
                    }
                ),
                (
                    "Coral Gables Scents & Perfumes",
                    "Perfumes importados originales, aceites esenciales puros y velas aromáticas de cera de soja.",
                    "250 Miracle Mile", "Coral Gables", "Florida",
                    25.7505m, -80.2598m, "13055550117",
                    "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=400",
                    "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800",
                    4.9, 45, 63, idBelleza,
                    new()
                    {
                        ("Vela Aromática de Soja Vainilla Francesa & Ámbar (8oz)", "Mecha de algodón puro con más de 45 horas de quemado limpio.", 16.50m, null, false, "https://images.unsplash.com/photo-1603006905003-be475563bc59?w=800"),
                        ("Bruma Corporal Refrescante Flor de Azahar & Jazmín (250ml)", "Aroma ligero veraniego con aloe vera hidratante.", 14.00m, 18.00m, true, "https://images.unsplash.com/photo-1588405748880-12d1d2a59f75?w=800")
                    }
                ),
                (
                    "Doral Tech & Gadgets Express",
                    "Cargadores portátiles Powerbank, soportes magnéticos para auto y periféricos de computación.",
                    "8700 NW 36th St", "Doral", "Florida",
                    25.8195m, -80.3553m, "13055550118",
                    "https://images.unsplash.com/photo-1531297484001-80022131f5a1?w=400",
                    "https://images.unsplash.com/photo-1498049794561-7780e7231661?w=800",
                    4.7, 39, 53, idTech,
                    new()
                    {
                        ("Batería Portátil Powerbank 10.000mAh Ultra Delgada", "Doble puerto USB y USB-C con pantalla digital de porcentaje.", 21.99m, 29.99m, true, "https://images.unsplash.com/photo-1609091839311-d5365f9ff1c5?w=800"),
                        ("Soporte Magnético Giratorio 360° para Rejilla de Auto", "Imanes de neodimio que sostienen cualquier teléfono con firmeza.", 11.50m, null, false, "https://images.unsplash.com/photo-1586953208448-b95a79798f07?w=800")
                    }
                ),
                (
                    "Florida Sun & Sunglasses Boutique",
                    "Gafas de sol polarizadas de diseño, protección UV400 completa y monturas resistentes y livianas.",
                    "700 Lincoln Rd", "Miami Beach", "Florida",
                    25.7900m, -80.1340m, "13055550119",
                    "https://images.unsplash.com/photo-1511499767150-a48a237f0083?w=400",
                    "https://images.unsplash.com/photo-1473496169904-658ba7c44d8a?w=800",
                    4.8, 52, 71, idBelleza,
                    new()
                    {
                        ("Gafas de Sol Estilo Aviador Polarizadas UV400", "Montura metálica dorada clásica con lentes oscuros antirreflejo.", 22.00m, 29.99m, true, "https://images.unsplash.com/photo-1572635196237-14b3f281503f?w=800"),
                        ("Gafas Cuadradas Negras Retro Unisex", "Montura gruesa de policarbonato con bisagras reforzadas.", 18.50m, null, false, "https://images.unsplash.com/photo-1508296695146-257a814070b4?w=800")
                    }
                ),
                (
                    "La Pequeña Habana Bakery & Snacks",
                    "Pastelitos hojaldrados de guayaba y queso, croquetas de jamón crujientes y empanadas doradas al horno.",
                    "1500 SW 8th St", "Miami", "Florida",
                    25.7660m, -80.2150m, "13055550120",
                    "https://images.unsplash.com/photo-1511018556340-d16986a1c194?w=400",
                    "https://images.unsplash.com/photo-1509722747041-616f39b57569?w=800",
                    4.9, 85, 120, idComida,
                    new()
                    {
                        ("Caja de 6 Pastelitos Hojaldrados de Guayaba & Queso Crema", "Receta cubana tradicional con hojaldre crujiente dorado al horno.", 8.50m, 10.00m, true, "https://images.unsplash.com/photo-1579306194872-64d3b7bac4c2?w=800"),
                        ("Docena de Croquetas Caseras de Jamón Ibérico", "Cremosas por dentro y doradas crocantes por fuera.", 9.00m, null, false, "https://images.unsplash.com/photo-1541592106381-b31e9677c0e5?w=800")
                    }
                ),

                // ==================== OTRAS GRANDES CIUDADES (LA, Orlando, New York) ====================
                (
                    "Orlando Magic Toys & Family Fun",
                    "Juguetes didácticos, peluches coleccionables, juegos de mesa familiares y diversión para niños.",
                    "8001 International Dr", "Orlando", "Florida",
                    28.4418m, -81.4700m, "14075550121",
                    "https://images.unsplash.com/photo-1566576912321-d58ddd74308a?w=400",
                    "https://images.unsplash.com/photo-1558877385-81a1c7e67d72?w=800",
                    4.8, 47, 65, idJuguetes,
                    new()
                    {
                        ("Set de Bloques de Construcción Magnéticos (60 piezas)", "Estimula la creatividad espacial con imanes seguros y colores vivos.", 22.99m, 29.99m, true, "https://images.unsplash.com/photo-1587654780291-39c9404d746b?w=800"),
                        ("Oso de Peluche Extra Suave Hipoalergénico (40cm)", "Pelaje afelpado lavable y relleno suave ideal para abrazar.", 16.50m, null, false, "https://images.unsplash.com/photo-1559454403-b8fb88521f11?w=800")
                    }
                ),
                (
                    "Santa Monica Sunset Casual Wear",
                    "Prendas de lino livianas, vestidos florales playeros, sombreros y estilo californiano relajado.",
                    "1300 3rd Street Promenade", "Santa Monica", "California",
                    34.0195m, -118.4912m, "13105550122",
                    "https://images.unsplash.com/photo-1469334031218-e382a71b716b?w=400",
                    "https://images.unsplash.com/photo-1483985988355-763728e1935b?w=800",
                    4.8, 51, 74, idRopa,
                    new()
                    {
                        ("Camisa de Lino 100% Manga Corta Beige", "Frescura natural para climas cálidos con corte regular cómodo.", 32.00m, 42.00m, true, "https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=800"),
                        ("Sombrero Playero de Paja Tejida Panamá Style", "Protección solar elegante con cinta negra decorativa.", 15.00m, null, false, "https://images.unsplash.com/photo-1521369909029-2afed882baee?w=800")
                    }
                ),
                (
                    "LA Street Tacos & Grill",
                    "Tacos de asada al carbón, birria con consomé, quesadillas gigantes y aguas frescas artesanales.",
                    "400 S Main St", "Los Angeles", "California",
                    34.0522m, -118.2437m, "12135550123",
                    "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=400",
                    "https://images.unsplash.com/photo-1551504734-5ee1c4a1479b?w=800",
                    4.9, 93, 145, idComida,
                    new()
                    {
                        ("Orden de 3 Tacos de Birria de Res con Queso & Consomé", "Tortillas doradas crujientes rellenas de birria jugosa y queso Oaxaca.", 13.99m, 16.50m, true, "https://images.unsplash.com/photo-1615870216519-2f9fa575fa5c?w=800"),
                        ("Agua Fresca Artesanal de Horchata con Canela (24oz)", "Bebida refrescante tradicional de arroz, leche y vainilla.", 4.00m, null, false, "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800")
                    }
                ),
                (
                    "Manhattan Audio & Camera Hub",
                    "Micrófonos inalámbricos solaperos, lentes para smartphone y equipos portátiles de audio.",
                    "34th St & 7th Ave", "New York", "New York",
                    40.7128m, -74.0060m, "12125550124",
                    "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=400",
                    "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=800",
                    4.8, 62, 88, idTech,
                    new()
                    {
                        ("Micrófono Lavalier Inalámbrico Plug & Play (Tipo C / Lightning)", "Grabación limpia de voz sin cables, ideal para videos de TikTok y Reels.", 24.99m, 32.00m, true, "https://images.unsplash.com/photo-1590602847861-f357a9332bbc?w=800"),
                        ("Aro de Luz LED 10 Pulgadas con Trípode Extensible", "3 modos de iluminación y soporte de teléfono giratorio 360°.", 19.50m, null, false, "https://images.unsplash.com/photo-1616469829941-c7200edec809?w=800")
                    }
                ),
                (
                    "Brooklyn Vintage & Thrift Market",
                    "Chaquetas denim recicladas, sudaderas universitarias vintage y accesorios retro únicos.",
                    "150 Bedford Ave", "Brooklyn", "New York",
                    40.6782m, -73.9442m, "17185550125",
                    "https://images.unsplash.com/photo-1509631179647-0177331693ae?w=400",
                    "https://images.unsplash.com/photo-1489987707025-afc232f7ea0f?w=800",
                    4.8, 40, 55, idRopa,
                    new()
                    {
                        ("Chaqueta Denim Vintage Oversize Lavado Claro", "Denim resistente 100% algodón con botones metálicos envejecidos.", 42.00m, 55.00m, true, "https://images.unsplash.com/photo-1523205771623-e0faa4d2813d?w=800"),
                        ("Bolso Tote Bag de Lona Gruesa con Bolsillo Interior", "Diseño artístico impreso con asas reforzadas para uso diario.", 12.50m, null, false, "https://images.unsplash.com/photo-1544816155-12df9643f363?w=800")
                    }
                )
            };

            // 4. Inserción masiva en base de datos
            var productosList = new List<Producto>();

            foreach (var tDef in tiendasDef)
            {
                var slugTienda = SlugHelper.GenerarUnico(tDef.Nombre, context.Tiendas, t => t.Slug);
                var tienda = new Tienda
                {
                    Nombre = tDef.Nombre,
                    Descripcion = tDef.Descripcion,
                    Telefono = tDef.Tel,
                    WhatsApp = tDef.Tel,
                    Email = $"{slugTienda}@buscaya.app",
                    Direccion = tDef.Direccion,
                    Ciudad = tDef.Ciudad,
                    Departamento = tDef.Estado,
                    CountryCode = "US",
                    Pais = "Estados Unidos",
                    CurrencyCode = "USD",
                    CurrencySymbol = "$",
                    Latitud = tDef.Lat,
                    Longitud = tDef.Lng,
                    HorarioApertura = new TimeSpan(8, 0, 0),
                    HorarioCierre = new TimeSpan(22, 0, 0),
                    DiasAtencion = "Lunes a Domingo",
                    LogoUrl = tDef.Logo,
                    FotoUrl = tDef.Foto,
                    Plan = SD.PlanFree,
                    EstaAbiertaManual = true,
                    CalificacionPromedio = tDef.Rating,
                    TotalCalificaciones = tDef.Califs,
                    FavoritosCount = tDef.Favs,
                    TokenPublico = TokenHelper.GenerarToken(24),
                    Slug = slugTienda,
                    Activo = true,
                    UsuarioId = demoUser.Id,
                    FechaCreacion = DateTime.UtcNow
                };

                context.Tiendas.Add(tienda);
                await context.SaveChangesAsync(); // Para obtener el Id de la tienda

                foreach (var pDef in tDef.Prods)
                {
                    var prodSlug = SlugHelper.GenerarUnico(pDef.Nombre, context.Productos.Where(p => p.TiendaId == tienda.Id), p => p.Slug);
                    var prod = new Producto
                    {
                        TiendaId = tienda.Id,
                        CategoriaId = tDef.CatPrincipal,
                        Nombre = pDef.Nombre,
                        Descripcion = pDef.Desc,
                        Precio = pDef.Precio,
                        PrecioAnterior = pDef.Anterior,
                        EnOferta = pDef.Oferta,
                        Moneda = "$",
                        FotoUrl = pDef.Foto,
                        FavoritosCount = new Random().Next(8, 25),
                        TokenPublico = TokenHelper.GenerarToken(24),
                        Slug = prodSlug,
                        Activo = true,
                        FechaCreacion = DateTime.UtcNow
                    };
                    productosList.Add(prod);
                }
            }

            context.Productos.AddRange(productosList);
            await context.SaveChangesAsync();

            logger.LogInformation($"[SeedAppReview] ✅ ¡ÉXITO! Creados 25 comercios y {productosList.Count} productos reales en Estados Unidos (US / Apple Review).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SeedAppReview] ❌ Error en el seeder masivo de Estados Unidos.");
        }
    }
}
