using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuscaYa.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace BuscaYa.Controllers.API;

[ApiController]
[Route("api/v1/push/notificationlog")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationLogController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotificationLogController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int? GetUserId()
    {
        var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(v, out var id) ? id : null;
    }

    [HttpPost("{id:int}/opened")]
    public async Task<IActionResult> MarkOpened(int id)
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        var log = await _context.NotificationLogs.FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == userId.Value);
        if (log == null) return NotFound();
        _context.NotificationLogs.Remove(log);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("opened-all")]
    public async Task<IActionResult> MarkAllOpened()
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
