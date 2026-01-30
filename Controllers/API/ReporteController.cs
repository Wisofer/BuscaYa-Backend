using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuscaYa.Controllers.API;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class ReporteController : ControllerBase
{
    private readonly IReporteService _reporteService;
    private readonly ILogger<ReporteController> _logger;

    public ReporteController(IReporteService reporteService, ILogger<ReporteController> logger)
    {
        _reporteService = reporteService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CrearReporte([FromBody] CrearReporteRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Datos inválidos", detalles = ModelState });
            }

            // Obtener ID del usuario desde el token
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out int usuarioId))
            {
                return Unauthorized(new { error = "Token inválido" });
            }

            // Validar razones permitidas
            var razonesPermitidas = new[]
            {
                "Contenido inapropiado",
                "Información falsa o engañosa",
                "Producto o tienda duplicada",
                "Spam o publicidad no deseada",
                "Otro"
            };

            if (!razonesPermitidas.Contains(request.Razon))
            {
                return BadRequest(new { error = "Razón no válida" });
            }

            // Validar que el recurso existe
            var recursoExiste = await _reporteService.ValidarRecursoExisteAsync(request.Tipo, request.RecursoId);
            if (!recursoExiste)
            {
                return NotFound(new { error = $"El {request.Tipo} con ID {request.RecursoId} no existe" });
            }

            // Crear el reporte
            var reporte = await _reporteService.CrearReporteAsync(usuarioId, request);

            return StatusCode(201, new
            {
                id = reporte.Id,
                mensaje = "Reporte recibido. Gracias por tu ayuda."
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error al crear reporte: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear reporte");
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }
}
