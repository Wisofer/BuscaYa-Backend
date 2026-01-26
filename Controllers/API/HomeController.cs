using Microsoft.AspNetCore.Mvc;

namespace BuscaYa.Controllers.API;

[ApiController]
[Route("api")]
public class HomeController : ControllerBase
{
    [HttpGet]
    [Route("")]
    public IActionResult Index()
    {
        return Ok(new
        {
            mensaje = "BuscaYa API - Backend funcionando correctamente",
            version = "1.0.0",
            estado = "activo",
            endpoints = new
            {
                publicos = new[]
                {
                    "GET /api/public/buscar",
                    "GET /api/public/tienda/{id}",
                    "GET /api/public/categorias",
                    "GET /api/public/sugerencias"
                },
                autenticacion = new[]
                {
                    "POST /api/auth/login",
                    "POST /api/auth/register"
                },
                cliente = new[]
                {
                    "GET /api/cliente/favoritos",
                    "POST /api/cliente/favoritos/tienda/{id}",
                    "POST /api/cliente/favoritos/producto/{id}",
                    "GET /api/cliente/historial",
                    "GET /api/cliente/direcciones",
                    "POST /api/cliente/direcciones",
                    "POST /api/cliente/crear-tienda"
                },
                tienda = new[]
                {
                    "GET /api/tienda/perfil",
                    "PUT /api/tienda/perfil",
                    "GET /api/tienda/productos",
                    "POST /api/tienda/productos",
                    "GET /api/tienda/estadisticas"
                }
            },
            documentacion = "Ver FLUJOS_COMPLETOS.md para documentación completa",
            prueba = "GET /api/public/categorias para verificar que la API responde"
        });
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            estado = "healthy",
            timestamp = DateTime.UtcNow,
            mensaje = "API funcionando correctamente"
        });
    }
}
