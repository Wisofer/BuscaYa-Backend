using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuscaYa.Data;
using BuscaYa.Models.DTOs.Requests;
using BuscaYa.Models.DTOs.Responses;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace BuscaYa.Controllers.API;

[ApiController]
[Route("api/notifications")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PushNotificationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPushNotificationService _pushService;

    public PushNotificationController(ApplicationDbContext context, IPushNotificationService pushService)
    {
        _context = context;
        _pushService = pushService;
    }

    private int? GetUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

    // --- Templates (Admin) ---
    [HttpGet("templates")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> GetTemplates()
    {
        var list = await _context.NotificationTemplates
            .OrderByDescending(t => t.Id)
            .Select(t => new TemplateDto
            {
                Id = t.Id,
                Title = t.Title,
                Body = t.Body,
                ImageUrl = t.ImageUrl,
                Name = t.Name,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("templates/{id:int}")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> GetTemplate(int id)
    {
        var t = await _context.NotificationTemplates.FindAsync(id);
        if (t == null) return NotFound();
        return Ok(new TemplateDto
        {
            Id = t.Id,
            Title = t.Title,
            Body = t.Body,
            ImageUrl = t.ImageUrl,
            Name = t.Name,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        });
    }

    [HttpPost("templates")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { error = "Title y Body son requeridos" });
        var t = new NotificationTemplate
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            ImageUrl = !string.IsNullOrWhiteSpace(request.ImageUrl) ? request.ImageUrl.Trim() : null,
            Name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name.Trim() : null
        };
        _context.NotificationTemplates.Add(t);
        await _context.SaveChangesAsync();
        return Ok(new TemplateDto
        {
            Id = t.Id,
            Title = t.Title,
            Body = t.Body,
            ImageUrl = t.ImageUrl,
            Name = t.Name,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        });
    }

    [HttpPut("templates/{id:int}")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] CreateTemplateRequest request)
    {
        var t = await _context.NotificationTemplates.FindAsync(id);
        if (t == null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { error = "Title y Body son requeridos" });
        t.Title = request.Title.Trim();
        t.Body = request.Body.Trim();
        t.ImageUrl = !string.IsNullOrWhiteSpace(request.ImageUrl) ? request.ImageUrl.Trim() : null;
        t.Name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name.Trim() : null;
        t.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new TemplateDto
        {
            Id = t.Id,
            Title = t.Title,
            Body = t.Body,
            ImageUrl = t.ImageUrl,
            Name = t.Name,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        });
    }

    [HttpDelete("templates/{id:int}")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var t = await _context.NotificationTemplates.FindAsync(id);
        if (t == null) return NotFound();
        _context.NotificationTemplates.Remove(t);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // --- Devices (usuario autenticado) ---
    [HttpPost("devices")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.FcmToken))
            return BadRequest(new { error = "FcmToken es requerido" });
        var platform = (request.Platform ?? "unknown").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(platform)) platform = "unknown";

        var existing = await _context.Devices.FirstOrDefaultAsync(d => d.FcmToken == request.FcmToken.Trim());
        if (existing != null)
        {
            if (existing.UsuarioId != userId.Value)
            {
                existing.UsuarioId = userId.Value;
                existing.Platform = platform;
                existing.LastActiveAt = DateTime.UtcNow;
                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Ok(new DeviceDto
            {
                Id = existing.Id,
                FcmToken = existing.FcmToken,
                Platform = existing.Platform,
                LastActiveAt = existing.LastActiveAt,
                UsuarioId = existing.UsuarioId,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = existing.UpdatedAt
            });
        }

        var device = new Device
        {
            FcmToken = request.FcmToken.Trim(),
            Platform = platform,
            UsuarioId = userId.Value,
            LastActiveAt = DateTime.UtcNow
        };
        _context.Devices.Add(device);
        await _context.SaveChangesAsync();
        return Ok(new DeviceDto
        {
            Id = device.Id,
            FcmToken = device.FcmToken,
            Platform = device.Platform,
            LastActiveAt = device.LastActiveAt,
            UsuarioId = device.UsuarioId,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt
        });
    }

    [HttpGet("devices")]
    public async Task<IActionResult> GetDevices()
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        var list = await _context.Devices
            .Where(d => d.UsuarioId == userId.Value)
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new DeviceDto
            {
                Id = d.Id,
                FcmToken = d.FcmToken,
                Platform = d.Platform,
                LastActiveAt = d.LastActiveAt,
                UsuarioId = d.UsuarioId,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("devices/{id:int}")]
    public async Task<IActionResult> GetDevice(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        var device = await _context.Devices
            .FirstOrDefaultAsync(dev => dev.Id == id && dev.UsuarioId == userId.Value);
        if (device == null) return NotFound();
        return Ok(new DeviceDto
        {
            Id = device.Id,
            FcmToken = device.FcmToken,
            Platform = device.Platform,
            LastActiveAt = device.LastActiveAt,
            UsuarioId = device.UsuarioId,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt
        });
    }

    [HttpPost("devices/refresh-token")]
    public async Task<IActionResult> RefreshDeviceToken([FromBody] UpdateDeviceTokenRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.CurrentFcmToken) || string.IsNullOrWhiteSpace(request.NewFcmToken))
            return BadRequest(new { error = "CurrentFcmToken y NewFcmToken son requeridos" });

        var device = await _context.Devices.FirstOrDefaultAsync(d =>
            d.FcmToken == request.CurrentFcmToken.Trim() && d.UsuarioId == userId.Value);
        if (device == null) return NotFound(new { error = "Dispositivo no encontrado" });

        device.FcmToken = request.NewFcmToken.Trim();
        device.Platform = string.IsNullOrWhiteSpace(request.Platform) ? device.Platform : request.Platform.Trim();
        device.LastActiveAt = DateTime.UtcNow;
        device.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new DeviceDto
        {
            Id = device.Id,
            FcmToken = device.FcmToken,
            Platform = device.Platform,
            LastActiveAt = device.LastActiveAt,
            UsuarioId = device.UsuarioId,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt
        });
    }

    [HttpDelete("devices/{id:int}")]
    public async Task<IActionResult> DeleteDevice(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        var device = await _context.Devices
            .FirstOrDefaultAsync(dev => dev.Id == id && dev.UsuarioId == userId.Value);
        if (device == null) return NotFound();
        _context.Devices.Remove(device);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // --- Send (Admin) ---
    [HttpPost("send")]
    [Authorize(Policy = "Administrador")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request)
    {
        var template = await _context.NotificationTemplates.FindAsync(request.TemplateId);
        if (template == null) return NotFound(new { error = "Plantilla no encontrada" });

        List<Device> devices;
        if (request.UserIds != null && request.UserIds.Count > 0)
            devices = await _context.Devices
                .Where(d => request.UserIds.Contains(d.UsuarioId) && !string.IsNullOrEmpty(d.FcmToken))
                .ToListAsync();
        else
            devices = await _context.Devices.Where(d => !string.IsNullOrEmpty(d.FcmToken)).ToListAsync();

        if (devices.Count == 0)
            return Ok(new SendNotificationResponse
            {
                Success = true,
                Message = "No hay dispositivos registrados para enviar",
                SentCount = 0,
                FailedCount = 0,
                TotalDevices = 0
            });

        try
        {
            var extraData = request.ExtraData ?? new Dictionary<string, string>();
            await _pushService.SendPushNotificationAsync(template, devices, extraData, request.DataOnly);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SendNotificationResponse
            {
                Success = false,
                Message = ex.Message,
                SentCount = 0,
                FailedCount = devices.Count,
                TotalDevices = devices.Count
            });
        }

        return Ok(new SendNotificationResponse
        {
            Success = true,
            Message = "Envío solicitado",
            SentCount = devices.Count,
            FailedCount = 0,
            TotalDevices = devices.Count
        });
    }

    // --- Logs (usuario) ---
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = _context.NotificationLogs
            .Where(l => l.UsuarioId == userId.Value)
            .OrderByDescending(l => l.SentAt);
        var total = await query.CountAsync();
        var list = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new NotificationLogDto
            {
                Id = l.Id,
                Status = l.Status,
                Payload = l.Payload,
                SentAt = l.SentAt,
                DeviceId = l.DeviceId,
                TemplateId = l.TemplateId,
                UsuarioId = l.UsuarioId,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        // Rellenar title y body desde payload (formato: {"messageId":"...", "title":"...", "body":"..."})
        foreach (var item in list)
        {
            if (!string.IsNullOrEmpty(item.Payload))
            {
                try
                {
                    using var doc = JsonDocument.Parse(item.Payload);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("title", out var titleEl))
                        item.Title = titleEl.GetString();
                    if (root.TryGetProperty("body", out var bodyEl))
                        item.Body = bodyEl.GetString();
                }
                catch { /* logs antiguos pueden tener otro formato; title/body quedan null */ }
            }
        }

        return Ok(new { total, page, pageSize, items = list });
    }

    [HttpPost("logs/{id:int}/opened")]
    public async Task<IActionResult> MarkLogOpened(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        var log = await _context.NotificationLogs.FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == userId.Value);
        if (log == null) return NotFound();
        log.Status = "opened";
        log.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("logs/{id:int}")]
    public async Task<IActionResult> DeleteLog(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        var log = await _context.NotificationLogs.FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == userId.Value);
        if (log == null) return NotFound();
        _context.NotificationLogs.Remove(log);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("logs/opened-all")]
    public async Task<IActionResult> MarkAllLogsOpened()
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        var logs = await _context.NotificationLogs.Where(l => l.UsuarioId == userId.Value).ToListAsync();
        foreach (var l in logs)
        {
            l.Status = "opened";
            l.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
